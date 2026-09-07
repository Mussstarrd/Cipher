# IRON SHAOLIN: RUNNING THE CHAMBERS
## Game Design Document — v0.2 (The Blueprint)

> "Lateral thinking with withered technology. The endless runner is the withered
> technology — a decade old, understood by every thumb on Earth. The lateral move
> is this: the level IS the music. Nobody has made the breakbeat itself the level
> designer."

**Genre:** Rhythm-infused lane runner — top-down, portrait (2.5D)
**Revision:** v0.2 — pivoted from side-on to overhead after playtest (see §6)
**Platforms:** iOS / Android
**Session length target:** 90 seconds – 4 minutes per run
**Rating target:** E10+ / PEGI 7 (stylized action, no blood, no licensed content)
**Legal note:** All lore, names, iconography, and music are original works. No
trademarks, samples, or likenesses from any real-world artist or group.

---

## 1. Title & High-Level Pitch (The Elevator Hook)

### Title: **IRON SHAOLIN: RUNNING THE CHAMBERS**
(Short form / app icon brand: **IRON SHAOLIN**)

### The Hook (one breath):
*"Temple Run had legs. Ours has FLOW. A one-thumb runner, held in portrait, where
the beat builds the world — every obstacle lands on the snare, every lane change
rides the bassline, and the better you run, the fatter the track gets. You don't
play a level. You play a record."*

### The Hook (thirty seconds):
You are a masked disciple of the Iron Shaolin Syndicate, running the rooftops and
subway veins of Gousetsu City to recover the **36 Stolen Wax Plates** — vinyl
pressings holding the Syndicate's lost teachings. The city itself is procedurally
assembled FROM the music: BPM sets run speed, the drum pattern places obstacles,
the bass drops open new districts. Chain perfect moves and the mix "builds" —
layers of the beat unmute one by one until you're sprinting inside a full,
knocking instrumental. Miss, and the track strips back to a naked kick drum.
**Your skill is the DJ. The audio state IS the score multiplier, and you can
HEAR your own mastery.** That's the viral clip: no two runs sound the same, and
a god-tier run sounds *incredible* on a phone speaker.

### Why the Runner over the Tactical RPG (the decision, on the record):
- **Withered tech, lateral twist.** Runners are a solved genre with universal
  onboarding (one thumb, three verbs). We spend zero tutorial budget teaching
  input and 100% of our innovation budget on the beat-built world. An RPG spends
  its whole budget teaching systems before the player feels cool.
- **Virality is audiovisual.** A 15-second clip of a beat-matched flow-state run
  is self-explanatory on any social feed. A turn-based menu screenshot is not.
- **Session shape fits mobile.** 2-minute runs match bus stops and bathroom
  breaks. Retention hooks (daily chambers, crew battles) bolt on cleanly.
- **Scope honesty.** A polished runner ships with a skunkworks team in months.
  A tactical RPG that *feels* premium is a 2+ year, content-treadmill commitment.
  The RPG becomes title #2, set in the same universe, once the brand knocks.

---

## 2. Core Gameplay Loop

### The 90-Second Loop (moment to moment)
The camera looks straight down. The disciple runs north across the rooftops in
three lanes; the world scrolls south. Three verbs, one thumb, phone held upright:
1. **Swipe left / right — STEP** (change lane; the core verb, and the one that
   makes the beat physical — you are placing yourself on the grid)
2. **Swipe up or tap — VAULT** (leap the gaps where the roof ends)
3. **Press & hold — FLOW STANCE** (breath control; phase through drone swarms at
   full tempo, drains the Flow meter)

The lateral mechanic — **THE BUILD**:
- The instrumental starts stripped: kick drum only.
- Every obstacle sits on a beat-grid slot. Clearing it **on the beat window**
  ("On Time") banks Flow; clearing off-beat still survives but banks nothing.
- Flow milestones **unmute stems** in order: kick → snare/hat → bassline →
  sample chops → lead melody → full mix ("**GOD FLOW**": screen chromatics
  bloom, score x8, crowd sounds join the mix).
- Taking a hit doesn't kill you — it **mutes a stem** (you *hear* the mistake).
  Getting hit on a naked kick drum ends the run. Death has a sound design: the
  record slows to a stop like a hand on the platter.
- This makes difficulty self-balancing: strong players live in dense, fast,
  full-mix audio; weak players get a sparse, readable, forgiving track.

### The Session Loop (why they open the app daily)
1. **Daily Chamber** — one handcrafted, fixed-seed run per day (same for the
   whole world, like a Wordle seed). One leaderboard. Bragging rights reset at
   midnight local.
2. **Wax Hunt** — each run scatters 3 **Wax Fragments** along risky lines;
   collect fragments across runs to press full **Wax Plates** (the collectible
   spine of the game: 36 Plates = 36 original instrumentals + lore scrolls).
3. **Crew Ciphers (async multiplayer)** — your crew of up to 9 pools weekly
   distance/style points against rival crews. No live netcode; pure async
   leaderboard warfare. Crews are the retention anchor.
4. **The Scroll** — a single daily login ritual: one panel of lore, one small
   gift, delivered by your Abbot. 10 seconds, skippable, never a chore-wall.

### The Meta Loop (weeks/months)
- **Disciples:** unlockable runners, each with one signature passive that changes
  the *musical* run, not the odds — e.g. **Old Dirty Sifu** turns the build
  stems into grimy detuned versions; **Lady Nine Bells** adds a melodic
  counter-line when she's in God Flow; **The Chessmonk** shows the next 4 beats
  of obstacles as ghost notes. Cosmetic + audio identity, minimal stat creep.
- **Styles (cosmetics):** masks, jackets, sneaker trails, spray-tag victory
  animations, and **turntable skins** for the results screen.
- **The 36 Chambers Trials:** handcrafted challenge runs unlocked by Plates —
  finite, authored content that teaches advanced tech (lane-juggle chains,
  double-step scoring). Completing a trial row unlocks a Disciple.

---

## 3. The Fictional Universe & Aesthetic

### The Lore: The Iron Shaolin Syndicate
Gousetsu City, 199X-forever — a rain-slick megacity where an ancient monastic
order went underground a century ago and re-founded itself in the boiler rooms
and record shops: **The Iron Shaolin Syndicate**, nine houses ("Chambers") each
guarding one discipline — breath, rhythm, ink, steel, silence, echo, hunger,
mercy, and dust. Their scripture was pressed into 36 wax plates so it could
never be burned — only *played*. A corporate ghost-clan, **The Hollow Suit**,
has stolen the plates to silence the city's pulse and replace it with muzak.
You run to take the music back.

Tone: reverent and grimy at once. Zen koans spray-painted on water towers.
An abbot who quotes chess and dice games in the same breath. Humor is deadpan,
never parody. Nothing references real artists, groups, logos, or lyrics —
the *vibe* is 90s underground: staticky kung-fu-cinema philosophy, crate-digger
mysticism, boombox-as-reliquary.

### Visual Direction
- **Style:** 2.5D — hand-inked 2D characters (thick brush lines, limited-frame
  "kung-fu cel" animation with smear frames) over stylized low-poly 3D
  environments. Reads instantly at speed, cheap to render, ages beautifully.
- **Palette:** *Concrete & Neon over Ash.* Base world in charcoal, wet asphalt
  blue-grays, and rust. Accents in **iron gold**, **jade neon green**, and
  **blood-orange sodium lamplight**. God Flow washes the world in gold-on-black
  ink. UI in aged-paper cream with chop-stamp red seals.
- **Signature imagery:** rooftop dojos with sagging power lines, subway cars as
  moving platforms, pigeon flocks that scatter on the snare, giant hand-painted
  movie billboards for fake kung-fu films, vinyl crates glowing like shrines,
  calligraphy smoke, chain-link fences backlit by neon hanzi-esque glyphs
  (original invented script — no real language, no real logos).

### Audio Direction (the co-star)
- **Original score only.** Commission 36 instrumentals from underground
  producers (work-for-hire, stems delivered): dusty boom-bap 84–96 BPM, heavy
  low end, chopped pentatonic/erhu-flavored samples *recorded in-house* (no
  clearance risk), kung-fu-flick foley (blade shings, wooden dummy knocks),
  turntable scratches as UI sounds.
- **Every sound is quantized.** Menu taps land on the grid of the ambient menu
  loop. The pause screen is a record on a slipmat — pausing scratches, resuming
  needle-drops. Coin pickups are pitched to the track's key.
- **Stem architecture is the tech spine:** every track ships as 5–6 aligned
  stems + a beat-map file (authored in-house or auto-extracted, then hand-QA'd).

---

## 4. Monetization & Retention — "The Honor System"

Philosophy: *an underground crew never charges you to enter the cipher — but
you pay respect for craftsmanship.* Translation: **never sell power, never
gate the run, never interrupt with unrequested ads.** We sell identity, music,
and patience-skips — and we make the free player feel like a valued head-nod
member, because free players are the content (crews, leaderboards, clips).

### Revenue pillars (in priority order)
1. **The Wax Pass (season pass, ~$7.99/quarter, aligned to a "Season = an
   Album"):** each season is a new 9-track "album" of runs, cosmetics, a new
   Disciple at tier 30, and lore. Free track runs parallel with real value.
   Seasons-as-albums is the brand flex: players talk about seasons like drops.
2. **Direct cosmetic shop:** masks, fits, trails, tag animations, turntable
   skins, and **stem skins** (run the game's music through "cassette,"
   "35mm grindhouse," or "basement tape" mix filters). Rotating stock,
   transparent prices, **no loot boxes, no gacha.** Ever. That's the honor code
   and the App Store featuring pitch in one move.
3. **Rewarded ads only, diegetic:** watching an ad is "crate digging" — 
   optional, capped at 3/day, pays Wax Fragments or a run-revive. Never
   interstitials, never forced.
4. **One-time "Iron Membership" (~$19.99, permanent):** removes even rewarded-ad
   prompts (converts them to free digs 1/day), +1 daily Chamber attempt, gold
   name-plate chop in crews. The whale-respectful ceiling without power creep.

### Retention machinery
- **D1:** first run ends at a scripted near-God-Flow moment — the player *hears*
  what mastery sounds like before they can earn it. Unfinished business.
- **D7:** crew invite unlocks at player level 5; crew weekly war resolves
  Sundays — an appointment. Daily Chamber streak grants escalating (cosmetic)
  streak chops.
- **D30:** Plate collection (36 long-term goals), Trials skill ladder, and
  season-album cadence. Skill expression depth (beat-juggle scoring, stance
  cancels) gives creators a tech ceiling worth making videos about.
- **Anti-churn honor rule:** streaks *pause* instead of breaking for up to 3
  days ("the temple holds your place"). Lapsed players return to a gift, not
  a guilt screen.

### KPIs we design toward (not vanity)
- D1 ≥ 45%, D7 ≥ 18%, D30 ≥ 8% (genre-beating, justified by daily seed + crews)
- Average session ≥ 3 runs; share-clip rate ≥ 2% of God Flow runs
- ARPDAU target $0.06–0.10 without a single pay-to-win lever

---

## 5. Technical Stack Recommendation

### Verdict: **Unity (LTS) + C#** — and it isn't close.

| Need | Unity | Flutter | React Native |
|---|---|---|---|
| Sample-accurate audio scheduling | ✅ `AudioSettings.dspTime`, custom DSP, native plugins | ⚠️ plugin-dependent, GC jitter risk | ❌ JS bridge latency kills beat-sync |
| 2.5D at locked 60fps on mid-tier Android | ✅ URP, SRP batching, proven | ⚠️ possible (Flame/Impeller) but unproven at this scope | ❌ not a game runtime |
| Shader-driven style (ink, God Flow bloom) | ✅ Shader Graph | ⚠️ limited | ❌ |
| Hiring pool / battle-tested mobile game ops | ✅ enormous | small | wrong tool |

Flutter and React Native are honorable app frameworks — and the wrong dojo.
A beat-matching game lives or dies on **audio latency and frame pacing**, which
means we need a real game engine with a DSP clock. Withered technology doctrine
applies: Unity LTS is boring, proven, and everywhere. We spend novelty on the
game, not the engine.

### Architecture spine
- **Engine:** Unity 6 LTS, URP, 2D animation package + 3D environment meshes.
- **The Conductor (core system):** a beat-clock service built on
  `AudioSettings.dspTime` — the single source of truth. Obstacle spawner,
  animation events, VFX, input-grading windows (±90ms "On Time", ±45ms
  "Perfect") all subscribe to the Conductor. Gameplay is authored in *beats*,
  not seconds; world speed derives from BPM.
- **Stem playback:** all stems start simultaneously, muted/unmuted via mixer
  snapshots (crossfade ≤ 1 beat) — never start/stop, so phase alignment is free.
- **Track data:** per-track JSON beat-map (BPM, time signature, stem manifest,
  obstacle-pattern lanes, drop markers). Authored via an in-house editor tool;
  this is also how we'll ship seasonal content without app updates
  (Addressables + CDN).
- **Procedural chunking:** hand-authored "phrase chunks" (4/8-bar obstacle
  patterns tagged by intensity) stitched procedurally to match the track's
  energy curve. Handcrafted feel, procedural variety.
- **Backend:** thin. Firebase (or Nakama if we want self-hosted) for auth,
  cloud save, leaderboards, crew async wars, remote config, A/B. No live
  netcode anywhere — async by design.
- **Performance budget:** 60fps on a 2019 mid-tier Android (Snapdragon 665
  class), < 250MB initial download (base + 6 tracks; rest streamed), cold
  start < 4s, audio output latency path via Android `AAudio`/low-latency flag.
- **Analytics/attribution:** privacy-lean event schema day one (run funnel,
  build-state at death, shop views) — we tune the groove with data, not vibes.

### Production pipeline (skunkworks order of operations)
1. **The Conductor + movement kernel** — beat-clock, three verbs, input
   grading, one gray-box street. *If this doesn't groove, nothing else matters.*
2. **Stem build system** — mute/unmute mixing tied to Flow meter, one real
   licensed-for-dev track cut into stems.
3. Chunk spawner + beat-map format + editor tool.
4. Art target slice: one block of Gousetsu City, one Disciple, God Flow VFX.
5. Meta shell: daily seed, Wax fragments, results screen, share clip export.
6. Crews, Wax Pass, shop, LiveOps plumbing. Soft launch (PH/CA/NZ). Tune. Ship.

---

*Document status: v0.1 blueprint for team review. Next revision after the
playable groove-kernel proves the core feel.*

---

## 6. Revision log

### v0.2 — the overhead pivot
Playtested the v0.1 groove kernel with kids. Two notes, both acted on:

1. **"It should be top-down, north-south, not side-on."** They are right, and the
   reasons are commercial as much as aesthetic. Portrait is the dominant mobile
   posture and the only genuinely one-handed one; a vertical field uses the whole
   phone instead of letterboxing a widescreen strip into the middle of it; and
   rooftops seen from above is a more distinctive look than another side-scroller
   silhouette. The pivot also *improved* the core mechanic: lane-stepping is a
   discrete three-state choice landed on a beat, which is a truer rhythm input
   than a binary jump/slide. Everything underneath — the Conductor, the stem
   build, the chamber structure, the grading windows — carried across untouched.

2. **"It needs to open full screen."** The game now enters an immersive mode the
   moment a run starts: the chrome folds away, the field takes the whole viewport,
   and it requests real fullscreen where the host permits it. There is also a
   manual toggle in the corner of the stage.

**Consequences to carry into Unity:** the play field is a fixed portrait rectangle
letterboxed into whatever screen it gets, so three lanes mean the same thing on
every device. Do not stretch it to fit — a lane must be a lane everywhere, or the
grading windows stop meaning anything across the install base.
