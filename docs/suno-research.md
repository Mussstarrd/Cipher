# Suno Prompting Mechanics — Phase 1 Research

**Compiled:** 2026-07-25
**Purpose:** Empirical grounding for the CIPHER prompt engine. Every constant, rule, or heuristic encoded in the engine must trace back to a claim in this document. Claims are labeled **[OFFICIAL]** (Suno docs/blog/social), **[COMMUNITY]** (guides, wikis, creator blogs, aggregated Reddit practice), **[PRESS]** (trade press), or **[UNVERIFIED]** (no adequate source found — do not encode).

**Methodology caveat:** The research environment's egress proxy blocked direct page fetches for most sites (suno.com, help.suno.com, reddit.com, most guide sites), so the bulk of the evidence comes from search-index extracts of the cited pages, corroborated across multiple independent searches, plus two GitHub guides read in full (`daveshap/suno`, `AlijeeWrites/suno-v55-prompt-guide`). Reddit was unreachable entirely; "community consensus" is mediated through 2026 guide sites that aggregate community testing, and that ecosystem cross-copies heavily. Treat percentages and "tested" claims from guide sites as practitioner convention, not measurement. Phase 3 (real Suno runs) is where these get validated against audio.

---

## 1. Version landscape (July 2026)

| Model | Released | Availability | Notes |
|---|---|---|---|
| v4.5 | May 2025 | — (superseded) | Introduced 8-min songs, 1,000-char style field, "1,200+ styles", genre mashups |
| v4.5+ | July 2025 | — | Added Add Vocals / Add Instrumentals |
| v5 | Sept 23, 2025 | Pro/Premier | |
| v4.5-all | Oct 21, 2025 | **Free tier's model** | |
| **v5.5** | **March 26, 2026** | **Pro/Premier — current flagship** | Voices, Custom Models, My Taste, Duration slider |

- **v5.5 is real and current.** [OFFICIAL] https://suno.com/blog/v5-5 ; https://suno.com/release-notes (Mar 26, 2026); [PRESS] musicbusinessworldwide.com. The earlier spec-review suspicion that "Suno v5.5" was hallucinated is **withdrawn** — the version claim was correct. (The "Mureka V9" claim remains unchecked; Mureka is out of scope for Phase 1.)
- **"v6" does not exist** as of July 2026 — only announcements that licensed "music-industry partnership" models will roll out during 2026. [PRESS] musicbusinessworldwide.com "Where's v6?"; [COMMUNITY] jackrighteous.com. Label anything "v6" as rumor.
- Tier → model: Free = v4.5-all only; Pro ($10/mo) and Premier ($30/mo) = v5 and v5.5. [OFFICIAL extract] help.suno.com article 5782721 (model timeline); [COMMUNITY] pricing roundups, 2026.

**Engine implication:** Target **v5/v5.5 semantics** as the default profile, with a v4.5-all fallback profile (free tier). Never hard-code "v5.5" into user-facing output as an assumption of what the user runs — make the model profile a parameter.

## 2. Field character budgets

| Field | v4 and older | v4.5 / v4.5-all / v5 / v5.5 | Source quality |
|---|---|---|---|
| Style | 200 chars | **1,000 chars** | [COMMUNITY] strong consensus + third-party API docs; no official number published |
| Lyrics | 3,000 chars | **5,000 chars** | same |
| Title | 80 chars (API) / 100 (app) | 100 chars | same; minor conflict between sources |
| Exclude styles | undocumented | **1,000 chars** (per API resellers' `negativeTags` cap) | [COMMUNITY/API] docs.sunoapi.org, docs.kie.ai; official limit [UNVERIFIED] |
| Simple-mode "Description" | — | ~350 chars effective (text beyond reportedly ignored) | [COMMUNITY]; separate field from Custom-Mode Style — do not conflate |

- Sources: docs.kie.ai and docs.sunoapi.org `generate-music` docs (unofficial API resellers, 2026); hookgenius.app/learn/suno-character-limits/; aimusicapi.ai; raagengine.com (all [COMMUNITY], 2026). Suno's own docs do not publish exact numbers — the 200/1,000/5,000 figures are best-available consensus, not official.
- **Truncation is silent** — text past the cap is dropped with no warning. [COMMUNITY] hookgenius, roo.beehiiv, aimusicapi, 2026. Engine must therefore enforce budgets itself and warn loudly.
- No evidence that the character limits differ by subscription tier on the same model; limits track the model version. [COMMUNITY]
- A one-time "350-char style limit" claim in the wild is a field-confusion artifact (Simple-mode Description vs Custom-mode Style).

**Engine implication:** Per-field budgets confirmed as the right model: style ≤ 1,000 (target well under — see §3), lyrics ≤ 5,000, exclude ≤ 1,000 (target ≤ 5 items — see §5). Encode budgets per model profile (v4.5+ vs legacy).

## 3. Style field mechanics

### 3.1 Token position / front-loading
- **No official statement** on positional weighting exists. [UNVERIFIED officially]
- **Strong community consensus that early tokens dominate:** "terms at the front carry more influence"; "the first three words carry more weight than the last ten"; "place important tags in the first 20–30 words"; "moving genre from position 5 to position 1 improves accuracy." [COMMUNITY] hookgenius.app/learn/suno-prompt-guide-2026/; medium.com/@aitooldiscovery r/SunoAI aggregate (Jul 2026); musci.io/blog/suno-tags.
- Partial dissent: one v5.5 guide advises putting key elements "at the beginning **and end**" (primacy + recency). [COMMUNITY] github.com/AlijeeWrites/suno-v55-prompt-guide (read in full).
- Recommended ordering across many guides: **genre → mood → instruments → vocal style → production/BPM**.

### 3.2 Comma tags vs prose
- [OFFICIAL] From v4.5, conversational prose is supported ("your instructions can now include a more conversational prompt" — help.suno.com article 5782849; v4.5 blog touts "smarter prompt handling").
- [COMMUNITY] consensus: **comma-separated descriptor phrases still win for control**; the gap narrowed on v4.5+. The de-facto 2026 standard is a **hybrid**: comma-joined short phrases (not sentences, not bare single words) with embedded BPM and vocal direction. Even guides that advise "natural language" write all their examples as comma-phrase chains (AlijeeWrites, read in full).
- Length sweet spot: ~15–30 words / roughly 5–15 tags; guides conflict on the exact count — treat as a range. Vague affective words ("beautiful", "epic") are widely reported no-ops. Limit to 1–2 genres and 2–4 instruments to avoid signal averaging. [COMMUNITY]
- One conditioning-theory account: Suno "compresses text into a small, lossy conditioning vector dominated by the loudest tags" — so one strong unambiguous identity beats many weak signals. [COMMUNITY] roo.beehiiv.com (2026).
- Syntax rule: the **style box must be bracket-free** (brackets there can cause glitches/ignored genres); brackets belong only in the lyrics box. [COMMUNITY] tagasong.com.

### 3.3 Genre/mood vocabulary that works (rap/trap/phonk focus)
- [OFFICIAL] v4.5 supports "1,200+ styles" including explicit mashups ("midwest emo + neosoul", "EDM + folk") — mashup prompting is an officially advertised capability. suno.com/blog/introducing-v4-5. Official "Music Glossary for Suno" exists (help.suno.com article 9010177).
- [COMMUNITY] vocabulary lists: sunoaiwiki.com genre list (2024, still live); github.com/AlijeeWrites/best-suno-ai-prompts (170+ genres, read in full); hookgenius "300+ tested v5.5 tags" (their claim).
- **Trap formula** [COMMUNITY, hookgenius/jackrighteous/undetectr 2026]: subgenre + BPM + drum character + 808/bass character + mood + vocal flow + regional reference. Example: "Trap, 142 BPM half-time feel, crisp triplet hi-hat rolls and punchy clap, booming distorted 808 with glides, dark and confident, assertive male rap with sparse ad-libs, modern Atlanta sound."
  - **Name the 808 and hi-hats explicitly** — vague prompts default to weak low end. "heavy 808" = sustained sub; "distorted 808" = clipped/gritty.
  - Specify vocal flow ("confident male rap", "melodic auto-tuned vocals", "laid-back storytelling flow").
- **Phonk vocabulary** [COMMUNITY, openmusicprompt/hookgenius 2026]: "phonk cowbell" / "high-pitched cowbell" / "cowbell melody", "Memphis rap sample", "pitched-down vocal samples", "chopped and screwed", "distorted 808", "tape-saturated drums", "lo-fi crunch". "drift phonk" reportedly auto-triggers the characteristic cowbell melody; drift phonk ≈ 150–160+ BPM aggressive, Memphis phonk ≈ ~130 BPM darker/horror-tinged.
- **Emo rap vocabulary** [COMMUNITY, openmusicprompt 2026]: "emo rap, melodic trap, clean electric guitar riff intro, heavy 808s, auto-tuned melodic vocals, heartbreak lyrics, catchy hook, 150 BPM"; guitar tone descriptors ("clean electric guitar, chorus effect, reverb-soaked arpeggio").
- Genre-entry descriptor seeds from AlijeeWrites list (read in full): Trap ("808 bass, hi-hat rolls, dark atmosphere"), Atlanta Trap ("melodic trap, auto-tune, ad-libs"), Memphis Trap ("dark lo-fi trap, phonk influence"), Emo Rap ("emotional lyrics, guitar loops, melancholic"), Memphis Phonk ("distorted bass, cowbells, dark"), plus Trap Soul, Latin Trap.
- BPM anchors [COMMUNITY]: boom bap 85–100; trap 130–170 half-time; drill 140–145; drift phonk 130–160; Brazilian phonk 140–160. Naming the exact subgenre + BPM matters more than slider tuning for genre accuracy.

### 3.4 Artist names
- [OFFICIAL] Artist names are a moderation category: prompts containing "well known artist or people names, copyrighted or trademarked terms…" may fail to generate (help.suno.com article 3198209). Suno does not say which word triggered a block.
- Two observed failure modes [COMMUNITY, 2026]: (1) **hard block** with "prompt contained inappropriate material" — removing artist names "clears more blocks than any other change" (jackrighteous); (2) **silent strip/substitution** reported for v5.5 (roo.beehiiv — single source, treat as tentative).
- Replacement practice: **subgenre + era + region + production descriptors** (e.g. "modern Atlanta sound", "Toronto 808s + melodic R&B flows"), or Personas/Voices for identity persistence.

**Engine implication:** The artist-DNA → descriptor library is validated as the core need. Front-load genre tokens; emit comma-phrase hybrid style text of ~15–30 words within a 1,000-char ceiling; never emit artist names into any Suno-bound field.

## 4. Lyrics box and bracket tags

Universal caveat [COMMUNITY, all sources]: bracket tags are **probabilistic hints, not commands**; v5 respects them more consistently than earlier versions, but nothing is guaranteed.

### 4.1 Structural tags
- [OFFICIAL] Suno confirms `[Verse]`, `[Chorus]`, `[Break]`-style tags in the lyrics box guide structure (help.suno.com article 2415873). No complete official metatag list has ever been published.
- **Reliable tier** [COMMUNITY 2026, hookgenius et al.]: `[Intro]`, `[Verse]`/`[Verse 1]`/`[Verse 2]`, `[Pre-Chorus]`, `[Chorus]`, `[Bridge]`, `[Instrumental Break]`, `[Guitar Solo]`, `[Outro]`, `[End]`.
- Recognized wider set [COMMUNITY]: `[Hook]`, `[Instrumental]`, `[Break]`, `[Interlude]`, `[Drop]`, `[Build-Up]`, `[Breakdown]`.
- Placement rule: tag on its own line, directly above that section's lyrics.
- `[Chorus]` > `[Hook]` in reliability; v3-era testing found `[Hook]` behaves more like an intro. Use `[Chorus]`. [COMMUNITY conflict, resolved toward `[Chorus]`]
- `[Bridge]` is weak — historically often rendered like a verse; `[Instrumental Bridge]` was the more effective variant; use at most once. [COMMUNITY, daveshap v3-era, uncontradicted since]
- **Ordering is respected**: with tags, Suno follows the written section order; without tags it guesses (often wrongly). A `[Chorus]` recurs only if you write it (with lyrics) again; keep chorus lyrics identical between repeats for a consistent hook. [COMMUNITY 2026]

### 4.2 Beat switches / tempo changes
- **No verified `[Beat Switch]` tag exists.** [UNVERIFIED]
- `[Tempo Change: 95 BPM]` mid-song: one site (tagasong) claims a verified example; another (jgbeatslab) says mid-song BPM changes "usually sound broken" and recommends generating sections separately and splicing. [COMMUNITY conflict — treat as experimental]
- Practical beat-switch *feel*: `[Build]` + `[Drop]` (+ `[Breakdown]`) work across genres including hip-hop, plus describing the switch in the style field ("switch between minimal trap beats and explosive drops" appears as a live Suno user-style). [COMMUNITY jackrighteous; suno.com/style/* page]
- **Engine implication:** implement beat-switch templates via `[Build]`/`[Drop]`/`[Breakdown]` + style-field description; offer `[Tempo Change: X BPM]` only as a flagged experimental option.

### 4.3 Delivery / performance cues
- **Modified section tags (adjective form, no colon)** are the longest-standing working format: `[Whispered Chorus]`, `[Angry Verse]`, `[Spoken Verse]`. Emotive + intensity + pacing adjectives work best; genre adjectives inside brackets mostly don't; chorus modifiers are often ignored (the model decides chorus rendering). [COMMUNITY, daveshap read in full; still recommended 2026]
- **Colon form (`[Verse 2: aggressive triplet flow]`, `[Energy: High]`)**: promoted as "new in v5" by several 2026 guide sites but flagged experimental with variable results; an opposing camp says bracketed parameter tags read as unweighted strings. **Unresolved — flag as experimental in the engine.** [COMMUNITY conflict]
- **Parameter/percentage syntax is placebo**: `[Reverb: 30%]`, `[Vocals: Humanize]` do nothing. Descriptive words work; numbers don't (except BPM). [COMMUNITY, hookgenius "What Works, What's Placebo"]
- Reliably honored delivery dimensions on current models: **gender, delivery mode (whisper/rap/belt/spoken word), BPM, basic texture** ("breathy", "raspy" reliable; "falsetto" inconsistent). [COMMUNITY 2026]
- Standalone delivery tags in use: `[Whisper]`, `[Spoken Word]`, `[Scream]`, `[Yelled]`, `[Shouted]`, `[Growl]`, `[Rap]`, `[Auto-tune]`, `[Ad-lib]`. [COMMUNITY]
- Voice switching: `[Male Voice]` / `[Female Voice]` at the start of lines, reinforced with "male vocal"/"female vocal" in the style field. More reliable than the single-bracket colon form (`[Chorus: female vocal]` itself unverified). [COMMUNITY]
- **Rap flow control:** flow cues work ("aggressive", "technical", "fast flow"), but **the syllable density and cadence of the written bars control flow more than any tag** — multiple sources. [COMMUNITY]

### 4.4 Sound-design tags
- Working: `[Instrumental]`, `[Instrumental Break]`, `[Break]` (~one measure; instrument variants like `[Drum Break]`, `[Lead Guitar Break]` work), `[Guitar Solo]` (better with modifiers: `[Soaring Lead Guitar Solo]`), `[Interlude]`/`[Melodic Interlude]`, `[Build]`, `[Drop]`. [COMMUNITY, daveshap + 2026 lists]
- `[Bass Drop]`: works but genre-dependent (EDM-ish styles); use one at a time. `[Beat Drop]` as distinct tag: [UNVERIFIED].
- `[808]` / `[808 slide]` as lyrics tags: [UNVERIFIED] — 808 character belongs in the **style field** ("sliding 808s", "distorted 808 with glides").
- Asterisk sound effects (`*gunshots*`, `*static*`): v3-era evidence only; current behavior [UNVERIFIED].

### 4.5 Ad-libs (parentheses)
- **Strong cross-source consensus: parentheses produce background vocals / ad-libs / call-and-response** — `(oh yeah)`, `(hey!)`, echo pattern `I'm chasing the light (chasing the light)`. Verified from v3-era through 2026 guides; one of the most consistent findings. [COMMUNITY, multiple independent]
- Keep parentheticals **1–3 words** (max ~4); longer ones risk being sung verbatim as lead vocal. [COMMUNITY]
- Parentheses are **singable content, not commands** — instructions must be in `[ ]`. [COMMUNITY]
- `[Ad-lib]` bracket tag exists in guides but is less consistently documented than plain parentheticals. "no ad libs" as a directive reportedly reduces spontaneous ad-libs. [COMMUNITY]

### 4.6 Hook-first structure and runtime control
- **Hook-first:** put `[Chorus]` as the first tag (no `[Intro]`) — Suno starts with it after a short instrumental lead-in. To suppress the lead-in: `[Intro: Cold Open]`, `[Intro: Hook Preview]`, or the reported `[Urgent Intro]` trick (first vocal by ~6s). [COMMUNITY, hookgenius/songsmith/apipass 2026]
- Hook craft: chorus 2–4 lines, heavy repetition, simple phrasing. [COMMUNITY]
- **Runtime:** no official length parameter in the lyrics box (v5.5 adds a UI **Duration slider**, web, July 2026 [OFFICIAL release notes]). Length is controlled by lyric quantity × section count × BPM:
  - <~15 lines of lyrics → short song. [COMMUNITY]
  - ~2:30 target ≈ 2 verses (6–8 lines each) + 2–3 choruses (4–6 lines), no bridge (or 2-line bridge), moderate-to-fast BPM. At 80 BPM every section runs ~50% longer than at 120 BPM; ~3 syllables/beat compresses, 1/beat stretches. [COMMUNITY, songsmith "song length formula"]
- **Clean ending:** `[Outro]` then `[End]` as the final line with nothing after — "90% of abrupt cutoffs come down to missing or misplaced ending tags". `[Fade Out]` alone is inconsistent. [COMMUNITY]
- Max song length [OFFICIAL, help.suno.com article 2409473]: v3.5/v4 = 4:00; v4.5/v5 = 8:00 (some community sources said 4:00 for v5 — official says 8, went with official). v5.5 = Duration slider; only third-party API docs give a number (10–360s) — official max [UNVERIFIED].

### 4.7 Text tricks (caps, punctuation)
- ALL CAPS = shouted/louder delivery — claimed by all current-era guides, but v3-era testing said caps were ignored; no rigorous A/B test found. Use sparingly (1–3 words per section, or the effect averages out). [COMMUNITY, version-dependent, partially verified]
- `!` = attack/aggression on the line's end — with **bleed-over**: the aggressive delivery carries into following lines and is hard to walk back to melodic. [COMMUNITY 2026 + weak v3-era corroboration]
- Ellipses `...` slow delivery. [COMMUNITY, daveshap, echoed 2026]
- Stretched spellings work and are required for vocalizations: "Oooooohhh whoaaa" — the model won't render non-word vocalizations unless written out. [COMMUNITY]

**Engine implication:** Structure templates should emit the reliable-tier tags only by default; colon-form delivery cues, `[Tempo Change]`, and asterisk SFX go behind an "experimental" flag. Lyric shaping (density, caps, `!`, ellipses, parenthetical ad-libs ≤3 words) is a first-class engine feature, since bar cadence beats tags for flow control.

## 5. Exclude-styles field

- Launched Sept 19, 2024, Pro/Premier early-access; Custom Mode → Advanced Options → Exclude; excluded terms display with `-` prefix (display only — **do not type minus signs**). [OFFICIAL] suno.com/release-notes/exclude-styles; Suno X post; help.suno.com article 3161921. Free-tier availability in 2026 [UNVERIFIED].
- **Behavior: soft guidance, not a hard ban. Leakage is universal consensus** and implicit in Suno's own wording ("gives the model information about elements you do not want"). Every guide surveyed independently warns excluded elements can still appear — especially when the positive style prompt implies them. [OFFICIAL wording + COMMUNITY unanimous]
- The exclude field **is** parsed more reliably than writing "no X" in the style field. The "don't think of an elephant" effect is documented for **negations inside the style field** ("'no drums' makes it worse — the model focuses on drums"), not for the dedicated exclude field; no source claims the exclude field increases likelihood. [COMMUNITY]
- **Dominance order: Style beats Exclude.** A strongly implied element in the style prompt overrides its exclusion. [COMMUNITY]
- **Best practice — pair every exclusion with a positive replacement** that fills the vacated musical role: e.g. instead of only `Exclude: guitar` → `Styles: piano-led soul ballad, intimate lead vocal, upright bass, brushed drums` + `Exclude: electric guitar, acoustic guitar, guitar solo`. "'Acoustic only' beats 'no electric'." [COMMUNITY, jackrighteous/songsmith 2026]
- **Item budget:** >5 exclusions degrades output ("sparse, thin outputs"); recommendations range 1–3 (start) to 5 (max). [COMMUNITY]
- What it handles well vs poorly [COMMUNITY, anecdotal]: good at binary/categorical elements (male vs female vocals, whole genres, distinct-timbre instruments); leaks on diffuse qualities (modern polish, autotune character, "pop sheen") and genre-implied rhythm.
- Instrumental tracks need **triple-layering**: `[Instrumental]` toggle/tag + Exclude "vocals, singing, lyrics, humming, vocalizations" + no lyrics — any single layer alone leaks vocals. [COMMUNITY]
- At least one user report says the field does nothing for instruments/vocal features (attribution unverifiable) — one voice, not consensus.

**Engine implication:** **Negative-to-positive inversion is confirmed as the correct core strategy.** The engine should (1) cap exclude output at ~5 comma terms, (2) require/auto-generate a dominant competing positive in the style field for every exclusion, (3) never emit "no X" phrasing into the style field, (4) never emit `-` prefixes.

## 6. Weirdness / Style Influence sliders

- [OFFICIAL, help.suno.com article 6141377] Custom-mode Creative Sliders, **Pro & Premier**: **Weirdness** ("Safe ↔ Chaos"; 50% = "normal" expected result), **Style Influence** ("Loose ↔ Strong" adherence to the style field text), **Audio Influence** (only with an audio upload). Same panel: Vocal Gender selector, Exclude field. UI shows no numbers — all percentages below are community convention. v5.5 adds a separate **Duration slider**; Studio has its own slider set. Free-tier access [UNVERIFIED, official positioning says paid].
- **Weirdness behavior** [COMMUNITY]: 0–30% simple/predictable/textbook; **40–60% widely cited sweet spot** ("for 90% of songs"); 60–80% intricate but style-drift risk ("a chorus that no longer feels like the same song"); 80–100% experimental chaos. A "~81% glitch-mode cliff" (fragmented/glitchy output, useful only for textures) is single-source (acetaggen) — [UNVERIFIED beyond one source]. Practitioner dissent exists ("keep it ≤50, sometimes 0").
- **Style Influence behavior** [COMMUNITY]: low = tags treated as hints, genre-adjacent drift; 40–70 = tags guide with interpretation; 70–100 = tags as hard constraints ("100 = strict specification"). Downside of very high: all candidates sound alike — lower it for variety. Rule: raise when Suno ignores the requested genre; lower for variety.
- **Rap/phonk recipes** [COMMUNITY, sunostyles/acetaggen — consensus but partially search-synthesized]:
  - **Genre-faithful:** Weirdness 10–25% + Style Influence 80–100%.
  - **Creative-but-listenable:** W 30–50 / SI 60–80.
  - **Experimental fusion (structured):** W 50–70 / SI 60–80 — keeps the fusion inside a lane.
  - No phonk-specific slider data found; apply the recipes above. [UNVERIFIED whether phonk wants higher weirdness]
- **Interactions** [COMMUNITY]: high W + high SI = "unusual but within genre constraints"; high W + low SI = chaos. Style Influence and Audio Influence compete — both at 100% yields incoherence (single source). ~55% Audio Influence preserves melody while re-rendering the arrangement (single source). Repeated rules: don't max everything at once; move one slider at a time; sliders redirect variation, they are not quality knobs.
- No documented mechanical change of the sliders between v4.5 and v5/v5.5. [COMMUNITY, absence of evidence]

**Engine implication:** Emit recommended slider settings per build type using the three community recipes above, expressed as ranges with the caveat that the UI is unnumbered. Fusion builds pair high-ish weirdness with mid-high style influence, never low SI.

## 7. Identity persistence: Personas → Voices, Covers, adjacent features

- **Personas** (Nov 2024, Pro/Premier): capture a source song's "essence" — lead vocal timbre, style/instrumentation, vibe — reusable across new generations. [OFFICIAL] suno.com/blog/personas. Community practice: build a Persona from your best track, then vary only genre/mood prompts; don't stack extra vocal descriptors on top (conflicting instructions). Limitation: does **not** guarantee the same voice every generation — "recalls essence, doesn't freeze parameters." [COMMUNITY 2025–26]
- **v5.5 reorganization (Mar 2026):** Create-menu "Personas" replaced by **Voices**; old feature persists as "Style Personas" within it. New: **Voices** (record/upload your own vocal, 15s–4min, identity-verified, private, Pro/Premier); **Custom Models** (train a personal v5.5 variant on ≥6 own tracks, up to 3 models, Pro/Premier); **My Taste** (all tiers). [OFFICIAL extracts] help.suno.com articles 11362305, 11362497. Voices/Custom Models reportedly lock vocal identity better than classic Personas. [COMMUNITY]
- **Covers**: re-renders a track (yours or uploaded) in a new style **keeping melody, lyrics, structure** — the re-skin primitive; credit-efficient for style exploration. [OFFICIAL] suno.com/blog/covers; help article 2872257.
- Adjacent workflow features [OFFICIAL blogs/help]: Song Editor (Replace Section / Extend / Crop, Pro/Premier), Remaster, Stems (up to 12 + MIDI export), upload own audio (≤8 min) with Add Vocals / Add Instrumentals, Suno Studio (Premier, Sept 2025), Hooks (in-platform short-video feature).
- **Credits** [OFFICIAL pricing extract + COMMUNITY]: 10 credits per generation → 2 songs. Free 50/day; Pro 2,500/mo; Premier 10,000/mo. Community: keepers typically take 3–5 generations.
- **API: no official public API** as of July 2026. Suno announced (July 1, 2026) it is *exploring* a partner API via intake form — no launch/pricing. All existing "Suno APIs" are unofficial third-party wrappers with ToS risk. [PRESS] musicbusinessworldwide.com. **Engine implication:** CIPHER stays a copy-paste-package generator; no API integration path exists yet.

## 8. Decisions this research locks in for the engine

1. **Model profiles**, not hard-coded versions: `v4.5-all` (free) and `v5/v5.5` (paid) profiles carrying field budgets and feature flags. Current budgets: style 1,000 / lyrics 5,000 / exclude 1,000 chars.
2. **Style emission format:** comma-joined descriptor phrases, genre-first ordering (genre → mood → groove/drums → instrumentation → vocal → production/BPM), ~15–30 words, 1–2 genres, 2–4 instruments, explicit 808/hi-hat/BPM language, no brackets, no artist names, no "no X" phrasing.
3. **Artist-DNA library** maps artists to: genre anchors, era/region descriptors, drum/808 character, vocal delivery, texture, BPM range — because artist names are blocked and region+era+production descriptors are the documented substitute.
4. **Structure templates** emit reliable-tier tags only; hook-first = `[Chorus]` first (optionally `[Intro: Cold Open]`); always end `[Outro]` → `[End]`; runtime targeted via line-count × BPM heuristics (~2:30–2:50 ≈ 2 verses + 2–3 choruses, no bridge); beat-switch variant via `[Build]`/`[Drop]`/`[Breakdown]` + style-field switch description.
5. **Lyric shaping is first-class:** parenthetical ad-libs ≤3 words, echo pattern, sparing ALL-CAPS/`!` with bleed-over warning, ellipses for laid-back pacing, syllable-density guidance per flow type.
6. **Exclude output:** ≤5 comma terms, each exclusion paired with an auto-injected dominant positive (negative-to-positive inversion), never minus-prefixed.
7. **Slider presets per build type:** faithful W10–25/SI80–100; balanced W30–50/SI60–80; fusion W50–70/SI60–80 (ranges, since the UI is unnumbered).
8. **Experimental flag** for: colon-form delivery cues, `[Tempo Change: X BPM]`, asterisk SFX, `[Hook]`, `[Fade Out]`-only endings.

## 9. Open questions for Phase 3 (validate against real audio)

- Do colon-form section cues (`[Verse 2: aggressive triplet flow]`) do anything on v5.5, vs adjective form (`[Aggressive Verse 2]`)?
- Does ALL CAPS reliably shout on v5.5?
- Does `[Tempo Change: X BPM]` produce usable beat switches, or is `[Build]`/`[Drop]` strictly better?
- Actual v5.5 max duration and the Duration slider's range.
- Does the exclude field help at all for diffuse qualities ("pop sheen"), or is crowding-out the only lever?
- Phonk-specific slider sweet spots (esp. whether fusion builds tolerate W >70).
- Whether v5.5 silently substitutes artist names (single-source claim) vs hard-blocking.
- Free-tier access to sliders and exclude field.

## 10. Source register

**Official Suno:** suno.com/blog (v5-5, introducing-v4-5, personas, covers, songeditor, suno-studio), suno.com/release-notes (+ /exclude-styles), suno.com/pricing, help.suno.com articles 2409473 (song length), 2415873 (custom lyrics), 3161921 (exclude), 3198209 (moderation), 5782721 (model timeline), 5782849 (v4.5 style instructions), 6141377 (creative sliders), 9010177 (music glossary), 3484161 (personas), 11362305/11362497/11362369 (v5.5), @SunoMusic on X (v4.5 launch; exclude launch Sept 2024). *(All via search extracts — direct fetch blocked.)*

**Community (2025–2026 unless noted):** github.com/daveshap/suno (v3-era, read in full); github.com/AlijeeWrites/suno-v55-prompt-guide + best-suno-ai-prompts (read in full; carries affiliate links); jackrighteous.com (trap guide, negative prompting, sliders, metatag A/B tests, build/drop, v5.5 guides); hookgenius.app (prompt guide 2026, character limits, metatags, vocal prompts, phonk, trap, wrong-genre, negative prompting); songsmith.studio (negative prompts, structure cheat sheet, song-length formula, endings, symbols, hook patterns); sunostyles.com (parameters); acetaggen.com (advanced parameters, vocal sheet); tagasong.com (tag library, syntax); openmusicprompt.com (phonk, emo trap, drill); sunoaiwiki.com (2024 genre/metatag lists); roo.beehiiv.com (v5/v5.5 conditioning); blakecrosley.com/guides/suno; neuralanalog.com; freesongwritingtools.com; devspyder.net; sunometatagcreator.com; howtopromptsuno.com; suno.wiki; usesuno.com; musci.io; undetectr.com; james-palm.medium.com; medium.com/@aitooldiscovery (r/SunoAI aggregate, Jul 2026); medium.com/@creativeaininja; orustech.substack.com; apipass.dev; suno.hk; titanxt.io; aisharenet.com; jgbeatslab.com; pricing roundups (margabagus, stacksheriff, techjacksolutions, costbench, sunnoai, gptprompts.ai); tiktok.com/@bruce_chamoff.

**Third-party API docs (unofficial):** docs.sunoapi.org, docs.kie.ai — best available proxy for field limits.

**Press:** musicbusinessworldwide.com (v5.5 launch, "Where's v6?", developer-API exploration Jul 2026); musically.com (v5 launch); tomsguide.com (Personas).

**Independent:** SSRN abstract 5998376 (Jan 2026, slider interaction framing; abstract-level only).

**Known reliability gaps:** no official character limits anywhere; front-loading is experiential consensus, not measured; Reddit unreachable (community claims mediated via SEO guide sites that cross-copy); single-source claims flagged inline (81% weirdness cliff, 55% audio-influence recipe, v5.5 silent artist substitution).
