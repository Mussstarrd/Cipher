# ReleaseLine — spec v1

**One line.** ReleaseLine is a local, step-by-step helper that walks each track from Suno
download to posted: a stage checklist per track, Kling video-prompt drafts generated from
your track details, one-click clip cutting and audio/video assembly when ffmpeg is
installed, title/tag/description drafts, and a release log. It organizes, drafts, and runs
local commands. It does not promise plays, streams, or income.

## 1. Users
- **The owner.** Hobby producer. Makes tracks in Suno, masters in FL Studio, wants short
  AI video clips (Kling) for each beat, posts to YouTube/TikTok/Instagram and a
  distributor. Windows laptop likely, Android phone, time-poor, works in bursts. Loses
  momentum between "track is done" and "track is posted" because the chain has eight
  steps across five tools and nothing tracks where each track is.
- **Others like him.** Solo, AI-assisted music makers releasing beats and songs on their
  own. Same chain, same drop-off.

## 2. Core loop
1. **New track.** Title, artist (default from Settings), genre/style tags, mood words,
   energy (low/mid/high), BPM and key (optional), hook line or lyric excerpt, Suno link
   (optional), notes. Creates `library/<slug>/` with `source/ master/ clip/ video/ final/`.
2. **Track page** shows an eight-stage rail. The current stage is expanded with concise
   instructions, a checklist, the inputs that stage needs, generated drafts with Copy
   buttons, and **Mark done** / **Skip**. Previous stages collapse but stay editable.
3. **Stages**
   1. *Download from Suno* — checklist (download WAV if offered, else highest MP3; save
      into `source/`); record the file path (text field, server verifies it exists;
      "file not found" is a sentence, not a crash).
   2. *Master in FL Studio* — checklist (import, trim silence, EQ, compressor, limiter;
      commonly used streaming loudness target around -14 LUFS integrated, stated as a
      common target, not a rule; export WAV 24-bit/44.1k and MP3 320); record the
      mastered file path; notes.
   3. *Cut the 30-second clip* — inputs: start time (`m:ss`), length (default 30 s),
      fade in/out (default 0.5 s). If ffmpeg is available: **Cut clip** runs it and saves
      `clip/<slug>-clip.wav` and `.mp3`. If not: show the exact ffmpeg command to copy,
      plus a sentence on installing ffmpeg. Tip text: start on the hook.
   4. *Video prompt* — generator. Inputs: style preset (cinematic, anime, VHS/lo-fi, 3D
      render, street documentary, abstract/visualizer, surreal), setting (free text or
      pick), aspect ratio (9:16 default, 16:9, 1:1), clip length (5 s or 10 s), number of
      shots to cover the audio clip (default = ceil(clip_len / shot_len)). Output: 3–5
      prompt variants, each with scene/subject, camera motion, lighting, style, mood
      words, and a negative prompt; plus a **shot list** (one prompt per shot, visually
      continuous, varying camera and moment). Deterministic template engine over vocab
      banks keyed by genre, mood, energy, and preset; **Shuffle** reseeds. Copy per
      prompt; **Use this** saves chosen prompts to the track.
   5. *Generate video in Kling* — checklist (paste prompt, set aspect and duration,
      generate, download, save into `video/` named `shot-01.mp4` …); record one path per
      shot; the app lists what it finds in `video/`.
   6. *Edit together* — if ffmpeg: **Assemble** normalizes each shot to the chosen
      aspect (1080×1920 for 9:16, 1920×1080 for 16:9, 1080×1080 for 1:1, 30 fps,
      H.264), concatenates in order, trims or loops the visual to the audio clip length,
      muxes the audio clip (AAC 192k), writes `final/<slug>-final.mp4`. If not ffmpeg:
      show the commands and a CapCut/DaVinci manual checklist. Record final path.
   7. *Tags and description* — generator from track fields: 3 title variants (plain,
      "type beat" convention if genre suggests it, hook-led), short and long
      descriptions, hashtag sets per platform (YouTube, TikTok, Instagram), a YouTube
      tag list kept under 500 characters, and a one-line distributor note (genre, mood,
      explicit yes/no field). Copy buttons; edits persist.
   8. *Upload* — one row per platform enabled in Settings (YouTube, TikTok, Instagram
      Reels, SoundCloud, distributor) with a link to its upload page, format reminders,
      and a **posted URL** field. Track becomes **Released** when every enabled platform
      has a URL or the owner presses **Release anyway**.
4. **Dashboard.** Tracks grouped by stage with the next action for each, a Released list
   with post links, counts, and the Feedback button.

## 3. Data model (SQLite, `data/releaseline.db`; plain files beside it)
- `tracks` — id, slug UNIQUE, title, artist, genre, mood, energy, bpm, key, hook,
  suno_url, notes, status (active|released|archived), created_at, updated_at
- `stages` — id, track_id, key (download|master|clip|prompt|video|assemble|meta|upload),
  state (todo|done|skipped), data JSON (stage-specific fields and paths), done_at
- `assets` — id, track_id, kind (source|master|clip|video|final), path, created_at
- `drafts` — id, track_id, kind (prompt|shotlist|title|description|tags|hashtags),
  content, chosen (0/1), seed, created_at
- `platforms` — id, name UNIQUE, enabled, upload_url, notes
- `posts` — id, track_id, platform_id, url, posted_at
- `feedback` — id, created_at, screen, text; also appended to `data/feedback.md`
- Library root default `data/library/`, overridable in Settings. Backup = copy `data/`.

## 4. UI sketch (plain HTML/CSS/JS, served locally, no CDN)
- **Dashboard** — columns or grouped list by stage; each card: title, genre, next action,
  "last touched". Empty state: "No tracks yet. Add the one you are working on." Error
  state (database unreadable): a sentence and the path to check.
- **New track** — one form; only Title required; inline validation sentences.
- **Track** — header (title, artist, genre, status), stage rail with ticks, expanded
  current stage, collapsed others, Notes, Assets list (what exists on disk, missing
  files flagged), Drafts (chosen first). Every generated field has Copy. ffmpeg status
  pill ("ffmpeg found" / "ffmpeg not found — commands shown instead").
- **Settings** — artist name, library root, ffmpeg path (auto-detected, overridable,
  "Test" button), platforms on/off with upload URLs, defaults (aspect, shot length,
  clip length, fades).
- **Feedback** — button in the footer of every screen opens a one-box note, saved with a
  timestamp and screen name.
- Footer on every screen: "ReleaseLine organizes your release steps and drafts text from
  your inputs. It cannot predict or promise how a release will perform."

## 5. CLI (`python3 -m releaseline …`)
`serve [--port N]`, `new "Title" [--genre …] [--mood …]`, `list`, `prompts <track> [--n 5]
[--seed S] [--preset P]`, `cut <track> --start m:ss [--len 30]`, `assemble <track>`,
`meta <track>`, `feedback "text"`. Human-readable errors. Exit 0 ok, 1 user error,
2 unexpected.

## 6. Stack
Python 3.10+ **standard library only** (http.server, sqlite3, json, subprocess, shutil,
pathlib, unittest). Plain HTML/CSS/JS, no build, no CDN, fully offline. **ffmpeg is
optional**: auto-detected on PATH or set in Settings; without it the app shows commands
instead of running them. Run: `python3 -m releaseline serve` (also `run.sh` /
`run.bat`). Test: `python3 -m unittest discover -s tests`. Env: `RELEASELINE_DATA`
(default `./data`), `RELEASELINE_PORT` (default 8477). Rationale: zero installs means
the one command still works on a stranger's Windows laptop in a year.

## 7. Out of scope for v1
Audio analysis, waveform display, automatic hook detection, loudness measurement;
calling Suno, Kling, YouTube, TikTok, or distributor APIs; OAuth uploads; LLM-generated
prompts (optional Claude API key is the first backlog item); burned-in captions or title
cards; lyric or hook *writing*; multi-user, auth, cloud sync, mobile app, Docker.

## 8. Shelf-ready checklist for this app
1. Fresh clone, `python3 -m releaseline serve`, browser opens the Dashboard. Nothing else.
2. Every screen readable at 1280×720 and on a phone-width window; no dead controls.
3. Missing file paths, bad timestamps, ffmpeg absent, ffmpeg failing, corrupt DB,
   read-only data dir: each a sentence in the UI, never a traceback; DB writes are
   transactional.
4. Tests cover: track creation and slugging, stage advance/skip, prompt generation
   determinism and variety, shot-list sizing, ffmpeg command building for cut and
   assemble (always), ffmpeg execution (skipped with a message when ffmpeg is absent),
   metadata generation and the 500-char tag cap, server endpoints and error JSON, CLI
   exit codes, feedback persistence.
5. README: requirements, run, test, 60-second walkthrough, installing ffmpeg on
   Windows/macOS/Linux, where data lives, backup, troubleshooting, uninstall.
6. Copy passes the honesty rule: no "viral", "guaranteed", "grow your audience".

## 9. Open risks
- **ffmpeg missing on Windows** → commands shown, install guide in README, status pill.
- **Windows paths** → pathlib everywhere; tests include backslash paths and spaces.
- **Template prompts feel generic** → large vocab banks per genre/mood/preset, shot
  continuity rules, Shuffle, free-text setting; LLM option in backlog.
- **Kling UI changes** → instructions stay generic (aspect, duration, prompt, download).
- **Honesty drift in metadata drafts** → generator never emits performance claims.
