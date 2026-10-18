# Runbook

## How a beat flows (the target, once steps 3b to 3f are built)

```
Suno (ONE OF ONE roll) -> inbox\ -> py -m anom run -> queue page (10 min of taps) -> BeatStars + TikTok + Reels + Shorts -> weekly report
```

1. Roll in ONE OF ONE, render in Suno, keep the one you like, download it into `inbox\`.
2. `py -m anom run`. One command: analyze, cut the hook, write the BeatStars package, render the videos, schedule the posts.
3. Open the queue page (`py -m anom queue`, a local web page). Clear it: confirm any `BPM?`/`KEY?` flag by ear, pick a cover from three, approve the persona clip, press "copy fields" and drag the package folder into BeatStars, tap any post the platform would not let us send.
4. Monday: one short report. Nothing else interrupts you.

## Your setup checklist (do once, in this order)

1. Install Git, Python 3.11+ and ffmpeg on the Lenovo. Clone the repo, check out the work branch, run the install block below, confirm `6 passed`.
2. Accounts: switch Instagram `anomculture` to a **Business** account; confirm the YouTube handle; make sure BeatStars, TikTok, Instagram and YouTube all use the same display name and (for now) no avatar until the persona still is approved.
3. Persona: run Recipe B from `persona-two-ways.md` four to six times in any image tool, pick one, save it as `docs\publicity\reference\persona-three-v1.png`, commit and push. That image is the character sheet for every video.
4. Keys: create `secrets.env` at the repo root (gitignored) with `HEDRA_API_KEY=` and `ELEVENLABS_API_KEY=`. Fund the Hedra wallet ($5) only after you have said yes here.
5. Send one real render into this chat (drag the file in) so the video step can be tuned on actual audio.
6. First beat: drop a render in `inbox\`, run `py -m anom ingest --render-date YYYY-MM-DD`, commit `catalog\catalog.json`, push. That is how a track reaches me.


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
