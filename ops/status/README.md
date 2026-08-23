# Status reports — droplet to design session

`server/heartbeat.sh` appends one block to `ops/status/YYYY-MM-DD.md` every two
hours and pushes it. Nothing here is written by a model; it is instrumentation.

Read it to answer, without guessing:

- is the service up, and since when
- did the 07:00 / 12:00 / 17:00 / 22:00 wakes actually fire
- is this box ahead of or behind origin
- which credentials are still unset (never their values — only set/unset and a
  character count, which is what caught a 15-character app password once)
- what errored in the last six hours
- which `ops/instructions/` entries are still open

These files are `merge=union` in `.gitattributes`, so two sessions appending on
the same day merge without a conflict. Never rewrite an old block — append only.
Old days can be deleted once read; nothing depends on them.
