"""
Cipher — FastAPI backend for Blink home situational awareness.

Routes:
  GET  /                                  dashboard UI
  GET  /api/health                        readiness + camera count
  GET  /api/cameras                       list cameras + state
  POST /api/refresh                       force a state refresh
  GET  /api/cameras/{name}/snapshot.jpg   snapshot (?fresh=1 forces new capture)
  POST /api/cameras/{name}/live/start     begin live view -> {playlist_url}
  POST /api/live/{stream_id}/stop         end a live session
  GET  /live/{stream_id}/{filename}       HLS playlist + segments
"""

from __future__ import annotations

import logging
from contextlib import asynccontextmanager
from pathlib import Path

from fastapi import FastAPI, HTTPException, Query
from fastapi.responses import FileResponse, JSONResponse, RedirectResponse, Response

from .blink_manager import BlinkManager, BlinkNotReady, CameraNotFound
from .livestream_manager import LiveStreamManager, LiveViewUnavailable

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(name)s %(levelname)s %(message)s")
_LOGGER = logging.getLogger("cipher")

STATIC_DIR = Path(__file__).parent / "static"

blink = BlinkManager()
live = LiveStreamManager()


@asynccontextmanager
async def lifespan(app: FastAPI):
    try:
        await blink.connect()
    except BlinkNotReady as exc:
        _LOGGER.error("Blink not ready: %s", exc)
        # Start anyway so the UI can show a helpful error instead of failing to boot.
    yield
    await live.stop_all()
    await blink.close()


app = FastAPI(title="Cipher", lifespan=lifespan)


@app.get("/")
async def index():
    return FileResponse(STATIC_DIR / "index.html")


@app.get("/api/health")
async def health():
    ready = blink.blink is not None
    return {
        "ready": ready,
        "cameras": blink.camera_names() if ready else [],
        "hint": None if ready else "Run `python authenticate.py`, then restart the server.",
    }


@app.get("/api/cameras")
async def cameras():
    if blink.blink is None:
        raise HTTPException(503, "Blink not connected. Run authenticate.py and restart.")
    return {"cameras": blink.list_cameras()}


@app.post("/api/refresh")
async def refresh():
    try:
        await blink.refresh(force=True)
    except BlinkNotReady as exc:
        raise HTTPException(503, str(exc))
    return {"cameras": blink.list_cameras()}


@app.get("/api/cameras/{name}/snapshot.jpg")
async def snapshot(name: str, fresh: bool = Query(False)):
    try:
        data = await blink.snapshot(name, fresh=fresh)
    except CameraNotFound:
        raise HTTPException(404, f"No camera named '{name}'.")
    except BlinkNotReady as exc:
        raise HTTPException(503, str(exc))
    return Response(
        content=data,
        media_type="image/jpeg",
        headers={"Cache-Control": "no-store"},
    )


@app.post("/api/cameras/{name}/live/start")
async def live_start(name: str):
    try:
        camera = blink.get_camera(name)
    except CameraNotFound:
        raise HTTPException(404, f"No camera named '{name}'.")
    except BlinkNotReady as exc:
        raise HTTPException(503, str(exc))

    try:
        session = await live.start(camera)
    except LiveViewUnavailable as exc:
        raise HTTPException(409, str(exc))

    ready = await live.wait_for_playlist(session)
    if not ready:
        await live.stop(session.stream_id)
        raise HTTPException(
            504,
            "Live stream did not produce video in time. The camera may be busy, "
            "offline, or Blink declined the session — try again, or use snapshots.",
        )

    return {
        "stream_id": session.stream_id,
        "playlist_url": f"/live/{session.stream_id}/index.m3u8",
        "camera": name,
    }


@app.post("/api/live/{stream_id}/stop")
async def live_stop(stream_id: str):
    await live.stop(stream_id)
    return {"stopped": stream_id}


@app.get("/live/{stream_id}/{filename}")
async def hls_file(stream_id: str, filename: str):
    # Guard against path traversal; only serve simple HLS artifacts.
    if "/" in filename or ".." in filename or not (
        filename.endswith(".m3u8") or filename.endswith(".ts")
    ):
        raise HTTPException(404, "Not found")

    session = live.get(stream_id)
    if session is None:
        raise HTTPException(404, "Live session not found or ended.")
    session.touch()  # keep-alive: viewer is still watching

    path = session.hls_dir / filename
    if not path.exists():
        raise HTTPException(404, "Segment not ready.")

    media = "application/vnd.apple.mpegurl" if filename.endswith(".m3u8") else "video/mp2t"
    return FileResponse(path, media_type=media, headers={"Cache-Control": "no-store"})
