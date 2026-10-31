# Candidates

Inventor output, 2026-10-31. Thirty ideas across three bands. Not scored, not ranked, not
culled; that is the judge's job.

Inputs: `research/interview.md` (pain areas A–E, constraints), `research/landscape.md`,
`decisions/0003-landscape-constraints.md`. Pain areas referenced by letter:

- **A** trading-research workflow has no durable home (setup → trade → outcome → scorecard)
- **B** email as a failed catch-all system of record
- **C** household and school logistics arriving by email, relayed by hand
- **D** work-travel expense preparation
- **E** music-production catalog scattered across folders and versions

Every idea below is local-first (SQLite / JSON / Markdown / CSV on the owner's machine),
needs no account, and is meant to be usable in five minutes a day from a laptop or Android
phone browser. Ideas that strain "buildable in days" or "fully offline" are flagged inline.
Anything touching trading **records, organizes, computes from the owner's own entries, and
surfaces what happened**. Nothing here predicts prices or promises returns.

---

## Conservative

### 1. Setup Ledger  [conservative]
**Pitch:** The owner opens one local page each morning and pastes the day's setups report
(the text of the email, or the CSV export of the saved screener). Each flagged ticker
becomes a row: date, ticker, the screener columns that came with it (IV, relative volume,
RSI, earnings date), and an empty "what I did" cell. In the evening he comes back, marks each
row *took / skipped / watching*, and pastes the fill details for anything taken. That is the
whole daily loop, under five minutes. After three months there are roughly sixty days of
flagged setups with a decision attached to every one, and a scorecard tab that computes, from
his own entries only, how many flagged setups he acted on, how the taken ones closed out, and
how the skipped ones looked when he next logged a price for them. Every number is labelled
"based on your entries".
**Why it might fail:** The evening paste is the friction point; if the pipeline already
emails him an execution report, re-keying it feels like homework and he stops on day nine.
**Pain areas served:** A.
**Nearest existing thing:** LuxAlgo/trade-journal (starts at the fill, not the screener
hit); the scout found no tool that ingests the screener hit itself.

### 2. Signal Scorecard  [conservative]
**Pitch:** A replacement for the weekly signal-performance summary email, except it is a
page, it persists, and it gets sharper every week. The owner points it at the folder (or
IMAP label, optional) where his pipeline's reports land, or pastes them in. The page shows,
per named signal, rolling 4-week / 12-week / all-time counts: flagged, taken, closed up,
closed down, average hold, all computed from logged rows. First 60 seconds: pick a signal,
see its curve of hit rate over time. After three months: the "is the system working" answer
the interview asks for, in one glance, with the caveat printed on the page that it reflects
his own logged entries and nothing more.
**Why it might fail:** It is only as good as the join between signal and outcome; if
outcomes are not logged consistently the scorecard confidently shows noise.
**Pain areas served:** A.
**Nearest existing thing:** The owner's own weekly email; no commercial journal reports
by signal rather than by trade tag (scout's inference).

### 3. Local Options Round-Trip Journal from Robinhood CSV  [conservative]
**Pitch:** The owner drops the Robinhood account-activity CSV into a folder, runs one
command, and gets a local web page listing closed round trips, with multi-leg options
grouped into one strategy row rather than split into legs. Columns: open date, close date,
underlying, strategy shape, net debit/credit, result, days held, tags. Tags are free text
and remembered. First 60 seconds: import, see the last month of trades grouped sanely. Three
months later: a searchable, filterable history in a single SQLite file he can back up by
copying. No account, no subscription, no sync token to expire.
**Why it might fail:** The options row layout of the Robinhood CSV is unverified
(landscape, area A); if it is ambiguous, leg grouping becomes a guessing game and the
tool is wrong in ways that are hard to spot.
**Pain areas served:** A.
**Nearest existing thing:** Tradervue / TradesViz (cloud, paid for options), rrmetrics
(Robinhood sync, options support unstated), TradeTally (options, Postgres, no Robinhood).

### 4. Skipped-Setup Follow-Up  [conservative]
**Pitch:** The narrow piece every journal omits: what happened to the ones you did not take.
Each morning the setups report is pasted; each evening the owner marks skipped rows. Then,
on each later day, the page asks for one number per open skipped ticker: the close (pasted
from the next morning briefing, which already carries prices, or typed). After five trading
days a skipped row is closed out automatically and filed. The summary tab shows, from logged
closes, how skipped setups fared versus taken ones. First 60 seconds: mark three rows
skipped. Three months: an honest picture of whether skipping was, based on his entries,
costing or saving him. *Flag:* automatic price fetching would need an external quote
source; the default stays paste-only to remain offline.
**Why it might fail:** Typing closes for skipped tickers is the kind of chore people do for
two weeks and then abandon, which leaves the comparison half-populated and misleading.
**Pain areas served:** A.
**Nearest existing thing:** Babypips "missed trades" advice; no tool found.

### 5. Report Vault  [conservative]
**Pitch:** The pipeline currently emails three reports a day to the owner's own inbox,
where they pile up and become part of the catch-all problem. Report Vault is a one-line
change in the pipeline plus a local page: every report is also written to disk as a dated
Markdown file, and the page lets him browse by calendar, search across all days, and
pin one line from any report as a note. First 60 seconds: scroll back to last Tuesday's
setups and see it in one click. After three months: ninety days of briefings, setups, and
executions in plain files, searchable offline, no longer in the inbox at all.
**Why it might fail:** It is a filing cabinet, and filing cabinets are not opened daily
unless something else drives you there; without the scorecard it may become a quiet
archive nobody reads.
**Pain areas served:** A, B (combined).
**Nearest existing thing:** None found; Gmail search is the de facto version.

### 6. Five-Minute Evening Check-In  [conservative]
**Pitch:** One page, opened at the end of each market day on the phone browser. Three
prompts, all optional: *What did the system flag today?* (paste or type tickers), *What did
you do?* (took / skipped / away), *One sentence about why.* Save. It stamps the date,
shows a streak counter, and lists the last seven entries. The point is to make the habit
cheap enough to survive travel weeks. After three months the entries feed any of the
scorecard ideas above, and the owner can read back ninety one-sentence "why"s, which the
landscape says is what journals never capture.
**Why it might fail:** A diary with no computed payoff is easy to skip; if nothing reads
these entries back in a useful form, the streak counter is the only reward and it is not
enough.
**Pain areas served:** A.
**Nearest existing thing:** Stonk Journal (manual entry, no import); Edgewonk's
manual-first journal (now cloud).

### 7. Setup Rulebook with Followed/Broken Tally  [conservative]
**Pitch:** The owner writes his screener thesis down once as a short checklist (the
conditions that make a flagged ticker a real setup, the exit rule, the size rule). Each
logged trade gets a tick per rule: followed or broken. The page shows, from his entries,
results split by "all rules followed" versus "at least one broken", per rule. First 60
seconds: write the rulebook, tick three boxes on today's trade. Three months later: he can
see which rule he breaks most and what broken-rule trades have looked like in his own
history. Borrows the "playbook" pattern from LuxAlgo (MIT, attribution).
**Why it might fail:** If the thesis changes every few weeks, the rulebook churns and the
tally never accumulates enough rows per version to say anything.
**Pain areas served:** A.
**Nearest existing thing:** LuxAlgo/trade-journal playbooks; rrmetrics strategy rules.

### 8. Read-Only Inbox Digest  [conservative]
**Pitch:** A local script connects to Gmail over IMAP with an app password (no OAuth,
nothing written to the mailbox, no third party) and produces one local page each morning:
what arrived in the last 24 hours grouped by sender category (school, bills, pipeline
reports, newsletters, everything else), with one line per message and a link to open it in
Gmail. First 60 seconds: skim the page instead of the inbox. Three months later: a daily
record of what came in, per category, searchable, and a sense of which categories are
noise. *Flag:* requires network to Gmail by design; works from cached data offline.
**Why it might fail:** Gmail's own tabs and search already do 80% of this, and the extra
local page is one more thing to look at rather than one fewer.
**Pain areas served:** B.
**Nearest existing thing:** MailDigest (MIT, IMAP digest), inbox-zero (hosted-first AI
assistant), SaneBox (paid, server-side).

### 9. School Email to Calendar File  [conservative]
**Pitch:** The owner pastes the text of a school or activity email into a local page. The
page finds dates, times, and the subject line's event name, shows them for a yes/no check,
and writes them into a local `.ics` file. The file can be imported into any calendar or
shared with the other adult in the household by copying it; nothing goes through a vendor.
First 60 seconds: paste, confirm two dates, done. Three months later: a growing local
calendar of school events and a plain-text log of every email that fed it.
**Why it might fail:** Date extraction from freeform school email is messy ("the Friday
before break"), and if it misses one event the trust is gone and relaying by hand resumes.
**Pain areas served:** C.
**Nearest existing thing:** Nori, FamilyHero, Plancove (all cloud, account-based, forward-
to-vendor).

### 10. Trip Receipt Stager  [conservative]
**Pitch:** A local folder per trip and a page that watches it. The owner saves or
photographs receipts into the trip folder during the week (from the phone browser via an
upload box when on the same network, or by dropping files later). The page lists each
receipt, lets him type amount, vendor, and category in one row, flags days with no
lodging or meal receipt, and on request produces a single zip plus a CSV summary ready to
feed the employer's mandatory expense tool. First 60 seconds: start a trip, drop two
receipts. Three months later: every past trip is a closed folder with a summary, and
prep for the next report takes minutes instead of an evening.
**Why it might fail:** The employer tool is mandatory and has its own receipt capture; a
staging step only pays off if the owner's current prep is genuinely painful rather than
merely annoying.
**Pain areas served:** D.
**Nearest existing thing:** Expensify free tier (SmartScan), Paperless-ngx, Receipt
Wrangler (AGPL, do not vendor).

### 11. Beat Catalog  [conservative]
**Pitch:** Point it at the folders where beats live. It indexes every bounce and project
file, groups versions by shared name stem ("fire_beat_34" and all its `_v2`, `_final`,
`_final_final` siblings), shows the newest bounce with an in-browser player, and lets the
owner add BPM, key, mood, and a one-line status (idea / working / done / sent) per beat.
First 60 seconds: see every beat in one list with how many versions each has. Three months
later: a browsable catalog of his own WIPs, with a lineage view per beat, stored as one
JSON index beside the files. *Flag:* automatic BPM/key detection would strain "days";
v1 is filename and metadata only.
**Why it might fail:** Episodic use; if he produces in bursts, the index goes stale
between sessions and the first act every time is a re-scan.
**Pain areas served:** E.
**Nearest existing thing:** Sononym / ADSR (samples, not WIPs), Navidrome / beets
(finished releases), Splice Studio (dead).

### 12. Sender Triage to Gmail Filters  [conservative]
**Pitch:** Over IMAP with an app password, read headers only for the last 90 days and
show one row per sender: message count, how many were ever opened (from the `\Seen`
flag), last date. The owner clicks *keep / archive-on-arrival / label* per sender. The tool
writes a Gmail filter export file (the XML format Gmail itself imports) which he uploads
once in Gmail settings. First 60 seconds: see the top 20 senders by volume, decide on
them. Three months later: a repeatable triage he re-runs quarterly, with a local record of
every decision. Nothing is written to the mailbox by the tool itself.
**Why it might fail:** It is a one-off clean-up disguised as a product; after the first
pass the open frequency drops to quarterly, and Gmail's own "unsubscribe" suggestions
keep improving.
**Pain areas served:** B.
**Nearest existing thing:** Clean Email, Unroll.me (both cloud, both with data-handling
history); Gmail's native filter import.

---

## Adjacent

### 13. Thesis Lab Notebook  [adjacent]
**Pitch:** Borrowed from how a lab tracks experiments rather than how a broker tracks
fills. The unit of record is a *hypothesis* ("large-caps down 3%+ with heavy options
volume tend to recover within N days", written in the owner's words) with a dated
version. Each day's flagged set is logged against the hypothesis version that produced
it. Outcomes are logged as they close. The page shows each hypothesis version with its
sample size and result distribution so far, and warns when the sample is too small to
read. First 60 seconds: write hypothesis v1, attach today's flags. Three months later: a
record of what he believed, when he changed it, and what each version saw, computed only
from his entries.
**Why it might fail:** It asks him to be disciplined about versioning the thesis, which
is more structure than a five-minute habit tolerates.
**Pain areas served:** A.
**Nearest existing thing:** None found; Traders Second Brain's blog describes the idea in
prose only.

### 14. Why-Is-It-Down Tagger  [adjacent]
**Pitch:** Journals record what, not why (landscape). This page adds one ten-second step
to each flagged ticker: pick a reason tag from a short list he defines (earnings, sector
move, news, index drag, no reason found, other). That is all it adds. The scorecard then
splits results by reason tag, so after three months the owner can see, from his own
logged outcomes, whether "no reason found" rows have behaved differently from "news"
rows in his history. First 60 seconds: tag five tickers.
**Why it might fail:** Reason tags are subjective and applied in a hurry; a scorecard
built on fuzzy tags may look like a finding and be nothing.
**Pain areas served:** A.
**Nearest existing thing:** Trade tags in every journal, but none are about the setup's
cause and none compute by tag over a screener feed.

### 15. Away-Day Aware Journal  [adjacent]
**Pitch:** The owner travels several days a month and cannot act on those days. This
journal has a calendar where travel days are marked (typed in, or imported from the
trip folders of idea 10). Every flagged setup is tagged *could act* or *away*. The
scorecard shows two columns side by side: how the system's flags looked on days he was
present versus days he was away, from logged closes. The question it answers is "does
missing travel days matter, based on what I've logged", not "what would I have made".
First 60 seconds: mark next week's trip. Three months later: a clear picture of how
much of the system's activity happens while he is unavailable.
**Why it might fail:** The away-day split is a single insight; once seen, the extra
feature carries no daily weight and the journal underneath has to stand on its own.
**Pain areas served:** A, D (combined).
**Nearest existing thing:** None found.

### 16. Household Week Sheet  [adjacent]
**Pitch:** Takes the school-email parsing of idea 9 and the inbox digest of idea 8 and
produces one thing: a single-page "this week in the house" view every Sunday, listing
dated items pulled from household emails (school, activities, appointments), each with the
sender and a yes/no "told the other adult" checkbox. It also writes the `.ics`. First 60
seconds: skim the week, tick what has been relayed. Three months later: a running record
of what came in and what was passed on, so "did anyone tell you about Thursday" has an
answer.
**Why it might fail:** The other adult may not want another artifact; if they live in a
shared Google calendar already, the `.ics` is friction and the sheet is a duplicate.
**Pain areas served:** C, B (combined).
**Nearest existing thing:** Cozi (shared calendar, ads), FamilyWall; none local.

### 17. Pipeline Heartbeat  [adjacent]
**Pitch:** "Is the system working" read literally. A tiny local monitor that knows the
owner's pipeline should produce a morning briefing, a setups report, and an execution
report on each market day and a weekly summary on the weekend. It checks (by watching the
output folder, or optionally the inbox over IMAP) whether each arrived, when, and whether it
was empty or suspiciously short. A page shows a green/amber/red grid by day. First 60
seconds: see this week's grid. Three months later: a run history of the pipeline itself,
with gaps and failures logged, so a silent break is noticed the same day instead of
weeks later.
**Why it might fail:** The pipeline may already be reliable, in which case this is a
dashboard of green squares that nobody needs to open.
**Pain areas served:** A, B (combined).
**Nearest existing thing:** Generic uptime monitors (Healthchecks.io-style, cloud); none
for personal report pipelines.

### 18. Setups Diff  [adjacent]
**Pitch:** Paste today's setups report; the page compares it to yesterday's and every
prior day. Each ticker is shown as *new today*, *persisting (day N)*, or *dropped off*.
The owner can see at a glance which names keep showing up and how long they have been
flagged. After three months there is a persistence history per ticker and a tally, from
logged outcomes, of how first-day flags versus repeat flags have fared in his record.
First 60 seconds: paste, see three new names and two repeats.
**Why it might fail:** Persistence may turn out not to matter to his thesis, and then the
diff is a curiosity rather than a reason to open the page.
**Pain areas served:** A.
**Nearest existing thing:** None found; screeners show today only.

### 19. Trip Receipt Harvester  [adjacent]
**Pitch:** Combines idea 10 with read-only IMAP. Given a trip's start and end dates, it
searches the inbox for messages in that window from known receipt senders (airlines,
hotels, rideshares, restaurants; the list is editable), saves each as PDF or HTML into the
trip folder, and pre-fills vendor and date in the stager. The owner confirms amounts.
First 60 seconds: enter trip dates, watch the folder fill. Three months later: each trip's
receipts are gathered in minutes, offline-readable, and the forwarded-receipt-vanishes
problem in the employer tool is sidestepped because he uploads files directly.
**Why it might fail:** Receipt emails vary wildly; amount extraction will be wrong often
enough that he checks every one, which is most of the original work.
**Pain areas served:** D, B (combined).
**Nearest existing thing:** Concur/Expensify email-in (fragile per their forums); Receipt
Wrangler (AGPL).

### 20. Beat Changelog  [adjacent]
**Pitch:** Instead of indexing files after the fact (idea 11), this watches the beat folders
and, whenever a new version of a project or bounce appears, asks one question in a small
local page: "What changed?" One line, optional. The catalog then shows each beat as a story:
v1 "first sketch", v4 "swapped drums", v7 "sent to M." First 60 seconds: save a bounce,
type five words. Three months later: a changelog per beat that explains which of the
ten `_final` files is actually the one, written at the time instead of guessed later.
**Why it might fail:** Interrupting a creative session to write a line is the one thing a
producer in flow will not do, and a changelog with gaps is less trustworthy than none.
**Pain areas served:** E.
**Nearest existing thing:** Splice Studio (dead), git for DAW projects (hostile to binary
files).

### 21. Decision Inbox  [adjacent]
**Pitch:** A single text box on a local page the owner can reach from the phone browser
during the day. He types whatever he decided or noticed ("skipped XYZ, earnings in two
days", "took ABC half size"). Each entry is timestamped and scanned for ticker symbols,
which become links. In the evening, the entries are shown beside that day's flagged setups
so each decision lands on the row it belongs to with one tap. First 60 seconds: type one
sentence. Three months later: the "why" layer the landscape says is missing, captured in
the moment rather than reconstructed.
**Why it might fail:** Reaching a local page from the phone while out requires the laptop
to be on and reachable (or a sync step), and if that is ever fiddly the habit dies.
*Flag:* phone access away from home strains local-first; v1 may be laptop-only with the
phone as a nice-to-have.
**Pain areas served:** A.
**Nearest existing thing:** Notes apps; no journal joins free-text notes to screener rows.

### 22. The Evening Page  [adjacent]
**Pitch:** One local page, opened once each evening, with three short panels: today's
trading check-in (ideas 1/6), this week's household dates still unrelayed (idea 16), and
open receipts for the current trip (idea 10). Each panel is two or three lines and one
action. First 60 seconds: scan all three, tick what is done. Three months later: a single
habit covers three recurring pains, and each panel's history is kept in its own plain
file. *Flag:* three panels in days is tight; v1 would ship with one panel live and two
stubbed, which is risky for shelf-readiness.
**Why it might fail:** A page that does three things does none of them deeply, and the
trading panel alone would be the reason to open it, making the other two dead weight.
**Pain areas served:** A, C, D (combined).
**Nearest existing thing:** Personal dashboards (Homepage, Dashy on r/selfhosted); none
with these panels.

### 23. Never-Read Senders  [adjacent]
**Pitch:** A narrower, repeated version of idea 12. Weekly, over read-only IMAP, it
computes one list: senders whose messages he has not opened in 60 days, with counts. The
page shows the list and a one-click "I've handled this" per sender that just records the
decision locally. First 60 seconds: see twelve senders he never reads. Three months later:
a shrinking list and a log of what he muted and when, with no third party ever holding
his mail.
**Why it might fail:** The useful action (unsubscribe, filter) still happens in Gmail;
the tool is a report that points at work elsewhere.
**Pain areas served:** B.
**Nearest existing thing:** Gmail's unsubscribe nudges; Clean Email.

### 24. Weekly Review Composer  [adjacent]
**Pitch:** On Sunday the owner opens one page. It pre-fills from the week's logged setups,
decisions, and outcomes: counts per signal, rules followed/broken, the three "why" notes he
wrote, and a blank line labelled "one thing I'd change". He edits for two minutes and
saves; the result is a dated Markdown file in a reviews folder, and the page shows the last
twelve reviews stacked. First 60 seconds: read the pre-filled summary. Three months later:
twelve honest Sunday notes that show whether the system, and the operator, are improving,
in his own words.
**Why it might fail:** It depends on the daily logging ideas feeding it; without them the
pre-fill is empty and it becomes a blank diary.
**Pain areas served:** A.
**Nearest existing thing:** Edgewonk's review prompts (cloud); the owner's weekly email.

---

## Weird

### 25. The No-Trade Journal  [weird]
**Pitch:** A journal that refuses to record trades. It only records the setups the owner
looked at and did not take, with the reason, and later the logged close. The inversion:
his existing pipeline and broker already know what he did; nobody knows what he declined
and why. After three months the page shows, from his entries, the population of declined
setups and how they closed, next to a one-line reminder that this is a record of
discipline, not a forecast. First 60 seconds: log two "no"s.
**Why it might fail:** Logging only the negatives feels like keeping a list of regrets,
and that is emotionally expensive enough that people stop.
**Pain areas served:** A.
**Nearest existing thing:** None found.

### 26. Shadow Book  [weird]
**Pitch:** Every flagged setup is "taken" by a mechanical shadow: a fixed, written rule
(enter at the logged price, exit after N days or at a logged level). The owner never
trades the shadow; he just logs closes. The page shows two books side by side, his actual
logged trades and the mechanical shadow, each computed strictly from prices he entered or
pasted. The question is not "which is better" but "how far did I drift from the rule this
week". *Flag:* needs daily closes for every flagged ticker, so either a paste routine from
the morning briefing or an optional quote source; the offline default is paste.
**Why it might fail:** Any side-by-side invites the owner to read it as a prediction of
what he should have done, which is exactly the honesty line this studio refuses to cross,
and the labelling has to work very hard.
**Pain areas served:** A.
**Nearest existing thing:** Backtesting features in TraderSync (cloud, "bloated" per
reviewers); none paste-driven.

### 27. One Look a Day  [weird]
**Pitch:** The version that deletes the task. The pipeline's reports are written to a
folder, and this page is the only way the owner reads them: it reveals the day's reports
once, at a time he chooses, and otherwise shows a plain screen with a count of how many
times he came back to look. The count is logged. After three months he has a record of
how often he checked and whether checking more often coincided with anything in his logged
decisions. First 60 seconds: set the reveal time to 6 p.m.
**Why it might fail:** He can open the email instead. A self-imposed lockout that is
trivially bypassed is theatre.
**Pain areas served:** A, B.
**Nearest existing thing:** Focus/blocker apps; nothing report-specific.

### 28. The Markdown Journal  [weird]
**Pitch:** The "useless" version: no web page at all. The journal is one Markdown file
per day in a folder, written in any editor on any device that syncs files, following a
six-line convention (`- TICKER took|skipped reason` and `- TICKER closed PRICE`). A single
CLI command reads the folder and prints the scorecard, by signal, by rule, by week, and
writes an HTML page beside it. First 60 seconds: type two lines in a text file. Three
months later: ninety plain files that will outlive every app, plus the same scorecard
everything else promises. Trivially local, trivially backed up.
**Why it might fail:** Free-text conventions drift, the parser rejects a line, and the
owner finds out in week six that half of March never counted.
**Pain areas served:** A.
**Nearest existing thing:** Spreadsheet journals (r/RealDayTrading); plain-text
accounting (ledger-cli) as a pattern.

### 29. Beats as Setups  [weird]
**Pitch:** Point the setup → decision → outcome ledger at beats instead of tickers. Each
beat version is a "setup", sharing it with someone is "taking it", the feedback or
silence that follows is the "outcome", all logged by hand. The same scorecard code shows
which kinds of beats he finishes, which he sends, and what came back, from his entries.
Role inversion: one ledger engine, two very different hobbies, and the music one has
no stakes, which is where the habit might actually form.
**Why it might fail:** Treating creative work as a pipeline can kill the fun of it, and
the owner may simply not want a scorecard for music.
**Pain areas served:** E, A (combined, shared engine).
**Nearest existing thing:** BeatStars/SendBeatsTo sales dashboards (cloud, for sellers).

### 30. Plain-Language Weekly for the Household  [weird]
**Pitch:** A different reader. Each Sunday the tool writes one paragraph in plain words,
no tickers or jargon, from the week's logged entries: "The system flagged 14 names this
week. You acted on 3 and followed your own rules on 2 of them. You were travelling 2 days.
Based on your entries, this month's taken setups closed up 5 times and down 4." It is meant
to be read aloud to, or by, another adult in the house, so the hobby is legible and
accountable to the people who share the time it costs. First 60 seconds: read the
paragraph. Three months later: twelve paragraphs anyone could follow.
**Why it might fail:** The owner may not want the household reading his trading record at
all; it is a sensitivity the interview could not probe because it was delegated.
**Pain areas served:** A, C (combined).
**Nearest existing thing:** Kinfo (social, verified performance, cloud); nothing private
or plain-language.

---

## Coverage check

| Area | Candidates |
|---|---|
| A | 1, 2, 3, 4, 5, 6, 7, 13, 14, 15, 17, 18, 21, 22, 24, 25, 26, 27, 28, 29, 30 |
| B | 5, 8, 12, 16, 17, 19, 23, 27 |
| C | 9, 16, 22, 30 |
| D | 10, 15, 19, 22 |
| E | 11, 20, 29 |

Combined-area ideas: 5 (A+B), 15 (A+D), 16 (C+B), 17 (A+B), 19 (D+B), 22 (A+C+D),
27 (A+B), 29 (E+A), 30 (A+C).

Flagged as straining "days" or "offline": 4 (quotes optional), 8/12/17/19/23 (need Gmail
IMAP by design; read-only, app password, no OAuth), 11 (no audio analysis in v1),
21 (phone reach when away), 22 (three panels), 26 (daily closes).
