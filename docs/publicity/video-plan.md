# How the videos get made

Target: every beat yields three to five vertical videos with zero hands from Jeff beyond a tap in the queue. This is the design for step 3c; nothing here is built yet except the audio side (3a).

## The two kinds of persona footage

**1. Three at the desk (the "engineering" footage).** A one-time library, not per beat.
- From the approved character still (Recipe B), generate 12 to 20 short image-to-video clips, 5 to 10 seconds each, with Kling or Veo 3.1 Lite: Three head-bobbing at an MPC, hands on pads, leaning back with eyes closed, pointing at the screen, dragging a fader, turning to the camera, standing up on the drop. Same room, same low cold light, harmonica on the neck rack in every shot.
- Cost once: about $15 to $20 at $0.10 per second. After that, $0 per beat forever.
- Beat sync is done in ffmpeg, not in the generator: each clip's natural nod rate is measured once, then the clip is time-stretched so the nod lands on the beat of whichever track it is laid under. Hard cuts on bar lines only, never a crossfade. That is the brand rule applied to picture.

**2. Three talking (the hook line).** Per beat, 3 to 6 seconds.
- ElevenLabs renders the line in the persona's synthetic voice (Voice Design, not a clone of anyone): "One. Two. ... Three. The anomaly in this one is the harmonica. It trips on three."
- Hedra Character-3 drives the character still with that audio. 720p, about 5 cents a second, so about 25 cents a clip.
- The video opens on this, then hard-cuts into the beat on beat 1.

## Assembly (ffmpeg, on the Lenovo)

Layers, 1080 x 1920, 30 fps:
1. Base: desk footage, beat-synced, cut on bars.
2. Spectrum or waveform strip in the low third, drawn from the actual audio (`showspectrum` / `showwaves`), coloured Anom red on black.
3. Top left, first second onward: `AI CHARACTER · ANOM CULTURE` (the disclosure, small, always).
4. Hook text, first second, big: the anomaly line, for example `the harmonica trips on three`.
5. Lower third: `140 BPM · C minor · beatstars.com/anomculture`.
6. Last 2 seconds: end card with the BeatStars link and the beat title.
7. Audio: the hook window from the catalogue (3a already finds it). TikTok and Reels get 15 to 30 s; Shorts up to 60 s.

## Formats, in build order

| Format | What it is | Footage |
|---|---|---|
| Beat of the day | Talking intro, hard cut, 20 s of the hook over desk footage | library + 1 Hedra clip |
| Producer reacts | Three in a small window reacting (nods, flinch on the anomaly) over the full-frame spectrum | 1 Hedra clip driven by the beat itself |
| Cook-up time-lapse | Desk footage sped 4x to 8x, spectrum, the beat building section by section | library |
| Which one hits harder | Two beats, split screen or back to back, poll sticker text | library |
| Pack drop (weekly) | 5 beats, 5 s each, hard cuts, one end card | library |

## Platform specs the exporter must hit
- TikTok and Reels: 9:16, 1080 x 1920, H.264, AAC, under 90 s for Reels via API, under 287 MB.
- Shorts: 9:16, under 60 s (up to 3 min allowed, 60 s is the target).
- AI label set on upload for all three, plus the burned-in text.

## What has to exist before 3c can be built
1. The approved character still (Recipe B), saved as `docs/publicity/reference/persona-three-v1.png`. Synthetic, so it may be committed.
2. One real render in this chat so the visualizer is tuned on actual audio.
3. Hedra API key and ElevenLabs API key in `secrets.env`.
4. Your yes to the one-time desk-footage library (about $20) and the $5 Hedra wallet.
