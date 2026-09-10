# Art pipeline & skins — when do capsules become characters?

**Date:** 2026-09-10 · **Type:** research memo (brain-trust, technical art) · **Status:** proposal, no code changed
**Answers:** owner's ask in [feedback/2026-09-10-hero-first-play.md](../feedback/2026-09-10-hero-first-play.md) — "give me an idea of when we may even see some skins on these characters so I can manage expectations."

## TL;DR

- First non-capsule hero and runner in a build: **~3 weeks** (placeholder art, 8–11 engineer-days). Two hero outfits + two runner variants: **~7 weeks**. Skins that match the pitch's earned tiers (tracksuit → armor-weave → gold-plated): **not before ~5 months**, and the long pole is *art*, not code — we have no artist, and Milestones 2–3 (maze, gun) rightly outrank cosmetics.
- Do the **URP migration before any art lands.** Confirmed: shaders (including the VAT shader) are pipeline-specific and Unity's converter does not port custom shaders. It costs 1–2 days today with four procedural materials and no scenes; it costs weeks later.
- Runners render via **baked bone-matrix animation textures + `DrawMeshInstanced`** (an extension of the current path). Hero stays a normal `SkinnedMeshRenderer` + Animator.

## 1. Rendering 1,000 animated runners (Android + PC)

**(a) Vertex/bone animation textures — recommended.** Bake the skinned animation offline into a texture and let a vertex shader animate an ordinary `Mesh`, so thousands of instances go out in ~1 draw call per 1,023 (the same `Graphics.DrawMeshInstanced` path `FloodBootstrap.cs` already uses; Unity 6's `RenderMeshInstanced` lifts the 1,023 cap). Two flavours: *per-vertex* VAT (position per vertex per frame — simple, big textures, no attachments) and *bone-matrix* VAT (bone transforms per frame, bone weights stashed in UV channels — tiny textures, supports LOD meshes and weapon/prop attachments, which the skin system will want). Tools that exist for Unity 6:

| Tool | Notes |
|---|---|
| [Unity "Animation-Instancing" sample](https://github.com/Unity-Technologies/Animation-Instancing) | Bone-matrix approach, Built-in-RP shaders, "Unity 5.4+", 2017 codebase. Use as reference only. |
| [GPU ECS Animation Baker](https://assetstore.unity.com/packages/tools/animation/gpu-ecs-animation-baker-250425) (paid) | Bone-matrix into UV1–3 + textures, ShaderGraph subgraph, URP/HDRP, LODs, blending. Requires Entities 1.0+ — only relevant if the DOTS reconciliation pulls rendering into ECS. |
| [Mesh Animator](https://assetstore.unity.com/packages/tools/animation/mesh-animator-animate-massive-crowds-26009) (paid) | Per-vertex bake, memory-heavy, older; not preferred. |
| [UnityVATBaker](https://github.com/Mamantenok1599/UnityVATBaker) / [codewriter Mesh-Animation](https://github.com/codewriter-packages/Mesh-Animation) (OSS) | Per-vertex VAT bakers; UnityVATBaker targets Unity 6+. Good fallback. |
| Custom baker | ~2–3 days: editor script samples clips into an RGBAHalf texture, a ShaderGraph subgraph reads it, per-instance `_ClipFrame` via `MaterialPropertyBlock` float arrays. |

**Pick:** custom bone-matrix baker + ShaderGraph subgraph (we own it, it fits our non-ECS draw path, attachments work). **Fallback:** per-vertex VAT via UnityVATBaker if the bone-matrix shader fights us on Mali GPUs.

**(b) `SkinnedMeshRenderer` per agent — dead on arrival at scale.** SMRs cannot be GPU-instanced, so every runner is its own draw call plus its own Animator evaluation. A cited mobile crowd of ~50 characters already hit 300+ batches at ~50 fps ([forum](https://discussions.unity.com/t/performance-skinned-mesh-renderer-vs-mesh-renderer/867813)); 1,000 SMRs on a mid-tier Android means 1,000+ draw calls and roughly 50–100 ms of Animator CPU per frame. Fine for the hero, lieutenants, and a hard-capped ≤12 elites (docs/03 Trap 3); never for the flood.

**(c) Entities Graphics / DOTS animation (2026 state).** Entities Graphics 1.4 mesh deformation is still labelled *"experimental — not yet ready for production"* ([docs](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.4/manual/mesh_deformations.html)); Unity's own `com.unity.animation` package never shipped. Third parties (Rukhanka, GPU ECS Animation Baker) fill the gap, but all require the swarm to live in ECS, which is an open decision. VAT-on-`DrawMeshInstanced` stays valid either way.

**(d) Crowd LOD.** Three mesh LODs by camera distance (≈1,500 / 600 / 250 tris), then a billboard impostor (8-direction sprite sheet baked from the same clips) beyond ~40 m. With the chase camera most of the flood sits at mid-distance; impostors are the Android lever. Sequence it *after* the first VAT pass.

**URP implication — confirmed.** Built-in uses CGPROGRAM/surface shaders; URP uses HLSL with its own includes, and the [Render Pipeline Converter](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/features/rp-converter.html) "doesn't support converting custom shaders." Any VAT or skin shader written now is thrown away. Migrate first.

## 2. Where placeholder assets come from

| Source | Licence | Use |
|---|---|---|
| [Quaternius](https://quaternius.com/packs/animatedzombie.html) | CC0 | **Runner:** Animated Zombie Pack (FBX, atlas-textured) + Universal Animation Library; Universal Base Characters as hero backup. Cleanest licence. |
| [Mixamo](https://www.licenseorg.com/guide/3d-assets/mixamo) | Free, commercial use OK, no attribution, no standalone redistribution; governed by Adobe's general Terms of Use (some publishers flag the ambiguity) | **Hero:** a Mixamo humanoid + rifle run/strafe/aim/fire/idle, auto-rigged. Fine for placeholder; re-check before shipping. |
| Kenney | CC0 | Props, barricades, cars — not characters. |
| Unity Asset Store free packs | Per-pack Standard EULA | Ship-in-game OK; check each pack. |
| [Synty POLYGON](https://syntystore.com/pages/one-time-purchase-licence) (paid, one-time) | Perpetual; no NFT / AI-training use | Apocalypse / Gang Warfare / Heist packs are close to the pitch's look; the likely art-direction-pass source. |

**"Placeholder character" means:** (i) *Hero* — one rigged humanoid ≤15k tris, 2×1024 textures on PC / 512 on Android, `SkinnedMeshRenderer` + Animator with run/aim/shoot. (ii) *Runner* — one low-poly zombie, LOD0 ≤1,500 tris, one shared 512² atlas (ASTC 6×6 on Android), run/attack/die baked to a bone-matrix texture (~40 bones × ~90 frames ≈ 128×128 RGBAHalf — negligible).

## 3. What "skins" means technically

Three mechanisms, cheapest to dearest: **material/colour swap** (tint, atlas region), **mesh swap** (outfit body), **attachment set** (rings, coat, gold-plated weapon on bone sockets). The hero uses all three; runners get tint plus a couple of mesh variants only, so the flood stays one material and one draw per 1,023.

Structure it as data: a `SkinDefinition` ScriptableObject table — `id, tier, unlockMilestone, bodyMesh, materialSet, attachments[]` — mirroring the pitch's rule that tiers are earned, never sold, so a new tier is a design entry, not code. Runner variety rides on per-instance data: `MaterialPropertyBlock.SetVectorArray("_Tint")` / `SetFloatArray("_Variant")` alongside the matrix array. The SRP-batcher "MPB breaks batching" caveat does not apply here — `DrawMeshInstanced` is not SRP-batched.

## 4. Timeline (one AI engineer, session-based, owner playtests)

| Milestone | Engineer-days | Dependencies | Biggest risk / what slips it |
|---|---|---|---|
| **A — first non-capsule hero + runner** (placeholder, VAT) | 8–11: URP 1–2, VAT baker+shader 3–4, sourcing/import/retarget 1–2, hero Animator 2, integration+LOD 1–2 | URP migration; asset download by owner (Mixamo needs an Adobe ID) | No Unity CI yet — `game/` edits are unverified until the owner opens the editor, so each bug costs a session. VAT on Mali needs a real Android device. **Calendar: ~3 weeks.** |
| **B — art-direction pass**: 2 hero outfits, 2 runner variants | 6–9: skin table, sockets, mesh swap, runner tint/variant, first material polish | A; Synty purchase decision; owner taste loop | "Art direction" iterates by nature — budget two review rounds. Attachment sockets must match across outfit meshes. **~T+7 weeks.** |
| **C — real skins per pitch tiers** | 15–25 engineering, plus outsourced art (rigged hero with 3 outfit tiers: ~4–10 weeks turnaround, roughly $2–8k) | B; campaign milestone system (there is nothing to *earn* yet); gold PBR/sheen shader; coat that reads at 50 m | Art is the long pole; M2 maze and M3 gun outrank it; no artist on staff. **Earliest ~T+5 months (Feb 2027)**, later if M2/M3 slip. |

Pessimism note: these dates assume cosmetics run *alongside* M2/M3, taking ~30% of engineering. If the owner wants skins faster, the trade is slower maze/gun work — surfaced, not hidden.

## 5. What the owner will see and when

| When | What is in the build |
|---|---|
| T+1 week | Project on URP; same graybox, same fps. First VAT runner in an editor test scene (still ugly). |
| T+3 weeks | Hero is a rigged human that runs/aims/shoots; runners are a low-poly zombie flood with run/attack/die. Still placeholder art. Android device check. |
| T+2 months | Hero has 2 swappable outfits via a skin table; runners have 2 variants + per-instance tints; LOD/impostors in. First real "look." |
| T+5 months+ | Earned cosmetic tiers as in the pitch, gated on campaign milestones — requires commissioned art and the progression system. |

## Sources

- [Unity Animation-Instancing sample](https://github.com/Unity-Technologies/Animation-Instancing) · [GPU ECS Animation Baker](https://assetstore.unity.com/packages/tools/animation/gpu-ecs-animation-baker-250425) · [UnityVATBaker](https://github.com/Mamantenok1599/UnityVATBaker)
- [Entities Graphics 1.4 — mesh deformations (experimental)](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.4/manual/mesh_deformations.html)
- [Render Pipeline Converter (no custom shaders)](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/features/rp-converter.html)
- [Mixamo licence summary](https://www.licenseorg.com/guide/3d-assets/mixamo) · [Quaternius Animated Zombie (CC0)](https://quaternius.com/packs/animatedzombie.html) · [Synty one-time licence](https://syntystore.com/pages/one-time-purchase-licence)
- [SMR vs MeshRenderer crowd thread](https://discussions.unity.com/t/performance-skinned-mesh-renderer-vs-mesh-renderer/867813)
