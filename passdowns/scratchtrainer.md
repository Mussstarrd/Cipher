# Pass-down — ScratchTrainer build agent

**Written:** 2026-08-24 · **Session:** Claude Code on the web (remote container, Linux VM)
**Repo/branch:** `Mussstarrd/Cipher` · `claude/app-market-research-6eaezg`
**Audience:** the next agent (or human) picking this up cold.

---

## 1. What this agent was built to do

Started as an open-ended product-ideation partner: the owner asked for research
into app-store categories with high review volume and recurring complaints, to
find a marketable gap a two-person team (owner + AI) could fill.

It has since become the **build partner for one product**: ScratchTrainer, a
turntablism trainer for the Pioneer DDJ-REV1 controller. The agent's job is now
to write and test the code, run verification spikes, and push to the branch
above. The owner supplies the hands, the hardware, and the product direction.

Working split as of this writing: **this cloud session owns app code**; a
**local Claude Code session on the owner's Windows laptop** (installed
2026-08-24) owns machine-level work — Demucs install, sample-bank builds,
anything touching hardware or the local filesystem. Both write to the same
repo. **Collision risk on `app/index.html` is real — pull before editing.**

## 2. What it has built so far

**Research phase**
- Four parallel research sweeps (printing, small-business, organization apps,
  indie-market dynamics), synthesized into a ranked opportunity report,
  published as an Artifact. Family-organizer concept brief was written, then
  abandoned when the owner pivoted.

**ScratchTrainer — shipped to the branch**
- `prototypes/rev1-trainer/` — P0/P1 prototypes (Web MIDI proof, on-device
  BPM/beat/kick/snare analysis, Surface Learn mapping tool). Superseded but
  kept; **Surface Learn is now a dev tool**, per the owner's build brief.
- `spikes/` — Phase 0 verification harness plus **all four spike reports with
  real hardware results**. This is the most valuable directory in the repo;
  read it before making any architectural decision.
- `app/` — the current product. Scoring overlay: stroke engine, mandatory
  latency calibration, Baby Scratch and Chirp drills, top-down deck schematic
  UI, backing-beat engine (synthesized boom-bap/house + user-track loop with
  tempo detection), hardened `rev1-map.json`.
- `tools/build_sample_bank.py` — batch Demucs stemming into a practice bank.
  **Never run against real audio yet** (no GPU or model weights in this
  container). First live run is unproven.
- `docs/rev1-curriculum.md` — control inventory, three-level skill taxonomy,
  music-event → move mapping, drill design.

**Architecture, decided by evidence not preference:** Serato DJ Lite owns the
audio; ScratchTrainer reads the same MIDI stream in parallel and scores the
hands. No audio engine, no JUCE, no DVS.

## 3. Signals and sources it watches

- **The owner's field reports from real hardware.** The single highest-value
  signal. This agent has no eyes, ears, or USB access.
- **Spike reports** in `spikes/*.md` — measured hardware facts.
- **Headless browser tests** (Playwright + Chromium in-container) for JS
  errors, drill scoring, DSP accuracy, and screenshots of its own UI.
- **Web search** for external facts (Serato file formats, Demucs setup,
  Google API scope tiers, competitor landscape).
- No live feeds, no polling, no market data. Nothing streams in.

## 4. What it got right and wrong

**Right**
- **Spike-first discipline paid for itself twice.** Spike B proved Chrome can
  read the REV1 while Serato holds it (1,306 events/10s on Windows), which
  deleted the entire native-audio-engine phase from the roadmap. Spike C proved
  browser audio scrubbing is unusable (40 ms output-latency floor) *before*
  anyone spent a month building it.
- Spike A produced a usable hardware spec: center-64 encoding on ch1/CC34,
  720 ticks/rev, touch gate on note 54, 660 msg/s peak.
- Tempo analyser measured 0.17% error against a synthetic 96 BPM signal.
- Pushed back correctly on scope: when the owner asked for beatmatching, the
  agent flagged that his own build brief had explicitly cut it, explained why
  it could work anyway, and recommended sequencing it after the kill-criteria
  test rather than silently building it.

**Wrong — concrete cases**
- **The scoring race condition (worst one).** Strokes were scored only when
  they *ended*, but targets despawned 200 ms after their beat. A relaxed
  350 ms stroke finished after its target was already dead, so only frantic
  fast scratching registered. The owner found it on hardware; the agent's own
  tests had passed because its **simulated strokes were unrealistically fast**.
  Lesson: synthetic test parameters must span real human ranges, or tests
  launder the bug.
- **The chirp chart was badly designed.** Original: a lopsided
  hit/hit/hit/rest over two beats requiring the player to open the fader
  *while* pulling back. Owner: "not organic… choppy forced." Rewritten
  symmetric — both hands out on the beat, both back on the "and". The agent
  designed a rhythm it could not itself perform or hear.
- **Instructions written in glyph notation** ("Amber ◆ (filled): CUT the fader
  closed on the next half-beat…") were incomprehensible to the owner. Rewritten
  as a counted "ONE… and… TWO". Happened *twice* before the agent recognised
  that teaching the move is the actual product problem, not a copy problem.
- **Overstated the Google Calendar API risk** in the family-organizer brief —
  claimed a paid CASA security assessment was likely; verification showed
  Calendar is a *sensitive* scope (free review, ~3–10 business days), not a
  restricted one. Corrected only because the owner pushed back.
- **Assumed velocity would be encoded in MIDI value magnitude.** Spike A showed
  values stay within ±2 of centre even at full speed; velocity lives in
  *message rate*. Design corrected before implementation — the spike caught it,
  not the agent.

**Standing pattern:** this agent's failures cluster in the gap between
simulation and embodiment. Its code correctness record is decent; its
judgement about *what a human body and ear experience* is unreliable and needs
field verification every time.

## 5. Credentials and connections held (names only)

Per the request, **names only — no values, tokens, or secrets appear here, and
none should ever be written into this repo.**

- **GitHub** — write access to `Mussstarrd/Cipher` only, via the session's git
  credentials and the `github` MCP server. Scope was explicitly limited to that
  one repository at session start.
- **MCP servers connected to this session:** `github`, `Gmail`,
  `Google_Calendar`, `Google_Drive`, `Robinson_Trading`, `Claude_Code_Remote`.
  **Of these, only `github` has ever been used by this agent.** The others have
  been connected but untouched — no mail read or sent, no calendar or drive
  access, no trading calls of any kind.
- **Artifact publishing** to claude.ai (private by default). Three artifacts
  published: the market-opportunity report, the family-organizer brief, the
  ScratchTrainer concept brief.
- The owner's email address is known to the session for attribution and is
  deliberately **not** recorded in this file.
- No API keys, no service credentials, and no secrets are stored in the repo.

## 6. Open questions

1. **Does the reworked chirp feel right?** Not yet field-tested. Transform
   (Level 3) is designed to build on it — building Transform before this is
   answered risks compounding a bad foundation. **This is the top blocker.**
2. **Desktop-wrapper decision, awaiting the owner.** Packaging as Tauri/Electron
   would buy a translucent always-on-top overlay, auto-detection of the track
   Serato has loaded, and reading Serato's own beatgrids/cue points from the
   `_Serato_` folder (mature open-source parsers exist). The owner's brief
   requires flagging anything needing a native binary rather than building it —
   flagged, not built, no answer yet.
3. **Does `tools/build_sample_bank.py` work against real audio?** Logic is
   tested with stubs; a real Demucs run has never happened.
4. **Right-deck MIDI map is presumed, not measured** (`rev1-map.json` marks it
   `presumed`). Same for PLAY/CUE buttons, EQs, lever FX — needed before
   over-the-beat drills, which plan to use the physical PLAY press as the
   sync signal between Serato and the chart.
5. **Does gamified scoring teach transferable DJ skill, or just the game?** The
   owner's brief asks this and it remains unanswered. It is the product's
   central assumption.
6. **Kill criteria are unmet and untested:** 500 waitlist signups from three
   TikTok clips; 20 of 30 beta testers completing the chirp drill without a
   written explanation. Given that the owner — a motivated adult with the
   developer on call — could not parse the chirp instructions on first read,
   the second criterion should be considered at serious risk.
7. **Commissioned scratch sample pack** remains an unresolved dependency. The
   canonical battle-record sentences are copyrighted; "zero licensing cost" is
   only true if original samples are cut.
8. **Product naming.** "ScratchTrainer" is a placeholder. The owner's brief
   correctly rejected using the controller's model number as a product name.
