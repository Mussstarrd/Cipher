# Accounts & services — what to sign up for, in what order

**Date:** 2026-09-10 · **Type:** decision memo · **Status:** proposal, no code changed
**Answers:** the owner's ask in [feedback/2026-09-10-maze-v1-first-play.md](feedback/2026-09-10-maze-v1-first-play.md) — *"let me know what accounts I may need to make to get you repositories for ai generation for graphics or backgrounds or sounds or droplet or AWS resources, whatever makes sense."*

**The most important column below is "Can we ship it?"** CIPHER is meant to sell on Steam, and a tool that makes beautiful output we are not licensed to distribute is worth nothing to us. Every licence claim links to the vendor's own terms; where a vendor blocked verification it says **UNVERIFIED**, meaning unknown, not yes. **Measured on this machine:** RTX 4060 Laptop GPU, **8 GB VRAM**, 16 GB RAM — that number drives §4.

---

## 1. One-page summary

| Service | What it unblocks | Cost/mo at our scale | Can we ship it? | Priority |
|---|---|---|---|---|
| **GitHub** (have) | CI, build artifacts, the repo | **$0** Free tier | n/a | Have |
| **Unity Personal** (have) | The engine; splash removable | **$0** under $200k/yr | Yes | Have |
| **Steamworks** | Store page, playtest, cloud saves, **Remote Play Together** | **$100 once**, recoupable | Yes | **Now** |
| **ComfyUI, local** | Textures, backdrops, UI, concept art | **$0**, unlimited | **Yes** — Apache/MIT models | **Now** |
| **Quaternius + Mixamo** | The actual runner & hero meshes | **$0** | Yes | **Now** |
| **Sonniss GDC 2026, FilmCow, Freesound CC0** | Real SFX, replacing synthesis | **$0** | Yes, royalty-free | **Now** |
| **ElevenLabs Starter** | Bespoke SFX, later voice | **$6** | Yes, paid tier only | Soon |
| **ambientCG / Poly Haven / Material Maker** | CC0 materials, HDRIs, PBR authoring | **$0** | Yes, CC0 / MIT | Soon |
| **Sloyd** | Static props, barricades, crates | **$15** | Yes, paid tier | Soon |
| **Meshy or Rodin** | Hero *blockout* only | $20–30 | Needs retopo | Later |
| **Scenario / Midjourney** | Hosted image gen if local chafes | $10–15 | Yes, paid tier | Later |
| **Sentry / itch.io** | Crash reports, playtest distribution | **$0** | n/a | Later |
| **Composer buyout** | Soundtrack, adaptive stems | $400–800/min once | Yes, with warranty | Later |
| **Suno / Udio** | Music | $10–30 | **Risky / No** | **Skip** |
| **Hunyuan3D** | Local 3D gen | $0 | **No** — barred in EU/UK | **Skip** |
| **DigitalOcean / AWS** | Nothing we need today | $4–24 | n/a | **Skip** |

---

## 2. Already have, or free

**GitHub — stay on Free, but change one setting.** Private repos get **2,000 Actions minutes/month** and **Windows burns them at 2×** ([multipliers](https://docs.github.com/en/billing/reference/actions-minute-multipliers)). Our Unity job runs ~35 min wall clock → **70 charged minutes → ~28 Windows builds/month**, about one per weekday. Overage is cheap (~$0.35 per extra build). **The real ceiling is storage:** Free includes **500 MB**, a Unity player is 1–5 GB, billed at **$0.25/GB-month** ([pricing](https://docs.github.com/en/billing/reference/actions-runner-pricing)). Our workflow keeps builds 14 days. **Engineering action: drop to 3–5 days.** Usage: [github.com/settings/billing](https://github.com/settings/billing).

**Unity Personal — free until $200,000.** The [Editor Software Terms](https://unity.com/legal/terms-of-service/software): *"The Financial Threshold for Unity Personal is $200,000 USD for the most recent twelve (12) month period"* — **revenue *and* funding combined**, trailing 12 months. The Runtime Fee is dead, and in Unity 6 the **splash screen is removable on Personal** ([pricing](https://unity.com/pricing)). Consoles would need Pro ($2,310/yr) — not our problem yet. Pipeline note: Unity 6 now declares manual `.alf`/`.ulf` activation **unsupported for Personal**, confirming our buildalon setup is correct — don't let anyone migrate us to the old GameCI flow.

**Steamworks — $100, and pay it sooner than feels natural.** [Steam Direct](https://partner.steamgames.com/steamdirect): *"a $100.00 fee for each product… recoupable in the payment made after your product has at least $1,000.00 Adjusted Gross Revenue."* Pay now: **the 30-day fee-to-release clock starts at payment** and nothing gates paying. It unlocks the store page, **Steam Playtest** (free child app, no second $100), Cloud saves, achievements, Steam Input, and Remote Play Together.

**Remote Play Together deserves your attention.** It *"allows users to invite their Steam Friends to join the game as though they were sitting at the same computer… Only the host needs to own and install the game,"* and is *"enabled automatically for games that are listed with Local Multiplayer, Local Co-op, or Shared/Split Screen capability"* ([docs](https://partner.steamgames.com/doc/features/remoteplay)). **The couch co-op you asked for therefore becomes online co-op for free, with no netcode** — a big, cheap win I'd like to design toward.

You'll need: legal name (enter "Sole Proprietorship"), a bank account whose holder name matches **exactly**, and a tax interview (2–7 business days).

---

## 3. AI asset generation — what actually ships

Honest headline: **AI is genuinely useful for 2D surfaces and sound, and largely not yet useful for the 3D characters we need.**

**Text-to-image — ships, use it.** Textures, backdrops, UI panels and icons are real wins. Best value is **local and free** (§4). Hosted alternatives: **[Scenario](https://www.scenario.com/pricing)** ($15) is game-asset oriented with the clearest terms — *"All paid plans include a full commercial license… Free plan outputs are for personal and evaluation use only."* **Midjourney** ($10, [ToS](https://docs.midjourney.com/hc/en-us/articles/32083055291277-Terms-of-Service)) grants *"You own all Assets You create,"* but output is **public by default** unless you pay $60 for Stealth. **Leonardo's free tier is a trap** — its ToS vests output IP in *them*.

Two rules apply everywhere: **free tiers are never shippable**, and **no vendor indemnifies an indie**. Adobe Firefly's advertised indemnity is gated to Creative Cloud *enterprise* customers **and** excludes modified or combined assets — i.e. every game texture ([Adobe's terms](https://www.adobe.com/cc-shared/assets/pdf/legal/servicetou/adobe-generative-ai-product-specific-terms-en-us-20260423.pdf)).

**Text-to-3D — reference only, for now.** Measured against our actual pipeline: **no 2026 text-to-3D tool produces a game-ready low-poly *rigged* humanoid.** The first reason is structural — our VAT plan bakes animation against **one shared skeleton** so 1,000 runners draw from one bake, but every AI auto-rigger invents a *different* skeleton per asset, destroying exactly the property we need. Second, "quad output" is not "edge flow": quads come out uniformly distributed rather than looped at elbows and shoulders, so rigs pinch. Third, auto-UVs rarely survive decimation to 1,500 tris.

**So meshes come from where `design/art-pipeline-and-skins.md` already said**: **[Quaternius](https://quaternius.com/license.html)** packs plus **Mixamo** rigging and clips, both $0. ⚠️ **Correction to that doc:** Quaternius is **no longer CC0**. Current licence: *"You can use these assets, free of charge, in personal, educational, and commercial games and other projects, with no credit required"* — but **no redistributing the raw assets**. Still fine for us; the doc needs updating.

Where AI 3D *does* pay off: **static props** (barricades, crates, turret housings) have no deformation and so no edge-flow requirement — **[Sloyd](https://www.sloyd.ai/terms-of-use)** at $15 fits. **Hero blockout** via Meshy ($20) or Rodin ($30) saves sculpting, but saves neither retopo nor rigging.

**Do not use Hunyuan3D**, despite being free and local. Its licence defines Territory as *"worldwide… excluding the European Union, United Kingdom and South Korea"* then bars distributing **the output** outside it ([LICENSE](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1/blob/main/LICENSE)). Steam sells into the EU by default. Likewise **Roblox Cube** is research-only despite an `openrail` tag on HuggingFace — a HuggingFace tag is not a licence.

**Materials — all free.** [ambientCG](https://docs.ambientcg.com/license) and [Poly Haven](https://polyhaven.com/license) are CC0 (*"free to use without attribution — even in commercial circumstances"*); Poly Haven's HDRIs will do real work for URP lighting. For authoring: **Material Maker** (MIT) and **Materialize** (GPL-3, photo→PBR, outputs Unity's Smoothness convention natively). Skip Substance ($60/mo) — its text-to-texture AI appears *removed* in v5.0. `withpoly.com` is dead; `armorlab.org` has lapsed and now serves casino spam — do not download from it.

**Audio — the easiest win on this page, and nearly all free.** The **[Sonniss GDC 2026 bundle](https://gdc.sonniss.com/)** is 7.47 GB of professional library SFX; [licence v2.0](https://sonniss.com/gdc-bundle-license/) reads *"Licensee may use and modify the licensed sound effects for personal and commercial projects without attribution"* (no reselling them as a library). The licence version is **pinned to your download date**, so we log dates. Add **[FilmCow](https://filmcow.itch.io/filmcow-sfx)** (~4,000 WAVs, *"completely royalty-free… not required to credit me"*), **[Freesound](https://freesound.org/help/faq/)** filtered to **CC0** — avoid CC-BY-NC, which bars commercial use — and **[Kenney](https://kenney.nl/support)** (CC0). For bespoke sounds, **[ElevenLabs](https://elevenlabs.io/terms-of-use) Starter ($6)**: free tier is non-commercial, paid is *"for commercial purposes"* with *"you retain all rights in and to your Output."* Two traps — your SFX are sublicensed to other users unless you **opt out**, and **Eleven Music is governed separately** on UNVERIFIED terms that may exclude downloadable games. Use it for SFX, not soundtrack.

**Music: skip Suno and Udio.** **Udio is impossible, not merely risky** — its terms say *"You may not download copies of any Output"* and grant personal, non-commercial use only. **Suno** is contractually cleaner (rights are perpetual and survive cancellation) but *"makes no representation or warranty… that any copyright will vest in any Output,"* gives **no indemnity**, UMG and Sony remain active plaintiffs, and in July 2026 a Munich court ruled against Suno **at the output level** — precisely the exposure that reaches a game selling into the EU. Also skip **Epidemic Sound**, whose licence forbids *"any interactivity with the Licensed Work"* (fatal to an adaptive combat score) and excludes game UGC (fatal to streamer coverage), and **Artlist**, which routes games to Enterprise. The only route that moves risk off the studio is a **composer buyout with an IP warranty and indemnity** — $400–800/min, roughly $12–25k for a full score. [Incompetech](https://incompetech.com/music/royalty-free/licenses/) (CC-BY, or ~$30/track to drop attribution) works as greybox placeholder. None of this is needed yet.

**One compliance item regardless of tool:** Valve **requires disclosure of AI-generated content that ships in the build** ([content survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey)), so AI textures must be declared — but our use of Claude to write C# does **not**, since Valve explicitly exempts dev tooling. Separately, purely AI-generated art is **not copyrightable**, so anything we want to defend needs human paint-over. I'll keep a per-asset provenance log (source, URL, licence, licence *version*, download date) from the first sourced asset; Sonniss's version-pinning makes the download date legally load-bearing, and retrofitting such a log is miserable.

---

## 4. Local vs cloud generation — run it locally

**Recommendation: install ComfyUI on the laptop and generate images locally.** Free, unlimited, private, and it removes a whole category of licence ambiguity — no subscription to lapse, no terms changing under us, no public gallery exposing our art direction pre-launch, and no per-image cost when a texture takes forty attempts. With **8 GB VRAM**:

| Model | Licence | On 8 GB |
|---|---|---|
| **SDXL** | Open RAIL++-M | **Yes** — tight, but the workhorse |
| **SD 1.5** | Open RAIL-M | Trivially |
| **FLUX.2 [klein] 4B** | **Apache 2.0** | **Yes** — the clean modern pick |
| FLUX.1 [dev] | ambiguous | Q4 only — **avoid on licence grounds** |
| SD 3.x | terminates at $1M revenue | Avoid |

Use **[ComfyUI](https://github.com/comfyanonymous/ComfyUI)** (Automatic1111 has been abandoned since Feb 2025). Stick to **Apache-2.0 / MIT / RAIL** weights, which grant output rights outright with no revenue cliff and no registration. Avoid FLUX **[dev]**, whose terms simultaneously permit commercial output and define commercial use as excluded. 8 GB is enough for tiling textures, backdrops and UI at our resolutions; if we ever need big photoreal output, *that* is the moment to rent a cloud GPU by the hour — not before.

---

## 5. Cloud / infra — do not buy any of this yet

**You mentioned a droplet or AWS. We need neither today, and probably not for a year.** GitHub already hosts the repo, runs CI, and stores artifacts. A server adds a bill, a machine to patch, and secrets to manage, in exchange for nothing we lack.

| Don't buy | Why not | Trigger that changes it |
|---|---|---|
| **DigitalOcean / AWS** | Nothing to run on it | We need an authoritative game server, or a 24/7 service |
| **S3 / Spaces / R2** | GitHub artifacts + itch.io cover it | Artifact overage exceeds ~$5/mo |
| **Unity Pro** ($2,310/yr) | Personal covers Steam fully | Revenue+funding nears $200k, or console port |
| **Photon / Unity Relay** | Remote Play Together gives co-op free | We commit to *online* co-op on separate machines |
| **Substance 3D** ($60/mo) | Free tools cover our stylised look | We hire an artist who demands Painter |
| **Sentry paid** | Free tier is 5k events/mo | Playtesters exceed it |

**Playtest distribution, when needed:** **itch.io is free** and beats any droplet — "Restricted" mode gives revocable download keys, hides the page from search, and `butler` does **delta patching**, so testers download only the diff. Steam Playtest is the other free option once the $100 is paid. A droplet would cost money to do this *worse*.

**Crash reporting, when needed:** **Sentry free** (5,000 events/month) with the official Unity SDK is the only $0 option that verifiably handles IL2CPP line numbers, Windows minidumps and Android ANRs. ⚠️ Unity's legacy Cloud Diagnostics was **deprecated in Aug 2025**. Caveat: with 300–1,000 agents one per-agent exception can burn 5,000 events in seconds, so I'll add rate-limiting before it ships. **Save-game backup:** Steam Cloud, free with the $100.

**If couch co-op ever becomes online co-op:** Steam's relay networking is free to partners, and Epic Online Services is free and works on Android. Only if both fail do we rent a server — and because `sim/` is already pure deterministic C# with zero UnityEngine references, it would run headless on a **$6/month** droplet with almost no porting work. Good position to be in, and a reason not to pre-buy.

---

## 6. The shortest path — do these this week

**Four things, one afternoon:**

1. **Steamworks — $100.** [partner.steamgames.com](https://partner.steamgames.com/steamdirect). Do it first; it starts a 30-day clock and unlocks the store page, playtest, cloud saves and Remote Play Together. Have legal name, bank details and tax info ready.
2. **Download the free asset sets** — no account, or a throwaway one: [Sonniss GDC 2026](https://gdc.sonniss.com/) and [FilmCow](https://filmcow.itch.io/filmcow-sfx) (SFX), [Quaternius](https://quaternius.com/) (characters), [Poly Haven](https://polyhaven.com/) + [ambientCG](https://ambientcg.com/) (materials, HDRIs). Put them where I can reach them and tell me the path. **This is the highest-value hour on the list** — it is what turns capsules into characters and synthesis into real sound.
3. **Adobe ID for [Mixamo](https://www.mixamo.com/)** — free, needed for hero rigging and animation clips.
4. **ElevenLabs Starter, $6** — [elevenlabs.io](https://elevenlabs.io/pricing). Optional; the Sonniss bundle may cover more than you expect.

**I'll install ComfyUI locally myself** — no account, no key, nothing to buy.

### What to hand me, and what never to hand anyone

**Safe in GitHub repo secrets** (Settings → Secrets and variables → Actions) — encrypted, never printed in logs, unreadable even by you once saved: `UNITY_USERNAME` / `UNITY_PASSWORD` (already required; the Unity build stays skipped until they exist), and later a Steam build-upload credential or itch.io butler key.

**Never paste into a chat window — including to me:** any password, ever (Unity's goes into the GitHub secrets UI directly, not through chat); bank details, tax IDs, card numbers, government ID; Steam account-recovery codes or Steamworks partner credentials.

**The rule that covers every case:** put credentials into the *system that needs them*, never into a message. If a key does leak into a chat, a log, or a commit, assume it is public and rotate it immediately — rotating takes two minutes and is always cheaper than hoping. Most tools above are used through their websites and need no API key at all.

---

## Verification notes

Every licence quoted above was read on the vendor's own terms page, and the RTX 4060 / 8 GB figure was measured on the machine.

**Could not verify — treat as unknown:** Adobe Firefly's indemnification wording and Mixamo's current licence text (`helpx.adobe.com` blocks automated access — worth a manual read before Mixamo assets ship); ElevenLabs' *Music* commercial-rights table; Tripo's price tiers; Epidemic's current subscription in-game clause; Substance Sampler's text-to-texture feature. Suno/Udio litigation status is press-sourced, not primary.

**Open decisions this raises for the register:** whether we design toward Remote Play Together as the co-op story; whether we ever commission a soundtrack rather than skipping music; and whether Steam Deck Verified becomes a third performance target alongside Android 300–500 and PC 1,000+.
