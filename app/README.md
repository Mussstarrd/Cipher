# ReleaseLine

A local, step-by-step helper that walks each music track from "downloaded from Suno" to
"posted everywhere". It keeps an eight-stage checklist per track, drafts Kling video
prompts and titles/tags/descriptions from the details you type, cuts the audio clip and
assembles the final video when ffmpeg is installed, and logs where each track was posted.

It organizes, drafts text from your inputs, and runs local commands. It does not and
cannot predict or promise how a release will perform.

## Requirements

- **Python 3.10 or newer.** Nothing to `pip install`; standard library only.
  Check with `python3 --version` (Windows: `py -3 --version`).
- **ffmpeg (optional).** Needed only for the one-click *Cut clip* and *Assemble* buttons.
  Without it, ReleaseLine shows you the exact commands to run instead and everything else
  works.
  - Windows: `winget install Gyan.FFmpeg` (or `choco install ffmpeg`), then open a new
    terminal. If ReleaseLine still cannot find it, paste the path to `ffmpeg.exe` in
    Settings → ffmpeg path and press Test.
  - macOS: `brew install ffmpeg`
  - Debian/Ubuntu: `sudo apt install ffmpeg`
- A browser. Works offline; nothing is sent anywhere.

## Run (one command)

```
cd app
python3 -m releaseline serve
```

or `./run.sh` (macOS/Linux) or `run.bat` (Windows). A browser tab opens at
`http://127.0.0.1:8477/`. Press Ctrl+C in the terminal to stop.

Options: `--port 8500`, `--no-browser`. Environment: `RELEASELINE_PORT` (default 8477),
`RELEASELINE_DATA` (default `./data`).

## Test (one command)

```
cd app
python3 -m unittest discover -s tests
```

Tests that actually execute ffmpeg are skipped with a message when ffmpeg is not
installed; command-building tests always run.

## 60-second walkthrough

1. Open the Dashboard. It is empty. Press **New track**.
2. Type a title (required) and whatever you know: genre, mood, energy, BPM, hook line.
   Press **Create track**. A folder `data/library/<slug>/` with `source/ master/ clip/
   video/ final/` is created.
3. You land on the track page. The stage rail shows eight stages; stage 1 is open with a
   checklist. Work the checklist, paste the path to the downloaded file, **Save path**,
   **Mark done**. Stage 2 opens.
4. Stage 3: type where the hook starts (`0:45`), press **Cut clip**. With ffmpeg, the WAV
   and MP3 land in `clip/`; without it, copy the shown command. A start time past the end
   of the file is refused with a sentence; a length that runs past the end gives a shorter
   clip and says so.
5. Stage 4: pick a style preset, press **Generate**. Read the variants, **Shuffle** if they
   feel flat, **Copy** the one you like, press **Use this** so it is saved to the track.
   The variants stay on screen with the chosen one marked.
6. Stage 5: paste prompts into Kling, download the shots into `video/`.
7. Stage 6: **Assemble** builds `final/<slug>-final.mp4` (or shows the commands).
8. Stage 7: **Generate** titles, descriptions, hashtags and tags. Edit in place, **Copy**.
9. Stage 8: upload on each platform, paste the posted URL per row. The track moves to
   **Released** on the Dashboard.

## The eight stages

1. **Download from Suno** — checklist; record the file path (verified to exist).
2. **Master in FL Studio** — checklist incl. the commonly used ~-14 LUFS streaming target
   (a common target, not a rule); record the mastered WAV path and notes.
3. **Cut the 30-second clip** — start time, length, fades; ffmpeg cut to WAV + MP3 320.
4. **Video prompt** — preset, setting, aspect, Kling clip length, shot count; 3–5 prompt
   variants with per-shot lists; Shuffle reseeds; Use this saves.
5. **Generate video in Kling** — checklist; the app lists what is in `video/`; record
   shot paths.
6. **Edit together** — ffmpeg normalizes each shot to the aspect (1080×1920 / 1920×1080 /
   1080×1080, 30 fps, H.264), concatenates, loops or trims to the clip, muxes AAC 192k.
7. **Tags and description** — 3 titles (plain, "type beat" when the genre suggests it,
   hook-led), short + long descriptions, hashtags per platform, YouTube tags under 500
   characters, distributor one-liner with explicit yes/no.
8. **Upload** — one row per enabled platform with upload link, format reminders and a
   posted-URL field; Released when every row has a URL or you press Release anyway.

## How prompts and metadata are generated

Templates. The generators pick phrases from built-in vocabulary banks keyed by your genre
family, mood words, energy and style preset, and fill them with your title, hook, BPM and
key. A seed drives the choice, so the same seed gives the same output and Shuffle gives a
new one. No AI service, no API key, no internet. Edit anything that reads flat; edits are
saved.

## Where data lives

Everything is in `app/data/` (or wherever `RELEASELINE_DATA` points):

- `releaseline.db` — SQLite: tracks, stages, drafts, posts, feedback.
- `settings.json` — artist, library root, ffmpeg path, defaults, in plain JSON.
- `feedback.md` — your in-app notes, append-only Markdown.
- `library/<slug>/` — the track folders with your audio, shots and final video (unless
  you changed the library root in Settings).

**Backup:** copy the `data/` folder. **Restore:** put it back. That is all. Recorded file
paths are absolute, so if you move `data/` to another machine or drive the Assets list will
flag the old paths as "file not found" until you re-save them; the files themselves are in
`library/` and are listed there regardless.

## Command line

```
python3 -m releaseline new "Title" [--genre …] [--mood …] [--energy low|mid|high]
python3 -m releaseline list
python3 -m releaseline prompts <id-or-slug> [--n 5] [--seed S] [--preset anime]
python3 -m releaseline cut <id-or-slug> --start 0:45 [--len 30] [--fade 0.5] [--src file]
python3 -m releaseline assemble <id-or-slug>
python3 -m releaseline meta <id-or-slug> [--explicit]
python3 -m releaseline feedback "text"
```

Exit codes: 0 ok, 1 something you can fix (message says what, including wrong or missing
arguments), 2 unexpected.

## Troubleshooting

- **"Port 8477 is already in use."** Another program (or another ReleaseLine) has the
  port. Run `python3 -m releaseline serve --port 8500`.
- **"python3: command not found" / "ReleaseLine needs Python 3.10 or newer. You have
  3.8."** Install Python 3.10+ from python.org. On Windows use `py -3 -m releaseline
  serve` (that is what `run.bat` does). Check the version with `python3 --version`.
- **Pill says "ffmpeg not found".** Install it (see Requirements), open a new terminal,
  restart ReleaseLine. Or set the full path to `ffmpeg.exe` in Settings and press Test.
  Until then the Cut and Assemble buttons show commands you can run by hand.
- **"File not found: …"** The path you pasted does not point at a file. On Windows, paste
  the full path from Explorer (Shift + right-click → Copy as path, then remove the
  quotes). Paths with spaces are fine. Network drives must be mounted.
- **"0:45 is past the end of the file (0:30 long). Pick an earlier start."** The start
  time you typed is beyond the end of the audio. ReleaseLine reads the file's length with
  ffprobe before cutting and writes nothing in this case. If the start is inside the file
  but the length runs past the end, the clip is cut shorter and the message says so
  ("The clip will be 0:12 long because the file ends there.").
- **"ffmpeg produced an empty clip …"** ffmpeg exited cleanly but the output had no
  audio in it (a bare header). Nothing was saved and the stage stays to-do. Check that the
  source is a playable audio file.
- **"ffmpeg failed while trying to …"** The last meaningful lines ffmpeg printed are in
  the message (progress counters and memory addresses are stripped). Usual causes: a shot
  that is not a video file, a clip that was moved, or a read-only folder.
- **Dashboard says "The database was recreated; N track folders in … are not linked to
  any track."** `data/releaseline.db` was missing (deleted or lost) and a fresh, empty
  one was created, but `library/` still holds the old track folders. Nothing on disk was
  removed. Restore `data/` from a backup to get the tracks back, or create tracks with the
  same titles so the folders are reused.
- **"The database at … could not be opened."** The `data/` folder is missing, read-only,
  or `releaseline.db` is corrupt. Fix permissions or restore `data/` from your backup.
- **"The settings file … could not be read."** `data/settings.json` is not valid JSON.
  Fix it or delete it to return to defaults.
- **Browser did not open.** Go to `http://127.0.0.1:8477/` yourself.
- **Windows path tips.** Use the full path including the drive letter
  (`C:\Users\you\Music\track.wav`). Forward slashes also work. OneDrive folders are fine
  as long as the file is downloaded, not cloud-only.

## Uninstall

Stop the server (Ctrl+C) and delete the folder. If you want to keep your tracks, copy
`data/` out first. Nothing is installed anywhere else: no services, no registry entries,
no packages.
