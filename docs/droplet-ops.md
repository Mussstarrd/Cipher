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
cd /opt/hearth/server && npm run mailtest    # diagnose mail alone
```

A healthy start prints five lines: `awake`, `backup`, `calendar`, `mail`, `push`.
Anything `OFF` is a missing value in `.env`.

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

## Open work

- `backup OFF` — memory has no off-machine copy. Highest-value remaining task:
  create a **private** repo, add a fine-grained token, set `BACKUP_GIT_REMOTE`.
- The first real 07:00 wake has not happened yet. Watch it; treat anything it
  gets wrong as a finding, not a nuisance.
- Suzan, Aiden and Abby are not on it yet. Adoption is the actual test.
