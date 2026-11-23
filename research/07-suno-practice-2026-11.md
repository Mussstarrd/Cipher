# Suno practice, November 2026: what actually steers a hip hop instrumental

Research memo for **suno-forge**. Compiled 2026-11-23. All sources accessed 2026-11-23.
Scope: the nine questions in the brief (Styles reading, variety, instrumentals, transitions,
human drums, mid-song events, Lyrics tags, sliders, lane drift), text-only steering, current
models only.

## 0. How to read this, and what the evidence is worth

**Evidence grades used on every claim**

| Grade | Meaning here |
|---|---|
| **CONFIRMED** | Stated in official Suno material (help.suno.com, suno.com/blog, suno.com/release-notes). See the access caveat below: in this session official pages could only be read through search-engine summaries, so every CONFIRMED item is "confirmed via a summary of the official page", with the URL given for a one-click check. |
| **COMMUNITY (n)** | Blog, GitHub guide, newsletter, vendor write-up. `n` is the number of sources I count as independent. Many Suno blogs copy each other; where two sources trace to one test I count one. |
| **HANDOFF** | Rule from `research/handoff/` (the previous generator's team, "a few hundred rendered tracks"). Not public, not reproducible by me, but it is the only render-tested hip hop instrumental evidence in this whole memo. |
| **MY JUDGMENT** | My inference as a producer from the above. Untested. |

**Access caveat (same as research/01 and /02).** Direct fetches of suno.com, help.suno.com,
reddit.com, medium.com, hookgenius.app, jackrighteous.com, undetectr.com, beehiiv, pastebin
and every other blog host failed (not on this session's network allow-list). Only
`raw.githubusercontent.com` was readable in full. Everything else is a search-engine summary
of the page. I read no Reddit thread, no Discord log and no YouTube video directly. Where a
finding rests on a summary I say "as summarized".

**The honest headline.** For five of the nine questions (transitions, human drum timing,
mid-song events, the vocal one-shot, lane drift) there is **no public controlled test at
all**. The public literature is prompt-template blogs that call themselves "tested" and
show no method. The strongest evidence for those five is the project's own handoff plus the
owner's ear. What the public sources do establish, with numbers, is narrower: Variety
rewrites the Style text; negatives in Style do not work; take-to-take variance is large;
key and exact BPM are weak; Exclude is a bias that works best short.

---

## 1. Current models (verified first, as asked)

| Claim | Grade | Source |
|---|---|---|
| The generatable models are **v6, v6-wild, v6-mini**, released 2026-09-09; v6 and v6-wild are Pro/Premier, v6-mini is on every plan; all earlier models retired. | CONFIRMED (summary of official pages) + press (5+) | https://help.suno.com/en/articles/13924737 ; https://suno.com/release-notes/introducing-v6 ; https://suno.com/blog/introducing-v6 ; https://axios.com/2026/09/09/suno-v6-ai-music-warner-bmg ; https://www.musicradar.com/music-tech/suno-has-rebuilt-its-ai-music-models-from-scratch-with-licensed-music ; https://musicbusinessworldwide.com/suno-v6-ai-music-models-launch-in-partnership-with-wmg-bmg-and-believe |
| **No newer music model** exists as of 2026-11-23. The release-notes index shows a 2026-11-17 entry (beat-1 detection, clip timestretch) and still describes the v6 family as current. | CONFIRMED (summary of https://suno.com/release-notes) + COMMUNITY (2: bitwize changelog entries of 2026-11-14 and 2026-11-20 name only v6 and v6-mini; scriptbyai timeline) | https://suno.com/release-notes ; https://raw.githubusercontent.com/bitwize-music-studio/claude-ai-music-skills/main/CHANGELOG.md ; https://www.scriptbyai.com/suno-ai-timeline/ |
| Features shipped since research/01 (2026-10-31): Speech beta (2026-10-01), **MIDI input on the Create page (2026-11-04)**, Suno Bridge for Ableton (2026-11-13, Premier), Visuals (2026-11-17), a Suno MCP server. None changes the Style / Lyrics / Exclude fields or their limits. | CONFIRMED for MIDI (summary of official help + blog); COMMUNITY (1 timeline) for the dates of the rest | https://help.suno.com/en/articles/14594177 ; https://suno.com/blog/MIDI-in-create ; https://www.scriptbyai.com/suno-ai-timeline/ |
| MIDI in Create: desktop web, Pro/Premier, **Simple mode only, v6-wild only** at launch; a prompt describes genre, instrumentation, mood; "Suno takes no lyrics with a score". | CONFIRMED (summary) + COMMUNITY (2) | https://help.suno.com/en/articles/14594177 ; https://www.aimusicpreneur.com/ai-tools-news/suno-midi-upload-create-v6-wild-pro-premier/ ; bitwize CHANGELOG 0.104.0 (2026-11-14) |
| Field limits unchanged: Styles 1,000, Lyrics 5,000, Exclude Styles 1,000, "read off the live v6 composer". | COMMUNITY (3: HookGenius/MeriaApp guide of 2026-11-01, Replayed Studio, bitwize) | https://raw.githubusercontent.com/MeriaApp/suno-prompt-guide/main/README.md ; https://replayedstudio.com/blog/suno-v6-prompts-not-working/ ; https://raw.githubusercontent.com/bitwize-music-studio/claude-ai-music-skills/main/reference/suno/best-practices.md |

`lib/spec.json` is correct on models and limits. Nothing to change there.

A note on MIDI, because it bears on the owner's "unquantized drums" complaint: a MIDI file is
notes and timing, not audio, and it is the only input Suno has that can carry an exact
off-grid pattern. It is still an upload, it cannot be combined with Advanced mode, a Lyrics
field or (at launch) v6, so it does not fit the suno-forge workflow today and project rule 6
as written forbids it. Flagged for the owner, not recommended. MY JUDGMENT.

---

## 2. How the Styles field is actually read

### 2.1 What is established

| # | Claim | Grade | Source |
|---|---|---|---|
| 2.1 | At any Variety setting above Off, Suno **rewrites the Style text** before generation; at Off it kept the text word for word on **52 of 54 takes** (test dated 2026-10-22). Suno's FAQ: reduce Variety to 0 "to retain full control of your style tags". | CONFIRMED (FAQ, summary) + COMMUNITY (1 measured, widely repeated) | https://help.suno.com/en/articles/13924481 ; https://hookgenius.app/learn/suno-v6-guide/ ; bitwize creative-sliders.md |
| 2.2 | **Every word in Style is additive.** "no reverb" / "no autotune" in Style "is not reliably honoured and sometimes renders the very thing", measured over 585 generations (2026-08-25, v5.5 era; no percentage published). v6: "no drums" and "monotone vocals" in the prompt were ignored in a launch-week press test; "no male vocal" read as a request for one. | COMMUNITY (4: HookGenius/MeriaApp, bitwize citing The Verge, Replayed Studio, a dev.to tool builder) | MeriaApp README ; bitwize best-practices.md ; https://replayedstudio.com/blog/suno-v6-prompts-not-working/ ; https://dev.to/build_master_5fc54ebe3f3d/what-i-learned-building-an-audio-to-suno-v6-prompt-tool-language-regional-genres-and-provider-1o6k |
| 2.3 | **Earlier text carries more weight**; the first genre has the most influence. | COMMUNITY (5+, no measurement, no official statement) + HANDOFF ("the first 200 to 260 characters decide the genre, tempo, key and feel") | bitwize 100-expert-tips.md tip 11 ; mttkllr field guide ; https://roo.beehiiv.com/p/suno-v5-and-v5-5-prompts-why-it-ignores-half-your-words-and-how-to-fix-it ; research/handoff/handoff-generator-team.md s.2 |
| 2.4 | **Genre and mood dominate; BPM and key are weak.** One ranking puts dominant mood at about 25% of the output's character, genre about 20%, BPM about 3%; another says genre is "the heaviest signal in a Suno prompt". The percentages have no published method. | COMMUNITY (2, they disagree on whether mood or genre is first) | https://freesongwritingtools.com/blog/suno-prompt-components-ranked ; https://brahmstorm.com/blog/suno-chord-progression-prompts-what-works-what-gets-ignored |
| 2.5 | **Long physical prose late in the field is read loosely; bar-level instructions in Style are ignored.** Rhythm notation and chord symbols are "not something Suno was trained to interpret". | HANDOFF + COMMUNITY (2) | handoff s.2 ; https://strudelhub.com/blog/why-suno-ignores-chord-prompts ; brahmstorm (above) |
| 2.6 | "Suno collapses similar descriptions into the same sound": two rolls that both say "a plucked synth lead" come back identical whatever else differs. | HANDOFF | handoff s.2 |
| 2.7 | Specific concrete nouns steer; vague adjectives do not. A 200-prompt reverse-engineering piece lists the words that moved output across seeds: a specific instrument noun, a named reverb type (plate, room, hall, spring), "tape saturation", "close-miked", "behind the beat", "mono", and "BPM" plus a number. | COMMUNITY (1 with a stated method, consistent with bitwize tips 16-17 "era anchors beat adjectives", "name the instruments you want to hear") | https://james-palm.medium.com/reverse-engineered-200-suno-prompts-12-words-that-work-d8f06c0da727 ; bitwize 100-expert-tips.md |
| 2.8 | Name **2 to 4 instruments**; past about four Suno drops or blends them. | COMMUNITY (2: bitwize template "2-4 key instruments"; one v6 guide, as summarized, aimusicdaily or suno.bi) | bitwize best-practices.md ; https://aimusicdaily.com/suno-v6-prompts |

### 2.2 What is contested: length, and tags versus prose

The sources do not agree, and I could not find a controlled length test on v6.

| Position | Who | 
|---|---|
| 5 to 8 tags is the sweet spot; fewer than 4 gives "default Suno" | HookGenius tag research (vendor, pre-v6 pages) https://hookgenius.app/learn/suno-style-tag-research/ |
| About 10 descriptors; "the 4-7 rule is not a Suno rule"; synonym piles dilute | bitwize best-practices.md |
| Up to 14 tags are used, influence drops from the 15th (author's own measurement) | https://note.com/chachi_iezaki/n/ncf49f4895d85 |
| 12 to 20 dense tags beat long vague ones | dev.to tool builder (above) |
| 1 to 2 sentences; twenty descriptors make it muddier | https://aimusicdaily.com/suno-v6-prompts |
| v6 follows the Style field "more literally"; a longer brief saying what each instrument does beats a short list | https://undetectr.com/blog/suno-v6-prompts ; https://roo.beehiiv.com/p/best-suno-prompts-v6 ("full sentences, not short tag lists") |
| A working vendor's own generated Style prompts: **median 595 of 1,000 characters** | MeriaApp README (2026-11-01) |

Reading them together (MY JUDGMENT): the disagreement is mostly about format, not about
information. Everyone who gives a number lands between roughly 8 and 20 distinct decisions,
and nobody with data recommends filling the field. "Each comma-separated part pins down one
decision the model would otherwise randomize" (https://roo.beehiiv.com/p/ai-song-prompts-suno-v6,
COMMUNITY 1) is the most useful way to state it. A 990-character field with four
sub-clauses per sound is about 35 to 45 decisions, which is past every published ceiling,
and the handoff's own finding 2.5 says the tail of it is read loosely.

### 2.3 Does long physical description help or get averaged away?

- It helps **up to the noun**. "A grand piano recorded close, the hammer thud louder than
  the note" reliably gets you a close, thuddy piano (2.7: instrument noun plus "close-miked"
  are both on the short list of words that work). HANDOFF + COMMUNITY (1).
- It is **averaged away past the second or third modifier**, and anything bar-level
  ("on the and of 2", "four times, on the fourth pass one note lower") is ignored (2.5).
  HANDOFF + COMMUNITY (2).
- A description **with no source noun** falls back to the model's most common instrument
  for that register. MY JUDGMENT, and the likeliest cause of "the same three stock
  instruments": when the model cannot resolve "a long scraped string-like tone, grainy, the
  pitch leaning sharp", the prior for hip hop is piano, bass, guitar. Rule 9's `tone` family
  is the most exposed part of the grammar to this. A rare but real noun (a named instrument
  or object) is a stronger steer than a careful description of a timbre. This is untested
  and is the first thing I would A/B (section 11, rule 3).

---

## 3. Variety between generations

| # | Claim | Grade | Source |
|---|---|---|---|
| 3.1 | **Take-to-take variance is larger than a small prompt edit.** "The gap between the two takes of one generation exceeded the effect of a deliberate prompt change." Consequence: never judge a wording change on one take; generate 4 to 6. | COMMUNITY (2: HookGenius v6 test; bitwize tip 2 "generate in batches of 4-6, never 1") | https://hookgenius.app/learn/suno-v6-guide/ ; bitwize 100-expert-tips.md |
| 3.2 | What moves the output, strongest first: genre/subgenre tag, dominant mood, era and region words, the 2-4 instrument nouns, production words attached to an element, then tempo. | COMMUNITY (3, order partly disputed, see 2.4) | freesongwritingtools ; brahmstorm ; bitwize tip 16 ; a hip hop prompt guide, as summarized: "skip the era and region, and Suno defaults to a generic modern radio sound no matter how specific your instrument list is" (https://sunoaipromptguide.org/rap-prompts/ or https://undetectr.com/blog/suno-prompts-for-hip-hop ; the summary did not say which) |
| 3.3 | **A written key is close to ignored in hip hop.** "For modern pop, EDM, hip-hop, key center prompts have almost no effect on the actual output key." An informal DAW check found key "even less reliable" than BPM. | COMMUNITY (2) | brahmstorm ; https://heho.ai/blog/does-suno-follow-bpm |
| 3.4 | **Mode names work as mood words, not as scales.** "Dorian mode" does not raise the sixth, it "triggers a cluster of sonic memories" and often yields natural minor; Lydian is handled less reliably than Dorian or Phrygian; a full scale name with a strongly coloured genre ("D phrygian dominant" in a Turkish trap prompt) is reported to help. | COMMUNITY (2, one author each) | brahmstorm ; https://www.genprompt.site/learn/turkish-trap-suno-prompts |
| 3.5 | **BPM is a soft target.** "A bare number is a weak signal it is free to round off, average out, or ignore"; one tester found most DAW-measured tempos "not close"; a v6 post reports 74 BPM written, 130 shown in the editor (double-time reading or ignored, not separated); bitwize's take checker treats double and half readings as a match; v6 on Duration Auto "leans slow, sparse and long" when tempo and density are left open. The genre's own tempo convention pulls harder than the number. | COMMUNITY (5, direction agreed; no adherence rate exists) | https://sunomarket.com/guides/control-bpm-tempo ; heho.ai ; https://replayedstudio.com/blog/suno-v6-vocals-flat-rushed/ ; bitwize CHANGELOG 0.104.0 ; bitwize models.md ; freesongwritingtools |
| 3.6 | Era words are a strong, reliable steer ("era anchors beat adjectives"). | COMMUNITY (3) | bitwize tip 16 ; research/02 s.6 ; undetectr hip hop guide |
| 3.7 | Mix words steer when attached to an element or a concrete technique (mono, close-miked, tape saturation, a named reverb type); bare compliments ("radio-ready", "high quality") change nothing. | COMMUNITY (2) | james-palm (2.7) ; research/02 s.6 |
| 3.8 | Higher Weirdness raises variance between takes. | COMMUNITY (1) | bitwize creative-sliders.md |
| 3.9 | The Variety slider is Suno's designed diversity control, but it works by rewriting the prompt, so it trades control for variety. | CONFIRMED (FAQ, summary) | help 13924481 |

**What this means for "every take sounds alike" (MY JUDGMENT).** The generator's diversity
currently lives mostly where Suno is not listening: the tonic (ignored, 3.3), the exact
BPM inside a lane's band (weak, 3.5), beat positions of the kick and snare (ignored, 2.5),
and third-order modifiers of each sound (averaged, 2.3). Meanwhile the parts Suno weighs
most (the first genre tag, the mood words, the era, the two or three source nouns) are
set by the lane and so are nearly constant within a lane. Two rolls can have different
fingerprints in `engine.js` and the same first 120 characters as far as the model can tell.
Same energy follows from the same mood words; same cadence follows from the genre tag's
default drum pattern, because the written cadence is not read.

---

## 4. Instrumentals, and one sampled vocal one-shot

### 4.1 True instrumental

The stack in project rule 4 is the community consensus and nothing found contradicts it:
Instrumental toggle on, Variety Off, "instrumental" in the Style head, no vocal-implying
words in Style, vocal nouns in Exclude, bracket tags only in Lyrics.
COMMUNITY (5+; research/01 s.6 lists them) ; MeriaApp README (2026-11-01) repeats it for v6.

New or sharpened since research/01:

| # | Claim | Grade | Source |
|---|---|---|---|
| 4.1 | "Suno sings whatever is written under a tag", so an instrumental section is a tag with nothing under it. (Bitwize shipped this as a fix on 2026-11-20.) | COMMUNITY (2) | bitwize CHANGELOG 0.105.1 ; bitwize structure-tags.md ; https://www.mysoniq.com/blog/suno-instrumental-break |
| 4.2 | Long sentences in brackets are the usual cause of a tag being sung or ignored; advice is 1 to 3 words, or one label plus a short cue. | COMMUNITY (3, pre-v6 and v6 mixed) | https://freesongwritingtools.com/blog/suno-metatags-guide/ ; https://vocuno.com/blog/suno-ai-prompt ; bitwize SKILL.md ("at most 3 bracketed descriptors per section") |
| 4.3 | **Whether Lyrics tags are read at all with the Instrumental toggle on is disputed in public.** One 2026 tag guide (as summarized): "The Instrumental switch ignores the lyrics and the voice words entirely." Bitwize's hands-on v6 workflow and the handoff's renders say structure tags are followed. | CONTRADICTORY: COMMUNITY (1) against COMMUNITY (1 hands-on) + HANDOFF | https://sunoai-music.ai/blog/suno-tags or https://hookgenius.app/learn/suno-lyrics-formatting/ (the summary did not say which; not opened) ; bitwize SKILL.md ; handoff s.2 |
| 4.4 | Asking to "remove the humming" produced more humming; the fix is to say what the opening is and put short category nouns in Exclude. | COMMUNITY (1, already in research/01) | https://undetectr.com/blog/suno-v6-intro-outro-prompts |

4.3 matters more than it looks. If the toggle really discards the Lyrics field on the
current UI, every layout in `lib/layouts.js` is inert and all structure the owner hears is
the model's default. The handoff's evidence says otherwise and I trust rendered takes over
a blog, but it costs two generations to settle: same Style, toggle on, once with the layout
and once with an empty Lyrics field. If the two are indistinguishable in structure across
4 takes each, the layouts are not being read. I could not verify this either way.

### 4.2 Allowing one vocal one-shot without sung lines

Evidence is thin: no source addresses a single shout or hum on an otherwise instrumental
track. What exists:

| # | Claim | Grade | Source |
|---|---|---|---|
| 4.5 | A voice can sit in an instrumental if it is framed as a sample or texture, not a performer: "chopped vocal samples as texture" with lead vocals excluded. | COMMUNITY (2, no test shown) | https://lyromusic.com/prompts/instrumental ; https://www.rareconnections.io/suno-prompts ("Wordless female vocal chops" in an instrumental template, as summarized) |
| 4.6 | Excluding "vocal chops" / "voice samples" removes them, so the Exclude list must leave out exactly the one thing wanted. | COMMUNITY (2) | https://hookgenius.app/learn/suno-negative-prompting/ ; rareconnections (above) |
| 4.7 | Words that summon group or sung voices even on instrumentals: chant, choir, crowd, gospel, anthem, soulful. | HANDOFF + COMMUNITY (2) | handoff s.2 ; research/01 s.6 |
| 4.8 | A YouTube test of nine v6 vocal-effect prompts (chop, stutter, slowed, screwed) exists; I could not watch it. | not assessed | https://www.youtube.com/watch?v=movgr65fyTU |

MY JUDGMENT on wording. The model distinguishes "a singer is present" from "a record with
a voice on it was sampled". Keep every word on the sampler side: name the object (a sampled
shout, one syllable), the machine (pitched down, on a pad), the count as a state (the same
one-shot each time), and the place in the mix (dry, behind the snare). Keep every word off
the performer side: no "vocalist", "sings", "hums a melody", "ad-libs" (plural implies a
performer reacting). A wordless hum is the riskier of the two families because "humming"
is the exact failure mode the Exclude list exists for; expect a lower hit rate than the
shout. The project's 2026-11-23 exception in rule 4 (Exclude starts `lead vocals, rapping,
singing, lyrics, spoken words` and omits the one needed word) is the right shape and
matches 4.5 and 4.6.

---

## 5. Suppressing transitions, fills, risers, reversed lead-ins and scratches

**There is no public test of this.** I searched for it six ways. What the public sources
contain is the opposite craft: how to get bigger fills and drops. That literature is still
useful, read backwards, because it shows which words produce the things the owner hates.

| # | Claim | Grade | Source |
|---|---|---|---|
| 5.1 | Naming a build, drop, riser or fill in Style or in a section tag produces one. EDM guides instruct exactly that ("add sweeping riser and filter sweep before the drop"; "a snare roll and riser into each drop"; "tom fills into each section"). | COMMUNITY (4) | https://undetectr.com/blog/suno-prompts-for-edm ; MeriaApp kits ; https://hookgenius.app/suno-prompts/ ; https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/suno-ai-build-intensity-prompt |
| 5.2 | A drop-to-silence cue "tends to generate one to two bars of near-silence **before a rebuild**". The model stages a re-entry around any gap it is told about. | COMMUNITY (1, author's repeated tests) | https://brahmstorm.com/blog/suno-silence-prompts-how-to-make-space-work-in-your-tracks/ |
| 5.3 | Suno "builds a big drop only when the section before it pulls back": contrast between adjacent sections is what triggers the transition machinery. | COMMUNITY (2) | undetectr EDM guide ; jackrighteous build-intensity guide |
| 5.4 | Negatives in Style do not suppress and can plant (2.2). Blogs still recommend "no fills, relentless", "minimal drum fills", "no synth risers", "no big build or drop" in Style; none shows a result. | COMMUNITY (4) against; the recommendations are unsupported | section 2.2 sources ; https://songsmith.studio/blog/suno-drum-pattern-cheat-sheet ; https://note.com/webdirector/n/n26195398f6c6 |
| 5.5 | "Explicit boundary cues at section transitions tell Suno to make clean cuts rather than gradual fades" (v5.5 guide). This contradicts the project's rule 5 and rule 8. | COMMUNITY (1, v5.5, no test) | https://apipass.dev/blogs/suno-v5-v5-5-prompt-hacks-templates |
| 5.6 | The handoff's own position: positive anti-transition phrases in the drums line ("hard cuts only, straight in on beat 1", "the beat stays frozen from start to finish") plus the Exclude list plus vocabulary bans, "three ways at once". The owner has since heard reversed kicks and lead-ins and rule 8 now bans the boundary phrases too. | HANDOFF, superseded by the owner's ear | handoff s.6 ; CLAUDE.md rule 8 |
| 5.7 | **Exclude Styles works as a bias, not a ban**; officially it is for "things you don't want included". It is "the only reliable negative path" on v6. | CONFIRMED that the field exists and its purpose (summary) ; COMMUNITY (5) on bias-not-ban | https://help.suno.com/en/articles/3161921 ; https://suno.com/release-notes/exclude-styles ; bitwize best-practices.md ; https://songsmith.studio/blog/suno-negative-prompts-guide ; https://tinystudio.fm/guides/exclude-styles-cookbook/ |
| 5.8 | **Exclude works best short.** "Keep it to 2-4 items, over-specifying dilutes the effect"; "max 2-3, more confuses the model"; "do not pre-emptively exclude a long list on the first generation, it over-constrains the model"; "a negative list of 30+ items overwhelms the parser". Wording: bare nouns, no "no". | COMMUNITY (5 agree on short; none publishes a count-versus-effect test) | bitwize best-practices.md ; mttkllr field guide ; songsmith negative guide ; tinystudio cookbook ; https://www.genprompt.site/learn/suno-negative-prompts |
| 5.9 | In the v6 style view, Exclude terms are shown appended to the style with minus signs. | COMMUNITY (1) ; consistent with the 2024 release note showing "-piano" | hookgenius v6 guide ; suno.com/release-notes/exclude-styles |
| 5.10 | Excluding a genre can push the model to a neighbouring sound; broad exclude terms remove things you wanted. | COMMUNITY (1) | tinystudio cookbook |
| 5.11 | One GitHub PR claims Suno silently swaps some excluded terms for "similar styles". | UNVERIFIED (single source, messy diff) | https://github.com/git-z0man/claude/pull/34 |
| 5.12 | DJ scratches: prompt guides list "turntablism", "scratching hooks", "vinyl crackle", "turntable scratches cutting up the hook" as standard boom bap vocabulary, i.e. in the training captions scratches travel with boom bap, vinyl and turntable words. No source explains unprompted scratches or tests excluding them. | COMMUNITY (3 on the association) ; cause is MY JUDGMENT | https://wavecanvas.io/suno-prompts/rap ; https://openmusicprompt.com/suno/hip-hop/boom-bap ; https://hookgenius.app/learn/suno-hip-hop-prompts/ |
| 5.13 | Fewer section tags give a shorter track ("section count is the primary lever"). Nobody has measured tags against transitions. | COMMUNITY (1) on length ; no evidence on transitions | bitwize SKILL.md |

**Answers to the brief's direct questions**

- *Does naming a boundary plant it?* Not tested publicly. Everything adjacent says yes:
  Style words are additive (2.2), a silence cue gets a rebuild (5.2), contrast triggers
  staging (5.3). One v5.5 blog says the opposite (5.5) with no evidence. The handoff said
  "hard cuts only" worked; the owner's later listening says reversed kicks and lead-ins
  arrive at exactly the places the prompt talks about. I side with rule 8. MY JUDGMENT,
  supported by COMMUNITY (3) indirectly, contradicted by COMMUNITY (1).
- *Does Exclude work?* As a bias, yes (5.7). *Best wording?* Bare nouns, short list (5.8).
- *Do fewer section tags mean fewer transitions?* MY JUDGMENT: yes, and it is the lever
  with the best mechanism behind it. A transition needs a boundary, the model learns
  boundaries from section labels, and every tag with a different description is a contrast
  (5.3). Five tags that all say nearly the same thing are five invitations to mark a
  change that the text then says did not happen. Untested; cheap to test.

**The Exclude list is the biggest conflict with current practice.** A roll on today's tree
(`node forge.js --lane paak-pocket --seed 7`) emits 33 Exclude terms, 387 characters:
four vocal terms, twenty transition and FX terms, nine genre guards. Five independent
sources say 2 to 4, and `lib/spec.json` itself says "bites hardest with a short list; if
Suno ignores it, trim to the 4 most important terms" and sets the default preset to "core +
first 4 lane guards". The lanes file does not follow the spec. If 5.9 is right and Exclude
terms sit in the same conditioning text with a minus sign, then twenty near-synonyms for
"transition" are twenty mentions of transitions, each weakly negated, and the four vocal
terms that matter most are diluted by 29 others. The handoff built the long list and
believed in it; nobody has A/B'd it against a short one. I would.

---

## 6. Non-quantized, syncopated, human drums

No source has measured timing in Suno output (research/02 s.4 said the same a month ago;
still true). A vendor that sells drum repair says flatly that re-prompting will not fix
drums and that real players "drag the off-beats late, by the same amount, every bar";
random lateness does not read as human. COMMUNITY (1): https://voleor.com/docs/fix-ai-song-drums/

| Word or phrase | Reported effect | Grade | Source |
|---|---|---|---|
| "behind the beat" | The best-supported feel phrase. On one author's short list of words that move output across seeds; called with "pocket groove" the "single biggest dial Suno responds to". | COMMUNITY (3) | james-palm 12 words ; https://songsmith.studio/blog/suno-drum-pattern-cheat-sheet ; MeriaApp neo-soul kit ("live drums played loose and behind the beat") |
| "live drums", "played by hand", "one take", "recorded in one take with the band in the room" | Loosens the grid, and **changes the genre**: these are band words. | COMMUNITY (3) + HANDOFF ("played by hand", "one take, the wrong notes left in") | https://songsmith.studio/blog/suno-humanization-controls ; bitwize best-practices.md ; handoff s.4 |
| "hard swung drums", "swung hi-hats", "lazy swung drums" | Used in boom bap and lo-fi kits. The handoff found swing, shuffle and lilt came back as carnival or jazz in trap-tempo lanes. mttkllr lists "swing" as a weak tag needing reinforcement. | CONTRADICTORY: COMMUNITY (3 use it) against HANDOFF and COMMUNITY (1 weak) | MeriaApp boom bap kit ; undetectr lo-fi guide ; handoff s.2 and s.5 ; mttkllr field guide |
| "unquantized", "the snare a hair late", "clap layers a few milliseconds apart" | Handoff's human-feel tags; "quantised, robotic results came from prompts that had none of these". Public guides use "unquantized MPC swing" with no result shown. | HANDOFF ; COMMUNITY (1, untested) | handoff s.4-5 ; https://wavecanvas.io/suno-prompts/hip-hop |
| "MPC swing 58%", "12-20% timing jitter", any percentage | No evidence the model maps numbers to timing; bracketed percentage notes measured at 0% compliance for mix parameters. | COMMUNITY (2) says placebo | research/02 s.4 ; https://hookgenius.app/learn/suno-vocal-prompts/ |
| "off-grid", "drunk drums" | **Not found in any source.** No evidence for or against. | none | - |
| A producer's name used as a feel word | Appears in pre-v6 templates; Suno rewrites prompts with names (research/01). Not usable here. | - | project rule 1 |
| "complex drums" | Overcorrects to chaos. | COMMUNITY (1) | songsmith drum cheat sheet |
| "syncopated" + an element ("syncopated snare patterns", "syncopated 808 hits") | Standard in drill and trap templates; reads as a genre trait more than an instruction. | COMMUNITY (3 use it, none tests it) | songsmith ; https://hookgenius.app/learn/suno-hip-hop-prompts/ |
| Beat-position language ("kick on 1 and the and of 2") | One cheat sheet prescribes it; the handoff and two other sources say bar-level instruction in Style is not read. | CONTRADICTORY: COMMUNITY (1) against HANDOFF + COMMUNITY (2) | songsmith ; handoff s.2 ; strudelhub |
| Naming the drum **source and era** ("dusty sampled", "SP-1200 style drums", "mid-90s East Coast") | Moves drum character reliably; "boom bap requires vintage production tags or Suno will modernize it". | COMMUNITY (3) | undetectr v6 pro prompts ; rareconnections ; research/02 s.4 |

**What I take from it (MY JUDGMENT).**

1. Suno has no timing parameter. Feel comes from which recordings the words point at. The
   words that work ("behind the beat", "live", "played by hand", "swung") work because
   they point at bands and sample-based 90s records, and they drag the genre with them.
   That is the mechanism behind the soul lane's drift (section 10).
2. Syncopation written as beat arithmetic is not read (2.5). Syncopation written as a
   **named rhythm** is read, because the name is a genre tag and genre is the heaviest
   signal: dembow, two-step, a second-line or bounce pattern, a clave, a drill pattern.
   CLAUDE.md already says this (`FEEL_WORLDS`); the evidence supports leaning on it harder
   and spending fewer characters on written cadences.
3. Lateness should be attached to one element and stated as a constant ("the snare always
   a hair late"), matching how real drummers drag (voleor) and avoiding the band words.
4. Accept the ceiling. On v6 the grid is the default and text moves it a little. The
   owner's "generic, quantized" complaint is partly a model property that no prompt fixes.

---

## 7. Mid-song events with no transition

| # | Claim | Grade | Source |
|---|---|---|---|
| 7.1 | `[Key Change]` is unreliable: "Suno won't reliably obey a key-change tag"; "results are inconsistent across model versions; for more reliable key changes, generate in two parts and use Extend"; "[Change key to G minor]" is on a list of tags not reliably processed. | COMMUNITY (3) | https://lyromusic.com/prompts/key-change ; https://usesuno.com/guide/tags/ ; https://acetaggen.com/blog/bracket-tags-directors-language-suno |
| 7.2 | `[Beat Switch]` is listed in tag glossaries as "a distinct new tempo/feel mid-song"; nobody reports a hit rate. Tags generally are "probabilistic hints, not commands". | COMMUNITY (3 list it, 0 test it) | https://usesuno.online/tools/structure-tags ; https://musci.io/blog/suno-tags ; sovietmenace cheat sheet (v5.5) |
| 7.3 | v6 reads direction written inside a section bracket: a detail that existed only in a per-section cue showed up in the output. | COMMUNITY (2, possibly the same launch-day observation) | https://hookgenius.app/learn/why-you-shouldnt-use-suno-v6/ ; bitwize structure-tags.md |
| 7.4 | The handoff's switch rule: one axis only (half-time, key falls a minor third, strip to 808 and snare); the axis words appear in the head tag, in the switch cue and **in every section tag after it**, so nothing "returns". | HANDOFF | handoff s.7 |
| 7.5 | A silence or drop cue produces a gap and then a staged rebuild (5.2). | COMMUNITY (1) | brahmstorm silence post |
| 7.6 | High Weirdness (above about 60) causes tempo shifts and structure drift on its own. | COMMUNITY (2) | bitwize tip 56 ; research/01 |
| 7.7 | Replace Section (Pro/Premier) re-renders a highlighted region from edited tags. | CONFIRMED (feature, summary) | https://help.suno.com/en/articles/3271873 |

**Assessment (MY JUDGMENT).** There is no public evidence that any tag produces a clean,
unannounced switch. The handoff's method is the best available and it is consistent with
everything in section 5: describe **two states**, never the moment between them. The
switch is then just the place where the section description changes, which is the only kind
of event the model renders without decorating. Three practical points:

- A switch needs the fewest possible other boundaries. Two long sections, state A then
  state B, give the model one boundary to handle and it is the one you want.
- A **one-off glitch** is the hardest request in the brief. Anything said in Style is
  applied everywhere or periodically (the model has no "once"). It can only live in one
  section tag, as that section's state ("the snare doubled and smeared"), and the section
  must be short. Expect a low hit rate; Replace Section on a keeper is the reliable route.
- A key change by tag is the least reliable of the three (7.1) and a written key is barely
  read to begin with (3.3). Say the audible consequence instead: the same riff lower and
  darker, the lead an octave down.

---

## 8. Lyrics-field metatags for instrumentals on v6

| # | Claim | Grade | Source |
|---|---|---|---|
| 8.1 | Suno has never published a tag list or grammar; "adherence varies by model, song and section"; tag syntax and behaviour carried over unchanged from v5.5. | COMMUNITY (4) | https://roo.beehiiv.com/p/suno-commands ; https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/why-your-suno-v6-meta-tags-arent-working ; vocuno ; bitwize structure-tags.md |
| 8.2 | Reliability tiers (not model-labelled): `[Verse]`, `[Chorus]`, `[End]`, `[Fade Out]` reliable; `[Bridge]`, `[Break]`, `[Outro]` moderate; `[Intro]` "notoriously unreliable"; custom names less reliable; "standard tags like [Instrumental] or [Break] are more predictable than custom ones like [Solo] or [Drop]". | COMMUNITY (3) | bitwize structure-tags.md ; mysoniq ; mttkllr field guide |
| 8.3 | **Pipe directions inside a bracket** are a current, recommended v6 format: `[Verse 1 \| close and dry, half-spoken]`; every layout in a 2026-11-01 vendor guide uses it. An older field guide rates pipe stacking "High" reliability with a cap of 7 elements per tag. Bitwize documents a dash and "a word or two" instead. | COMMUNITY (2 for pipe, 1 for dash; no comparison) | MeriaApp README ; mttkllr field guide ; bitwize structure-tags.md |
| 8.4 | Keep cues short: a word or two, at most 3 descriptors per section; long sentences get sung or ignored. Percent-style parameter tags (`[Reverb: 30%]`) are placebo. | COMMUNITY (4) | bitwize SKILL.md ; freesongwritingtools metatags ; vocuno ; hookgenius vocal prompts |
| 8.5 | Bar counts in tags are "targets, not guarantees". A pastebin v6 note (as summarized, single-user reports) says a bare `[Solo]` or `[Instrumental Interlude]` can collapse to about a second and should carry a duration and a function. | COMMUNITY (2) | bitwize best-practices.md ; https://pastebin.com/ZFsx2ZAt (not opened) |
| 8.6 | `[End]` is "the strongest stop signal"; "about 85-90%" comes from a promotional post and should not be quoted as a number. A v6 outro left undescribed "tends to turn into a vamp". Duration Custom hard-cuts at the value. | COMMUNITY (3) | bitwize SKILL.md ; mttkllr field guide ; https://undetectr.com/blog/suno-v6-intro-outro-prompts |
| 8.7 | Section count is the main length lever; Duration Auto on v6 runs long and sparse; on one identical prompt v6 returned 3:08 to 3:16 where v5.5 returned 2:42 to 2:47. | COMMUNITY (3) | bitwize SKILL.md ; bitwize models.md ; https://hookgenius.app/learn/suno-v6-vs-v55/ |
| 8.8 | A per-section description can make one section sound different from the rest (e.g. a distinct chorus over a drier verse). | COMMUNITY (2) | CHACHI note.com ; MeriaApp README |
| 8.9 | Whether any of this is read with the Instrumental toggle on: disputed (4.3). | CONTRADICTORY | section 4.1 |

For instrumentals this adds up to (MY JUDGMENT): standard-looking labels, a pipe, at most
six to eight words of state description, nothing underneath, three to five sections,
`[End]` last, Duration Custom. The layout from today's tree has tags of up to 150
characters carrying bar-level and boundary language (`... the guitar chucks three stabs at
the end of each phrase, then nothing`; `[Outro | kick alone, stops dead on beat 1, about
3:03]`). That is past every published length guidance, and it places sounds against
boundaries, which CLAUDE.md rule 8 already forbids. The tree was reverted to an older build
in commit 60e82a8, so this may already be in hand.

---

## 9. Sliders

| # | Claim | Grade | Source |
|---|---|---|---|
| 9.1 | Weirdness runs Safe to Chaos, "50% is the normal expected result". Style Influence runs Loose to Strong, "how close you stay to your style input". Tweak one at a time. | CONFIRMED (summary) | https://help.suno.com/en/articles/6141377 |
| 9.2 | Variety "adjusts and updates your style prompts"; five stops Off / Normal / High / Extra / Max; default Normal on v6 and v6-mini, Off on v6-wild; the default returns when you switch model. | CONFIRMED (FAQ) for the mechanism ; COMMUNITY (2) for stops and defaults | help 13924481 ; bitwize creative-sliders.md ; bitwize CHANGELOG 0.104.0 |
| 9.3 | Style Influence defaults to 50; "at 50 it discards half the prompt's authority"; "very high Style Influence on a thin or contradictory prompt can produce a rigid, wooden result". | COMMUNITY (2) | bitwize SKILL.md and creative-sliders.md ; replayedstudio |
| 9.4 | Guide ranges for hip hop: Weirdness 20-40, Style Influence 50-70 (bitwize); 25 / 75-78 (undetectr templates; a masterforge benchmark). All "starting points, not Suno defaults". | COMMUNITY (3, opinion) | bitwize creative-sliders.md ; https://undetectr.com/blog/suno-prompts-for-hip-hop ; https://masterforge.app/blog/suno-v6-what-changed/ |
| 9.5 | "High Weirdness + high Style Influence: adventurous within the genre lane, the sweet spot for freshening a familiar genre." "High Weirdness + low Style Influence: may dissolve the genre." "Low Weirdness + high Style Influence: safe, on-genre, risk of generic." | COMMUNITY (1) | bitwize creative-sliders.md |
| 9.6 | Weirdness 0 sounds stale; above 90 risks artifacts; above 60, tempo shifts and structure drift. v6-wild for exploring at Weirdness 60-75 with lower Style Influence. | COMMUNITY (2; the 150-song reviewer was sponsored by Suno and says so) | https://moelueker.com/blog/suno-v6-vs-wild-vs-mini-150-song-test ; bitwize tip 56 |
| 9.7 | v6-wild ignored an instruction in the 150-song test ("started with vocals right away, which I had specifically asked it not to do"); v6 won prompt adherence 7 of 7 against wild in a seven-genre A/B. | COMMUNITY (2) | moelueker ; bitwize models.md |
| 9.8 | Max Mode "uses more compute to maximize consistency throughout the song", recommended over two minutes. | CONFIRMED (FAQ, summary) | help 13924481 |

**Reading for suno-forge (MY JUDGMENT).** The project's current v6 setting (Weirdness
48-65, Style Influence 70-85, raised on 2026-11-11) sits in the 9.5 "freshen a familiar
genre" quadrant and is defensible. It is two to three times the community hip hop
Weirdness figure, which is fine for the stated goal, but two cautions follow. First, with
Style Influence at 80 the model is pushed hard toward the Style text, so a field that names
a boundary, a band word or a party word gets that word rendered hard: high Style Influence
makes vocabulary mistakes worse, not better. Second, the upper end (60-65) is inside the
range where tempo and structure drift is reported, and structure drift is one of the
things that sounds like an unwanted transition. A sensible split: 45-58 for straight and
loop layouts, 50-60 for switch layouts, 65-80 only on v6-wild. `lib/spec.json`
(`hipHopRange`: "20-40 commercial, 40-55 experimental") and CLAUDE.md rule 10 (48-65)
currently disagree with each other.

---

## 10. Holding a lane

No source tests lane drift. What exists is co-occurrence evidence: which words sit together
in working prompts for the genre you do not want.

### 10.1 "Technical syncopated bounce" turning into bright party music

- A v6 vendor kit named **"Bright party rap"** reads: "Upbeat West Coast party rap, 98 BPM,
  a playful female rapper with a **bouncy**, rhythmic flow and call-and-response ad-libs",
  talk-box lead; its Exclude list is "dark pads, distorted 808, slow tempo". COMMUNITY (1):
  MeriaApp kits.json and README.
- Mood is ranked as heavy as or heavier than genre (2.4). COMMUNITY (2, disputed order).
- Conflicting tags make the model pick one ("jazzy + hyperpop = model picks randomly").
  COMMUNITY (1): https://roo.beehiiv.com/p/complete-list-of-prompts-styles-for-suno-ai-music-2026

MY JUDGMENT. "Bounce", "bouncy", "playful", "party", "upbeat", "fun", "West Coast", "talk
box", "claps", "funky" and a major key are one cluster in the model's captions. A lane
whose head says "bounce" and whose attitude words are "confident, strutting, unbothered"
has sent two party signals and no dark one; "technical" and "syncopated" are weak words
that do not counter them. And the kit's own Exclude list is a gift: the things that keep a
track out of party territory are **dark pads, a distorted 808 and a slower tempo**. To hold
the lane, the mood slot (worth as much as the genre slot) must say cold, tense or
menacing; the low end must be named heavy and distorted; the key colour minor; and the
word "bounce" should either go or be pinned by a regional rhythm name that is not a party
word. Put "party music, pop rap" (two terms, not nine) in Exclude.

### 10.2 A soul-sample lane turning into jazz or neo-soul

- A v6 vendor kit named **"Neo-soul groove"** reads: 86 BPM, "warm" vocal, Rhodes, a short
  horn line, "**live drums played loose and behind the beat**". COMMUNITY (1): MeriaApp.
- The same vendor's **boom bap** kit: "Mid-90s East Coast boom bap, 92 BPM", sampled jazz
  piano, vinyl crackle, "hard swung drums with a crunchy snare"; Exclude "autotune, 808
  slides, synth pads, trap hi-hats". COMMUNITY (1): MeriaApp.
- "Boom bap requires vintage production tags or Suno will modernize it." "MPC style chops
  tells Suno to chop samples rather than run a continuous loop." "If you want sample-based
  production, specify the source." COMMUNITY (3, as summarized; which quote belongs to
  which of these pages is approximate): research/02 s.4 (HookGenius) ; https://wavecanvas.io/suno-prompts/hip-hop ; https://james-palm.medium.com/the-complete-list-of-suno-ai-prompts-for-hip-hop-and-rap-trap-boom-bap-drill-3af109dafd21
- Jazz terms ("extended chords", "maj7") push toward complexity; Rhodes, sax, smooth, mellow
  pull elevator jazz. COMMUNITY (1) + HANDOFF: brahmstorm ; handoff s.2.

MY JUDGMENT. Neo-soul in the model's captions is a **live band**: live drums, loose, behind
the beat, warm, keys, a walking bass guitar, a clean guitar comping, horns. Today's
soul-lane roll says, in its first 200 characters, "bass guitar walking in like it owns the
place", "the keys loop", "a twitching hint of the guitar chucks", and later "a clean
electric guitar chucking short chords", "loose", with the lane titled "live pocket". Every
one of those is a neo-soul or funk band cue. The single word "sampled" is outvoted. Then
the Exclude list says "jazz, smooth jazz, funk, disco, elevator music", which per 5.10
pushes to the nearest neighbour that is not excluded: neo-soul. The words that hold a
sample lane are the ones that say a machine played a record: sampled, chopped, a sampler,
pads, pitched up or down, a specific era and region, mono, hard drums, a crunchy snare.
The humanizing has to come from the sample and the chop ("the chop lands a hair late"),
never from a player. This also resolves the tension in section 6: "behind the beat" and
"live" are the best-supported human-feel words and they are exactly the neo-soul trigger,
so in this lane they cannot be used on the drums.

---

## 11. Recommended changes for suno-forge

Numbered, concrete, each with example wording and a test. Example wording contains no
artist names and no negations. Grades show what each rule rests on. Because take-to-take
variance exceeds the effect of a small edit (3.1), **every render test below means at
least 4 takes per arm (2 generations), same model, Variety Off, same sliders, one change
only.**

1. **Cap the Style field by decisions, not characters.** Target 12 to 18 comma-separated
   decisions and 450 to 650 characters for mid and full density, 300 to 450 for sparse.
   One sound gets a source noun and at most two modifiers. (COMMUNITY, sections 2.2-2.3;
   contradicts rule 3's "full, up to 1000".)
   Example: `cold trap, 142 BPM half-time, dark minor, instrumental, menacing, a detuned music box up front, distorted 808 long and low, tight dry kick, rimshot always a hair late, sparse hats, mono, close and dry`
   Test: `selfaudit.js` reports decision count and length; `npm test` fails a non-sparse roll over 700 characters or over 20 comma parts. Render A/B: the same roll at 990 characters against its 550-character reduction.

2. **Put the diversity where the model listens.** The first five parts of the head (genre
   or subgenre tag, mood pair, era or region, tempo-feel word, signature noun) must differ
   between consecutive rolls; the tonic letter is not a diversity axis. (COMMUNITY 2.4, 3.2,
   3.3; HANDOFF 2.6.)
   Example heads that differ on every strong axis: `early-2000s southern trap, stomping, sinister, ...` / `mid-90s east coast boom bap, weary, grey, ...` / `dembow-driven dark club rap, tense, clipped, ...`
   Test: over 2,000 seeds, the mean Jaccard distance between consecutive rolls' first 150 characters stays above a set floor, and no genre tag or mood word opens more than 15% of rolls within a lane.

3. **Every lead has a real source noun in the head.** A `tone`-family description keeps its
   physical words but gains a noun the model has heard of. (MY JUDGMENT from 2.7 and 2.3;
   contradicts rule 9's "instrument names should stay a minority". This is the one I am
   least sure of and most want tested.)
   Example: `a bowed saw, grainy, leaning sharp` in place of `a long scraped string-like tone, grainy, the pitch leaning sharp`; other nouns of the same kind: kalimba, clavinet, steel tongue drum, mellotron flute, music box, talking drum, marimba, zither.
   Test: render A/B, noun version against no-noun version, 4 takes each; count takes whose lead is piano, bass guitar or acoustic guitar.

4. **Stop writing the key as a tonic; write its colour.** Keep minor or major and a mode
   word only as mood, in one short part; drop the letter name from the head (keep it in the
   block header for the user). (COMMUNITY 3.3-3.4.)
   Example: `dark minor, flat-second menace` or `bright Mixolydian strut` in place of `in Gb major`.
   Test: guard rule that no Style head contains a note letter followed by major or minor. Render check: detect the key of 8 takes with and without the tonic written; expect no difference.

5. **Write tempo as number plus feel plus a genre that lives at that tempo.** For
   half-time lanes always say which reading is meant. (COMMUNITY 3.5.)
   Example: `142 BPM half-time, slow heavy head-nod` / `92 BPM, unhurried` / `168 BPM double-time hats over a slow kick`.
   Test: `npm test` asserts every head has a BPM immediately followed by a feel phrase from a fixed list, and that lanes written above 120 contain the half-time phrase.

6. **Replace written beat positions with named rhythms wherever one exists; keep at most
   one physical in-bar phrase per roll.** (HANDOFF 2.5; COMMUNITY 2; supports CLAUDE.md's
   `FEEL_WORLDS`.)
   Example: `dembow kick pattern`, `two-step kick`, `second-line snare`, `stomp and clap`, `drill-pattern hats`, and one physical line such as `the 808 stumbles into three on beat 3`.
   Test: `audit.js` counts beat-position phrases ("on the and of", "beat 2", "16th ahead") per roll; cap 1, and at least 60% of rolls carry a named feel.

7. **Human timing is one constant on one element, never a band word.** (COMMUNITY section 6;
   voleor; MY JUDGMENT.)
   Example: `the snare always a hair late`, `the clap a touch behind the kick every time`, `hats played unevenly by hand, the same unevenness each bar`.
   Test: guard DENY for "live drums", "drummer", "band", "one take", "in the room" in sample-based lanes; `npm test` asserts exactly one lateness phrase per roll.

8. **Cut the Exclude field to 6 terms or fewer: 2 to 3 vocal, 2 for the roll's worst
   transition risk, 1 to 2 lane guards.** Make `lanes.js` obey `spec.json`'s existing
   preset. (COMMUNITY 5 sources, 5.8; contradicts the handoff's 22-term list and today's
   33-term output.)
   Example (straight layout, soul lane): `vocals, humming, drum fills, risers, neo-soul, live band`
   Example (vocal-signature roll): `lead vocals, singing, rapping, drum fills, risers, pop`
   Test: `qa.js` fails any Exclude field over 6 terms or 80 characters. Render A/B, the 33-term list against the 6-term list, 6 takes each; count fills, risers and reversed lead-ins per take. If the long list wins, revert this rule and record the result here.

9. **Never exclude a genre without naming where the track should sit instead, and never
   exclude a whole family of neighbours.** (COMMUNITY 5.10; MY JUDGMENT.)
   Example: Style `sample-based boom bap, chopped from an old soul 45`, Exclude `neo-soul, live band` rather than Exclude `jazz, smooth jazz, funk, disco, elevator music`.
   Test: `npm test` asserts at most 2 genre words in any Exclude list.

10. **Three to five section tags per layout, each a standard-looking label, a pipe and at
    most eight words describing a steady state.** Nothing bar-level, nothing about an
    ending, a start or a phrase end, no timestamps. (COMMUNITY 8.3-8.4, 5.3; MY JUDGMENT on
    fewer boundaries.)
    Example:
    ```text
    [Instrumental]

    [Hook | full beat, the music box up front]

    [Verse | same drums, music box an octave down]

    [Hook | full beat, the music box up front]

    [End]
    ```
    Test: `npm test` asserts 3 to 5 tags between `[Instrumental]` and `[End]`, each 60 characters or fewer, each passing the boundary guard. Render A/B: a 7-tag layout against its 4-tag reduction at the same Duration; count audible section-boundary events.

11. **Adjacent sections differ by one melodic thing and the drums line is word-for-word
    identical in each.** Contrast is what triggers staging (5.3), so give the model none in
    the drums. (COMMUNITY 2; HANDOFF "every section identical except one sound".)
    Example: `[Verse | same drums, bass alone under the kick]` then `[Hook | same drums, the clavinet doubles the bass]`.
    Test: `npm test` asserts the drum words in every tag of a straight or loop layout are the same string.

12. **A switch is two states and one boundary.** Switch layouts get exactly two long
    sections; the second repeats the axis words; the head names the axis once as a property
    of the song. (HANDOFF 7.4; MY JUDGMENT.)
    Example head part: `two beats in one song, the second half-time and darker`; tags: `[Part One | full beat, the zither riff up front]` and `[Part Two | half-time drums, same riff an octave down, heavier 808]`.
    Test: existing switch-axis QA, plus an assertion that a switch layout has exactly 2 body tags and that no tag between them exists.

13. **A one-off event lives in one short section tag as that section's state, and nowhere
    in Style.** Tell the user in POST-GEN NOTES that Replace Section on the keeper is the
    dependable way to get it. (MY JUDGMENT; COMMUNITY 7.1, 7.5.)
    Example: `[Break | the snare doubled and smeared, two bars]` placed once, with the Style field silent about it.
    Test: `npm test` asserts the signature's event words appear in exactly one tag and zero times in Style when the signature is a one-off.

14. **Vocal-signature rolls use sampler language only.** Object, machine, constancy, place;
    never a performer word. (COMMUNITY 4.5-4.6; HANDOFF 4.7; MY JUDGMENT.)
    Example: `one sampled shout, a single syllable, pitched down, the same one-shot each time, dry behind the snare` / `a warped hum off an old record, wordless, looped, buried under the keys`.
    Test: guard ALLOW for the family admits only {sampled, shout, one-shot, hum, wordless}; guard DENY keeps voice, vocalist, sings, ad-libs, chant, choir, crowd. Render check: 6 takes, count takes with any sung or rapped phrase.

15. **Bounce lane: one dark mood word and one heavy low-end word in the first 100
    characters; party-cluster words denied.** (COMMUNITY 10.1; MY JUDGMENT.)
    Example head: `cold west coast club rap, 98 BPM, tense, minor, instrumental, distorted 808 knocking, tight dry clap, a detuned clavinet riff`
    DENY for the lane: bouncy, playful, party, upbeat, fun, funky, feel-good, talk box, sunny. Exclude: `party music, pop rap`.
    Test: `npm test` asserts every roll in the lane has a word from {cold, tense, menacing, sinister, grim} and one from {distorted, heavy, sub-heavy} before character 100. Render check: 8 takes, owner marks party or not.

16. **Soul-sample lane: every melodic sound is a sample of a record, the drums are hard and
    machine-played, an era and region are named; band words denied.** (COMMUNITY 10.2;
    MY JUDGMENT.)
    Example head: `mid-90s east coast boom bap, 92 BPM, unhurried, instrumental, grim, a chopped soul 45 pitched up, the chop always a hair late, hard kick, crunchy snare, sub under the sample, mono`
    DENY for the lane: live, pocket, walking bass, bass guitar, comping, chucking, keys, Rhodes, horn section, warm, smooth, groove, jam. Rename the lane label from "live pocket".
    Test: guard walk over the lane's families; `npm test` asserts "sampled" or "chopped" within the first 120 characters. Render check: 8 takes, owner marks band or sample.

17. **Keep turntable and vinyl words out of every lane that does not want scratches.**
    (COMMUNITY 5.12 on the association; MY JUDGMENT on the cause.)
    Example: say `off an old 45, pitched up, grainy` and avoid vinyl, turntable, crackle, record player, DJ.
    Test: guard rule; `audit.js` reports any hit.

18. **Settle whether the Lyrics field is read with Instrumental on, once, and record it.**
    (CONTRADICTORY, 4.3.)
    Protocol: one Style, toggle on, 4 takes with a 4-tag layout that has an obvious
    half-time second part, 4 takes with an empty Lyrics field. If the half-time part appears
    in the first set and not the second, tags are read. Write the result into
    `lib/spec.json` (`instrumental.lyricsTagsHonored`) with the date.

19. **Sliders: make spec and rules agree, and split by layout.** v6 at Weirdness 45-58 and
    Style Influence 72-82 for loop and straight layouts, 50-60 for switch layouts; v6-wild at
    65-80 and 55-70 unchanged. (COMMUNITY 9.4-9.6; MY JUDGMENT.)
    Test: `qa.js` range check reads the ranges from `spec.json`; `spec.json` `hipHopRange` and CLAUDE.md rule 10 state the same numbers.

20. **Adopt a standing A/B protocol for any new rule.** 4 to 6 takes per arm, one change,
    Variety Off, both takes of each generation heard. Log arm, seed, takes and the owner's
    verdict in `research/`. (COMMUNITY 3.1.)
    Test: a `scripts/ab.js` that emits the two blocks for a seed with exactly one named difference and a results template.

---

## 12. Where this contradicts current project rules or the handoff

| Topic | Project / handoff says | Evidence here says | Weight |
|---|---|---|---|
| Exclude list length | Handoff: 22 terms; `lanes.js` today: 33-38 terms | 2 to 4, "over-specifying dilutes"; `spec.json` already agrees | COMMUNITY (5), untested against the long list |
| Style length | Rule 3: full density "up to 1000"; today's roll 991 | 8-20 decisions, vendor median 595; long tail read loosely | COMMUNITY (6, split on format) + HANDOFF |
| Written key and scale in the head | Rule 3 head tags; `theory.js` names seven scales | Key centre nearly ignored in hip hop; modes act as mood | COMMUNITY (2) |
| Beat-position cadences in Style | `CADENCE` in grammar; handoff s.5 syncopation tags | Bar-level instruction in Style not read (the handoff says this too, in s.2) | HANDOFF + COMMUNITY (2) against COMMUNITY (1) |
| "Instrument names a minority" | Rule 9 | A specific instrument noun is among the few words that reliably steer | COMMUNITY (2) + MY JUDGMENT |
| "hard cuts only" as a positive phrase | Handoff s.6 recommends; CLAUDE.md rule 8 bans; today's reverted tree emits it | No public test; adjacent evidence favours the ban; one v5.5 blog favours the phrase | MY JUDGMENT |
| Human-feel words | Handoff s.4: "played by hand", "one take" | They work and they are band words that feed neo-soul drift | COMMUNITY (3) + MY JUDGMENT |
| Lyrics tag length | Handoff: "under about 100 characters"; today up to 150 | A word or two, at most 3 descriptors, 7 elements at the outside | COMMUNITY (4) |
| Weirdness on v6 | Rule 10: 48-65; `spec.json`: 40-55; handoff: 40-55 | 20-40 typical, above 60 drift; high Weirdness with high Style Influence is defensible | COMMUNITY (3) |

Not contradicted: models and limits, Variety Off, Max Mode, no inline negatives, no artist
names, `[Instrumental]` first and `[End]` last, Duration Custom, no parentheses.

---

## 13. Could not verify

1. The text of any official Suno page (suno.com and help.suno.com were unreachable; all
   CONFIRMED items rest on search summaries).
2. Any Reddit or Discord thread, any YouTube video, and the pastebin v6 notes.
3. Whether Lyrics tags are read when the Instrumental toggle is on (4.3).
4. Whether naming a boundary plants a transition; whether fewer tags reduce transitions.
   No public test exists.
5. Any measured effect of any word on drum timing. "Off-grid" and "drunk drums" appear in
   no source.
6. Whether a long Exclude list performs worse than a short one in practice; how Exclude is
   applied internally (5.9 is one vendor's observation of the UI); the "swaps excluded
   terms for similar styles" claim (5.11).
7. BPM and key adherence rates on v6 (direction only; nobody publishes a rate).
8. The component-weight percentages in 2.4 (no method published).
9. Any hip-hop-specific producer assessment of v6 drums or 808s. The one independent v6
   drum opinion on record is still research/01's ("crisper" versus "flat").
10. Whether `[Beat Switch]`, `[Key Change]` or a half-time cue has a usable hit rate on v6.
11. HookGenius's figures (52 of 54; 585 generations; 12,843 songs) are one vendor's own
    tests with no published method; they are the best numbers available and should be
    treated as such.
12. The full contents of several pages returned only as fragments: the roo.beehiiv
    251-prompt audit, the unfilteredai 10,000-prompt piece, the jackrighteous v6 metatag
    article, the tinystudio cookbook, the HookGenius 77-tag ranking.

---

## 14. Sources (all accessed 2026-11-23)

**Official Suno (read through search summaries only)**
- Release notes index: https://suno.com/release-notes
- Introducing v6: https://suno.com/release-notes/introducing-v6 ; https://suno.com/blog/introducing-v6
- Current Models: v6: https://help.suno.com/en/articles/13924737
- v6 FAQ: https://help.suno.com/en/articles/13924481
- Creative Sliders: https://help.suno.com/en/articles/6141377
- How do I exclude elements of a song?: https://help.suno.com/en/articles/3161921 ; Exclude Styles release note (2024-09-19): https://suno.com/release-notes/exclude-styles
- Using MIDI files in Create: https://help.suno.com/en/articles/14594177 ; https://suno.com/blog/MIDI-in-create
- Replace Section: https://help.suno.com/en/articles/3271873

**Press (v6 launch)**
- https://axios.com/2026/09/09/suno-v6-ai-music-warner-bmg
- https://www.musicradar.com/music-tech/suno-has-rebuilt-its-ai-music-models-from-scratch-with-licensed-music
- https://musicbusinessworldwide.com/suno-v6-ai-music-models-launch-in-partnership-with-wmg-bmg-and-believe
- https://engadget.com/2251539/suno-trained-its-v6-ai-music-models-with-help-from-warner-and-bmg/
- https://boingboing.net/2026/09/16/mixed-reviews-for-ai-music-generator-sunos-industry-approved-new-model.html

**Community, read in full (GitHub raw)**
- bitwize-music-studio/claude-ai-music-skills: CHANGELOG.md (entries 2026-09-16, 2026-11-14, 2026-11-20), reference/suno/best-practices.md, creative-sliders.md, 100-expert-tips.md, models.md, structure-tags.md, tips-and-tricks.md, instrumental-tags.md, skills/suno-engineer/SKILL.md and genre-practices.md. Base: https://raw.githubusercontent.com/bitwize-music-studio/claude-ai-music-skills/main/
- MeriaApp/suno-prompt-guide (HookGenius, updated 2026-11-01): https://github.com/MeriaApp/suno-prompt-guide (README.md, data/kits.json)
- mttkllr/suno-field-guide (v4 to v5.5, last updated June 2026): https://github.com/mttkllr/suno-field-guide
- sovietmenace/suno cheat sheet (v5/v5.5, April 2026): https://github.com/sovietmenace/suno/blob/main/Suno_Master_Cheatsheet_Complete.md

**Community, read as search summaries**
- HookGenius: https://hookgenius.app/learn/suno-v6-guide/ ; https://hookgenius.app/learn/suno-v6-vs-v55/ ; https://hookgenius.app/learn/why-you-shouldnt-use-suno-v6/ ; https://hookgenius.app/learn/suno-prompt-guide-2026/ ; https://hookgenius.app/learn/suno-style-tag-research/ ; https://hookgenius.app/learn/suno-negative-prompting/ ; https://hookgenius.app/learn/suno-vocal-prompts/ ; https://hookgenius.app/learn/suno-hip-hop-prompts/ ; https://hookgenius.app/suno-prompts/
- Replayed Studio: https://replayedstudio.com/blog/suno-v6-prompts-not-working/ ; https://replayedstudio.com/blog/suno-v6-vocals-flat-rushed/
- Undetectr: https://undetectr.com/blog/suno-v6-prompts ; https://undetectr.com/blog/suno-v6-intro-outro-prompts ; https://undetectr.com/blog/suno-prompts-for-hip-hop ; https://undetectr.com/blog/suno-prompts-for-edm ; https://undetectr.com/blog/suno-v6-pro-production-prompts
- Roo newsletter: https://roo.beehiiv.com/p/ai-song-prompts-suno-v6 ; https://roo.beehiiv.com/p/best-suno-prompts-v6 ; https://roo.beehiiv.com/p/suno-commands ; https://roo.beehiiv.com/p/suno-v5-and-v5-5-prompts-why-it-ignores-half-your-words-and-how-to-fix-it ; https://roo.beehiiv.com/p/complete-list-of-prompts-styles-for-suno-ai-music-2026
- Brahmstorm: https://brahmstorm.com/blog/suno-chord-progression-prompts-what-works-what-gets-ignored ; https://brahmstorm.com/blog/suno-silence-prompts-how-to-make-space-work-in-your-tracks/
- Songsmith: https://songsmith.studio/blog/suno-humanization-controls ; https://songsmith.studio/blog/suno-drum-pattern-cheat-sheet ; https://songsmith.studio/blog/suno-negative-prompts-guide
- Tempo and key: https://heho.ai/blog/does-suno-follow-bpm ; https://sunomarket.com/guides/control-bpm-tempo ; https://freesongwritingtools.com/blog/suno-prompt-components-ranked ; https://www.genprompt.site/learn/turkish-trap-suno-prompts
- Exclude: https://tinystudio.fm/guides/exclude-styles-cookbook/ ; https://www.genprompt.site/learn/suno-negative-prompts ; https://github.com/git-z0man/claude/pull/34
- Tags: https://usesuno.com/guide/tags/ ; https://usesuno.online/tools/structure-tags ; https://lyromusic.com/prompts/key-change ; https://acetaggen.com/blog/bracket-tags-directors-language-suno ; https://musci.io/blog/suno-tags ; https://freesongwritingtools.com/blog/suno-metatags-guide/ ; https://vocuno.com/blog/suno-ai-prompt ; https://www.mysoniq.com/blog/suno-instrumental-break ; https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/why-your-suno-v6-meta-tags-arent-working ; https://sunoai-music.ai/blog/suno-tags ; https://hookgenius.app/learn/suno-lyrics-formatting/
- Other: https://james-palm.medium.com/reverse-engineered-200-suno-prompts-12-words-that-work-d8f06c0da727 ; https://note.com/chachi_iezaki/n/ncf49f4895d85 ; https://dev.to/build_master_5fc54ebe3f3d/what-i-learned-building-an-audio-to-suno-v6-prompt-tool-language-regional-genres-and-provider-1o6k ; https://strudelhub.com/blog/why-suno-ignores-chord-prompts ; https://voleor.com/docs/fix-ai-song-drums/ ; https://aimusicdaily.com/suno-v6-prompts ; https://moelueker.com/blog/suno-v6-vs-wild-vs-mini-150-song-test ; https://masterforge.app/blog/suno-v6-what-changed/ ; https://www.scriptbyai.com/suno-ai-timeline/ ; https://www.aimusicpreneur.com/ai-tools-news/suno-midi-upload-create-v6-wild-pro-premier/ ; https://lyromusic.com/prompts/instrumental ; https://www.rareconnections.io/suno-prompts ; https://wavecanvas.io/suno-prompts/hip-hop ; https://wavecanvas.io/suno-prompts/rap ; https://openmusicprompt.com/suno/hip-hop/boom-bap ; https://apipass.dev/blogs/suno-v5-v5-5-prompt-hacks-templates ; https://jackrighteous.com/en-us/blogs/guides-using-suno-ai-music-creation/suno-ai-build-intensity-prompt ; https://note.com/webdirector/n/n26195398f6c6
- Found but not opened: https://pastebin.com/ZFsx2ZAt ; https://www.youtube.com/watch?v=movgr65fyTU

**Project-internal**
- research/handoff/handoff-generator-team.md ; lib/spec.json ; research/01-suno-platform.md ; research/02-metatag-grammar.md ; CLAUDE.md ; output of `node forge.js --lane paak-pocket --seed 7` on 2026-11-23.
