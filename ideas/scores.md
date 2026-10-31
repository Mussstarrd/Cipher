# Scores

Judge output, 2026-10-31. Every one of the 30 candidates in `ideas/candidates.md` scored
1–5 on the six criteria in `CLAUDE.md`, one sentence per score, total out of 30.

**How I scored.** Against the owner's actual situation, not a generic trader:

- A home-built pipeline already emails a morning briefing, a setups report, and an
  execution report most market days plus a weekly signal-performance summary. So the
  owner already looks at this workflow daily, and **the pipeline already knows what was
  flagged and what was done**. Any idea that asks him to re-key that by hand loses on
  open frequency; any idea that reads it loses nothing.
- He declined the interview because it was too much input. **Heavy manual entry is the
  single biggest predictor of abandonment here**, and I docked open frequency for it
  every time.
- He works away from a desk and travels several days a month, on an Android phone plus a
  laptop. A page served from the laptop is unreachable from a hotel; a synced file is not.
- He is comfortable with code, so a CLI or a one-line pipeline change is not a barrier.
- Honesty rule: anything whose pitch invites a "what I should have made" reading was
  docked on sellability and delight, and I say where.
- Sellability is read from GitHub stars, forum tone, and gap analysis in
  `research/landscape.md`, not review sites (decision 0003).

Tie-breaker applied: where totals are equal, criteria 1 (pain) + 2 (frequency) decide the
order.

## Table (sorted by total)

| Rank | # | Candidate | Pain | Freq | Comp | Sell | Build | Delight | Total |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 2 | Signal Scorecard | 5 | 4 | 5 | 3 | 3 | 4 | **24** |
| 2 | 1 | Setup Ledger | 4 | 3 | 5 | 3 | 4 | 3 | **22** |
| 3 | 18 | Setups Diff | 3 | 4 | 4 | 2 | 4 | 4 | **21** |
| 4 | 5 | Report Vault | 3 | 3 | 4 | 2 | 5 | 3 | **20** |
| 5 | 28 | The Markdown Journal | 3 | 3 | 4 | 3 | 4 | 3 | **20** |
| 6 | 7 | Setup Rulebook | 3 | 2 | 4 | 3 | 4 | 3 | **19** |
| 7 | 24 | Weekly Review Composer | 3 | 2 | 4 | 3 | 3 | 4 | **19** |
| 8 | 14 | Why-Is-It-Down Tagger | 3 | 3 | 4 | 2 | 3 | 3 | **18** |
| 9 | 21 | Decision Inbox | 3 | 3 | 4 | 2 | 3 | 3 | **18** |
| 10 | 3 | Robinhood CSV Round-Trip Journal | 3 | 2 | 4 | 4 | 2 | 3 | **18** |
| 11 | 13 | Thesis Lab Notebook | 3 | 2 | 4 | 2 | 3 | 3 | **17** |
| 12 | 6 | Five-Minute Evening Check-In | 2 | 3 | 3 | 2 | 5 | 2 | **17** |
| 13 | 8 | Read-Only Inbox Digest | 3 | 3 | 2 | 3 | 3 | 2 | **16** |
| 14 | 4 | Skipped-Setup Follow-Up | 3 | 2 | 4 | 2 | 3 | 2 | **16** |
| 15 | 11 | Beat Catalog | 2 | 1 | 3 | 3 | 3 | 4 | **16** |
| 16 | 12 | Sender Triage to Gmail Filters | 3 | 1 | 2 | 3 | 3 | 3 | 15 |
| 17 | 26 | Shadow Book | 3 | 2 | 4 | 2 | 2 | 2 | 15 |
| 18 | 22 | The Evening Page | 3 | 3 | 3 | 2 | 1 | 2 | 14 |
| 19 | 9 | School Email to Calendar File | 2 | 2 | 2 | 3 | 3 | 2 | 14 |
| 20 | 17 | Pipeline Heartbeat | 2 | 2 | 2 | 1 | 5 | 2 | 14 |
| 21 | 25 | The No-Trade Journal | 2 | 2 | 3 | 2 | 4 | 1 | 14 |
| 22 | 16 | Household Week Sheet | 3 | 2 | 2 | 2 | 2 | 2 | 13 |
| 23 | 15 | Away-Day Aware Journal | 2 | 2 | 3 | 1 | 3 | 2 | 13 |
| 24 | 23 | Never-Read Senders | 2 | 2 | 2 | 2 | 3 | 2 | 13 |
| 25 | 10 | Trip Receipt Stager | 2 | 1 | 2 | 2 | 4 | 2 | 13 |
| 26 | 20 | Beat Changelog | 1 | 1 | 3 | 2 | 3 | 3 | 13 |
| 27 | 30 | Plain-Language Weekly for the Household | 1 | 2 | 3 | 1 | 3 | 2 | 12 |
| 28 | 19 | Trip Receipt Harvester | 2 | 1 | 2 | 2 | 2 | 2 | 11 |
| 29 | 27 | One Look a Day | 1 | 2 | 2 | 1 | 4 | 1 | 11 |
| 30 | 29 | Beats as Setups | 1 | 1 | 3 | 1 | 3 | 2 | 11 |

Ranks 1–10 go to the stress test. Ranks 11–15 survive the cull but are not finalists;
they are mutation material. Ranks 16–30 are dropped.

## Rationales

### 2. Signal Scorecard — 24
- **Pain 5.** It is the interview's definition of life-changing, verbatim: one page, is the system working, sharper every week.
- **Freq 4.** Reports land daily, so if it reads the folder (or IMAP label) there is a daily reason to open it with zero typing; not 5 because the first version may only show taken trades until outcome prices are available.
- **Comp 5.** Rolling 4/12-week windows are literally nothing on day one and the whole point on day 300.
- **Sell 3.** "Hit rate by signal" exists nowhere in the landscape, but the audience is people who run their own pipeline, which is a sliver of r/algotrading.
- **Build 3.** Parsing the owner's own report formats is unknown work and the signal-to-outcome join is the hard problem the pitch itself flags.
- **Delight 4.** Watching a per-signal curve sharpen is the one thing the weekly email cannot do, and it is the owner's own thesis on screen.

### 1. Setup Ledger — 22
- **Pain 4.** It fills the exact "screener hit to decision to outcome" gap the scout found empty, but it re-keys data the pipeline already emails.
- **Freq 3.** Two manual touches a day from an owner who declined an interview for being too much input; realistic steady state is a few times a week.
- **Comp 5.** Every row with a decision attached is permanent, and the scorecard tab only gets better.
- **Sell 3.** Nobody sells this shape, but paste-driven journals are what the spreadsheet-proud community already does for free.
- **Build 4.** Paste parser, SQLite, a table, a scorecard tab; the only unknown is the setups-report format.
- **Delight 3.** The morning paste is fine; the evening re-key is the homework the pitch admits to.

### 18. Setups Diff — 21
- **Pain 3.** New-versus-persisting is useful context for a mean-reversion thesis but is not a pain the owner reported.
- **Freq 4.** If it reads the day's setups report automatically it is a free daily glance; the paste version would be a 3.
- **Comp 4.** Persistence history per ticker grows daily; the first-day-versus-repeat tally needs outcomes it does not own.
- **Sell 2.** Depends on having a daily screener dump; a view, not a product.
- **Build 4.** Set difference on ticker lists plus a history table.
- **Delight 4.** Three new names and two repeats at a glance is the kind of small satisfying read that gets a page opened.

### 5. Report Vault — 20
- **Pain 3.** Gets ninety days of reports out of the catch-all inbox and makes them searchable, which is real relief for area B and the substrate for area A.
- **Freq 3.** A filing cabinet is opened when something sends you there, not every day on its own.
- **Comp 4.** Plain dated files accumulate forever and nothing reads them wrongly.
- **Sell 2.** It is specific to one person's pipeline; the general version is "save emails to disk", which Gmail Takeout already does.
- **Build 5.** A one-line pipeline change and a calendar-browse page; nothing can really go wrong.
- **Delight 3.** Scrolling back to last Tuesday in one click is pleasant, not thrilling.

### 28. The Markdown Journal — 20
- **Pain 3.** Same gap as the Setup Ledger, same re-keying cost, minus the UI friction for a code-comfortable owner.
- **Freq 3.** Two lines in a synced text file is the lowest-ceremony manual entry possible and works from a phone in a hotel, but it is still manual entry every day.
- **Comp 4.** Ninety plain files outlive every app; the scorecard sharpens exactly as the ledger's would.
- **Sell 3.** The plain-text-accounting crowd and HN reliably adopt this shape; everyone else bounces off a convention.
- **Build 4.** A six-line grammar, a parser, and an HTML render are days of work; strict error reporting is the part that must not be skimped.
- **Delight 3.** Satisfying for someone who likes plain text, and the "half of March never counted" failure is a real cliff.

### 7. Setup Rulebook with Followed/Broken Tally — 19
- **Pain 3.** Knowing which rule he breaks most is useful, but his thesis is one rule set and the tally grows one row per trade.
- **Freq 2.** Ticks happen per trade, and the owner's selective thesis means a few trades a week at most.
- **Comp 4.** Rows accumulate and the per-rule split needs volume to say anything, which it eventually gets.
- **Sell 3.** LuxAlgo's playbooks prove demand; a local options version with no account is a plausible niche.
- **Build 4.** Checklist, per-trade ticks, grouped results; LuxAlgo (MIT) can be studied.
- **Delight 3.** Seeing a broken-rule column fill up is sobering rather than fun.

### 24. Weekly Review Composer — 19
- **Pain 3.** It turns the weekly summary email into a reflection the owner writes himself, which the landscape says journals never capture.
- **Freq 2.** Weekly by design; the pre-fill is empty unless something else is logging daily.
- **Comp 4.** Twelve stacked reviews are a genuinely compounding record of the operator, not just the system.
- **Sell 3.** Edgewonk charges for review prompts; a local version has an audience.
- **Build 3.** The composer is simple; it has no data of its own and needs a logging layer first.
- **Delight 4.** Reading a pre-filled week and writing one honest line is the most pleasant moment in the whole pile.

### 14. Why-Is-It-Down Tagger — 18
- **Pain 3.** The "no reason found" tag is the owner's thesis in one word, so splitting outcomes by it is close to the question he actually has.
- **Freq 3.** One tap per flagged ticker is cheap, but it is still a daily manual step on a ledger that has to exist first.
- **Comp 4.** The by-reason split means nothing until a few dozen rows exist, then means more every week.
- **Sell 2.** A column, not a product.
- **Build 3.** Trivial on its own, but it requires the whole ledger underneath to be worth anything.
- **Delight 3.** Tagging is neutral; the eventual split is interesting, and the pitch honestly flags that fuzzy tags can look like findings.

### 21. Decision Inbox — 18
- **Pain 3.** Capturing the "why" in the moment is the layer every journal lacks.
- **Freq 3.** The owner is away from the laptop during market hours and travels; a localhost page is unreachable from the field, and the phone's notes app is one tap away.
- **Comp 4.** Timestamped decisions joined to setup rows accumulate well.
- **Sell 2.** The phone-reach problem is structural for any local-first tool and the pitch admits v1 is laptop-only.
- **Build 3.** The text box is easy; reliable phone access away from home is not.
- **Delight 3.** One sentence feels good; discovering the laptop was asleep does not.

### 3. Local Options Round-Trip Journal from Robinhood CSV — 18
- **Pain 3.** The execution report already records fills, so this adds leg grouping and searchable history rather than removing a daily chore.
- **Freq 2.** The CSV takes 2–24 hours to generate on demand, which makes this a monthly ritual, not a daily open.
- **Comp 4.** A grouped history in one SQLite file grows and stays useful.
- **Sell 4.** The scout found no local-first options journal with a Robinhood path; r/thetagang and r/options are large and subscription-skeptical.
- **Build 2.** The options row layout of the Robinhood CSV is unverified; leg grouping on an unknown format is a guessing game that is wrong quietly.
- **Delight 3.** Seeing a spread as one row instead of four is satisfying once a month.

### 13. Thesis Lab Notebook — 17
- **Pain 3.** A dated record of what he believed and when is the honest version of "did the thesis change or did the market".
- **Freq 2.** Versioning a hypothesis is more structure than a five-minute, low-input habit tolerates.
- **Comp 4.** Each version's sample grows, and the small-sample warning is exactly the honest framing the studio wants.
- **Sell 2.** Lab-notebook framing appeals to a narrow, quantitative slice.
- **Build 3.** Versioned hypotheses plus the same join problem as the scorecard.
- **Delight 3.** Intellectually pleasing, emotionally dry.

### 6. Five-Minute Evening Check-In — 17
- **Pain 2.** It records but computes nothing; the pitch concedes the streak counter is the only reward.
- **Freq 3.** It asks for a daily open and is cheap, but the owner's low-input stance means a diary with no payoff lapses.
- **Comp 3.** Entries accumulate for something else to read; on its own, a pile.
- **Sell 2.** Stonk Journal already does manual-entry journaling for free and gets two stars for it.
- **Build 5.** Three fields, a date stamp, a list.
- **Delight 2.** A streak counter with nothing behind it.

### 8. Read-Only Inbox Digest — 16
- **Pain 3.** Area B is daily pain and a category-grouped skim is less noisy than the inbox.
- **Freq 3.** It would be opened daily, but it is one more page to look at rather than one fewer, as the pitch admits.
- **Comp 2.** Yesterday's digest has almost no value today.
- **Sell 3.** r/selfhosted likes IMAP-only tools and MailDigest shows the shape works.
- **Build 3.** IMAP with an app password is the boring path, but sender categorisation rules and network dependence add work.
- **Delight 2.** Gmail's tabs already do 80% of this.

### 4. Skipped-Setup Follow-Up — 16
- **Pain 3.** What happened to the ones he did not take is genuinely absent from every tool.
- **Freq 2.** Typing a close per open skipped ticker every day is the chore the pitch itself predicts he abandons in two weeks.
- **Comp 4.** The skipped-versus-taken population compounds if fed.
- **Sell 2.** Docked: "whether skipping was costing or saving him" is a counterfactual money framing that the honesty rule says software should not imply.
- **Build 3.** Simple tables, but the price-entry loop and the five-day auto-close need care.
- **Delight 2.** Logging the ones that got away is not fun, and the half-populated comparison misleads.

### 11. Beat Catalog — 16
- **Pain 2.** Hunting through `_final_final` versions is real but episodic and low-stakes, as the interview ranks it.
- **Freq 1.** Production happens in bursts; between bursts the catalog is not opened.
- **Comp 3.** The index and metadata grow, but a stale index re-scans on every visit.
- **Sell 3.** Three dead cloud attempts show demand and a local index of WIPs is a true gap.
- **Build 3.** Indexing and a player are fine; version grouping by name stem is heuristic.
- **Delight 4.** Every beat in one list with a player is the most fun object in the pile.

### 12. Sender Triage to Gmail Filters — 15
- **Pain 3.** A one-time inbox clean-up removes real noise.
- **Freq 1.** Quarterly at best after the first pass, as the pitch concedes.
- **Comp 2.** A log of decisions, little else.
- **Sell 3.** Clean Email's audience is price- and privacy-sensitive, and the Gmail XML import is a clever account-free path.
- **Build 3.** IMAP header pull plus XML writer, with the `\Seen` flag doing the work.
- **Delight 3.** Seeing the top 20 senders by volume is a satisfying one-off.

### 26. Shadow Book — 15
- **Pain 3.** "How far did I drift from the rule" is a real question.
- **Freq 2.** Needs a daily close for every flagged ticker, which is the heaviest input burden in the pile.
- **Comp 4.** Two books side by side get richer with every row.
- **Sell 2.** Docked: a side-by-side invites the "what I should have done" reading the honesty rule refuses, and the pitch admits the labelling has to work very hard.
- **Build 2.** Daily closes by paste or an external quote source, plus two full books.
- **Delight 2.** Docked for the same reason; it will often feel like a regret machine.

### 22. The Evening Page — 14
- **Pain 3.** Three recurring pains behind one habit is the right instinct.
- **Freq 3.** The trading panel alone would earn the open; the others are dead weight.
- **Comp 3.** Each panel keeps its own history.
- **Sell 2.** Personal dashboards are a crowded hobby category.
- **Build 1.** The pitch itself says v1 ships with two panels stubbed, which fails the shelf-ready definition outright.
- **Delight 2.** A page that does three things does none deeply.

### 9. School Email to Calendar File — 14
- **Pain 2.** Weekly relay pain, shared with another adult, and ranked C by the interview.
- **Freq 2.** Weekly when school email arrives.
- **Comp 2.** A log of events; little compounds.
- **Sell 3.** The local `.ics` gap is real in a crowded cloud field.
- **Build 3.** Date extraction from "the Friday before break" is messy.
- **Delight 2.** One missed event ends trust, as the pitch says.

### 17. Pipeline Heartbeat — 14
- **Pain 2.** The owner reads the reports daily and would notice a missing one himself.
- **Freq 2.** A grid of green squares is opened when something is red.
- **Comp 2.** A run history is nice to have, not valuable.
- **Sell 1.** Healthchecks.io-style monitors already exist and nobody else has this pipeline.
- **Build 5.** Watch a folder, check timestamps and sizes.
- **Delight 2.** Green squares.

### 25. The No-Trade Journal — 14
- **Pain 2.** Declined setups are unrecorded anywhere, but the execution report already covers the other half.
- **Freq 2.** Logging only "no"s is a habit few sustain.
- **Comp 3.** The declined population does accumulate.
- **Sell 2.** An inversion that reads as a gimmick to most.
- **Build 4.** A narrow ledger.
- **Delight 1.** The pitch says it: a list of regrets.

### 16. Household Week Sheet — 13
- **Pain 3.** Weekly household relay is real and shared.
- **Freq 2.** Sunday only.
- **Comp 2.** A "told the other adult" log is a record, not an asset.
- **Sell 2.** The other adult may already live in a shared Google calendar.
- **Build 2.** Email parsing plus IMAP plus `.ics` is three fragile parts.
- **Delight 2.** A duplicate of a shared calendar.

### 15. Away-Day Aware Journal — 13
- **Pain 2.** A single insight, as the pitch concedes.
- **Freq 2.** Marking trips is occasional; the journal underneath has to carry the daily open.
- **Comp 3.** The present-versus-away split grows with data.
- **Sell 1.** Almost nobody else has this exact combination.
- **Build 3.** A calendar and a tag on top of a journal that does not exist yet.
- **Delight 2.** Once seen, done.

### 23. Never-Read Senders — 13
- **Pain 2.** Points at work that still happens in Gmail.
- **Freq 2.** Weekly list, shrinking.
- **Comp 2.** A mute log.
- **Sell 2.** Gmail's own unsubscribe nudges are closing this gap.
- **Build 3.** IMAP header scan with `\Seen` flags.
- **Delight 2.** A to-do list for another app.

### 10. Trip Receipt Stager — 13
- **Pain 2.** Bounded, and the employer tool is mandatory (decision 0003).
- **Freq 1.** A few days a month, and only while travelling.
- **Comp 2.** Closed trip folders accumulate but are rarely revisited.
- **Sell 2.** Expensify's free tier does capture already.
- **Build 4.** A folder watcher and a table.
- **Delight 2.** Receipts.

### 20. Beat Changelog — 13
- **Pain 1.** Low-pain area, and the pitch admits a producer in flow will not stop to type.
- **Freq 1.** Bursts only.
- **Comp 3.** A per-beat story is a real compounding record if kept.
- **Sell 2.** Splice Studio died doing a bigger version of this.
- **Build 3.** Folder watching and a prompt.
- **Delight 3.** Reading "v4 swapped drums" months later is charming.

### 30. Plain-Language Weekly for the Household — 12
- **Pain 1.** Removes none of the owner's time; adds a reader.
- **Freq 2.** Weekly.
- **Comp 3.** Twelve paragraphs do accumulate.
- **Sell 1.** The pitch itself flags that the owner may not want the household reading his trading record, and the interview could not probe it.
- **Build 3.** Templated prose over logged entries that something else must produce.
- **Delight 2.** Accountability is not the same as pleasure.

### 19. Trip Receipt Harvester — 11
- **Pain 2.** Employer tool is mandatory; this only stages.
- **Freq 1.** Per trip.
- **Comp 2.** Per-trip folders.
- **Sell 2.** Concur's own email-in exists, however fragile.
- **Build 2.** IMAP plus per-vendor receipt parsing that the pitch admits will be wrong often enough to check every one.
- **Delight 2.** Checking every amount is most of the original work.

### 27. One Look a Day — 11
- **Pain 1.** Removes nothing; adds a lock he can bypass by opening the email.
- **Freq 2.** Daily by design, but theatre.
- **Comp 2.** A count of looks.
- **Sell 1.** Focus apps exist; nobody wants a report-specific one.
- **Build 4.** Trivial.
- **Delight 1.** A self-imposed lockout that does not lock.

### 29. Beats as Setups — 11
- **Pain 1.** Neither hobby asked for this.
- **Freq 1.** Bursts, logged by hand.
- **Comp 3.** A shared ledger engine does accumulate.
- **Sell 1.** The pitch admits treating creative work as a pipeline can kill the fun.
- **Build 3.** Reuses the ledger engine, if one exists.
- **Delight 2.** A scorecard for music.

## Cull: the 15 dropped

| # | Candidate | Why it goes |
|---|---|---|
| 12 | Sender Triage to Gmail Filters | A one-off clean-up; quarterly open frequency fails the tie-breaker. |
| 26 | Shadow Book | Heaviest input burden in the pile and a side-by-side that invites the counterfactual reading the honesty rule forbids. |
| 22 | The Evening Page | Ships with two of three panels stubbed by its own admission, which is not shelf-ready. |
| 9 | School Email to Calendar File | Weekly pain, messy extraction, and one miss ends trust. |
| 17 | Pipeline Heartbeat | The owner reads the reports daily and would notice a gap faster than a dashboard would. |
| 25 | The No-Trade Journal | A list of regrets; delight 1 is fatal for a daily habit. |
| 16 | Household Week Sheet | Three fragile parts to duplicate a shared calendar the household may already use. |
| 15 | Away-Day Aware Journal | One insight on top of a journal that does not exist yet. |
| 23 | Never-Read Senders | A report that points at work in Gmail. |
| 10 | Trip Receipt Stager | Monthly at most, feeding a mandatory employer tool. |
| 20 | Beat Changelog | Interrupts the creative moment it depends on; lowest-pain area. |
| 30 | Plain-Language Weekly for the Household | Adds a reader the owner may not want; removes no time. |
| 19 | Trip Receipt Harvester | Receipt parsing wrong often enough that he checks every one. |
| 27 | One Look a Day | A lockout he bypasses by opening the email. |
| 29 | Beats as Setups | Neither hobby asked for it. |

Nothing in this list scored above 3 on open frequency. Nothing dropped is unique in a way
the survivors cannot absorb: the household and receipt ideas are kept alive only as
possible later panels, the inbox ideas as an optional IMAP ingest path.

**Kept but not finalists (ranks 11–15):** 13 Thesis Lab Notebook, 6 Five-Minute Evening
Check-In, 8 Read-Only Inbox Digest, 4 Skipped-Setup Follow-Up, 11 Beat Catalog. These
survive as mutation material. Beat Catalog is the strongest non-trading idea and the only
one with a delight score of 4 outside area A; it loses purely on open frequency.

## Stress test: would he still open this in six months?

### 2. Signal Scorecard — **Yes** (conditional)
- *Compound or novelty?* Compounds: rolling windows by signal are the owner's actual
  question and they sharpen weekly.
- *Random Tuesday trigger?* The pipeline's report email arrives; the page shows the same
  day's row already parsed. If it reads from a folder or IMAP label, the trigger costs
  nothing. If it depends on paste, the trigger is "remember to paste", and the verdict
  becomes Doubtful.
- *What he reaches for instead?* The weekly signal-performance email. It already exists,
  which is both the threat and the proof of demand. The page wins only if it shows
  something the email cannot: history, rolling windows, and the join to what he did.
- *Busy week?* Reports keep landing in the folder; the page catches up with zero backlog.
  This is the only candidate that recovers from a travel week without the owner doing
  anything.
- *Verdict:* **Yes**, provided ingest is automatic. The outcome join is the honest caveat:
  taken trades close via the execution report, but flagged-and-not-taken outcomes need a
  later price, and if the morning briefing does not carry one the v1 scorecard covers only
  taken trades. That is still "how did the trades I took on this signal do", which is
  worth opening.

### 1. Setup Ledger — **Doubtful** as written, Probably if mutated
- *Compound or novelty?* Compounds; every row with a decision is permanent.
- *Random Tuesday trigger?* Morning paste, evening mark. Two manual touches from an owner
  who finds input expensive.
- *What he reaches for instead?* Nothing: the morning setups report and the execution
  report already tell him what was flagged and what he did; the ledger asks him to copy
  them.
- *Busy week?* Rots. Five days of unmarked rows become a nag, and a nag becomes a closed
  tab. Recoverable only if unmarked rows default to "skipped" after N days.
- *Verdict:* **Doubtful** as specified. It becomes **Probably** the moment the evening step
  is replaced by reading the execution report, at which point it is the Signal Scorecard's
  front end rather than a separate product.

### 18. Setups Diff — **Probably** as a view, Doubtful alone
- *Compound or novelty?* Persistence history compounds; the glance itself is a small
  daily pleasure.
- *Random Tuesday trigger?* "Which names are back today" is a real morning question for a
  mean-reversion thesis.
- *What he reaches for instead?* Eyeballing yesterday's email next to today's.
- *Busy week?* Zero cost if automatic; the diff over a five-day gap is still correct.
- *Verdict:* **Probably** as the day-view of the scorecard. **Doubtful** as a standalone
  product, where it is a curiosity, as the pitch admits.

### 5. Report Vault — **Doubtful** alone, Yes as substrate
- *Compound or novelty?* Files accumulate; nothing reads them.
- *Random Tuesday trigger?* None. Filing cabinets are opened when something sends you there.
- *What he reaches for instead?* Gmail search.
- *Busy week?* Nothing happens; it is a folder. Perfect recovery, no reason to return.
- *Verdict:* **Doubtful** on its own. It is, however, the one-line pipeline change that
  makes every other area-A idea low-input, and it should be absorbed rather than dropped.

### 28. The Markdown Journal — **Probably**
- *Compound or novelty?* Compounds; ninety plain files plus a scorecard.
- *Random Tuesday trigger?* Typing `- XYZ skipped earnings` in a synced text file from
  the phone takes ten seconds and needs no server running. For an owner who travels and
  is comfortable with code, this is the most robust manual path in the pile.
- *What he reaches for instead?* Nothing simpler exists; a notes app is the same gesture
  without the scorecard.
- *Busy week?* Degrades gracefully: missing days are missing, nothing nags, nothing
  corrupts. The real risk is drift: a convention typo in week six that silently drops rows
  until the parser is made loud about rejects.
- *Verdict:* **Probably**. It still asks for manual entry every day, which is the owner's
  stated dislike, but it is the cheapest possible form of it.

### 7. Setup Rulebook — **Doubtful**
- *Compound or novelty?* Compounds slowly, one row per trade, and the thesis is a single
  rule set.
- *Random Tuesday trigger?* Only on days he trades.
- *What he reaches for instead?* Memory; he knows which rule he breaks.
- *Busy week?* Nothing to tick; recovers fine, but also nothing pulls him back.
- *Verdict:* **Doubtful** alone. Worth one column in a ledger later.

### 24. Weekly Review Composer — **Doubtful** alone, Probably if fed
- *Compound or novelty?* Twelve honest Sunday notes compound.
- *Random Tuesday trigger?* None; Sunday only, and the weekly email is already the Sunday
  trigger.
- *What he reaches for instead?* The weekly email.
- *Busy week?* The pre-fill is empty if nothing logged daily; a blank diary on Sunday is
  skipped.
- *Verdict:* **Doubtful** standalone; **Probably** as the weekly tab of a scorecard that
  already has the data.

### 14. Why-Is-It-Down Tagger — **Probably** as a column
- *Compound or novelty?* The by-reason split compounds, and "no reason found" is the
  owner's thesis in one tag, so this is the most thesis-relevant manual field in the pile.
- *Random Tuesday trigger?* Only if a ledger row is already on screen.
- *What he reaches for instead?* Nothing; no tool does this.
- *Busy week?* Untagged rows are simply untagged; the split just shows fewer rows.
- *Verdict:* **Probably** as the one optional manual field on a ledger. Not a product.

### 21. Decision Inbox — **Doubtful**
- *Compound or novelty?* Compounds if used.
- *Random Tuesday trigger?* He is in the field; the page is on a laptop at home. The
  pitch concedes v1 is laptop-only.
- *What he reaches for instead?* The phone's notes app, every time.
- *Busy week?* Nothing captured, nothing to join.
- *Verdict:* **Doubtful**. The Markdown Journal's synced-file approach solves the same
  capture problem without a server.

### 3. Robinhood CSV Round-Trip Journal — **Doubtful** (on frequency)
- *Compound or novelty?* History compounds and leg grouping is a durable improvement
  over the execution report.
- *Random Tuesday trigger?* None. The CSV takes 2–24 hours to generate, so this is a
  monthly import, and the execution report already tells him what filled.
- *What he reaches for instead?* The execution report; Robinhood's own history.
- *Busy week?* Nothing happens; recovers fine; also no reason to return.
- *Verdict:* **Doubtful** against the daily bar, although it is the most sellable idea in
  the pile and the only one strangers could use without the owner's pipeline. If the owner
  cares more about something to list than something to open, this is the mutation to ask
  for.

## Judge's note on the pile

The pile is not weak, but it is one product described twelve ways. Twenty-one of thirty
candidates are area A, and the top ten contain three distinct products at most:

1. **Read the pipeline, show the scorecard** (2 + 5 + 18, with 14 and 24 as later tabs).
2. **Paste into a ledger, mark decisions** (1, with 14 as a column; 6, 13, 7 are variants).
3. **Type two lines in a text file, run a command** (28; 21 is the server-bound version).

Candidate 3 is a fourth product (fill-based journal) that is the most sellable and the
least likely to be opened. The finalists in `ideas/finalists.md` are the first three,
with the merges stated explicitly.
