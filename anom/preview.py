"""30-second preview cut at the hook, with short fades, via ffmpeg."""
from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

from anom.config import FFMPEG


def ffmpeg_available() -> bool:
    return shutil.which(FFMPEG) is not None


def cut_preview(src: Path, start_s: float, dst: Path, seconds: float = 30.0, fade_s: float = 0.5) -> Path:
    dst.parent.mkdir(parents=True, exist_ok=True)
    fade_out_at = max(seconds - fade_s, 0)
    cmd = [
        FFMPEG, "-y", "-loglevel", "error",
        "-ss", f"{start_s:.3f}", "-t", f"{seconds:.3f}", "-i", str(src),
        "-af", f"afade=t=in:st=0:d={fade_s},afade=t=out:st={fade_out_at}:d={fade_s}",
        "-codec:a", "libmp3lame", "-b:a", "192k", str(dst),
    ]
    subprocess.run(cmd, check=True)
    return dst
