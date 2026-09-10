#!/usr/bin/env python3
"""
Concept-art generator for CIPHER: DEAD TURF.

Talks to Replicate's HTTP API and writes PNGs into art/concept/. Nothing about this
touches the game build; it exists so the owner can see a target before the art phase
starts guessing at one.

    python tools/concept/generate.py              # every shot in shots.json
    python tools/concept/generate.py street goon  # only those shot ids
    python tools/concept/generate.py --n 3        # 3 variations each

The API token is read from, in order:
    1. the REPLICATE_API_TOKEN environment variable
    2. .secrets/replicate.txt   (gitignored)

MODEL LICENCE, on purpose: the default is FLUX.1 [schnell], which is Apache-2.0, so
anything it makes can ship. FLUX.1 [dev] looks better but its licence is
non-commercial, so it is available here only behind --model dev and its output must
stay internal reference. Do not let a dev-model image reach a build or a store page.
"""

import argparse
import json
import os
import pathlib
import sys
import time
import urllib.error
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
SHOTS = pathlib.Path(__file__).with_name("shots.json")
OUT = ROOT / "art" / "concept"
API = "https://api.replicate.com/v1"

MODELS = {
    "schnell": ("black-forest-labs/flux-schnell", True),   # Apache-2.0: shippable
    "dev": ("black-forest-labs/flux-dev", False),          # non-commercial: reference only
}


def token() -> str:
    tok = os.environ.get("REPLICATE_API_TOKEN", "").strip()
    if tok:
        return tok
    path = ROOT / ".secrets" / "replicate.txt"
    if path.exists():
        tok = path.read_text(encoding="utf-8").strip()
        if tok:
            return tok
    sys.exit(
        "No API token.\n"
        "  Put it in .secrets/replicate.txt (gitignored) or set REPLICATE_API_TOKEN."
    )


def post(url: str, payload: dict, tok: str, wait: bool = True) -> dict:
    body = json.dumps(payload).encode()
    req = urllib.request.Request(url, data=body, method="POST")
    req.add_header("Authorization", f"Bearer {tok}")
    req.add_header("Content-Type", "application/json")
    if wait:
        # Replicate holds the connection open until the prediction settles, up to 60s.
        req.add_header("Prefer", "wait=60")
    with urllib.request.urlopen(req, timeout=180) as r:
        return json.loads(r.read())


def get(url: str, tok: str) -> dict:
    req = urllib.request.Request(url)
    req.add_header("Authorization", f"Bearer {tok}")
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read())


def settle(pred: dict, tok: str) -> dict:
    """Polls until the prediction finishes, if Prefer: wait did not already finish it."""
    deadline = time.time() + 300
    while pred.get("status") in ("starting", "processing"):
        if time.time() > deadline:
            raise TimeoutError("prediction did not finish within 5 minutes")
        time.sleep(2)
        pred = get(pred["urls"]["get"], tok)
    return pred


def save(url: str, dest: pathlib.Path) -> int:
    with urllib.request.urlopen(url, timeout=180) as r:
        data = r.read()
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_bytes(data)
    return len(data)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("ids", nargs="*", help="shot ids to render (default: all)")
    ap.add_argument("--n", type=int, default=1, help="variations per shot")
    ap.add_argument("--model", choices=sorted(MODELS), default="schnell")
    ap.add_argument("--ratio", default="16:9")
    args = ap.parse_args()

    model, shippable = MODELS[args.model]
    if not shippable:
        print(f"!! {model} is NON-COMMERCIAL. Internal reference only; never ship it.\n")

    shots = json.loads(SHOTS.read_text(encoding="utf-8"))
    style = shots["style"]
    wanted = args.ids or [s["id"] for s in shots["shots"]]

    tok = token()
    made = 0
    for shot in shots["shots"]:
        if shot["id"] not in wanted:
            continue
        prompt = f"{shot['prompt']}. {style}"
        for i in range(1, args.n + 1):
            dest = OUT / f"{shot['id']}-{args.model}-{i:02d}.png"
            print(f"-> {shot['id']} ({i}/{args.n}) ... ", end="", flush=True)
            try:
                pred = post(
                    f"{API}/models/{model}/predictions",
                    {"input": {
                        "prompt": prompt,
                        "aspect_ratio": args.ratio,
                        "output_format": "png",
                        "num_outputs": 1,
                        "disable_safety_checker": False,
                    }},
                    tok,
                )
                pred = settle(pred, tok)
                if pred.get("status") != "succeeded":
                    print(f"FAILED: {pred.get('error') or pred.get('status')}")
                    continue
                out = pred.get("output")
                url = out[0] if isinstance(out, list) else out
                kb = save(url, dest) // 1024
                print(f"{dest.relative_to(ROOT)}  ({kb} KB)")
                made += 1
            except urllib.error.HTTPError as e:
                detail = e.read().decode(errors="replace")[:300]
                print(f"HTTP {e.code}: {detail}")
                if e.code in (401, 402):
                    return 1        # bad token or no credit: stop, do not burn the rest
            except Exception as e:  # noqa: BLE001 - one bad shot must not kill the batch
                print(f"ERROR: {e}")

    print(f"\n{made} image(s) in {OUT.relative_to(ROOT)}")
    return 0 if made else 1


if __name__ == "__main__":
    raise SystemExit(main())
