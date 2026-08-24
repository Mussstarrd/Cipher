# Pass-down: Pocket Runner agent (Cipher repo)

Shift-handoff document for the agent session working branch
`claude/pocket-runner-poc-074t4l` on `Mussstarrd/Cipher`. Written
2026-08-10 at the request of the repo owner (Jeffery), relayed via his
trading-MCP session. Honest account; the embarrassing parts are kept in.

## 1. What this agent was built to do

The original brief was a proof-of-concept **rhythm-based auto-run 2D
platformer** for Android (Godot 4.x, GDScript, landscape, sideloadable),
with hard architecture rules: all gameplay timing derived from audio
playback position (never frame delta), unmute-based hit feedback instead
of reactive tap sounds, beat-locked jump arcs, JSON chart levels, WAV-only
audio, calibration screen, debug overlay, and an in-game chart author.

The project has since **pivoted twice at the owner's direction** and the
current product is a **street-dance rhythm game**: a goofy dancer faces
the camera; tap cues scroll along a neon LED sidewalk ticker into a hit
marker (right thumb) while "fuse" shapes burn on the left side of the
screen and must be ridden with a finger (left thumb). Combos escalate
dance tiers (bounce → twitch → Harlem shake → zombie → breakdance →
cartwheels), draw a crowd (up to 26, two rows), and get money thrown.
Off-beat taps, missed cues, and abandoned fuses trip the dancer and reset
the combo. The platformer (`runner.gd`) and chart author (`author.gd`)
still exist in-repo but are off the menu.

The timing architecture from the original brief survived both pivots
intact and is the project's core asset.

## 2. What has been built so far

Twelve shipped builds (v1–v12), each installed and play-tested on the
owner's Samsung Fold (Android 16). Current state:

- **Timing core**: `autoload/song_clock.gd` (smoothed audio-position
  clock, swing-aware grid math), `autoload/stems.gd` (sync-started stem
  players, unmute feedback, silent-placeholder→click-track fallback),
  calibration screen (8 taps, median with outlier rejection), adjustable
  A/V visual offset (debug overlay VIS ±10 ms buttons, persisted).
- **Dance mode** (`scripts/dance.gd` + `dancer.gd`, `spectator.gd`,
  `money_bill.gd`): cue judgment with two-tier windows clamped so
  adjacent windows never overlap; multitouch input (raw per-index
  touches; emulated-mouse events filtered to prevent double-fire); screen
  split at 40% into trace zone / tap zone; burning-fuse traces with seven
  waypoint shapes; procedural everything (no art assets).
- **Three songs, three backdrops**, selectable from the menu:
  - *Dragon's Breath* — 85.04 BPM (measured; label said 85) — alley.
  - *Syrup Erhu* — 89.04 BPM (detected, unlabeled) — Chinatown lanterns.
  - *Mall Show* — 71.03 BPM (detected) — 8 Mile mall stage with velvet
    rope; the 11th spectator is the mall Santa (owner's spec, verified
    unique and mall-only).
- **Audio analysis pipeline** (`scratchpad`, not committed): STFT onset
  detection, tempo sweep with half/double disambiguation, split-half
  drift verification, bar anchoring via kick band, tap cues snapped to
  actual hit transients (per-cue `nudge_ms`), trace placement in gaps,
  synthesized drum/hat stems on the measured grid. Validated against
  synthetic ground truth: −0.5 ms mean bias, ±3.4 ms sd.
- **Headless test suites** (scratchpad scripts, rerun before every
  ship): tap routing, author-tool UI, cue judgment, fuse geometry and
  coverage, per-song load/backdrop/tier/crowd/Santa. All green at v12.
- **Delivery**: non-gradle "Android Sideload" release-template export
  preset, built headless in this cloud container with Godot 4.5.2 and
  signed with a locally generated debug keystore. APKs ≤30 MiB were
  attached in chat; v12 (34 MiB, three songs) ships as
  `releases/pocket-runner-v12.apk` committed in-repo.

## 3. What signals or sources this agent watches

This is not a monitoring agent. It has **no PR subscriptions, no cron
jobs, no scheduled wakeups, no market or external data feeds**. Its only
inputs are:

- The owner's chat messages (bug reports from on-device play-testing are
  the primary quality signal — several bugs were only findable there).
- Audio files the owner uploads (`/root/.claude/uploads/...`).
- Its own headless Godot test runs and build output.

The in-game debug overlay (clock jitter, output latency, judgment deltas)
exists so the *owner* can report device-side signals back; see open
questions.

## 4. What it has gotten right and wrong

**Right (concrete):**

- Diagnosed the v1 "App not installed" failure as **4 KB-only ELF page
  alignment** in Godot 4.4.1 templates vs the Fold's 16 KB-page Android
  16 kernel (verified via `readelf`: `0x1000` LOAD alignment → rebuilt on
  4.5.2 → `0x4000`). Installed first try after the fix.
- Found the "taps never register" bug: full-screen Controls
  (calibration root, background ColorRects) consuming events in the GUI
  phase before `_unhandled_input`. Fixed with `mouse_filter = IGNORE` and
  locked in with synthetic-tap regression tests.
- Validated the beat-detection pipeline against synthesized ground truth
  before trusting it, then caught that the real track is humanized
  (hits drift ±45 ms off-grid) and snapped cues to actual transients.
- Correctly guessed from the owner's description ("all it did is place
  dots... couldn't exit") that the author-tool buttons were merely too
  small for touch, not broken — verified by test, fixed with 84 px
  buttons plus a dead zone.
- Kept every gameplay-critical time derivation on the audio clock through
  two full gameplay pivots; the timing core needed zero rework.

**Wrong (concrete, and what it cost):**

- Shipped a **runtime-generated looping AudioStreamWAV** for the
  calibration metronome; it crashed the app natively at the first loop
  wrap on device (v2). Replaced with a long linear click track. Lesson
  applied since: avoid loop points in runtime-built streams.
- Shipped the trace mechanic rendered via a **draw-signal-connected
  helper node** that never rendered on device (v9, "no shape ever
  appeared") — precisely the code path headless tests cannot exercise.
  Moved to the proven `_draw()` pipeline. Lesson: anything that must
  render goes through a path already observed working on device.
- **Misread truncated `aapt` output** and confidently announced a wrong
  root cause ("targetSdk missing") for the first install failure before
  finding the real one (page alignment). The wrong fix shipped in between.
- Wrote synthetic-input tests in the wrong coordinate space twice (the
  headless window is 64×64 with a ~20× stretch scale), producing false
  "buttons broken" failures that burned an investigation cycle — and,
  worse, a false PASS earlier ("timeline tap places event" passed only
  because placement ignored position). The dead-zone check it validated
  read `get_viewport().get_mouse_position()`, which taps don't reliably
  update on Android — caught and switched to event positions.
- Under-weighted perceived-pulse vs notated tempo: the first beat ring
  pulsed on quarter notes of a half-time track and read as "not on beat"
  to the owner. The cue-based redesign came from his feedback, not from
  this agent's initiative.
- Initial calibration demanded 16 taps (~15 s); owner found it
  unacceptable. 8 taps now. Small thing, real friction.
- v12 dropped instrumentals to mono for size without asking first; owner
  has not yet objected, but it was a unilateral quality tradeoff.

## 5. Credentials and connections held (named only — no values)

- **GitHub push access** to `Mussstarrd/Cipher` (the only repo in scope),
  held by the Claude Code Remote session; used for all commits/pushes to
  `claude/pocket-runner-poc-074t4l`.
- **GitHub MCP toolset** (read/write within the scoped repo).
- **Anthropic-managed outbound HTTPS proxy** (used to download Godot
  binaries/templates from github.com and packages via apt/pip).
- **Local throwaway Android debug keystore** (`/root/debug.keystore`,
  generated in-session, standard debug-signing conventions; not a secret
  of value — but a *consistent* one: installs upgrade in place only while
  builds are signed with this container's keystore; a new container means
  a new keystore and the owner must uninstall/reinstall once).
- **Owner's upload directory** (audio files he sends in chat).
- Explicitly **not** held: Play Store/console access, any release
  keystore of record, Robinhood or any trading access (despite this
  request arriving via that session), analytics, ad networks.

## 6. Open questions

1. **The A/V offset number.** The owner was asked twice for the VIS ±
   value his Fold settles on (it quantifies his device's audio-latency
   misreport). Never reported. Without it, visual sync ships on defaults.
2. **Fuse difficulty feel** — is 60% coverage / 95 px flame radius fair
   on the zigzag and Z at speed? Tunables are one-liners in `dance.gd`.
3. **Swing.** All three charts judge on a straight grid
   (`swing_percent: 50`). The engine supports swung off-16ths; nobody has
   measured whether any of the tracks actually swing.
4. **Mono audio** — acceptable permanently, or restore stereo once
   delivery moves off the 30 MiB chat limit (e.g., proper GitHub
   Releases, or Play internal testing)?
5. **Distribution.** Sideloading + Play Protect friction every install is
   wearing. Real answer is a release keystore and Play internal testing;
   needs the owner's decision (and a Play account).
6. **Legacy platformer** (`runner.gd`, `author.gd`) — revive, adapt the
   author tool for dance charts (currently only edits the old
   `user://charts/demo.chart.json`, which dance mode never reads), or
   delete?
7. **Monetization.** The owner's friend asked about "pay-per-tap"; the
   agent flagged gambling-adjacent policy risk and steered toward
   ads/IAP. No monetization exists in this repo, and none should be added
   without the owner's explicit design.
8. **Unverified-by-machine visuals.** Backdrops, dancer animations, and
   fuse rendering are only verifiable on-device; the owner's play reports
   are the test suite of record for anything visual. Keep it that way
   until there's a rendering-capable CI.
