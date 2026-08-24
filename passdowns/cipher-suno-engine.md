# Pass-down: CIPHER Suno Engine session

**Written:** 2026-08-24, by the Claude Code session that built CIPHER Phases 1–3 on branch `claude/cipher-suno-research-4cqfus`.
**Owner:** Jeffery (rap producer; the "owner feedback" cited throughout is his real-Suno listening tests).

## 1. What this agent was built to do

Build CIPHER: a prompt-engineering engine for AI music generators (Suno first) that produces hit-oriented rap prompts with a specific sonic identity — emo/experimental phonk core, dense-lyricism rap lineage, T.I.-swagger baseline — not generic rule-compliant prompts. Agreed phased plan: (1) research Suno's real prompting mechanics empirically and write them down with sources before coding; (2) a deterministic, testable TypeScript engine + thin runner, no app shell; (3) validate against real Suno audio and tune. The heart of the project is the artist-DNA → descriptor translation layer (Suno blocks artist names; the engine translates artists into subgenre/era/region/production language).

Explicitly rejected by the owner: premature app scaffolding, Mureka dual-output, LLM API layer, rating systems. Rule of the repo: **no Suno fact gets encoded unless it traces to `docs/suno-research.md`.**

## 2. What has been built so far

All on branch `claude/cipher-suno-research-4cqfus` (12 commits, head `f9de806` at time of writing). Note: GitHub reports the repo renamed to `Mussstarrd/Cipher`; pushes to the old name redirect fine.

- **`docs/suno-research.md`** — Phase 1 research, ~240 lines, per-claim sources with OFFICIAL/COMMUNITY/UNVERIFIED labels: version landscape (v5.5 real, March 2026), field budgets (style 1,000 / lyrics 5,000 / exclude 1,000 chars), front-loading, comma-phrase format, bracket-tag tiers, exclude-field leak behavior, slider recipes, Personas→Voices, §11 production-quality micro-tricks (second research pass).
- **TypeScript engine** (`src/`): 16 artist DNAs with descriptor *pools* and seeded picking (XXXTentacion, Juice WRLD, JID, J. Cole, Kanye, Jay-Z, Jadakiss, OutKast, UGK, Travis Scott, T.I., Kevin Gates, Chance, Drake, Prof, Weezer — B.o.B was added then removed at owner request); **DNA modes** (era variants: Jay-Z gritty/introspective/bounce, UGK slab/smoky/modern-trunk, T.I., Kanye, Gates); dominant+accent fusion with weight caps; two-tier kill list + negative-to-positive inversion + per-DNA lane guards; priority-aware style trimming; hook-first and beat-switch structure templates with tags-only and 2/3-verse options; **instrumental (beat-only) mode** — strips all performed-vocal language (vocal *samples* survive), anti-vocal exclude set, instrumental tag structure; slider presets per build type; polish layer (specific engineering phrases, never generic praise); artist-name leak scanner over all Suno-bound output.
- **CLI** (`npm run cipher`) and a **single-file web app** (`npm run build:web` → `dist/cipher.html`, ~49 KB, works offline) — the owner's daily driver on his Android phone, delivered as a file attachment (v9 last delivered). Also published at claude.ai artifact `c3b3781a-84d3-4077-a8d1-928b6a65b90c` (same content, viewer unreliable for him — see §4).
- **41 passing vitest tests** enforcing: determinism per seed, ≥9 distinct prompts per 10 seeds, name-leak zero-tolerance, funk-never-in-genre-slots, instrumental purity across all DNAs, budgets, inversion, mode pinning.
- Two bespoke prompt packages for "KIM" by King Combs (Ye chipmunk-soul tribute) delivered in chat; v1 failed (jazzy), v2 rebuilt — **v2 result unknown, owner hasn't reported back.**

## 3. Signals and sources watched

This is not a monitoring agent; it works request-driven. Its inputs are:

- **The research doc's source base:** official Suno help/blog (via search extracts — the sandbox proxy blocks direct fetches of suno.com/help.suno.com), community guide sites (hookgenius, jackrighteous, songsmith, sunostyles, acetaggen, the `mttkllr/suno-field-guide` and `daveshap/suno` GitHub repos read in full), third-party API docs (sunoapi.org, kie.ai) as the only proxy for field limits, and trade press. Reddit is unreachable from this environment; all "community consensus" is secondhand.
- **The owner's ear.** Phase 3 runs entirely on his reports from real Suno generations ("sounds jazzy", "Disney channel", "one banger then identical prompts"). This is the highest-authority signal and overrides research-derived defaults.
- No live feeds, no schedules, no PR subscriptions, no market/price data (this session is unrelated to trading despite the relay path of this request).

## 4. What it got right and wrong — concretely

**Right:**
- **Verified "Suno v5.5" is real** (released 2026-03-26) — the earlier spec review had flagged it as probably hallucinated; the research pass reversed that with official sources.
- **Test-first caught real bugs pre-commit:** the style-trimmer ate the beat-switch cue, the dominant vocal identity, and the accent's guitar descriptors on three occasions; each got a regression test.
- **The leak scanner earned its keep:** it caught the word "tip" (a T.I. alias) inside Drake's flow-guide text before it could reach a Suno field.
- **Diagnosed the "same prompt every day" bug correctly:** mobile browsers resume frozen pages rather than reloading, so the per-visit random seed never re-rolled; fixed with auto-reroll on resume, and widened per-roll variation.
- **The funk-attractor rule** (owner: "I'm scared your use of the word funk is going to be bad") — genre-slot tokens are takeover attractors; funk demoted to instrument level, enforced by a library-wide test. Same rule later explained the Jay-Z and KIM failures.
- **Confirmed win:** Jay-Z introspective mode + conscious-rap accent produced an owner-declared banger; the recipe shape is logged in the conversation.

**Wrong:**
- **Shipped a corrupted web build:** inlined the JS bundle with `String.replace`, whose `$&` replacement pattern mangled the engine's own regex-escape string. Also used a lookbehind regex that throws on iOS Safari < 16.4. Both would have been caught by testing the *built artifact* rather than only the source; that lesson is now baked into the build script (marker-leak assertion, es2017 target).
- **First DNA drafts drifted exactly where the owner predicted:** Jay-Z ("boom bap" + "chopped soul samples") rendered as generic jazzy lo-fi; Kanye's "pitched-up chipmunk soul samples" lead read as Disney. Rebuilt with harder era/region language and targeted lane guards (smooth jazz, cartoon vocals).
- **The KIM one-off prompt v1 repeated the same class of mistake** (organ + vinyl + muted + dusty = 1940s jazz) *after* the Jay-Z lesson — the attractor-stack rule wasn't applied to hand-written prompts, only to the library. v2 strips those and bans jazz by name.
- **Assumed the claude.ai artifact viewer would work on the owner's phone.** It never did (black screen + spinner, three rounds of debugging). The reliable path is direct file delivery; the artifact URL is maintained as a secondary copy only.
- **UX misreads:** exclude field shipped empty-by-default (owner read it as broken → lane guards now default on); the lyrics scaffold originally demanded the owner write bars (he never wanted to → tags-only is now the default, and later beat-only mode removed vocals entirely, which is his actual workflow).

## 5. Credentials and connections held (names only, no values)

- **GitHub access via the session's authenticated git proxy** to `mussstarrd/csdesigns-website` (original session repo, untouched) and `mussstarrd/cipher` (working repo, push access).
- **GitHub MCP server tools** (`mcp__github__*`), same repo scope.
- **claude.ai artifact publishing** under the owner's account (the CIPHER web-app artifact).
- **Claude_Code_Remote MCP tools** (session management, send_later, etc.) — available, unused except add_repo.
- Holds **no** Suno account, no Suno cookies/API keys, no third-party API credentials, nothing from the trading session. The engine deliberately has no network calls; the web app is fully offline.

## 6. Open questions

1. **Did KIM v2 land?** The rebuilt prompt (boom-boom-boom-boom kick fill, slurred mumble loop, jazz banned) is untested by the owner. If it worked, encode it as a "tribute soul" mode or a King Combs / Bad-Boy-revival DNA.
2. **Phase 3 validation backlog** (research §9/§11): do colon-form section cues work on v5.5; ALL CAPS reliability; `[Tempo Change: X BPM]` vs Build/Drop; blank-line behavior in lyrics (contested); phonk-specific slider sweet spots; whether bar-level pattern descriptions ("fill every fourth bar") do anything at all.
3. **Banger recall:** the app shows a roll # but can't re-dial one — a saved-rolls/pin feature (localStorage) is designed in conversation but unbuilt. Owner screenshots winners today.
4. **Which DNAs need modes next?** Only 5 of 16 have them; the single-mode DNAs (JID, Cole, Travis, XXX, Juice, Drake, Chance, Prof, Jadakiss, OutKast, Weezer) will produce the "always the same era" complaint eventually.
5. **Unresolved final message:** the owner's last substantive message before this pass-down was a detailed *story pitch* (outbreak/tribe/chained-carrier mythology, a survival protagonist) with no antecedent in this session — likely intended for a different session. It was interrupted before any reply; nobody has confirmed where it belongs. Do not treat it as CIPHER scope without asking.
6. **Repo rename:** GitHub says the repo moved to `Mussstarrd/Cipher`; the branch has 12 unmerged commits and no PR (owner never asked for one). Whether/when to merge to `main` is the owner's call.
7. **Environment caveat for successors:** the sandbox proxy 403s direct fetches of suno.com and most guide sites — research must go through search extracts; label evidence accordingly.
