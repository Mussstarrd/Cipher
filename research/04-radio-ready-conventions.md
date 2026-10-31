# 04 — Radio-Ready Conventions for Hip Hop Instrumentals

Research memo for suno-forge (Suno prompt generator for radio-ready hip hop instrumentals).
Compiled 2026-10-31. All URLs were located via web search on that date; where a page could not be opened directly (most hosts were outside this session's allowed network list), the figure is taken from the search snippet and is flagged **[snippet]**. Items marked **[inferred]** are this memo's synthesis, not a cited claim. Items marked **[unverified]** could not be confirmed with a source.

---

## 0. Headline numbers (for the generator's defaults)

| Parameter | Default | Range / note | Basis |
|---|---|---|---|
| Runtime | 3:00 | 2:30–3:30 (hook-first compact ≈2:45; full radio form ≈3:10–3:30) | #1 hits avg 3:29 (2024 & 2025); overall Hot 100 top-10 avg ≈3:25 (2025); Spotify charting avg ≈3:00 (2024); longest-charting 2025 songs avg 3:17. See §1.1. |
| Intro | 4 bars | 2–8 bars; ≤15 s absolute | #1-hit intros avg 14 s (2023, 2024); hip hop intros avg 17 s (2017); playlist/radio guides: <15 s. See §1.2. |
| First hook lands by | 0:20–0:35 | never later than 0:45 for radio/playlist lanes | Hip hop first chorus avg 30 s (2017); Spotify counts a stream at 30 s; 33% of 2025 #1s now put first chorus after 1:00 (counter-trend, artist-driven). See §1.2. |
| Hook length | 8 bars | 4 bars ×2 or 8 bars | §1.3 |
| Verse length | 16 bars | 12–16 (8 for a short "verse 3") | §1.3 |
| Ending | Cold/hard stop on the downbeat, or 1–2 bar ring-out | Fade-outs are rare in modern hits | §1.5 |
| Integrated loudness (hip hop release) | −9 LUFS | −8 to −6 common for aggressive trap/phonk; −10 to −12 for neo-soul lanes | §2.1 |
| True peak | −1.0 dBTP | −2.0 dBTP if master is louder than −14 LUFS (Spotify guidance) | §2.1 |
| Streaming normalization reference | −14 LUFS (Spotify, YouTube, Amazon, Tidal); −16 LUFS (Apple Sound Check) | | §2.1 |

---

## 1. Radio-ready structure for a hip hop instrumental

### 1.1 Target runtime (2:30–3:30) and the length trend 2019–2026

* **Billboard Year-End Hot 100, 1959–2025 (6,618 songs):** mean length peaked ≈4:37 around 1990, fell to **3:16 in 2020**, and sits at **3:24 in 2025**. From 2019 to 2025 the mean grew 5.5% while the **median moved −0.3%** — the "typical" hit is flat at roughly 3:15–3:25; the recent uptick is a few long outliers. (Wang, dev.to, 2026) **[snippet]**
* **Hot 100 #1 hits:** average length **3:29 in 2025, identical to 2024**, and 4 seconds longer than the overall top-10 average in 2025 (≈3:25). Songs under 3:00 were the most common bucket in the overall top 10 but only 11% of #1s. (Hit Songs Deconstructed, *2025 #1 Hit Report*, Feb 2026) **[snippet]**
* **Longevity favours shorter:** 2025's longest-charting songs averaged **3:17 vs 3:29** for #1s and were nearly twice as likely to run under 3:00. (Hit Songs Deconstructed, HSD Wire, 2026) **[snippet]**
* **Spotify charting songs:** average ≈**3:00 in 2024**, ~15 s shorter than 2023 and ~30 s shorter than 2019; **hip hop and Latin fell the most, −29 s each, 2018→2024**. (Chartmetric, *Year in Music 2024*) **[snippet]**
* **Long-run hip hop:** 4.5 min (1995) → 3.2 min (2022) across 200 popular artists. (Chartmetric, "Are songs actually getting shorter?") **[snippet]**
* **Earlier benchmarks:** MIDiA found the average top-10 hit fell 16%, from 4:22 to 3:42 (221.5 s) over ~20 years; Tauberg (2018) estimated Hot 100 songs lost ~40 s since 2000 (≈4:10 → 3:30). **[snippet]**

**Takeaway for instrumentals [inferred]:** a radio/playlist-oriented hip hop instrumental should land at **2:45–3:20**; the 2:30 floor keeps it a "full song" for sync/leasing; the 3:30 ceiling matches the #1-hit average. Shorter (2:15–2:45) is acceptable for hook-first, playlist-first phonk/trap-pop; longer than 3:30 should only come from a deliberate two-part beat switch.

### 1.2 Intro length and the "hook by 0:30–0:45" / 30-second rule

* **Spotify counts a stream only after 30 seconds**; a skip before 0:30 generates no royalty and is a negative recommendation signal. Guides therefore advise putting a vocal entry, hook, or the most distinctive production element **before the 30-second mark**. (Flightcast; Dynamoi "Spotify 30-second rule"; Plain English) **[snippet]**
* **2014 Spotify skip study:** 48.6% of plays were skipped before the song finished. (Digital Music News, 2014, reporting Paul Lamere's analysis) **[snippet]**
* **Hit Songs Deconstructed (Hot 100 top 10, 2017):** hip hop/rap reached the **first chorus at ~30 s on average vs ~39 s for pop**; hip hop **intros averaged 17 s** (pop 9 s); hip hop first verses averaged 44 s (pop 23 s); by end-2017 **86% of hip hop/rap top-10 songs placed a chorus before the first verse**. ("Hip Hop vs. Pop: Which Hooks You Faster?") **[snippet]**
* **Hit Songs Deconstructed, #1 hits:** average **intro 14 s in 2024, same as 2023**; chorus-before-first-verse has "traditionally been more common in hip hop." (2024 #1 Hit Focus Report) **[snippet]**
* **Counter-trend:** #1 hits with the first chorus **after 1:00 rose to 33% in 2025** (from 6% in 2021), e.g., Kendrick Lamar "Luther," "Not Like Us," HUNTR/X "Golden." These are superstar records; the median song still hooks early. (HSD 2025 #1 Hit Report) **[snippet]**
* **Older HSD data:** 25% of Q4-2014 top-10 songs opened with the chorus; 41% in Q2-2015 when "intro → chorus → verse" is included. **[snippet]**
* **Radio/playlist promo guidance:** music directors often listen to only the first ~15 s (Symphonic); keep intros under 15 s and songs under 4:00 for playlisting (CD Baby DIY Musician); radio-focused tracks sometimes enter the vocal within 3–5 s (StudioPros). **[snippet]**

**Rule for the generator:** Intro **≤8 bars**, default **4 bars** (≈7–11 s at the tempos in §1.4); the **first hook must start by ~0:20–0:35** and never later than **0:45**. For an instrumental, "hook" means the lead melodic/sample motif with full drums and bass in.

### 1.3 Section map with bar counts

Community/guide consensus (emastered "Rap Song Structure"; HipHopMakers "Song Arrangements"; Violet Recording "How to Structure a Beat"; Garnish Music Production; WA Production; MusicRadar) **[snippet]**:

* Verses: **16 bars** (most common across boom bap and trap); 8 or 12 also used.
* Hooks: **8 bars** (a 4-bar hook repeated twice fills the slot); 16-bar hooks exist in trap.
* Intro: **4–8 bars**; sparser; "catches the ear without overshadowing potential vocals."
* Bridge/break: **8 bars**, optional — "bridges are not heard often in most hip hop."
* Outro: **4–8 bars**.
* Verse = simplified beat (room for vocals); hook = fullest arrangement.
* Beat-maker template (HipHopMakers): Intro 8 / Verse 16 / Hook 8 / Verse 16 / Hook 8 / Verse 16 / Hook 8 / Outro 8.
* Garnish's beat/hip hop template: **Intro – Verse – Hook – Verse – Hook – Verse – Hook ×2 – Outro.**

**suno-forge structure templates (bars at the *written* tempo):**

| Template | Sections | Total bars |
|---|---|---|
| A. Radio (hook-first) | Intro 4 / Hook 8 / Verse 16 / Hook 8 / Verse 16 / Bridge-or-Break 8 / Hook 8 / Outro 4 | 72 |
| B. Radio (full) | Intro 8 / Hook 8 / Verse 16 / Hook 8 / Verse 16 / Bridge 8 / Hook 8 / Outro 8 | 80 |
| C. Compact playlist | Intro 4 / Hook 8 / Verse 16 / Hook 8 / Verse 16 / Hook 8 / Outro 4 | 64 |
| D. Type-beat / lease | Intro 8 / Hook 8 / Verse 16 / Hook 8 / Verse 16 / Hook 8 / Hook 8 (2×) / Outro 8 | 88 |

### 1.4 Bars-to-seconds (4/4; seconds per bar = 240 ÷ BPM)

| Bars | 85 BPM | 92 BPM | 100 BPM | 130 BPM | 140 BPM | 150 BPM | 160 BPM |
|---|---|---|---|---|---|---|---|
| 1 | 2.82 s | 2.61 s | 2.40 s | 1.85 s | 1.71 s | 1.60 s | 1.50 s |
| 4 | 0:11.3 | 0:10.4 | 0:09.6 | 0:07.4 | 0:06.9 | 0:06.4 | 0:06.0 |
| 8 | 0:22.6 | 0:20.9 | 0:19.2 | 0:14.8 | 0:13.7 | 0:12.8 | 0:12.0 |
| 12 | 0:33.9 | 0:31.3 | 0:28.8 | 0:22.2 | 0:20.6 | 0:19.2 | 0:18.0 |
| 16 | 0:45.2 | 0:41.7 | 0:38.4 | 0:29.5 | 0:27.4 | 0:25.6 | 0:24.0 |
| 24 | 1:07.8 | 1:02.6 | 0:57.6 | 0:44.3 | 0:41.1 | 0:38.4 | 0:36.0 |
| 32 | 1:30.4 | 1:23.5 | 1:16.8 | 0:59.1 | 0:54.9 | 0:51.2 | 0:48.0 |
| 48 | 2:15.5 | 2:05.2 | 1:55.2 | 1:28.6 | 1:22.3 | 1:16.8 | 1:12.0 |
| 64 | 3:00.7 | 2:47.0 | 2:33.6 | 1:58.2 | 1:49.7 | 1:42.4 | 1:36.0 |
| 72 | 3:23.3 | 3:07.8 | 2:52.8 | 2:12.9 | 2:03.4 | 1:55.2 | 1:48.0 |
| 80 | 3:45.9 | 3:28.7 | 3:12.0 | 2:27.7 | 2:17.1 | 2:08.0 | 2:00.0 |

**Template totals at each written tempo:**

| Template | 85 | 92 | 100 | 130 | 140 | 150 | 160 |
|---|---|---|---|---|---|---|---|
| A. 72 bars | 3:23 | 3:08 | 2:53 | 2:13 | 2:03 | 1:55 | 1:48 |
| B. 80 bars | 3:46 | 3:29 | 3:12 | 2:28 | 2:17 | 2:08 | 2:00 |
| C. 64 bars | 3:01 | 2:47 | 2:34 | 1:58 | 1:50 | 1:42 | 1:36 |
| D. 88 bars | 4:09 | 3:50 | 3:31 | 2:43 | 2:31 | 2:21 | 2:12 |

**Half-time correction (trap, phonk, trap/boom-bap hybrids at 130–160 written BPM).** The kick/snare pattern sits at half the written tempo, so the *felt* pulse is 65–80 BPM and a rapper's "16 bars" is counted at the felt tempo = **32 written bars**. (Violet Recording "What is trap music"; Tidal Magazine "What Makes Trap Trap"; RouteNote "Exploring trap BPMs"; Mixgraph half-time glossary) **[snippet]** Use felt bars for section planning in those lanes:

| Written BPM | Felt BPM | 1 felt bar | 8 felt bars (hook) | 16 felt bars (verse) | 48 felt bars | 56 felt bars | 64 felt bars |
|---|---|---|---|---|---|---|---|
| 130 | 65 | 3.69 s | 0:29.5 | 0:59.1 | 2:57 | 3:27 | 3:56 |
| 140 | 70 | 3.43 s | 0:27.4 | 0:54.9 | 2:45 | 3:12 | 3:39 |
| 150 | 75 | 3.20 s | 0:25.6 | 0:51.2 | 2:34 | 2:59 | 3:25 |
| 160 | 80 | 3.00 s | 0:24.0 | 0:48.0 | 2:24 | 2:48 | 3:12 |

Practical half-time maps **[inferred]**: at 140 written, *Intro 4 / Hook 8 / Verse 16 / Hook 8 / Verse 12 / Hook 8 / Outro 2* (felt bars) = 58 felt bars ≈ **3:19**; a compact *Intro 2 / Hook 8 / Verse 12 / Hook 8 / Verse 12 / Hook 8 / Outro 2* = 52 felt bars ≈ **2:58**. At 85–100 BPM (boom bap, neo-soul) use the written-tempo templates A–C directly: 92 BPM × 72 bars = 3:08 is the sweet spot.

### 1.5 Arrangement contrast and ending conventions

**Contrast devices** (EDM-oriented sources, transferable; Point Blank "Tension and release"; Baby Audio "Buildups and beat drops"; Future Music "6 tricks for adding movement"; Ableton forum "Transitions/Build ups"; HipHopMakers) **[snippet]**:
* **Drop-outs/mutes:** pull the drums or the 808/sub for the last 1–2 bars before a hook; remove non-essential layers right before the drop so the re-entry hits harder.
* **Filter sections:** high-pass sweep on the master or on drums/pads across 2–4 bars into the hook, snapping open on the downbeat; low-pass "underwater" verse intros.
* **Verse vs hook density:** verse = stripped beat (space for vocals); hook = fullest layer count; add a counter-melody or octave layer on hook 2/3.
* **Risers/fills:** drum fill, kick roll, reverse cymbal, or riser in the final bar of a section.
* **Energy map:** draw the arc first and hold back elements until later (see §4).
* **Beat switches:** two-part songs ("Sicko Mode," Kendrick "DNA.", J. Cole, Tyler) are a hip hop signature; XXL/Complex/DJBooth maintain "best beat switch" lists; WKNC compares the switch to the EDM drop as the payoff moment; execution quality ("fewer flaws") is what makes them stick. **[snippet]** For sync and leasing, a switch should land on a clean bar line and at a stable key relationship so editors can cut either half (see §5).

**Endings:**
* **Fade-outs have nearly vanished** from hits: among 1985 year-end top-10 songs there was not one cold ending; the technique has "now almost completely disappeared" (William Weir via NPR; A Journal of Musical Things; Production Expert). Owsinski's informal survey: most current hits have hard endings. **[snippet]**
* Radio edits either cut before the ending or fade early when necessary (Wikipedia "Radio edit"). Radio singles were sometimes labelled "Close cold" vs "Close fade" (Straight Dope forum, anecdotal). Cold endings reduce dead air and simplify segues to IDs/ads. **[snippet]**
* **Library/sync preference:** a "solid final ending with a long ring-out" and a stinger/button ending that can close the piece anywhere. (Music Library Report forums) **[snippet]**
* **Default for suno-forge [inferred]:** cold/hard stop on the downbeat of the final bar with a 1–2 s tail (reverb/808 ring-out). Offer "fade" only as a variation, and for sync lanes offer "button ending."

### 1.6 Beat-leasing / type-beat market conventions

* Buyers are artists writing to the beat, so **clear verse and chorus sections with obvious vocal drop-in points** are the priority; titles pair a reference artist with a mood/tempo ("[Artist] type beat"). (Violet Recording "How to sell beats online") **[snippet]**
* **Producer tags:** one tag within the first 4–8 bars; repeat roughly every 20–40 s, plus at the intro and the drop, loud enough to deter theft but not ruin the listen; tagged preview vs untagged licensed file. (Violet Recording "How to tag your beats"; "How to add a producer tag") **[snippet]**
* **Structure:** intro / verse / hook / verse / hook / verse / hook / outro, often with **hook ×2 at the end** (Garnish template), 16-bar verses and 8-bar hooks, intros that tease the hook melody. **[snippet]** Lease stores commonly list hook/verse timestamps in descriptions **[unverified — common practice, no source retrieved]**.
* **Implication for Suno output [inferred]:** use explicit section tags ([Intro], [Hook], [Verse], [Hook], [Verse], [Hook], [Hook], [Outro]); keep verse beds sparse; make the hook melody appear briefly in the intro.

---

## 2. Loudness and mastering vocabulary

### 2.1 Loudness targets and ceilings

| Context | Integrated LUFS | True peak | Source |
|---|---|---|---|
| Spotify playback normalization (default "Normal") | −14 LUFS (ITU-R BS.1770); "Loud" −11; "Quiet" −19 | Spotify's own limiter engages at −1 dB sample peak for quiet tracks | Spotify for Artists support, "Loudness normalization" **[snippet]** |
| Spotify mastering advice | target −14 integrated | ≤ −1 dBTP; **≤ −2 dBTP if master is louder than −14 LUFS** (lossy-codec distortion) | same |
| Apple Music Sound Check | ≈ −16 LUFS (AES TD1008; switched to LUFS and on-by-default in 2022; third-party measure ≈ −15.83) | Apple Digital Masters: avoid clipping at 0 dBFS; Apple Podcasts spec −16 LKFS ±1, −1 dBTP | Production Expert (2022); MeterPlugs (2022-03-23); Apple Digital Masters PDF; Apple Podcasts audio requirements **[snippet]** |
| YouTube | ≈ −14 LUFS reference (since 2019); turns down, generally does not boost | — | MeterPlugs (2019-09-18); Production Advice "Stats for Nerds" **[snippet]** |
| Amazon Music / Tidal / Deezer | −14 / −14 / −15 (secondary sources; Amazon sometimes quoted at −2 dBTP) | −1 to −2 dBTP | raespoon; MasteringBOX; Orphiq **[snippet, unverified vs. platform docs]** |
| iZotope general streaming advice | "at least a little above −14 LUFS"; genre-dependent; drums hit the limiter first in rhythm-heavy genres, so don't over-push hip hop (Jonathan Wyner) | −1 dBTP max | iZotope "Mastering for streaming platforms"; "Your mastering questions answered" **[snippet]** |
| Sonarworks | −14 is "somewhat quiet for most commercial music"; suggested −10 to −16; a −8 LUFS master is turned down ≥4 dB | −1 dBTP (−2 dBTP for loud masters on Spotify) | Sonarworks "What level should I master for streaming" **[snippet]** |
| LANDR | "loudness penalty" overstated; −14 is a reference not a goal; support page says don't exceed −14; lists Spotify/YouTube −14, Apple −16 | — | LANDR "Loudness Penalty Debunked"; LANDR support **[snippet]** |
| **Hip hop / trap releases in practice** | **−8 to −10** (Waves; Violet Recording); **−10 to −12** (Plugin Alliance); **−7.5 to −8.5 "hard rap"** (KVR forum); **−6 to −10** "loudness-forward genres" (raespoon); measured **−7.3 to −9.4** for hip hop/trap (Orphiq, internally inconsistent) | −1 dBTP typical; −0.3 to −0.5 on some loud masters **[unverified]** | see sources §7 **[snippet]** |
| Radio broadcast | Not a LUFS target for the producer: station chains apply heavy multiband compression/limiting; over-squashed or peaky masters pump or splatter (BMI "Mixing your music for radio broadcast") | — | **[snippet]** |

**Working defaults [inferred]:** phonk/trap/trap-pop master −8 to −7 LUFS, −1 dBTP; boom-bap/hybrid −9 LUFS; neo-soul/live funk −11 to −10 LUFS to preserve transients. All get normalized to −14 on Spotify anyway; the louder masters trade dynamic range for density, which is the genre expectation.

### 2.2 What "radio-ready" means to an engineer

No formal technical standard exists; engineers and marketing use the phrase to mean a finished, consistent master that **competes with major-label releases and translates across playback systems**, with loudness as one factor. (ProSoundWeb "Mastering as another mix layer"; Making A Scene; Disc Makers; home-recording forums calling it "a myth apart from track length") **[snippet]** Component properties, with sources where available:

* **Balanced tonal curve** against a genre reference (iZotope Tonal Balance "Bass Heavy — EDM, Hip Hop" curve) **[snippet]**.
* **Controlled, mono-compatible low end:** high-pass the side channel so everything below ~100–130 Hz is mono; sub ≈20–60 Hz; make the sub mono *before* adding harmonics; mono bass "ensures playability across multiple speaker systems" (Sage Audio, several articles) **[snippet]**. Big low end eats limiter headroom — the central hip hop mastering problem (Splice "How to master hip hop") **[snippet]**.
* **Clear transients:** rhythm-heavy genres should not be pushed as hard because the drums hit the limiter first (iZotope/Wyner) **[snippet]**.
* **Stereo correlation** between roughly +0.5 and +0.75 for a wide but compatible mix (iZotope correlation meter guidance) **[snippet]**.
* **Tamed highs / de-essing** on hats and vocal-like leads so radio multiband processing doesn't splatter (BMI) **[snippet; the de-essing point is inferred]**.
* **Glue** (bus compression) and consistent section-to-section level **[inferred, standard practice]**.
* **Survives broadcast processing**: leave the limiter some headroom; the station will add its own (BMI) **[snippet]**.

### 2.3 Descriptor vocabulary for a text-to-music model

Terms organised by what they ask for. Those in **bold** appear in Suno-community prompt guides as "production quality" cues.

| Category | Descriptors |
|---|---|
| Finish | **mastered**, **professional mastering**, **polished mix**, **radio-ready**, **broadcast-ready**, **hi-fi**, release-ready, major-label quality, clean mix |
| Loudness / density | **loud**, dense, **punchy**, slamming, in-your-face, **club-ready**, competitive loudness |
| Low end | **clean low end**, **tight sub**, controlled 808, deep sub bass, mono bass, round bass, no mud |
| Transients / drums | **punchy drums**, **crisp transients**, snappy snare, hard-hitting kick, tight drums, knocking drums |
| Width / space | **wide stereo**, wide mix, spacious, centered low end, 3D, immersive |
| Tone | **warm**, bright, crisp, airy highs, smooth highs, de-essed, analog warmth, tape saturation |
| Cohesion | **glued**, cohesive, balanced, well-mixed, dynamic (for neo-soul lanes) |
| Lo-fi (opposite pole, for exclusions) | lo-fi, tape hiss, muddy, distorted, unpolished, bedroom demo |

**Evidence on which of these Suno honors (community/vendor only; no controlled tests found; Reddit was not reachable from this session):**
* r/SunoAI consensus summarised on Medium: structure the style prompt as **genre → exclusions → instruments → tags**; **concrete instruments beat vague adjectives** ("energetic," "cool" "tell Suno almost nothing"); keep the style prompt under ~100 words. **[snippet]**
* HookGenius: "polished mix, professional mastering, radio-ready, hi-fi quality, punchy drums, wide stereo" presented as effective quality terms; style field ≈1,000 chars on v5/v5.5 vs ≈200 on v4; overflow silently truncated. (Vendor; unverified.) **[snippet]**
* roo.beehiiv "Why v5/v5.5 ignores half your words": prompt is compressed into a lossy conditioning vector dominated by the "loudest" tags; put production descriptors last; earlier tokens weigh more. (Practitioner claim.) **[snippet]**
* Version-comparison guides: **v5.5 tends to over-polish** raw genres; adding "lo-fi tape warmth," "unpolished" counteracts it — i.e., the model does respond to finish descriptors, in both directions. jackrighteous: "lo-fi tape hiss" vs "clean, radio-ready mix" yields audibly different outputs from otherwise identical prompts. **[snippet]**
* Several services sell post-generation mastering for Suno output to −14 LUFS (RoEx; AirGigs), implying actual loudness/true-peak compliance is **not** reliably achieved by prompt words alone. **[snippet]**
* **Net [inferred]:** expect "polished / radio-ready / punchy drums / wide stereo / tight sub / clean low end / warm" to shift *timbre and perceived finish*; do not expect a specific LUFS. Keep production words to 3–5 tokens at the end of the style string; spend the budget on instruments and groove.

---

## 3. Tempo and key tables per lane

**Caveat on key data:** Tunebat/Musicstax/SongBPM/Mixgraph use automatic key detection; they frequently report the *relative major* of a dark minor track (e.g., "B♭ major" for a G-minor phonk beat, "A♭ major" for F minor, "C major" for A minor) and disagree with each other on several songs below. Treat the major/minor label as unreliable and the **pitch class** as roughly reliable. Mixed In Key's own 2025 benchmark puts its best-in-class detector at 85.2% accuracy **[snippet]**.

### 3.1 Phonk (original / drift / experimental)

* **Published ranges:** 130–170 BPM overall (Melodics); drift phonk 140–160, OG/classic 130–145, Memphis 130–150, house phonk 125–135 (Violet Recording; Orphiq; BeatsToRapOn; Epidemic Sound) **[snippet]**. The kick/snare plays at half the BPM, so the felt groove is **65–80 BPM** **[snippet]**.
* **Measured hits (Tunebat/Musicstax/SongBPM) [snippet]:** Kordhell "Murder In My Mind" **120 BPM**, B♭ major (≈ G minor); Kordhell "Scopin" 98 BPM, A♭ major (≈ F minor); Dxrk "RAVE" **120 BPM**, A♭ major (≈ F minor); Hensonn "Sahara" 115 BPM, D major (≈ B minor); DVRST "Close Eyes" 96 BPM, C major (≈ A minor); ONIMXRU/SMITHMANE "SHADOW" 120 BPM; INTERWORLD "METAMORPHOSIS" 175 BPM (detector likely doubled ≈ 87.5), G minor. **Observation:** the TikTok-era drift-phonk hits cluster at **115–125 BPM written**, below the 130–150 blog range; ask Suno for 120–135 for "viral drift" feel and 140–150 for aggressive/"experimental" phonk.
* **Keys:** qualitative sources say nearly all phonk lives in natural minor, often a slice of it, with Phrygian motifs for drift (Melodics; Violet Recording) **[snippet]**; the measured sample above resolves to **G minor, F minor, B minor, A minor** (as relative minors). Use **C minor, D minor, F minor, G minor, G♯/A♭ minor, B minor**; the requested "C minor / D minor / F minor / G♯ minor" set is consistent with the qualitative sources but **no dataset-level phonk key distribution was found [unverified]**.

### 3.2 Anderson .Paak-style live neo-soul / funk hip hop

* **Tempo:** 85–100 BPM target. Measured .Paak catalog [snippet]: "Come Down" **98**; "Lockdown" **100**; "Bubblin" **107**; "Tints" **109**; "Am I Wrong" **115**; "Make It Better" 179 (≈ 89.5 half); Mixgraph's catalog page lists him around 99 BPM on "Come Down." His singles skew **95–110**, so widen the lane to **88–110** with 92–98 as the pocket.
* **Keys/modes:** detectors give "Come Down" A♭ major (Tunebat) vs B♭ minor (Music Gateway) vs A♯/B♭ minor (SongBPM) — illustrating the detector problem; "Tints" C♯ minor; "Lockdown" C♯ minor; "Am I Wrong" B minor; "Bubblin" C♯ major; "Make It Better" E minor. **[snippet]**
* **Harmonic language:** jazz-derived extensions — 7ths, 9ths, 11ths, 13ths (emastered "Neo Soul Chord Progressions"); extended minor progressions (e.g., i–v–iv with m7 then m9 voicings) typify D'Angelo/Badu/Maxwell (Hear and Play); **Dorian** mode chords Dm7–Em7–FMaj7–G7–Am7–Bm7♭5–CMaj7 and m7/m9/m11/m13 vamps (D Dorian reference, associates D'Angelo); minor i–iv–v loops (Music Gateway). **[snippet]**
* **Recommended key set [inferred from the above]:** E♭ major / C minor, F major / D minor, A♭ major / F minor, plus B♭ minor and C♯ minor (which actually appear in his catalog); default **Dorian or minor-7/9 vamps** in **D Dorian, C minor, F minor, C♯ minor**.

### 3.3 Trap / trap-pop (Connor Price-style bouncy pop-rap)

* **Trap tempo:** written **130–140 most common**, 130–180 wide, felt 65–90 (Violet Recording; Tidal; RouteNote; SampleFocus "Everything about 140 BPM") **[snippet]**. 140 written / 70 felt is the canonical grid because the faster grid gives precision for hi-hat rolls **[snippet]**.
* **Connor Price measured (Tunebat) [snippet]:** "Spinnin" (with Bens) **140 BPM, B minor**; "Buddy" 140, F♯ minor; "GOLDEN GOOSE" 130, A major; "Don't Tell Me" 114, A♯/B♭ minor; "EGOT" 105, F♯ minor; "DIP" 100, C♯ minor; "Trendsetter" 95, F minor; "AMG" 95, B♭ major; "It's Going Down" 90, C major; "You Said" 80, C major. **Observation:** his catalog is bimodal — **half-time trap at 130–140 written** and **straight-time bounce at 90–105** (which is the same felt pulse range as 140 half-time ≈ 70 ×1.3). The requested "140–160 half-time" is right for the trap half; add a **92–105 straight-time** option for the bouncy pop-rap half.
* **Keys:** Mixed In Key's trap top-10 analysis names **G♭/F♯ minor, F minor, B major** most frequent and says ~60% sit on F or G (figures inconsistent across its pages); Unison (uncited) lists **E minor, B minor, F minor** as most popular trap keys; Mixed In Key: "no universal best key," suggests Fm–D♭–E♭–Fm as a starting loop **[snippet]**. Connor Price sample: **B minor, F♯ minor, C♯ minor, F minor, B♭ minor**. Requested set (D minor, F minor, G minor, C♯ minor) is plausible; add **B minor, F♯ minor**.

### 3.4 Dark boom-bap / trap hybrid (JID-style)

* **Boom bap tempo:** **85–95 BPM** core, 80–100 wide, 90 the common default; NI tutorial at 93; Strudel example 92; some West Coast 80–90, early Def Jam 98–105 (Violet Recording; MusicMetricsVault; RouteNote 86–100; Native Instruments; Gearspace) **[snippet]**.
* **JID measured (Tunebat/Musicstax/SongBPM/Mixgraph) [snippet]:** "151 Rum" **130 BPM**, G major (Mixgraph: E minor); "Dance Now" **135**, C♯ major; "Surround Sound" **76 (152 double-time)**, F major / G minor / F minor (sites disagree); "Off Deez" **85**, A major; "Stick" 119, G major; "NEVER" 104, C♯ major; "Lisa" 150, C♯ major. **Observation:** the hybrid lane really spans **85 (boom bap) / 104–119 (mid) / 130–152 (trap)**; for Suno give **88–95 straight** or **140–150 half-time (70–75 felt)**, and for a two-part beat switch pair 90 ↔ 135–150 so the felt pulse relationship is near 4:3 or 2:1.
* **Keys:** detector output above resolves to E/G, C♯, F/G, A, G — dark minor equivalents **E minor, C♯ minor, F minor, G minor, A minor**; requested set should include **E minor, G minor, C minor, F minor, C♯ minor** **[inferred]**.

### 3.5 General tables

**Most common keys — hip hop / trap (best available evidence; no genre-wide dataset was retrievable this session):**

| Rank-ish | Key | Evidence |
|---|---|---|
| 1 | F minor | Mixed In Key trap top-10; Unison; Mixgraph "Phonon" sample; Connor Price "Trendsetter"; Fm–D♭–E♭ cited as the stock trap loop **[snippet]** |
| 2 | F♯/G♭ minor | Mixed In Key trap top-10 (top key); Connor Price "EGOT," "Buddy" **[snippet]** |
| 3 | C♯/D♭ minor | .Paak "Tints," "Lockdown"; Connor Price "DIP"; JID "Dance Now"/"NEVER" (as relative) **[snippet]** |
| 4 | G minor | Mixed In Key (~60% on F or G); Kordhell "Murder In My Mind" (relative); JID "151 Rum"/"Stick" (relative) **[snippet]** |
| 5 | E minor | Unison; Mixgraph "151 Rum"; .Paak "Make It Better" **[snippet]** |
| 6 | B minor | Unison; Connor Price "Spinnin"; .Paak "Am I Wrong"; Hensonn "Sahara" (relative) **[snippet]** |
| 7 | A minor / C major | Hooktheory: A minor is the most popular minor key (7th overall) and C major #1 overall (5,920 of >75,000 TheoryTabs; D major 4,712; G major 4,478) — pop-wide, not hip hop-specific **[snippet]** |
| 8 | B♭/A♯ minor | Mixed In Key (20% of a trap sample in B♭); .Paak "Come Down" (one detector); Connor Price "Don't Tell Me" **[snippet]** |
| — | G♯ minor | Hooktheory: least popular minor key (24th overall) — yet common in phonk/trap by ear **[snippet; unverified]** |
| — | Minor share | Artist-level Mixgraph samples: Rich The Kid 80% minor, Schoolboy Q 67% minor, "Phonk D" 73% minor (small n) **[snippet]** |

**Most common BPMs — hip hop lanes:**

| Lane | Written BPM | Felt BPM | Evidence |
|---|---|---|---|
| Boom bap / golden era | 85–95 (90 default) | same | Violet Recording; MusicMetricsVault; RouteNote; NI **[snippet]** |
| Historic hip hop range | 85–115 | same | Musiio/Digital Music News RapCaviar analysis (2020) **[snippet]** |
| Modern streaming rap (RapCaviar, 2020) | 74% of tracks above 115 (i.e., half-time trap grids) | 65–90 | same **[snippet]** |
| Hip hop chart average (weak source) | avg 106, range 77–150 | — | SignalKey **[snippet]** |
| Neo-soul / live funk hip hop | 88–110 (92–98 pocket) | same | .Paak catalog (Tunebat) **[snippet]** |
| Trap | 130–140 core; 130–180 wide | 65–90 | Violet; Tidal; RouteNote; SampleFocus **[snippet]** |
| Trap-pop bounce | 90–105 straight or 130–140 half-time | 70–105 | Connor Price catalog (Tunebat) **[snippet]** |
| Phonk (blog ranges) | 130–150 OG/Memphis; 140–160 drift; 125–135 house phonk | 65–80 | Melodics; Violet; Orphiq; Epidemic **[snippet]** |
| Phonk (measured hits) | 96–125 (cluster 115–120) | 58–62 or straight | Tunebat/Musicstax **[snippet]** |
| Boom-bap/trap hybrid | 85 / 104–119 / 130–152 | — | JID catalog **[snippet]** |

---

## 4. Energy-arc templates (section-by-section, 1–10)

Method sources: draw an energy line over time and avoid a straight ramp (Secrets of Songwriting); score each section 1–5 for emotional intensity, not volume (jackrighteous "Section energy mapping"); separate structure from arrangement with a per-section element grid (Orphiq); contrast is what keeps arrangements engaging (MusicProductionWiki); the energy curve need not rise — it should suit the genre (DeviantNoise). **[snippet]** The five templates below are this memo's synthesis **[inferred]**.

| Template | Intro | Hook 1 | Verse 1 | Hook 2 | Verse 2 / Bridge | Hook 3 (final) | Outro | Best for |
|---|---|---|---|---|---|---|---|---|
| **1. Build–release** (classic) | 3 | 7 | 5 | 8 | 4 (bridge/break: drop-out) → riser | 10 | 4 → stop | Radio, playlist; boom bap, neo-soul |
| **2. Flat-bang / hook-first** | 6 (hook motif, filtered) | 9 | 6 | 9 | 6 | 10 (+layer) | 7 → cold stop | Playlist/TikTok phonk, trap-pop; passes the 30-s rule by default |
| **3. Slow burn** | 2 | 4 (motif only, no drums or half drums) | 5 | 7 | 6 | 9 | 8 → long ring-out | Sync/underscore, cinematic trap, dark hybrid; **not** for radio unless intro ≤8 bars and drums enter by 0:30 |
| **4. Stop–start** | 5 | 8 | 4 (mutes every 4 bars) | 8 | 2 (full drop-out 2 bars) → 8 | 9 | 1 (hard stop) | Trap, phonk; club/DJ; very editor-friendly for sync because of natural cut points |
| **5. Two-part beat switch** | 3 | 7 | 5 | 8 | **switch at ~55–60%**: new beat enters at 6, new hook 9 | 10 | 5 → stop | Album-style trap/hybrid (JID lane); long-form sync; use when runtime >3:15 is wanted |

Guidance **[inferred from §1 and §5]**:
* **Radio:** templates 1 or 2; first ≥7 energy moment by 0:30; final hook is the peak; cold ending.
* **Playlist/algorithmic:** template 2 (hook first), compact 64-bar map, 2:30–3:00.
* **Sync/library:** templates 3 or 4 with clearly stepped energy plateaus every 8 bars, bar-aligned drop-outs for edit points, button/stinger ending, and alt mixes (§5); also deliver a 60/30/15 cutdown of the peak section.
* **Beat-leasing:** template 1 or 2 with hook ×2 at the end; keep verse energy ≤6 so vocals carry the section.

---

## 5. Sync / library-music guidance relevant to instrumentals

* **No uncleared samples:** libraries reject tracks with uncleared samples and require "one-stop," fully controlled rights (Audiosocket); samples only if 100% cleared for commercial distribution (Storyblocks); undisclosed samples are the most common reason distributors pause a release (Interspace); "unresolved samples" is a frequent sync-review failure (Crossfadr, 2025-12) **[snippet]**. For Suno output this means: no interpolations of recognisable melodies, and keep provenance notes for AI generation (jackrighteous 2026 sync/AI guide notes gatekeepers increasingly ask) **[snippet]**.
* **Deliverables package:** full mix, clean version (n/a for instrumentals), **instrumental**, and **stems**, exported from the same session so they line up (Ted Lowe/Choicetracks via Hypebot & BMI; mn2s; Bandzoogle; Lalal.ai) **[snippet]**. Supervisors want no-vocal/"no lead" versions so dialogue or VO sits on top (Soundfly; Alibi) **[snippet]**.
* **Alt mixes commonly requested:** instrumental, **no-lead/no-melody**, **drums-and-bass only ("bed")**, **no-drums**, no-FX, **loopable 15–30 s stinger/loop**, alt **stinger endings** (Music Library Report forums; Xposure Music; Audiosocket "Instrumentals and Alts") **[snippet]**. This maps directly onto suno-forge's "Variations" output: generate (a) main, (b) no-lead bed, (c) drums+bass bed, (d) drumless/ambient, (e) 30-s loop/stinger.
* **Cutdowns:** full / **60 / 30 / 15** plus bed; write endings to land on the :30 and :10 marks about two seconds before the cut-down ends (TAXI forum); UK ad versions require silence at both ends (a :30 spot = 29 s of music; a :10 = max 9 s) (Spitfire); don't assume a 60-s cue works at 15 s (Film Editing Pro) **[snippet]**.
* **Clean edit points:** build in natural pauses/breaks between sections so editors can cut or stop the track cleanly; rhythmic styles take frequent edit points well; a solid final ending with a long ring-out plus a stinger that can end the piece anywhere (Music Library Report "Cue Tips"; forums; Sound On Sound "Library Work") **[snippet]**.
* **Consistent key and tempo:** keep one key and one tempo per cue so any 8-bar block can be looped or spliced; if a beat switch is used, deliver each half as its own cue **[inferred — no explicit source retrieved]**.
* **Dynamics:** broadcast/streaming buyers want loud-but-dynamic mixes with headroom, not heavily limited masters; stems must be unmastered or consistently processed (Horus Music/Symphonic/mn2s) **[snippet]**. Deliver sync masters nearer **−12 to −10 LUFS, −1 dBTP** than the −8 "club" master **[inferred]**.
* **Metadata:** BPM, key, mood, instrumentation, and "instrumental" flag in filename/ID3 (mn2s "sync-ready catalog") **[snippet]**.

---

## 6. Open items / could not verify this session

* Direct page reads were blocked for nearly all hosts (dev.to, support.spotify.com, izotope.com, hooktheory.com, hitsongsdeconstructed.com, mixgraph.io, mixedinkey.com, masteringthemix.com, violetrecording.com, emastered.com, alibimusic.com, musiclibraryreport.com, soundfly.com, hypebot.com, digitalmusicnews.com, hookgenius.app, chartmetric.com); everything above is from search snippets. The web-search budget was then exhausted before the final confirmation pass.
* **No genre-wide key-frequency dataset for hip hop, trap, or phonk** was found; the key tables are triangulated from Mixed In Key marketing pages, small artist samples, and Hooktheory's pop-wide ranking. Hooktheory's full minor-key ranking (counts for E/D/F/C♯ minor) was not retrieved.
* **No 2025 hip hop-specific intro/first-chorus averages** (HSD only published #1-hit and 2017 hip hop figures).
* **Phonk BPM discrepancy** (blogs 130–160 vs measured hits 96–125) was not resolved against a larger sample.
* **Suno behaviour** for mastering descriptors rests on vendor and practitioner claims; Reddit is not crawlable from this tool. A controlled A/B (same prompt ± "mastered, punchy, wide stereo", measured in LUFS/spectrum) is recommended.
* YouTube's and Amazon's current official loudness statements were not retrieved from first-party pages.
* "Hook/verse timestamps in lease listings" and "2× hook at the end" as a *market norm* rest on one template (Garnish) and general practice.

---

## 7. Sources (URL; date of publication where known; accessed 2026-10-31)

### Song length, intro, hook timing
1. Wang, T. "Are songs really getting shorter? 6,618 Hot 100 hits 1959–2025." dev.to, 2026. https://dev.to/tonywangca/are-songs-really-getting-shorter-6618-hot-100-hits-1959-2025-4cdg
2. Hit Songs Deconstructed. *2025 #1 Hit Report* (PDF), Feb 2026. https://www.hitsongsdeconstructed.com/wp-content/uploads/2026/02/Hit-Songs-Deconstructed-2025-1-Hit-Report.pdf
3. Hit Songs Deconstructed. "What Did 2025's Longest-Charting Songs Have in Common?" 2026. https://www.hitsongsdeconstructed.com/hsd_wire/what-did-2025s-longest-charting-songs-have-in-common/
4. Hit Songs Deconstructed. "Hip-Hop Was on Top & Songs Got Longer — 2024 No. 1 Hit Focus Report." 2025. https://accelerator.hitsongsdeconstructed.com/uncategorized/hip-hop-was-on-top-songs-got-longer-hit-songs-deconstructeds-2024-no-1-hit-focus-report-reveals ; highlights PDF: https://www.hitsongsdeconstructed.com/wp-content/uploads/2025/02/The-Hit-Songs-Deconstructed-1-Hit-Focus-Report-Highlights.pdf
5. Hit Songs Deconstructed. "Hip Hop Vs. Pop: Which Hooks You Faster?" (2017 data). https://reports.hitsongsdeconstructed.com/articles/hip-hop-vs-pop-which-hooks-you-faster/
6. Hit Songs Deconstructed. "Hit Song Choruses" / Q4-2014 & Q2-2015 Top 10 Deconstructed highlights. https://reports.hitsongsdeconstructed.com/articles/hit-song-choruses/ ; https://reports.hitsongsdeconstructed.com/articles/top-10-deconstructed-report-highlights-q2-2015/
7. Hit Songs Deconstructed. *The State of the Hot 100 Top 10 — 2025 Year-End Report Highlights* (PDF), Jan 2026. https://www.hitsongsdeconstructed.com/wp-content/uploads/2026/01/The-State-of-the-Hot-100-Top-10-2025-Year-End-Report-Highlights.pdf
8. Chartmetric. *Year in Music 2024.* https://reports.chartmetric.com/2024/chartmetric-year-in-music-2024
9. Chartmetric. "Are Songs Actually Getting Shorter? It's Not That Simple." https://hmc.chartmetric.com/shorter-songs-trend-streaming-history/
10. MIDiA Research. "How the DNA of a hit has changed over 20 years." https://www.midiaresearch.com/blog/how-the-dna-of-a-hit-has-changed-over-20-years
11. Hypebot. "Songs are getting shorter and the surprising truth behind it." Apr 2025. https://www.hypebot.com/hypebot/2025/04/songs-are-getting-shorter-and-the-surprising-truth-behind-it.html
12. t2online. "Note deflation: is social media forcing musicians to record shorter songs?" (cites Tauberg 2018). https://t2online.in/music/western/note-deflation-is-social-media-forcing-musicians-to-record-shorter-songs-/1454705
13. Flightcast. "Understanding Spotify streams vs plays." https://knowledge.flightcast.com/en/article/understanding-spotify-streams-vs-plays-a-complete-guide-1t8xnjn/
14. Dynamoi. "Spotify 30-second rule explained." https://dynamoi.com/da/learn/playlist-pitching/spotify-30-second-rule-explained
15. Plain English. "Streaming royalties are changing how popular songs are made." https://plainenglish.com/number/207/
16. Digital Music News. "48.6% of all people skip a song before it finishes." 2014-05-06. https://www.digitalmusicnews.com/2014/05/06/people-skipping-songs/
17. CD Baby DIY Musician. "4 editing tips to make your song more attractive to playlisters." https://diymusician.cdbaby.com/music-promotion/make-your-songs-more-playlist-friendly-with-these-four-editing-tricks/
18. Symphonic. "5 tips to help you get radio airplay." 2018-05-23. https://blog.symphonic.com/2018/05/23/5-tips-to-help-you-get-radio-airplay/
19. StudioPros. "Songwriting Tip: Intros." https://ns2.studiopros.com/songwriting-tip-intros
20. Hypebot. "Why you should create a radio edit version of your single." https://www.hypebot.com/why-you-should-create-a-radio-edit-version-of-your-single/

### Structure, arrangement, type beats, endings
21. eMastered. "Rap Song Structure: Complete Guide with Examples." https://emastered.com/blog/rap-song-structure
22. HipHopMakers. "How to Make Beats — Song Arrangements." https://hiphopmakers.com/how-to-make-beats-song-arrangements
23. Violet Recording. "How to Structure a Beat." https://violetrecording.com/how-to-structure-a-beat/
24. Violet Recording. "How to Sell Beats Online." https://violetrecording.com/how-to-sell-beats-online/
25. Violet Recording. "How to Tag Your Beats." https://violetrecording.com/how-to-tag-your-beats/ ; "How to Add a Producer Tag." https://violetrecording.com/how-to-add-a-producer-tag-to-your-beats/
26. Garnish Music Production School. "Mixtape — arrangement templates (Beats or Hip-Hop: Intro–Verse–Hook–…–Hook ×2–Outro)." https://www.garnishmusicproduction.com/mixtape/
27. WA Production. "Arrangement, Song and Structure Tips." https://blog.waproduction.com/arrangement-song-and-structure-tips
28. MusicRadar. "Anatomy of an arrangement: your guide to song sections." https://www.musicradar.com/how-to/song-sections-explained-intro-verse-chorus-middle8-outro-tag-bridge
29. Point Blank Music School. "Creating tension and release in electronic dance music." https://www.pointblankmusicschool.com/blog/creating-tension-and-release-in-electronic-dance-music/
30. Baby Audio. "Buildups and beat drops." https://babyaud.io/blog/buildups-and-beat-drops
31. Future Music (via PressReader). "6 tricks for adding movement and tension to your arrangement." 2022-02-08. https://www.pressreader.com/australia/future-music-9629/20220208/282059100398063
32. Ableton Forum. "Transitions/Build ups" (energy map advice). https://forum.ableton.com/viewtopic.php?p=1359581
33. XXL. "Hip-Hop Beat Switches." https://www.xxlmag.com/hip-hop-beat-switches/
34. Complex. "The 25 Best Hip-Hop Beat Switches of All Time." https://www.complex.com/music/a/dimassanfiorenzo/best-hip-hop-beat-switches-of-all-time
35. WKNC. "Beat switches" (compares to EDM drop). https://wknc.org/?p=80321
36. Wikipedia. "Sicko Mode." https://en.wikipedia.org/wiki/Sicko_Mode
37. Wikipedia. "Radio edit." https://en.wikipedia.org/wiki/Radio_edit
38. NPR. Interview with William Weir on the fade-out (transcript). https://www.npr.org/transcripts/349261382
39. A Journal of Musical Things. "The fade of the fade in popular music." https://www.ajournalofmusicalthings.com/fade-fade-popular-music/
40. Production Expert. "Three elements that have disappeared from modern music production." https://www.production-expert.com/production-expert-1/three-elements-that-have-disappeared-from-modern-music-production
41. Owsinski, B. "Streaming song structure." https://bobbyowsinskiblog.com/streaming-song-structure/
42. Straight Dope forum. "Radio fade-out" ("Close cold"/"Close fade"). https://boards.straightdope.com/t/radio-fade-out/713301

### Loudness, mastering, "radio-ready"
43. Spotify for Artists. "Loudness normalization." https://support.spotify.com/us/artists/article/loudness-normalization/ ; "Track not as loud as others?" https://support.spotify.com/us/artists/article/track-not-as-loud-as-others/
44. Apple. *Apple Digital Masters* (PDF). https://www.apple.com/apple-music/apple-digital-masters/docs/apple-digital-masters.pdf ; Apple Podcasts audio requirements (−16 LKFS, −1 dBTP). https://podcasters.apple.com/support/893-audio-requirements
45. Production Expert. "Apple choose −16 LUFS loudness level for Apple Music — here's why." 2022. https://www.production-expert.com/production-expert-1/apple-choose-16lufs-loudness-level-for-apple-music-heres-why
46. MeterPlugs. "Apple switch to LUFS." 2022-03-23. https://www.meterplugs.com/blog/2022/03/23/apple-switch-to-lufs.html ; "YouTube changes loudness reference to −14 LUFS." 2019-09-18. https://meterplugs.com/blog/2019/09/18/youtube-changes-loudness-reference-to-14-lufs.html
47. Production Advice. "Stats for Nerds — YouTube loudness." https://productionadvice.co.uk/stats-for-nerds/
48. iZotope. "How to master for streaming platforms: normalization, LUFS, and loudness." https://www.izotope.com/community/blog/mastering-for-streaming-platforms ; "Your Audio Mastering Questions, Answered" (Wyner on hip hop). https://www.izotope.com/en/learn/we-are-listening-your-mastering-questions-answered.html ; "A LUFS and tonal analysis of top-charting tracks from emerging artists." https://www.izotope.com/community/blog/emerging-artists-analysis
49. Sonarworks. "What Level Should I Master for Streaming?" https://www.sonarworks.com/blog/learn/what-level-should-i-master-for-streaming
50. LANDR. "The Loudness Penalty Debunked." https://www.landr.com/loudness-penalty ; LANDR Support. "Why does my song sound quieter on streaming platforms?" https://support.landr.com/hc/en-us/articles/4419027207447-Why-does-my-song-sound-quieter-on-streaming-platforms
51. Mastering The Mix. "Mastering audio for SoundCloud, Apple Music, Spotify, Amazon Music and YouTube." https://masteringthemix.com/blogs/learn/76296773-mastering-audio-for-soundcloud-itunes-spotify-and-youtube
52. Waves. "How many LUFS should my master be?" https://www.waves.com/how-many-lufs-should-my-master-be
53. Plugin Alliance. "Which LUFS level should you mix and master to?" https://plugin-alliance.com/en/blog/blogpost/items/mixing_and_mastering_tips_how_loud_should_your_master_be.html
54. Violet Recording. "How to master hip hop." https://violetrecording.com/how-to-master-hip-hop/ ; "How loud should my master be." https://violetrecording.com/how-loud-should-my-master-be/
55. Splice. "How to master hip hop." https://splice.com/blog/?p=4806
56. KVR Audio forum. "What exactly are LUFS" (hard rap −7.5 to −8.5). https://www.kvraudio.com/forum/viewtopic.php?p=8979058
57. raespoon. "Mastering for streaming LUFS guide." https://raespoon.com/dispatches/mastering-for-streaming-lufs-guide/
58. Orphiq. "Mastering for Streaming: Loudness and Format Guide." https://orphiq.com/resources/mastering-for-streaming
59. MasteringBOX. "Mastering for streaming." https://masteringbox.com/learn/mastering-for-streaming
60. Sage Audio. "Top 5 Mastering Tricks for Amazing Sub Bass." https://www.sageaudio.com/articles/top-5-mastering-tricks-for-amazing-sub-bass ; "How to Create Punchy Sub Bass in Your Master." https://www.sageaudio.com/blog/mastering/how-to-create-punchy-sub-bass-in-your-master.php ; "How to Make Clean Masters." https://www.sageaudio.com/articles/how-to-make-clean-masters ; "4 Steps to Make Your Master Bigger." https://www.sageaudio.com/articles/4-steps-to-make-your-master-bigger
61. BMI. "Mixing Your Music for Radio Broadcast." https://www.bmi.com/advice/mixing_your_music_for_radio_broadcast
62. ProSoundWeb. "In the Studio: Mastering as another mix layer." https://www.prosoundweb.com/in-the-studio-mastering-as-another-mix-layer/
63. Making A Scene. "The Future of Mastering: How AI Helps Indie Artists Get a Radio-Ready Sound." https://www.makingascene.org/the-future-of-mastering-how-ai-helps-indie-artists-get-a-radio-ready-sound/
64. Disc Makers Support. "Will mastering help me get an advantage in the marketplace?" https://support.discmakers.com/hc/en-us/articles/213783937-Will-Mastering-help-me-get-an-advantage-in-the-marketplace
65. HomeRecording.com forum. "Getting song radio ready." https://homerecording.com/bbs/threads/getting-song-radio-ready.317881/

### Suno prompt behaviour (community / vendor)
66. AI Tool Discovery (Medium). "Suno Reddit Tips and Prompts: What r/SunoAI Users Actually Do." https://medium.com/@aitooldiscovery/suno-reddit-tips-and-prompts-what-r-sunoai-users-actually-do-65b59d51bcad
67. HookGenius. "Suno mixing & production tips." https://hookgenius.app/learn/suno-mixing-production-tips/ ; "Suno metatags complete list." https://hookgenius.app/learn/suno-metatags-complete-list/
68. roo.beehiiv. "Suno v5 and v5.5 prompts: why it ignores half your words and how to fix it." https://roo.beehiiv.com/p/suno-v5-and-v5-5-prompts-why-it-ignores-half-your-words-and-how-to-fix-it
69. AceTagGen. "Suno V5 vs V4.5 vs V5.5: the complete version comparison." https://acetaggen.com/blog/suno-v5-vs-v4-complete-comparison
70. Blake Crosley. "Suno Guide: Tags, Meta Tags & Prompts (V5.5)." https://blakecrosley.com/guides/suno
71. Jack Righteous. "Unlock Suno AI's full potential — comprehensive music prompt guide." https://jackrighteous.com/de-gb/blogs/guides-using-suno-ai-music-creation/unlock-suno-ai-s-full-potential-with-comprehensive-music-prompt-guide
72. RoEx. "How to mix and master your Suno tracks (and actually sound professional)." https://www.roexaudio.com/blog/how-to-mix-and-master-your-suno-tracks-(and-actually-sound-professional)
73. Soundverse. "How to structure prompts for Suno AI music generation." https://www.soundverse.ai/blog/article/how-to-structure-prompts-for-suno-ai-music-generation-0402

### Tempo and key references
74. Tunebat track pages (accessed 2026-10-31): Connor Price — "Spinnin" https://tunebat.com/Info/Spinnin-Connor-Price-Bens/4r0kug4vaNx3d8qyDLhQes ; "EGOT" https://tunebat.com/Info/EGOT-Connor-Price/4ZvzH0HKoh04HAgLq9PfWD ; "Buddy" https://tunebat.com/Info/Buddy-Connor-Price-Hoodie-Allen/5GZp8q9eZ9qn0TSwAayT1o ; "GOLDEN GOOSE" https://tunebat.com/Info/GOLDEN-GOOSE-Connor-Price/1Twewmebxt8FyDlmpj0zGt ; "DIP" https://tunebat.com/Info/DIP-Connor-Price-Kazuo/0Hcmw8rpKUqeilvueDJq7S ; "Trendsetter" https://tunebat.com/Info/Trendsetter-Connor-Price-Haviah-Mighty/09VW2o6EHSWwzCdhW4ARU1 ; "AMG" https://tunebat.com/Info/AMG-Connor-Price-Killa/7Lyr5TLa4s2nbH8gYUJ3wm ; "Don't Tell Me" https://tunebat.com/Info/Don-t-Tell-Me-Connor-Price-John-Roa/2Nup8oYRzBaqtPSU9qnkls ; "It's Going Down" https://tunebat.com/Info/It-s-Going-Down-Nic-D-Connor-Price/0CbvZqFKsu2HMwIpnCAQRM ; "You Said" https://tunebat.com/Info/You-Said-Connor-Price/2xk4fowrdbKvwlpaeSD4V6
75. Tunebat/Musicstax/SongBPM — Anderson .Paak: "Bubblin" https://tunebat.com/Info/Bubblin-Anderson-Paak/6BvtitRX5lQC87YlA6rq0n ; "Come Down" https://tunebat.com/Info/Come-Down-Anderson-Paak/276zciJ7Fg7Jk6Ta6QuLkp ; "Tints" https://tunebat.com/Info/Tints-feat-Kendrick-Lamar-Anderson-Paak-Kendrick-Lamar/1gnwGVoG7V08vMX3hyr90x ; "Am I Wrong" https://tunebat.com/Info/Am-I-Wrong-feat-ScHoolboy-Q-Anderson-Paak-ScHoolboy-Q/1UjyF6okUHhnVxaLV8ojsA ; "Lockdown" https://tunebat.com/Info/Lockdown-Anderson-Paak/5oZps6mYqU5s7A4WjUZggJ ; "Make It Better" https://musicstax.com/track/make-it-better/4SBVWkRIMJ6WBCYPvr5Bwr ; Music Gateway "Come Down" https://www.musicgateway.com/song-key-bpm/anderson-paak/come-down ; Mixgraph artist page https://www.mixgraph.io/artists/anderson-paak
76. Tunebat/Musicstax/SongBPM — JID: "Surround Sound" https://tunebat.com/Info/Surround-Sound-feat-21-Savage-Baby-Tate-JID-21-Savage-Baby-Tate/0cp97b37sFNsdIbQH6po3T ; https://songbpm.com/@jid/surround-sound-feat--21-savage-baby-tate-1A5ot1ENn9 ; "151 Rum" https://tunebat.com/Info/151-Rum-JID/22WV03i2lBbwNVCE1g671p ; https://www.mixgraph.io/tracks/jid-151-rum ; "Dance Now" https://tunebat.com/Info/Dance-Now-feat-Kenny-Mason-JID-Kenny-Mason/2WrldKXSBVbNNm3A9vJryi ; "Off Deez" https://tunebat.com/Info/Off-Deez-with-J-Cole-JID-J-Cole/3JAeYOjyJodI4PRs44lx2l ; "Stick" https://tunebat.com/Info/Stick-with-JID-J-Cole-feat-Kenny-Mason-Sheck-Wes-Dreamville-JID-J-Cole-Kenny-Mason-Sheck-Wes/1BzXvBpIFWJgu0P8P6xmP4 ; "Lisa" https://tunebat.com/Info/Lisa-with-6LACK-JID-6LACK/7fV2JI2daonjH7UE7NYkpO
77. Tunebat/Musicstax/SongBPM — phonk: Kordhell "Murder In My Mind" https://tunebat.com/Info/Murder-In-My-Mind-Kordhell/6qyS9qBy0mEk3qYaH8mPss ; Kordhell "Scopin" https://tunebat.com/Info/Scopin-Kordhell/7KmbiagSkUbepU88x7NWjb ; Dxrk "RAVE" https://tunebat.com/Info/RAVE-Dxrk-/01kfSdF9zfcDLri5sSWEoL ; Hensonn "Sahara" https://tunebat.com/Info/Sahara-Hensonn/6nqdgUTiWt4JbABDurkxMI ; INTERWORLD "METAMORPHOSIS" https://tunebat.com/Info/METAMORPHOSIS-INTERWORLD/2ksyzVfU0WJoBpu8otr4pz ; DVRST "Close Eyes" https://tunebat.com/Info/Close-Eyes-DVRST/3CLSHJv5aUROAN2vfOyCOh ; Musicstax phonk playlists https://musicstax.com/playlist/phonk/37i9dQZF1DWWY64wDtewQt
78. Melodics. "Producer's guide to phonk music." https://melodics.com/blog/producers-guide-to-phonk-music
79. Violet Recording. "What is phonk." https://violetrecording.com/what-is-phonk/ ; "How to make phonk." https://violetrecording.com/how-to-make-phonk/ ; "What is trap music." https://violetrecording.com/what-is-trap-music/ ; "What is boom bap." https://violetrecording.com/what-is-boom-bap/ ; "Genre BPM chart." https://violetrecording.com/tools/genre-bpm-chart/
80. Orphiq. "What Is Phonk Music?" https://orphiq.com/resources/what-is-phonk-music ; "BPM Chart by Genre." https://orphiq.com/resources/bpm-tempo-guide
81. Epidemic Sound. "Drift phonk" genre page. https://www.epidemicsound.com/music/genres/drift-phonk/
82. Tidal Magazine. "What Makes Trap … Trap?" https://tidal.com/magazine/article/what-makes-trap-trap/1-72722
83. RouteNote Create. "Exploring trap BPMs: a guide for beatmakers and artists." https://create.routenote.com/blog/exploring-trap-bpms-a-guide-for-beatmakers-and-artists/
84. SampleFocus. "Everything You Need to Know About 140 BPM." https://samplefocus.com/blog/everything-you-need-to-know-about-140-bpm ; "Ultimate guide to tempo and BPM." https://blog.samplefocus.com/blog/ultimate-guide-to-tempo-and-bpm-the-best-bpms-for-hip-hop-trap-dnb-and-more
85. Mixgraph. "Half-Time & Double-Time." https://www.mixgraph.io/tools/half-time-double-time ; "BPM for trap (hip hop)." https://www.mixgraph.io/bpm-for/trap-hip-hop ; artist samples https://www.mixgraph.io/artists/rich-the-kid ; https://www.mixgraph.io/artists/schoolboy-q ; https://www.mixgraph.io/artists/phonon
86. Native Instruments. "What is boom bap." https://blog.native-instruments.com/what-is-boom-bap/
87. MusicMetricsVault. "Boom bap." https://www.musicmetricsvault.com/genres/boom-bap/497
88. Gearspace. "Classic BPM rates." https://gearspace.com/threads/classic-bpm-rates.193285/
89. Digital Music News. "Spotify RapCaviar analysis (Musiio)." 2020-08-18. https://www.digitalmusicnews.com/2020/08/18/spotify-rapcaviar-analysis/
90. SignalKey. "Genre: hip hop" chart stats (weak). https://www.signalkey.io/charts/genre-hip-hop
91. Mixed In Key. "Trap chord progressions: write around the 808 / Best chords for trap." https://mixedinkey.com/captain-plugins/wiki/best-chords-for-trap/ ; "Hip-hop chord progressions." https://mixedinkey.com/captain-plugins/wiki/best-chord-progressions-for-hip-hop/ ; "Best key detection software" (2025 benchmark). https://mixedinkey.com/best-key-detection-software/
92. Unison Audio. "Trap chord progressions." https://unison.audio/trap-chord-progressions/
93. Hooktheory. "Key popularity" cheat sheet. https://hooktheory.com/cheat-sheet/key-popularity ; A minor key page https://Hooktheory.com/cheat-sheet/key/a/minor ; blog "I analyzed the chords of 1300 popular songs." https://www.hooktheory.com/blog/i-analyzed-the-chords-of-1300-popular-songs-for-patterns-this-is-what-i-found/
94. eMastered. "Neo Soul Chord Progressions: A Beginner's Guide." https://emastered.com/blog/neo-soul-chord-progressions
95. Hear and Play. "Neo soul chords — minor chords" (D'Angelo et al.). https://hearandplay.com/main/neo-soul-chords-minor-chords/
96. Guitar Composer's Toolbox. "D Dorian piano scale" (diatonic chords; D'Angelo association). https://guitarcomposerstoolbox.com/piano-scales/d-dorian/
97. Music Gateway. "Neo soul history & famous artists" (minor i–iv–v). https://www.musicgateway.com/blog/music-industry/neo-soul-history-famous-artists

### Energy mapping
98. Secrets of Songwriting. "The advantage of sketching a map for your song." https://www.secretsofsongwriting.com/?p=4977 ; "Controlling the energy levels of your songs." 2010-02-19. https://www.secretsofsongwriting.com/2010/02/19/controlling-the-energy-levels-of-your-song/
99. Jack Righteous. "Section energy mapping guide." https://jackrighteous.com/de-us/blogs/music-creation-process-guide/section-energy-mapping-make-chorus-hit-harder
100. Orphiq. "How to Arrange a Song for Maximum Impact." https://orphiq.com/resources/how-to-arrange-a-song
101. DeviantNoise. "How to Arrange Music." https://deviantnoise.net/education/music-production/arrangement/
102. MusicProductionWiki. "How to arrange a song." https://musicproductionwiki.com/articles/how-to-arrange-a-song.html

### Sync / library
103. Audiosocket Artist Resources. "Do you accept music with samples?" https://audiosocket.crunch.help/en/artist-resources/can-an-artist-submit-music-with-samples ; "What files should I upload, and should I include stems or alternate versions?" https://audiosocket.crunch.help/en/artist-resources/files-metadata-versions-stems
104. Storyblocks Contributor FAQ. "Submission requirements for audio tracks." https://contribute-faq.storyblocks.com/en/articles/4543815-what-are-the-submission-requirements-for-audio-tracks
105. Interspace Music Help. "Releasing music with samples, interpolations, or purchased beats." https://help.interspacemusic.com/article/releasing-music-with-samples-interpolations-or-purchased-beats
106. Hypebot. "Prepare music for sync licensing success" (Ted Lowe, Choicetracks, via BMI). https://www.hypebot.com/prepare-music-for-sync-licensing-success/
107. Soundfly Flypaper. "How to Make a Music Supervisor Happy: Be Prepared with Instrumentals & Alt Mixes." https://flypaper.soundfly.com/hustle/how-to-make-a-music-supervisor-happy-be-prepared-with-instrumentals-alt-mixes/
108. mn2s. "How to Build a Sync-Ready Catalog: stems, instrumentals and metadata done right." https://mn2s.com/news/label-services-resources/how-to-build-a-sync-ready-catalog-stems-instrumentals-and-metadata-done-right/
109. Symphonic. "How to prepare music for sync licensing." 2020-02. https://blog.symphonic.com/2020/02/how-to-prepare-music-for-sync-licensing/
110. Horus Music. "How to get your music ready for sync licensing." https://horusmusic.global/how-to-get-your-music-ready-for-sync-licensing/
111. Bandzoogle. "How to get your stems and instrumentals for sync licensing." https://bandzoogle.com/blog/how-to-get-your-stems-and-instrumentals-for-sync-licensing-spatial-audio-and-more
112. Alibi Music. "Anatomy of an Alibi track." https://alibimusic.com/blog/anatomy-of-an-alibi-track ; "Cutting your commercial to music? 4 reasons you need stems and alt versions." https://alibimusic.com/blog/cutting-your-commercial-to-music-here-are-4-reasons-you-need-to-seek-out-the-stems-and-alt-versions
113. Xposure Music. "The micro-licensing goldmine: getting your catalogue into stock music libraries." https://info.xposuremusic.com/article/the-micro-licensing-goldmine-getting-your-catalogue-into-stock-music-libraries
114. Music Library Report. "Cue Tips." https://musiclibraryreport.com/composer-tips/cue-tips/ ; forum "Creating edits." https://musiclibraryreport.com/forums/topic/creating-edits ; forum "Submitting WFH cues after being accepted by a library." https://musiclibraryreport.com/forums/topic/submitting-wfh-cues-after-being-accepted-by-a-library
115. TAXI forums. "Any tips for commercial edits?" https://forums.taxi.com/post521555.html
116. Sound On Sound. "Library Work." https://www.soundonsound.com/techniques/library-work
117. Spitfire Audio. "Creating Production Music." https://www.spitfireaudio.com/pages/creating-production-music
118. Film Editing Pro. "Quick tips for cutting music cues." https://www.filmeditingpro.com/quick-tips-for-cutting-music-cues/
119. Lalal.ai. "Beat these 5 biggest music sync challenges with stem separation." https://www.lalal.ai/blog/beat-these-5-biggest-music-sync-challenges-with-stem-separation/
120. Crossfadr. "The sync license boom: how to get your beats placed in video games, TV shows and commercials." 2025-12-12. https://crossfadr.com/2025/12/12/the-sync-license-boom-how-to-get-your-beats-and-productions-placed-in-video-games-tv-shows-and-commercials/
121. Jack Righteous. "Sync licensing and AI music: what's accepted in 2026." https://jackrighteous.com/de-us/blogs/ai-music-distribution-guide/sync-licensing-and-ai-music-what-s-accepted-in-2026
