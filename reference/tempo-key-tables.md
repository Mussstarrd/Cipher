# Tempo and key tables

Built from `research/04-radio-ready-conventions.md` and `research/03-artist-vocabulary.md` (accessed 2026-10-31). Key data from auto-detectors is unreliable about major vs. relative minor; treat pitch class as roughly right and the minor label as the intended dark reading. Suno treats a BPM number in the Style field as a bias, not a setting; always pair it with a feel word ("140 BPM, half-time feel", "92 BPM, laid-back swung pocket").

## Radio-ready structure numbers

| Item | Target | Evidence |
|---|---|---|
| Runtime | **2:30-3:30**, default 3:00 | Hot 100 #1s averaged 3:29 (2024, 2025); Spotify charting average ~3:00; hip hop shortened ~29 s 2018-2024 |
| Intro | **4 bars default, 8 max** (about 15 s) | #1-hit intros averaged 14 s (2023-24); hip hop intros ~17 s with first chorus near 0:30; Spotify counts a stream at 30 s |
| First hook / drop | by **0:30-0:45**, hook-first arcs by ~0:15 | 30-second skip rule; counter-trend: a third of 2025 #1s delayed the chorus past 1:00 (superstar outliers) |
| Sections | Hook 8 / Verse 16 / Bridge or Break 4-8 / Outro 4-8 bars (straight time); in half-time lanes hooks, pre-hooks, drops and outros double in written bars, verses stay 16 | Template A (72 bars) = 3:08 at 92 BPM |
| Ending | **Cold / hard stop**; fade-outs have nearly vanished from hits | |
| Beat-leasing convention | hook x2 at the end, clear hook/verse markers, verse energy <= 6 so a vocal can sit on top | |

## Bars to seconds (4/4; seconds per bar = 240 / BPM)

| BPM | sec/bar | 2 bars | 4 bars | 8 bars | 12 bars | 16 bars | 24 bars | 32 bars |
|---|---|---|---|---|---|---|---|---|
| 85 | 2.82 | 0:06 | 0:11 | 0:23 | 0:34 | 0:45 | 1:08 | 1:30 |
| 92 | 2.61 | 0:05 | 0:10 | 0:21 | 0:31 | 0:42 | 1:03 | 1:23 |
| 100 | 2.40 | 0:05 | 0:10 | 0:19 | 0:29 | 0:38 | 0:58 | 1:17 |
| 130 | 1.85 | 0:04 | 0:07 | 0:15 | 0:22 | 0:30 | 0:44 | 0:59 |
| 140 | 1.71 | 0:03 | 0:07 | 0:14 | 0:21 | 0:27 | 0:41 | 0:55 |
| 150 | 1.60 | 0:03 | 0:06 | 0:13 | 0:19 | 0:26 | 0:38 | 0:51 |
| 160 | 1.50 | 0:03 | 0:06 | 0:12 | 0:18 | 0:24 | 0:36 | 0:48 |

Half-time lanes (written 130-160 BPM) feel at half the count: a 16-written-bar hook at 140 BPM is 27 s, the same felt length as an 8-bar hook at 70 BPM.

## Loudness and mastering vocabulary

| Context | Integrated loudness | True peak |
|---|---|---|
| Streaming normalization (Spotify, YouTube, Amazon) | -14 LUFS (Apple ~-16) | <= -1 dBTP (Spotify: <= -2 dBTP if louder than -14) |
| Hip hop releases in practice | -8 to -10 LUFS (some -6 to -8) | -1 dBTP |
| suno-forge lane targets for a DAW master after stems | trap-pop / phonk -8 to -7; hybrid -9; neo-soul -11 to -10 | -1 dBTP |

Suno has no loudness control; these numbers are for the post-Suno polish. Mix words a text-to-music model responds to (use 1-2 at the end of the Style field, describe the result not the process): punchy drums, glued cohesive mix, wide stereo image, clean low end, tight controlled sub, bright clear top end, well-defined bass, good instrument separation, clean production, loud modern master, polished, radio-ready, balanced full-range, crisp transients. Technical jargon ("sidechain compression at 4:1") is reported as ignored.

## Per-lane tempo and key

| Lane | Written BPM (default) | Felt pulse | Time feel | Keys (generator picks from the first three by default) | Notes |
|---|---|---|---|---|---|
| `connor-price` | 130-160 (**140**) | 65-80 | half-time | B minor, F minor, F# minor, C# minor, D minor, G minor | Catalog is bimodal: 130-141 half-time trap or 90-105 straight bounce. `--bpm 105` gives the straight bounce (planner switches to straight time under 120). |
| `paak-pocket` | 85-100 (**92**) | same | straight, swung 16ths | F minor, D Dorian, C minor, Eb major, C# minor, Ab major, Bb minor | Pocket 86-96; 98-105 leans funk; 78-86 leans lo-fi soul. Minor 7th/9th and Dorian vamps. |
| `jid-dark` | 130-160 (**146**) | 65-80 | half-time | E minor, C minor, G minor, F minor, C# minor, A minor | Boom-bap variant at 88-95 straight (`--bpm 90`). For a beat switch pair 90 with 135-150 so the felt pulses relate ~2:3 or 1:2. |
| `phonk-experimental` | 130-150 (**140**) | 65-75 | half-time | C minor, G minor, F minor, D minor, G# minor, B minor | Blog ranges 130-150 for Memphis/OG; measured viral drift hits sit 115-125 (not our lane). Natural minor, occasional Phrygian motif. |
| `hybrid` | 136-150 (**142**) | 68-75 | half-time | F minor, C minor, D minor, G minor, Bb minor | Phonk drums on the written count; Rhodes/bass pocket on the felt pulse. |

## Most common keys in hip hop and trap (best available evidence; no genre-wide dataset exists)

| Rank (approx.) | Key | Evidence |
|---|---|---|
| 1 | F minor | Mixed In Key trap top-10; "Fm-Db-Eb" cited as the stock trap loop |
| 2 | F# / Gb minor | Mixed In Key top trap key |
| 3 | C# / Db minor | Frequent across analysed trap-pop and neo-soul catalogs |
| 4 | G minor | Mixed In Key: ~60% of trap sits on F or G |
| 5 | E minor | Unison; several analysed hybrids |
| 6 | B minor | Unison; analysed trap-pop and phonk |
| 7 | A minor | Hooktheory: most popular minor key pop-wide |
| 8 | Bb / A# minor | Mixed In Key sample |
| low | G# minor | Rare pop-wide but common in phonk and trap by ear (unverified) |

## Lyrics layouts (as implemented in `lib/layouts.js`)

Each roll picks a layout first; the layout decides the mode. Nominal bars estimate the runtime; in half-time lanes hooks double and loops run 1.5x. Runtime at representative tempos (first full beat in brackets):

| Layout | Mode | 88 BPM | 96 | 106 | 132 half | 140 half | 152 half |
|---|---|---|---|---|---|---|---|
| `bare_loop` Two-bar loop, nothing changes | loop | 3:11 (11s) | 2:55 (10s) | 2:38 (9s) | 3:05 (7s) | 2:55 (7s) | 2:41 (6s) |
| `one_in_one_out` One sound added, one sound removed | loop | 3:16 (11s) | 3:00 (10s) | 2:43 (9s) | 3:09 (7s) | 2:58 (7s) | 2:44 (6s) |
| `kick_and_lead` Stripped to kick and lead | loop | 3:11 (11s) | 2:55 (10s) | 2:38 (9s) | 3:05 (7s) | 2:55 (7s) | 2:41 (6s) |
| `hook_first` Hook first | straight | 3:05 (0s) | 2:50 (0s) | 2:34 (0s) | 3:02 (0s) | 2:51 (0s) | 2:38 (0s) |
| `verse_bare_hook_full` Verse bare, hook full | straight | 3:16 (33s) | 3:00 (30s) | 2:43 (27s) | 2:55 (22s) | 2:45 (21s) | 2:32 (19s) |
| `slow_motif` Slow burn: long intro on the lead, the motif grows | straight | 3:05 (33s) | 2:50 (30s) | 2:34 (27s) | 3:02 (22s) | 2:51 (21s) | 2:38 (19s) |
| `halftime_last_third` Half-time for the last third | switch | 3:11 (33s) | 2:55 (30s) | 2:38 (27s) | 3:05 (22s) | 2:55 (21s) | 2:41 (19s) |
| `a_then_b` A then B, two beats one song | switch | 3:16 (11s) | 3:00 (10s) | 2:43 (9s) | 3:09 (7s) | 2:58 (7s) | 2:44 (6s) |
| `call_response` Call and reply, two-bar phrases | straight | 3:16 (33s) | 3:00 (30s) | 2:43 (27s) | 2:55 (22s) | 2:45 (21s) | 2:32 (19s) |
| `lead_out_middle` Lead out for 8 bars, slams back | straight | 3:16 (33s) | 3:00 (30s) | 2:43 (27s) | 3:09 (22s) | 2:58 (21s) | 2:44 (19s) |
| `switch_strip` Drums cut, lead alone, then beat B slams in | switch | 3:27 (33s) | 3:10 (30s) | 2:52 (27s) | 3:02 (22s) | 2:51 (21s) | 2:38 (19s) |

| Use case | Layouts |
|---|---|
| Radio | hook_first, verse_bare_hook_full, lead_out_middle; cold ending |
| Playlist / algorithmic | hook_first, a_then_b |
| Hypnotic / minimal (a third of rolls) | bare_loop, one_in_one_out, kick_and_lead |
| Sync / underscore | slow_motif, kick_and_lead |
| Album-style | a_then_b, halftime_last_third, switch_strip |
