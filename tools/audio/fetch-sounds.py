#!/usr/bin/env python3
"""Fetch commercially-usable sounds from Freesound into art/audio/ (gitignored).

WHY THIS EXISTS
---------------
The game's audio is synthesised at startup (`game/Assets/Scripts/Audio/`) and that is right for
the things a pulse weapon and an EMP are: synthetic by nature, and able to respond to tier and
distance because they are generated rather than played back. Synthesis is hopeless at the organic
half -- rain, wind, footsteps, breath, a crowd two streets away -- which is exactly what the owner
asked for, and exactly what Freesound has.

THE LICENCE RULE IS ABSOLUTE
----------------------------
Freesound is a MIXED library. A large share of it is CC-BY-NC, which forbids commercial use and
can never enter this game. Three independent guards, because one is how a non-commercial sample
ends up in a shipped build:

  1. every query is licence-filtered at the API;
  2. every RESULT is re-checked here against ALLOWED before anything is written, so a carelessly
     edited filter in sounds.json cannot smuggle one through;
  3. everything that lands is written into docs/CREDITS-AUDIO.md with its licence and author,
     which is required for CC-BY and free insurance for CC0.

We take the OGG preview, not the original: same licence, Unity reads .ogg natively, a fraction of
the size, and the original usually needs OAuth rather than a token.

USAGE
-----
    python tools/audio/fetch-sounds.py                 # everything in sounds.json
    python tools/audio/fetch-sounds.py weather foley   # only those groups
    python tools/audio/fetch-sounds.py --dry-run       # show what would be taken

The token lives in .secrets/freesound.txt (gitignored). Files land in art/audio/<group>/<slot>/
(gitignored); copy what you actually use into game/Assets/Resources/Audio/, exactly the way the
free model packs work.
"""

import json
import os
import pathlib
import sys
import urllib.parse
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
TOKEN_FILE = ROOT / ".secrets" / "freesound.txt"
MANIFEST = pathlib.Path(__file__).resolve().parent / "sounds.json"
OUT = ROOT / "art" / "audio"
CREDITS = ROOT / "docs" / "CREDITS-AUDIO.md"

API = "https://freesound.org/apiv2/search/text/"

# The only two licences that may ever reach a build. Freesound reports the full deed URL.
ALLOWED = {
    "http://creativecommons.org/publicdomain/zero/1.0/": "CC0",
    "https://creativecommons.org/publicdomain/zero/1.0/": "CC0",
    "http://creativecommons.org/licenses/by/4.0/": "CC-BY 4.0",
    "https://creativecommons.org/licenses/by/4.0/": "CC-BY 4.0",
    "http://creativecommons.org/licenses/by/3.0/": "CC-BY 3.0",
    "https://creativecommons.org/licenses/by/3.0/": "CC-BY 3.0",
}

# Sent to the API so it never returns anything outside the set above in the first place.
LICENCE_FILTER = '(license:"Creative Commons 0" OR license:"Attribution" OR license:"Attribution 4.0")'


def token() -> str:
    if not TOKEN_FILE.exists():
        sys.exit(
            f"No API token at {TOKEN_FILE}.\n"
            "Get one at https://freesound.org/apiv2/apply/ and save it there, one line, nothing else."
        )
    value = TOKEN_FILE.read_text(encoding="utf-8").strip()
    if not value:
        sys.exit(f"{TOKEN_FILE} is empty.")
    return value


def search(key: str, query: str, extra_filter: str, want: int):
    params = {
        "query": query,
        "filter": f"{LICENCE_FILTER} {extra_filter}".strip(),
        "fields": "id,name,license,username,url,duration,previews",
        "sort": "rating_desc",
        # Over-fetch: some results fail the second licence check or have no preview.
        "page_size": str(min(60, max(want * 4, 10))),
    }
    url = f"{API}?{urllib.parse.urlencode(params)}"
    request = urllib.request.Request(url, headers={"Authorization": f"Token {key}"})
    with urllib.request.urlopen(request, timeout=45) as response:
        return json.load(response).get("results", [])


def fetch(key: str, groups, dry_run: bool):
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    taken = []

    for group, entries in manifest.items():
        if group.startswith("_"):
            continue
        if groups and group not in groups:
            continue

        for entry in entries:
            slot, want = entry["slot"], int(entry.get("take", 1))
            print(f"\n[{group}/{slot}] {entry['query']!r}")

            try:
                results = search(key, entry["query"], entry.get("filter", ""), want)
            except Exception as error:                      # noqa: BLE001 - report and carry on
                print(f"  ! search failed: {error}")
                continue

            kept = 0
            for result in results:
                if kept >= want:
                    break

                licence = ALLOWED.get(result.get("license", ""))
                if licence is None:
                    # The API filter should have caught this. If it did not, WE do.
                    print(f"  - skip {result['id']}: licence {result.get('license')!r} not allowed")
                    continue

                preview = (result.get("previews") or {}).get("preview-hq-ogg")
                if not preview:
                    continue

                destination = OUT / group / slot
                path = destination / f"{result['id']}.ogg"
                record = {
                    "id": result["id"],
                    "name": result["name"],
                    "author": result.get("username", "unknown"),
                    "licence": licence,
                    "url": result.get("url", ""),
                    "group": group,
                    "slot": slot,
                    "seconds": round(float(result.get("duration", 0)), 1),
                }

                if dry_run:
                    print(f"  . would take {record['id']} {record['name'][:46]!r} [{licence}]")
                else:
                    destination.mkdir(parents=True, exist_ok=True)
                    try:
                        with urllib.request.urlopen(
                            urllib.request.Request(preview, headers={"Authorization": f"Token {key}"}),
                            timeout=90,
                        ) as source, open(path, "wb") as sink:
                            sink.write(source.read())
                    except Exception as error:              # noqa: BLE001
                        print(f"  ! download failed for {result['id']}: {error}")
                        continue
                    print(f"  + {path.relative_to(ROOT)}  [{licence}]  {record['seconds']}s")

                taken.append(record)
                kept += 1

            if kept < want:
                print(f"  ! only {kept} of {want} usable results")

    return taken


def write_credits(taken):
    """Attribution is REQUIRED for CC-BY and written for CC0 too.

    A credits file that only lists what legally needs listing is a credits file nobody trusts, and
    the day somebody swaps a CC0 sound for a CC-BY one is the day the distinction stops being
    tracked. Every sound that lands is recorded.
    """
    by_group = {}
    for record in taken:
        by_group.setdefault(record["group"], []).append(record)

    lines = [
        "# Audio credits",
        "",
        "Every sound shipped with PROJECT EXODUS, its author and its licence.",
        "",
        "**Generated by `tools/audio/fetch-sounds.py` — do not hand-edit.** Re-run the fetcher",
        "instead, so the file and the files on disk cannot drift apart.",
        "",
        "Only **CC0** and **CC-BY** are permitted. CC-BY-NC forbids commercial use and must never",
        "enter a build; the fetcher refuses it at the query AND again on the result.",
        "",
        "Sounds not listed here are **synthesised at runtime** (`game/Assets/Scripts/Audio/`) and",
        "belong to this project.",
        "",
    ]
    for group in sorted(by_group):
        lines.append(f"## {group}")
        lines.append("")
        lines.append("| Sound | Author | Licence | Source |")
        lines.append("|---|---|---|---|")
        for record in sorted(by_group[group], key=lambda r: (r["slot"], r["id"])):
            name = record["name"].replace("|", "/")
            lines.append(
                f"| `{record['slot']}` — {name} | {record['author']} | {record['licence']} | "
                f"[{record['id']}]({record['url']}) |"
            )
        lines.append("")

    CREDITS.parent.mkdir(parents=True, exist_ok=True)
    CREDITS.write_text("\n".join(lines), encoding="utf-8", newline="\n")
    print(f"\nWrote {CREDITS.relative_to(ROOT)} ({len(taken)} sounds)")


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    dry_run = "--dry-run" in sys.argv

    taken = fetch(token(), set(args), dry_run)
    if not taken:
        sys.exit("\nNothing taken.")
    if not dry_run:
        write_credits(taken)
    print(f"\n{len(taken)} sounds. Copy what you use into game/Assets/Resources/Audio/.")


if __name__ == "__main__":
    main()
