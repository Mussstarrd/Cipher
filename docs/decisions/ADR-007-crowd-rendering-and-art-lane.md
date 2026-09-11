# ADR-007 — How a thousand people get rendered, and which art lane we buy into

**Status:** Proposed (ENG recommendation; the money question is the owner's) · **Date:** 2026-09-11
**Answers:** the owner's question, *"when am I going to be able to play an actual graphical beta level"*
**Builds on:** ADR-004 (the setting), ADR-005 (the loop)

## Context

The game draws 1,000+ agents. Today they are untextured capsules. They need to be recognisable clothed
people, at 300–500 on a mid Android phone and 1,000+ on PC, and no amount of shader polish gets there: a
`SkinnedMeshRenderer` per agent dies in the low hundreds.

Two research passes ran in parallel on 2026-09-11, one on the rendering technique and one on where the art
comes from. This ADR records both, because they constrain each other: the rendering technique demands that
every civilian share one skeleton, which decides what we are allowed to buy.

## Decision 1 — Vertex Animation Textures, not DOTS

**We bake animation into textures and keep drawing with `Graphics.DrawMeshInstanced`.**

A compute pass bakes each clip's per-vertex positions and normals into a texture, one column per frame. The
rig is thrown away; what ships is a static mesh plus a vertex shader that reconstructs the pose by sampling
that texture at a per-instance time value. A VAT agent is a plain mesh, so everything we already do keeps
working.

**Why this and not the alternatives:**

| Option | Verdict |
|---|---|
| **VAT + `DrawMeshInstanced`** | **Chosen.** Pure URP, Vulkan-safe, no new architecture. Per-instance outfit and tint ride along as instanced properties, and a per-instance time offset stops the crowd marching in lockstep. |
| `BatchRendererGroup` | Right answer *later*. It lifts the 1,023-per-call cap and culls on GPU, and it uses the same VAT shader. Migrate the batching layer only when PC counts demand it. |
| URP GPU Resident Drawer | Helps only once agents are plain mesh renderers, which VAT makes them. Free upside, not a plan. |
| **DOTS / Entities Graphics** | **Rejected.** It is Unity's showcase crowd path, and it requires converting `sim/` to ECS. Our sim is deterministic pure C# with a state-hash guard, and that is worth more than the batching. **Do not adopt DOTS incidentally; it needs its own ADR.** |

**The LOD rule that makes 1,000 affordable:** three tiers by camera distance, bucketed in the same loop that
already produces positions. Full VAT mesh near the fight, a reduced-vertex VAT mesh mid, flipbook impostors
beyond. Android never needs more than roughly 250 full-quality agents at once.

**Known traps:** bake normals or lighting goes flat; use half-float precision or vertices jitter; chunk at
1,023 instances; randomise the time offset at spawn.

## Decision 2 — One art lane, bought, stylised

The single biggest failure mode in the research was **mixing a photoreal pack with a stylised one**. Pick a
lane and hold it.

**Recommendation: the stylised lane, built on Synty POLYGON.** Roughly **$135 total** for suburbs,
nature/biomes, apocalypse (which carries ordinary-clothed civilians, vehicles and abandoned cars) and a war
pack for sandbags and barricades.

**Why stylised, given the concept art is photoreal:**

1. **One shared humanoid rig across every civilian pack**, which is exactly what VAT batching requires. The
   photoreal route fragments across vendors and rigs.
2. **Low polycount is the whole game.** We render a thousand of them.
3. It is **$135 and available today** against a photoreal crowd pipeline that costs an order of magnitude
   more in money and far more in labour.
4. **The concept art still governs.** Palette, light, weather, framing and staging carry the tone. The
   winter-Virginia overcast lighting pass that shipped with this ADR does more for the mood than model
   fidelity would.

**This is a real trade and the owner should see it clearly.** The renders he approved are photoreal. Synty
will not look like them; it will look like a well-directed stylised game set in the same place. The
alternative is a longer, dearer road to something closer to the renders. **That call is his.**

**Licence position, verified:** Unity Asset Store's EULA permits commercial shipping with no attribution.
Poly Haven is CC0, Quaternius and Kenney are effectively unrestricted. Sketchfab is per-model and its
editorial licence is never shippable, so every Sketchfab model needs checking individually. Synty's per-seat
terms should be re-read at purchase. Mixamo animations may ship but not be redistributed, and its long-term
future under Adobe is uncertain enough to note.

**AI 3D generation is a prop tool, not a character tool.** Meshy's paid tiers grant full commercial rights
and its output is fine for clutter; topology and rigging are not yet good enough for crowds or heroes.

## Decision 3 — The look is inked comic book (amended 2026-09-11)

The owner asked, after seeing the stylised-versus-photoreal trade: *"It can be comic book graphics?"*

**Yes, and it resolves decision 2 rather than complicating it.** Built and running the same day.

**Why this is the right answer and not a compromise.** Stylised-because-it-was-cheap looks cheap.
Comic-because-it-was-chosen looks authored. Same models, different intent, and the audience can tell.
Specifically:

1. **It makes the cheap lane look deliberate.** Low-poly models under flat banded light with a hard ink
   line read as drawn art. The same models under attempted photorealism read as a budget shortfall.
2. **It is shader work, which is ours.** No purchased asset determines the look. We control it in one file
   we already own, and it applies to everything ever added.
3. **It is the single best answer to legibility at a thousand agents**, which is a gameplay problem, not a
   taste one. Under PBR a distant crowd is grey mush. Under ink outlines every individual keeps a readable
   silhouette, which is what the player needs to judge a wave.
4. **It is faster than PBR**, not slower. Banded lighting is cheaper than a full BRDF, and the outline is an
   extra instanced pass rather than a screen-space effect, so it costs roughly one extra draw per batch.
5. **It matches something the owner asked for months ago** and we never delivered: comic-book presentation.
   Mission briefings can now be panels in the same visual language as the game.
6. **The photoreal concept art stays useful**, and in the one way that survives a style change: palette,
   weather, staging, composition and light direction. Those are the parts that were already doing the work.

**How it is implemented**, in `Assets/Shaders/InstancedLit.shader`:

| Element | Technique |
|---|---|
| Ink outline | Inverted hull in a pass tagged `SRPDefaultUnlit`, width scaled by view distance so the line keeps a near-constant screen weight. No renderer feature, no RenderGraph, and it instances with the body. |
| Cel shading | Wrapped lambert quantised into bands, so the dark side stays readable instead of crushing to black. |
| Shadow | A printed colour tint rather than an absence of light. |
| Halftone | 45-degree dot grid in screen space, applied only in the darkest band so it reads as shading and not noise. |
| Silhouette | Cool rim light, which matters enormously once a thousand agents overlap. |

**What this does to the budget.** Nothing. The same purchase still applies and the same VAT crowd pipeline
still applies. The only thing that changed is that the result now looks like a decision.

## What shipped with this ADR

The URP migration, on 2026-09-11:

- `com.unity.render-pipelines.universal` 17.0.4, with `ExodusPipeline`/`ExodusRenderer` assets created
  reproducibly by `UrpSetup.cs` so CI can rebuild them from a clean checkout.
- **`Exodus/InstancedLit`**, a hand-written instanced URP shader with forward, shadow-caster and depth
  passes. It replaces `Shader.Find("Standard")`, which means **the ~98,000-variant Android problem in
  CLAUDE.md is fixed**: Keep All stripping is now cheap because the shader has a handful of variants.
- An overcast-winter lighting pass: low raking sun, soft shadows, trilight ambient off a grey sky and brown
  leaf litter, exponential-squared haze. Half the game is daylight and this is what that daylight looks like.
- 61 game tests green; Windows player built.

## Consequences

| Area | Effect |
|---|---|
| **Android** | Unblocked. The variant explosion was the blocker and it is gone. |
| **Sim** | Untouched, and deliberately so. Rejecting DOTS keeps determinism and the state-hash guard. |
| **Asset pipeline** | New and needed: FBX → VAT bake → static mesh + textures. Tooling to build. |
| **Budget** | About $135, one time, owner's approval. |
| **Look** | Stylised models, photoreal lighting and staging. |

## Open questions for the owner

1. ~~Stylised or photoreal.~~ **Resolved by decision 3: inked comic book.** The owner picked it and it is
   better than either option originally offered.
2. **The purchase still needs approving**, about $135, and it blocks every date in
   `docs/design/ROADMAP-graphical-beta.md`.
3. **How hard should the ink be?** Line weight, band count and halftone strength are all material
   properties and can be tuned in minutes once the owner has seen them in motion rather than in a still.
