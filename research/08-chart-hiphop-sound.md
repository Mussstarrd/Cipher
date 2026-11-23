# 08 — Chart hip hop sound: frequencies, tempo and key, leads, cadences, injections (research for suno-forge)

Research date: 2026-11-23. All sources accessed 2026-11-23.

Extends `research/06-addictive-sound-theory.md` (hook and groove science) and
`research/04-radio-ready-conventions.md` (structure, loudness, lane tempo tables). Nothing from
those files is repeated here except as a one-line pointer.

## How this was researched, and a caveat on access

Direct page fetches were blocked in this session for every host tried (arxiv.org,
www.musicradar.com, www.hitsongsdeconstructed.com, themixreview.org: "not in the session's
allowed network domains"). Every sourced claim below therefore rests on **search-engine snippets
of the named page**, with the URL given so it can be confirmed with one click. Where two
sources disagree, both are given. Where a number is my own arithmetic from a sourced number, it
says "arithmetic".

Labels, as asked:

- **MEASURED** = data, a published analysis, a documented credit, or an engineer's written
  analysis of a specific record (corpus statistics, a mix critique that reads Hz off a
  spectrum, a tempo database, a peer-reviewed measurement).
- **INTERVIEW** = a producer's or engineer's own statement, including practitioner guides and
  tutorials written by working engineers (these are advice, not measurements).
- **MY JUDGMENT** = my synthesis as a producer and mix engineer. Not sourced. Arithmetic on a
  sourced number is marked "arithmetic" and is also mine.

Artist, producer and record names appear in sections 1 to 7 **only** as research. None may enter
a prompt (CLAUDE.md rule 1). Section 8 holds the only strings meant to be lifted toward `lib/`,
and they were machine-checked (see 8.5).

---

## 0. Headline numbers

| What | Number | Label |
|---|---|---|
| Hip hop tempo inside the Hot 100 top 10, Q1 to Q3 2023 | **82 BPM** average (all genres 97; non-hip-hop 101) | MEASURED |
| Tempo of 2024 Hot 100 No. 1 hits | **100 BPM** average, 4 BPM faster than 2023; songs under 79 BPM fell to 13%, the lowest in over a decade | MEASURED |
| Standout rap hits 2022 to 2025 | cluster at **~100 BPM straight time** (two West Coast records at 100 to 101) or at **150 to 162 written** with a club or double-time pattern, not at the 65 to 80 felt half-time of stock trap | MEASURED per record, pattern is MY JUDGMENT |
| Mode | minor is hip hop's home; all-genre top 10 went 53% major / 45% minor in the first half of 2025 as hip hop's share fell | MEASURED |
| 808 fundamental | **30 to 60 Hz** typical; F1 (43.65 Hz) and G1 (49 Hz) named as the popular tunings; engineer readings of 37, 40 and 50 Hz on real records | INTERVIEW and MEASURED |
| Snare | body **150 to 250 Hz**, crack **2 to 5 kHz** | INTERVIEW |
| Ear's most sensitive band | **2 to 5 kHz** (dip in the threshold curve near 4 kHz) | MEASURED |
| Phone speaker passband | about **500 Hz to 10 kHz**; steep loss below 500 to 600 Hz, lift between 3 and 9 kHz | MEASURED on one phone, INTERVIEW otherwise |
| Swing in the classic hip hop drum-break canon | median 16th swing ratio **1.2:1** (about 15 ms late at 90 BPM, arithmetic) | MEASURED |
| Laid-back snare advice | **5 to 15 ms** late with the hats on the grid; clap layers a few ms either side of the snare | INTERVIEW |
| Key changes in No. 1 hits | 23% of No. 1s from 1958 to 1990; **one** in 2010 to 2020, and that one is a three-beat rap record | MEASURED |

---

## 1. Frequencies: where chart hip hop puts its energy

### 1.1 The low end: 808, sub and kick

| # | Finding | Label | Source |
|---|---|---|---|
| F1 | 808 fundamentals sit in sub-bass territory, "typically between 30 and 60 Hz or so". A tuning-tool page widens it to 30 to 80 Hz and names **F1 (43.65 Hz) and G1 (49 Hz)** as the popular tuning choices "because they balance power with audibility". | INTERVIEW (practitioner guides; the tuner page is promotional) | [iZotope, How to Mix 808s](https://www.izotope.com/en/learn/how-to-mix-808s), [MusicRadar 808 guide](https://musicradar.com/how-to/808-kick-guide), [808 tuner page](https://bchillmix.com/pages/808-tuner) |
| F2 | Tune the 808 to the root of the key, or to the fifth (+7 semitones) when kick and 808 fight for the same range. | INTERVIEW | [Icon Collective, 808 mixing tips](https://iconcollective.edu/808-mixing-tips) |
| F3 | A fundamental below about 40 Hz does not play on phone speakers; the harmonics at 80 to 200 Hz do. | INTERVIEW | [Kalu Bandali, make an 808 cut through](https://payhip.com/KaluBandali/blog/news/make-a-808-cut-through-a-mix) |
| F4 | ISMIR 2024 paper on the TR-808 bass drum: the kick has a **limited usable pitch range** if its character is to survive; across the 37 "long" samples of one library the initial pitch drop has a median of about **one half-tone**; in a 2017 flute-and-808 trap hit the 808 samples are tuned to the song's key (D minor) at "roughly 40 Hz", "almost subsonic"; one hit-making producer is described as transposing **the whole song to fit the 808** instead of tuning the 808 to the song. | MEASURED | [arXiv 2502.07524](https://arxiv.org/abs/2502.07524v1), [ISMIR 2024 poster](https://ismir2024program.ismir.net/poster_86.html), [author's summary](https://medium.com/@derutycsl/why-adjusting-the-songs-tonality-to-the-kick-drum-sample-is-a-good-idea-3905c6a68e1c) |
| F5 | Mix engineer Mike Senior, reading spectra of chart records: an 808-style kick with pitched components at **37 Hz and 74 Hz** that act as a bass note (a D) on a 2010 pop-rap hit; a 2021 pop hit with the kick's energy focused around **50 Hz** over a bass line whose fundamentals run **37 to 73 Hz**; on the 2017 trap hit above, the 808 sample is "compressed and saturated" until it sustains like a bass note and is then pitch-shifted into a bass line, and the sparse arrangement is what keeps the performance intelligible. | MEASURED (engineer's analysis) | [The Mix Review: What's My Name](https://themixreview.org/reviews/whats-my-name/), [Bad Habits](https://themixreview.org/reviews/bad-habits/), [Mask Off](https://themixreview.org/reviews/mask-off/) |
| F6 | Same engineer: high-passing a bass at about 70 Hz reveals **distortion products in the lower mid-range that help the notes survive on smaller speakers**; a bass whose 100 to 200 Hz region is strong "registers well on smaller speakers"; a kick's lower-mid "knock" lives at 300 to 800 Hz (which review this last figure belongs to was not confirmed). | MEASURED (engineer's analysis) | [Don't Call Me Angel](https://themixreview.org/reviews/dont-call-me-angel/), [Love Spreads](https://themixreview.org/reviews/love-spreads/), [review index](https://themixreview.org/reviews/) |
| F7 | Kick punch 40 to 150 Hz, kick body 80 to 100 Hz. | INTERVIEW (EQ guides) | [Guitar Center EQ cheat sheet](https://www.guitarcenter.com/riffs/gear-tips/recording/eq-cheat-sheet-for-mixing), [Dummies EQ guidelines](https://dummies.com/article/home-recording-eq-frequency-guidelines-179779) |

Equal-temperament note to Hz, A4 = 440 (arithmetic; this is the lookup the generator's key
choice implies for the 808 root):

| Note | Hz | Note | Hz |
|---|---|---|---|
| B0 | 30.9 | F#1 | 46.2 |
| C1 | 32.7 | G1 | 49.0 |
| C#1 | 34.6 | G#1 | 51.9 |
| D1 | 36.7 | A1 | 55.0 |
| D#1 | 38.9 | A#1 | 58.3 |
| E1 | 41.2 | B1 | 61.7 |
| F1 | 43.7 | C2 | 65.4 |

MY JUDGMENT: this table is why F, F# and G minor are the stock trap keys in the project's own
`reference/tempo-key-tables.md`: their roots land at 44 to 49 Hz, low enough to be felt and high
enough to reproduce. A key of C or C# forces a choice between a root at 33 to 35 Hz (felt, barely
heard, gone on a phone) and one at 65 to 69 Hz (heard, less weight). D at 37 Hz is the "almost
subsonic" case in F4 and F5. The transferable rule is not the Hz: it is **one long tuned note,
very low, with enough grit above it that the pitch can be followed on a small speaker**.

### 1.2 Snare, hats and air

| # | Finding | Label | Source |
|---|---|---|---|
| F8 | Snare **body 150 to 250 Hz**; attack and cut 2 to 3.5 kHz (iZotope), 2 to 4 kHz (ProSoundWeb), "crack 3 to 5 kHz, the money spot" (Music Guy Mixing); stick transient near 5 kHz. | INTERVIEW | [iZotope, How to EQ Drums](https://www.izotope.com/en/learn/how-to-eq-drums), [ProSoundWeb, the art of the snare](https://www.prosoundweb.com/the-art-of-the-snare-getting-the-crack-thud-right-part-2-in-the-how-z-does-it-series/), [Music Guy Mixing drum EQ chart](https://www.musicguymixing.com/drum-eq-chart/) |
| F9 | Hi-hat "air and sparkle" around 12 kHz; air generally 8 kHz and above. | INTERVIEW | [iZotope, How to EQ Drums](https://www.izotope.com/en/learn/how-to-eq-drums) |

### 1.3 Where the lead sits, and why

| # | Finding | Label | Source |
|---|---|---|---|
| F10 | The ear is most sensitive between **2 and 5 kHz** (ear-canal resonance and the middle-ear transfer function); the threshold curves are lowest from 1 to 5 kHz with a dip at 4 kHz; below about 100 Hz loudness falls away steeply, as much as 24 dB per octave. | MEASURED (ISO 226 family of data) | [Equal-loudness contour](https://en.wikipedia.org/wiki/Equal-loudness_contour), [SFU Handbook for Acoustic Ecology](https://www.sfu.ca/sonic-studio-webdav/handbook/Equal_Loudness_Contours.html) |
| F11 | Phone speakers: "a sharp roll-off below 500 Hz and a high-end boost between 3 and 9 kHz", struggling below 200 Hz (LANDR). A measured iPhone 7 Plus "drops off quickly below 600 Hz and above 9 kHz", best-case usable range **about 500 Hz to 10 kHz** (Sonarworks, citing a third-party measurement). | INTERVIEW (LANDR); MEASURED on one phone (Sonarworks) | [LANDR, bass audible on phone speakers](https://blog.landr.com/make-bass-audible-phone-speakers), [Sonarworks, mixes on small speakers](https://www.sonarworks.com/blog/learn/make-your-mix-sound-great-on-small-speakers) |
| F12 | The fix engineers use: do not boost the sub; move energy to the bass's upper harmonics; saturate the upper part of the 808 "while the sub portion remains clean"; split the band in parallel. The psychoacoustic basis is the **missing fundamental**: two or more consecutive harmonics are enough for the brain to supply the absent low note. | INTERVIEW; the effect itself is MEASURED psychoacoustics | [Waves, mixes on small speakers](https://www.waves.com/tips-for-great-mixes-on-small-speakers), [Sonarworks](https://www.sonarworks.com/blog/learn/make-your-mix-sound-great-on-small-speakers), [bass-enhancement patent](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/8625813), [Sound On Sound, Mixing For Mobile](https://www.soundonsound.com/techniques/mixing-mobile) |
| F13 | Lead synths "benefit from emphasis in the midrange, around 1 to 4 kHz"; boost 2 to 5 kHz for presence; most of a lead's character lives at 200 Hz to 4 kHz; treat a lead like a lead singer's part and give it its own frequency pocket. | INTERVIEW | [Splice, mixing synths](https://splice.com/blog/mixing-with-neutron-2-synths), [Mastering The Mix, EQ for keys, synths and leads](https://www.masteringthemix.com/blogs/learn/how-to-eq-guitar-keys-synths-and-leads-when-mixing), [Fiveable EQ study guide](https://fiveable.me/electronic-music-composition/unit-13/eq-frequency-balance/study-guide/Sonz93YUGox3NlfE) |

Harmonic arithmetic (mine). The presence band is not a place a hip hop lead's **note** can sit:
2 kHz is B6 and 5 kHz is D#8. The band is where a lead's **overtones** sit.

| Lead note | Fundamental | Overtones inside 2 to 5 kHz | Overtones inside the phone passband (500 Hz up) |
|---|---|---|---|
| G3 (low-mid) | 196 Hz | 10th to 25th | 3rd and up |
| C4 | 262 Hz | 8th to 19th | 2nd and up |
| C5 | 523 Hz | 4th to 9th | the note itself |
| C6 | 1047 Hz | 2nd to 4th | the note itself |

MY JUDGMENT, and the most useful thing in this file for the "samey" complaint. The project
already learned that high leads come back shrill and keeps leads low or low-mid. A low lead only
reaches the ear's sensitive band, and only survives a phone, through **strong upper overtones**.
That is exactly what a soft piano, a clean nylon guitar and a mellow electric piano do not have,
and it is why those three read as background and collapse into the same soul-sounding wash. The
leads on the records in section 3 are all **low or mid notes with a hard, bright, buzzy or
clipped edge**: a sped-up string stab, a squeak, a ringtone, a clank, a square-wave bass riff.
The grammar should ask for the edge in physical words (rasp, buzz, bite, squeak, click, clipped,
nasal, tin speaker) on a low note, never for a higher note.

### 1.4 Mix moves that make a lead cut on a phone (summary)

1. Thin the lead's low end so it stays clear of the 808 (F13, F6). INTERVIEW.
2. Put the edge in the overtones: saturation, clipping, bit grain, a reed or a squeak (F12, 1.3 arithmetic). INTERVIEW plus MY JUDGMENT.
3. Speed a sample up: L1 and L6 in section 3 are both built on a **sped-up** sample; raising a sample's speed moves its whole spectrum up toward the presence band and shortens it. MEASURED that they did it; the spectral reading is MY JUDGMENT.
4. Filter a sample narrow: the 2023 No. 1 in L5 high-passed its sample "to all but remove the original bass line" and made it **more** lo-fi than the source. MEASURED (engineer's analysis).
5. Keep the low mids empty: one bass note below, one lead above, little between. MY JUDGMENT, consistent with F5's "sparse arrangement".

---

## 2. Tempo, key and mode

### 2.1 Statistics

| # | Finding | Label | Source |
|---|---|---|---|
| T1 | First three quarters of 2023, Hot 100 top 10: all songs averaged 97 BPM; **hip hop averaged 82 BPM**; non-hip-hop 101 BPM. (MY JUDGMENT: an 82 average is what a mix of half-time trap counted at 65 to 80 and a few 100-BPM records gives.) | MEASURED (Hit Songs Deconstructed, via press) | [The Freeman, sonic characteristics of 2023](https://www.pressreader.com/philippines/the-freeman/20240320/281668259962577), [HSD Q3 2023 highlights PDF](https://www.hitsongsdeconstructed.com/wp-content/uploads/2023/11/Q3-2023-Top-10-2023-Highlights.pdf) |
| T2 | 2024 Hot 100 No. 1s: hip hop/rap led for the first time since 2018 with a **38% share**, more than double 2023; average tempo of the No. 1s was **100 BPM**, 4 BPM faster than 2023; the under-79 BPM range fell to 13% of songs, its lowest in over a decade. | MEASURED | [HSD wire, 2024 No. 1 report](https://www.hitsongsdeconstructed.com/hsd_wire/hip-hop-was-on-top-songs-got-longer-hit-songs-deconstructeds-2024-no-1-hit-focus-report-reveals), [HSD highlights PDF](https://hitsongsdeconstructed.com/wp-content/uploads/2025/02/The-Hit-Songs-Deconstructed-1-Hit-Focus-Report-Highlights.pdf) |
| T3 | Of 2024 songs that stayed in the top 10 for ten weeks or more, 65% were over 100 BPM; the most common range was 100 to 119 BPM (all genres). | MEASURED (via MusicRadar) | [MusicRadar, how to write a sticky hit](https://musicradar.com/artists/how-to-write-a-sticky-hit-pure-luck-can-put-you-in-the-charts-but-sticking-around-to-make-the-big-bucks-needs-pure-science) |
| T4 | Mode: from 2019 to 2022 major and minor ran neck and neck on Billboard's Streaming Songs chart; the 2023 jump in major keys was linked partly to hip hop's decline, "since hip hop commonly favors minor tonalities". First half of 2025, Hot 100 top 10: **53% major, 45% minor**. A secondary site gives 56% minor for the 2024 No. 1s (not confirmed against the report). | MEASURED (ChartCipher and HSD, via secondary coverage) | [Music 3.0 on ChartCipher](https://music3point0.com/?p=7797), [HSD 2025 mid-year preview PDF](https://www.hitsongsdeconstructed.com/wp-content/uploads/2025/07/The-State-of-the-Hot-100-Top-10-2025-Mid-Year-Report-Preview.pdf), [secondary statistics page](https://voxbooster.com/blog/song-tempo-bpm-key-distribution-charts-statistics-2026) |
| T5 | Context for "take hip hop back": on the Hot 100 dated 2025-10-25 there was **no rap song in the top 40 for the first time since February 1990**; Billboard put rap's market share at 30% in 2020 and 24% in 2025 to date (a chart-rule change removed one long-running rap hit that week). The same week in 2020 had 16 rap songs in the top 40. | MEASURED | [Complex](https://www.complex.com/music/a/markelibert/no-rap-songs-in-billboard-hot-100-top-40-for-the-first-time-since-1990), [The Fader](https://www.thefader.com/2025/10/29/billboard-top-40-no-rap-songs-35-years), [Billboard Canada](https://ca.billboard.com/music/rb-hip-hop/no-rap-songs-hot-100-top-40-first-time-since-1990-1236100625/) |
| T6 | What producers are reaching for (sample-library data): in 2024 hip hop packs were downloaded 48.7 million times (+1.8%) and trap 35.0 million (+3.1%), flat; the fastest growers were **pluggnb (+342.8%)** and **Jersey club (+281.3%)**. The following year's report has Jersey club down 21% but still three times its 2023 size, **sexy drill the fastest-growing genre search (+569%)**, and plain drill out of favour. | MEASURED (Splice and MIDiA) | [Music Business Worldwide](https://www.musicbusinessworldwide.com/splice-which-hit-nearly-350m-downloads-in-2024-reveals-the-fastest-growing-genres-on-its-platform), [Hypebot](https://www.hypebot.com/hypebot/2025/01/pluggnb-is-musics-fastest-growing-genre-on-splice-in-2024.html), [EDM.com on the 2026 report](https://edm.com/industry/whats-next-electronic-music-production-2026-splice-midia-research/) |
| T7 | Style tempo bands: UK and New York drill about 138 to 151 BPM; Jersey club about 130 to 140 BPM; sexy drill quoted at 138 to 145 (vendor copy). | MEASURED (encyclopedic), INTERVIEW (vendor) | [UK drill](https://en.wikipedia.org/wiki/UK_drill), [Orphiq on Jersey club](https://orphiq.com/resources/what-is-jersey-club), [Mixcloud guide](https://blog.mixcloud.com/2023/07/05/mixclouds-on-rotation-a-guide-to-jersey-club/) |

### 2.2 The outliers that stood out, and what they did differently

Tempo and key come from automated databases and transcription sites; where they disagree both
readings are shown. Treat pitch class as roughly right and mode as uncertain.

| Record (year) | Tempo | Key reading | What it did differently | Sources |
|---|---|---|---|---|
| West Coast diss record, 2024 No. 1 | **101 BPM** (one site 102) | B minor (Hooktheory, Melodics, a backing-track seller); one tool says C# minor | Straight time at about 100, not half-time; a **sped-up** orchestral sample of violins, piano and brass from a 1968 cover; bass line, snares and finger snaps; made in about 30 minutes | [Wikipedia](https://en.wikipedia.org/wiki/Not_Like_Us), [backing-track listing](https://www.karaoke-version.com/mp3-backingtrack/kendrick-lamar/not-like-us.html), [Hooktheory](https://hooktheory.com/theorytab/view/kendrick-lamar/not-like-us), [Melodics](https://melodics.com/blog/how-to-play-not-like-us-keys) |
| Same artist and producer, late 2024 | **100 BPM** (200 on the double-time reading; outliers 108 and 113) | D major (two tools) or F# minor (two tools) | Two separate beats joined; the first half rides an orchestral sample; detectors cannot agree whether it is major or minor, which is itself the sound: brighter than trap's plain minor | [Songbpm](https://songbpm.com/@kendrick-lamar/tv-off-gq4pg), [Tunebat](https://tunebat.com/Info/tv-off-feat-lefty-gunplay-Kendrick-Lamar-Lefty-Gunplay/75bsrBtmM57lhi55yOfzBL), [Planetary](https://planetary.music/song-analysis/kendrick-lamar/tv-off), [Wikipedia](https://en.wikipedia.org/wiki/TV_Off) |
| Three-artist rap No. 1, 2024 | "variable, around **162 BPM**" (81 on the half reading) | C# minor (seller) or F# (transcription tool) | A **sped-up** sample of an older bass-driven rap record plus a second old-school sample: the hook is a borrowed riff played too fast | [Wikipedia](https://en.wikipedia.org/wiki/Like_That_(Future,_Metro_Boomin_and_Kendrick_Lamar_song)), [backing-track listing](https://www.karaoke-version.com/mp3-backingtrack/future/like-that.html), [Klangio](https://songs.klang.io/en/s/Future/Like%20That/N9bKBAA22Go) |
| Jersey club rap single, 2022 | **150 BPM** | D minor (Hooktheory) or B minor / E-flat minor (seller) | A club kick pattern of five kicks per bar under rap; dance, hip house and Jersey club elements; faster than the club genre's usual 130 to 140 | [backing-track listing](https://www.karaoke-version.com/mp3-backingtrack/lil-uzi-vert/just-wanna-rock.html), [Hooktheory](https://www.hooktheory.com/theorytab/view/lil-uzi-vert/just-wanna-rock), [Wikipedia](https://en.wikipedia.org/wiki/Just_Wanna_Rock) |
| Pop-rap No. 1, 2023 | not found | two chords only, Gm7 and Am7 | One two-chord loop start to finish over a filtered 1964 trumpet hook and finger snaps | [HSD deconstruction](https://reports.hitsongsdeconstructed.com/2024/01/06/paint-the-town-red-deconstructed/), [Stereogum theory column](https://stereogum.com/2236347/a-composer-breaks-down-the-music-theory-behind-doja-cats-1-hit-paint-the-town-red/columns), [Wikipedia](https://en.wikipedia.org/wiki/Paint_the_Town_Red_(Doja_Cat_song)) |
| Three-beat rap No. 1, 2018 (the template for switches) | 155 BPM (single database figure) | G minor, then C# minor, then E-flat minor (Wikipedia mirror; unconfirmed) | Three movements, three abrupt beat changes, the only No. 1 of its decade with a key change | [Songfacts](https://www.songfacts.com/facts/travis-scott/sicko-mode), [Wikipedia](https://en.wikipedia.org/wiki/Sicko_Mode) |

MY JUDGMENT on the pattern. The stock is half-time minor trap at a felt 65 to 80 with a piano
or pad. The records that cut through did one or more of: (a) moved the felt pulse to **95 to
105 straight**, which is also where the groove research in file 06 puts the body's preferred
tempo; (b) kept a fast written tempo but swapped the trap pattern for a **club or bounce
cadence**; (c) built on **one borrowed, instantly recognisable midrange object** (a sped-up
stab, a bass riff, a trumpet line, a squeak) instead of a chord progression; (d) blurred major
and minor instead of sitting in plain natural minor. For the generator: more rolls at a felt 95
to 105 with a bounce cadence, more two-chord or one-chord rolls, and fewer rolls where the lead
is a chordal instrument at all.

---

## 3. Lead sounds that defined recent hits, and why they stuck

### 3.1 What the records used

| # | Record (year) | The lead or hook sound | Label | Source |
|---|---|---|---|---|
| L1 | West Coast diss No. 1 (2024) | "Stirring" violins, piano and brass taken from a 1968 cover and **sped up** for a "relentless" and "urgent" feel; an added bass line, snares and finger snaps; described elsewhere as orchestral stabs. The producer called it "this really weird-ass beat, but I really love it", made it in about 30 minutes, and got the hand-clap idea by asking what one West Coast veteran would do with a crunk producer clapping in the room. | MEASURED (credits and description); INTERVIEW (the producer's words) | [Wikipedia](https://en.wikipedia.org/wiki/Not_Like_Us), [Songfacts](https://www.songfacts.com/facts/kendrick-lamar/not-like-us), [Complex](https://complex.com/music/a/markelibert/mustard-says-he-made-not-like-us-beat-in-30-minutes), [The Root](https://www.theroot.com/not-like-us-producer-mustard-breaks-down-making-kendric-1851559221), [MusicRadar](https://www.musicradar.com/news/kendrick-not-like-us-dj-mustard) |
| L2 | Follow-up single, same team (2024) | First half on an orchestral sample from the same 1960s arranger; "its orchestral sample giving way to the producer's trademark bounce". | MEASURED | [Wikipedia](https://en.wikipedia.org/wiki/TV_Off), [Switched On Pop](https://switchedonpop.substack.com/p/kendrick-lamar-from-pulitzer-to-pop) |
| L3 | Album cut, same producer (2024) | The producer said he was "trying to make a West Coast version" of a famous early-2000s rap record. That record is built on almost nothing but a drum pattern (this characterisation is mine, from memory): the drums are the hook. | INTERVIEW; the characterisation is MY JUDGMENT | [Okayplayer](https://www.okayplayer.com/mustard-was-trying-to-make-a-west-coast-version-of-grindin-for-kendrick-lamars-hey-now/313325) |
| L4 | Album opener single, same artist (2024) | A bass line sampled from a 1984 freestyle dance single (one source says 1983): "a Moog synthesizer-driven groove". The lead **is** the bass riff. | MEASURED | [Songfacts](https://www.songfacts.com/facts/kendrick-lamar/squabble-up), [Wikiwand](https://www.wikiwand.com/en/articles/Squabble_Up) |
| L5 | Pop-rap No. 1 (2023) | A 1964 trumpet hook, **high-pass filtered "to all but remove the original bass line"**, processed to sound **more lo-fi than the original**, with extra parts "layered surreptitiously" alongside it; a "subtly brassy, finger snap-laden beat"; two chords. | MEASURED (engineer's analysis) | [The Mix Review](https://themixreview.org/reviews/paint-the-town-red/), [Wikipedia](https://en.wikipedia.org/wiki/Paint_the_Town_Red_(Doja_Cat_song)) |
| L6 | Three-artist rap No. 1 (2024) | A **sped-up** sample of an older bass-driven rap record (the producer asked its maker for permission to use "the sped-up sample"), plus a second old-school rap sample. | MEASURED | [Wikipedia](https://en.wikipedia.org/wiki/Like_That_(Future,_Metro_Boomin_and_Kendrick_Lamar_song)), [Songfacts](https://www.songfacts.com/facts/future-and-metro-boomin/like-that) |
| L7 | Toronto club single (2025) | A phone **ringtone** sampled throughout; "a sweaty Atlanta bass beat peppered with flashy synths"; in the second half "the beat stutters and slows into a rougher groove". | MEASURED | [Songfacts](https://www.songfacts.com/facts/drake/nokia), [Wikipedia mirror](https://wiki.krisyotam.com/content/wikipedia_en_all_nopic_2026-03/Nokia_(song)), [HotNewHipHop](https://www.hotnewhiphop.com/885823-drake-nokia-stream) |
| L8 | Marching-band rap single (2024) | "A hard, clanking beat" compared to the same lunch-table record as L3; horns from a 2007 Southern single arriving late; the first minute is a cappella; written with marching bands in mind. A mix engineer singles out the **"unapologetic dissonance" of the bass**: a tritone leap (F to B) and bass notes that clash with the held note of the hook. | MEASURED (review and engineer's analysis) | [Stereogum](https://stereogum.com/2285632/premature-evaluation-tyler-the-creator-chromakopia/reviews/premature-evaluation), [Grammy.com](https://grammy.com/news/tyler-the-creator-chromakopia-new-album-review-takeaways), [The Mix Review](https://themixreview.org/reviews/sticky/), [Complex](https://www.complex.com/music/a/tracewilliamcowen/tyler-the-creator-sticky-marching-band-video) |
| L9 | Squeak-led hit (2019 to 2020; outside the date range, kept as the template) | A squeak "like a windshield wiper that needs replacing", performed by the artist, over a beat the producers say took 10 to 15 minutes. | MEASURED and INTERVIEW | [Rolling Stone](https://www.rollingstone.com/music/music-news/roddy-ricch-the-box-934137/), [Revolt](https://revolt.tv/2020/1/10/21059055/travis-scott-roddy-ricch-producer-30-roc-interview), [Roland](https://articles.roland.com/?p=28795) |
| L10 | Jersey club as a genre | The recurring signature is a **bed-spring squeak** lifted from the intro of a 2004 Southern rap record (one source says the sound was a squeaking studio chair), first chopped up by one of the genre's founding DJs. | MEASURED | [Vice](https://www.vice.com/en/article/part-three-of-our-jersey-club-documentary-delves-into-how-the-genre-went-global/), [Orphiq](https://orphiq.com/resources/what-is-jersey-club) |
| L11 | Rage (2021 onward) | "Short, looping, stereo-widened" synth hooks taken from 2010s dance music; laser shots and bells as percussion; the founding track's loop was a dance sample-pack loop played in reverse. | MEASURED (encyclopedic) | [Wikipedia](https://en.wikipedia.org/wiki/Rage_(music_genre)), [HotNewHipHop](https://hotnewhiphop.com/431841-the-rise-of-rage-music-news) |
| L12 | Plugg and pluggnb | "Simple chords, hard-hitting 808s and repetitive, thoughtful, addictive melodies"; airy, minimal, synthetic. | MEASURED (encyclopedic) | [Wikipedia](https://en.wikipedia.org/wiki/Plugg) |
| L13 | Sexy drill | Drill's bounce under samples of older R&B and soul songs. | MEASURED (press) | [Okayplayer](https://www.okayplayer.com/cash-cobain-sexy-drill), [Revolt](https://www.revolt.tv/article/explaining-sexy-drill-and-some-of-the-subgenre-top-artists) |

### 3.2 Transferable physical properties (MY JUDGMENT, each tied to rows above)

1. **A low or mid note with a bright, hard edge.** Sped-up stabs (L1, L6), a squeak (L9, L10), a
   ringtone (L7), a clank (L3, L8). The note is not high; the overtones are. See 1.3.
2. **One object, not a progression.** Every row is a single sampled or synthesized thing you
   could name in two words. None is "piano chords". Two chords at most (L5), often one.
3. **Short and percussive.** Fast attack, short decay, cut by hand or by the sampler. The lead
   doubles as a drum (L3, L8) or the bass doubles as the lead (L4, L6).
4. **Few notes and a lot of air.** Two to five notes, a loop of one or two bars, the rest of the
   bar empty; the squeak in L9 is two notes.
5. **Made narrow on purpose.** Low end filtered off, deliberately lo-fi (L5): it sounds like it
   comes from a small speaker, so it survives one, and it leaves the 808 the whole bottom.
6. **Sped up or pitched off its natural speed.** L1 and L6 both raise the speed of an old
   recording: urgency, a shifted spectrum, and a faint wrongness.
7. **One wrong thing.** A dissonant bass under a plain hook (L8), a beat its own maker calls
   weird (L1), a squeak where a synth should be (L9).
8. **Dry and central.** Finger snaps and claps, little reverb (L1, L5); the wide element, when
   there is one, is the hook itself (L11).

The five I would act on first: 1, 2, 3, 5, 7.

---

## 4. Drum cadences and unquantized timing

### 4.1 Where standout patterns put the kick and snare

Counted in one bar of four beats, sixteenths named "1, the 16th after 1, the and of 1, the 16th
before 2".

| # | Pattern | Placement | Label | Source |
|---|---|---|---|---|
| C1 | Stock trap (the default to avoid) | Half-time: kick on 1, snare or clap on 3, hats in 16ths with triplet bursts; heard as snare on 2 by a listener counting slow. | MEASURED (genre analysis) | [Tidal, what makes trap trap](https://tidal.com/magazine/article/what-makes-trap-trap/1-72722), [MusicRadar beginner's guide](https://musicradar.com/news/beginners-guide-to-trap) |
| C2 | Jersey club | "Two quarter-note kicks, then two dotted-eighth kicks, then an eighth": kicks on 1, 2, 3, the 16th before 4 and the and of 4 (the conversion to positions is my arithmetic). Another guide calls it a triplet-feeling, off-balance five-count. | MEASURED | [Native Instruments](https://blog.native-instruments.com/jersey-club/), [Orphiq](https://orphiq.com/resources/what-is-jersey-club) |
| C3 | UK and New York drill | A two-bar pattern: the main snare on beat 3 of the first bar and on beat 4 of the second; hats on a three-three-two grouping; sliding 808. | MEASURED | [UK drill](https://en.wikipedia.org/wiki/UK_drill), [Attack, Beat Dissected: UK drill](https://www.attackmagazine.com/technique/beat-dissected/uk-drill/), [drill beat guide](https://beatstorapon.com/blog/how-to-make-a-drill-beat/) |
| C4 | Dembow | Kick on all four beats; snare or rim on the 16th before 2, the and of 2, the 16th before 4 and the and of 4: a three-three-two figure laid twice against four even kicks. | MEASURED (single clear source plus encyclopedic) | [Orphiq on dembow](https://orphiq.com/resources/what-is-dembow), [Dembow](https://en.wikipedia.org/wiki/Dembow), [Bandcamp Daily](https://daily.bandcamp.com/lists/dembow-rhythm-list) |
| C5 | West Coast bounce | Clap on 2 and 4, a sparse simple kick, an open hat, the closed hat buried; individual hits nudged off the grid; minimal drum programming plus a signature shout; on L1, snares and finger snaps. | INTERVIEW (tutorial), MEASURED (press) | [Attack, Beat Dissected: West Coast](https://www.attackmagazine.com/technique/beat-dissected/west-coast-hip-hop/), [Dazed](https://www.dazeddigital.com/music/article/21210/1/mustard-power), [Wikipedia](https://en.wikipedia.org/wiki/Not_Like_Us) |
| C6 | A displaced 808 as the only variation | On a 2017 No. 1, the lone 808 of the hook is moved "from the downbeat at 0:15 to the first backbeat at 0:31", and its long tone is set against a shorter, higher kick. | MEASURED (engineer's analysis) | [The Mix Review](https://themixreview.org/reviews/bodak-yellow/) |
| C7 | What "generic" means in the data | In Billboard Pop Airplay year-end songs 2012 to 2021, drum patterns are "usually variations of either a standard rock (backbeat) pattern or a four-on-the-floor pattern". Percentages were not readable. | MEASURED (corpus study) | [Geary, Music Theory Online 30.2](https://mtosmt.org/issues/mto.24.30.2/mto.24.30.2.geary.html) |

Syncopation types, defined for the generator (MY JUDGMENT, built from C1 to C6 and the medium
syncopation finding in file 06):

- **Displaced snare**: the backbeat moves by an 8th or a 16th (onto the and of 2, onto 3 alone,
  or 3 in one bar and 4 in the other as in C3).
- **Missing downbeat**: beat 1 has bass or a rim and the first kick arrives on the and of 1 or on 2.
- **Three against four**: kicks spaced three 16ths apart (the three-three-two of C3 and C4), so
  the kick figure crosses the beats while the snare holds them.
- **Late clap**: a second clap or snap a 16th behind where the ear expects it.
- **Ghost notes**: quiet snare taps a 16th either side of a loud one.
- **Role swap**: a tom, a rim or a clap does the kick's or the snare's job (C5, L3).

### 4.2 Measured unquantized timing

| # | Finding | Label | Source |
|---|---|---|---|
| M1 | Frane 2017, *Music Perception* 34(3): 30 classic drum breaks from hip hop's breakbeat canon; 16th-note swing was common but "often fairly subtle"; **median swing ratio 1.2:1**. | MEASURED | [abstract](https://www.sciencegate.app/document/10.1525/mp.2017.34.3.291) |
| M2 | MPC swing settings are named by percentage, 50% being straight: **54, 58, 62, 66 and 71%**. The designer of that swing function: between 50% and about 70% "are lots of wonderful little settings" that turn a rigid beat into something people move to. | MEASURED (manual values), INTERVIEW (designer) | [Attack, swing in DAWs and drum machines](https://www.attackmagazine.com/technique/passing-notes/daw-drum-machine-swing/), [Attack, Roger Linn interview](https://www.attackmagazine.com/features/interview/roger-linn-swing-groove-magic-mpc-timing/3/), [Melodiefabriek](https://melodiefabriek.com/blog/mpc-swing-reason) |
| M3 | Programming tutorials: a hip hop beat at 50 to 60% swing; an 808-and-R&B beat at 60 to 70% with ghost notes at lower velocity; a "drunk drummer" beat with hits placed both ahead of and behind the grid. | INTERVIEW (tutorials) | [Attack beat 1](https://www.attackmagazine.com/?p=2114), [Attack beat 2](https://attackmagazine.com/?p=27482), [Attack, drunk drummer grooves](https://www.attackmagazine.com/technique/beat-dissected/drunk-drummer-style-grooves/) |
| M4 | Delay the snare **5 to 15 ms** for a laid-back hip hop and neo-soul feel, and keep the hats tight on the grid: the contrast between the locked hat and the late snare is the push-pull. Place a clap a few ms before the snare for extra smack or a few ms after for a longer tail. Vary velocities in a small window (one guide: 87 to 102 of 127; which of the two cited guides gives that figure was not confirmed). | INTERVIEW (tutorials) | [Sample Focus, programming grooves](https://samplefocus.com/blog/swing-shuffle-and-humanization-how-to-program-grooves.md), [iZotope, humanize drums](https://www.izotope.com/en/learn/how-to-humanize-and-dehumanize-drums), [SoundBridge, drum programming tips](https://soundbridge.io/en/drum-programming-tips-for-producers-of-all-levels) |
| M5 | Some hit producers barely leave the grid: "I usually never moved something off the grid", with the note that a sample triggered "a couple of milliseconds after the beat" is late and "that might be the sound you like". | INTERVIEW | [Waves, humanizing beats](https://www.waves.com/humanizing-beats-anderson-paak-focus) |
| M6 | Oslo rhythm research: "only a few milliseconds" of deviation from the grid is needed to create groove (interviews with dance-music producers); in interviews across five genres, hip hop and R&B producers were conscious that **sound choice changes perceived timing** (a softer, slower attack is heard later), and split between preferring "flawed" timing and quantized tracks. | MEASURED (interview studies, peer reviewed) | [RITMO news](https://uio.no/ritmo/english/news-and-events/news/2020/a-fraction-of-a-second-is-all-you-need), [Danielsen et al. 2023, Popular Music](https://duo.uio.no/handle/10852/101729) |
| M7 | The best-known programmed feel: hats on a five-way split of the beat (played on the first and fourth fifths, between straight and triplet) with the **snare slightly early**; that producer's microtiming is programmed, so it "is repeated identically, many times per song"; on one beat the backbeat claps are "just a tiny bit early" over near-straight eighths. | MEASURED (analysis by ear and DAW grid, not in ms) | [The Pudding, groove essay](https://pudding.cool/2023/06/groove/), [Ethan Hein, Dilla Time](https://ethanhein.com/wp/2022/dilla-time/) |
| M8 | A small student study of quantised against unquantised hip hop: musicians preferred slightly off-grid versions, non-musicians preferred fully quantised. Not confirmed as peer reviewed. | MEASURED (weak) | [Alton, Hershon and Nandhra](https://musicscience.net/wp-content/uploads/2025/11/8-alton-et-al-livingoffgrid-p.-62-71.pdf) |

Converting ratios to milliseconds (arithmetic; the delay of every second 16th):

| Swing | Ratio | at 90 BPM | at 100 BPM | at 140 BPM written |
|---|---|---|---|---|
| 54% | 1.17:1 | 13 ms | 12 ms | 9 ms |
| 54.5% (the M1 median) | 1.2:1 | 15 ms | 14 ms | 10 ms |
| 58% | 1.38:1 | 27 ms | 24 ms | 17 ms |
| 60% (five-way split) | 1.5:1 | 33 ms | 30 ms | 21 ms |
| 62% | 1.63:1 | 40 ms | 36 ms | 26 ms |
| 66.7% (full triplet) | 2:1 | 56 ms | 50 ms | 36 ms |

MY JUDGMENT. Three things follow. First, the working range is **about 5 to 40 ms**, the low end
for a single late snare (M4, and the 17 ms laid-back snare in file 06), the high end for hats
that lean. Past roughly 50 ms it stops being feel and becomes a triplet, which is the carnival
result the project already bans by name. Second, the feel is **one voice moved against a fixed
one** (hat straight, snare late; or hat leaning, snare early), never everything loosened.
Third, and this fits rule 8 exactly: a programmed offset is **identical in every bar** (M7). So
"unquantized" and "every bar of drums identical" are compatible: write the offset as a fixed
property of the pattern ("the snare a hair late, the same amount every time").

---

## 5. Injections: beat switch, key change, one-off hit

| # | Finding | Label | Source |
|---|---|---|---|
| I1 | The pop key change is close to extinct: 23% of Hot 100 No. 1s from 1958 to 1990 were in more than one key; in 2010 to 2020 there was **one**, and it is the three-beat rap record in 2.2. A hip hop historian's explanation: the device became a crutch, and popular music moved from melody to rhythm. | MEASURED (chart analysis by Chris Dalla Riva) | [Tedium](https://tedium.co/2022/11/09/the-death-of-the-key-change), [NPR](https://www.npr.org/2022/11/30/1139707179/where-did-all-the-key-changes-go) |
| I2 | That record: four producers, "three abrupt beat change-ups", three movements (to about 1:00, to about 3:00, to the close), each reported in a different key. | MEASURED (keys unconfirmed) | [Songfacts](https://www.songfacts.com/facts/travis-scott/sicko-mode), [Wikipedia](https://en.wikipedia.org/wiki/Sicko_Mode) |
| I3 | The 2024 follow-up single in L2: "actually two separate beats" that the artist joined himself; at about two minutes the production stops under a single shouted word and the second beat is simply the next thing heard, with the producer's tag. | INTERVIEW (producer), MEASURED (description) | [Wikipedia](https://en.wikipedia.org/wiki/TV_Off), [Complex](https://www.complex.com/music/a/backwoodsaltar/kendrick-lamar-mustard-tv-off-memes), [Songfacts](https://www.songfacts.com/facts/kendrick-lamar/tv-off) |
| I4 | L7: in the second half "the beat stutters and slows into a rougher groove": a switch of tempo feel and texture inside one song. | MEASURED (description) | [Wikipedia mirror](https://wiki.krisyotam.com/content/wikipedia_en_all_nopic_2026-03/Nokia_(song)) |
| I5 | L8: one minute with nothing but the performers, then the drums; a horn sample held until late in the song. Arrangement by subtraction and late addition, nothing ramping. | MEASURED (review) | [Grammy.com](https://grammy.com/news/tyler-the-creator-chromakopia-new-album-review-takeaways), [Stereogum](https://stereogum.com/2285632/premature-evaluation-tyler-the-creator-chromakopia/reviews/premature-evaluation) |
| I6 | C6: the only change between two hook sections is that one 808 hit has moved from the downbeat to the backbeat. A one-hit variation with nothing leading to it. | MEASURED (engineer's analysis) | [The Mix Review](https://themixreview.org/reviews/bodak-yellow/) |
| I7 | Silence as the event: "that empty beat where you expect something but get silence makes the next hit feel massive"; a critic on a 2000s minimalist production: "all that silence weighs heavy, and bells and whistles would only diminish its power". | INTERVIEW (guide), MEASURED (criticism) | [Sample Focus, space and silence](https://blog.samplefocus.com/blog/using-space-and-silence-in-music-production), [Phoenix New Times](https://www.phoenixnewtimes.com/music/a-whisper-in-the-crowd-6407215/) |
| I8 | The beat switch has a long line in hip hop and R&B and is now a signature move of its biggest names. | MEASURED (press history) | [District Magazine](https://districtmagazine.ie/music/a-brief-history-of-the-beat-switch-in-hip-hop), [Complex](https://www.complex.com/music/a/dimassanfiorenzo/best-hip-hop-beat-switches-of-all-time) |

MY JUDGMENT. In current rap the key change survives **only as a by-product of a beat switch**:
the second beat is in another key because it is another beat (I1 to I3). The mechanism is an
edit point, two finished things placed side by side, not a modulation that is prepared. That is
good news for a model that decorates every boundary it is told about: an injection should be
written as **two steady states**, or as **one hit that is different**, and never as the moment
between them. In the tables below every injection is phrased as what is true during a stretch of
the song ("the second half sits a minor third lower") or as a single altered hit ("one time
only the snare is a hard digital slap"), with the placement left to a section tag in the Lyrics
field. Honest limit: a one-time event at a named place is the hardest thing to get from Suno;
the state-change kind (C1 to C9 in Table C) is far more reliable than the single-hit kind.

---

## 6. What makes a beat feel technical yet mainstream

| # | Finding | Label | Source |
|---|---|---|---|
| X1 | Across music styles, album sales rise as instrumentational complexity falls: a style gets more uniform and less varied as it becomes mainstream. | MEASURED (PLoS ONE 2014, correlational) | [Percino, Klimek and Thurner](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4281146/) |
| X2 | Medium syncopation beats both none and maximum for the urge to move (file 06, G1 and G2). | MEASURED | `research/06-addictive-sound-theory.md` |
| X3 | The biggest rap record of 2024 took about 30 minutes, was called weird by its maker, and uses a sped-up sample, a bass line, snares and snaps. The producer's catalogue is known for "minimal drum programming". | INTERVIEW, MEASURED | [Complex](https://complex.com/music/a/markelibert/mustard-says-he-made-not-like-us-beat-in-30-minutes), [Dazed](https://www.dazeddigital.com/music/article/21210/1/mustard-power) |
| X4 | L9: 10 to 15 minutes, and the thing everyone remembers is a squeak. | INTERVIEW | [Revolt](https://revolt.tv/2020/1/10/21059055/travis-scott-roddy-ricch-producer-30-roc-interview) |
| X5 | L5: two chords for the whole song. L12: "simple chords" and "repetitive, addictive melodies". | MEASURED | [HSD deconstruction](https://reports.hitsongsdeconstructed.com/2024/01/06/paint-the-town-red-deconstructed/), [Wikipedia](https://en.wikipedia.org/wiki/Plugg) |
| X6 | L8: the technical part is hidden in plain sight, a bass line that clashes on purpose under a hook anyone can shout. | MEASURED (engineer's analysis) | [The Mix Review](https://themixreview.org/reviews/sticky/) |
| X7 | A 2025 rap duo's reunion album, produced by one hit-maker alone, was reviewed as "familiar high-gloss minimalism". | MEASURED (criticism) | [Flood Magazine](https://floodmagazine.com/199821/clipse-let-god-sort-em-out/) |

MY JUDGMENT, the working recipe:

- **Four to six sounds**: one lead object, one 808 or bass, kick, snare or clap, at most one hat
  or shaker, at most one signature. This is the project's sparse density; the evidence says it
  should be the majority, not four rolls in ten.
- **One or two bars, repeated**, with the repetition doing the work (file 06, H3 and H5).
- **Exactly one strange thing**, and it must be loud: a wrong timbre (squeak, ringtone), a wrong
  note (the clashing bass in X6), or a wrong placement (the moved 808 in C6). Not three.
- **Technique lives in placement, not in count**: one displaced hit per bar, one voice late, a
  kick figure that crosses the beat. Density stays low.
- **Contrast by taking away**: a mute, a half with a different kit, never an added layer or a ramp.

---

## 7. Saying it in words a text-conditioned model can use

What is known about how such models read text: the public caption set these systems grew from
was written by musicians describing ten-second clips in plain words; the MusicLM authors note
that a few words struggle to pin down timbre, melody and rhythm; a research model (Mustango)
does accept control sentences for tempo in BPM and for key; a 2025 paper shows short timbre
descriptors ("warm lead timbre; acoustic guitar strums; intimate close space") can stand in for
artist names. MEASURED (papers): [MusicCaps](https://paperswithcode.com/dataset/musiccaps),
[MusicLM](https://ar5iv.labs.arxiv.org/html/2301.11325),
[Mustango](https://arxiv.org/html/2311.08355v3),
[The Name-Free Gap](https://arxiv.org/html/2509.00654v1). No source was found that tests Hz or
millisecond values in a Suno prompt; the project's own listening tests (file 06, 2.2) found a
millisecond figure reads as noise. Everything in the right-hand column is MY JUDGMENT.

| The number (keep in research) | Is it a number Suno can use? | Say this instead (a physical description) |
|---|---|---|
| Tempo in BPM | **Yes**, as a bias; keep it | "101 BPM" plus a feel word in the head |
| Key and mode | **Yes**, as a bias; keep it | "in F minor"; the scale name where it matters |
| 808 fundamental 37 to 50 Hz | No | "a tuned 808 sitting very low, one long round note you feel in the chest" |
| Sub clean, harmonics at 80 to 200 Hz and up | No | "the sub pure and steady, a little grit on top so each bass note reads on a phone speaker" |
| Kick knock above the 808 (50 to 100 Hz body, tick at 3 to 5 kHz) | No | "a short knocking kick with a hard tick on the front, sitting just above the 808" |
| Snare body 150 to 250 Hz, crack 2 to 5 kHz | No | "a dry cracking snare, a wooden thump under a bright snap" |
| Hats and air 8 to 12 kHz | No | "thin crisp hats tucked under the snare, airy on top" |
| Lead overtones in 2 to 5 kHz on a low note | No | "the lead low but biting: nasal, buzzing, a hard bright edge on a low note" |
| Lead low end thinned | No | "the lead thin at the bottom, as if from a small speaker, clear of the bass" |
| Phone passband 500 Hz to 10 kHz | No | "the whole beat reads on a small speaker" |
| Empty low mids | No | "a clear gap between the bass and the lead, the middle of the mix left open" |
| Snare 5 to 15 ms late | No | "the snare a hair late, the same amount every time, the hats dead straight" |
| Swing 54 to 58% | No | "the off-beat 16ths lean a touch late, barely" |
| Swing 60 to 62% (five-way split) | No | "the hats lean hard behind the kick, drunk but even" |
| Clap a few ms off the snare | No | "the clap and the snare a hair apart, a smeared double hit" |
| Velocity window (87 to 102) | No | "each hat a slightly different strength, played by hand" |
| Sample sped up | No | "sped up until it sounds urgent and thin" |
| Sample high-passed | No | "the low end filtered off, tinny, like a kitchen radio" |
| Beat positions (the and of 2) | **Partly**: a named feel is obeyed more often than a position (the project's own finding) | give the position **and** a short physical tag: "snare on 3, then on 4" |

---

## 8. Ready-to-use vocabulary

Wording rules applied to every string in 8.1 to 8.4: no artist, producer, label or song names;
no negations; none of fill, roll, riser, sweep, build, drop, transition, enters, comes in,
waltz, shuffle, swing, swung, time signatures, voice, vocal, choir, cinematic, smooth, jazz,
funk; each entry a physical description. In addition every string was written to pass the
stricter `lib/guard.js` rules described in CLAUDE.md rule 8 (nothing placed against the close of
a bar, nothing that stops, cuts, returns or reverses, nothing periodic). In the tables the pipe
between description and handle is written `\|` because of Markdown; copy it as a plain `|`.

Lane notes (hand-applied, not machine-checked): strings that say "808" are not for the
live-pocket lane; "bit-reduced" and "bit-grain" strings are not for the live-pocket lane;
"strings" in A36 belongs to the dark lane's ALLOW.

### 8.1 Table A: lead sounds (46)

Form: `description|the handle`. Kinds: found, struck, electronic, machine, synth, sampled,
blown. Three of the 46 derive from a piano, a guitar or an orchestral sample (A36, A40, A41),
and none of those three is played as that instrument. Registers follow the project rule (low or
low-mid notes); the "why it sticks" column is the property from 3.2.

| # | Kind | Lead | Register and why it sticks |
|---|---|---|---|
| A1 | found | `a rubber sole squeaking on a polished floor, two short squeaks tuned a fourth apart, close and dry\|the sneaker squeak` | Mid register with a bright edge; two notes, recognised in a second, reads on a phone speaker. |
| A2 | found | `a windshield wiper dragging across dry glass, sampled and tuned, a two-note creak that falls a half step\|the wiper creak` | Low-mid with a rasping top; the half step is the one wrong interval. |
| A3 | found | `a bed spring creak sampled and tuned to one pitch, four quick creaks per bar\|the spring creak` | Mid register; one pitch used as rhythm, the hook is the placement. |
| A4 | found | `a drinking straw dragged through a plastic cup lid, the squawk tuned and cut to two notes\|the straw squawk` | Mid register, nasal and rude; an object everyone has heard, tuned. |
| A5 | found | `a tennis ball can popped open, the pop tuned and played as a three-note riff, dry\|the can pop` | Low-mid; a round thump with a sharp front, half drum and half note. |
| A6 | found | `a metal ruler flicked on a desk edge so each note slides upward as it shortens, three flicks per bar\|the ruler flick` | Low sliding to mid; the bend is built into the object. |
| A7 | found | `a shop counter bell tapped twice and choked by a palm, pitched down a fifth\|the counter bell` | Low-mid; bright strike, hand-stopped ring, two hits and air. |
| A8 | found | `a stapler punched in time, a low tuned thud layered under each punch so it carries a note\|the stapler punch` | Low; a hard click on a tuned thud, reads as drum and riff at once. |
| A9 | struck | `a row of clay flower pots struck with a wooden spoon, each pot one note, a dry four-note riff\|the clay pots` | Low-mid; dull and woody with a short ring, far from any keyboard. |
| A10 | struck | `an empty gas cylinder struck with a rubber mallet, a dull ringing note with a short metal tail, tuned to the root\|the tank ring` | Low; one note with a strange metallic bloom, a hook made of one hit. |
| A11 | struck | `a brake drum hit with a hammer, a short dead clang, three hits tuned to a minor triad\|the brake drum` | Low-mid; clank as melody, hard attack and almost zero sustain. |
| A12 | struck | `tuned rimshots on a tight snare shell, each one pitched, a five-note riff played where keys would be\|the pitched rimshots` | Mid; the drum kit carries the tune, so the beat itself is the hook. |
| A13 | struck | `a hollow log-drum note with a knocking front, played as the riff an octave above the bass\|the log knock` | Low; a knock that is also a bass note, bouncing between the kicks. |
| A14 | struck | `a length of plastic tube slapped on its open top, a deep round thwop, four tubes for four notes\|the tube slap` | Low; soft body and a slap on the front, comic and heavy together. |
| A15 | electronic | `an old mobile phone ringtone of five square-wave notes, tinny and dry, played an octave down\|the old ringtone` | Low-mid square wave, all edge; a sound the ear is trained to notice. |
| A16 | electronic | `a checkout scanner beep pitched into a three-note riff, each beep clipped square\|the scanner beep` | Mid; one flat hard tone, the riff is rhythm more than tune. |
| A17 | electronic | `a heart monitor beep pitched down to a soft square note, one per beat, one note a minor third up on beat 3\|the monitor beep` | Low-mid; steady pulse with a single changed note, tense and bare. |
| A18 | electronic | `a short pitched zap that falls a fifth in an instant, used as a note, three zaps per bar\|the zap` | Mid falling to low; a percussive blip with a pitch you can follow. |
| A19 | electronic | `a bit-reduced sine tone with coarse steps of grain, played as a slow two-note seesaw\|the stepped tone` | Low; a plain note made gritty, so it reads on a small speaker. |
| A20 | electronic | `a battery-starved toy keyboard whose notes sag flat as they sustain, one finger, three notes\|the sagging toy keys` | Low-mid; the sag is the wrong thing, cheap and sad. |
| A21 | machine | `a delivery truck warning beeper, one flat note pulsing on the off-beats, heard from across a street\|the truck beeper` | Mid; one note, pure placement, a street sound everybody knows. |
| A22 | machine | `a camera flash charging whine held as one thin note that bends a half step sharp\|the flash whine` | Mid, thin and nasal; one long note against short drums. |
| A23 | machine | `an electric screwdriver squeezed in short bursts, the motor note tuned to the root and the fifth\|the motor bursts` | Low, buzzing; a two-note riff with a rasp that carries on a phone. |
| A24 | machine | `an electric razor pressed against a tabletop, a low rasping note gated into short stabs\|the razor rasp` | Low; pure buzz, rich in overtones, cut into rhythm. |
| A25 | machine | `a subway door chime, two falling notes, sampled on a phone and pitched down a fourth\|the door chime` | Low-mid; two notes and a rest, tinny from the phone it was caught on. |
| A26 | machine | `a turn-signal relay click with a tuned low thump under it, tick-tock on two pitches\|the turn signal` | Low; a clock-like tick with a note in it, sits inside the drums. |
| A27 | machine | `a pneumatic nail gun, each shot a hard tick with a pitched tail of air, tuned to three notes\|the nail gun` | Mid; violent front, breathy pitched tail, three notes only. |
| A28 | synth | `a rubbery square-wave bass synth riff played an octave too high and too fast, dry and mono, five notes then a gap\|the hyper bass riff` | Low-mid; a bass line promoted to lead, urgent because it is too fast. |
| A29 | synth | `a hard-clipped saw synth playing one three-note hook, spread wide to both sides, the centre left for the kick\|the wide saw hook` | Mid with a harsh top; three notes, the width is the event. |
| A30 | synth | `a sine-wave lead gliding slowly between two notes, a little distortion so it buzzes on small speakers\|the gliding sine` | Low; nearly a bass, the glide is the hook, the buzz makes it audible. |
| A31 | synth | `a square-wave synth pluck with the filter almost shut, each note a soft knock with a short bright tick on top\|the muffled pluck` | Low-mid; knock plus tick, percussive, leaves the top of the mix open. |
| A32 | synth | `a two-oscillator synth stab tuned slightly apart so the pitches beat against each other, one stab on the and of 2\|the beating stab` | Mid; one sour hit per bar, the placement and the sourness are the hook. |
| A33 | synth | `a filter pinged by a click so it rings one short note, tuned for each note of a four-note riff\|the filter chirp` | Mid; a chirp with a hard front, electronic and dry. |
| A34 | synth | `a noise burst through a tuned comb filter so it carries a pitch, short and breathy, five hits per bar\|the tuned noise hit` | Mid; more texture than tone, a riff you feel as rhythm. |
| A35 | synth | `a synth lead that glides up a whole octave on the first note of the riff and stays there\|the octave glide` | Low sliding to mid; one wide leap, then stillness. |
| A36 | sampled | `a sped-up stab of massed strings and brass, two notes, the low end filtered off so it is thin and urgent\|the thin stab` | Mid, thin and bright; two notes, sped up so it sounds borrowed and tense. |
| A37 | sampled | `a brass-band horn hit sampled from a parade, one fat chord pitched down a third and cut to an eighth\|the parade hit` | Low-mid; a whole band in one short hit, played as a drum. |
| A38 | sampled | `a trumpet phrase with the low end filtered away so it sounds like a tin speaker, four notes, mono\|the tin-speaker trumpet` | Mid, narrow band; already sounds like a phone, so it survives one. |
| A39 | sampled | `a double-reed horn sample, nasal and piercing, pitched down a fifth so it growls, three notes\|the nasal reed` | Low-mid with a piercing edge; the reed buzz is all overtone. |
| A40 | sampled | `one piano note stretched to ten times its length until it turns into a grainy held tone\|the stretched grain tone` | Low; a familiar source made unfamiliar, one held note. |
| A41 | sampled | `a guitar feedback squeal caught in a sampler and played from pads, the same squeal at four pitches\|the sampled feedback` | Mid; a squeal used as a note, harsh and exact. |
| A42 | blown | `a plastic toy horn, one braying flat note bent a half step by the lips, short blasts\|the toy horn` | Mid; one rude note, the bend makes it human. |
| A43 | blown | `a duck call squeezed into three pitches, nasal and rude, played low\|the duck call` | Low-mid; reedy and funny, three notes, impossible to ignore. |
| A44 | blown | `a kazoo through a small amp, doubled an octave down, a buzzing four-note riff\|the amp kazoo` | Low; all buzz, a tune anyone can repeat after one pass. |
| A45 | blown | `a length of plastic drainpipe blown like a horn, a rough low honk, two honks per bar\|the drainpipe honk` | Low; a honk in place of a tuba, two hits and air. |
| A46 | blown | `a conch shell blown as one long hollow note that cracks upward as the breath runs out\|the shell horn` | Low-mid; one long note, the crack is the one strange moment. |

### 8.2 Table B: drum cadences (34)

Form: `placement|head tag`. Each is a fixed pattern of one bar or two, the same every time.
None overlaps the 24 entries already in `CADENCE`. Nothing is placed against the bar line: the latest hit in any pattern is a 16th after 4 (B4, B11).

| # | Type | Cadence |
|---|---|---|
| B1 | ghost note | `kick on 1 and a 16th after 2, the snare on 3, a quiet second snare a 16th after 3\|ghost snare after 3` |
| B2 | crowded front | `kick on 1, the and of 1 and the and of 2, a clap on 2 and 4\|three kicks crowding the front` |
| B3 | missing downbeat | `kick on the and of 1 and on 3, the snare on 2 and 4, beat 1 left to the bass alone\|bass alone on the one` |
| B4 | club five-kick | `kick on 1 and 2, then on the 16th before 3, the and of 3 and a 16th after 4, a clap on 2 and 4\|five kicks, the later three uneven` |
| B5 | half a dembow | `kick on every beat, rim hits on the 16th before 2 and on the and of 2 only\|rims crowding beat 2` |
| B6 | displaced snare, two bars | `a two-bar pattern, the snare on 3 in the first bar and on 4 in the second, the kick on 1 and the and of 2 both times\|snare on 3, then on 4` |
| B7 | split backbeat | `kick on 1 and the and of 3, the snare on 2 only, a rim click on 4\|snare on 2, rim on 4` |
| B8 | three against four | `five kicks spaced three 16ths apart starting on 1, the snare on 2 and 4\|kick every three 16ths` |
| B9 | front-loaded | `kick on 1, the 16th before 2 and the and of 2, one snare on 3, the second half of the bar left to the bass\|front-loaded kicks, lone snare` |
| B10 | early snare | `kick on 1 and the and of 2, the snare a 16th ahead of 2 and square on 4\|early snare ahead of 2` |
| B11 | late clap | `kick on 1 and the and of 3, claps on 2 and a 16th after 4\|second clap a 16th late` |
| B12 | empty beat | `the snare on 3, kicks on 1, the and of 3 and on 4, beat 2 left empty\|empty beat 2` |
| B13 | missing downbeat | `kick on 2 and the and of 3, the snare on 4 only, beat 1 carried by the bass\|kick waits for 2` |
| B14 | displaced snare | `a rim click on 2 and 4, the snare once on the and of 3, the kick on 1 and a 16th after 2\|snare on the and of 3` |
| B15 | flam | `kick on 1 and the and of 2, two snare hits a 16th apart with the second on 3, the first one soft\|flammed snare on 3` |
| B16 | clave rim | `a rim click on 1, the 16th before 2, the and of 2, the and of 3 and on 4, the kick only on 1 and 3\|clave rim over a plain kick` |
| B17 | kick answers snare | `the snare on 2 and 4, kicks on 1, the and of 2 and the 16th before 3\|two kicks chasing the snare` |
| B18 | bounce | `kick on 1, the and of 2 and on 3, a clap on 2 and 4 with a finger snap on top of it\|snap-and-clap bounce` |
| B19 | kick against snare | `the snare on 3 only, kicks on 1 and the 16th before 3 so the second kick sits tight against the snare\|kick tight against the snare` |
| B20 | two bars, one change | `a two-bar pattern, the first bar kick on 1 and snare on 3, the second bar the same with one more kick on the and of 2\|one extra kick in the second bar` |
| B21 | two bars, kick moves | `a two-bar pattern, kicks on 1 and the and of 2 in the first bar, on the and of 1 and on 3 in the second, the snare on 2 and 4 in both\|kick swaps sides` |
| B22 | two bars, snare thins | `a two-bar pattern, the snare on 2 and 4 in the first bar and only on 3 in the second, the kick on 1 in both\|backbeat bar, then a lone snare on 3` |
| B23 | lone off-beat snare | `kick on 1 and on 4, the snare on the and of 2 only\|lone snare on the and of 2` |
| B24 | displaced second snare | `kick on 1 and the 16th before 4, the snare on 2 and the and of 3\|snare on 2 and the and of 3` |
| B25 | ghost notes | `kick on 1 and the and of 2, a loud snare on 3, two quiet snare taps on the 16ths either side of 2\|quiet taps around 2` |
| B26 | role swap | `a low tom on 1 and the and of 1, a clap on 2 and 4, the kick only on 3\|tom, tom, clap` |
| B27 | clap on all four | `a clap on every beat, kicks on 1, the and of 2 and the 16th before 4\|clap on all four` |
| B28 | late first kick | `kick a 16th after 1 and on 3, the snare on 2 and 4, beat 1 marked by a rim click\|kick a 16th late off the one` |
| B29 | three against four | `three evenly spaced snares across the bar against a kick on every beat\|three snares over four kicks` |
| B30 | two bars, snare trades | `a two-bar pattern, kicks on 1 and 3 in both bars, the snare on 4 in the first bar and on 2 in the second\|one snare per bar, trading places` |
| B31 | bare fourth beat | `kick on 1, on 2 and the and of 2, the snare on 3, the fourth beat left bare\|bare fourth beat` |
| B32 | inverted | `kicks on 2 and 4, the snare on the and of 1 and the and of 3\|kick and snare trade places` |
| B33 | two bars, second trips | `a two-bar pattern, kicks on 1 and the and of 2 throughout, the first bar a snare on 2 and 4, the second bar a rim on 2 and the snare on the and of 3\|second bar trips on the and of 3` |
| B34 | three-hit backbeat | `one long kick on 1, a clap on 2, a snare on 3, a clap on 4\|clap, snare, clap` |

The five I would try first, because each is unmistakable in a head tag and unlike the trap
default: **B6** (snare on 3, then on 4), **B3** (bass alone on the one), **B8** (kick every
three 16ths), **B32** (kick and snare trade places), **B26** (tom, tom, clap).

### 8.3 Table C: one-time injections (24)

Form: `what happens|head tag`. C1 to C9 are a change of state that holds for the remainder of
the song (the reliable kind, and the form the switch layouts already use). C10 to C24 are a
single altered hit or a single altered bar; place them with one section tag in the Lyrics field
and expect a lower hit rate from Suno.

| # | Type | Injection |
|---|---|---|
| C1 | beat swap | `from the midpoint on, a second drum pattern: the snare moves from 2 and 4 to 3 alone and the kick thins to one per bar\|second half on a new drum pattern` |
| C2 | beat swap | `from the midpoint on, the kick pattern sits one 8th later against everything else, the snare where it was\|second half, kick an 8th late` |
| C3 | kit swap | `the second half keeps the pattern and swaps the kit: a deeper, longer kick and a rim click where the snare was\|second half, new kit, same pattern` |
| C4 | kit swap | `the second half trades the snare for a stack of handclaps and adds a kick on the and of 2\|claps take over in the second half` |
| C5 | rate change | `the second half doubles the hat rate while kick and snare hold their pattern\|hats twice as fast in the second half` |
| C6 | key change | `for the second half the whole beat sits a minor third lower, lead, bass and drums pitched together\|second half a minor third lower` |
| C7 | key change | `the final third sits a whole step higher, every sound moved together, the tempo unchanged\|final third a whole step up` |
| C8 | mode change | `the second half is in the parallel major: same riff, same root, the third raised\|second half turns major` |
| C9 | register swap | `from the midpoint on the riff and the bass trade registers, the riff down low and the bass line up top\|riff and bass trade registers` |
| C10 | snare slap | `one time only the snare is a hard digital slap, clipped and too loud, on its usual beat\|one clipped snare slap` |
| C11 | snare slap | `once in the song the snare lands a 16th early and twice, a flam that sounds like a sampler misfire\|one misfired snare` |
| C12 | snare slap | `once, two snares a 16th apart where one belongs, the second harder\|one double slap` |
| C13 | snare slap | `once, the snare is answered by its own copy a 32nd later and an octave down\|one shadowed snare` |
| C14 | snare slap | `once, a burst of bit-grain noise the length of a 16th stands in for the snare\|one noise snare` |
| C15 | mute | `once, the whole mix is muted for one beat, the hole exactly where the snare would be\|one muted beat` |
| C16 | mute | `once, everything but the bass is muted for a single bar\|one bar of bass alone` |
| C17 | freeze | `once, the lead holds a single flat note for a full bar while the drums carry on under it\|one frozen bar of lead` |
| C18 | pitch | `once, the lead plays its riff a tritone away from home for one pass\|one pass a tritone off` |
| C19 | pitch | `once, the riff is an octave higher for one pass\|one pass an octave up` |
| C20 | role swap | `for one bar only the kick takes the snare's places and the snare takes the kick's\|one swapped bar` |
| C21 | filter | `once, one bar plays through a telephone-thin filter, lows and highs removed, the pattern unchanged\|one bar on a tiny speaker` |
| C22 | width | `once, the mix folds to mono and bone dry for one bar\|one narrow bar` |
| C23 | speed | `once, one bar plays at half speed with the pitch an octave down\|one half-speed bar` |
| C24 | bass | `once, the 808 holds one note for two whole bars while the drums play on\|one long held 808` |

### 8.4 Frequency and mix phrases (15)

Each phrase is a number from section 1 said as a physical result (the mapping is in section 7).

| # | Phrase | Stands for |
|---|---|---|
| D1 | `a tuned 808 sitting very low and clean, one long round note under everything` | fundamental 37 to 50 Hz, tuned to the key |
| D2 | `the sub felt in the chest more than heard, pure and steady` | clean sine below 60 Hz |
| D3 | `a little grit on top of the 808 so every bass note reads on a phone speaker` | harmonics at 80 to 200 Hz and above |
| D4 | `a short knocking kick with a hard tick on the front, sitting just above the 808` | kick body 50 to 100 Hz, click 3 to 5 kHz |
| D5 | `kick and 808 taking turns, the kick's knock first and the 808's weight right behind it` | kick and bass sharing the low end |
| D6 | `a dry cracking snare, a wooden thump under a bright snap` | body 150 to 250 Hz, crack 2 to 5 kHz |
| D7 | `thin crisp hats tucked under the snare, airy on top` | 8 to 12 kHz, low in level |
| D8 | `the lead low but biting, nasal and buzzing, a hard bright edge on a low note` | overtones in 2 to 5 kHz |
| D9 | `the lead thin at the bottom, as if from a small speaker, clear of the bass` | lead high-passed |
| D10 | `a clear gap between the bass and the lead, the middle of the mix left open` | empty low mids |
| D11 | `the lead forward and dead centre, the loudest thing after the snare` | presence and level |
| D12 | `bass and kick in the centre, mono, only the hats and one small sound out wide` | low end mono, narrow image |
| D13 | `dry and close, short rooms, every tail kept short` | little reverb |
| D14 | `the whole beat reads on a small speaker, every part with an edge you can follow` | 500 Hz to 10 kHz translation |
| D15 | `loud and dense, every hit the same strength, the snare on top` | modern master, controlled dynamics |

### 8.5 How the tables were checked

Every string in 8.1 to 8.4 was extracted from this file by script and run through (a) the task's
banned-word list and a negation test (which also rejects "not", "never" and "nothing"), (b) the
strict rule set of `lib/guard.js` as it stood during this session (voice, meter, transition,
boundary, DJ-effect, EDM and film-score, lounge, negation, stock words; the file was being
edited by someone else, so the rules were copied into the check script) plus the `EVERY` cap,
and (c) the working-tree `check()` with its names list. The notes columns were checked too.
Result at the time of writing: 284 strings, zero hits. Lane ALLOW and DENY lists were not
applied; run `npm test` after lifting anything into `lib/`. Duplicates against the existing handles in
`lib/grammar.js`, `lib/lanes.js` and `lib/hints.js` were checked by hand, not by script.

---

## 9. Could not verify

- **Everything was read through search snippets.** No page was opened. The numbers most worth a
  one-click check: the hip hop 82 BPM average (T1), the 1.2:1 swing median (M1), the iPhone
  passband (F11), the 37, 50 and 74 Hz spectrum readings (F5).
- A **median 808 fundamental of 49.5 Hz** was attributed to the ISMIR paper in one snippet and
  could not be found again; left out of the tables.
- **No hip-hop-only key or mode statistic** for 2022 to 2026 was found. The mode figures are all
  genres; "hip hop favors minor" is the analysts' statement, not a percentage. The 56% minor
  figure for 2024 No. 1s comes from a secondary page only.
- **No hip-hop-only tempo distribution** beyond the single 82 BPM average. Per-record tempo and
  key come from automated databases that disagree with each other (see 2.2).
- **No published transcription** of the kick and snare on any 2022 to 2026 hit was found. The
  placements in 4.1 are genre-level descriptions (drill, Jersey club, dembow, trap, West Coast)
  plus one engineer's note on a 2017 record. Detroit, Milwaukee and sexy drill patterns: nothing
  usable found.
- **No millisecond measurement of any 2022 to 2026 rap record.** The timing numbers are a
  breakbeat corpus (M1), drum-machine settings (M2), tutorial advice (M3, M4) and arithmetic.
- Which Mix Review article gives the 300 to 800 Hz kick "knock", and which of two guides gives
  the 87 to 102 velocity window.
- The per-section keys of the three-beat 2018 record (one Wikipedia mirror).
- Whether Suno responds to Hz or millisecond values: no test found; the right-hand column of
  section 7 is judgment informed by the project's own listening notes.
- **2026 chart sound**: Billboard's rap chart pages for early 2026 were visible as titles only
  (a Jersey-club-associated producer had the rap No. 1 in February 2026, which fits the tempo
  pattern in 2.2), with no production description. Treat 2026 as uncovered.
- Beat-switch mechanics for several records I wanted (a 2022 Atlanta single with a soul sample,
  a 2022 Toronto and Atlanta duet, a 2023 Houston album cut): searches returned nothing usable.
- The percentages in the pop drum-pattern corpus study (C7).
- Table C has not been rendered. The single-hit injections (C10 to C24) are the least likely
  thing here to work as written.

## 10. Sources (all accessed 2026-11-23, via search snippets)

Frequencies and mix
- iZotope, How to Mix 808s: https://www.izotope.com/en/learn/how-to-mix-808s
- MusicRadar, 808 kick guide: https://musicradar.com/how-to/808-kick-guide
- 808 tuner page (promotional): https://bchillmix.com/pages/808-tuner
- Icon Collective, 808 mixing tips: https://iconcollective.edu/808-mixing-tips
- Kalu Bandali, make an 808 cut through a mix: https://payhip.com/KaluBandali/blog/news/make-a-808-cut-through-a-mix
- Harmonic and Transposition Constraints Arising from the Use of the Roland TR-808 Bass Drum (ISMIR 2024): https://arxiv.org/abs/2502.07524v1 and https://ismir2024program.ismir.net/poster_86.html
- Author's summary of the same: https://medium.com/@derutycsl/why-adjusting-the-songs-tonality-to-the-kick-drum-sample-is-a-good-idea-3905c6a68e1c
- The Mix Review (Mike Senior): https://themixreview.org/reviews/whats-my-name/ , https://themixreview.org/reviews/bad-habits/ , https://themixreview.org/reviews/mask-off/ , https://themixreview.org/reviews/dont-call-me-angel/ , https://themixreview.org/reviews/love-spreads/ , https://themixreview.org/reviews/bodak-yellow/ , https://themixreview.org/reviews/sticky/ , https://themixreview.org/reviews/paint-the-town-red/ , https://themixreview.org/reviews/
- iZotope, How to EQ Drums: https://www.izotope.com/en/learn/how-to-eq-drums
- ProSoundWeb, the art of the snare: https://www.prosoundweb.com/the-art-of-the-snare-getting-the-crack-thud-right-part-2-in-the-how-z-does-it-series/
- Music Guy Mixing, drum EQ chart: https://www.musicguymixing.com/drum-eq-chart/
- Guitar Center, EQ cheat sheet: https://www.guitarcenter.com/riffs/gear-tips/recording/eq-cheat-sheet-for-mixing
- Dummies, EQ frequency guidelines: https://dummies.com/article/home-recording-eq-frequency-guidelines-179779
- Equal-loudness contour: https://en.wikipedia.org/wiki/Equal-loudness_contour
- SFU Handbook for Acoustic Ecology: https://www.sfu.ca/sonic-studio-webdav/handbook/Equal_Loudness_Contours.html
- LANDR, bass audible on phone speakers: https://blog.landr.com/make-bass-audible-phone-speakers
- Sonarworks, mixes on small speakers: https://www.sonarworks.com/blog/learn/make-your-mix-sound-great-on-small-speakers
- Waves, mixes on small speakers: https://www.waves.com/tips-for-great-mixes-on-small-speakers
- Sound On Sound, Mixing For Mobile: https://www.soundonsound.com/techniques/mixing-mobile
- Psychoacoustic bass enhancement patent: https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/8625813
- Splice, mixing synths: https://splice.com/blog/mixing-with-neutron-2-synths
- Mastering The Mix, EQ for keys, synths and leads: https://www.masteringthemix.com/blogs/learn/how-to-eq-guitar-keys-synths-and-leads-when-mixing
- Fiveable, EQ and frequency balance: https://fiveable.me/electronic-music-composition/unit-13/eq-frequency-balance/study-guide/Sonz93YUGox3NlfE

Tempo, key, charts
- The Freeman, sonic characteristics of 2023's chart-toppers: https://www.pressreader.com/philippines/the-freeman/20240320/281668259962577
- Hit Songs Deconstructed, Q3 2023 highlights: https://www.hitsongsdeconstructed.com/wp-content/uploads/2023/11/Q3-2023-Top-10-2023-Highlights.pdf
- Hit Songs Deconstructed, 2024 No. 1 report: https://www.hitsongsdeconstructed.com/hsd_wire/hip-hop-was-on-top-songs-got-longer-hit-songs-deconstructeds-2024-no-1-hit-focus-report-reveals and https://hitsongsdeconstructed.com/wp-content/uploads/2025/02/The-Hit-Songs-Deconstructed-1-Hit-Focus-Report-Highlights.pdf
- Hit Songs Deconstructed, 2025 mid-year preview: https://www.hitsongsdeconstructed.com/wp-content/uploads/2025/07/The-State-of-the-Hot-100-Top-10-2025-Mid-Year-Report-Preview.pdf
- Hit Songs Deconstructed, deconstruction of the 2023 pop-rap No. 1: https://reports.hitsongsdeconstructed.com/2024/01/06/paint-the-town-red-deconstructed/
- MusicRadar, how to write a sticky hit: https://musicradar.com/artists/how-to-write-a-sticky-hit-pure-luck-can-put-you-in-the-charts-but-sticking-around-to-make-the-big-bucks-needs-pure-science
- Music 3.0 on ChartCipher: https://music3point0.com/?p=7797
- Secondary statistics page: https://voxbooster.com/blog/song-tempo-bpm-key-distribution-charts-statistics-2026
- No rap in the top 40: https://www.complex.com/music/a/markelibert/no-rap-songs-in-billboard-hot-100-top-40-for-the-first-time-since-1990 , https://www.thefader.com/2025/10/29/billboard-top-40-no-rap-songs-35-years , https://ca.billboard.com/music/rb-hip-hop/no-rap-songs-hot-100-top-40-first-time-since-1990-1236100625/
- Splice and MIDiA: https://www.musicbusinessworldwide.com/splice-which-hit-nearly-350m-downloads-in-2024-reveals-the-fastest-growing-genres-on-its-platform , https://www.hypebot.com/hypebot/2025/01/pluggnb-is-musics-fastest-growing-genre-on-splice-in-2024.html , https://edm.com/industry/whats-next-electronic-music-production-2026-splice-midia-research/
- Billboard Hot Rap Songs, 2026-02-07: https://www.billboard.com/charts/rap-song/2026-02-07/
- Per-record tempo and key: https://www.karaoke-version.com/mp3-backingtrack/kendrick-lamar/not-like-us.html , https://hooktheory.com/theorytab/view/kendrick-lamar/not-like-us , https://melodics.com/blog/how-to-play-not-like-us-keys , https://songbpm.com/@kendrick-lamar/tv-off-gq4pg , https://tunebat.com/Info/tv-off-feat-lefty-gunplay-Kendrick-Lamar-Lefty-Gunplay/75bsrBtmM57lhi55yOfzBL , https://planetary.music/song-analysis/kendrick-lamar/tv-off , https://www.karaoke-version.com/mp3-backingtrack/future/like-that.html , https://songs.klang.io/en/s/Future/Like%20That/N9bKBAA22Go , https://www.karaoke-version.com/mp3-backingtrack/lil-uzi-vert/just-wanna-rock.html , https://www.hooktheory.com/theorytab/view/lil-uzi-vert/just-wanna-rock

Records, leads, producers
- https://en.wikipedia.org/wiki/Not_Like_Us , https://www.songfacts.com/facts/kendrick-lamar/not-like-us
- https://complex.com/music/a/markelibert/mustard-says-he-made-not-like-us-beat-in-30-minutes , https://www.theroot.com/not-like-us-producer-mustard-breaks-down-making-kendric-1851559221 , https://www.musicradar.com/news/kendrick-not-like-us-dj-mustard
- https://en.wikipedia.org/wiki/TV_Off , https://www.songfacts.com/facts/kendrick-lamar/tv-off , https://www.complex.com/music/a/backwoodsaltar/kendrick-lamar-mustard-tv-off-memes , https://switchedonpop.substack.com/p/kendrick-lamar-from-pulitzer-to-pop
- https://www.okayplayer.com/mustard-was-trying-to-make-a-west-coast-version-of-grindin-for-kendrick-lamars-hey-now/313325
- https://www.songfacts.com/facts/kendrick-lamar/squabble-up , https://www.wikiwand.com/en/articles/Squabble_Up
- https://en.wikipedia.org/wiki/Paint_the_Town_Red_(Doja_Cat_song) , https://stereogum.com/2236347/a-composer-breaks-down-the-music-theory-behind-doja-cats-1-hit-paint-the-town-red/columns
- https://en.wikipedia.org/wiki/Like_That_(Future,_Metro_Boomin_and_Kendrick_Lamar_song) , https://www.songfacts.com/facts/future-and-metro-boomin/like-that
- https://www.songfacts.com/facts/drake/nokia , https://wiki.krisyotam.com/content/wikipedia_en_all_nopic_2026-03/Nokia_(song) , https://www.hotnewhiphop.com/885823-drake-nokia-stream , https://www.complex.com/music/a/tracewilliamcowen/drake-elkan-nokia-beat
- https://stereogum.com/2285632/premature-evaluation-tyler-the-creator-chromakopia/reviews/premature-evaluation , https://grammy.com/news/tyler-the-creator-chromakopia-new-album-review-takeaways , https://www.complex.com/music/a/tracewilliamcowen/tyler-the-creator-sticky-marching-band-video
- https://www.rollingstone.com/music/music-news/roddy-ricch-the-box-934137/ , https://revolt.tv/2020/1/10/21059055/travis-scott-roddy-ricch-producer-30-roc-interview , https://articles.roland.com/?p=28795
- https://www.vice.com/en/article/part-three-of-our-jersey-club-documentary-delves-into-how-the-genre-went-global/ , https://orphiq.com/resources/what-is-jersey-club , https://blog.native-instruments.com/jersey-club/ , https://blog.mixcloud.com/2023/07/05/mixclouds-on-rotation-a-guide-to-jersey-club/ , https://en.wikipedia.org/wiki/Just_Wanna_Rock
- https://en.wikipedia.org/wiki/Rage_(music_genre) , https://hotnewhiphop.com/431841-the-rise-of-rage-music-news , https://en.wikipedia.org/wiki/Plugg
- https://www.okayplayer.com/cash-cobain-sexy-drill , https://www.revolt.tv/article/explaining-sexy-drill-and-some-of-the-subgenre-top-artists
- https://www.dazeddigital.com/music/article/21210/1/mustard-power
- https://floodmagazine.com/199821/clipse-let-god-sort-em-out/

Drum patterns and timing
- https://tidal.com/magazine/article/what-makes-trap-trap/1-72722 , https://musicradar.com/news/beginners-guide-to-trap
- https://en.wikipedia.org/wiki/UK_drill , https://www.attackmagazine.com/technique/beat-dissected/uk-drill/ , https://beatstorapon.com/blog/how-to-make-a-drill-beat/
- https://orphiq.com/resources/what-is-dembow , https://en.wikipedia.org/wiki/Dembow , https://daily.bandcamp.com/lists/dembow-rhythm-list
- https://www.attackmagazine.com/technique/beat-dissected/west-coast-hip-hop/
- Geary, Formal Functions of Drum Patterns in Post-Millennial Pop Songs, 2012 to 2021: https://mtosmt.org/issues/mto.24.30.2/mto.24.30.2.geary.html
- Frane 2017, Swing Rhythm in Classic Drum Breaks From Hip-Hop's Breakbeat Canon: https://www.sciencegate.app/document/10.1525/mp.2017.34.3.291
- Attack, swing in DAWs and drum machines: https://www.attackmagazine.com/technique/passing-notes/daw-drum-machine-swing/
- Attack, Roger Linn interview: https://www.attackmagazine.com/features/interview/roger-linn-swing-groove-magic-mpc-timing/3/
- Melodiefabriek, MPC swing: https://melodiefabriek.com/blog/mpc-swing-reason
- Attack beats: https://www.attackmagazine.com/?p=2114 , https://attackmagazine.com/?p=27482 , https://www.attackmagazine.com/technique/beat-dissected/drunk-drummer-style-grooves/
- Sample Focus, programming grooves: https://samplefocus.com/blog/swing-shuffle-and-humanization-how-to-program-grooves.md
- iZotope, humanize and dehumanize drums: https://www.izotope.com/en/learn/how-to-humanize-and-dehumanize-drums
- SoundBridge, drum programming tips: https://soundbridge.io/en/drum-programming-tips-for-producers-of-all-levels
- Waves, humanizing beats: https://www.waves.com/humanizing-beats-anderson-paak-focus
- RITMO, a fraction of a second: https://uio.no/ritmo/english/news-and-events/news/2020/a-fraction-of-a-second-is-all-you-need
- Danielsen et al. 2023, Shaping rhythm: timing and sound in five groove-based genres: https://duo.uio.no/handle/10852/101729
- The Pudding, groove essay: https://pudding.cool/2023/06/groove/
- Ethan Hein, Dilla Time: https://ethanhein.com/wp/2022/dilla-time/
- Alton, Hershon and Nandhra, Living Off-Grid: https://musicscience.net/wp-content/uploads/2025/11/8-alton-et-al-livingoffgrid-p.-62-71.pdf

Injections and minimalism
- https://tedium.co/2022/11/09/the-death-of-the-key-change , https://www.npr.org/2022/11/30/1139707179/where-did-all-the-key-changes-go
- https://www.songfacts.com/facts/travis-scott/sicko-mode , https://en.wikipedia.org/wiki/Sicko_Mode
- https://districtmagazine.ie/music/a-brief-history-of-the-beat-switch-in-hip-hop , https://www.complex.com/music/a/dimassanfiorenzo/best-hip-hop-beat-switches-of-all-time
- https://blog.samplefocus.com/blog/using-space-and-silence-in-music-production , https://www.phoenixnewtimes.com/music/a-whisper-in-the-crowd-6407215/
- Percino, Klimek and Thurner 2014, Instrumentational Complexity of Music Genres and Why Simplicity Sells: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4281146/

Text-to-music models
- https://paperswithcode.com/dataset/musiccaps , https://ar5iv.labs.arxiv.org/html/2301.11325 , https://arxiv.org/html/2311.08355v3 , https://arxiv.org/html/2509.00654v1
