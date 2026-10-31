# 0003 — Constraints the landscape imposes on what can be built

**Date:** 2026-10-31
**Phase:** 2
**Decided by:** scout (recorded by historian)
**Status:** accepted

## Context
`research/landscape.md` surveyed five pain areas. Several findings constrain every
candidate the inventor may propose and the judge may score.

## Options considered
Not a choice between options; a record of external constraints.

## Decision
The following are treated as fixed for Phase 3 and 4:
- **No public Robinhood API.** Unofficial libraries need stored credentials and TOTP
  and break often. Robinhood does offer an on-demand CSV account-activity report. Any
  brokerage ingestion is CSV/paste first; live sync is optional and never required.
- **Gmail OAuth is hostile to a one-user local app** (testing-mode tokens expire in 7
  days; production needs restricted-scope verification). If mail is ever read, use
  IMAP with an app password, and keep it optional.
- **Employer expense tools are mandatory.** A tool in area D can only stage receipts.
- **Co-parent sharing always leaves the machine.** Only `.ics` subscription is
  account-free.
- **Licensing:** LuxAlgo trade-journal (MIT) may be studied and borrowed from with
  attribution; Mailpile and Receipt Wrangler are AGPL and must not be vendored.
- **Review sites for trade journals are affiliate-driven.** Sellability is judged from
  GitHub stars, forum tone, and community size, not review scores.

## Why
Buildability (criterion 5) and honesty: designing around a fragile unofficial API or an
OAuth flow that expires weekly would fail "runs with one command" within a week.

## Consequences
Candidates in area A must work from exports and manual entry on day one. Revisit if
Robinhood publishes a public API or the CSV format is documented officially.
