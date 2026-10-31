# Finalists

Judge output, 2026-10-31. Three finalists drawn from `ideas/scores.md`. Two of them are
merges of candidates that overlapped so heavily that presenting them separately would have
been dishonest; the merges are stated and the merged thing is what is scored.

**Why these three and not simply ranks 1–3.** Ranks 1, 3, and 4 (Signal Scorecard, Setups
Diff, Report Vault) are one product: a store for the pipeline's reports, a day view over
them, and a scorecard computed from them. Ranks 2 and 8 (Setup Ledger, Why-Is-It-Down
Tagger) differ by one column, and that column is the owner's thesis. Rank 5 (Markdown
Journal) is a genuinely different answer to the same question, so it stands alone. The
three finalists are therefore three different ways to feed the same scorecard: automatic,
web form, or text file. That is the real decision.

## Side by side

| | A. Pipeline Scorecard | B. Setup Ledger | C. Markdown Journal |
|---|---|---|---|
| Merged from | 2 Signal Scorecard + 5 Report Vault + 18 Setups Diff | 1 Setup Ledger + 14 Why-Is-It-Down Tagger | 28, unchanged |
| Input on a normal day | None (pipeline writes files; page reads them) | Two pastes and a few taps | Two typed lines in a text file |
| Pain removed | 5 | 4 | 3 |
| Open frequency | 4 | 3 | 3 |
| Compounding | 5 | 5 | 4 |
| Sellability | 3 | 3 | 3 |
| Buildability | 3 | 4 | 4 |
| Delight | 4 | 3 | 3 |
| **Total** | **24** | **22** | **20** |
| Six-month verdict | **Yes**, if ingest is automatic | **Doubtful** as written; Probably with skipped-by-default | **Probably** |
| Works from a phone while travelling | Read-only, yes (if the page is reachable); nothing to enter | Only if the laptop is on and reachable | Yes, via any synced editor |
| Survives a busy week | Catches up by itself | Backlog of unmarked rows | Missing days are just missing |

## A. Pipeline Scorecard (2 + 5 + 18)

**What it is.** A one-line change makes the owner's pipeline write each morning briefing,
setups report, execution report, and weekly summary to a dated folder; a local page parses
them into SQLite and shows, for today, which flagged names are new or persisting and what
the execution report says was done, and, per named signal, rolling 4-week / 12-week /
all-time counts of flagged, taken, closed up, closed down, all labelled "based on your
logged reports". Nothing is typed on a normal day; the owner opens the page instead of
the email.

**Strongest argument for.** It is the only candidate that an owner who dislikes input
will still be using in six months, because it costs nothing to feed and it answers the
interview's exact definition of life-changing: one page each evening, is the system
working, sharper every week. It also removes the three daily reports from the inbox,
which is the one area-B fix that needs no IMAP.

**Strongest argument against.** It depends on parsing report formats the studio has not
seen, and the outcome join for flagged-but-not-taken names needs a later price that the
reports may not carry. If they do not, the v1 scorecard is "how did the trades I took on
this signal do", which is narrower than the pitch and must be labelled as such.

**Key tradeoff versus B and C.** It trades the owner's judgement for his pipeline's: B
and C capture a reason and a decision in his own words; A captures only what the reports
contain. The "why" layer that the landscape says journals never record is absent in v1
and would have to be added as an optional tag later (candidate 14 is the obvious one).

**Buildability risk.** Medium. The architect needs three or four sample reports before
writing a spec; without them the parser is guesswork. Mitigation: the ingest is a plugin
per report type with a loud "could not parse" state per file, and the first milestone is
"files land, page lists them" (Report Vault alone) before any scorecard.

## B. Setup Ledger with reason tag (1 + 14)

**What it is.** Each morning the owner pastes the setups report into a local page and
each flagged ticker becomes a row; each evening he marks each row took / skipped /
watching, optionally picks one reason tag (earnings, sector, news, no reason found,
other), and pastes fill details for anything taken. A scorecard tab computes, from his
entries only, how flagged setups fared by decision and by reason tag.

**Strongest argument for.** It needs no knowledge of the pipeline's formats and no
pipeline change: the owner is the parser, which makes it the lowest-risk build and the
only finalist whose spec could be written today. The reason tag turns a journal into a
test of the actual thesis, because "no reason found" is the thesis.

**Strongest argument against.** It re-keys what the execution report already says, twice
a day, for an owner who declined an interview because it was too much input. The six-
month test says Doubtful, and every candidate in the pile that depended on manual entry
scored 3 or lower on open frequency.

**Key tradeoff versus A and C.** It buys build certainty and the "why" layer at the price
of a daily chore. Against A it is slower to feed and faster to build; against C it has a
real UI and a tag picker but requires the laptop to be awake and reachable, which it is
not from a hotel. The honest mutation is to keep the morning paste and replace the evening
mark with reading the execution report, which turns B into A's front end.

**Buildability risk.** Low. Paste parser for one report format, SQLite, a table, a
scorecard tab. The one hard rule: unmarked rows must default to "skipped" after N days so a
travel week does not leave a nagging backlog.

## C. The Markdown Journal (28)

**What it is.** No web app. One Markdown file per day in a synced folder, written in any
editor on any device, following a six-line convention (`- TICKER took|skipped reason`,
`- TICKER closed PRICE`). One command parses the folder, prints the scorecard by signal,
by week, and by reason, and writes an HTML page beside it.

**Strongest argument for.** It is the only manual path that works from the phone while
travelling without a server, and the files will outlive every app. For a code-comfortable
owner it is the cheapest possible gesture per day and it degrades gracefully: missing days
are just missing, nothing nags, nothing corrupts.

**Strongest argument against.** It is still manual entry every day from someone who
dislikes input, and the convention-drift failure is quiet: a typo in week six drops rows
until someone looks. The "clean UI" bar is met only by the generated page; entry itself has
no UI, no validation at the moment of typing, and no tag picker.

**Key tradeoff versus A and B.** It trades every convenience for durability and reach.
Against A it captures the "why" and works from anywhere, but asks for daily typing A does
not. Against B it drops the server dependency and the form, and picks up a parser that
must be loud about every rejected line.

**Buildability risk.** Low to medium. The grammar and parser are a day; the discipline is
in error reporting: every unparsed line must be listed by file and line number on the
generated page, or the week-six failure is guaranteed.

## Recommendation

Build **A, the Pipeline Scorecard**, staged so that the first milestone is the Report
Vault alone (pipeline writes files, page lists and searches them) and the scorecard is the
second. It is the only finalist whose six-month verdict is Yes, and the reason is
structural, not clever: the owner already produces the data every market day, so a tool
that reads it costs him nothing to keep alive, while B and C both ask for a daily habit he
has already told the studio he does not want. The honest caveat is that A's v1 may score
only taken trades if the reports carry no later prices for untaken flags; that is still a
per-signal answer to "is the system working" and it is more than the weekly email gives
him today. B's reason tag should be the first improvement after v1 ships, as one optional
field on a row the page already shows, which gets the "why" layer without the re-keying.
C is the fallback if the pipeline's reports turn out to be unparseable or the owner would
rather not touch the pipeline at all.

**The one question the owner should ask himself:** *Am I willing to add one line to my
pipeline so it writes its reports to a folder?* If yes, A is the clear pick and the studio
needs three or four sample reports to write the spec. If no, the choice is between B and C,
and it comes down to whether he will type into a form on the laptop or into a text file on
whatever device is in his hand.

---

**Pick one, or ask the inventor to mutate one.** The mutations most worth asking for, if
none of the three is right: A with an optional IMAP ingest instead of a folder (candidate 8's
mechanism), B with the evening step replaced by reading the execution report, or candidate
3 (Robinhood CSV round-trip journal) if the owner cares more about something strangers
could use than about something he opens daily.
