"""Cover art for the beat store: 3000 x 3000 from the persona still plus the
catalogue data. Red on black, Impact-style caps, the "— ANOM" sign-off, and
the AI-character mark, because the face on it is synthetic.

Pillow only. The still is centre-cropped to a square, biased toward the top
so a 3:4 portrait keeps the face; a black gradient rises from the bottom so
the type reads on any picture.
"""
from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from anom.video import FONT_BOLD_CANDIDATES, FONT_CANDIDATES, find_font

RED = (255, 45, 45)
WHITE = (255, 255, 255)
GREY = (200, 200, 200)
AI_MARK = "AI CHARACTER"
BRAND = "ANOM CULTURE"
SIGN = "— ANOM"

# Impact is the storefront's face. Windows ships it; on Linux and macOS the
# bold candidates from video.py stand in.
DISPLAY_FONT_CANDIDATES = (
    "C:/Windows/Fonts/impact.ttf",
    "/usr/share/fonts/truetype/msttcorefonts/Impact.ttf",
    "/Library/Fonts/Impact.ttf",
    "/System/Library/Fonts/Supplemental/Impact.ttf",
) + FONT_BOLD_CANDIDATES


def display_font() -> str:
    return os.environ.get("ANOM_FONT_DISPLAY") or find_font(DISPLAY_FONT_CANDIDATES)


def _square(im: Image.Image, size: int, top_bias: float = 0.35) -> Image.Image:
    w, h = im.size
    side = min(w, h)
    if w > h:
        left, top = (w - side) // 2, 0
    else:
        left, top = 0, int((h - side) * top_bias)
    return im.crop((left, top, left + side, top + side)).resize((size, size), Image.LANCZOS)


def _gradient(size: int, start: float = 0.50, peak: float = 0.88) -> Image.Image:
    """Black that fades in from `start` of the height to `peak` opacity at the bottom."""
    mask = Image.linear_gradient("L").resize((1, size))     # 0 at top, 255 at bottom
    px = mask.load()
    y0 = int(size * start)
    for y in range(size):
        t = 0.0 if y < y0 else (y - y0) / max(1, size - y0)
        px[0, y] = int(255 * peak * (t ** 1.4))
    mask = mask.resize((size, size))
    black = Image.new("RGBA", (size, size), (0, 0, 0, 255))
    black.putalpha(mask)
    return black


def _fit(draw: ImageDraw.ImageDraw, text: str, font_path: str, max_w: int, start: int, floor: int = 60) -> ImageFont.FreeTypeFont:
    size = start
    while size > floor:
        f = ImageFont.truetype(font_path, size)
        if draw.textlength(text, font=f) <= max_w:
            return f
        size -= 8
    return ImageFont.truetype(font_path, floor)


def make_cover(still: Path, out: Path, title: str, bpm: int, key: str, size: int = 3000, quality: int = 92) -> Path:
    out.parent.mkdir(parents=True, exist_ok=True)
    base = _square(Image.open(still).convert("RGB"), size).convert("RGBA")
    base.alpha_composite(_gradient(size))
    d = ImageDraw.Draw(base)
    margin = int(size * 0.06)
    disp, bold, reg = display_font(), find_font(FONT_BOLD_CANDIDATES), find_font(FONT_CANDIDATES)

    # red bar across the top; brand left, AI mark right
    d.rectangle((0, 0, size, int(size * 0.012)), fill=RED)
    small = ImageFont.truetype(reg, int(size * 0.028))
    d.text((margin, int(size * 0.035)), BRAND, font=small, fill=WHITE)
    w = d.textlength(AI_MARK, font=small)
    d.text((size - margin - w, int(size * 0.035)), AI_MARK, font=small, fill=GREY)

    # title, big, red, Impact caps, shrunk to fit one line
    title_u = title.upper()
    tf = _fit(d, title_u, disp, size - 2 * margin, int(size * 0.17))
    tb = d.textbbox((0, 0), title_u, font=tf)
    th = tb[3] - tb[1]
    y_title = size - margin - int(size * 0.20) - th
    d.text((margin - tb[0], y_title - tb[1]), title_u, font=tf, fill=RED)

    # BPM · KEY under it, white
    meta = f"{bpm} BPM  ·  {key.upper()}"
    mf = ImageFont.truetype(bold, int(size * 0.045))
    y_meta = y_title + th + int(size * 0.03)
    d.text((margin, y_meta), meta, font=mf, fill=WHITE)

    # sign-off bottom right
    sf = ImageFont.truetype(disp, int(size * 0.05))
    sw = d.textlength(SIGN, font=sf)
    d.text((size - margin - sw, size - margin - int(size * 0.05)), SIGN, font=sf, fill=RED)

    base.convert("RGB").save(out, "JPEG", quality=quality, optimize=True)
    return out
