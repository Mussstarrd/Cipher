"""Command line: python3 -m releaseline <command>. Exit 0 ok, 1 user error, 2 unexpected."""

from __future__ import annotations

import argparse
import os
import sys
import traceback
from contextlib import closing
from pathlib import Path

from . import UserError, __version__, media, meta, prompts, settings as settings_mod, tracks
from .db import connect


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(prog="python3 -m releaseline",
                                description="ReleaseLine: walk each track from download to posted.")
    p.add_argument("--version", action="version", version=f"ReleaseLine {__version__}")
    sub = p.add_subparsers(dest="cmd")

    s = sub.add_parser("serve", help="start the local web app")
    s.add_argument("--port", type=int, default=None, help="port (default $RELEASELINE_PORT or 8477)")
    s.add_argument("--no-browser", action="store_true", help="do not open a browser tab")

    n = sub.add_parser("new", help="create a track")
    n.add_argument("title")
    for opt in ("genre", "mood", "artist", "bpm", "key", "hook", "notes"):
        n.add_argument(f"--{opt}", default=None)
    n.add_argument("--energy", choices=tracks.ENERGIES, default="mid")
    n.add_argument("--suno", dest="suno_url", default=None, help="Suno link")

    sub.add_parser("list", help="list tracks with their current stage")

    pr = sub.add_parser("prompts", help="generate Kling prompt variants for a track")
    pr.add_argument("track", help="track id or slug")
    pr.add_argument("--n", type=int, default=4, help="variants, 3-5")
    pr.add_argument("--seed", type=int, default=None)
    pr.add_argument("--preset", default="cinematic", help=", ".join(prompts.PRESETS))
    pr.add_argument("--setting", default="", help="free-text setting")
    pr.add_argument("--aspect", default=None, choices=prompts.ASPECTS)
    pr.add_argument("--shot-len", type=int, default=None, choices=prompts.SHOT_LENS)

    c = sub.add_parser("cut", help="cut the audio clip with ffmpeg (or print the command)")
    c.add_argument("track")
    c.add_argument("--start", required=True, help="m:ss or seconds")
    c.add_argument("--len", dest="length", default=None, help="seconds (default from Settings)")
    c.add_argument("--fade", default=None, help="seconds (default from Settings)")
    c.add_argument("--src", default=None, help="audio file to cut (default: recorded master/source)")

    a = sub.add_parser("assemble", help="assemble shots + clip into the final video")
    a.add_argument("track")
    a.add_argument("--aspect", default=None, choices=prompts.ASPECTS)

    m = sub.add_parser("meta", help="generate titles, descriptions, hashtags and tags")
    m.add_argument("track")
    m.add_argument("--explicit", action="store_true")

    f = sub.add_parser("feedback", help="save a feedback note")
    f.add_argument("text")
    return p


def main(argv=None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if not args.cmd:
        parser.print_help()
        return 0
    try:
        return run_command(args)
    except UserError as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nStopped.")
        return 0
    except Exception as exc:  # noqa: BLE001
        print(f"Unexpected error ({type(exc).__name__}): {exc}", file=sys.stderr)
        if os.environ.get("RELEASELINE_DEBUG"):
            traceback.print_exc()
        else:
            print("Run again with RELEASELINE_DEBUG=1 for details.", file=sys.stderr)
        return 2


def run_command(args) -> int:
    base = settings_mod.data_dir()
    if args.cmd == "serve":
        from .server import serve
        port = args.port
        if port is None:
            raw = os.environ.get("RELEASELINE_PORT", "8477")
            try:
                port = int(raw)
            except ValueError:
                raise UserError(f"RELEASELINE_PORT should be a number, not '{raw}'.")
        if not 1 <= port <= 65535:
            raise UserError("Port must be between 1 and 65535.")
        serve(port, open_browser=not args.no_browser and not os.environ.get("RELEASELINE_NO_BROWSER"),
              base=base)
        return 0

    settings = settings_mod.load(base)
    with closing(connect(base)) as conn:
        if args.cmd == "new":
            fields = {k: getattr(args, k) for k in
                      ("title", "genre", "mood", "artist", "bpm", "key", "hook", "notes", "suno_url", "energy")}
            t = tracks.create_track(conn, settings, fields, base)
            print(f"Created track {t['id']} '{t['title']}' (slug {t['slug']}).")
            print(f"Library folder: {tracks.track_dir(settings, t['slug'], base)}")
            return 0

        if args.cmd == "list":
            items = tracks.list_tracks(conn)
            if not items:
                print("No tracks yet. Create one with: python3 -m releaseline new \"Title\"")
                return 0
            for t in items:
                stage = t["current_stage_title"] or ("Released" if t["status"] == "released" else "All stages done")
                print(f"{t['id']:>3}  {t['title'][:40]:<40}  {t['status']:<9}  {stage}")
            return 0

        if args.cmd == "feedback":
            fb = tracks.add_feedback(conn, args.text, "cli", base)
            print(f"Saved feedback #{fb['id']} to {base / 'feedback.md'}.")
            return 0

        track = tracks.get_track(conn, args.track)

        if args.cmd == "prompts":
            result = prompts.generate(
                track, preset=args.preset, setting=args.setting, aspect=args.aspect or settings["aspect"],
                shot_len=args.shot_len or settings["shot_len"], clip_len=settings["clip_len"], n=args.n,
                seed=args.seed)
            print(prompts.as_text(result))
            return 0

        if args.cmd == "cut":
            result = media.cut_clip(conn, settings, track, args.start, args.length, args.fade, args.src, base)
            print(result["message"])
            if not result["ran"]:
                print("Run this yourself once ffmpeg is installed:")
                print("  " + result["commands"][0])
            return 0

        if args.cmd == "assemble":
            result = media.assemble(conn, settings, track, args.aspect, base)
            print(result["message"])
            if not result["ran"]:
                print(f"Write this list to {result['listfile']}:")
                print(result["listfile_text"])
                print("Then run, in order:")
                for c in result["commands"]:
                    print("  " + c)
            return 0

        if args.cmd == "meta":
            result = meta.generate(track, settings, explicit=args.explicit)
            from .server import _draft_rows
            for kind, rows_ in _draft_rows(result).items():
                tracks.replace_drafts(conn, track["id"], kind, rows_)
            print(meta.as_text(result))
            return 0
    raise UserError(f"Unknown command {args.cmd}.")


if __name__ == "__main__":
    sys.exit(main())
