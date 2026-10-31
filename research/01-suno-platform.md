# Suno Platform: Current State (research for suno-forge)

Research date: 2026-10-31. All sources accessed 2026-10-31.

## How this was researched, and a caveat on access

Direct fetches of help.suno.com, suno.com, reddit.com, wikipedia.org and most third-party
blogs were blocked by this session's network policy (HTTP 403 at the egress proxy), so
official pages could only be read via search-engine snippets/summaries of those pages. Only
github.com was directly fetchable. Consequences:

- "VERIFIED" below means the fact appears in a search snippet of an official Suno page
  (help.suno.com, suno.com/blog, suno.com/release-notes) or is in-app UI text quoted verbatim
  by a source that has been hands-on with the app. The article URL is given so the project
  can confirm with one click.
- "COMMUNITY-REPORTED" means multiple independent community sources agree, or a
  hands-on maintainer tested it, but no official page states it.
- "UNVERIFIED" means a single source, or sources conflict.
- The search budget ran out before every item could be chased; those are in "Could not verify".

The single most important finding: **as of 2026-09-09 the only generatable Suno models are
v6, v6-wild and v6-mini. Every earlier model (v3.5, v4, v4.5, v4.5+, v4.5-all, v5, v5.5) is
retired.** A prompt generator that targets "v4.5+ limits" or "v5 behavior" is now targeting
models the user cannot select.

---

## Verified vs. unverified summary table

| # | Fact | Status | Primary source |
|---|------|--------|----------------|
| 1 | Current model picker: **v6**, **v6-wild**, **v6-mini** (+ "Custom: name" for Custom Models) | VERIFIED | help.suno.com/en/articles/13924737, 13924993 |
| 2 | v6 family released **2026-09-09**; all prior models retired same day | VERIFIED | suno.com/release-notes/introducing-v6; help 13924481 |
| 3 | v6, v6-wild = Pro/Premier; v6-mini = all plans incl. Free | VERIFIED | help 13924737, 13924481 |
| 4 | Max length **8 minutes** per generation, all three v6 variants | VERIFIED | help 13924929 |
| 5 | Standard generation = 2 songs for **10 credits** | VERIFIED | help 13924481 |
| 6 | **Max Mode** exists; "costs more credits"; recommended for >2 min, covers, style transfer, consistency | VERIFIED | help 13924481 |
| 7 | Max Mode = **2x credits (20/generation)**; tooltip "Uses more compute to maximize consistency throughout the song. Costs 2x credits per song." | COMMUNITY-REPORTED (in-app tooltip, quoted by hands-on maintainer; not on help pages) | bitwize issue #562, best-practices.md |
| 8 | **Variety** control exists; it "adjusts and updates your style prompts"; set to 0/Off to keep your style tags | VERIFIED | help 13924481 |
| 9 | Variety stops: Off "Exact style" / Normal "Balanced variety" / High "Distinct styles" / Extra "Bold exploration" / Max "Unreasonably varied"; default Normal (v6, v6-mini), Off (v6-wild) | COMMUNITY-REPORTED (in-app labels, hands-on) | bitwize creative-sliders.md, #562 |
| 10 | **Weirdness** slider: Safe to Chaos, 50% = "normal expected result"; Custom mode only | VERIFIED | help 6141377 |
| 11 | **Style Influence** slider: Loose to Strong | VERIFIED | help 6141377 |
| 12 | **Audio Influence** slider appears only with an audio upload | VERIFIED | help 6141377 |
| 13 | Slider defaults Weirdness 50 / Style Influence 50 / Audio Influence 25; range 0-100 | COMMUNITY-REPORTED (hands-on) | bitwize best-practices.md, suno-field-guide |
| 14 | Style field **1,000 chars**, Lyrics **5,000**, Exclude Styles **1,000** on v6 (unchanged from v4.5+/v5/v5.5) | COMMUNITY-REPORTED, consistent across many sources and measured by a hands-on maintainer on 2026-09-16; Suno publishes no limits table | bitwize #562, hookgenius, aimusicapi |
| 15 | Simple-mode description box 3,000 chars | COMMUNITY-REPORTED | bitwize #562 |
| 16 | Song title limit 80 vs 100 chars | UNVERIFIED (sources conflict) | hookgenius, sunoapi docs |
| 17 | Older limits: v3.5/v4 = ~200 style / 3,000 lyrics / 80 title | COMMUNITY-REPORTED (historical, moot now) | hookgenius |
| 18 | Duration control: Auto, or Custom 10 s to 6:00 (default 3:00), present on v6 | COMMUNITY-REPORTED (hands-on) | bitwize #562, CHANGELOG |
| 19 | **Instrumental** toggle exists in Custom mode ("toggle on Instrumental if you prefer not using lyrics") | VERIFIED | help 3726721 (Android), 3726657, 6882817 |
| 20 | **Exclude Styles** field exists; Pro/Premier; entries are bare terms (shown as "-piano") | VERIFIED | suno.com/release-notes/exclude-styles (2024-09-19); help 3161921 |
| 21 | Personas renamed **Voices**; "In the Create menu, the Voices button has replaced Personas"; Style Personas still inside Voices menu | VERIFIED | help 11362369 / 11362433 |
| 22 | "Songs with Voices cannot be instrumental" (UI text) | COMMUNITY-REPORTED (in-app text, hands-on) | bitwize best-practices.md |
| 23 | Artist names are rewritten: "We don't reference artists directly, so we've replaced your prompt with similar styles you might like." | COMMUNITY-REPORTED (in-app text, multiple hands-on sources; also cited in a publicity-rights lawsuit) | bitwize SKILL.md, #562 |
| 24 | Get Stems: Auto Split up to 12 stems / 50 credits; Split from Mix 10 credits per stem; Advanced Split (choose from ~100 instruments) 10 credits/stem, Premier only; Get Stems is Pro/Premier | VERIFIED | help 13925185, 12702337 |
| 25 | Download caps since 2026-09-03: Free 7 lifetime, Pro 20/mo, Premier 60/mo; Studio exports uncapped | VERIFIED | help 13614785, 13926209 |
| 26 | Credits: Free 50/day, Pro 2,500/mo, Premier 10,000/mo | VERIFIED | help 2417089 etc. |
| 27 | Suno Studio (browser DAW) is Premier-only; Studio 2.0 live | VERIFIED | suno.com/blog/studio-2; help 13670529 |
| 28 | Cover/Extend/Remaster/Replace Section/Crop credit costs | UNVERIFIED (Suno does not publish; third parties conflict) | lumimusic, sunodownloader |
| 29 | Front-loading: early words in the Style field carry more weight | COMMUNITY-REPORTED (consistent across guides; no official statement) | multiple |
| 30 | Comma list vs. prose: which is better | UNVERIFIED (community split) | multiple |
| 31 | "Custom" vs "Advanced" as the current name for the detailed mode | UNVERIFIED (sources conflict; UI changed mid-Sept 2026) | jackrighteous, note.com, bitwize |

---

## 1. Available models today

### The lineup (VERIFIED)

Suno's help center article "Current Models: v6" (help.suno.com/en/articles/13924737) and the
"v6 FAQ" (13924481) describe exactly three generatable models, plus user Custom Models:

| Picker label | Tier | Release | Max length | Official positioning |
|---|---|---|---|---|
| **v6** (picker shows "v6 · Pro") | Pro, Premier | 2026-09-09 | 8 min | "our flagship model ... reliable, precise and consistently delivers polished music across every genre and style"; "expressive, versatile, high-quality generations" |
| **v6-wild** ("v6-wild · Pro") | Pro, Premier | 2026-09-09 | 8 min (observed 50 s to 7 min, unpredictable) | "built for exploration ... less predictable and more varied"; "leans into unexpected choices, genre-blending, and results that push further from the prompt"; "unexpected, textured and ambitious results" |
| **v6-mini** | All plans incl. Free | 2026-09-09 | 8 min | "faster and more efficient"; "brings improved generation to the Free plan"; the only model a Free account can select |
| **Custom: \<name\>** | Pro, Premier | (v5.5-era feature, auto-upgraded to v6) | 8 min | Private model fine-tuned on 6+ of your own tracks ("Upload 24+ songs for best results"); 100 credits to create; max 3 per account; not shareable |

Internal IDs seen in generation logs (COMMUNITY-REPORTED): `chirp-hawk` (v6), `chirp-hawk-wild`
(v6-wild), `chirp-goose` (v6-mini).

Release blog: "Introducing v6", suno.com/release-notes/introducing-v6 and suno.com/blog, dated
2026-09-09. Built "with industry partners, including Warner Music Group, BMG and Believe"
(suno.com/blog/wmg-partnership). The help FAQ: "All models prior to v6 have been retired";
old songs remain playable/shareable; Extend/Cover/Remaster on old songs now run on v6.

### Retired models (status check requested by the task)

| Model | Exists in picker today? | History |
|---|---|---|
| v3.5 | **No** (retired 2026-09-09) | Legacy; was the Free-tier model until Oct 2025; ~4 min max; ~200-char style / 3,000-char lyrics (COMMUNITY) |
| v4 | **No** | Nov 2024 era; "legacy, unpredictable" (COMMUNITY) |
| v4.5 | **No** | Launched to Pro/Premier **2025-05-01** (VERIFIED, help 5782593); raised max length from 4 to 8 min without Extend; raised style limit to ~1,000 chars (COMMUNITY) |
| v4.5+ | **No** | Mid-2025 (exact date could not be verified); shipped alongside Add Vocals / Add Instrumentals / Inspire (COMMUNITY) |
| v4.5-all | **No** | **2025-10-21**, "A new free model for everyone" (VERIFIED, suno.com/release-notes) |
| v5 | **No** | Announced **2025-09-23** for Pro/Premier (VERIFIED, x.com/suno status 1970583230807167300, musically.com 2025-09-25); cleaner mixes, instrument separation; launched with Suno Studio |
| v5.5 | **No** | **2026-03-26**, "Introducing v5.5: Voices, Custom Models, and My Taste" (VERIFIED, suno.com/release-notes); Duration slider added ~July 2026 (COMMUNITY) |
| v6, v6-wild, v6-mini | **Yes** | 2026-09-09 |
| Anything newer (v6.5, v7) | **No evidence** as of 2026-10-31 | |

### Instrumental-relevant notes per current model (COMMUNITY-REPORTED unless noted)

Launch-week and follow-up testing (bitwize CHANGELOG 2026-09-10/11, aggregated from ~15
creator videos and ~75 r/SunoAI posts; plus eesel.ai, replayedstudio, moelueker, aimusicpreneur):

- **v6 (flagship)**: Best prompt adherence of the three (one 7-genre A/B test: v6 won
  "prompt adherence" 7/7 vs. wild). Reported strong on pop, drum & bass, UK garage; drums
  "crisper and clearer ... no longer buried" vs v5.5 (aimusicpreneur), but another producer
  called v6 drums "flat" and over-processed (replayedstudio). Two independent reports of a
  **narrower stereo image** than v5.5. Multiple reports that mixes go **muffled after ~2:30 in
  standard mode**, which Max Mode is meant to address (Suno's own Max Mode guidance says use it
  for songs over two minutes). With Duration on Auto, outputs often run slower, sparser and
  longer than prompted. v6 stems split cleanly along instrument lines; drum stem usable.
  Covers hold melody/structure with Variety Off.
- **v6-wild**: Same Style/Lyrics/Exclude fields; Exclude Styles confirmed working. Defaults
  Variety to **Off** (the only model that does). Drifts from the brief *by design*; wins on
  "character" in some genres (indie rock, R&B/soul, metal), loses on fidelity/adherence. Length
  is unpredictable. Several reviewers found it "not always distinguishable from v6". Suggested
  workflow: generate on wild, Cover the keeper on v6. Max Mode and Variety > Off on wild are
  untested. Not a synonym for high Weirdness; it is a different model checkpoint.
- **v6-mini**: "Simpler and often more artifacts" (The Verge, via bitwize). In a 150-song test
  it landed "much closer to V6 than to Wild ... a little flatter". Free accounts also see
  1-minute "v6 preview" clips of the paid model with an "Upgrade for full song" button. Free
  downloads (7 lifetime) are non-commercial.
- **Tendency to add vocals**: No v6-specific official statement. Community reports (Undetectr
  2026) that asking v6 to "remove humming" in the Style field produced *more* humming, i.e.
  negative phrasing in Style backfires; the fix is Exclude Styles. See section 6.

---

## 2. Custom Mode: field names and character limits

### Field names

Official help pages (Android/iOS Create articles 3726721, 3197377; Personas 3484161; Exclude
3161921) use these labels: **Simple** vs **Custom** mode; **Styles** (older pages say "Style of
Music"); **Lyrics**; **Instrumental** toggle; **Advanced Options** (containing **Exclude
Styles**, and in older pages the Exclude menu "at the top"); **Song Title**; **Personas** area
"above the lyrics field" (now **Voices**); **Workspace** (where new songs land; stems are reached
from "your Workspace or Library ... More Actions (...) menu").

Hands-on 2026-09 reports (bitwize CHANGELOG 2026-09-16; jackrighteous "Inside Suno V6:
Create Explained") describe the v6 web UI as **Simple / Advanced / Sounds**, with the detailed
controls under **More Options** (Variety, Max Mode, Personalize, Vocal Gender, Duration,
Exclude Styles, Weirdness, Style Influence, Audio Influence). One user (note.com, 2026-09-18 to
09-20) reported the Simple/Advanced split merging and then separating again. So:

- UNVERIFIED which label ("Custom" or "Advanced") the detailed mode carries today. Both terms
  are in current circulation; help pages still say Custom. suno-forge copy should say
  "Custom (Advanced) mode" or similar.
- "More Options" vs "Advanced Options": both reported; the newer hands-on reports say More
  Options.

### Character limits

Suno publishes **no** limits table. Figures below are read off the live field counters by
multiple independent community sources and were re-measured on a Pro account on 2026-09-16
(bitwize CHANGELOG). They did **not** change with v6.

| Field | v6 / v6-wild / v6-mini (current) | v4.5 through v5.5 (retired) | v3.5 / v4 (retired) |
|---|---|---|---|
| Styles / Style of Music | **1,000** chars | 1,000 | ~200 (some guides say 120 as advice, not a cap) |
| Lyrics | **5,000** chars | 5,000 | 3,000 |
| Exclude Styles | **1,000** chars | 1,000 | n/a or same |
| Song Title | **80 or 100** (UNVERIFIED; sources conflict; third-party API docs say 80, several UI guides say 100) | same conflict | 80 |
| Simple-mode description | **3,000** chars | ~500 earlier | ~200 |

Limits are tied to the selected model, not the plan. Since only v6 models exist, one set of
limits applies to everyone. Note the official v4.5 help article (5782977) recommends putting
extra genre/production context in the **Lyrics** box when Style is not enough; this still works
and for instrumentals is where structure tags go.

---

## 3. Advanced Options / sliders

Official article: "How to Use: Creative Sliders" (help.suno.com/en/articles/6141377, June
2025, introduced in the "Artist Update" for Pro/Premier; "Creative Sliders are used in Custom
mode").

| Control | Range | Default | Official description | Community understanding | Models |
|---|---|---|---|---|---|
| **Weirdness** | 0-100% ("Safe" to "Chaos") | **50%** (official: 50% is "the normal expected result") | Controls how far from the expected result the model goes | 10-30 safe/commercial; 40-60 balanced; 60+ experimental (tempo shifts, odd instrumentation). Raises take-to-take variance | All v6 models (hands-on confirmed on v6) |
| **Style Influence** | 0-100% ("Loose" to "Strong") | **50** (COMMUNITY; not stated officially) | "choose how close you stay to your style input" | Higher = stricter adherence to Style tags; 70+ for specific engineered prompts; very high on thin/contradictory prompts gives rigid results. Common "commercial" setting cited: Style Influence 70 / Weirdness 20 | All v6 models |
| **Audio Influence** | 0-100% | **25** (COMMUNITY) | Appears "only if you're using an Audio Upload" | How closely output follows uploaded audio (Cover, Upload, Voices). **Not used by suno-forge**; tell users to leave it alone / it will not appear without an upload | All |
| **Variety** (new with v6) | 5 discrete stops, shown as a slider 0-4: Off "Exact style", Normal "Balanced variety", High "Distinct styles", Extra "Bold exploration", Max "Unreasonably varied" | **Normal** on v6, v6-mini, Custom Models; **Off** on v6-wild. Default re-asserts when you switch models | Official FAQ: "The Variety slider is designed to introduce variety in your outputs by adjusting and updating your style prompts. If you'd like to retain full control of your style tags, reduce the Variety slider to 0." | At any setting above Off, Suno **rewrites/expands the Style text** (differently for each of the two takes) before the model sees it; this is the #1 reported cause of "v6 ignores my prompt". Engineered prompts should run at **Off** | v6 family only |
| **Personalize** | toggle | Off | "Make Variety match your taste" (uses My Taste profile) | Has no effect when Variety is Off | v6 family |
| **Vocal Gender** | Male / Female / — | — | | Irrelevant for instrumentals; leave at — | v6 family |
| **Duration** | Auto, or Custom 10 s to 6:00 | Auto (Custom default 3:00) | Not found on help pages | Custom hard-cuts at the value; short settings can cut a song off mid-phrase; Auto on v6 tends to run long. Added ~July 2026 for v5.5; still present on v6 | v6 family |

Notes: typing "Weirdness 20%" into the Style field does nothing (community). Double-clicking a
slider resets it to default (community). Suno's own guidance quoted by suno-field-guide:
"Tweak one slider at a time; don't jump above 50% immediately."

---

## 4. Persona / Voices feature

- **What it is**: Personas (launched Nov 2024) "save the essence of a song, including its
  vocals and style" for reuse; public by default, can be set private; created from a library
  song via More Actions > Create > Make Persona; in Custom mode the Persona area sits above
  Lyrics and selecting one auto-fills its style under Style of Music (VERIFIED, help 3484161).
- **Renamed**: with v5.5 (2026-03-26) "In the Create menu, the Voices button has replaced
  Personas. However, Style Personas are still available within the Voices menu" (VERIFIED, help
  11362369/11362433). The picker now offers "Voice (new) — better vocal consistency" and "Style
  Voice (legacy) — the old method" (COMMUNITY, in-app text). Old Voices show "Upgrade Voice to
  v6".
- **Voices (new)** = your own recorded/uploaded voice (15 s to 4 min upload; 18+; rights
  attestation; Pro/Premier). Requires a compatible model (was v5.5-only; now v6).
- **Instrumentals**: in-app text reads **"Songs with Voices cannot be instrumental"**
  (COMMUNITY-REPORTED, hands-on). A Voice is a vocal identity, so it is incompatible with the
  Instrumental toggle. Whether a legacy *Style* Persona can be combined with Instrumental is
  not documented; the help article says a Persona carries "vocals and style" and auto-fills the
  Style field, so at best it is a style-prefill.
- **Verdict for instrumental-only workflows**: not worth using. Voices are blocked outright;
  Style Personas add nothing a well-written Style field cannot, and risk importing vocal traits.
  For album-level consistency of an *instrumental* sound, the relevant tool is a **Custom
  Model** (6+ owned tracks, 100 credits, Pro/Premier), not a Persona. (COMMUNITY: Custom Models
  can be used for instrumentals; not tested here.)

---

## 5. Max Mode

- **Official (help 13924481)**: "Max Mode is an option you can turn on for any generation when
  you want v6 to spend more on getting it right. It costs more credits and it's best for: songs
  longer than two minutes, covers where you want the result to stay close to the original,
  transferring the style of one song onto another, and keeping vocals and style consistent
  through the whole track." Standard mode "is fine for quick ideas and shorter songs".
- **Cost**: in-app tooltip "Uses more compute to maximize consistency throughout the song.
  Costs 2x credits per song." = **20 credits per generation** (vs 10). Suno's help pages do not
  print the multiplier; the tooltip does (COMMUNITY-REPORTED, hands-on, consistent across
  sources).
- **Models**: toggle present on v6, v6-wild, v6-mini (plan-gated on Free; untested on wild).
- **What it changes**: consistency over the duration; community links it to the "muffled after
  2:30" degradation in standard mode. One launch-day report of two weak outputs with Max Mode on
  (single report). No separate "Pro"/"Studio quality" generation mode exists; "Studio" is the
  Premier DAW (section 7), not a quality mode.
- **For suno-forge**: recommend Max Mode **on** for any instrumental intended to exceed ~2:00
  (most beats), **off** for 30-60 s palette tests.

---

## 6. Forcing a true instrumental

### Mechanisms available

1. **Instrumental toggle (Custom/Advanced mode)** — VERIFIED to exist (help 3726721: "toggle
   on Instrumental if you prefer not using lyrics"; help 6882817 refers to "an instrumental ...
   using the Instrumental toggle in the create form"). This is Suno's own no-vocals switch and
   is the primary method. With it on, the Lyrics box is either hidden or treated as structure
   only (behavior differs by UI version; see below).
2. **Structure tags in Lyrics** — `[Intro]`, `[Instrumental]`, `[Instrumental Break]`,
   `[Break]`, `[Drop]`, `[Build]`, `[Bridge]`, `[Outro]`, `[End]`, and described variants like
   `[Instrumental Break - Guitar Solo]`. Community (hookgenius, songsmith, blakecrosley,
   bitwize) consistently says bracket tags are read as *structural* instructions and are
   followed more reliably than prose in Style.
3. **Leaving Lyrics blank** — works with the toggle on; without the toggle, blank lyrics in
   Custom mode causes Suno to write its own lyrics (community, consistent).
4. **"instrumental" / "no vocals" in Style** — weak and sometimes counterproductive: Suno
   reads the token "vocals" and the community reports that negative phrasing in Style can pull
   the thing in ("asked V6 to remove the humming and got *more* humming", Undetectr 2026).
   HookGenius still recommends "instrumental, no vocals" near the front of Style *in addition
   to* the toggle; bitwize's hands-on notes say inline "no X" in Style is ignored on v6.

### Toggle on AND tags in Lyrics: does Suno honor the tags?

COMMUNITY CONSENSUS (bitwize SKILL.md hands-on workflow, hookgenius, suno-field-guide,
blakecrosley): **yes**. The documented v6 instrumental workflow is: Instrumental **On**; Style
describes genre/instrumentation/mood/tempo with **no vocal descriptors**; Lyrics contains
**structure tags only** (e.g. `[Intro]`, `[Main Theme]`, `[Drop]`, `[Bridge]`, `[Outro]`,
`[End]`) with no sung lines. This was exercised on a Pro account 2026-09-16 ("Advanced Mode, an
engineered Style Box at Variety Off, structure tags, Exclude Styles, and Max Mode On. It behaved
as the docs describe"). Caveats: `[Instrumental]` as a *section* tag "may add a measure or two"
rather than a full solo; `[Instrumental Break]` and named breaks (`[Guitar Solo]`,
`[Melodic Interlude]`) are reported as the most reliable mid-song tags; `[End]` works ~85-90%
of the time (UNVERIFIED single source). No official Suno statement on tag handling in
instrumental mode exists.

### The humming / ad-lib / vocal-chop failure mode and how people prevent it

Reported on every model generation including v6. Causes identified by community: (a) Style
descriptors that *imply* voice ("soulful", "gospel", "anthemic", "chant", "choir pad", "vocal
chops", "R&B"); (b) Variety above Off rewriting the Style text and adding descriptors; (c) long
atmospheric intros where the model "fills" with hums; (d) the model not classifying humming or
wordless vocals as "vocals".

Prevention stack (community, consistent across hookgenius, Undetectr, freesongwritingtools,
Meloty, james-palm, bitwize):

1. Instrumental toggle **On**.
2. **Variety Off** (so the engineered Style is used verbatim).
3. Strip vocal-implying adjectives from Style; name concrete instruments instead.
4. **Exclude Styles** (bare terms, comma-separated, no "no"): a representative union of
   community lists is `vocals, vocal, singing, lyrics, rap, choir, humming, ad-libs, vocal chops,
   spoken word, crowd vocals, backing vocals`. Hands-on guidance (bitwize) says Exclude works
   best with **2-4 items**; others use longer lists. Suggested compromise for suno-forge: default
   to a short core list (`vocals, humming, ad-libs, vocal chops`) and expose the long list as an
   option.
5. Structure tags only in Lyrics; optionally `[Instrumental]` at the top.
6. Keep intros short or describe them positively (`[Intro: filtered drum loop, 4 bars]`).
7. If a take still leaks a hum, use **Replace Section** (Pro/Premier) on that region rather
   than regenerating.

Official Suno position: the Exclude article (help 3161921) says to enter "instruments or other
details you want left out"; Exclude Styles release note (2024-09-19) shows excluded terms listed
as "-piano". Nothing official addresses humming specifically.

---

## 7. Other current features relevant to the workflow

| Feature | What it does | Tier | Credits | Status |
|---|---|---|---|---|
| **Extend** | Adds music after a chosen point ("click and drag from the white arrow"); "Get Whole Song" stitches. Max total 8 min. | All (Free can extend) | Not published; community says 10 | VERIFIED feature, UNVERIFIED cost (help 2409601) |
| **Replace Section** | Highlight a region, edit lyrics/prompt, Confirm, get two versions | Pro/Premier | Not published; one source says 4 credits under 29.5 s else 10; another says 10 | VERIFIED feature (help 3271873), UNVERIFIED cost |
| **Remaster** | "create subtle variations of a clip, giving you more control over the final sound"; now runs on v6 | Pro/Premier (community) | Not published; community says 10 | VERIFIED feature (help 8105281) |
| **Cover** | Re-render a song in a new style; part of the "Remix" group | All | Not published; one source claims first 200 free then 10 (single source) | VERIFIED feature; UNVERIFIED cost. Max Mode + Variety Off recommended for faithful covers |
| **Crop** | Trim start/end on the waveform; result added to Library/Workspace | Pro/Premier (community) | Likely free (community) | VERIFIED feature (help 3166529) |
| **Get Stems** | Auto Split up to **12 stems**, 50 credits; Split from Mix (one stem + complement), 10 credits per stem; Advanced Split (pick from ~100 instruments), 10 credits per stem, **Premier only** | Pro/Premier | as stated | VERIFIED (help 13925185, 12702337) |
| **Suno Studio / Studio 2.0** | Browser multitrack DAW: generate stems into a timeline, upload/record, tempo, effects, automation, plugins; exports 32-bit/48 kHz, exports **not** subject to download caps; "Regenerate section" reported garbling vocals for two creators | **Premier** | Uses credits for generations | VERIFIED (suno.com/blog/studio-2, help 13670529) |
| **Song Editor** | Waveform editor hosting Replace Section / Crop / Extend | Pro/Premier | | VERIFIED (help 6141505) |
| **Add Vocals** | Layers generated vocals onto an uploaded or generated instrumental from your lyrics; Audio Strength slider | Pro/Premier beta | | VERIFIED (help 6882817) |
| **Add Instrumental** | Builds an arrangement around uploaded vocals (uploads only, not Suno clips) | Pro/Premier | | COMMUNITY (no help article found) |
| **v6 Simple-mode extras** | Plain-language section edits ("make the second chorus a gospel choir"), single-lyric swaps, mashups from multiple songs/playlists/uploads/images/video; many image/video inputs raise credit cost | varies | 10 base | VERIFIED (help 13924801, 13924481) |
| **Download caps** | Since 2026-09-03: Free 7 lifetime (non-commercial), Pro 20/mo, Premier 60/mo; one song = one download incl. all stems/WAV; re-downloads free; extra download packs purchasable; streaming/sharing unlimited | | | VERIFIED (help 13614785, 13926209) |
| **Credits** | Free 50/day (v6-mini only); Pro 2,500/mo; Premier 10,000/mo; Pro/Premier get 50/day after monthly credits run out | | | VERIFIED (help 2417089) |
| **Pricing** | Pro ~$10/mo, Premier $24-30/mo (sources conflict; annual $288) | | | UNVERIFIED exact prices |

---

## 8. Style-field weighting, formatting, and artist names

- **Front-loading (COMMUNITY, consistent)**: every current guide (hookgenius, roo.beehiiv,
  bitwize tip 11, suno-field-guide, freesongwritingtools) says the earliest tokens carry the
  most weight: "the first genre has the most influence, and each later tag has diminishing
  weight"; recommended order Genre -> sub-genre/era -> tempo/key -> lead instruments -> drums/bass
  -> production/texture -> mood. Keep to ~10 focused descriptors (3-5 minimal, 8-15 detailed,
  ~20 max); synonym piles dilute. Suno has never published a weighting statement.
- **Comma list vs. prose (UNVERIFIED, community split)**: one camp says the Style field is a
  comma-separated tag list where order matters less than specificity; another says commas make
  items roughly equal candidates and periods/"with"/"and" mark required elements; a third says
  Suno's own v4.5 prompt-enhancer produces conversational prose and the model was trained on
  rich descriptions. Suno's v4.5 blog does say users "can now rely on evocative descriptions".
  Practical consensus: short comma-separated noun phrases, grouped by sentence/period into
  blocks, concrete nouns and numeric BPM over adjectives. **Variety must be Off** or this
  engineering is rewritten anyway.
- **Inline negatives in Style** ("no vocals", "no drums"): reported ignored or counterproductive
  on v6; use Exclude Styles.
- **Artist names (COMMUNITY-REPORTED, in-app text)**: real artist, band, producer and composer
  names are not blocked with an error; Suno **rewrites** the prompt and shows a notice: **"We
  don't reference artists directly, so we've replaced your prompt with similar styles you might
  like."** and/or **"Artist name '…' replaced"**. The replacement discards the intent, so
  describe the sound instead (e.g. "dark trap, sliding 808s, sparse minor-key synth lead"). v6
  did not relax this ("opt-in artist products are phase two"). A 2026 publicity-rights class
  action alleges the filter is "easily defeated" by misspellings; suno-forge should not rely on
  that and should keep its own blocklist (the bitwize project maintains
  reference/suno/artist-blocklist.md including Metallica, Nirvana, Hans Zimmer, Koji Kondo).

---

## Could not verify (explicit list)

1. Song **Title** character limit: 80 vs 100 (conflicting third-party sources; no official).
2. The exact current label of the detailed create mode: "Custom" (help pages) vs "Advanced"
   (2026-09 hands-on reports), and "Advanced Options" vs "More Options".
3. Official numeric **defaults** for Style Influence (50) and Audio Influence (25); only
   Weirdness 50% is stated officially.
4. **Max Mode = 2x credits** is in-app tooltip text, not on any help page I could reach.
5. **Credit costs** for Cover, Extend, Remaster, Replace Section, Crop (Suno does not publish;
   third parties conflict: Replace Section 4 vs 10; Cover "first 200 free").
6. **v4.5+** release date (mid-2025 presumed; no official note surfaced). Moot: retired.
7. Whether **v5.5** officially supported 8-minute generation (v4.5 and v6 do; v5.5 page not read).
8. Whether a legacy **Style Persona** can be combined with the Instrumental toggle.
9. Whether **Custom Models** work for instrumental-only generation (reported, untested).
10. Whether Variety is rendered as a continuous slider or 5 discrete stops (FAQ says "reduce
    the Variety slider to 0"; hands-on says five labeled stops).
11. Any **hip-hop-specific** v6 drum/808 quality assessment from a producer source (results
    were generic prompt-template blogs).
12. The current **Pro/Premier prices** (sources gave Premier $24 and $30).
13. Any newer model than v6 (none found; absence of evidence as of 2026-10-31).
14. Direct r/SunoAI threads on v6 instrumental vocal leakage (reddit was unreachable; only
    second-hand summaries via blogs and the bitwize dossier of ~75 posts).

---

## Implications for suno-forge

1. **Target v6 only.** Drop per-model limit tables for v3.5/v4/v4.5/v5/v5.5; they cannot be
   selected. Offer a model recommendation: **v6** for finished beats, **v6-wild** for
   exploratory/experimental palettes (expect drift and unpredictable length), **v6-mini** for
   Free users and quick sketches.
2. **Limits to enforce**: Styles <= 1,000 chars; Lyrics <= 5,000; Exclude Styles <= 1,000;
   Title <= 80 (safe bound). Hard-cap in the generator with a counter.
3. **Always emit a settings block** alongside the prompt: Mode Custom/Advanced; Instrumental
   ON; **Variety OFF** (critical: otherwise Suno rewrites the Style text); Max Mode ON for
   anything over ~2:00; Weirdness ~20-40 and Style Influence ~65-80 for genre-faithful beats
   (community ranges; 50/50 defaults for balanced); Vocal Gender —; Audio Influence: leave
   alone / do not upload audio; Personalize off; Duration Auto or Custom to target length
   (10 s to 6:00).
4. **Instrumental enforcement**: toggle ON + structure-only Lyrics (`[Intro]` ... `[Outro]`
   `[End]`) + Exclude Styles with a short core list (`vocals, humming, ad-libs, vocal chops`,
   optionally expanded) + no vocal-implying adjectives in Style. Do not rely on "no vocals" in
   Style; if included, put it at the front and keep the toggle as the real control.
5. **Style field structure**: front-load genre/sub-genre, then BPM as a number, then drums/808
   character, then lead instruments, then production/texture, then mood; ~10 descriptors;
   comma-separated phrases grouped by periods; no inline negatives; no artist names (generate a
   "sounds-like" description instead and warn users that names get silently rewritten).
6. **Do not offer Persona/Voice options** for instrumental output; mention Custom Models as the
   Pro/Premier route to a consistent house sound.
7. **Credits/UX copy**: 10 credits per 2 takes, 20 with Max Mode; Exclude Styles, Replace
   Section, Get Stems, Studio require Pro/Premier (Studio: Premier). Download caps mean users
   should iterate with streaming and download only keepers.
8. **Re-verify monthly.** Suno changed model lineup, mode names and download rules within one
   month (Sep 2026). The help URLs below are the stable anchors to recheck.

---

## Sources (all accessed 2026-10-31)

Official (read via search-engine summaries; direct access blocked in this session):
- Suno help, "v6 FAQ": https://help.suno.com/en/articles/13924481 (published ~2026-09-09)
- Suno help, "Current Models: v6": https://help.suno.com/en/articles/13924737 (~2026-09-09)
- Suno help, "What's new in v6?": https://help.suno.com/en/articles/13924801 (~2026-09-09)
- Suno help, "How do I change models?": https://help.suno.com/en/articles/13924993 (~2026-09)
- Suno help, "How long will my song be?": https://help.suno.com/en/articles/13924929 (~2026-09)
- Suno help, "How to Use: Creative Sliders": https://help.suno.com/en/articles/6141377 (June 2025)
- Suno help, "What is included in Suno's Artist Update?": https://help.suno.com/en/articles/6141313 (June 2025)
- Suno help, "How do I exclude elements of a song?": https://help.suno.com/en/articles/3161921 (~Dec 2025 revision)
- Suno help, "Android Create: Custom Mode": https://help.suno.com/en/articles/3726721; "iOS Create: Custom Mode": https://help.suno.com/en/articles/3197377; "Android Create: Simple Mode": https://help.suno.com/en/articles/3726657
- Suno help, "What are Personas?": https://help.suno.com/en/articles/3484161 (~Dec 2025 revision)
- Suno help, "Voices: Use Your Voice in Suno": https://help.suno.com/en/articles/11362369; "Voices FAQ": https://help.suno.com/en/articles/11362433 (Mar 2026)
- Suno help, "Using Custom Models": https://help.suno.com/en/articles/11362497 (Mar 2026); "My Taste": https://help.suno.com/en/articles/11362561
- Suno help, "Add Vocals": https://help.suno.com/en/articles/6882817
- Suno help, "How do I get stems?": https://help.suno.com/en/articles/13925185 (Sep 2026); "Advanced Stem Separation": https://help.suno.com/en/articles/12702337
- Suno help, "How do I make my song longer?": https://help.suno.com/en/articles/2409601; "Can I replace a section of a song?": https://help.suno.com/en/articles/3271873; "How do I crop a song?": https://help.suno.com/en/articles/3166529; "Remaster": https://help.suno.com/en/articles/8105281; "How to Use: Song Editor": https://help.suno.com/en/articles/6141505; "Create in V4.5: Better Prompts in Lyrics": https://help.suno.com/en/articles/5782977; "What's new in V4.5": https://help.suno.com/en/articles/5782593 (May 2025)
- Suno help, downloads: https://help.suno.com/en/articles/13614785 (Upcoming Changes FAQ), https://help.suno.com/en/articles/13926209, https://help.suno.com/en/articles/13876865 (Sep 2026)
- Suno help, credits/plans: https://help.suno.com/en/articles/2417089, https://help.suno.com/en/articles/9601601
- Suno help, Studio 2.0: https://help.suno.com/en/articles/13670529; Studio intro: https://help.suno.com/en/articles/7940161
- Suno release notes index: https://suno.com/release-notes
- Suno, "Introducing v6": https://suno.com/release-notes/introducing-v6 and https://blog.suno.com/blog/introducing-v6 (2026-09-09); product page https://suno.com/v6
- Suno, "Introducing v5.5: Voices, Custom Models, and My Taste": https://suno.com/release-notes/introducing-v5-5-voices-custom-models-and-my-taste (2026-03-26); blog https://suno.com/blog/v5-5
- Suno, "v4.5-all: A new free model for everyone": https://suno.com/release-notes/v4-5-all-a-new-free-model-for-everyone (2025-10-21)
- Suno, "Introducing v4.5": https://suno.com/blog/introducing-v4-5 (2025-05-01)
- Suno, "Exclude Styles" release note: https://suno.com/release-notes/exclude-styles (2024-09-19)
- Suno, "Introducing Suno Studio": https://suno.com/blog/suno-studio (2025-09); "Introducing Studio 2.0": https://suno.com/blog/studio-2 (2026)
- Suno, WMG partnership: https://suno.com/blog/wmg-partnership
- Suno on X, v5 launch: https://x.com/suno/status/1970583230807167300 (2025-09-23)

Press:
- Music Ally, "Suno launches v5...": https://musically.com/2025/09/25/suno-launches-v5-claiming-its-the-worlds-best-music-model/ (2025-09-25)
- Music Business Worldwide on v5: https://www.musicbusinessworldwide.com/suno-releases-most-powerful-version-yet-days-after-record-labels-allege-illegal-stream-ripping-from-youtube/ (2025-09)
- Wikipedia, "Suno (platform)" (snippets only): https://en.wikipedia.org/wiki/Suno_(platform)

Community / hands-on (2026 unless noted):
- bitwize-music-studio/claude-ai-music-skills, issue #562 "Update the plugin for Suno v6": https://github.com/bitwize-music-studio/claude-ai-music-skills/issues/562 (2026-09-10); reference/suno/models.md, best-practices.md, creative-sliders.md, CHANGELOG.md (entries 2026-09-10/11/12/16), 100-expert-tips.md, artist-blocklist.md; skills/suno-engineer/SKILL.md (fetched directly from GitHub)
- mttkllr/suno-field-guide: https://github.com/mttkllr/suno-field-guide (covers through v5.5, Mar 2026)
- HookGenius: https://hookgenius.app/learn/suno-character-limits/, /learn/suno-custom-mode-guide/, /learn/suno-instrumental-prompts/, /learn/suno-instrumental-music/, /learn/suno-negative-prompting/, /learn/suno-v6-guide/ (2026)
- Jack Righteous: sliders guide, duration slider guide, "Inside Suno V6: Create Explained", Exclude guide (jackrighteous.com, 2026)
- Undetectr: https://undetectr.com/blog/suno-ai-custom-mode-guide, /blog/suno-v6-intro-outro-prompts, /blog/suno-v6-changing-your-prompt, /blog/suno-v6-reactions (2026)
- The AI Musicpreneur: https://www.aimusicpreneur.com/ai-tools-news/suno-v6-v6-wild-v6-mini-launch/ and v6-vs-v5.5 comparison (2026-09)
- Mubert blog: https://mubert.com/blog/suno-v6-v6-wild-v6-mini-vs-v5-5 (2026-09)
- eesel.ai v6 review: https://www.eesel.ai/blog/suno-v6-review; replayedstudio.com v6 posts; moelueker.com 150-song test; choosely.ai; i-scoop.eu (2026-09/10)
- roo.beehiiv.com, "Suno v6 Is Live: Every Feature and the 96% Credit Trap" and prompt guide (2026)
- lumimusic.ai, "Suno Credits Explained" (2026); sunodownloader.ai credits explainer
- james-palm.medium.com, "Why Suno AI Adds Vocals When You Don't Ask For Them" (2025)
- freesongwritingtools.com, "How to Make a Suno Instrumental (No Vocals, Every Time)" (2025-26)
- Meloty, "How to fix Suno artifacts / random vocals" (2026)
- note.com user reports on the 2026-09-18/20 UI change and Duration slider (Japanese, 2026)
- docs.sunoapi.org (third-party API reseller; limits 1000/5000/80 for "V6" models) and aimusicapi.ai limits post (2026). Note: Suno has no official public API; these resellers' model identifiers are not Suno's.
