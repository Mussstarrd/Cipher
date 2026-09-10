> **⚠️ PARTIALLY SUPERSEDED (2026-09-10):** the engine decision and all UE5-specific machinery below (Mass Entity, Niagara, Nanite/Lumen, VAT-in-UE5, EOS, Perforce/team-scale infra) are superseded by **[ADR-001](decisions/ADR-001-engine-unity6.md)** — the engine is **Unity 6**, and docs/04–05 define the actual (solo-owner + AI) operating model. Still valid here: the AI-tooling assessment (§2), the architectural principles (flow fields, instanced crowds, snapshot netcode, CI perf gates), and the reasoning trail. Do not implement UE5-specific guidance from this doc.

# Part 1 — The Modern Game Dev Pipeline Audit

*CTO assessment. Written to be acted on, not admired. Every recommendation here assumes a small senior team (3–8 people), a hybrid action/tower-defense game with extreme enemy density, and a mandate to ship fast without enterprise bloat.*

---

## 1. Engine Selection

### The verdict up front

**Unreal Engine 5 (5.5+), with a data-oriented enemy layer and strict discipline about which UE5 marquee features we actually use.** Unity 6 + ECS is the runner-up and only wins if we staff C#-heavy. Godot is out for this title.

### Why the decision is harder than the marketing suggests

Our game has two conflicting technical identities:

1. **A third-person character-action game** — where UE5 is the undisputed best-in-class (animation tooling, camera, gunplay feel, Niagara, Metahuman-adjacent fidelity).
2. **A 1,000+ agent swarm simulator** — where naive use of *any* engine dies, and where Unity's DOTS/ECS is architecturally the most honest fit.

The mistake teams make is picking the engine for identity #1 and then implementing identity #2 the idiomatic way (one Actor/GameObject + skeletal mesh + behavior tree per enemy). That's a 150-enemy ceiling in either engine. **The swarm is a rendering + simulation problem, not a gameplay-object problem**, and once you accept that, the engine choice tilts decisively toward UE5 because the swarm layer is custom either way, and everything *around* it (combat feel, VFX, lighting, environment fidelity) is where UE5 pays us back daily.

### UE5 — what we use and what we ban

**Use:**

- **Niagara (GPU sims)** — this is the single biggest reason we're on UE5. Screen-clearing thermite barrages, gib fountains, smoke columns, tracer storms. GPU particles with collision against scene depth get us 100k+ particles for the "dopamine" moments. Non-negotiable for the fantasy we're selling.
- **Mass Entity framework** for the horde. Mass is Epic's ECS: enemies are entity fragments (position, velocity, health, archetype), not Actors. It shipped in production in Epic's own crowd tech and is stable enough in 5.5/5.6 for our use. We only "promote" an entity to a lightweight Actor proxy when it needs individual interaction (grabbed by a trap, executed by the player up close).
- **Vertex Animation Textures (AnimToTexture plugin) + instanced static meshes** for horde rendering. Bones are for heroes and elites; the other 900 enemies are VAT-driven instances. One draw call per archetype per LOD. This is the industry-standard trick behind every "how are there 5,000 zombies on screen" game (*World War Z*, *They Are Billions*-scale renderers) and it's well-supported in UE5.
- **Nanite for environments only.** Dense urban wreckage, rubble, interiors — Nanite eats it and frees us from LOD authoring on static geometry. Nanite is **banned for enemies** (skinned/WPO paths carry costs and our VAT instancing path is faster for identical meshes anyway).
- **Lumen in software mode as default**, hardware RT as an ultra setting. Gritty urban night scenes with neon, fires, and muzzle flash lighting are exactly Lumen's showcase. But we lock a performance budget: if Lumen costs us the 60fps floor during max-density waves, we ship baked + distance-field AO on lower tiers without apology.
- **Chaos destruction, pre-fractured and budgeted.** Barricades crumbling under a bloater is core fantasy. Runtime dynamic fracture of arbitrary geometry is not; everything destructible is authored as a geometry collection with capped simulation islands.

**Ban / defer:**

- Per-enemy behavior trees (see AI section — swarms don't think, they flow).
- World Partition streaming complexity — our maps are arena-scale, one persistent level each.
- Blueprint for anything in the per-frame swarm path. Blueprint is for designers wiring content (wave tables, upgrade definitions, UI); the simulation core is C++.
- MetaHumans. Gorgeous, wrong pipeline weight for us. Stylized-realistic custom characters instead.

**Cost note:** 5% royalty after $1M gross per product. That's a good problem to have; ignore it in the decision.

### Unity 6 — the honest runner-up

Unity's ECS/DOTS is the *architecturally correct* answer to "simulate 5,000 agents": Burst-compiled jobs, cache-coherent data, and it's genuinely production-grade now. If our founding team were Unity veterans, I'd bless it. But:

- The fidelity target (dark, wet, neon-lit, volumetric urban decay) means HDRP, and HDRP's out-of-box results require significantly more graphics engineering to hit the "AAA sheen" UE5 gives you by default.
- VFX Graph is capable but Niagara is a tier above for the choreographed, gameplay-coupled explosions we need (Niagara talks to gameplay data natively).
- The hybrid "GameObject hero world + ECS swarm world" boundary in Unity is a constant tax; in UE5, Mass↔Actor interop is one framework designed by one vendor.
- Unity's trust deficit post-2023 pricing chaos is mostly repaired (runtime fee cancelled), but Epic's incentives are simply better aligned with premium PC/console titles.

### Godot 4 — respect, but no

Godot 4.4 is a real engine now, and for a 2D or stylized mid-density game I'd consider it. For us it fails on three hard requirements: no answer at Nanite/Lumen fidelity tier, GPU particle and rendering perf ceilings well below Niagara at our density, and console porting is still a third-party service arrangement rather than a first-class path. The moment we say "high graphical fidelity" and "1,000 enemies," Godot is disqualified without malice.

---

## 2. AI & Generative Tools — Reality vs. Gimmick

### 2.1 Text-to-game / "automated game dev" generators

**Verdict: gimmick for production. Full stop.** Everything in the "describe your game and the AI builds it" category (and the adjacent "AI game engine" demos) produces something that demos well and collapses at the first real requirement: deterministic simulation, performance budgets, networked state, or a designer saying "make the third wave 8% harder." There is no production game of our ambition shipped this way, and there won't be during our dev cycle. We do not spend a single planning meeting on them.

What *is* real is the unbundled version of the same promise, which we adopt aggressively below.

### 2.2 Generative 3D & texture pipelines

**Verdict: production-real for a specific band of the asset stack — and that band is big enough to change our headcount math.**

The current tools (**Tripo, Meshy, Rodin/Hyper3D, CSM**) share a profile: image/text → textured mesh in minutes, quality now good enough that the output is a *draft*, not a toy. The honest capability matrix:

| Asset class | GenAI role | Verdict |
|---|---|---|
| Blockout / greybox props | Generate directly, use as-is | **Ship it into prototypes today** |
| Background environment clutter (debris, crates, signage, dead cars) | Generate → auto-retopo → material pass | **Production viable**, 5–10x cheaper than hand-modeling |
| Mid-ground props, weapon *concepts* | Generate → artist rebuilds topology, keeps silhouette | **Concept accelerator**, not final asset |
| Hero characters, first-person weapons, anything animated/deforming | — | **Hand-authored. No exceptions.** Topology, UV, and rig quality from generators are not there for close-up deforming assets |
| Enemies (our swarm archetypes) | Concept + basemesh draft only | Final meshes hand-built — they're instanced 1,000x, so per-asset polish has maximum leverage |

**Texture/material side:** tileable PBR generation (text-to-material tools, plus Substance's ML features) is legitimately production-grade for grunge, concrete, rust, asphalt — i.e., 80% of our surface area. Concept art via image models (Flux/SDXL-class with style LoRAs) is standard practice now and is how a 5-person team maintains a consistent gritty visual bible. **Rule: every AI-generated asset passes through a human art pass before main-branch import, and we keep provenance records (platform policies and storefront disclosure requirements around GenAI are live and evolving — Steam requires disclosure).**

### 2.3 AI-assisted coding & behavior generation

**Verdict: the highest-ROI AI category, and also where I'll enforce the most discipline.**

- **Agentic coding tools (Claude Code, Cursor, Copilot-class)** are force multipliers on exactly our profile of work: C++ boilerplate, Slate/UMG UI wiring, build scripts, data-table tooling, test harnesses, and porting reference algorithms (flow fields, boid steering) into our codebase. Expectation: 1.5–2.5x throughput on systems code for a senior engineer who reviews everything. They do *not* replace the person who understands frame budgets.
- **Swarm AI: the design insight is that we barely need behavior trees.** Massed enemies should be driven by **flow fields + local steering** (see the pathfinding section in Part 3), with tiny per-archetype state machines (advance / attack-barricade / stagger / die). LLMs are excellent at *authoring and iterating* this code and its tuning tools offline. **LLMs at runtime — zero.** No inference in the game loop, ever; it's non-deterministic, unbudgetable, and adds nothing to a horde.
- Where behavior trees *do* exist (elite enemies, rival-syndicate squads that flank and use cover), AI assistance generating BT node code and utility-scoring functions from design descriptions is genuinely effective — as generated *source we own and review*, not a black box.

---

## 3. Infrastructure & Backend

### Guiding principle

We are a small studio building a premium co-op PC game. Every infra decision defaults to **boring, cheap, self-hosted-where-stateless, managed-where-precious**. AWS's org-scale machinery (EKS, multi-account landing zones) is bloat for us; a couple of dedicated Hetzner boxes and a disciplined pipeline beat it on cost by 5–10x.

### 3.1 Source control & assets

- **Git + Git LFS** to start (team ≤ 6, disciplined binary hygiene, file-locking via LFS locks for uassets). **Migrate to Perforce Helix Core the week we feel merge pain on binary assets** — with UE5 at scale, that week will come; P4 free tier covers 5 users, and it runs happily on a single Hetzner dedicated server (~€50/mo) with nightly offsite snapshots. Don't theologize this; plan the migration path from day one (folder structure and naming that maps 1:1 to a depot).
- **Shared Derived Data Cache (DDC)** on the LAN/VPN from week one. This is the single cheapest UE5 team-velocity win that new teams skip: without it, every artist recompiles every shader locally.

### 3.2 CI/CD builds

- **Self-hosted runners on Hetzner dedicated hardware** (e.g., Ryzen 9 / 64–128GB class boxes, ~€100–140/mo each). UE5 compiles and cooks are brutally CPU/IO-bound; one dedicated box outperforms cloud CI runners that would cost 10x monthly at our build cadence. Two runners: one Windows (client builds, cook), one Linux (dedicated server builds).
- **Orchestrated by GitHub Actions** (repo is already on GitHub) driving **UAT/BuildGraph**: `BuildCookRun` for nightly packaged builds, PR-triggered compile + fast automation tests, nightly full cook + smoke test that boots the game, runs a scripted wave, and fails on crash or frame-time regression. **A nightly perf gate (median + p95 frame time on a fixed max-density replay) is in CI from month one** — density games die from performance regressions discovered too late.
- Look at **Epic's Horde** (shipped in the UE5 source distro: build automation + UnrealGameSync) when the team passes ~8 people; before that, it's more machinery than we need.
- Build artifacts → **object storage** (Cloudflare R2 or Backblaze B2 — egress-free/cheap, unlike S3) with retention policies. Steam branch uploads automated via `steamcmd` from CI: every nightly is playable by the whole team via a private Steam branch. This one habit — everyone plays last night's build — is worth more than any tool in this document.

### 3.3 Multiplayer & backend architecture

**Phase 1 (MVP → first playtests): no backend at all.**
Single-player and **listen-server co-op (2–4 players) over Epic Online Services** — EOS gives free lobbies, NAT punch/relay, voice, and Steam interop with zero servers owned by us. UE5's replication handles our scale *because of* the architecture choice below.

**The critical netcode decision, made now:** the swarm is **not** replicated per-enemy. 1,000 replicated actors is instant death. The server (listen or dedicated) owns the authoritative sim; clients receive **compressed swarm state snapshots** (archetype, quantized position/velocity, anim phase) via a custom replication path (Iris/replication-graph-style batching), and render locally with interpolation. Player characters, projectiles-that-matter, and structures replicate traditionally. Deterministic lockstep is tempting for a TD game but wrong for a game with third-person hitscan combat — snapshot + interpolation is the right call. **No networked physics** beyond cosmetic client-side effects; Chaos debris is never gameplay-authoritative.

**Phase 2 (if co-op takes off / progression integrity matters): dedicated servers, still cheap.**
- Linux dedicated server builds (already in CI from month one — building them early keeps us honest about server/client code separation even while we ship listen-server).
- Hosted on **Hetzner bare metal/cloud** behind a thin allocator: at our scale, **Agones on a single k3s cluster or even a hand-rolled allocator** (spin up a server process per lobby, ~30 lines of orchestration) beats adopting a managed fleet product. A 4-player PvE session server is small; one €50 box runs dozens.
- **Meta-services** (accounts, cloud saves, progression, telemetry): **Nakama (open-source, self-hosted on one DO droplet or Hetzner VM) or bare Postgres + a small Go/TS service.** We are premium PvE co-op — we don't need matchmaking ladders, anti-cheat arms races, or LiveOps platforms at launch. Progression validation server-side only if/when we see save-tampering hurt co-op.
- **Telemetry from the first playtest:** every death, wave-lost, build-placed, and frame-time-spike event ships to a ClickHouse or Postgres instance. Balancing a TD hybrid without funnel data is astrology.

### 3.4 What we deliberately do NOT build

No microservices, no Kubernetes-as-default, no custom launcher, no accounts system before we need cross-device saves, no AWS until a specific feature demands a managed service we can't cheaply self-host. Infra reviews quarterly; boredom is the KPI.

---

## Bottom line

| Decision | Call |
|---|---|
| Engine | **UE5 (5.5+)**: Mass Entity swarm, VAT instanced rendering, Niagara GPU VFX, Nanite (env only), Lumen (budgeted) |
| Text-to-game tools | **Rejected** — demo-ware |
| GenAI 3D (Tripo/Meshy/Rodin/CSM) | **Adopted** for blockouts, clutter, concepts; banned for hero/animated assets; human pass + provenance mandatory |
| AI coding | **Adopted aggressively** offline (agentic tools on systems code, BT/steering authoring); **zero runtime LLM** |
| Source control | Git LFS now, Perforce-on-Hetzner when binary merge pain arrives; shared DDC immediately |
| CI/CD | GitHub Actions + self-hosted Hetzner runners, nightly cook + perf gate, auto Steam branch push |
| Multiplayer | EOS listen-server co-op first; snapshot-replicated swarm (never per-enemy); Hetzner dedicated servers phase 2 |
| Backend | Nakama or Postgres + tiny service on one VM; telemetry from first playtest |
