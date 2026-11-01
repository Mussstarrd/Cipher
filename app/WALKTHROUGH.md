# ReleaseLine — screen by screen

Written for someone who has not opened it yet. Start it with `python3 -m releaseline
serve` from the `app/` folder; a tab opens at http://127.0.0.1:8477/.

## Every screen

- **Top bar.** "ReleaseLine" (goes to the Dashboard), links to Dashboard / New track /
  Settings, and a pill on the right: green "ffmpeg found" or amber "ffmpeg not found —
  commands shown instead". The pill tells you whether the Cut and Assemble buttons will
  run ffmpeg or hand you commands.
- **Footer.** The sentence "ReleaseLine organizes your release steps and drafts text from
  your inputs. It cannot predict or promise how a release will perform." and a
  **Feedback** button. Feedback opens a small box; type a note, press **Save note**. It is
  appended to `data/feedback.md` with the time and the screen you were on. Nothing
  leaves your machine.
- Every error is a sentence in place: under the field you typed in, or in a red line in
  the card. There are no pop-up stack traces.

## Dashboard (`#/`)

What you see: a count line ("2 in progress · 1 released"), a **New track** button, and
tracks grouped under the stage they are waiting on, in stage order. Each card shows the
title, artist · genre, the next action in one sentence ("Pick the hook start time and cut
the clip."), stages done out of eight, and "last touched 3 h ago". Click a card to open
the track.

Below that, **Released** is a table: track, links to each post (opens in a new tab), last
touched. Empty state on first run: "No tracks yet. Add the one you are working on." with
the button.

If the database cannot be read you get one sentence with the file path to check instead
of the lists. If the database file was missing and had to be recreated while `library/`
still holds track folders, an amber card at the top says "The database was recreated; N
track folders in … are not linked to any track." and that nothing on disk was deleted.

## New track (`#/new`)

One form. Only **Title** is required; if you leave it blank and press Create, the field
says so. Artist defaults from Settings. Genre / style tags drive the prompt vocabulary and
the "type beat" title rule; mood words and energy (low / mid / high) shape camera,
lighting and color. BPM and key are optional and are checked (BPM must look like a
number). Hook line, Suno link and notes are free text. **Create track** makes the library
folders and opens the track page.

## Track page (`#/track/<id>`)

Top card: title, status badge (active / released), a line with artist · genre · mood ·
energy · BPM · key, the hook in quotes, a link to Suno if given. Buttons: **Edit details**
(unfolds the same form as New track, pre-filled) and **Release anyway** (asks for
confirmation).

**Stage rail:** eight small buttons in a row (they wrap on a phone). Each has a circle:
number = to do, green tick = done, grey dash = skipped, blue = the current stage. Click any
to expand it.

Below the rail, the eight stages as cards. The current one is expanded; the others are
collapsed to a single header line you can click. Every expanded stage ends with
**Mark done** and **Skip** (or, once done, a "Done · 2 h ago" badge and **Reopen**).

1. **Download from Suno.** Four checklist items (tick boxes are remembered), the Suno
   link if you gave one, a "Downloaded file path" box and **Save path**. If the file does
   not exist you see "File not found: <path>. Check the path and try again." under the
   box, and nothing is saved. Pressing Save path with an empty box says "Enter a path
   first." (or "Path cleared." if there was one before).
2. **Master in FL Studio.** Checklist (import, trim, EQ, compressor, limiter with the
   ~-14 LUFS note phrased as a common target, export WAV 24/44.1 + MP3 320), mastered
   file path, a mastering-notes box.
3. **Cut the 30-second clip.** Start time (m:ss; tip: start on the hook), length and fade
   (pre-filled from Settings). Button reads **Cut clip** when ffmpeg is found: it runs,
   the stage shows "Clip: <path>" with a Copy button and marks itself done. Without
   ffmpeg the button reads **Show ffmpeg command** and gives you the exact command plus a
   sentence about installing ffmpeg. Bad time → sentence under the Start field. A start
   past the end of the file → "0:45 is past the end of the file (0:30 long). Pick an
   earlier start." and nothing is written. A length that runs past the end still cuts, and
   the toast adds "The clip will be 0:12 long because the file ends there." While ffmpeg
   runs the button reads "Cutting…" and returns to "Cut clip" when it finishes, success or
   not.
4. **Video prompt.** Pickers: style preset (cinematic, anime, VHS / lo-fi, 3D render,
   street documentary, abstract / visualizer, surreal), optional free-text setting,
   aspect ratio, Kling clip length (5 or 10 s), number of shots (defaults to clip length
   ÷ shot length, rounded up), number of variants (3–5). **Generate** fills the stage
   with variants; **Shuffle** reseeds. A grey line shows the seed and which vocabulary was
   used. Each variant: the full prompt with **Copy**, a small table (scene, camera,
   lighting, style, mood, negative prompt with its own Copy), a fold-out **Shot list**
   with one prompt per shot (same character, wardrobe and location; camera and moment
   change) each with Copy, plus "Copy all shots". **Use this** saves that variant and
   its shot list to the track's Drafts; the variants stay on screen, the chosen one gets a
   green "chosen" badge and border, and the Drafts card below updates at once. When you
   come back to the stage later, a grey line says which variant is chosen and that
   **Generate (same seed)** brings the variants back.
5. **Generate video in Kling.** Checklist, then "Found in video/" listing files already
   saved there, then a box for shot paths one per line with **Save paths** (each path is
   checked to exist). If the files are simply in `video/`, you do not need to type paths.
6. **Edit together.** One paragraph says exactly what Assemble does. **Assemble** (with
   ffmpeg) writes `final/<slug>-final.mp4` and marks the stage done; it can take a
   minute. Without ffmpeg: the concat list to save, the commands to run in order, and a
   CapCut / DaVinci checklist. A "Final video path" box lets you record a file you made by
   hand.
7. **Tags and description.** An "Explicit lyrics" tick box and **Generate**. The results
   appear as labelled boxes you can edit: three titles, short and long description, three
   hashtag sets (YouTube, TikTok, Instagram), the YouTube tag list (always under 500
   characters) and the distributor note. Each has **Copy**. Edits save when you click
   out of a box. This stage is the only place these boxes appear; the Drafts card below
   just links back here.
8. **Upload.** A table with one row per platform enabled in Settings: name with an "Open
   upload page" link, format reminders, a Posted URL box with **Save**. On a phone-width
   window each platform becomes its own block with a full-width URL box. When every row
   has a URL the track becomes Released and you get a toast saying so. Saving an empty box
   says "Paste the posted URL first." (or "Link cleared." if one was saved). A line
   reminds you about Release anyway for tracks that do not go everywhere.

Below the stages: **Notes** (a text box with Save), **Assets** (the library folder path,
what is actually in each of source/ master/ clip/ video/ final/, and recorded paths with
"file not found" flagged in red), and **Drafts** (the chosen prompt and shot list,
editable with Copy, plus one line pointing at stage 7 for titles, descriptions and tags).

## Settings (`#/settings`)

Four cards: *You and your files* (artist name, library root with the resolved default
shown, and the data folder path with the one-line backup instruction), *ffmpeg* (path box,
**Test** button that reports the version or a not-found sentence), *Defaults* (aspect,
Kling shot length, clip length, fade) and *Platforms* (on/off and upload page URL for
YouTube, TikTok, Instagram Reels, SoundCloud, Distributor). **Save settings** at the
bottom; validation errors appear as a sentence above it.

## Command line

The same actions exist as `python3 -m releaseline new|list|prompts|cut|assemble|meta|
feedback`; see README. Errors, including wrong or missing arguments, are one line starting
with "Error:" and exit code 1; only unexpected failures exit 2. On a Python older than
3.10 the only output is "ReleaseLine needs Python 3.10 or newer. You have X.Y."

## From your phone

Start the app on the computer with `python3 -m releaseline serve --host 0.0.0.0`. The
terminal prints the address to type into the phone's browser (same Wi-Fi). Every screen
works at phone width; the Upload stage stacks into blocks. Paths you type are paths on
the computer. See README "Using it from your phone" for the Termux route if you want the
whole app on an Android phone.
