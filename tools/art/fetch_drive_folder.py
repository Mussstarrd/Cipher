#!/usr/bin/env python3
"""
Download a PUBLIC Google Drive folder, recursively.

    python tools/art/fetch_drive_folder.py <folder-id-or-url> <dest-dir> [--only fbx,txt,jpg]

Why this exists: the free CC0 asset packs this project uses (Quaternius) put their downloads
behind a JavaScript button that resolves to a public Drive folder. There is no direct file URL to
curl, and no API key is available, so this scrapes the folder listing Drive renders for anonymous
visitors and then pulls each file through the ordinary uc?export=download endpoint.

It is deliberately conservative:
  * public folders only, no credentials, no OAuth
  * skips anything already downloaded, so a rerun is cheap and resumable
  * handles Drive's "file too large to virus scan" interstitial, which otherwise silently yields
    an HTML page with a .fbx name on it
"""

from __future__ import annotations

import argparse
import os
import re
import sys
import time
import urllib.parse
import urllib.request

UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
FOLDER_MIME = "application/vnd.google-apps.folder"


def http(url: str, timeout: int = 60) -> bytes:
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.read()


def folder_id(text: str) -> str:
    m = re.search(r"/folders/([-_A-Za-z0-9]{10,60})", text)
    return m.group(1) if m else text.strip()


def list_folder(fid: str):
    """Yields (id, name, is_folder) for a public folder."""
    html = http(f"https://drive.google.com/drive/folders/{fid}").decode("utf-8", "replace")

    # Drive ships the listing inside a big JS array. Entries look like
    #   "<id>",["<parent>"],"<name>",...,"<mime>"
    # Names and mimes are escaped as \" inside a quoted JS string, hence the doubled escapes.
    pattern = re.compile(
        r'\\"([-_A-Za-z0-9]{20,60})\\",\[\\"[-_A-Za-z0-9]{20,60}\\"\],\\"((?:[^\\"]|\\.)*?)\\",\\"([a-zA-Z0-9./+-]+)\\"'
    )
    seen = set()
    for fid_, name, mime in pattern.findall(html):
        if fid_ in seen or fid_ == fid:
            continue
        seen.add(fid_)
        name = name.encode().decode("unicode_escape", "replace")
        yield fid_, name, mime == FOLDER_MIME

    if not seen:
        # Fallback: some renders escape differently. Pull ids and probe each one.
        for fid_ in dict.fromkeys(re.findall(r'"([-_A-Za-z0-9]{28,44})"', html)):
            if fid_ == fid or fid_ in seen:
                continue
            seen.add(fid_)
            yield fid_, "", False


def probe(fid: str):
    """Returns (filename, size) for a file id, or (None, 0) when it is a folder/page."""
    url = f"https://drive.google.com/uc?export=download&id={fid}"
    req = urllib.request.Request(url, headers={"User-Agent": UA}, method="GET")
    try:
        with urllib.request.urlopen(req, timeout=45) as r:
            disp = r.headers.get("Content-Disposition", "")
            ctype = r.headers.get("Content-Type", "")
            size = int(r.headers.get("Content-Length") or 0)
            m = re.search(r'filename="([^"]+)"', disp)
            if m:
                return m.group(1), size
            if "text/html" in ctype:
                body = r.read(200_000).decode("utf-8", "replace")
                m2 = re.search(r'name="uuid" value="([^"]+)"', body)
                m3 = re.search(r"<span class=\"uc-name-size\"><a[^>]*>([^<]+)</a>", body)
                if m2 and m3:
                    return m3.group(1), 0  # large file behind the scan interstitial
            return None, 0
    except Exception:
        return None, 0


def download(fid: str, dest: str) -> bool:
    url = f"https://drive.google.com/uc?export=download&id={fid}&confirm=t"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=600) as r, open(dest, "wb") as f:
            while True:
                chunk = r.read(1 << 16)
                if not chunk:
                    break
                f.write(chunk)
        # A Drive error page is small and starts with markup; a real asset does not.
        if os.path.getsize(dest) < 2048:
            head = open(dest, "rb").read(400).lstrip()
            if head[:1] == b"<":
                os.remove(dest)
                return False
        return True
    except Exception as e:
        print(f"    !! {e}", file=sys.stderr)
        if os.path.exists(dest):
            os.remove(dest)
        return False


def walk(fid: str, dest: str, only: set[str], depth: int = 0, budget=None) -> int:
    os.makedirs(dest, exist_ok=True)
    count = 0
    pad = "  " * depth
    for child, name, is_folder in list_folder(fid):
        if budget is not None and budget[0] <= 0:
            return count
        if is_folder:
            print(f"{pad}[dir ] {name}")
            count += walk(child, os.path.join(dest, safe(name)), only, depth + 1, budget)
            continue

        fname, size = probe(child)
        if not fname:
            # Unknown: might be a folder the listing mislabelled. Try one level down, and treat a
            # 404 as "not a folder" rather than letting it kill the whole walk.
            try:
                sub = list(list_folder(child))
            except Exception:
                sub = []
            if sub:
                print(f"{pad}[dir?] {name or child}")
                count += walk(child, os.path.join(dest, safe(name or child)), only, depth + 1, budget)
            continue

        ext = os.path.splitext(fname)[1].lower().lstrip(".")
        if only and ext not in only:
            continue

        out = os.path.join(dest, safe(fname))
        if os.path.exists(out) and os.path.getsize(out) > 0:
            print(f"{pad}[skip] {fname}")
            continue

        print(f"{pad}[get ] {fname} ({size or '?'} bytes)")
        if download(child, out):
            count += 1
            if budget is not None:
                budget[0] -= 1
        time.sleep(0.4)      # be a polite anonymous visitor
    return count


def safe(name: str) -> str:
    return re.sub(r'[<>:"/\\|?*]+', "_", name).strip() or "unnamed"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("dest")
    ap.add_argument("--only", default="", help="comma-separated extensions to keep, e.g. fbx,txt")
    ap.add_argument("--max", type=int, default=0, help="stop after N files (0 = no limit)")
    args = ap.parse_args()

    only = {e.strip().lower() for e in args.only.split(",") if e.strip()}
    budget = [args.max] if args.max > 0 else None

    n = walk(folder_id(args.folder), args.dest, only, budget=budget)
    print(f"\n{n} file(s) into {args.dest}")
    return 0 if n else 1


if __name__ == "__main__":
    raise SystemExit(main())
