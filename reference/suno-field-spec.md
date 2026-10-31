# Suno field spec (verified 2026-10-31)

Source of truth for the generator and QA checker is `lib/spec.json`; this page is the human-readable mirror. Every row is tagged **VERIFIED** (official Suno help/blog/release notes), **COMMUNITY** (multiple independent hands-on reports; Suno publishes nothing) or **UNVERIFIED**. Details and URLs: `research/01-suno-platform.md`.

## Models

As of **2026-09-09** Suno retired every earlier model (v3.5, v4, v4.5, v4.5+, v4.5-all, v5, v5.5). Only these exist. **VERIFIED**

| Picker label | Tier | Max length | Positioning | suno-forge use |
|---|---|---|---|---|
| **v6** | Pro, Premier | 8 min | Flagship: "reliable, precise, polished"; best prompt adherence and structure-tag following | Default for every lane |
| **v6-wild** | Pro, Premier | 8 min (length unpredictable) | "Less predictable, more varied... pushes further from the prompt"; defaults Variety to Off | Experimental variant; try when v6 is too safe or too quantized (phonk and jid-dark benefit most) |
| **v6-mini** | All plans incl. Free | 8 min | Faster, lighter, more artifacts; "closer to v6 than to wild, a little flatter" | Free-plan fallback and cheap palette tests |

Community notes on v6 (not official): slightly narrower stereo than v5.5; standard-mode mixes can go muffled after ~2:30, which Max Mode addresses; Duration on Auto tends to run slow, sparse and long.

## Mode and field names

| Item | Name today | Notes |
|---|---|---|
| Detailed mode | **Custom** (help pages) / **Advanced** (2026-09 web UI reports) | Both in circulation. UNVERIFIED which label is live this week. |
| Options panel | **More Options** (newer) / **Advanced Options** (older help pages) | Holds Variety, Max Mode, Personalize, Vocal Gender, Duration, Exclude Styles, Weirdness, Style Influence, Audio Influence |
| Style field | **Styles** (older: "Style of Music") | |
| Lyrics field | **Lyrics** | Hidden or structure-only when Instrumental is on, depending on UI version |
| Instrumental toggle | **Instrumental** | VERIFIED exists in Custom mode |
| Negative field | **Exclude Styles** | VERIFIED; Pro/Premier; terms display as "-piano" |
| Title | **Song Title** | |
| Personas | **Voices** (renamed 2026-03-26; Style Personas live inside the Voices menu) | "Songs with Voices cannot be instrumental" (in-app text, COMMUNITY) |

## Character limits

Suno publishes no limits table; these are read off the live counters by several independent sources and re-measured on a Pro account 2026-09-16. Unchanged since v4.5. **COMMUNITY**

| Field | Limit | suno-forge soft target |
|---|---|---|
| Styles | **1,000** | ~500 (front-loaded; long prompts dilute early-token weight) |
| Lyrics | **5,000** | whatever the section map needs (typically 900-1,400 for an instrumental) |
| Exclude Styles | **1,000** | ~8 bare terms; trim to 4 if ignored |
| Song Title | **80** (some sources say 100) UNVERIFIED | stay under 80 |
| Simple-mode description | 3,000 | not used |

## Sliders and controls (Custom mode)

| Control | Range / stops | Default | What it does | suno-forge |
|---|---|---|---|---|
| **Weirdness** | 0-100 ("Safe" to "Chaos") | 50 = "normal expected result" VERIFIED | How far from the expected result the model wanders. Hip hop community range 20-40 commercial, 40-55 experimental; above 60 reports of tempo shifts, odd instrumentation, structure drift | Lane defaults 30-55; weirder variation caps at 70 |
| **Style Influence** | 0-100 ("Loose" to "Strong") | 50 COMMUNITY | How closely the output follows the Style text. 50-70 for specific engineered prompts; 70+ for thin prompts | Lane defaults 55-70 |
| **Audio Influence** | 0-100 | 25 COMMUNITY | Only appears when audio is uploaded | **Never used.** No audio references in this project. |
| **Variety** (v6 family) | 5 stops shown as a 0-4 slider: Off "Exact style", Normal "Balanced variety", High "Distinct styles", Extra "Bold exploration", Max "Unreasonably varied" (stop labels COMMUNITY) | Normal on v6 / v6-mini; Off on v6-wild | Official FAQ: it "adjusts and updates your style prompts... reduce the Variety slider to 0 to retain full control of your style tags." Above Off, Suno rewrites the Style text before the model sees it. VERIFIED | **Always Off.** The #1 cause of "v6 ignored my prompt". |
| **Max Mode** | toggle | Off | "Spend more on getting it right"; best for songs over two minutes, consistency through the track. VERIFIED. Costs 2x credits (20 vs 10) per in-app tooltip, COMMUNITY | **On** for every full-length beat; off for 30-60 s palette tests |
| **Duration** | Auto, or Custom 10 s to 6:00 (default 3:00) COMMUNITY | Auto | Custom hard-cuts at the value; Auto runs long on v6 | Custom = planned runtime + 10 s buffer, let `[End]` stop it early, Crop any tail |
| **Personalize** | toggle | Off | Makes Variety match your My Taste profile; no effect when Variety is Off | Off |
| **Vocal Gender** | M / F / none | none | Irrelevant to instrumentals | none |

## Forcing a true instrumental

Mechanism stack (Instrumental toggle VERIFIED; the rest COMMUNITY-consistent, exercised hands-on 2026-09-16):

1. Custom (Advanced) mode, **Instrumental ON**.
2. **Variety OFF** so the engineered Style text is used verbatim.
3. Style field describes genre, drums, instruments, mood, mix in positive terms; **no vocal-implying words** ("soulful", "gospel", "anthemic", "chant", "choir", "vocal chops", "R&B", "talkbox", "ad-lib"). Include the word "instrumental" once.
4. Lyrics field holds **bracketed structure tags only**, leading `[Instrumental]` line, `[End]` last, zero sung words, zero parentheses (parenthesized text is sung as backing vocals).
5. Exclude Styles lists vocal terms as **bare nouns**: `vocals, humming, ad-libs, vocal chops` plus up to 4 genre guards. Never "no vocals". Exclude is a bias, not a ban; shorter lists bite harder.
6. If a hum still leaks: Replace Section on that region, or re-roll. Do not add negatives to Style; "remove the humming" in Style produced more humming on v6.

Do structure tags still work with the toggle on? Community consensus is **yes** (hands-on v6 workflow: toggle on, tags-only Lyrics, Exclude list, Max Mode on "behaved as the docs describe"), but one guide reports conflicting experiences. Fallback if structure is ignored with the toggle on: toggle **off**, keep the tags-only Lyrics, keep "instrumental" in Style and the vocal terms in Exclude.

## Artist names

Suno does not error; it silently rewrites: "We don't reference artists directly, so we've replaced your prompt with similar styles you might like." You lose the whole engineered Style field. COMMUNITY (multiple hands-on reports). suno-forge bans names and "type beat / in the style of / sounds like" phrasing at QA time.

## Post-generation features

| Feature | Tier | Cost | Use in this workflow |
|---|---|---|---|
| Extend | All | ~10 credits (UNVERIFIED) | Continue a keeper whose back half drifted; paste only the remaining section tags |
| Replace Section | Pro/Premier | UNVERIFIED | Fix one leaked hum or an ignored break without re-rolling |
| Remaster | Pro/Premier | UNVERIFIED | Cleaner, louder master on the keeper (runs on v6) |
| Crop | Pro/Premier | likely free | Trim tails after `[End]` |
| Get Stems | Pro/Premier | Auto Split up to 12 stems = 50 credits; Split from Mix 10/stem; Advanced Split (~100 instruments) 10/stem, Premier only. VERIFIED | Export stems for a DAW polish |
| Suno Studio 2.0 | Premier | credits for generations | Browser DAW; exports bypass download caps |
| Cover / Add Vocals / Add Instrumental / Audio uploads | | | **Not used.** They are audio-reference features. |

Credits: standard generation 2 songs = 10 credits; Max Mode 20. Free 50/day (v6-mini only), Pro 2,500/mo, Premier 10,000/mo. Download caps since 2026-09-03: Free 7 lifetime, Pro 20/mo, Premier 60/mo. VERIFIED

## Could not verify (re-check with the `suno-researcher` agent)

Title limit (80 vs 100); whether the detailed mode is labeled Custom or Advanced today; official defaults for Style Influence and Audio Influence; the Max Mode 2x multiplier on an official page; credit costs for Cover, Extend, Remaster, Replace Section, Crop; whether a legacy Style Persona can combine with Instrumental; whether Variety is a continuous slider or five stops.
