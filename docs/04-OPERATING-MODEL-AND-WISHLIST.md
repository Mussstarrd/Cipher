# Part 4 — Operating Model & Resource Wish Lists

*How a one-human + Claude studio actually runs, and what to buy at each budget level.*

---

## 1. The operating model

### Division of labor

| Role | Who | What it means in practice |
|---|---|---|
| Vision, taste, veto | **You** | Play builds, react honestly ("gun feels mushy", "wave 4 is boring"), approve/veto ADRs. ~30–60 min/day when a build exists |
| Engineering (all of it) | **Claude** | Sim, gameplay, rendering integration, tooling, CI, backend. Parallel agent sessions per workstream once the Unity project exists |
| Art direction | **You** (verdicts) + **Claude** (generation, integration) | Claude drives gen-3D/concept pipelines and integration; you pick from contact sheets |
| Production | **Claude** proposes, **you** dispose | Status updates at every push; decisions surfaced as ADRs, never buried |

### The cadence

1. Claude works in branches; **every push compiles + tests in CI** (no green, no merge).
2. You get a **playable build** at every milestone beat (later: nightly APK to your phone via Firebase App Distribution / sideload, PC build via Steam private branch or direct download).
3. Your feedback goes into `docs/feedback/` verbatim (voice-note transcripts welcome) — Claude triages into issues and acts.
4. Big/irreversible calls become ADRs in `docs/decisions/` — you approve or veto asynchronously.

### The learning loop ("loop it into itself")

- **`CLAUDE.md`** is the studio's working memory — every Claude session reads it first and must keep it current. Conventions, gotchas, and hard-won facts go there, not into chat history that evaporates.
- **ADRs** capture *why*, so decisions are never re-litigated from scratch — only re-opened when their stated reversal conditions trigger.
- **Milestone retros** in `docs/retros/`: what slowed us down, what got automated in response. Rule: any failure that costs two fix-cycles gets a permanent guard (test, lint rule, CI check) — the pipeline ratchets, it never relies on remembering.
- **Telemetry from the first playable** (deaths, builds placed, wave losses, frame spikes) feeds balance decisions with data instead of vibes.
- **Perf gates in CI** hold the frame budget continuously, so quality compounds instead of eroding.

---

## 2. Wish lists — four tiers

Each tier includes everything below it. Prices are 2026 ballparks.

### Tier 0 — ABSOLUTELY NECESSARY (~$0/mo + ~$70 one-time)

The game genuinely gets built with this.

| Item | Cost | Why |
|---|---|---|
| Unity Personal | **Free** (revenue < $200k/yr) | The engine |
| GitHub Free (this repo) | **Free** | Source, CI (2,000 Actions min/mo), issues |
| Your existing Claude subscription | already paying | The engineering department |
| A laptop/PC that runs Unity Editor | you have it | Where you playtest; also a free self-hosted CI runner |
| Your Android phone + USB cable | you have it | Primary test device |
| Xbox controller (wired or BT) | ~$45 one-time | The design-center input; pairs to both PC and Android |
| Google Play dev account | $25 one-time (defer until shipping) | Store access |

### Tier 1 — LOW CASE (~$30–60/mo)

Removes the first friction points.

| Item | Cost | Why |
|---|---|---|
| Git LFS data pack | $5/mo | Binary assets (textures, audio) without pain |
| Meshy **or** Tripo starter plan | ~$20–30/mo | Gen-3D drafts: props, clutter, blockout enemies |
| Hetzner CX32-class VM | ~€7/mo | Always-on: build artifact host, later telemetry (Postgres) |
| GitHub Actions extra minutes | ~$10/mo as needed | Unity CI builds are minute-hungry; alternative: your PC as a free self-hosted runner |

### Tier 2 — MID CASE (~$200–400/mo + ~$500 one-time) ← *recommended once the loop is proven fun*

Buys throughput and real device coverage.

| Item | Cost | Why |
|---|---|---|
| Claude Max tier | $100–200/mo | Sustained multi-session agentic throughput — the single highest-leverage line item |
| Hetzner AX41/CCX dedicated box | ~€50/mo | Fast self-hosted CI: Unity license container builds, Android IL2CPP compiles, cache; kills the Actions-minutes bill |
| 2 extra test phones (one mid-tier Samsung, one budget/older device) | ~$400–500 one-time | Android perf floor is real hardware, not the editor profiler |
| Firebase (App Distribution + Test Lab free tiers, Crashlytics) | ~Free–$20/mo | Nightly APKs to your phone; crash reporting |
| Unity Asset Store budget | ~$50/mo avg | Buy solved problems (Feel/DOTween-class juice tools, audio packs) instead of building them |
| ElevenLabs starter | ~$5–22/mo | Placeholder VO barks with actual attitude |

### Tier 3 — BEST CASE (~$800–2,000/mo, phased)

This is "money buys time and polish," activated in stages — not all on day one.

| Item | Cost | When |
|---|---|---|
| Claude API budget for autonomous overnight pipelines (asset triage, balance sims, test generation) | $100–400/mo | Once pipelines exist to feed |
| Contract character artist (retainer/episodic) | $1–3k/mo equivalent | Vertical-slice phase — hero + 3 enemy archetypes at shipping quality |
| Contract audio designer (episodic) | $500–1.5k per drop | After first playable |
| Rodin/CSM higher tiers + Substance 3D | ~$50–80/mo | Art production phase |
| Steam App credit | $100 one-time | When the store page goes up (do this early for wishlists) |
| High-end Android device (latest Snapdragon) | ~$800 one-time | Ceiling tuning |
| Backblaze B2/Cloudflare R2 artifact storage | ~$5–15/mo | Build retention at volume |

### What I explicitly do NOT want you buying

Unity Pro (not until revenue forces it), any "AI game generator" subscription, Perforce hosting (Unity DevOps/Git LFS covers us at this team size), AWS anything (Hetzner/Firebase free tiers win at our scale), and paid analytics suites (self-hosted Postgres + a dashboard I'll build is enough).

---

## 3. Sign-up order (when you're ready)

1. **Now:** nothing — Tier 0 is already in hand minus the controller.
2. **When the Unity project lands in the repo (days):** Git LFS pack; decide Meshy vs Tripo (I'll run a bake-off with their free credits and recommend).
3. **When the graybox is fun (weeks):** Tier 2 — Claude Max + the Hetzner CI box are the two that multiply speed.
4. **When we cut the vertical slice (months):** Tier 3 art/audio contractors + Steam page.
