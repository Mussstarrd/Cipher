"""
LiveStreamManager — on-demand live video for a Blink camera.

Pipeline:
    camera.init_livestream()        blinkpy authenticates to Blink's immis://
        -> BlinkLiveStream          server and runs a local asyncio TCP server
        -> stream.start()           that emits an MPEG-TS video stream
        -> stream.feed()            (keepalive + polling handled by blinkpy)
        -> ffmpeg reads tcp://...   transcodes/repackages TS -> HLS segments
        -> browser plays HLS        via <video> + hls.js in the dashboard

Live view is inherently heavier than snapshots: it wakes the camera, drains
battery on battery-powered models, and Blink caps concurrent/duration. Each
session therefore auto-stops after an idle timeout and a hard max duration.

Requires `ffmpeg` on PATH.
"""

from __future__ import annotations

import asyncio
import logging
import os
import shutil
import time
import uuid
from pathlib import Path

_LOGGER = logging.getLogger("cipher.live")

HLS_ROOT = Path(os.environ.get("CIPHER_HLS_DIR", "hls_tmp"))
IDLE_TIMEOUT = float(os.environ.get("CIPHER_LIVE_IDLE_TIMEOUT", "20"))   # s since last playlist fetch
MAX_DURATION = float(os.environ.get("CIPHER_LIVE_MAX_DURATION", "180"))  # s hard cap per session
SEGMENT_SECONDS = os.environ.get("CIPHER_HLS_SEGMENT", "1")


class LiveViewUnavailable(RuntimeError):
    """Live view can't be started for this camera/host right now."""


class LiveSession:
    def __init__(self, stream_id: str, camera_name: str, hls_dir: Path):
        self.stream_id = stream_id
        self.camera_name = camera_name
        self.hls_dir = hls_dir
        self.started_at = time.time()
        self.last_access = time.time()
        self._stream = None          # blinkpy BlinkLiveStream
        self._feed_task: asyncio.Task | None = None
        self._ffmpeg: asyncio.subprocess.Process | None = None
        self._watchdog: asyncio.Task | None = None
        self._stopped = False

    @property
    def playlist(self) -> Path:
        return self.hls_dir / "index.m3u8"

    def touch(self) -> None:
        self.last_access = time.time()


class LiveStreamManager:
    def __init__(self, hls_root: Path = HLS_ROOT) -> None:
        self.hls_root = hls_root
        self._sessions: dict[str, LiveSession] = {}
        self._by_camera: dict[str, str] = {}
        self._lock = asyncio.Lock()

    def _ffmpeg_bin(self) -> str:
        exe = shutil.which("ffmpeg")
        if not exe:
            raise LiveViewUnavailable(
                "ffmpeg is not installed or not on PATH. Live view needs it "
                "(macOS: `brew install ffmpeg`, Debian: `apt-get install ffmpeg`). "
                "Snapshots still work without ffmpeg."
            )
        return exe

    async def start(self, camera) -> LiveSession:
        """Start (or reuse) a live session for the given blinkpy camera object."""
        ffmpeg_bin = self._ffmpeg_bin()
        name = camera.name

        async with self._lock:
            # Reuse an existing live session for this camera if one is running.
            existing_id = self._by_camera.get(name)
            if existing_id and existing_id in self._sessions:
                sess = self._sessions[existing_id]
                sess.touch()
                return sess

            stream_id = uuid.uuid4().hex[:12]
            hls_dir = self.hls_root / stream_id
            hls_dir.mkdir(parents=True, exist_ok=True)
            session = LiveSession(stream_id, name, hls_dir)

            # 1) Ask Blink to open a livestream (immis:// TS server).
            try:
                blink_stream = await camera.init_livestream()
            except NotImplementedError as exc:
                shutil.rmtree(hls_dir, ignore_errors=True)
                raise LiveViewUnavailable(
                    f"'{name}' returned an unsupported live-stream format ({exc}). "
                    "This camera model may not support app-style live view; use snapshots."
                ) from exc
            except Exception as exc:  # network / auth / Blink-side failure
                shutil.rmtree(hls_dir, ignore_errors=True)
                raise LiveViewUnavailable(f"Blink refused live view for '{name}': {exc}") from exc

            session._stream = blink_stream

            # 2) Start blinkpy's local TS server + its feed loop.
            await blink_stream.start(host="127.0.0.1", port=0)
            session._feed_task = asyncio.create_task(blink_stream.feed())
            tcp_url = blink_stream.url  # tcp://127.0.0.1:<port>
            _LOGGER.info("Live '%s' TS source at %s -> HLS %s", name, tcp_url, hls_dir)

            # 3) ffmpeg: TS in -> HLS out. Copy video; re-encode audio if present.
            cmd = [
                ffmpeg_bin,
                "-hide_banner", "-loglevel", "warning",
                "-fflags", "nobuffer", "-flags", "low_delay",
                "-analyzeduration", "1000000", "-probesize", "1000000",
                "-i", tcp_url,
                "-c:v", "copy",
                "-c:a", "aac", "-b:a", "64k",
                "-f", "hls",
                "-hls_time", SEGMENT_SECONDS,
                "-hls_list_size", "5",
                "-hls_flags", "delete_segments+append_list+omit_endlist+independent_segments",
                str(session.playlist),
            ]
            try:
                session._ffmpeg = await asyncio.create_subprocess_exec(
                    *cmd,
                    stdout=asyncio.subprocess.DEVNULL,
                    stderr=asyncio.subprocess.PIPE,
                )
            except FileNotFoundError as exc:
                await self._teardown(session)
                raise LiveViewUnavailable("Failed to launch ffmpeg.") from exc

            session._watchdog = asyncio.create_task(self._watch(session))
            self._sessions[stream_id] = session
            self._by_camera[name] = stream_id
            return session

    async def wait_for_playlist(self, session: LiveSession, timeout: float = 15.0) -> bool:
        """Wait until the HLS playlist + first segment exist (or ffmpeg dies)."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            if session._stopped:
                return False
            if session._ffmpeg and session._ffmpeg.returncode is not None:
                err = b""
                if session._ffmpeg.stderr:
                    try:
                        err = await asyncio.wait_for(session._ffmpeg.stderr.read(2048), 0.5)
                    except asyncio.TimeoutError:
                        pass
                _LOGGER.error("ffmpeg exited early for '%s': %s",
                              session.camera_name, err.decode(errors="replace"))
                return False
            if session.playlist.exists() and any(session.hls_dir.glob("*.ts")):
                return True
            await asyncio.sleep(0.3)
        return False

    def get(self, stream_id: str) -> LiveSession | None:
        return self._sessions.get(stream_id)

    async def stop(self, stream_id: str) -> None:
        session = self._sessions.get(stream_id)
        if session:
            await self._teardown(session)

    async def stop_all(self) -> None:
        for sid in list(self._sessions):
            await self.stop(sid)

    # ---------------------------------------------------------------- internal
    async def _watch(self, session: LiveSession) -> None:
        """Auto-stop on idle (no viewer) or hard max duration."""
        try:
            while not session._stopped:
                await asyncio.sleep(2)
                now = time.time()
                if now - session.last_access > IDLE_TIMEOUT:
                    _LOGGER.info("Live '%s' idle; stopping.", session.camera_name)
                    break
                if now - session.started_at > MAX_DURATION:
                    _LOGGER.info("Live '%s' hit max duration; stopping.", session.camera_name)
                    break
                if session._ffmpeg and session._ffmpeg.returncode is not None:
                    break
        finally:
            await self._teardown(session)

    async def _teardown(self, session: LiveSession) -> None:
        if session._stopped:
            return
        session._stopped = True
        self._sessions.pop(session.stream_id, None)
        if self._by_camera.get(session.camera_name) == session.stream_id:
            self._by_camera.pop(session.camera_name, None)

        if session._ffmpeg and session._ffmpeg.returncode is None:
            try:
                session._ffmpeg.terminate()
                await asyncio.wait_for(session._ffmpeg.wait(), 5)
            except (asyncio.TimeoutError, ProcessLookupError):
                try:
                    session._ffmpeg.kill()
                except ProcessLookupError:
                    pass

        if session._stream:
            try:
                session._stream.stop()
            except Exception:
                pass
        if session._feed_task:
            session._feed_task.cancel()

        shutil.rmtree(session.hls_dir, ignore_errors=True)
        _LOGGER.info("Live session %s (%s) torn down.", session.stream_id, session.camera_name)
