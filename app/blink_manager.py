"""
BlinkManager — owns the authenticated Blink connection.

Responsibilities:
  * restore the saved session (blink_session.json) created by authenticate.py
  * keep it refreshed on the network's throttle interval
  * expose the camera list + attributes
  * fetch snapshot JPEG bytes (latest thumbnail, or force a fresh capture)

A single instance is shared across the FastAPI app.
"""

from __future__ import annotations

import asyncio
import logging
import os
import time
from typing import Any

from aiohttp import ClientSession

from blinkpy.blinkpy import Blink
from blinkpy.auth import Auth
from blinkpy.helpers.util import json_load

_LOGGER = logging.getLogger("cipher.blink")

SESSION_FILE = os.environ.get("BLINK_SESSION_FILE", "blink_session.json")

# How long a forced snapshot capture is given to land before we re-read the
# thumbnail URL. Blink is cloud-round-trip + camera wake, so this is not instant.
FRESH_CAPTURE_WAIT = float(os.environ.get("CIPHER_FRESH_WAIT", "5.0"))


class BlinkNotReady(RuntimeError):
    """Raised when the manager is used before a successful connect()."""


class CameraNotFound(KeyError):
    """Raised when a requested camera name does not exist."""


class BlinkManager:
    def __init__(self, session_file: str = SESSION_FILE) -> None:
        self.session_file = session_file
        self._session: ClientSession | None = None
        self.blink: Blink | None = None
        self._lock = asyncio.Lock()
        self._last_refresh = 0.0

    # ------------------------------------------------------------------ setup
    async def connect(self) -> None:
        """Restore the saved session and complete Blink setup."""
        if not os.path.exists(self.session_file):
            raise BlinkNotReady(
                f"No session file at '{self.session_file}'. "
                "Run `python authenticate.py` first."
            )

        self._session = ClientSession()
        creds = await json_load(self.session_file)
        blink = Blink(session=self._session)
        blink.auth = Auth(creds, no_prompt=True, session=self._session)

        ok = await blink.start()
        if not ok or not blink.available:
            await self._session.close()
            raise BlinkNotReady(
                "Blink setup failed. The saved session may be expired or revoked — "
                "re-run `python authenticate.py`."
            )

        self.blink = blink
        self._last_refresh = time.time()
        # Persist any refreshed tokens so restarts stay painless.
        await blink.save(self.session_file)
        _LOGGER.info("Connected. Cameras: %s", ", ".join(blink.cameras.keys()))

    async def close(self) -> None:
        if self._session and not self._session.closed:
            await self._session.close()

    # --------------------------------------------------------------- refresh
    async def refresh(self, force: bool = False) -> None:
        """Refresh camera state, respecting Blink's throttle unless forced."""
        if self.blink is None:
            raise BlinkNotReady("connect() has not been called")
        async with self._lock:
            await self.blink.refresh(force=force)
            self._last_refresh = time.time()

    # ---------------------------------------------------------------- lookup
    def _require(self) -> Blink:
        if self.blink is None:
            raise BlinkNotReady("connect() has not been called")
        return self.blink

    def camera_names(self) -> list[str]:
        return list(self._require().cameras.keys())

    def get_camera(self, name: str):
        blink = self._require()
        cam = blink.cameras.get(name)
        if cam is None:
            raise CameraNotFound(name)
        return cam

    def list_cameras(self) -> list[dict[str, Any]]:
        """Return a JSON-serializable summary of every camera."""
        out: list[dict[str, Any]] = []
        for name, cam in self._require().cameras.items():
            attrs = dict(getattr(cam, "attributes", {}) or {})
            out.append(
                {
                    "name": name,
                    "serial": attrs.get("serial"),
                    "type": getattr(cam, "camera_type", "") or attrs.get("type"),
                    "network": attrs.get("network_id"),
                    "battery": attrs.get("battery"),
                    "battery_level": attrs.get("battery_level"),
                    "temperature": attrs.get("temperature"),
                    "wifi_strength": attrs.get("wifi_strength"),
                    "motion_detected": attrs.get("motion_detected"),
                    "thumbnail": attrs.get("thumbnail"),
                    "last_record": attrs.get("last_record"),
                }
            )
        return out

    # -------------------------------------------------------------- snapshot
    async def snapshot(self, name: str, fresh: bool = False) -> bytes:
        """
        Return current snapshot JPEG bytes for a camera.

        fresh=True commands the camera to capture a new frame first. That wakes
        battery cameras and costs battery, so the dashboard defaults to the
        latest cached thumbnail and only captures fresh on demand.
        """
        cam = self.get_camera(name)

        if fresh:
            async with self._lock:
                await cam.snap_picture()
            await asyncio.sleep(FRESH_CAPTURE_WAIT)
            await self.refresh(force=True)

        # get_media() downloads the current thumbnail and returns an aiohttp
        # response; fall back to any cached image bytes.
        response = await cam.get_media()
        if response is not None and response.status == 200:
            return await response.read()

        cached = cam.image_from_cache
        if cached:
            return cached

        raise BlinkNotReady(
            f"No image available yet for '{name}'. Try again in a few seconds."
        )
