# Running Hearth — for a Claude session on the droplet

You are **maintaining Hearth. You are not Hearth.**

`CLAUDE.md` in this repo is the household assistant's own brief — its voice, its
rules, its memory. Do not adopt it. You are an engineer with root on the machine
Hearth runs on. Read it for context; do not become it.

## Where things are

| Path | What |
| --- | --- |
| `/opt/hearth` | this repo |
| `server/src/` | the service: server, brain, mail, calendar, push, backup |
| `server/.env` | secrets. **Never commit. Never print in full.** |
| `server/data/` | runtime state, untracked |
| `memory/` | what Hearth has learned. The irreplaceable part. |
| `/etc/systemd/system/hearth.service` | the unit |

```bash
systemctl status hearth
journalctl -u hearth -f          # live
journalctl -u hearth -n 50 --no-pager
systemctl restart hearth

cd /opt/hearth/server
npm run preflight                # does Hearth actually work? writes nothing
npm run preflight -- --brief 07:00   # ...and render that check-in, without saving it
npm test                         # the scheduler decision, 11 cases
npm run mailtest                 # diagnose mail alone
npm run backuptest               # diagnose backup alone, including the push
```

**Start with `preflight`.** "It is running" and "it works" are different claims,
and the whole point of that command is to stop them looking alike. It exercises
memory, the model, calendar, mail, push, the URL notifications open, backup and
the clock, and writes nothing anywhere — safe at any hour, service up or down.

A healthy start prints six lines: `awake`, `slots`, `push`, `backup`, `calendar`,
`mail`. Anything `OFF` is a missing value in `.env`. `backup LOCAL ONLY` is not
`OFF` — memory is being versioned on this disk, but there is no copy off it.

## Keeping Jeffery's other session in the loop

He has a second Claude session that cannot reach this machine. **The repo is the
only thing you both see.** So:

- **Commit and push everything you change**, to
  `claude/life-quality-improvement-wii2bd`. Work that is not pushed is invisible.
- Write commit messages that explain *why*, with the symptom and the cause.
  These are read by someone who never saw the terminal output.
- If you diagnose something and decide not to change it, still record it — a
  note in `docs/` or `memory/misses.md`. A finding nobody wrote down is lost.

## Rules

1. **Never commit `server/.env` or `server/data/`.** They are gitignored; keep
   it that way. Never paste secrets into a chat or a commit message.
2. **Never regenerate the VAPID keys.** It silently unsubscribes every phone in
   the house and nothing reports why.
3. **Verify before claiming success.** Restart, read the logs, hit the endpoint.
   "Should work" has been wrong every time so far tonight.
4. **A silent failure is the enemy.** Anything that can fail must say so, in a
   place a human will see. A thin brief and a broken run look identical from
   outside; that ambiguity is what destroys trust in this product.
5. **Memory is sacred.** Only the 22:00 review rewrites `memory/`. Do not edit
   facts, rhythms or corrections by hand to make something look right.
6. **Ask before acting outward.** Hearth never sends, books, pays or registers on
   anyone's behalf. Neither do you.

## Failures already seen and fixed — check these first

| Symptom | Cause |
| --- | --- |
| `journalctl` shows only start/stop, no `[hearth]` lines | unit was redirecting output to a file |
| `mail: Command failed` | empty mailbox, or a bad app password |
| `Invalid credentials` | app password not exactly 16 lowercase letters — a paste dropped a character |
| `git pull` aborts on `state.json` | runtime state was tracked; now gitignored |
| no "Add to Home Screen" | icons missing, or served as `application/octet-stream` |
| iPhone cannot enable notifications | iOS only allows them for a Home-Screen app, never a Safari tab |
| Funnel refused | must be enabled once in the Tailscale admin console |
| service restart-loops ~5 min after `mail:` error | imapflow emits `'error'` as an **event**; unlistened, it kills the process. Fixed — keep the `client.on("error")` in `mail.js` |
| a check-in never arrived and nothing says why | the minute was missed. There is now a 90-min grace window; before that, an exact-match tick lost the day in silence |
| push notification opens a blank page | `HEARTH_URL` unset or malformed in `.env` → falls back to localhost. `preflight` fails on this |
| a value in `.env` has no effect | that line has no `=`, or the key is defined again lower down. `preflight` reports both |

## Open work

Session of 22 Aug: see `docs/findings-2026-08-22.md` for what was fixed, what
was diagnosed and left alone, and why. What remains:

- **Nobody is subscribed to push. Zero devices.** This is now the largest gap —
  every check-in the service produces ends in a notification that reaches no
  one. Needs a human to add the app to a home screen and allow notifications
  (on iPhone, only a Home-Screen app can; never a Safari tab). Ahead of backup,
  because a working brief nobody receives is the same as no brief.
- **No off-machine backup.** Memory is now versioned locally in
  `/opt/hearth-backup` after every wake, which covers a bad memory rewrite but
  not a lost droplet. Finishing it needs a private repo and a fine-grained
  token, both of which are Jeffery's to create: `docs/backup.md`.
- **Rotate the `origin` PAT.** It sat in a world-readable `.git/config` and was
  exposed in a session transcript. Permissions are fixed; the token is not.
- **Ask Jeffery about the calendar.** The configured feed is reachable and
  totally empty, while `rhythms.md` holds the family's real commitments. Either
  the ICS URL is the wrong calendar, or this household is genuinely standalone.
  One question settles it; guessing does not.
- **Check memory on the 23rd.** It currently records "nothing is scheduled",
  which was true of the deleted trigger and is false of the service. If the
  22:00 review has not reconciled it, that is a miss for `misses.md`.
- Suzan, Aiden and Abby are not on it yet. Adoption is the actual test.
