# Hearth — the server

The assistant *is* the app. It runs on a laptop or a cloud box, owns its own
clock, and everyone in the family opens the same URL. No Claude scheduler, no
platform, no app store.

```
hibernate ──► 07:00 ──► 12:00 ──► 17:00 ──► 22:00 ──► rewrite memory ──► hibernate
```

Between wakes it sleeps. On each wake it reads its own memory, works out what
the family needs to know, writes what it learned back to `memory/`, and pushes a
notification. Tap it and the app opens. In between, anyone can ask it anything.

The 22:00 wake is the one that matters: it is the only thing that rewrites
memory, and it asks *what did I get wrong today* rather than *what happened
today*. That is why tomorrow is better than today.

## Run it

```bash
cd server
npm install
cp .env.example .env          # add your ANTHROPIC_API_KEY
npm run keys                  # once — paste both keys into .env
npm start
```

Open http://localhost:8787.

`--env-file=.env` is built into Node 20+, so no dotenv dependency:

```bash
node --env-file=.env src/server.js
```

## Let the family in

Push notifications need HTTPS (localhost is exempt). A tunnel gives you both the
HTTPS and the remote access, and takes about a minute:

```bash
cloudflared tunnel --url http://localhost:8787
```

Send the family the URL it prints. On a phone: open it, **Add to Home Screen**,
then tap **notify me**. It behaves like an installed app from then on — no store,
no accounts.

Set `HEARTH_URL` in `.env` to that tunnel URL so notifications open the right
place, and set `HEARTH_PASSPHRASE` to something only the family knows.

## What it costs

Roughly $20–30/month at list price for one household: four wakes a day plus
questions. The memory prefix is identical on every call, so prompt caching does
most of the work. The 22:00 review is the expensive one — do not economise
there; it is where the compounding happens.

## Layout

| File | Job |
| --- | --- |
| `src/server.js` | The clock, the HTTP door, the wake loop |
| `src/brain.js` | The four wakes, answering questions, the nightly review |
| `src/memory.js` | Reads and writes the markdown in `../memory/` |
| `src/push.js` | Web push |
| `public/index.html` | What the family sees |

Memory is the repo's own markdown — the same files a human can read, diff and
correct. That is deliberate: the family must be able to read every word held
about them, and the first time someone feels watched by it, it is dead.

## Deliberate choices

- **It reads the clock, not a cron expression.** Wall-clock time in
  `America/New_York` is checked every 30 seconds, so daylight saving is handled
  by the calendar rather than by remembering to edit a schedule twice a year.
- **A failed wake is never silent.** It writes the failure into the day's log
  and posts it as a failed report, because a thin brief and a broken run look
  identical from the outside, and that is the failure mode that destroys trust.
- **State is written atomically** — a crash mid-write can never truncate memory.
- **Only the 22:00 review writes long-lived memory**, and it may only touch the
  six known layers. Nothing else can rewrite what the household knows.

## Not built yet

- **Connectors.** It cannot read Gmail or your calendar; everything arrives by
  someone telling it. That is standalone mode, and standalone has to work
  anyway — it is where most people would start.
- **Photos.** No upload yet.
- **Per-person identity.** Everyone picks their name; the server takes their
  word for it. Fine for a household, not for anything wider.
