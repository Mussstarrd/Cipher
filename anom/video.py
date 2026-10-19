"""Vertical video assembly with ffmpeg (step 3c, first format: beat of the day).

Layers, 1080x1920 @ 30 fps:
  1. the character sheet, slow push-in (replaced by desk footage once the library exists)
  2. a red waveform strip drawn from the actual audio
  3. the AI-character label, always, from frame one
  4. the hook text (the named anomaly) and the lower third (BPM, key, store)
  5. 1 s title card, then a hard cut into the beat on the hook's first beat. No fades on picture.
"""
from __future__ import annotations

import os
import shutil
import subprocess
from pathlib import Path

from anom.config import FFMPEG

# Font candidates per platform, first existing file wins. DejaVu on Linux
# (the cloud session), Arial on Windows (the Lenovo) and macOS.
FONT_BOLD_CANDIDATES = (
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    "C:/Windows/Fonts/arialbd.ttf",
    "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
    "/Library/Fonts/Arial Bold.ttf",
)
FONT_CANDIDATES = (
    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    "C:/Windows/Fonts/arial.ttf",
    "/System/Library/Fonts/Supplemental/Arial.ttf",
    "/Library/Fonts/Arial.ttf",
)


def find_font(candidates: tuple[str, ...]) -> str:
    """First font file that exists; env ANOM_FONT / ANOM_FONT_BOLD override. Raises if none."""
    for c in candidates:
        if Path(c).is_file():
            return c
    raise RuntimeError("no usable font found; set ANOM_FONT and ANOM_FONT_BOLD to .ttf paths")


def fonts() -> tuple[str, str]:
    bold = os.environ.get("ANOM_FONT_BOLD") or find_font(FONT_BOLD_CANDIDATES)
    regular = os.environ.get("ANOM_FONT") or find_font(FONT_CANDIDATES)
    return bold, regular

RED = "0xFF2D2D"
LABEL = "AI CHARACTER · ANOM CULTURE"
STORE = "beatstars.com/anomculture"
W, H, FPS = 1080, 1920, 30


def _font(path: str) -> str:
    # Windows paths need the drive colon escaped for ffmpeg's drawtext.
    return path.replace("\\", "/").replace(":", "\\:")


def _esc(text: str) -> str:
    return text.replace("\\", "\\\\").replace(":", "\\:").replace("'", "’").replace("%", "\\%")


def render_beat_of_day(
    still: Path, audio: Path, start_s: float, out: Path,
    bpm: int, key: str, hook_text: str, title: str, seconds: float = 20.0, card_s: float = 1.0,
) -> Path:
    if shutil.which(FFMPEG) is None:
        raise RuntimeError("ffmpeg not found")
    out.parent.mkdir(parents=True, exist_ok=True)
    total = seconds + card_s
    frames = int(total * FPS)
    bold, regular = fonts()
    fb, fr = _font(bold), _font(regular)
    lower = f"{bpm} BPM · {key} · {STORE}"
    vf = (
        # 1. still, cover-crop to 9:16, slow push-in
        f"[0:v]scale={W}:{H}:force_original_aspect_ratio=increase,crop={W}:{H},"
        f"zoompan=z='min(zoom+0.0004,1.12)':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d={frames}:s={W}x{H}:fps={FPS},"
        f"format=yuv420p[bg];"
        # 2. waveform strip from the audio
        f"[1:a]showwaves=s={W}x220:mode=p2p:colors={RED}|{RED}:scale=sqrt:rate={FPS},format=rgba[wave];"
        f"[bg]drawbox=x=0:y={H-440}:w={W}:h=260:color=black@0.45:t=fill[bg2];"
        f"[bg2][wave]overlay=0:{H-420}:shortest=1[v1];"
        # 5. title card: black for the first second, hard cut
        f"[v1]drawbox=x=0:y=0:w={W}:h={H}:color=black:t=fill:enable='lt(t,{card_s})',"
        f"drawtext=fontfile='{fb}':text='{_esc(title)}':fontcolor=white:fontsize=96:x=(w-text_w)/2:y=(h-text_h)/2-60:enable='lt(t,{card_s})',"
        f"drawtext=fontfile='{fr}':text='{_esc('beat of the day')}':fontcolor={RED}:fontsize=44:x=(w-text_w)/2:y=(h-text_h)/2+60:enable='lt(t,{card_s})',"
        # 3. AI label, always
        f"drawtext=fontfile='{fr}':text='{_esc(LABEL)}':fontcolor=white@0.85:fontsize=30:x=48:y=110,"
        # 4. hook text after the cut, lower third always
        f"drawtext=fontfile='{fb}':text='{_esc(hook_text)}':fontcolor=white:fontsize=56:line_spacing=10:x=(w-text_w)/2:y={H-760}:"
        f"box=1:boxcolor=black@0.55:boxborderw=28:enable='gte(t,{card_s})',"
        f"drawtext=fontfile='{fr}':text='{_esc(lower)}':fontcolor=white:fontsize=36:x=(w-text_w)/2:y={H-150}:box=1:boxcolor=black@0.55:boxborderw=18[v]"
    )
    af = f"[1:a]adelay={int(card_s*1000)}|{int(card_s*1000)},afade=t=out:st={total-0.5}:d=0.5[a]"
    cmd = [
        FFMPEG, "-y", "-loglevel", "error",
        "-loop", "1", "-framerate", str(FPS), "-i", str(still),
        "-ss", f"{start_s:.3f}", "-t", f"{seconds:.3f}", "-i", str(audio),
        "-filter_complex", vf + ";" + af,
        "-map", "[v]", "-map", "[a]",
        "-t", f"{total:.3f}", "-r", str(FPS),
        "-c:v", "libx264", "-preset", "medium", "-crf", "20", "-pix_fmt", "yuv420p",
        "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", str(out),
    ]
    subprocess.run(cmd, check=True)
    return out
