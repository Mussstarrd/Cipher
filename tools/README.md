# tools/

## build_sample_bank.py

Turns music you own into a scratch-practice bank. Each track is split into
stems with [Demucs](https://github.com/facebookresearch/demucs) so you get:

- `drums.wav`  — a beat to scratch over
- `vocals.wav` — raw material for scratch sentences and stabs
- `bass.wav`, `other.wav`

### One-time setup

```powershell
python tools/build_sample_bank.py --check     # what's missing?
python tools/build_sample_bank.py --setup     # installs torch (CUDA) + demucs
```

`--setup` installs the CUDA build of PyTorch, falling back to CPU if that
fails. **ffmpeg must be on your PATH** — it is the most common failure:

```powershell
winget install Gyan.FFmpeg    # then reopen the terminal
```

### Build the bank

```powershell
python tools/build_sample_bank.py -i "C:\Music" --dry-run    # preview
python tools/build_sample_bank.py -i "C:\Music" -o bank      # go
```

Useful flags: `-r` recurse into subfolders · `--two-stems drums` (much faster,
just drums vs everything else) · `--mp3` smaller output · `-n htdemucs`
(faster model) or `-n htdemucs_6s` (adds guitar/piano).

Re-running skips tracks already in the bank, so it is safe to interrupt and
resume. A `manifest.json` in the bank lists everything processed.

Use music you own or that is licensed for the purpose.
