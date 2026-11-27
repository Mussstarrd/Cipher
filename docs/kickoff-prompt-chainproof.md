# ChainProof — Claude Code Kickoff Prompt

Paste everything below the line into a fresh Claude Code session opened in an empty repository.

---

You are building **ChainProof**, a chain-of-custody and certificate-of-sanitization system for a small data-destruction company. Read this whole brief before writing code.

## Who this is for and why it exists

I own a small secure data-destruction company with defense-contractor and healthcare clients. We collect storage media (hard drives, SSDs, NVMe, tapes, phones), then wipe or physically destroy it. Clients need proof: which exact devices, what was done to each, by whom, when, and verified how. Today that proof is manual typing of serial numbers into document templates. It is slow, and a mistyped serial on a certificate is a liability for us and for the client.

ChainProof replaces that with: photograph the device label on a phone, confirm the extracted details, log every custody event, attach the erasure or destruction record, and issue a signed certificate the client can verify online.

The first deployment is single-tenant, for my company only. Design so multi-tenancy can be added later, but do not build it now.

I am technical (networking, security, scripting) but not a professional software developer. Prefer boring, well-documented choices I can maintain alone. Explain non-obvious decisions briefly in the README, not in long code comments.

## Hard rules

1. **The model never writes a certificate field.** Claude is used to extract and to reconcile, and a human confirms. Every value on a certificate comes from a database record that a named user confirmed.
2. **No serial number is accepted on model output alone.** A serial is confirmed when either (a) a decoded barcode on the label matches the model-extracted text, or (b) a user explicitly confirms it on screen. Mismatches go to a review queue.
3. **Custody events are append-only.** No updates, no deletes. Corrections are new events that reference the one they correct.
4. **We store device metadata only.** Never contents of any media, never patient or controlled information. If an uploaded photo shows anything other than a device label, the user can discard it; do not try to interpret it.
5. **Do not reproduce paid standards.** NIST SP 800-88 Rev. 2 is public. IEEE 2883-2022 is a paid standard: do not paste its text or tables. The sanitization rule matrix ships as an editable YAML file with placeholder rows that I will fill in and sign off from my own licensed copy.
6. **No compliance claims in the UI or documents.** Use "recorded in accordance with the company's media sanitization procedure", never "NIST compliant" or "CMMC compliant".
7. If you are unsure about a requirement in a standard, say so and leave a clearly marked TODO for me. Do not invent regulatory wording.

## Stack

- Python 3.12+, **FastAPI**, server-rendered **Jinja2 templates with HTMX**. No SPA framework, no Node build step.
- **SQLite** via SQLAlchemy 2.x and Alembic migrations. Keep queries portable to Postgres.
- **WeasyPrint** for PDF certificates from HTML templates.
- **Anthropic Python SDK** for label extraction and inventory reconciliation. Model IDs come from environment variables (`CHAINPROOF_EXTRACT_MODEL`, `CHAINPROOF_RECONCILE_MODEL`), never hard-coded. Check the current SDK documentation for image input and structured output before writing the client; do not rely on memory for the API shape.
- **pyzbar** (or zxing-cpp) for barcode and QR decoding of label photos.
- **cryptography** (Ed25519) for certificate signing; **qrcode** for the verification QR.
- Auth: session cookies, argon2 password hashes, three roles (admin, technician, client-viewer). TOTP for admin is post-MVP.
- **pytest** with fixtures; **ruff** for lint and format.
- Docker Compose for deployment to a single small VPS behind Caddy. Nightly SQLite backup script.

The capture screen must work well on a phone browser: large touch targets, `<input type="file" accept="image/*" capture="environment">`, works on a slow connection.

## Repository layout

```
chainproof/
  app/
    main.py                 # FastAPI app factory, routes mounted here
    config.py               # settings from environment
    db.py                   # engine, session
    models/                 # SQLAlchemy models
    routes/                 # jobs, assets, capture, custody, certificates, verify, clients, auth
    services/
      extraction.py         # label photo -> structured fields (Claude + barcode cross-check)
      reconciliation.py     # client inventory file vs. captured assets
      custody.py            # append-only event log + hash chain
      rules.py              # loads sanitization rule matrix, proposes method per media type
      erasure_import.py     # parsers for erasure-tool logs
      certificates.py       # render, sign, store
      evidence_pack.py      # per-job zip: certificates, custody log, reconciliation report
    templates/              # Jinja2 (base, capture, job, review queue, certificate, verify)
    static/
  rules/
    sanitization_matrix.yaml   # media type -> allowed methods, verification required
    control_mappings.yaml      # which records support which client obligations
  migrations/
  tests/
    fixtures/labels/        # sample label photos (I will supply real ones)
    fixtures/inventories/   # sample messy client spreadsheets
    fixtures/erasure_logs/
  scripts/                  # backup, key generation, seed data
  docker-compose.yml
  Caddyfile
  README.md
  CLAUDE.md                 # the hard rules above, plus run/test commands
```

## Data model (minimum)

- **Client**: name, contacts, notes.
- **Job**: client, site, dates, status (open, in progress, reconciled, certified), technician(s).
- **Asset**: job, manufacturer, model, serial, capacity, interface, media type (HDD, SATA SSD, NVMe, tape, mobile, other), client asset tag, confirmation status and method (barcode match, user confirmed), label photo reference.
- **CustodyEvent**: asset or job, type (received, transported, stored, sanitized, verified, destroyed, released, correction), actor, timestamp, location, details JSON, `prev_hash`, `hash`.
- **SanitizationRecord**: asset, method category (clear, purge, destroy), technique, tool and version, verification method and result, operator, timestamps, source log reference.
- **Certificate**: job, assets covered, PDF reference, payload hash, signature, issued by, issued at, revoked flag and reason.
- **ReviewItem**: anything a human must resolve (serial mismatch, low-confidence read, unmatched inventory row).
- **AuditLog**: every login, confirmation, issue and revocation.

Hash chain: each event's `hash = SHA-256(prev_hash || canonical JSON of the event)`. Provide a command that re-verifies the whole chain and reports the first break.

## Where Claude is used, and how

**Label extraction (`services/extraction.py`).** Input: one photo. Output: a strict schema with manufacturer, model, serial, capacity, interface, media type, each with a confidence, plus a list of every barcode-like string visible. Run barcode decoding separately in code. Compare. Return one of: confirmed by barcode match, needs user confirmation, or conflict (send to review). Never retry until the answers agree; a disagreement is information.

**Inventory reconciliation (`services/reconciliation.py`).** Input: the client's asset list as CSV or XLSX in whatever shape they send, plus the job's captured assets. First use the model once to map their columns onto ours and return that mapping for user approval. Then match in deterministic code (exact, then normalized: case, whitespace, common O/0 and I/1 confusions flagged as *possible* matches, never auto-accepted). Output three lists: matched, on the client's list but not received, received but not on the list.

**Erasure log parsers.** Start with the open-source `nwipe` log format and a generic CSV import. I will tell you which commercial tools we use; add a parser per tool, each with fixture files and tests. Where a format is undocumented, write the parser from the sample files and fail loudly on anything unrecognized.

Nothing else calls a model in the MVP. Certificates, the custody log and the evidence pack are rendered from templates and database fields only.

## Data sources and references

- NIST SP 800-88 Rev. 2, *Guidelines for Media Sanitization* (final, September 2025): https://csrc.nist.gov/pubs/sp/800/88/r2/final — read the certificate and program-record guidance there and base the certificate fields on it. Tell me what it actually says; I have only seen vendor summaries.
- NIST SP 800-171 Media Protection requirements, for `control_mappings.yaml`. Leave the mapping as a draft for my review.
- HIPAA Security Rule device and media controls, 45 CFR 164.310(d). Same treatment.
- IEEE 2883-2022: reference by name only. I supply the content.

## Build phases

**Phase 0 — Scaffold.** Repo layout, config, database, migrations, auth with three roles, base template, CI-ready test and lint commands, `CLAUDE.md`.

**Phase 1 — Jobs, assets, custody.** CRUD for clients and jobs, manual asset entry, append-only custody log with hash chain and verify command.

**Phase 2 — Capture.** Mobile capture page, photo storage, extraction service with barcode cross-check, confirmation screen, review queue.

**Phase 3 — Sanitization and certificates.** Rule matrix loader, method proposal per media type, sanitization records (manual and imported), certificate PDF, Ed25519 signing, public `/verify/<id>` page showing status and hash, revocation.

**Phase 4 — Reconciliation and evidence pack.** Inventory upload, column-mapping approval, match report, per-job evidence pack zip.

**Phase 5 — Client portal and deployment.** Read-only client login for their jobs and certificates, Docker Compose, Caddy, backups, restore drill documented.

**After MVP, do not start without asking:** multi-tenancy, billing, a Raspberry Pi intake station with scanner and label printer, TOTP, API.

## MVP acceptance criteria

1. A technician on a phone can capture 25 drives in under 10 minutes, with every serial either barcode-confirmed or explicitly user-confirmed.
2. On my set of real label photos (I will provide at least 50), no incorrect serial reaches a certificate. Report the extraction accuracy and the review-queue rate honestly, including failures.
3. Editing any stored custody event directly in the database is detected by the chain-verify command.
4. A certificate PDF lists, per device: manufacturer, model, serial, media type, method category, technique, tool and version, verification method and result, operator, and date, plus job, client, issuer and signature. The QR code opens a verify page that confirms the hash, and shows "revoked" if it has been.
5. A messy client spreadsheet (merged header rows, serials in mixed case, an extra notes column) reconciles into the three lists with the column mapping shown for approval first.
6. A job cannot be certified while it has open review items or unreconciled assets, unless an admin records an override reason, which appears in the evidence pack.
7. A client-viewer can see only their own client's jobs. There is a test proving cross-client access is denied.
8. `pytest` passes, `ruff` is clean, and a fresh clone reaches a running app with one documented command.
9. Backup and restore have been run once end to end and the steps are in the README.

## First session task list

Do these in order. Stop after step 7 and summarize what works, what is stubbed, and what you need from me.

1. Fetch and read NIST SP 800-88 Rev. 2. Write a short `docs/800-88r2-notes.md` listing what it says a sanitization record or certificate should contain, with section references. Flag anything that contradicts this brief.
2. Scaffold the repository per the layout above. Write `CLAUDE.md` with the hard rules and the run, test and lint commands.
3. Implement the models and the first migration.
4. Implement auth and the three roles, with tests including the cross-client denial test.
5. Implement the custody service with the hash chain and the verify command, with a test that tampers with a row and expects detection.
6. Implement client, job and manual asset screens, plain but usable on a phone.
7. Create `rules/sanitization_matrix.yaml` with the structure and clearly marked placeholder rows, and a loader that refuses to run in production mode while placeholders remain.

## What I will give you when asked

- 50+ real label photos with serials I have verified by hand.
- Two or three real client inventory spreadsheets with identifying details removed.
- Sample logs from the erasure tools we use, and their names.
- Our current certificate template, so the new one carries the same legal wording.
- The filled-in sanitization matrix from my copy of the standards.

Ask for these when you reach the phase that needs them. Until then, build with synthetic fixtures and label them as synthetic.
