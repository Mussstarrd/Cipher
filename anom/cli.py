"""`python -m anom <command>`.

  ingest   analyze every new audio file in the inbox, cut previews, update the catalogue
  analyze  print the analysis of one file (no catalogue write)
  package  write the BeatStars package for one beat (step 3b)
  video    render the beat-of-the-day video for one beat (step 3c)
  list     show the catalogue
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from anom import catalog as cat
from anom.analysis import analyze, sha1_of
from anom.config import AUDIO_EXT, CATALOG, INBOX, OUT, PREVIEW_SECONDS, SUNO_PLAN
from anom.preview import cut_preview, ffmpeg_available


def _audio_files(folder: Path) -> list[Path]:
    return sorted(p for p in folder.iterdir() if p.is_file() and p.suffix.lower() in AUDIO_EXT)


def cmd_analyze(args: argparse.Namespace) -> int:
    a = analyze(Path(args.file), PREVIEW_SECONDS)
    print(json.dumps(a.as_dict(), indent=2))
    return 0


def cmd_ingest(args: argparse.Namespace) -> int:
    inbox, out, catalog_path = Path(args.inbox), Path(args.out), Path(args.catalog)
    if not inbox.exists():
        print(f"inbox not found: {inbox}", file=sys.stderr)
        return 2
    data = cat.load(catalog_path)
    files = _audio_files(inbox)
    if not files:
        print(f"nothing in {inbox}")
        return 0
    have_ffmpeg = ffmpeg_available()
    if not have_ffmpeg:
        print("ffmpeg not found; previews skipped (set ANOM_FFMPEG or install ffmpeg)", file=sys.stderr)
    done = 0
    for f in files:
        sha = sha1_of(f)
        existing = cat.find_by_sha1(data, sha)
        if existing and not args.force:
            if args.render_date and not existing["rights"].get("render_date"):
                existing["rights"]["render_date"] = args.render_date
                cat.save(data, catalog_path)
                print(f"skip  {f.name} (already in catalogue; render date set to {args.render_date})")
            else:
                print(f"skip  {f.name} (already in catalogue)")
            continue
        a = analyze(f, PREVIEW_SECONDS)
        bid = cat.beat_id(f.stem, a.sha1)
        entry = cat.new_entry(bid, f, a.as_dict(), args.suno_plan, args.render_date)
        beat_dir = out / bid
        beat_dir.mkdir(parents=True, exist_ok=True)
        if have_ffmpeg:
            preview = cut_preview(f, a.hook_start_s, beat_dir / "preview.mp3", PREVIEW_SECONDS)
            entry["analysis"]["preview_path"] = str(preview.relative_to(out.parent)) if preview.is_relative_to(out.parent) else str(preview)
        cat.upsert(data, entry)
        cat.save(data, catalog_path)
        flags = []
        if a.bpm_confidence < 0.5:
            flags.append("BPM?")
        if a.key_confidence < 0.3:
            flags.append("KEY?")
        print(f"added {bid}: {a.bpm:.0f} bpm (alt {a.bpm_alt:.0f}), {a.key} {a.key_camelot}, {a.lufs:.1f} LUFS, hook {a.hook_start_s:.1f}s {' '.join(flags)}")
        done += 1
    print(f"{done} new, catalogue at {catalog_path}")
    return 0


def cmd_video(args: argparse.Namespace) -> int:
    from anom.video import render_beat_of_day
    data = cat.load(Path(args.catalog))
    e = data.get("beats", {}).get(args.beat_id)
    if not e:
        print(f"no such beat: {args.beat_id}", file=sys.stderr)
        return 2
    a = e["analysis"]
    bpm = a["bpm"] if a["bpm"] >= 100 else a["bpm_alt"]   # print the fast reading for trap
    audio = Path(args.audio) if args.audio else INBOX / e["source_file"]
    title = args.title or e.get("package", {}).get("title") or e["id"].rsplit("-", 1)[0].replace("-", " ").upper()
    hook = args.hook_text or e.get("anomaly") or "the anomaly in this one\nis the harmonica"
    out = Path(args.out) if args.out else OUT / e["id"] / "beat-of-the-day.mp4"
    render_beat_of_day(Path(args.still), audio, a["hook_start_s"], out, int(round(bpm)), a["key"], hook, title, args.seconds)
    print(out)
    return 0


def cmd_package(args: argparse.Namespace) -> int:
    from anom.package import build_package
    catalog_path = Path(args.catalog)
    data = cat.load(catalog_path)
    e = data.get("beats", {}).get(args.beat_id)
    if not e:
        print(f"no such beat: {args.beat_id}", file=sys.stderr)
        return 2
    if e.get("package") and not args.force:
        print(f"{args.beat_id} is already packaged (use --force to rebuild)")
        return 0
    out_root = Path(args.out)
    audio = Path(args.audio) if args.audio else Path(args.inbox) / e["source_file"]
    preview = out_root / e["id"] / "preview.mp3"
    pkg = build_package(
        e, audio, Path(args.still), out_root / e["id"] / "package", out_root.parent,
        title=args.title, artist=args.artist, lane=args.lane, anomaly=args.anomaly,
        type_beat=not args.no_type_beat, preview=preview,
    )
    e["package"] = pkg
    if args.anomaly:
        e["anomaly"] = args.anomaly
    if e.get("status") == "analyzed":
        e["status"] = "packaged"
    e["updated_at"] = cat._now()
    cat.save(data, catalog_path)
    print(f"packaged {e['id']}: {pkg['display_title']}  {pkg['bpm']} bpm  {pkg['key']}  {' '.join(pkg['flags'])}")
    print(out_root / e["id"] / "package" / "copy-fields.txt")
    if not audio.exists():
        print(f"note: source audio not found at {audio}; no tagged mp3 written", file=sys.stderr)
    return 0


def cmd_list(args: argparse.Namespace) -> int:
    data = cat.load(Path(args.catalog))
    beats = data.get("beats", {})
    if not beats:
        print("catalogue is empty")
        return 0
    for bid, e in sorted(beats.items()):
        a = e["analysis"]
        print(f"{bid:40} {e['status']:9} {a['bpm']:>5.0f} bpm  {a['key']:9} {a['lufs']:>6.1f} LUFS  {e['rights']['plan']}")
    return 0


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(prog="anom", description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)

    s = sub.add_parser("ingest", help="analyze new files in the inbox")
    s.add_argument("--inbox", default=str(INBOX))
    s.add_argument("--out", default=str(OUT))
    s.add_argument("--catalog", default=str(CATALOG))
    s.add_argument("--suno-plan", default=SUNO_PLAN)
    s.add_argument("--render-date", default=None, help="YYYY-MM-DD the renders were made (rights record)")
    s.add_argument("--force", action="store_true", help="re-analyze files already in the catalogue")
    s.set_defaults(fn=cmd_ingest)

    s = sub.add_parser("analyze", help="analyze one file and print JSON")
    s.add_argument("file")
    s.set_defaults(fn=cmd_analyze)

    s = sub.add_parser("video", help="render the beat-of-the-day video for one catalogue entry")
    s.add_argument("beat_id")
    s.add_argument("--still", default="docs/publicity/reference/persona-three-v1.jpg")
    s.add_argument("--audio", default=None, help="source audio (default: inbox/<source_file>)")
    s.add_argument("--hook-text", default=None)
    s.add_argument("--title", default=None)
    s.add_argument("--seconds", type=float, default=20.0)
    s.add_argument("--out", default=None)
    s.add_argument("--catalog", default=str(CATALOG))
    s.set_defaults(fn=cmd_video)

    s = sub.add_parser("package", help="write the BeatStars package for one catalogue entry")
    s.add_argument("beat_id")
    s.add_argument("--title", default=None, help='beat name, e.g. "BRASS ALONE" (default: from the id)')
    s.add_argument("--artist", default=None, help='artist for the "<Artist> Type Beat" title; default is the lane')
    s.add_argument("--lane", default=None, help="trap, pain-piano, cold-southern, soul-slice, west-coast-bounce, guitar-emo, pop-trap, trunk-808, slow-and-sad, weird")
    s.add_argument("--anomaly", default=None, help='the named anomaly, e.g. "the brass that trips on three"')
    s.add_argument("--no-type-beat", action="store_true", help="leave the Type Beat handle out of the title")
    s.add_argument("--still", default="docs/publicity/reference/persona-three-v1.jpg")
    s.add_argument("--audio", default=None, help="source audio (default: inbox/<source_file>)")
    s.add_argument("--inbox", default=str(INBOX))
    s.add_argument("--out", default=str(OUT))
    s.add_argument("--catalog", default=str(CATALOG))
    s.add_argument("--force", action="store_true", help="rebuild an existing package")
    s.set_defaults(fn=cmd_package)

    s = sub.add_parser("list", help="show the catalogue")
    s.add_argument("--catalog", default=str(CATALOG))
    s.set_defaults(fn=cmd_list)

    args = p.parse_args(argv)
    return args.fn(args)
