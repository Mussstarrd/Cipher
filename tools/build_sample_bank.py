#!/usr/bin/env python3
"""
Build a scratch-practice sample bank from music you already own.

Splits each track into stems with Demucs so you get, per track:
  drums.wav   -> a beat to scratch over
  vocals.wav  -> raw material for scratch sentences / stabs
  bass.wav, other.wav

Usage (Windows PowerShell / macOS / Linux):
  python tools/build_sample_bank.py --check            # is this machine ready?
  python tools/build_sample_bank.py --setup            # install torch(+CUDA), demucs
  python tools/build_sample_bank.py -i "C:\\Music" --dry-run
  python tools/build_sample_bank.py -i "C:\\Music" -o bank

Re-running skips tracks already in the bank, so it is safe to interrupt.
"""
import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

AUDIO_EXT = {".mp3", ".wav", ".flac", ".m4a", ".aiff", ".aif", ".ogg", ".opus", ".wma"}
CUDA_INDEX = "https://download.pytorch.org/whl/cu118"


def sh(cmd, **kw):
    return subprocess.run(cmd, shell=isinstance(cmd, str), **kw)


def check_env():
    """Report what is installed, without importing heavyweight modules twice."""
    info = {
        "python": sys.version.split()[0],
        "ffmpeg": shutil.which("ffmpeg") is not None,
        "demucs": False,
        "torch": None,
        "cuda": False,
        "gpu": None,
    }
    try:
        import demucs  # noqa: F401
        info["demucs"] = True
    except Exception:
        pass
    try:
        import torch
        info["torch"] = torch.__version__
        info["cuda"] = bool(torch.cuda.is_available())
        if info["cuda"]:
            info["gpu"] = torch.cuda.get_device_name(0)
    except Exception:
        pass
    return info


def print_check(info):
    ok = "OK  "
    bad = "MISS"
    print("\nEnvironment")
    print(f"  [{ok}] python {info['python']}")
    print(f"  [{ok if info['ffmpeg'] else bad}] ffmpeg on PATH"
          + ("" if info["ffmpeg"] else "   <- required; install and reopen your terminal"))
    print(f"  [{ok if info['demucs'] else bad}] demucs")
    print(f"  [{ok if info['torch'] else bad}] torch {info['torch'] or ''}")
    if info["torch"]:
        if info["cuda"]:
            print(f"  [{ok}] CUDA GPU: {info['gpu']}  (10-50x faster than CPU)")
        else:
            print(f"  [{bad}] CUDA not available - will run on CPU (slow)."
                  f"\n         Fix: pip install torch torchvision torchaudio --index-url {CUDA_INDEX}")
    ready = info["ffmpeg"] and info["demucs"] and info["torch"]
    print(f"\n  => {'ready to build the bank' if ready else 'run --setup first'}\n")
    return ready


def do_setup():
    print("Installing PyTorch with CUDA support, then Demucs...")
    r = sh([sys.executable, "-m", "pip", "install", "torch", "torchvision", "torchaudio",
            "--index-url", CUDA_INDEX])
    if r.returncode != 0:
        print("\nCUDA build failed; falling back to the default CPU build.")
        sh([sys.executable, "-m", "pip", "install", "torch", "torchvision", "torchaudio"])
    sh([sys.executable, "-m", "pip", "install", "-U", "demucs"])
    if shutil.which("ffmpeg") is None:
        print("\nNOTE: ffmpeg is still missing from PATH. Demucs needs it for mp3/m4a input.")
        print("      Windows: winget install Gyan.FFmpeg   (then reopen the terminal)")
    print_check(check_env())


def find_tracks(input_dir: Path, recursive: bool):
    it = input_dir.rglob("*") if recursive else input_dir.glob("*")
    return sorted(p for p in it if p.is_file() and p.suffix.lower() in AUDIO_EXT)


def stem_dir_for(out_root: Path, track: Path) -> Path:
    return out_root / track.stem


def process(track: Path, out_root: Path, model: str, two_stems, mp3: bool, dry: bool):
    dest = stem_dir_for(out_root, track)
    if dest.exists() and any(dest.iterdir()):
        return "skip"
    if dry:
        return "would-process"

    tmp = out_root / "_work"
    tmp.mkdir(parents=True, exist_ok=True)
    cmd = [sys.executable, "-m", "demucs", "-n", model, "-o", str(tmp)]
    if two_stems:
        cmd += ["--two-stems", two_stems]
    if mp3:
        cmd += ["--mp3"]
    cmd.append(str(track))

    r = sh(cmd)
    if r.returncode != 0:
        shutil.rmtree(tmp, ignore_errors=True)
        print("        (demucs failed - scroll up for its output. Most common cause:"
              " ffmpeg missing from PATH, or an unreadable/DRM file.)")
        return "error"

    produced = tmp / model / track.stem
    if not produced.exists():
        cands = list((tmp / model).glob("*")) if (tmp / model).exists() else []
        produced = cands[0] if cands else None
    if not produced or not produced.exists():
        shutil.rmtree(tmp, ignore_errors=True)
        print("        (demucs reported success but wrote no stems - skipping)")
        return "error"

    dest.mkdir(parents=True, exist_ok=True)
    for f in produced.iterdir():
        shutil.move(str(f), str(dest / f.name))
    shutil.rmtree(tmp, ignore_errors=True)
    return "done"


def main():
    ap = argparse.ArgumentParser(description="Build a scratch-practice sample bank with Demucs.")
    ap.add_argument("-i", "--input", type=Path, help="folder of audio files you own")
    ap.add_argument("-o", "--output", type=Path, default=Path("bank"), help="bank folder (default: ./bank)")
    ap.add_argument("-n", "--model", default="htdemucs_ft",
                    help="htdemucs_ft (best), htdemucs (faster), htdemucs_6s (adds guitar/piano)")
    ap.add_argument("--two-stems", metavar="STEM",
                    help="only split STEM vs the rest, e.g. drums or vocals (much faster)")
    ap.add_argument("--mp3", action="store_true", help="write mp3 stems instead of wav")
    ap.add_argument("-r", "--recursive", action="store_true", help="search subfolders too")
    ap.add_argument("--dry-run", action="store_true", help="list what would happen, change nothing")
    ap.add_argument("--check", action="store_true", help="report environment readiness and exit")
    ap.add_argument("--setup", action="store_true", help="install torch + demucs, then re-check")
    args = ap.parse_args()

    if args.check:
        sys.exit(0 if print_check(check_env()) else 1)
    if args.setup:
        do_setup()
        return
    if not args.input:
        ap.error("--input is required (or use --check / --setup)")
    if not args.input.is_dir():
        ap.error(f"not a folder: {args.input}")

    info = check_env()
    if not args.dry_run and not (info["demucs"] and info["torch"]):
        print_check(info)
        print("Run:  python tools/build_sample_bank.py --setup")
        sys.exit(1)

    tracks = find_tracks(args.input, args.recursive)
    if not tracks:
        print(f"No audio files found in {args.input}")
        sys.exit(1)

    args.output.mkdir(parents=True, exist_ok=True)
    print(f"\n{len(tracks)} track(s) found. Model: {args.model}. "
          f"Device: {'GPU ' + str(info['gpu']) if info['cuda'] else 'CPU (slow)'}")
    if args.dry_run:
        print("DRY RUN - nothing will be written.\n")

    counts = {"done": 0, "skip": 0, "error": 0, "would-process": 0}
    for i, t in enumerate(tracks, 1):
        print(f"[{i}/{len(tracks)}] {t.name}")
        status = process(t, args.output, args.model, args.two_stems, args.mp3, args.dry_run)
        counts[status] = counts.get(status, 0) + 1
        print(f"        -> {status}")

    if not args.dry_run:
        manifest = args.output / "manifest.json"
        entries = []
        for d in sorted(p for p in args.output.iterdir() if p.is_dir() and p.name != "_work"):
            entries.append({"name": d.name, "stems": sorted(f.name for f in d.iterdir() if f.is_file())})
        manifest.write_text(json.dumps({"model": args.model, "tracks": entries}, indent=2))
        print(f"\nManifest: {manifest}")

    print("\nSummary: " + ", ".join(f"{k}={v}" for k, v in counts.items() if v))
    if counts.get("done") or counts.get("would-process"):
        print(f"\nBank layout:  {args.output}/<track>/drums.wav | vocals.wav | bass.wav | other.wav")
        print("  drums.wav  -> load in Serato as a beat to scratch over")
        print("  vocals.wav -> chop for scratch sentences and stabs")


if __name__ == "__main__":
    main()
