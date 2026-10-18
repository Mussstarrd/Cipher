# Runbook

Grows one section per pipeline step. Commands are for the Lenovo (Windows, PowerShell); on Linux or macOS swap `py` for `python3`.

## Install (once)

```powershell
cd <repo>
py -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
winget install Gyan.FFmpeg      # or any ffmpeg build on PATH; or set ANOM_FFMPEG to the exe
py -m pytest -q                 # must print "6 passed"
```

## Step 3a: add a beat (catalogue + analysis + preview)

1. Download the render from Suno (Pro: 20 downloads a month) into `inbox\`. Keep the file name Suno gives it or name it after the lane.
2. Run:
   ```powershell
   py -m anom ingest --render-date 2026-10-18
   ```
   `--render-date` is the day you rolled it in Suno; it goes into the rights record. Omit it and the field stays null until you fill it.
3. Read the line it prints: BPM (and the half/double-time alternative), key with Camelot code, LUFS, hook start. `BPM?` or `KEY?` means low confidence; the daily queue will ask you to confirm by ear.
4. Outputs: `catalog\catalog.json` (the single source of truth, committed), `out\<beat-id>\preview.mp3` (30 s at the hook, not committed).

Re-run any time; files already in the catalogue are skipped by content hash. `--force` re-analyzes.

Other commands: `py -m anom analyze file.wav` prints the analysis without writing; `py -m anom list` shows the catalogue.

### Environment variables (all optional)

| Variable | Default | Purpose |
|---|---|---|
| `ANOM_INBOX` | `inbox/` | where renders land |
| `ANOM_OUT` | `out/` | previews, art, videos |
| `ANOM_CATALOG` | `catalog/catalog.json` | the catalogue |
| `ANOM_FFMPEG` | `ffmpeg` | path to ffmpeg |
| `ANOM_SUNO_PLAN` | `Pro` | plan tier written into each rights record |

## Credentials

None yet. When step 3c needs Hedra and ElevenLabs, keys go in `secrets.env` at the repo root (gitignored), never in chat.

## Scheduled jobs

None yet.
