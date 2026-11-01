# Addictive sound theory: hooks, groove, and three production references (research for suno-forge)

Research date: 2026-11-01. All sources accessed 2026-11-01.

## How this was researched, and a caveat on access

Direct page fetches were blocked for every academic and reference host tried in this session
(research.gold.ac.uk, pmc.ncbi.nlm.nih.gov, mtosmt.org, vijayiyer.scholars.harvard.edu,
www.frontiersin.org, en.wikipedia.org, musicmachinery.com, www.whosampled.com, www.duo.uio.no:
all "not in the session's allowed network domains"). Every claim below therefore rests on
search-engine snippets of the named page, with the URL given so the project can confirm with one
click. Labels:

- **VERIFIED** = the claim appears in a snippet of the primary source (the paper's abstract or
  repository page, the musician's own words, the label's or museum's own page).
- **SECONDARY** = the claim appears in press coverage, a citing paper, or a reputable
  music-tech outlet, but the primary text was not read.
- **UNVERIFIED** = single weak source, sources conflict, or the number could not be found.

Artist, producer and record names appear in this file **only** as research. None of them may
enter a prompt (`lib/guard.js` NAMES list; CLAUDE.md rule 1). Section 4 is the only part of this
file whose strings are meant to be lifted toward `lib/`, and every descriptor there was run
through `lib/guard.js` `check()` with no lane ALLOW and returned clean.

---

## 1. Earworm and hook science

### 1.1 What the research says

| # | Finding | Status | Source |
|---|---|---|---|
| H1 | Jakubowski, Finkel, Stewart & Müllensiefen, *Psychology of Aesthetics, Creativity, and the Arts* (online 2016, print 2017), "Dissecting an earworm": 3,000 people surveyed; the 100 most-named earworm tunes were matched against 100 never-named tunes of similar chart popularity. Earworm tunes were **faster in tempo**, had **more common (generic) global melodic contours** (e.g. first phrase rises, second falls, the "Twinkle Twinkle" shape), and **more unusual interval patterns** (unexpected leaps, or more repeated notes than the pop average). Examples the authors named for the unusual-interval ingredient: the instrumental riff of "My Sharona" and "In the Mood". Jakubowski's phrase: an "optimal level of complexity" the brain can latch on to. Most-named earworms: "Bad Romance", "Can't Get You Out of My Head", "Don't Stop Believin'", "Somebody That I Used to Know", "Moves Like Jagger". Exposure (recent, frequent) also predicted INMI. | SECONDARY (press of the paper; abstract page exists at Goldsmiths/Durham repositories but could not be fetched) | [APA press release](https://www.apa.org/news/press/releases/2016/11/earworms), [Goldsmiths news](https://gold.ac.uk/news/scientists-find-key-to-writing-catchy-pop-hits), [Durham repository](https://dro.dur.ac.uk/20274), [Goldsmiths eprint](https://research.gold.ac.uk/19405), [Live Science](https://livescience.com/56739-what-makes-an-earworm-so-catchy.html), [Time](https://time.com/4557229/earworm-music) |
| H2 | Williamson & Müllensiefen 2012, "Earworms from three angles" (ICMPC proceedings): computational comparison of several hundred earworm tunes against matched controls; precursor pilot Finkel, Jakubowski & Müllensiefen 2010 used 29 vs 29 tunes and logistic regression over 40 features. The 2012 paper's exact feature list could not be read; its authors suggested the features make tunes easier to sing along with, and that people who sing more report more and longer INMI. | SECONDARY | [Goldsmiths eprint 10192](https://research.gold.ac.uk/id/eprint/10192), [Finkel & Müllensiefen SysMus10](https://eprints-gro.gold.ac.uk/10204/1/SysMus10_FinkelMullensiefen_2010.pdf), [Vice](https://www.vice.com/en/article/why-songs-get-stuck-in-your-head) |
| H3 | Beaman & Williams 2010, *British Journal of Psychology* 101(4): diary study, earworm episodes averaged ~27 minutes; earworm potential depended on **exposure combined with the tune's simplicity and repetitiveness**; most attempts to suppress made it worse. | SECONDARY (BPS Research Digest) | [BPS Research Digest](https://bps.org.uk/research-digest/natural-history-earworm-song-wont-get-out-your-head) |
| H4 | Hyman et al. 2013, "Going Gaga", *Applied Cognitive Psychology* 27: proposed that hearing only **part** of a song leaves it incomplete and more likely to return (a Zeigarnik-style account). Later testing by other groups found **no reliable evidence** that playing a fragment triggers an earworm; Claire Arthur's review is cited as the sceptical summary. Treat Zeigarnik as a plausible hypothesis, not a confirmed mechanism. | SECONDARY, contested | [BPS "Z is for Zeigarnik"](https://bps.org.uk/psychologist/z-zeigarnik-effect), [Emory Arts & Brain blog](https://scholarblogs.emory.edu/artsbrain/?p=1469), [Reading review (Arthur)](https://centaur.reading.ac.uk/79201/) |
| H5 | Margulis, *On Repeat* (OUP 2013): repetition sets up pleasurable expectation and pulls the listener "into" the music; repeated motifs become shared property. Margulis 2013, *Empirical Studies of the Arts* 31(1): excerpts of unfamiliar post-tonal music with repetition **inserted** were rated more enjoyable, more interesting and more artistic than the untouched originals. (The specific composers used and the "human-made vs computer-made" framing could not be confirmed from snippets.) | VERIFIED (abstract wording in snippet) | [Princeton listing](https://collaborate.princeton.edu/en/publications/aesthetic-responses-to-repetition-in-unfamiliar-music/), [U. Arkansas news](https://news.uark.edu/articles/22615/repetition-in-music-pulls-us-in-and-pulls-us-together), [MTO review](https://www.mtosmt.org/issues/mto.14.20.4/mto.14.20.4.albrecht.php) |
| H6 | Huron, *Sweet Anticipation* (MIT 2006): the ITPRA theory; expectation evokes emotion through five response systems (imagination, tension, prediction, reaction, appraisal); syncopation, cadence, meter, tonality and climax are catalogued as devices that exploit those systems. The payoff of a correct prediction is pleasant; a near-miss that resolves is more pleasant still. | SECONDARY (publisher summary, reviews) | [ResearchGate front matter](https://researchgate.net/profile/David-Huron-2/publication/209436188_Sweet_Anticipation_Music_and_the_Psychology_of_Expectation/links/0c96052e02be139f9f000000/Sweet-Anticipation-Music-and-the-Psychology-of-Expectation.pdf), [Leonardo review](https://leonardo.info/reviews_archive/july2007/sweet_zilberg.html) |
| H7 | Salimpoor, Benovoy, Larcher, Dagher & Zatorre 2011, *Nature Neuroscience* 14: PET ([11C]raclopride) plus fMRI, 8 listeners selected from 217; dopamine released in the striatum at peak emotion (chills); **caudate** activity rose during **anticipation** (secondary accounts say up to ~15 s before the chill), **nucleus accumbens** at the **peak** itself. Anticipation and payoff are anatomically distinct rewards. | VERIFIED (abstract wording in snippet; the 15 s window is SECONDARY) | [Zatorre lab PDF](https://www.zlab.mcgill.ca/publications/docs/salimpoor_2011_nn.pdf), [BRAMS](https://brams.org/2011/01/09/salimpoor/), [Medical News Today](https://www.medicalnewstoday.com/articles/213412) |
| H8 | Burgoyne, Bountouridis, Van Balen & Honing 2013, "Hooked: a game for discovering what makes music catchy" (ISMIR): catchiness defined by effect on long-term memory; the public version (1,000 clips, ~12,000+ players) measured recognition times, fastest around 2.3 s for the top clip. Secondary reading: simple, repetitive material plus a strong melodic hook recognised fastest. | SECONDARY | [ISMIR 2013 paper](https://archives.ismir.net/ismir2013/paper/000101.pdf), [UvA abstract](https://www.mcg.uva.nl/mcg-2023/abstracts/Burgoyne-et-al-2013.html), [Vice interview](https://www.vice.com/en/article/we-spoke-to-the-scientist-who-knows-which-songs-stick-in-your-head/) |
| H9 | Streaming skips: Lamere (Spotify data, 2014) found a **24.14% chance a song is skipped within the first 5 seconds**, 35.05% within 30 s; most skips happen in the first ~20 s. Léveillé Gauvin 2017, *Musicae Scientiae* ("Drawing listener attention in popular music"): 303 US top-10 singles 1986-2015; average instrumental intro fell from >20 s to ~5 s (a 78% drop); tempo rose ~8%; titles got shorter; the title is sung earlier. His second study (60 popular vs 60 less popular 2015 songs by the same artists) found **no** within-artist support for the attention-economy hypothesis. The widely quoted "hook in the first 7 seconds" rule and "intros over 7 s are twice as likely to be skipped" (newmusicworld.org, "Dr. Clara Enzo") could not be traced to any study and should be treated as marketing. | VERIFIED (Lamere and OSU/Hypebot summaries of the paper); "7 seconds" UNVERIFIED | [Lamere, Music Machinery](https://musicmachinery.com/category/analytics/), [OSU news](https://news.osu.edu/has-music-streaming-killed-the-instrumental-intro/), [Hypebot](https://www.hypebot.com/hypebot/2017/04/did-streaming-kill-the-instrumental-intro.html), [Sonicfield abstract](https://sonicfield.org/library/drawing-listener-attention-in-popular-music-testing-five-musical-features-arisin), [arXiv skipping-and-structure paper](https://arxiv.org/pdf/1903.06008) |

### 1.2 Turning each finding into an instrumental writing device

The house style: a sound is a physical description with a handle; structure lives in the Lyrics
field as bracket tags; nothing is a label. The devices below are written so they can be dropped
into `lib/grammar.js` motif/motion slots or `lib/layouts.js` cue text.

| Device | Comes from | Instrumental form (prompt-ready phrasing) |
|---|---|---|
| D1 **Generic contour, one wrong interval** | H1 | "a four-note lead motif that climbs then falls, the third note a tritone leap the ear does not expect, the same four notes every pass" |
| D2 **Repeated note as the hook** | H1 (repeated notes count as unusual intervals), Runaway-style single-note openings | "one note of the detuned piano struck on every eighth, the same pitch for two bars, then one note a half step down on the last eighth" |
| D3 **Fast surface over a slow pulse** | H1 (faster tempo predicts INMI) in a half-time lane | "the lead moves in sixteenths while the drums sit half-time, so the motif feels quick and the pocket feels heavy" |
| D4 **Repeat four, change one** | H5 (repetition with small variation is rated more enjoyable and more interesting) | "the motif plays four times; on the fourth pass one note is a half step lower and the rest are identical" |
| D5 **Stop one note short** | H4 (incomplete tune returns; contested but cheap to use), H6 (unresolved prediction) | "the three-note motif stops one note short of the root every time, the last note left hanging over the kick" |
| D6 **Anticipation gap** | H7 (caudate fires before the payoff) | "two bars of the bass alone with the hats, the lead handle held back, then the full beat straight in on beat 1 of bar 3" (hard cut, never a riser) |
| D7 **Simple shape, rhythmic hiccup** | H1 contour + H2 "singable" + groove section below | "a five-note motif on a simple rise-and-fall shape, the second note always a sixteenth late, every pass" |
| D8 **Hook before 0:05, full beat by 0:45** | H9 (24% skipped by 5 s, intros now ~5 s) | Layout rule: the lead handle sounds inside the first 2 bars; the `[Hook]` with the full beat lands by 0:45 (already a project rule; H9 is its evidence) |
| D9 **Recognition in two seconds** | H8 (fastest hooks recognised in ~2.3 s) | "the lead sound is the first thing heard, alone, its timbre unmistakable in one note: a grand piano through a fuzz pedal, low register" (identity in the first sound, not in the arrangement) |
| D10 **Exposure, not novelty, inside the song** | H3 (exposure + simplicity + repetitiveness) | Loop-layout rule: the lead motif is never longer than two bars and recurs in every section; variation lives in the drums and the counter, never in the hook pitches |

Devices D1, D4, D5 and D7 can combine into one motif line: "a four-note motif, rise then fall,
one tritone leap, the second note a hair late, played four times with one note changed on the
fourth pass, always stopping one note short of the root".

---

## 2. Groove science

### 2.1 What the research says

| # | Finding | Status | Source |
|---|---|---|---|
| G1 | Witek, Clarke, Wallentin, Kringelbach & Vuust 2014, *PLoS ONE* 9(4) e94446, "Syncopation, body-movement and pleasure in groove music": web survey, funk drum-breaks varied in syncopation and entropy; **medium syncopation produced the strongest urge to move and the most pleasure (an inverted U)**; entropy was a poor predictor; the effect was strongest in people who like dancing. Witek's framing: syncopation is an invitation to the body to fill the metre. Sample size reported as ~60 (PsyPost) to ~100 (altmetric tables); a 2015 correction fixed stimulus indexing without changing the results. | VERIFIED (abstract wording in snippets); sample size UNVERIFIED | [Birmingham repository](https://research.birmingham.ac.uk/en/publications/syncopation-body-movement-and-pleasure-in-groove-music/), [PMC correction](https://pmc.ncbi.nlm.nih.gov/articles/PMC4581707), [Aarhus CFIN news](https://cfin.au.dk/news-events/news/show/artikel/new-article-in-plos-one-syncopated-rhythm-may-influence-our-desire-to-dance-to-music), [PsyPost](https://www.psypost.org/in-funk-music-rhythmic-complexity-influences-dancing-desire/) |
| G2 | Sioros, Miron, Davies, Gouyon & Madison 2014, *Frontiers in Psychology*: in synthesized melodies, **moderate syncopation produced significantly higher groove ratings** than either none or the maximum possible. Matthews, Witek, Heggli, Penhune & Vuust 2019, *PLoS ONE*: groove depends on the **interaction** of rhythmic and harmonic complexity (medium rhythmic complexity still wins; harmonic complexity shifts the peak). | VERIFIED (abstracts) | [Frontiers 2014 text](https://public-pages-files-2025.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2014.01036/text), [INESC TEC record](https://repositorio.inesctec.pt/items/1aa8eba1-cfdf-41c8-bc1d-013f777802ee), [PMC Matthews 2019](https://pmc.ncbi.nlm.nih.gov/articles/PMC6328141) |
| G3 | Janata, Tomic & Haberman 2012, *J. Exp. Psychol.: General* 141: groove is a measurable psychological construct; high-groove excerpts provoke spontaneous movement; groove ratings fall as tapping along gets harder. Stupacher, Hove & Janata (reusing the 2012 clips) found loudness did not drive groove. | VERIFIED (abstract) | [Zenodo record](https://zenodo.org/record/997607), [SysMus14 "Decrypt the groove"](https://journals.gold.ac.uk/index.php/sysmus14/article/view/256) |
| G4 | Etani et al. 2018, *Frontiers in Psychology*, "Optimal tempo for groove": drum patterns at six tempi 60-200 BPM; groove peaked at roughly **100-120 BPM**, linked to the tempo of comfortable back-and-forth body movement. | VERIFIED (abstract) | [Western Sydney record](https://researchers.westernsydney.edu.au/en/publications/optimal-tempo-for-groove-its-relation-to-directions-of-body-movem/), [DOAJ](https://doaj.org/article/087a169f341d4631a844e98cea7debf3) |
| G5 | Iyer 2002, *Music Perception* 19(3) 387-414, "Embodied mind, situated cognition, and expressive microtiming in African-American music": microtiming (the "laid-back" backbeat, the late or early placement of events inside a steady pulse) is a product of the body making music and of a cultural aesthetic that prizes that body; audio examples from well-known jazz recordings. The exact millisecond claims could not be read (PDF host blocked). | SECONDARY (abstract/library summary) | [Harvard publication page](https://vijayiyer.scholars.harvard.edu/publications/embodied-mind-situated-cognition-and-expressive-microtiming-african-american), [Sonicfield summary](https://labs.sonicfield.org/library/embodied-mind-situated-cognition-and-expressive-microtiming-in-african-american-) |
| G6 | Butterfield 2006, *Music Theory Online* 12.4, "The Power of Anacrusis": Keil's "engendered feeling" comes from participatory discrepancies interacting with syntactical pattern, mainly anacrusis at several levels; analysed on the jazz ride pattern, the basic rock beat and the groove of "Chameleon". **Slight delays in bass and drums temper strongly anacrustic onsets and give a more relaxed groove; earlier placement drives forward.** Benadon's 2007 commentary and Butterfield's reply debate whether the late notes are heard as late. Butterfield 2010, *Music Perception*: listeners could not consistently detect discrepancies of 30 ms or less. | SECONDARY (abstract + commentary) | [MTO article](https://mtosmt.org/issues/mto.06.12.4/mto.06.12.4.butterfield.php), [Benadon commentary](https://mtosmt.org/issues/mto.07.13.1/mto.07.13.1.benadon.php), [Butterfield reply](https://mtosmt.org/issues/mto.07.13.3/mto.07.13.3.butterfield.php), [Sonicfield 2010 summary](https://labs.sonicfield.org/library/participatory-discrepancies-and-the-perception-of-beats-in-jazz) |
| G7 | Danielsen, Waadeland, Sundt & Witek 2015, *JASA* 138(4) 2301-2316, "Effects of instructed timing and tempo on snare-drum sound": 10 expert drummers, rock pattern, three tempi, laid-back / on-the-beat / pushed. **Laid-back snare strokes landed ~17.4 ms late on average at 96 BPM** (figure quoted by Carter & von Appen) and **were played louder** than on-the-beat strokes; faster tempi raised level and lowered spectral centroid. Câmara et al. 2020 (*Music Perception* 38(1)) add that laid-back and pushed strokes also differ in snare **duration** and intensity, and that playing against an instrumental reference rather than a click produced stronger timing profiles. | VERIFIED (abstract: louder laid-back strokes); 17.4 ms SECONDARY | [Birmingham record](https://research.birmingham.ac.uk/en/publications/effects-of-instructed-timing-and-tempo-on-snare-drum-sound-in-dru/), [UiO record](https://duo.uio.no/handle/10852/55787?show=full), [Carter & von Appen](https://tnp.mtsnys.org/vol49-50/carter_von_appen), [Câmara 2020](https://duo.uio.no/handle/10852/79965) |
| G8 | Microtiming does **not** reliably raise groove in the lab: Davies, Madison, Silva & Gouyon 2013 (systematic microtiming lowered groove, liking and naturalness in synthesized jazz, funk and samba, except the simple jazz pattern); Frühauf, Kopiez & Platz 2013 (*Musicae Scientiae*: quantized version rated highest, "aesthetics of exactitude"); Senn et al. 2016 (*Frontiers*: original performed microtiming and fully quantized rated equally high, exaggerated timing rated lower); Kilchenmann & Senn 2015 (motion tracking, 160 listeners: only music experts moved more, and to tight-but-nonzero timing); Senn, Kilchenmann, Bechtold & Hoesl 2018 (*PLoS ONE*, 248 drum patterns: syncopation R² = 0.010 and event density R² = 0.011 both weakly positive; residual microtiming, beat salience and variability had **no** effect; listeners' style bias R² = 0.123 was the largest factor). | VERIFIED (abstracts) | [INESC TEC 2013](https://repositorio.inesctec.pt/handle/123456789/3901?mode=full), [Sonicfield Frühauf](https://labs.sonicfield.org/library/music-on-the-timing-grid-the-influence-of-microtiming-on-the-perceived-groove-qu), [PMC Senn 2016](https://pmc.ncbi.nlm.nih.gov/articles/PMC5050221), [Frontiers Kilchenmann 2015](https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2015.01232/full), [PMC Senn 2018](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6025871/) |
| G9 | Dilla time (Charnas, *Dilla Time*, 2022; Hein's grid analyses): not a midpoint between straight and swung but **several feels at once**: hats on a straight grid, kicks leaning late, snares/claps a hair early, bass sometimes a full 32nd late, melodic parts floating between. Built on an MPC3000 (96 PPQ) by nudging individual hits ("baby hairs"), quantize off; deliberate, never sloppy. Hein's method: anchor on the downbeat hats, then measure. Because the claps are loudest the ear orients to them and everything else sounds dragged. Danielsen's "Here, There and Everywhere" chapter (*Musical Rhythm in the Age of Digital Reproduction*, 2010) gives three competing accounts of the pulse in a Voodoo-era track built on the same feel; her 2024 "beat bins" paper formalises the idea that a beat is a window, not a point. | SECONDARY (reviews, blog analyses, book TOC) | [Hein, "Dilla Time"](https://ethanhein.substack.com/p/dilla-time), [Hein, "Get Dis Money"](https://ethanhein.com/wp/2022/get-dis-money/), [Hein, "Chameleon"](https://ethanhein.com/wp/2022/dilla-time-in-chameleon/), [Okayplayer interview](https://www.okayplayer.com/culture/j-dilla-time-book-dan-charnas-interview.html), [Ethnomusicology Review](https://ethnomusicologyreview.ucla.edu/node/1106), [Danielsen TOC](https://catalogo.abie.es/Record/unav.b33426533/TOC), [Danielsen 2024 "beat bins"](https://labs.sonicfield.org/library/theres-more-to-timing-than-time) |

### 2.2 Devices with the numbers behind them

| Device | Numbers | Instrumental form (prompt-ready phrasing) |
|---|---|---|
| **Medium syncopation, one surprise per bar** | G1, G2: medium syncopation beats both none and maximum; G8: event density and syncopation each add only ~1% of variance, so more is not better | Drums line: "the kick on the one and the and-of-two, the snare on three, one off-beat 808 on the and-of-four, the rest of the bar straight" plus the anti-transition phrase. Rule: one displaced hit per bar in the kick or 808, the snare and hats stay put. |
| **The laid-back snare: late, louder, longer** | G7: ~17 ms late at 96 BPM, played harder, longer decay; G6: delay relaxes, early drives; Butterfield 2010: under 30 ms is felt, not heard | "the snare a hair late and hit harder for it, its ring left to decay" ("a hair" is the right unit: 15-25 ms at 85-100 BPM is below what listeners can name but above what they feel). Never say the number in the prompt; Suno reads "17 ms" as noise. |
| **Hats straight, kick leaning, snare early** (the drunk pocket) | G9 | "hats dead straight on the grid, the kick leaning late behind them, the snare a hair early so the pocket feels pulled two ways" (the project's beat-3 slur, "the 808 stumbles into three on beat 3", is the same device concentrated on one beat) |
| **Tempo band** | G4: groove peaks ~100-120 BPM felt pulse | Half-time lanes written at 120-160 BPM have a felt pulse of 60-80; the lead moving in eighths or sixteenths restores a 100-120 "movement rate". Loop and live-pocket lanes at 88-100 BPM already sit inside the band. |
| **Exactness beats chaos** | G8: quantized and performed-tight rate equal; exaggerated timing rates lower | Keep "slight" and "subtle" on every human-timing clause; one loosened element per roll (snare OR hats OR 808), never all three. The project's "a few cents off, never corrected" and "slight" modifiers are the right dose. |
| **Timing lives on a sound, not a grid** | G7, Câmara 2020: laid-back strokes differ in loudness and duration, and timing is heard against the other instruments, not a click | Say it as performance: "played by hand against the bass, the second hit always a hair late" rather than "swung" or "humanised". |

---

## 3. Production facts behind three references and the hit structures

Names are here for the project's understanding only. Nothing in this section is prompt text;
the prompt-ready translations are in section 4.

### 3.1 Funkadelic, "Maggot Brain" (1971), the Eddie Hazel solo

| Fact | Status | Source |
|---|---|---|
| Ten-minute title track, mostly one improvised guitar solo, recorded in **one take**; George Clinton told Hazel to play as if he had just learned his mother had died. | VERIFIED (multiple consistent accounts incl. Clinton) | [Fender](https://fender.com/articles/artists/one-track-mind-the-passion-of-eddie-hazel-and-funkadelics-maggot-brain), [NPR at 50](https://www.npr.org/2021/07/22/993084954/making-funkadelic-maggot-brain) |
| Guitar chain: **fuzz** and **wah** (Hendrix-derived). Which fuzz is disputed: Maestro FZ-1 Fuzz-Tone vs Electro-Harmonix Big Muff (Clinton: "the wah wah, the Big Muff, and phasers"); wah usually cited as a Cry Baby. Hazel's general rig lists a 100-watt Marshall, MXR Phase 90 and Echoplex, but the phase shifter's role on this specific take is not established. | SECONDARY; fuzz model UNVERIFIED | [Equipboard](https://equipboard.com/pros/eddie-hazel), [guitar.com](https://guitar.com/review/album/the-genius-of-maggot-brain-by-funkadelic/), [Rock and Roll Globe](https://rockandrollglobe.com/funk/the-odyssey-of-funkadelic-and-eddie-hazel/) |
| The long **tape delay** was added at mixdown: Clinton, "When Eddie played originally it was over a more traditional slow band jam. I took all the other instruments off the track and then I Echoplexed it back on itself three or four times." | VERIFIED (Clinton memoir quote via Fender) | [Fender](https://fender.com/articles/artists/one-track-mind-the-passion-of-eddie-hazel-and-funkadelics-maggot-brain) |
| The **band was mixed down**: Clinton pulled most of the bass and drums out, leaving room for the echo; "ghostly echoes of drums occasionally but mostly the guitars". | VERIFIED (NPR) / SECONDARY | [NPR](https://www.npr.org/transcripts/993084954), [Rock and Roll Globe](https://rockandrollglobe.com/funk/the-odyssey-of-funkadelic-and-eddie-hazel/) |
| Tonality: E minor pentatonic over a slow jam; the mournful character comes from the slow tempo, the minor/Dorian-leaning bed, long bends held into feedback and the delay regenerating under the next phrase. | SECONDARY (Fender names E minor pentatonic; "Dorian" is the project's hearing, not a source) | [Fender](https://fender.com/articles/artists/one-track-mind-the-passion-of-eddie-hazel-and-funkadelics-maggot-brain) |
| Hip hop sampling of the title track (WhoSampled, page could not be fetched; entries from search snippets): The Coup "Hard Concrete"; MURS "Animal Style" (2011); Subject 13 "Slice of Soul" (1997); Travis Scott "HYAENA" (2023, catalogued as a vocal/lyric sample); Butch "LSD-25" (2012); Seeders "Crazy for You" (2007); L.F.T. "Visitors" (2019). Clinton let hip hop sample the P-Funk catalogue cheaply in the 1990s, which is why the catalogue, more than this track, is everywhere in rap. | SECONDARY | [WhoSampled album page](https://www.whosampled.com/album/Funkadelic/Maggot-Brain/), [WhoSampled MURS](https://www.whosampled.com/sample/120820/MURS-Animal-Style-Funkadelic-Maggot-Brain/), [WhoSampled Coup](https://www.whosampled.com/The-Coup/Hard-Concrete/), [Treble](https://www.treblezine.com/5-albums-that-wouldnt-exist-without-funkadelics-maggot-brain/) |

What a text-to-music model needs from this: a slow pulse; a single guitar through fuzz then
wah; long bends that sit on one note until it feeds back; a tape echo whose repeats regenerate
long enough to overlap the next phrase; a slow phase shift on the echo tail; the rest of the band
far back and dry; minor key; played by one person, one take, mistakes kept.

### 3.2 Marilyn Manson, *Antichrist Superstar* (1996) and *Mechanical Animals* (1998)

| Fact | Status | Source |
|---|---|---|
| *Antichrist Superstar*: produced by Manson, Trent Reznor, Dave "Rave" Ogilvie (Skinny Puppy's producer) and Sean Beavan; recorded Feb-Aug 1996 at Nothing Studios, New Orleans. Credits: Ogilvie programming/editing/mixing; Beavan producer/programming/engineer/digital editing; Chris Vrenna drums/programming/editing; Reznor editing/mixing/Mellotron. | VERIFIED (credits) | [Wikipedia](https://en.wikipedia.org/wiki/Antichrist_Superstar), [MansonWiki](https://manson.wiki/Antichrist_Superstar), [MansonWiki Beavan](https://manson.wiki/Sean_Beavan) |
| The Reznor/Nothing drum method of the period (documented for *The Downward Spiral*, same studio team): one kit recorded in ~25 rooms (live rooms, bathrooms, outdoors) with one or two distant mics for a "ringy, trashy" sound, then sampled at several velocities into Akai S1100s and played from a keyboard; drum loops from live drummers (Vrenna, Perkins) cut up and reassembled with synth and Pro Tools manipulation; the "Closer" kick is a heavily modified sample of an Iggy Pop kick through Roland R-70 beats and two S1100s with expanders. No source confirmed a distortion pedal or deliberate bit reduction on these drums, so treat "bitcrushed drums" as a sound-alike description, not a documented chain. | SECONDARY (Keyboard interview via MusicTech) | [MusicTech Landmark Productions](https://musictech.com/guides/essential-guide/nine-inch-nails-the-downward-spiral/), [NIN Hotline](https://www.theninhotline.com/archives/articles/display/548) |
| Ogilvie's Skinny Puppy background: tape splices, found-sound and film-dialogue samples, distortion and samplers as compositional tools; this is the lineage of the sample-mangling on ACS. | SECONDARY | [Wikipedia Ogilvie](https://en.wikipedia.org/wiki/Dave_Ogilvie), [Wikipedia "Testure"](https://en.wikipedia.org/wiki/Testure) |
| *Mechanical Animals*: produced by Michael Beinhorn with Sean Beavan and Manson (Dave Sardy produced the **next** record, *Holy Wood*, not this one; the task's attribution is a mix-up). Drummer Ginger Fish: a rented rehearsal room to pre-produce; "we rented every single snare drum in L.A.", three days choosing one; two four-piece kits, one of big drums in a large room with reflective brick walls; tom overdubs on top of a basic beat for a two-drummer feel. Beinhorn used an ARP 2600 on "The Dope Show" (Equipboard says for processing vocal parts, not bass) and a Telefunken ELA M 251 at Conway Studios; Mellotron and keyboard bass appear on "Coma White". | VERIFIED (Drum! interview, Wikipedia credits) / SECONDARY (Equipboard) | [MansonWiki Drum! 1998](https://manson.wiki/Interview:1998/11_Drum!), [Wikipedia](https://en.wikipedia.org/wiki/Mechanical_Animals), [Mix on Holy Wood/Sardy](https://www.mixonline.com/recording/blood-pavement-375242), [Produce Like A Pro "Dope Show"](https://producelikeapro.com/blog/marilyn-mansons-the-dope-show-inside-the-song-with-michael-beinhorn/), [Equipboard](https://equipboard.com/albums/marilyn-manson-mechanical-animals) |
| Specific per-track accounts of ring modulation, tape warps, gated noise or "skipping CD" edits on ACS were **not** found in any fetched snippet. Those words describe what the records sound like and are standard industrial-rock techniques of the Reznor/Ogilvie school, but they are not documented for these sessions here. | UNVERIFIED | Beavan's "Analog Abuse & Digital Depravity" 1998 interview is the best lead: [MansonWiki](https://manson.wiki/Interview:1998/07_Sean_Beavan_Interview_-_Analog_Abuse_%26_Digital_Depravity) |

What a text-to-music model needs from this: drums that are room-recorded, trashy and far-miked,
then resampled so each hit is a sample; machine and metal noises used as percussion; loops cut
into pieces and reassembled so they skip; guitars tuned down and distorted into a wall;
everything heavily compressed; a keyboard bass; a Mellotron-type tape-keyboard pad; the mix
loud, dense and dry.

### 3.3 Otis Redding and the Stax rhythm section

| Fact | Status | Source |
|---|---|---|
| Studio: a converted movie theatre at 926 E. McLemore, Memphis; the **sloped floor was kept** and deadened the room cheaply ("a signature part of the Stax sound"); musicians set up on the flat part near the old stage; control room where the screen had been. | VERIFIED (Stax Museum) | [Stax Museum 1957-1968](https://staxmuseum.org/1957-1968/), [KEF blog](https://us.kef.com/blogs/news/the-stax-sound-the-cornerstone-of-american-soul-music) |
| Recording: Jim Stewart bought an **Ampex 350 mono** console recorder (Wikipedia says 350; the Jim Stewart article says 300); the band tracked live in the room; on the *Soul Ballads* sessions Tom Dowd installed a two-track and a four-input Ampex mixer that put instruments on one channel and the voice plus echo on the other. "Hot to tape" is the project's description of the result, not a sourced quote. | VERIFIED (museum/Wikipedia for the recorder); mono-vs-two-track detail SECONDARY | [Wikipedia Stax Records](https://en.wikipedia.org/wiki/Stax_Records), [Wikipedia Jim Stewart](https://en.wikipedia.org/wiki/Jim_Stewart_(music_producer)), [Wikipedia Soul Ballads](https://en.wikipedia.org/wiki/The_Great_Otis_Redding_Sings_Soul_Ballads) |
| Al Jackson Jr.: "The Human Timekeeper"; described as rock-solid yet laid-back with "an unmistakable behind-the-beat approach" (examples: "In the Midnight Hour", "I Can't Turn You Loose", "Hold On, I'm Comin'"); kit tuned very low and deadened, a wallet on the snare; a Neumann KM 84 aimed at the snare; Ludwig and Rogers kits; Duck Dunn: "the pocket where Al put everything was the real secret of Stax". | SECONDARY (Reverb, Drummer Cafe, Sonicbids, Modern Drummer intro) | [Reverb Stax drums](https://reverb.com/news/get-the-drums-sounds-of-stax-records-reverbs-studio-sampled-sounds), [Drummer Cafe](https://drummercafe.com/featured/drummer-percussionist/al-jackson-jr), [Modern Drummer](https://www.moderndrummer.com/2010/03/al-jackson/), [Sonicbids](https://blog.sonicbids.com/5-musicians-who-made-their-mark-behind-the-scenes) |
| Duck Dunn: Fender Precision basses (mid-50s single-coil, then a 1958, then a 1959-60 rosewood board) strung with La Bella heavy flatwounds; studio amp an Ampeg B-15. | VERIFIED (Vintage Guitar interview) | [Vintage Guitar](https://www.vintageguitar.com/3021/donald-duck-dunn) |
| Steve Cropper: Telecaster, never the bass pickup, middle or bridge position for a trebly tone; "I've always thought of myself as a rhythm player ... I can play something over and over and over"; lock-step doubling of the bass line; short fills, few solos; writing split: he wrote a guitar line, Dunn a bass line, drums added after. (Cropper died 2025-12-03.) | VERIFIED (his own words via Premier Guitar / Guitar.com) | [Premier Guitar obituary](https://www.premierguitar.com/steve-cropper-obituary), [Hollywood Reporter](https://www.hollywoodreporter.com/music/music-news/steve-cropper-dead-booker-t-stax-records-guitarist-1236441839/) |
| Booker T. Jones: a **Hammond M3** (the spinet) on "Green Onions" and "Behave Yourself" by his own account, not the B-3 some sources claim. | VERIFIED (Jones interview) | [Wikipedia Green Onions](https://en.wikipedia.org/wiki/Green_Onions), [NPR Fresh Air](https://www.npr.org/transcripts/103709916) |
| Horns: Wayne Jackson (trumpet), Andrew Love (tenor), with Floyd Newman (baritone, Mar-Keys) and Joe Arnold (tenor) on Redding sessions. **No written charts**: Jackson said parts were worked out verbally with Redding, Cropper and Hayes; for "Fa-Fa-Fa-Fa-Fa (Sad Song)" Redding "pretty much orchestrated the band ... singing their horn parts for them". | VERIFIED (Jackson's words via Encyclopedia of Arkansas; Stax documentary via Substack) | [Encyclopedia of Arkansas](https://encyclopediaofarkansas.net/entries/wayne-jackson-8738/), [Wikipedia Floyd Newman](https://en.wikipedia.org/wiki/Floyd_Newman), [Something FM list](https://somethingfm.substack.com/p/the-25-greatest-songs-with-the-memphis) |
| The "got-ta, got-ta" stutter: Redding's trademark ad-lib ("gotta, gotta, gotta" in "Try a Little Tenderness"), partly inherited from Sam Cooke; on the Whisky a Go Go version Jackson sat out the first verse, came in with a tick-tock snare and then pushed the tempo, which drove Redding into the ad-libs. The horn-answer structure (voice phrase, horn stab reply) is audible on the records but no analysis text was found; it is the project's hearing. | SECONDARY / UNVERIFIED as analysis | [Here Comes the Song](https://www.herecomesthesong.com/post/2017/12/04/otis-redding-try-a-little-tenderness), [Last.fm bio](https://www.last.fm/music/Otis+Redding/+wiki) |

What a text-to-music model needs from this (instrumental): a live room with a dead floor; a
snare with a wallet on it, hit late and hard; a flatwound Precision bass through a small tube amp
playing the same two-bar figure; a bright single-coil guitar that doubles the bass line and chops
on the backbeat; a spinet-sized drawbar organ holding one chord; a three-horn stack (trumpet
over tenor and baritone) answering the lead in two-note stabs; a tambourine on two and four; the
whole band to one track, hot, dry, tight.

### 3.4 Piano and string "hit" structures in hip hop

| Fact | Status | Source |
|---|---|---|
| The **orchestra hit** (ORCH5, really ORCH2 in the Fairlight library): an eight-bit sample of the opening chord of Stravinsky's *Firebird*, popularised by "Planet Rock" (1982); *Musician* 1990: "the orchestral hit that was heard on every rap and techno-pop record of the early 1980s". Robert Fink, "The Story of ORCH5, or, the Classical Ghost in the Hip-Hop Machine", *Popular Music* 24(3) 339-356. This is the ancestor of every chopped string stab. | VERIFIED (Wikipedia, Fink citation) | [Wikipedia Orchestra hit](https://en.wikipedia.org/wiki/Orchestra_hit), [Perfect Circuit Fairlight history](https://www.perfectcircuit.com/signal/fairlight-cmi-history), [Synthtopia](https://www.synthtopia.com/?p=96931) |
| Staccato piano riff as hook ("Still D.R.E.", 1999, Scott Storch's riff, C minor): tutorials consistently say the bounce comes from **short, detached stabs**, played with the sustain pedal **off** because "it will make the notes bleed together and destroy the rhythmic integrity". | SECONDARY (tutorials and Boardroom; credit to Storch per Wikipedia) | [Melodics](https://melodics.com/blog/the-theory-behind-hip-hop-historys-still-d-r-e), [Boardroom](https://boardroom.tv/still-dre-business-dr-dre-snoop-dogg/), [Wikipedia Scott Storch](https://en.wikipedia.org/wiki/Scott_Storch) |
| Single repeated piano note as the whole intro ("Runaway", 2010): "begins with an isolated piano melody that repeats one note". | VERIFIED (Wikipedia) | [Wikipedia Runaway](https://en.wikipedia.org/wiki/Runaway_(Kanye_West_song)) |
| Chopped soul samples: the RZA method of chopping and pitch-shifting soul (strings, keys) to fit a beat, drums left unquantised for a loose feel, later copied by Kanye West and Just Blaze; DJ Premier's chopping and "drumwork" as the East Coast template. Producer forum: string quartets are favoured sample sources because the inner voices suggest bass lines. | SECONDARY | [Mixdown on RZA](https://mixdownmag.com.au/features/five-times-the-rza-showed-hes-a-sampling-genius/), [BeatTips on Premier](https://beattips.com/?p=741), [MPC Forums](https://www.mpc-forums.com/viewtopic.php?p=91209) |
| Trap-era orchestral/brass stabs (Lex Luger, 2010): his FL Studio method assigns orchestral instruments and tubular bells across drum pads so stabs are **played like drums**; sample packs ("Smoked Out Brass") were later sold to copy the sound. No per-track breakdown of the stabs was found. | SECONDARY | [Novation walkthrough](https://novationmusic.com/articles/how-to-make-a-beat-on-flkey-in-fl-studio-with-lex-luger), [Native Instruments](https://blog.native-instruments.com/watch-lex-luger-build-a-cinematic-trap-beat-with-maschine/) |
| The **tresillo (3+3+2)** cell: two dotted quarters then a quarter; "the most fundamental rhythmic pattern in Sub-Saharan African music as well as Latin American music"; hidden inside sixteenth-note strumming; present across EDM and hip hop (Splice); formal analysis in arXiv 2109.10256. 3+2 and 2+3 are its five-pulse cousins (the first half of son clave either way round). No source was found that explicitly ties 3+3+2 to trap piano stabs; the mapping below is the project's. | VERIFIED (definition); hip hop mapping is the project's | [Splice "5 most important rhythms"](https://splice.com/blog/5-most-important-rhythms), [arXiv Tresillo](https://arxiv.org/abs/2109.10256v1), [Fader on tresillo](https://www.thefader.com/2015/06/10/tresillo-club-music) |
| Hand-damping, sustain-pedal stamping and prepared piano in hip hop production: **no producer interview or analysis was found**. These remain physical descriptions the model can act on, but they are not documented hip hop practice here. | UNVERIFIED | |

Hit-structure grammar (the project's synthesis): a "hit" is a short, loud, damped event with a
fast attack and a hand-stopped release; it is placed on a cell (3+3+2 across one bar of
sixteenths; 3+2 or 2+3 across half a bar; an octave hit on the one with the cell answering);
one cell per bar, identical every bar, is a loop hook; the same cell with its last hit moved one
sixteenth late is the "repeat four, change one" device from section 1.

---

## 4. Vocabulary translation table

House style: what it is, how it was made, how it is processed, how a person plays it. Every
string below passed `lib/guard.js` `check()` with no lane ALLOW (verified 2026-11-01 by running
the table through the guard; see the note at the end of this section). Words the guard bans
that these descriptions had to avoid: phaser (say "phase shifter"), scratch, fill, roll, pickup,
crash, impact, stutter edit (bare "stutter" is fine), tape stop, swing, shuffle, hum, strings
(allowed in the dark-hybrid lane only), orchestra/orchestral, Hammond/Leslie, sax, cinematic,
house, electro, rave, ambient, lush, "no X". Lane notes flag the lane DENY lists: "tape",
"cassette" and "distorted 808" are denied in the bright-pop lane; "808", "bitcrush", "drum
machine", "quantized", "tape"-family words are denied in the live-pocket lane; "strings" only
in the dark-hybrid lane.

### 4.1 The fuzz-wah-tape-echo guitar (from 3.1)

| # | Descriptor (prompt-ready) | Lane fit |
|---|---|---|
| F1 | a single electric guitar through a fuzz pedal then a wah, low register, one long bend held until it feeds back\|the fuzz guitar | jid-dark, hybrid, phonk |
| F2 | tape echo on the guitar with the repeats regenerating so each phrase overlaps the last, the echo darker than the dry note | jid-dark, hybrid |
| F3 | a slow phase shifter on the echo tail only, the dry guitar left untouched | jid-dark, hybrid |
| F4 | the guitar bends up a whole step and sits on the note until the amp throws it back as feedback, then falls away | jid-dark, hybrid, phonk |
| F5 | played by one person in one take, the string noise and the missed note left in | all lanes (human clause) |
| F6 | the band far behind the guitar and dry, the kick and bass felt more than heard | jid-dark, hybrid (lead-out layouts) |
| F7 | minor key, the guitar leaning on the flat seventh and the fourth, slow pulse, mournful | jid-dark, hybrid, paak-pocket (key colour) |
| F8 | a wah rocked slowly across one held note so the tone opens and closes once per bar | jid-dark, hybrid, phonk |
| F9 | the fuzz set thick enough that two notes together turn to grit, so the line is single notes only | jid-dark, hybrid |

### 4.2 Industrial sample-mangling (from 3.2)

| # | Descriptor (prompt-ready) | Lane fit |
|---|---|---|
| I1 | drums recorded in a tiled room with one far microphone, then resampled so every hit is a sample, trashy and ringing\|the room-sampled drums | jid-dark, phonk, hybrid |
| I2 | a bitcrushed drum loop, the grain audible on the hats, the kick kept clean underneath | phonk, jid-dark, hybrid (never paak-pocket) |
| I3 | metal-on-metal hits and machine noise used as the percussion colour, each one tuned to the key | jid-dark, hybrid |
| I4 | a two-bar loop cut into pieces and reassembled out of order so it skips on beat 3 every bar, the skip identical every time | jid-dark, phonk, hybrid |
| I5 | a ring-modulated synth bass, the carrier tuned so the bass note stays clear and the overtones clang | jid-dark, hybrid |
| I6 | a detuned guitar wall, down a fourth, chugging eighths through heavy distortion, compressed flat | jid-dark, hybrid |
| I7 | gated noise bursts that open with the snare and shut with it, the length of a sixteenth | jid-dark, hybrid |
| I8 | tape-warped keys, the pitch sagging a quarter tone at the end of each chord | jid-dark, phonk, hybrid |
| I9 | a tape-keyboard pad of flutes-on-tape, one chord held, slightly out of tune with itself | jid-dark, hybrid |
| I10 | everything compressed hard, dense and dry, the mix loud with the snare on top | jid-dark, hybrid (mix tag) |
| I11 | a stuttered digital edit of one piano chord, the chord repeating in sixteenths for half a bar then stopping dead | jid-dark, phonk, hybrid |

### 4.3 The Memphis live pocket (from 3.3)

| # | Descriptor (prompt-ready) | Lane fit |
|---|---|---|
| M1 | a snare with a wallet on the head, tuned low and dead, hit a hair late and hard for it\|the wallet snare | paak-pocket, hybrid |
| M2 | a flatwound Precision bass through a small tube amp, the same two-bar figure start to finish, thumb-dull and round | paak-pocket, hybrid |
| M3 | a bright single-coil guitar doubling the bass line note for note, then chopping one muted chord on beats 2 and 4 | paak-pocket, hybrid |
| M4 | a spinet drawbar organ holding one chord under everything, a second chord only at the turn of the two bars | paak-pocket, hybrid |
| M5 | a horn stack of trumpet over tenor and baritone, two-note stabs that answer the lead a beat after it stops\|the horn stabs | paak-pocket, hybrid |
| M6 | a tambourine on two and four, slightly ahead of the snare, played by hand | paak-pocket, hybrid |
| M7 | the whole band in one room with a dead floor, to one track, hot and dry, the kick and bass as one sound | paak-pocket (mix tag) |
| M8 | the horns stab three times fast then hold once, got-ta-got-ta-GOT, the figure identical every pass | paak-pocket, hybrid |
| M9 | the drums sit out the first eight bars, then a tick-tock on the snare rim alone, then the full kit straight in on beat 1 | paak-pocket (slow-burn layouts; hard cut, never a riser) |
| M10 | the kick behind the bass by a hair so the pocket leans back, the hats dead straight | paak-pocket, hybrid |

### 4.4 Piano and string hit structures (from 3.4)

| # | Descriptor (prompt-ready) | Lane fit |
|---|---|---|
| P1 | a grand piano struck in short damped hits, the sustain pedal off, every note cut by the hand\|the damped piano hits | all lanes |
| P2 | piano hits on a 3+3+2 cell across one bar of sixteenths, the same cell every bar, the last hit a sixteenth late | all lanes (one "late" per roll) |
| P3 | an octave hit on the one, the cell answering in the gap, the right hand never above middle C | all lanes |
| P4 | one piano note repeated on every eighth for two bars, the same pitch, then one note a half step down | connor-price, jid-dark, hybrid |
| P5 | an eight-bit sampled chord hit, a whole band in one slam, pitched to the key and placed on beat 1 only\|the sampled hit | jid-dark, phonk, hybrid |
| P6 | string stabs chopped to sixteenths from a quartet recording, the inner notes left in so the bass moves with them | jid-dark only (strings ALLOW) |
| P7 | piano hits damped by the palm on the strings inside the lid, more thud than tone | jid-dark, hybrid, phonk |
| P8 | the sustain pedal stamped down on beat 4 and released on beat 1, the one chord blooming and cut | paak-pocket, hybrid |
| P9 | a prepared piano with felt on the hammers, two notes stabbed in 3+2 across the first half of the bar | jid-dark, hybrid |
| P10 | chord stabs played from drum pads like a drum part, each stab the length of a sixteenth, three per bar | connor-price, jid-dark, hybrid |
| P11 | the piano hit cell plays four bars identical, the fourth with its last hit moved a sixteenth late | all lanes (switch/straight layouts only; "every 4 bars" budget) |

Guard note: the table was checked with
`node -e "const g=require('./lib/guard'); ... g.check(cell)"` over every descriptor cell of
4.1-4.4 with `allow = null` and against the `EVERY` cap; zero hits after one fix (F4 first
said the amp "sings" the note back; "sings" is in the voice class). "strings" is not in the
universal regex (the guard bans the family's instrument names, not the word), but the grammar
reserves it for the dark-hybrid lane's ALLOW, so P6 is marked dark-hybrid only. Lane DENY lists
were not applied in that run; the lane-fit column is the hand-applied DENY (for example I2 and
I8 are kept out of paak-pocket because of "bitcrush" and "tape", and I1-I2 out of connor-price
because of "tape"-family and dark words).

---

## 5. Could not verify

- The exact statistics (effect sizes, tempo difference, contour feature names) in Jakubowski et
  al.; only the press summary was readable.
- Williamson & Müllensiefen 2012's specific feature list.
- Witek 2014's participant count (60 vs ~100 in different summaries).
- Any millisecond figures in Iyer 2002 (PDF host blocked).
- The 17.4 ms laid-back snare figure in Danielsen et al. 2015 was read only through Carter &
  von Appen, not the JASA paper.
- Which fuzz pedal was on the "Maggot Brain" take (Maestro FZ-1 vs Big Muff) and whether the
  phase shifter was used on it at all.
- Which element of "Maggot Brain" each hip hop track sampled (WhoSampled categories only).
- Any documented use of ring modulation, tape warping, gated noise or deliberate bit reduction
  on *Antichrist Superstar*; the sample-mangling lineage is documented, the specific tools are
  not. Dave Sardy produced *Holy Wood* (2000), not *Mechanical Animals*.
- Whether Stax tracked everything to a single mono track on the Redding sessions, or to the
  two-track Dowd installed; the museum confirms the mono Ampex and the live room.
- Any published analysis of Redding's "got-ta" stutter as a horn-answer device; it is the
  project's hearing.
- Any producer interview on hand-damping, sustain-pedal stamping or prepared piano in hip hop.
- The "hook in the first 7 seconds" rule: no study found; Lamere's 24% skip-by-5-seconds and
  Léveillé Gauvin's ~5-second intros are the real numbers.
- The Zeigarnik/incomplete-tune mechanism for earworms: proposed by Hyman et al., not supported
  by later tests.

## Sources (all accessed 2026-11-01)

Hook science
- APA press release, 2016-11-03: https://www.apa.org/news/press/releases/2016/11/earworms
- Goldsmiths news: https://gold.ac.uk/news/scientists-find-key-to-writing-catchy-pop-hits
- Durham repository (Jakubowski et al.): https://dro.dur.ac.uk/20274
- Goldsmiths eprint 19405: https://research.gold.ac.uk/19405
- Goldsmiths eprint 10192 (Williamson & Müllensiefen 2012): https://research.gold.ac.uk/id/eprint/10192
- Finkel & Müllensiefen 2010 SysMus: https://eprints-gro.gold.ac.uk/10204/1/SysMus10_FinkelMullensiefen_2010.pdf
- Live Science: https://livescience.com/56739-what-makes-an-earworm-so-catchy.html
- Time: https://time.com/4557229/earworm-music
- Vice (Jakubowski): https://www.vice.com/en/article/why-songs-get-stuck-in-your-head
- BPS Research Digest (Beaman & Williams): https://bps.org.uk/research-digest/natural-history-earworm-song-wont-get-out-your-head
- BPS "Z is for Zeigarnik": https://bps.org.uk/psychologist/z-zeigarnik-effect
- Reading, Arthur review: https://centaur.reading.ac.uk/79201/
- Emory Arts & Brain: https://scholarblogs.emory.edu/artsbrain/?p=1469
- Margulis 2013, Princeton listing: https://collaborate.princeton.edu/en/publications/aesthetic-responses-to-repetition-in-unfamiliar-music/
- U. Arkansas news on *On Repeat*: https://news.uark.edu/articles/22615/repetition-in-music-pulls-us-in-and-pulls-us-together
- MTO review of *On Repeat*: https://www.mtosmt.org/issues/mto.14.20.4/mto.14.20.4.albrecht.php
- Huron, *Sweet Anticipation* front matter: https://researchgate.net/profile/David-Huron-2/publication/209436188_Sweet_Anticipation_Music_and_the_Psychology_of_Expectation/links/0c96052e02be139f9f000000/Sweet-Anticipation-Music-and-the-Psychology-of-Expectation.pdf
- Leonardo review of Huron: https://leonardo.info/reviews_archive/july2007/sweet_zilberg.html
- Salimpoor et al. 2011 PDF: https://www.zlab.mcgill.ca/publications/docs/salimpoor_2011_nn.pdf
- BRAMS summary: https://brams.org/2011/01/09/salimpoor/
- Medical News Today: https://www.medicalnewstoday.com/articles/213412
- Burgoyne et al. 2013 ISMIR: https://archives.ismir.net/ismir2013/paper/000101.pdf
- UvA abstract: https://www.mcg.uva.nl/mcg-2023/abstracts/Burgoyne-et-al-2013.html
- Vice (Hooked on Music): https://www.vice.com/en/article/we-spoke-to-the-scientist-who-knows-which-songs-stick-in-your-head/
- Lamere, Music Machinery analytics: https://musicmachinery.com/category/analytics/
- OSU news (Léveillé Gauvin): https://news.osu.edu/has-music-streaming-killed-the-instrumental-intro/
- Hypebot: https://www.hypebot.com/hypebot/2017/04/did-streaming-kill-the-instrumental-intro.html
- Sonicfield abstract (Léveillé Gauvin): https://sonicfield.org/library/drawing-listener-attention-in-popular-music-testing-five-musical-features-arisin
- arXiv 1903.06008 (skipping and structure): https://arxiv.org/pdf/1903.06008

Groove science
- Witek et al. 2014, Birmingham record: https://research.birmingham.ac.uk/en/publications/syncopation-body-movement-and-pleasure-in-groove-music/
- Witek 2015 correction, PMC: https://pmc.ncbi.nlm.nih.gov/articles/PMC4581707
- Aarhus CFIN news: https://cfin.au.dk/news-events/news/show/artikel/new-article-in-plos-one-syncopated-rhythm-may-influence-our-desire-to-dance-to-music
- PsyPost: https://www.psypost.org/in-funk-music-rhythmic-complexity-influences-dancing-desire/
- Sioros et al. 2014 Frontiers: https://public-pages-files-2025.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2014.01036/text
- Matthews et al. 2019 PMC: https://pmc.ncbi.nlm.nih.gov/articles/PMC6328141
- Janata et al. 2012 Zenodo: https://zenodo.org/record/997607
- Stupacher/Hove/Janata SysMus14: https://journals.gold.ac.uk/index.php/sysmus14/article/view/256
- Etani et al. 2018: https://researchers.westernsydney.edu.au/en/publications/optimal-tempo-for-groove-its-relation-to-directions-of-body-movem/
- Iyer 2002 publication page: https://vijayiyer.scholars.harvard.edu/publications/embodied-mind-situated-cognition-and-expressive-microtiming-african-american
- Butterfield 2006 MTO: https://mtosmt.org/issues/mto.06.12.4/mto.06.12.4.butterfield.php
- Benadon 2007 commentary: https://mtosmt.org/issues/mto.07.13.1/mto.07.13.1.benadon.php
- Butterfield 2007 reply: https://mtosmt.org/issues/mto.07.13.3/mto.07.13.3.butterfield.php
- Butterfield 2010 summary: https://labs.sonicfield.org/library/participatory-discrepancies-and-the-perception-of-beats-in-jazz
- Danielsen et al. 2015 JASA, Birmingham record: https://research.birmingham.ac.uk/en/publications/effects-of-instructed-timing-and-tempo-on-snare-drum-sound-in-dru/
- Danielsen 2015 UiO record: https://duo.uio.no/handle/10852/55787?show=full
- Carter & von Appen (quotes the 17.4 ms): https://tnp.mtsnys.org/vol49-50/carter_von_appen
- Câmara et al. 2020: https://duo.uio.no/handle/10852/79965
- Davies et al. 2013: https://repositorio.inesctec.pt/handle/123456789/3901?mode=full
- Frühauf et al. 2013 summary: https://labs.sonicfield.org/library/music-on-the-timing-grid-the-influence-of-microtiming-on-the-perceived-groove-qu
- Senn et al. 2016 PMC: https://pmc.ncbi.nlm.nih.gov/articles/PMC5050221
- Kilchenmann & Senn 2015: https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2015.01232/full
- Senn et al. 2018 PLoS ONE: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC6025871/
- Hein, "Dilla Time": https://ethanhein.substack.com/p/dilla-time
- Hein, "Get Dis Money": https://ethanhein.com/wp/2022/get-dis-money/
- Hein, "Dilla Time in Chameleon": https://ethanhein.com/wp/2022/dilla-time-in-chameleon/
- Okayplayer, Charnas interview: https://www.okayplayer.com/culture/j-dilla-time-book-dan-charnas-interview.html
- Ethnomusicology Review of *Dilla Time*: https://ethnomusicologyreview.ucla.edu/node/1106
- Danielsen (ed.) 2010 TOC: https://catalogo.abie.es/Record/unav.b33426533/TOC
- Danielsen 2024 "beat bins" summary: https://labs.sonicfield.org/library/theres-more-to-timing-than-time

Production references
- Fender on "Maggot Brain": https://fender.com/articles/artists/one-track-mind-the-passion-of-eddie-hazel-and-funkadelics-maggot-brain
- NPR, Maggot Brain at 50: https://www.npr.org/2021/07/22/993084954/making-funkadelic-maggot-brain
- Rock and Roll Globe: https://rockandrollglobe.com/funk/the-odyssey-of-funkadelic-and-eddie-hazel/
- guitar.com review: https://guitar.com/review/album/the-genius-of-maggot-brain-by-funkadelic/
- Equipboard, Hazel: https://equipboard.com/pros/eddie-hazel
- WhoSampled album page: https://www.whosampled.com/album/Funkadelic/Maggot-Brain/
- WhoSampled MURS: https://www.whosampled.com/sample/120820/MURS-Animal-Style-Funkadelic-Maggot-Brain/
- WhoSampled The Coup: https://www.whosampled.com/The-Coup/Hard-Concrete/
- Treble on Maggot Brain's influence: https://www.treblezine.com/5-albums-that-wouldnt-exist-without-funkadelics-maggot-brain/
- Wikipedia, Antichrist Superstar: https://en.wikipedia.org/wiki/Antichrist_Superstar
- MansonWiki, Antichrist Superstar: https://manson.wiki/Antichrist_Superstar
- MansonWiki, Sean Beavan 1998 interview: https://manson.wiki/Interview:1998/07_Sean_Beavan_Interview_-_Analog_Abuse_%26_Digital_Depravity
- MansonWiki, Drum! 1998 (Ginger Fish): https://manson.wiki/Interview:1998/11_Drum!
- MusicTech, Downward Spiral production: https://musictech.com/guides/essential-guide/nine-inch-nails-the-downward-spiral/
- NIN Hotline archive: https://www.theninhotline.com/archives/articles/display/548
- Wikipedia, Dave Ogilvie: https://en.wikipedia.org/wiki/Dave_Ogilvie
- Wikipedia, Mechanical Animals: https://en.wikipedia.org/wiki/Mechanical_Animals
- Mix, Holy Wood with Dave Sardy: https://www.mixonline.com/recording/blood-pavement-375242
- Produce Like A Pro, "The Dope Show": https://producelikeapro.com/blog/marilyn-mansons-the-dope-show-inside-the-song-with-michael-beinhorn/
- Equipboard, Mechanical Animals: https://equipboard.com/albums/marilyn-manson-mechanical-animals
- Stax Museum history: https://staxmuseum.org/1957-1968/
- KEF, the Stax sound: https://us.kef.com/blogs/news/the-stax-sound-the-cornerstone-of-american-soul-music
- Wikipedia, Stax Records: https://en.wikipedia.org/wiki/Stax_Records
- Wikipedia, Jim Stewart: https://en.wikipedia.org/wiki/Jim_Stewart_(music_producer)
- Wikipedia, Soul Ballads: https://en.wikipedia.org/wiki/The_Great_Otis_Redding_Sings_Soul_Ballads
- Reverb, Stax drum sounds: https://reverb.com/news/get-the-drums-sounds-of-stax-records-reverbs-studio-sampled-sounds
- Drummer Cafe, Al Jackson Jr.: https://drummercafe.com/featured/drummer-percussionist/al-jackson-jr
- Modern Drummer, Al Jackson Jr.: https://www.moderndrummer.com/2010/03/al-jackson/
- Sonicbids on Al Jackson Jr.: https://blog.sonicbids.com/5-musicians-who-made-their-mark-behind-the-scenes
- Vintage Guitar, Duck Dunn: https://www.vintageguitar.com/3021/donald-duck-dunn
- Premier Guitar, Cropper obituary: https://www.premierguitar.com/steve-cropper-obituary
- Hollywood Reporter, Cropper: https://www.hollywoodreporter.com/music/music-news/steve-cropper-dead-booker-t-stax-records-guitarist-1236441839/
- Wikipedia, Green Onions: https://en.wikipedia.org/wiki/Green_Onions
- NPR Fresh Air, Booker T. Jones: https://www.npr.org/transcripts/103709916
- Encyclopedia of Arkansas, Wayne Jackson: https://encyclopediaofarkansas.net/entries/wayne-jackson-8738/
- Wikipedia, Floyd Newman: https://en.wikipedia.org/wiki/Floyd_Newman
- Something FM, Memphis Horns list: https://somethingfm.substack.com/p/the-25-greatest-songs-with-the-memphis
- Here Comes the Song, "Try a Little Tenderness": https://www.herecomesthesong.com/post/2017/12/04/otis-redding-try-a-little-tenderness
- Last.fm, Otis Redding bio: https://www.last.fm/music/Otis+Redding/+wiki
- Wikipedia, Orchestra hit: https://en.wikipedia.org/wiki/Orchestra_hit
- Perfect Circuit, Fairlight history: https://www.perfectcircuit.com/signal/fairlight-cmi-history
- Synthtopia, history of the orchestral hit: https://www.synthtopia.com/?p=96931
- Melodics, "Still D.R.E." theory: https://melodics.com/blog/the-theory-behind-hip-hop-historys-still-d-r-e
- Boardroom, "Still D.R.E.": https://boardroom.tv/still-dre-business-dr-dre-snoop-dogg/
- Wikipedia, Scott Storch: https://en.wikipedia.org/wiki/Scott_Storch
- Wikipedia, "Runaway": https://en.wikipedia.org/wiki/Runaway_(Kanye_West_song)
- Mixdown, RZA sampling: https://mixdownmag.com.au/features/five-times-the-rza-showed-hes-a-sampling-genius/
- BeatTips, DJ Premier: https://beattips.com/?p=741
- MPC Forums on sampling string quartets: https://www.mpc-forums.com/viewtopic.php?p=91209
- Novation, Lex Luger FLkey: https://novationmusic.com/articles/how-to-make-a-beat-on-flkey-in-fl-studio-with-lex-luger
- Native Instruments, Lex Luger Maschine: https://blog.native-instruments.com/watch-lex-luger-build-a-cinematic-trap-beat-with-maschine/
- Splice, five most important rhythms: https://splice.com/blog/5-most-important-rhythms
- arXiv 2109.10256, Tresillo: https://arxiv.org/abs/2109.10256v1
- The Fader on tresillo: https://www.thefader.com/2015/06/10/tresillo-club-music
