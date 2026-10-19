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

1. Install Git, Python 3.11+ and ffmpeg on the Lenovo. Clone the repo, check out the work branch, run the install block below, confirm `17 passed`.
2. Accounts: switch Instagram `anomculture` to a **Business** account; confirm the YouTube handle; make sure BeatStars, TikTok, Instagram and YouTube all use the same display name and (for now) no avatar until the persona still is approved.
3. Persona: done 2026-10-19, `docs\publicity
eference\persona-three-v2.jpg` is the character sheet (local file).
4. Keys. ElevenLabs: elevenlabs.io, profile icon top right, API Keys, Generate New API Key, allow Text to Speech and Voices read, copy it once. Hedra: hedra.com, sign up, `hedra.com/develop/api-keys` to create a key; the API wallet is prepaid and separate from Studio, fund it with $5 in the Develop billing page. Put both in `secrets.env` at the repo root (gitignored):
   ```
   HEDRA_API_KEY=keyid:secret
   ELEVENLABS_API_KEY=
   ```
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
py -m pytest -q                 # must print "17 passed"
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

## Step 3b: write the BeatStars package

BeatStars has no upload API, so the pipeline writes every field the Studio form asks for and you paste.

1. The beat must already be in the catalogue (step 3a) and its source file still in `inbox\`.
2. Run, naming the anomaly and the lane:
   ```powershell
   py -m anom package brass-alone-09d58a --title "BRASS ALONE" --lane cold-southern --anomaly "the brass that trips on three"
   ```
   Optional: `--artist "Travis Scott"` makes the title `"BRASS ALONE" | Travis Scott Type Beat` (otherwise the lane stands in: `| Cold Southern Type Beat`); `--no-type-beat` drops the handle; `--force` rebuilds. Lanes: trap, pain-piano, cold-southern, soul-slice, west-coast-bounce, guitar-emo, pop-trap, trunk-808, slow-and-sad, weird.
3. Read the line it prints. Flags: `ANOMALY?` (no anomaly named; the description carries `[name the anomaly]`), `BPM?`, `KEY?` (confirm by ear), `WAV?` (source is an MP3, so no WAV deliverable; the WAV lease needs a lossless render).
4. Outputs in `out\<beat-id>\package\`: `cover.jpg` (3000 x 3000, persona still, red Impact title, AI mark), `<slug>.mp3` (ID3-tagged with the cover; a Suno MP3 is copied, not re-encoded), `<slug>.wav` (only when the source is lossless), `preview.mp3`, `copy-fields.txt`, `metadata.json`. The same data is written into `catalog\catalog.json` under `package`, status moves to `packaged`. Commit the catalogue.
5. In BeatStars Studio: upload the audio and cover, then paste from `copy-fields.txt` top to bottom (Title, Genre, Moods, BPM, Key, Tags, Description), and set the licenses listed there. Why this stays a human step, and the one-minute routine: `beatstars-terms.md`.

Prices are the defaults from proposals.md D.7 (MP3 $29.99, WAV $49.99, Trackout $99.99, Unlimited $199.99, Exclusive by offer). Override one with an environment variable, e.g. `ANOM_PRICE_MP3=24.99`. Trackout is written into the record but marked "not listed: no stems yet" until a stem split exists.

Before the first real upload, read BeatStars' current terms on AI-generated audio and tell me what they say; the package records the generator and plan in `rights` either way.

Fonts: Arial on Windows, DejaVu on Linux, Impact for the cover title when present. Override with `ANOM_FONT`, `ANOM_FONT_BOLD`, `ANOM_FONT_DISPLAY`.

## Credentials

None yet. When step 3c needs Hedra and ElevenLabs, keys go in `secrets.env` at the repo root (gitignored), never in chat.

## Scheduled jobs

None yet.
