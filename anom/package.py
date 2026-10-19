"""Step 3b: the BeatStars package.

BeatStars has no upload API, so the pipeline writes everything the Studio
form asks for and the owner pastes it: a title in the "Type Beat" search
convention, BPM and key, tags, genre and moods, the description in the
brand voice, a 3000 x 3000 cover, the lease tiers with prices, and one
`copy-fields.txt` block to paste from. All of it lands in
`out/<beat-id>/package/` and is mirrored into the catalogue under `package`.

Rules carried from docs/publicity/identity.md:
  * "type beat" appears once, in the title and tags, never in the voice.
  * The anomaly is named. If nobody has named it yet the description carries
    a visible placeholder and the entry is flagged ANOMALY? for the queue.
  * Banned words never reach a tag or the description.
"""
from __future__ import annotations

import json
import re
import shutil
import subprocess
from datetime import datetime, timezone
from pathlib import Path

from anom.config import BRAND, FFMPEG, PRICES, STORE_URL

ANOMALY_PLACEHOLDER = "[name the anomaly]"
BANNED = {"fire", "vibes", "vibe", "banger", "free", "free for profit", "ai-powered", "ai powered"}
MAX_TAGS = 10
LOSSLESS = {".wav", ".flac", ".aiff", ".aif"}

# The lanes the ONE OF ONE generator rolls (identity.md), mapped to what the
# BeatStars form wants. Genre stays inside the names BeatStars already has.
LANES: dict[str, dict] = {
    "trap":              {"label": "Trap",              "genre": "Trap",    "moods": ["dark", "heavy", "hard"],            "tags": ["trap", "808"]},
    "pain-piano":        {"label": "Pain Piano",        "genre": "Trap",    "moods": ["sad", "emotional", "dark"],         "tags": ["pain", "piano", "trap"]},
    "cold-southern":     {"label": "Cold Southern",     "genre": "Trap",    "moods": ["cold", "dark", "heavy"],            "tags": ["southern", "trap", "808"]},
    "soul-slice":        {"label": "Soul Slice",        "genre": "Hip Hop", "moods": ["soulful", "warm", "sampled"],       "tags": ["soul", "sample", "boom bap"]},
    "west-coast-bounce": {"label": "West Coast Bounce", "genre": "Hip Hop", "moods": ["bouncy", "funky", "confident"],     "tags": ["west coast", "bounce", "g funk"]},
    "guitar-emo":        {"label": "Guitar Emo",        "genre": "Trap",    "moods": ["emotional", "sad", "melodic"],      "tags": ["guitar", "emo", "trap"]},
    "pop-trap":          {"label": "Pop Trap",          "genre": "Pop",     "moods": ["catchy", "melodic", "bright"],      "tags": ["pop", "trap", "melodic"]},
    "trunk-808":         {"label": "Trunk 808",         "genre": "Trap",    "moods": ["heavy", "hard", "aggressive"],      "tags": ["808", "trunk", "trap"]},
    "slow-and-sad":      {"label": "Slow and Sad",      "genre": "Trap",    "moods": ["slow", "sad", "ballad"],            "tags": ["slow", "sad", "trap"]},
    "weird":             {"label": "Weird",             "genre": "Hip Hop", "moods": ["weird", "off-grid", "strange"],     "tags": ["experimental", "weird"]},
}
DEFAULT_LANE = "trap"

# Lease tiers, prices from proposals.md D.7. Trackout needs stems, and a Suno
# render has none until a stem split exists, so it stays in the list but is
# marked unavailable and left out of the paste block.
TIERS = (
    {"tier": "mp3",       "name": "MP3 Lease",       "files": ["mp3"],                  "needs_stems": False},
    {"tier": "wav",       "name": "WAV Lease",       "files": ["mp3", "wav"],           "needs_stems": False},
    {"tier": "trackout",  "name": "Trackout Lease",  "files": ["mp3", "wav", "stems"],  "needs_stems": True},
    {"tier": "unlimited", "name": "Unlimited Lease", "files": ["mp3", "wav"],           "needs_stems": False},
    {"tier": "exclusive", "name": "Exclusive",       "files": ["mp3", "wav", "stems"],  "needs_stems": False},
)


def _now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def lane_key(name: str | None) -> str:
    if not name:
        return DEFAULT_LANE
    s = re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")
    if s in LANES:
        return s
    for k, v in LANES.items():
        if s == re.sub(r"[^a-z0-9]+", "-", v["label"].lower()):
            return k
    raise ValueError(f"unknown lane {name!r}; one of {', '.join(LANES)}")


def display_bpm(analysis: dict) -> int:
    """Trap is listed at the fast reading; the tracker often halves it."""
    bpm = analysis["bpm"] if analysis["bpm"] >= 100 else analysis["bpm_alt"]
    return int(round(bpm))


def default_title(beat_id: str) -> str:
    return beat_id.rsplit("-", 1)[0].replace("-", " ").upper()


def type_beat_title(title: str, artist: str | None, lane: str, type_beat: bool = True) -> str:
    """'"BRASS ALONE" | Artist Type Beat'. The name first, the search handle once."""
    head = f'"{title.upper()}"'
    if not type_beat:
        return head
    who = artist.strip() if artist else LANES[lane]["label"]
    return f"{head} | {who} Type Beat"


def _clean(s: str) -> str:
    return re.sub(r"\s+", " ", s.strip().lower())


def build_tags(title: str, artist: str | None, lane: str, key: str, bpm: int, anomaly: str | None) -> list[str]:
    cands: list[str] = []
    if artist:
        cands.append(f"{artist} type beat")
    cands.append(f"{LANES[lane]['label']} type beat")
    cands += LANES[lane]["tags"]
    cands.append(LANES[lane]["genre"])
    cands += [f"{key}", f"{bpm} bpm", BRAND, "one of one"]
    if anomaly:
        for w in ("harmonica", "erhu", "brass", "slur", "triplet"):
            if w in anomaly.lower():
                cands.append(w)
    out: list[str] = []
    for c in cands:
        c = _clean(c)
        if c and c not in out and not any(b == c or f" {b} " in f" {c} " for b in BANNED):
            out.append(c)
    return out[:MAX_TAGS]


def tier_prices() -> list[dict]:
    rows = []
    for t in TIERS:
        price = PRICES.get(t["tier"])
        rows.append({**t, "price_usd": price, "available": not t["needs_stems"]})
    return rows


def price_line(tiers: list[dict]) -> str:
    parts = []
    for t in tiers:
        if not t["available"]:
            continue
        parts.append(f"{t['name']} by offer" if t["price_usd"] is None else f"{t['name']} ${t['price_usd']:.2f}")
    return " · ".join(parts)


def description(title: str, bpm: int, key: str, camelot: str, lane: str, anomaly: str | None, tiers: list[dict]) -> str:
    a = anomaly.strip().rstrip(".") if anomaly else ANOMALY_PLACEHOLDER
    lines = [
        f"{title.upper()}. {bpm} BPM, {key} ({camelot}).",
        f"The anomaly in this one is {a}.",
        f"{LANES[lane]['label']}. Hard cuts only, straight in on beat 1. Mixed to radio standard.",
        "",
        price_line(tiers) + ".",
        f"Instant delivery. {STORE_URL}",
        "",
        "— ANOM",
    ]
    text = "\n".join(lines)
    low = text.lower()
    for b in BANNED:
        assert f" {b} " not in f" {low} ", f"banned word {b!r} reached the description"
    return text


def copy_fields(pkg: dict) -> str:
    """One block to paste from, in the order the BeatStars Studio form asks."""
    rows = [
        ("Title", pkg["display_title"]),
        ("Genre", pkg["genre"]),
        ("Moods", ", ".join(pkg["moods"])),
        ("BPM", str(pkg["bpm"])),
        ("Key", pkg["key"]),
        ("Tags", ", ".join(pkg["tags"])),
        ("Description", pkg["description"]),
        ("Licenses", "\n".join(
            f"  {t['name']}: " + ("by offer" if t["price_usd"] is None else f"${t['price_usd']:.2f}")
            + ("" if t["available"] else "  (not listed: no stems yet)")
            for t in pkg["licenses"])),
        ("Cover", pkg["files"]["cover"]),
        ("Audio", ", ".join(v for k, v in pkg["files"].items() if k in ("mp3", "wav") and v)),
    ]
    out = []
    for k, v in rows:
        out.append(f"=== {k} ===")
        out.append(v)
        out.append("")
    if pkg["flags"]:
        out.append("=== CHECK BEFORE UPLOAD ===")
        out.append(", ".join(pkg["flags"]))
        out.append("")
    return "\n".join(out)


def _tag_audio(src: Path, dst: Path, cover: Path, pkg: dict, encode: bool) -> None:
    """Write the deliverable with ID3 tags and the cover embedded. A Suno MP3 is
    copied, not re-encoded; a lossless source is encoded to 320k."""
    meta = [
        "-metadata", f"title={pkg['display_title']}",
        "-metadata", f"artist={BRAND}",
        "-metadata", f"album={BRAND}",
        "-metadata", f"comment={STORE_URL}",
        "-metadata", f"TBPM={pkg['bpm']}",
        "-metadata", f"TKEY={pkg['key']}",
        "-metadata", f"genre={pkg['genre']}",
    ]
    if dst.suffix.lower() == ".mp3":
        codec = ["-c:a", "libmp3lame", "-b:a", "320k"] if encode else ["-c:a", "copy"]
        cmd = [FFMPEG, "-y", "-loglevel", "error", "-i", str(src), "-i", str(cover),
               "-map", "0:a", "-map", "1:v", *codec, "-c:v", "mjpeg", "-id3v2_version", "3",
               "-metadata:s:v", "title=Album cover", "-metadata:s:v", "comment=Cover (front)", *meta, str(dst)]
    else:
        cmd = [FFMPEG, "-y", "-loglevel", "error", "-i", str(src), "-map", "0:a", "-c:a", "copy", *meta, str(dst)]
    subprocess.run(cmd, check=True)


def _rel(p: Path | None, root: Path) -> str | None:
    if p is None:
        return None
    try:
        return p.resolve().relative_to(root.resolve()).as_posix()
    except ValueError:
        return p.as_posix()


def build_package(
    entry: dict, source_audio: Path, still: Path, out_dir: Path, root: Path, *,
    title: str | None = None, artist: str | None = None, lane: str | None = None,
    anomaly: str | None = None, type_beat: bool = True, preview: Path | None = None,
) -> dict:
    """Write out_dir/{cover.jpg, <slug>.mp3[, <slug>.wav], preview.mp3, copy-fields.txt,
    metadata.json} and return the package dict for the catalogue."""
    from anom.cover import make_cover

    a = entry["analysis"]
    lane_k = lane_key(lane or entry.get("package", {}).get("lane"))
    title = (title or entry.get("package", {}).get("title") or default_title(entry["id"])).strip()
    anomaly = anomaly if anomaly is not None else entry.get("anomaly")
    bpm, key, camelot = display_bpm(a), a["key"], a["key_camelot"]
    tiers = tier_prices()
    flags = []
    if not anomaly:
        flags.append("ANOMALY?")
    if a.get("bpm_confidence", 1) < 0.5:
        flags.append("BPM?")
    if a.get("key_confidence", 1) < 0.3:
        flags.append("KEY?")

    out_dir.mkdir(parents=True, exist_ok=True)
    slug = entry["id"].rsplit("-", 1)[0]
    cover = make_cover(still, out_dir / "cover.jpg", title, bpm, key)

    pkg = {
        "built_at": _now(),
        "title": title,
        "display_title": type_beat_title(title, artist, lane_k, type_beat),
        "artist_type": artist,
        "lane": lane_k,
        "genre": LANES[lane_k]["genre"],
        "moods": list(LANES[lane_k]["moods"]),
        "bpm": bpm,
        "key": key,
        "camelot": camelot,
        "tags": build_tags(title, artist, lane_k, key, bpm, anomaly),
        "description": description(title, bpm, key, camelot, lane_k, anomaly, tiers),
        "licenses": tiers,
        "stems_available": False,
        "files": {"cover": _rel(cover, root), "mp3": None, "wav": None, "preview": None,
                  "copy_fields": None, "metadata": None},
        "flags": flags,
    }

    have_ffmpeg = shutil.which(FFMPEG) is not None
    if have_ffmpeg and source_audio.exists():
        lossless = source_audio.suffix.lower() in LOSSLESS
        mp3 = out_dir / f"{slug}.mp3"
        _tag_audio(source_audio, mp3, cover, pkg, encode=lossless)
        pkg["files"]["mp3"] = _rel(mp3, root)
        if lossless:
            wav = out_dir / f"{slug}.wav"
            _tag_audio(source_audio, wav, cover, pkg, encode=False)
            pkg["files"]["wav"] = _rel(wav, root)
        else:
            flags.append("WAV? (source is lossy; WAV lease needs a lossless render)")
    if preview and preview.exists():
        shutil.copyfile(preview, out_dir / "preview.mp3")
        pkg["files"]["preview"] = _rel(out_dir / "preview.mp3", root)

    cf = out_dir / "copy-fields.txt"
    pkg["files"]["copy_fields"] = _rel(cf, root)
    md = out_dir / "metadata.json"
    pkg["files"]["metadata"] = _rel(md, root)
    cf.write_text(copy_fields(pkg), encoding="utf-8")
    md.write_text(json.dumps(pkg, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return pkg
