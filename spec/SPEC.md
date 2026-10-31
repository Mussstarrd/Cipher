# Pipeline Scorecard — v1 spec

Architect, 2026-10-31. This page is the contract. Builders implement it; extras go to `ideas/backlog.md`.

## 1. Name and one line
**Pipeline Scorecard.** A local page that stores the reports your trading pipeline already emails, shows today's flagged setups and what was done about them, and counts per-signal outcomes from your logged reports and imported fills. It records, organizes, and computes; it predicts nothing.

## 2. Users
The owner: runs a screener pipeline that emails him a morning briefing, a setups report, and an execution report most market days plus a weekly signal summary; trades on Robinhood; travels; wants zero daily input and one evening page that answers "is the system working". Others like him: self-directed traders with a scripted screener and a CSV-exporting broker who will not pay for a cloud journal.

## 3. Core loop
1. Reports land in `data/inbox/` (pipeline writes them, or the owner saves/pastes). On start and every 30 s the app moves each file verbatim into the vault, parses it, and records parse status. Nothing typed.
2. Owner opens `http://127.0.0.1:8459`. **Today** shows, per signal, flags **new** today vs **persisting** (also flagged within the prior 10 market days), each with a status pill from the execution report, a fill, or a mark; an "N unparsed" badge if anything failed.
3. Optional: tap **Take** / **Skip** on a flag; **Close** asks price and date.
4. **Scorecard** shows per signal, for 4-week / 12-week / all-time: flagged, taken, skipped, closed up, closed down, still open.
5. Weekly: **Import** the Robinhood account-activity CSV; confirm the detected column mapping; fills join to flags by symbol within the window and outcomes fill in.

## 4. Data model
SQLite, `data/scorecard.db` (WAL, all writes in transactions). Vault files at `data/vault/YYYY/MM/<date>_<type>_<sha8>.<ext>`. Feedback at `data/feedback.md`. Config: `SCORECARD_DATA` (default `./data`), `SCORECARD_PORT` (default 8459). Backup = copy `data/`.

- **reports** id, vault_path, sha256 UNIQUE, report_date, report_type (briefing|setups|execution|weekly|unknown), source (jsonl|text|paste), subject, parse_status (parsed|partial|unparsed), imported_at
- **flags** id, report_id→reports, date, signal_id→signals, symbol, action (flag|take|skip|open|close), price?, note?; UNIQUE(report_id, date, signal_id, symbol, action)
- **fills** id, source_file, trade_date, symbol (underlying = first token of instrument), side (buy|sell), qty, price, raw_row JSON, row_hash UNIQUE, flag_id→flags?
- **marks** id, flag_id→flags, mark (take|skip|close|ignore), price?, close_date?, marked_at
- **signals** id, name UNIQUE, aliases JSON, active
- **feedback** id, created_at, screen, text (mirrored as a timestamped line in `feedback.md`, which `improve` reads)

**Ingest (a) JSONL**, one object per line: `{"date":"2026-10-31","report_type":"setups","signal":"red_for_no_reason","symbol":"XYZ","action":"flag","price":12.34,"note":"..."}`; `price`, `note` optional; a bad line is recorded as unparsed, the rest proceed. **Ingest (b) free text** (.md/.txt/.eml/.html, pasted): date from filename, mail `Date:` header, or first date in text; type from keywords in subject/filename/first five lines; signal from alias match, else `unknown`; symbols = `$?[A-Z]{1,5}` minus a stoplist of English words and finance abbreviations (`THE, CEO, EPS, ETF, ...`, extendable in Settings). HTML is stripped to text; the original is always kept.

**Outcome rules.** A flag is *taken* by a `take` mark, a `take`/`open` action in an execution report, or a buy fill for the symbol within `join_window_days` (default 14) after the flag; *closed* by a `close` mark or a later sell fill; up/down compares close price to entry price. Flags without both prices are *still open*. Counts only, no dollars.

## 5. UI sketch
Server-rendered HTML, a few hundred lines of JS for taps and paste. Nav on every screen: Today · Scorecard · Vault · Import · Settings · **Feedback** (inline text box; saves to `data/feedback.md`; confirms "Saved"; if unwritable, says so with the path). Footer on every data screen: *"Based on your logged reports and fills. Nothing here is a prediction."* Any failure renders a one-line human message; stack traces go to `data/scorecard.log` only.

- **Today.** Date header with prev/next. Per signal: two columns, New / Persisting; row = symbol, flag price, status pill (open|taken|skipped|closed ↑|closed ↓), Take / Skip / Ignore buttons. Empty: "No reports for <date>. Drop files in `data/inbox/` or paste on Import." Error: unreadable file → banner naming it; page still renders.
- **Scorecard.** Signal filter; table of signal × window with the six counts and a plain CSS bar for closed-up share of closed. Empty: "No flags yet." Note under table: "Counts only flags with recorded entry and close as closed."
- **Vault.** Reports newest first; filter by type and parse status; open one → original text (escaped `<pre>`), status, extracted flags, **Re-parse** button. Empty: ingest instructions. Error: vault file missing → "File missing from vault; record kept."
- **Import.** Three panes: file drop; paste box with optional date/type override; CSV upload → table *our field → detected column (confidence)* with dropdowns to override, then **Confirm** → "N fills added, N duplicates skipped, N unmatched." Errors (not CSV, no date/symbol column found): message, nothing written.
- **Settings.** Data path (display), port, signals with aliases (add/rename/merge), join window, stoplist additions. Inline validation.

## 6. CLI
`python3 -m scorecard serve [--port N]` · `ingest <file|dir>` · `import-fills <csv> [--map field=column]...` · `stats [--signal X] [--window 4w|12w|all]` (prints the scorecard table) · `feedback "text"`. Exit 0 ok, 1 usage, 2 bad input (missing file, unparseable CSV), 3 data error (locked/unwritable). One human line to stderr per error.

## 7. Stack
Python 3.10+ **standard library only**: `http.server`, `sqlite3`, `json`, `csv`, `email`, `html.parser`, `unittest`. Front end plain HTML/CSS/JS served from `app/scorecard/static/`, no build, no CDN, works offline. Run: `cd app && python3 -m scorecard serve` (`./run.sh` wraps it). Test: `cd app && python3 -m unittest discover -s tests`. Why: the stdlib has run unchanged for a decade, needs nothing installed, and there is no dependency to explain in a year.

## 8. Explicitly out of scope for v1
- Live brokerage API / unofficial Robinhood libraries — fragile, needs stored credentials (0003).
- Price fetching from the internet — v1 computes only from stored rows.
- Dollar P&L — options multipliers and fees make it easy to get wrong; counts first.
- Charts beyond CSS bars — not needed to answer "is it working".
- IMAP/email reading — owner saves or forwards files instead (Gmail OAuth hostile, 0003).
- "Reason tag" per flag — first post-v1 improvement (0004, backlog).
- Options greeks / strategy analytics, multi-user, auth, cloud sync, mobile app, Docker.

## 9. Shelf-ready checklist for this app
1. **One command:** `cd app && python3 -m scorecard serve` on a machine with only Python 3.10+ creates `data/`, the DB, and prints the URL.
2. **Clean UI:** five screens plus Feedback, same nav, usable at 1024 px and a phone width, no dead controls.
3. **Errors handled, never a trace or a corrupt file:** empty inbox; binary/unreadable file; bad JSONL line; text with no date or no symbols; CSV with unknown headers, missing columns, duplicates, or wrong type; port in use; DB locked; data dir or feedback.md unwritable.
4. **Tests cover:** JSONL ingest; text ingest from `.md/.txt/.eml/.html` fixtures; the three date inferences; stoplist; sha256 dedup; CSV mapping against `tests/fixtures/robinhood_sample.csv` and a renamed-header variant; flag↔fill join; marks; scorecard arithmetic per window; one HTTP smoke test per route; every error path in item 3; CLI exit codes.
5. **README:** install (Python only), run, the one-line pipeline change with a JSONL example, saving emails as files, Robinhood CSV export steps, each screen, troubleshooting, where data lives, backup, uninstall, honesty statement.
6. **Not embarrassing:** every label passes the CLAUDE.md honesty test; written ux-reviewer and qa-checker passes.

## 10. Open risks
- **Unknown report formats.** Mitigation: verbatim vault, visible unparsed count, Re-parse, JSONL path; milestone 1 is vault + Today before any scorecard.
- **CSV column drift.** Mitigation: fuzzy mapping shown and overridable, raw row stored, fixture plus drifted-header test.
- **Symbol false positives.** Mitigation: stoplist, `$` prefix, Ignore mark, Settings additions; Today labels symbols as "detected".
- **Owner never points the pipeline at the folder.** Mitigation: paste works day one; per 0004, no inbox files after two weeks triggers re-judging B/C at the first `improve`.
- **Honesty drift.** Mitigation: counts not returns, "still open" for anything without both prices, footer on every data screen, no "win rate" or "expected" wording.
