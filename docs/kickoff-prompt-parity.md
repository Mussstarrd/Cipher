# Parity — Claude Code Kickoff Prompt

Paste everything below the line into a fresh Claude Code session opened in an empty repository.

---

You are building **Parity**, a toolkit I will use to run a fixed-price service: migrating small businesses' Microsoft Access applications to hosted web applications, and proving the new system returns the same answers as the old one. Read this whole brief before writing code.

## The business this supports

Many small businesses run on an Access database (an `.accdb` or `.mdb` file with tables, saved queries, forms, reports and VBA code) that one person built years ago. They want it on the web, multi-user, backed up, without the original author. I sell that migration at a fixed price.

Three things make or break the service, and the toolkit exists to deliver them:

1. **A fast, accurate assessment** from the client's file, so I can quote a fixed price without losing money.
2. **A migration that is mostly generated**, onto one boring target stack I can host and maintain for many clients.
3. **Proof of equivalence**: a report the client can read showing that every table and every saved query produces the same results in the new system as in the old.

Excel and VB6 sources come later. Build for Access only now, but keep the source-reading layer behind an interface.

I am technical (networking, security, scripting) but not a professional developer. Prefer conventional choices I can maintain alone. Put explanations of non-obvious decisions in the README.

## Hard rules

1. **Client data stays on my machine.** Row data is never sent to a model. Models see schema, query SQL, VBA source, form and report definitions, and synthetic or masked sample rows only. Enforce this in one module with tests, not by convention.
2. **The model never decides that a migration is correct.** Correctness is decided by the parity harness, which is deterministic code. Model output is always a draft that the harness or I accept.
3. **Anything not understood is flagged, never guessed.** Unsupported Access functions, VBA with side effects, linked external tables, macros: list them in the assessment as manual-review items with file and line.
4. **Never modify the client's source file.** Open read-only, work on a copy, record its SHA-256 in every report.
5. **No silent data coercion.** Every type conversion, truncation, null-handling or encoding decision is logged in the migration report.
6. If something in this brief turns out to be wrong or infeasible, stop and tell me rather than working around it quietly.

## Stack

- **Python 3.12+** for the toolkit, packaged as a CLI named `parity` (Typer).
- **Reading Access files:** `mdbtools` and/or the `access-parser` Python library for tables, data and saved-query SQL on Linux. Forms, reports and VBA modules are not reliably readable without Access itself, so also write a small **Windows extraction script** (PowerShell driving Access through COM, using `SaveAsText`) that I or the client runs once to export those objects into a folder the toolkit ingests. Check the current capabilities of these libraries before committing to a design; do not rely on memory.
- **SQL translation:** `sqlglot` where it helps, plus a hand-written layer for Access-specific syntax and functions (`IIf`, `Nz`, `DateDiff`, `Format`, `*` wildcards, `#date#` literals, parameter queries, crosstabs).
- **Target stack for generated apps:** **PostgreSQL + Django**, using the Django admin for basic record editing and generated views and templates (HTMX, no JavaScript framework) for the forms that matter. Saved queries become database views or Django querysets. Reports become HTML with a print stylesheet and PDF export.
- **Model access:** Anthropic Python SDK, model IDs from environment variables, never hard-coded. Read the current SDK documentation before writing the client.
- **Tests:** pytest. **Lint and format:** ruff.
- **Deployment of generated apps:** Docker Compose, one small VPS per client or one shared host with a database per client. Decide after the MVP.

## Repository layout

```
parity/
  parity/
    cli.py
    source/
      base.py             # source-reader interface
      access.py           # tables, data, saved queries via mdbtools / access-parser
      access_export.py    # ingests the Windows SaveAsText export (forms, reports, VBA)
    inventory.py          # builds the full object inventory and dependency graph
    assess/
      complexity.py       # scoring rules -> effort estimate
      report.py           # client-facing assessment (HTML and PDF)
    privacy.py            # the only place data leaves the process; masking and checks
    translate/
      schema.py           # Access types -> PostgreSQL DDL, keys, indexes, relationships
      sql.py              # Access SQL -> PostgreSQL
      functions.py        # Access function equivalents
      vba.py              # model-assisted VBA -> spec -> Python, always flagged for review
    generate/
      django_project.py   # scaffolds the target project from the inventory
      forms.py            # form definitions -> views and templates
      reports.py
    harness/
      snapshot.py         # exports every table and query result from the source
      compare.py          # row counts, per-column checksums, ordered and unordered diffs
      report.py           # the parity report
    llm.py
  windows/
    Export-AccessObjects.ps1
  tests/
    fixtures/northwind/   # public sample database
  docs/
  README.md
  CLAUDE.md
```

## Commands the CLI must provide

- `parity inventory <file>`: every table, relationship, query, form, report, macro and module, with dependencies, as JSON and a readable summary.
- `parity assess <file>`: complexity score, effort estimate in days with the assumptions shown, the list of manual-review items, and a client-ready report. Must work in a schema-only mode that reads no row data.
- `parity migrate-schema` and `parity migrate-data`: create the PostgreSQL schema and load the data, with the coercion log.
- `parity translate-queries`: translate each saved query, mark each as translated, translated with warnings, or needs manual work.
- `parity generate-app`: produce the Django project.
- `parity verify`: run the harness and write the parity report.

## The parity harness

This is the core of the product. For each table: row count, per-column null count, per-column checksum over a canonical text form, and primary-key set comparison. For each saved select query: run it against the source, run the translation against PostgreSQL, compare as unordered sets unless the query has an ORDER BY, and report the first differing rows. Define canonical forms for dates, floats (stated tolerance), currency, booleans, nulls versus empty strings, and text encoding, and document them. Action queries (update, delete, append) are compared by running both against copies and diffing the resulting tables.

The parity report states, per object: pass, fail with the differing rows, or not comparable with the reason. A summary line gives counts of each. Never round a failure up to a pass.

## Assessment and quoting

The complexity score is transparent arithmetic, not a model's opinion: counts of tables, relationships, queries by type, forms and controls, reports, lines of VBA, external links, and manual-review items, each with a weight in a YAML file I can tune after real jobs. The model's role in assessment is limited to writing a plain-language description of what each form and module appears to do, labeled as a draft.

## Build phases

**Phase 0 — Scaffold.** Repository, CLI skeleton, CI commands, `CLAUDE.md`, and the Northwind fixture. Confirm what the chosen libraries can and cannot read from it and write that down.

**Phase 1 — Inventory and assessment.** Full inventory, dependency graph, complexity score, assessment report.

**Phase 2 — Schema and data.** Type mapping, keys, relationships, data load, coercion log, table-level parity.

**Phase 3 — Queries.** SQL translation with the function library, query-level parity.

**Phase 4 — App generation.** Django project with admin for all tables, generated views for forms, generated reports.

**Phase 5 — VBA.** Windows export script, model-assisted spec and translation, all output flagged for review.

**After MVP, ask first:** Excel and VB6 sources, hosting automation, a client-facing upload page for assessments.

## MVP acceptance criteria

Measured on the public Northwind sample database:

1. `parity inventory` lists every object in the file. Cross-check the counts against what Access itself reports and document any object type that cannot be read on Linux.
2. `parity assess` in schema-only mode produces a report without reading any row data. A test proves no row data reaches the model client under any command.
3. Every table migrates with matching row counts and column checksums. Every coercion is logged.
4. Every saved select query is either translated and passing parity, or listed as needing manual work with a specific reason. Report the real percentages; do not tune the harness to make failures pass.
5. The generated Django project starts with one command, shows every table in the admin with working relationships, and includes at least three generated form views and two generated reports.
6. The parity report is readable by a non-technical business owner and includes the source file's SHA-256.
7. The source file's hash is unchanged after every command.
8. `pytest` passes and `ruff` is clean.
9. The README documents an end-to-end run from a fresh clone.

## First session task list

Do these in order. Stop after step 6 and tell me what works, what is stubbed, and what surprised you.

1. Obtain a public Northwind Access sample database and add it as a fixture, with its source and license noted. If you cannot fetch one, stop and ask me to supply it.
2. Evaluate `mdbtools` and `access-parser` against it. Write `docs/reader-capabilities.md` stating exactly what each can read: tables, data types, relationships, saved-query SQL, forms, reports, VBA.
3. Scaffold the repository and write `CLAUDE.md` containing the hard rules and the run, test and lint commands.
4. Implement the source-reader interface and the Access reader for tables, relationships and saved queries.
5. Implement `parity inventory` with JSON and summary output, and tests.
6. Implement `privacy.py` with the test that fails if row data is passed to the model client.

## What I will supply when asked

- A Windows machine with Access installed, for the export script and for cross-checking results.
- Real client databases, once I have signed agreements, to tune the complexity weights.
- Decisions on hosting and pricing after the first real assessment.
