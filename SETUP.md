# Getting Hearth running

About 25 minutes. Everything can be done from a phone.

Nothing here is building — the code is written and tested. This is credentials
and deployment.

---

## Before you start: collect four things

Do these in a browser first. Paste them somewhere you can copy from later.

### 1. An Anthropic API key

console.anthropic.com → **API keys** → Create key. Copy it; it is shown once.

**This is not your Claude subscription.** It is billed separately, per token.
Go to **Billing** and add credit — $20 is months of Hearth. Without credit the
key exists but every call fails, which is a confusing way to lose an evening.

### 2. A Gmail app password for `fennassistant@gmail.com`

- Sign in as that account.
- myaccount.google.com → Security → turn on **2-Step Verification**. Required;
  app passwords do not exist without it.
- Then go to **myaccount.google.com/apppasswords**, name it `hearth`, and copy
  the 16-character password. Spaces do not matter.

No OAuth, no Google Cloud project, no verification. This is the whole thing.

### 3. Calendar addresses

In Google Calendar (as `fennassistant@`), for each calendar you want Hearth to
see: **Settings → the calendar → Integrate calendar → Secret address in iCal
format.** Copy each URL.

Start with the shared **Family** calendar. Add personal ones later, and only
with the person's say-so — Suzan's calendar is her call, not a default.

*Treat these URLs like passwords. Anyone holding one can read that calendar.*

### 4. Somewhere to run it

**Recommended: a new $6/month droplet.** Ubuntu 24.04, smallest size. Keep it
away from the PowerWorld server so a restart or a memory spike never touches
something you depend on.

**Adding it to the existing droplet works too** if it has room. Check first:

```bash
free -h          # want ~400MB+ available
sudo lsof -i :8787   # must print nothing
```

Under 400MB free, get the separate droplet. It is six dollars.

---

## Install

SSH into the droplet, then:

```bash
sudo apt-get update && sudo apt-get install -y git curl
git clone -b claude/life-quality-improvement-wii2bd https://github.com/Mussstarrd/Cipher /opt/hearth
sudo bash /opt/hearth/server/deploy.sh sk-ant-YOUR-KEY-HERE
```

That installs Node 22, generates push keys, and registers Hearth as a service so
it restarts on crash and survives reboots.

## Fill in the rest

One line at a time, so a mangled paste can only break one value and can never
leave the shell waiting on a heredoc terminator:

```bash
echo 'GMAIL_USER=fennassistant@gmail.com' >> /opt/hearth/server/.env
echo 'GMAIL_APP_PASSWORD=abcd efgh ijkl mnop' >> /opt/hearth/server/.env
echo 'CALENDAR_ICS_URLS=https://calendar.google.com/calendar/ical/.../basic.ics' >> /opt/hearth/server/.env
sudo systemctl restart hearth
sudo journalctl -u hearth -n 20 --no-pager
```

Keep the single quotes — they protect spaces and `%` signs. Several calendars go
in one `echo`, comma-separated, no spaces between them.

You want four lines saying `awake`, `calendar ON`, `mail ON as
fennassistant@gmail.com`, and `push ready`. **Anything saying OFF means that
value did not take — fix it before going further.**

## Let the family in

```bash
curl -fsSL https://tailscale.com/install.sh | sh
sudo tailscale up --ssh
sudo tailscale funnel --bg 8787
```

It prints a permanent `https://something.ts.net` URL. Put it in `.env` as
`HEARTH_URL`, then `sudo systemctl restart hearth`.

Funnel publishes that URL to the public internet — deliberately, so the family
needs nothing installed. The passphrase is therefore the only door, and Hearth
refuses to show anything without it.

`--ssh` turns on Tailscale SSH: from then on `ssh root@hearth` works from any
device on your tailnet, with no keys to generate, copy or lose. That is the real
administration path. Closing port 22 to the internet does not touch it.

## First run

1. Open the URL on your phone.
2. **Add to Home Screen.**
3. Tap **notify me** and allow notifications.
4. Tap **Run 07:00 now**.

Within a minute you should have a check-in built from your real inbox and
calendar, and a notification. If the check-in says it could not reach your mail,
that is Hearth telling you the truth — go back and check the app password.

Then send the URL and the passphrase (in `.env`) to Suzan. Same three steps.

## Lock the front door

Once the first run works. A fresh droplet has SSH open to the internet and bots
start guessing passwords on new IPs within minutes. You never need SSH — the
DigitalOcean Console reaches the machine another way, and Tailscale gives you a
private path.

```bash
ufw default deny incoming
ufw default allow outgoing
ufw allow in on tailscale0
ufw --force enable
ufw status verbose
```

Safe to run **from the DigitalOcean Console**, precisely because the Console is
not SSH — the same commands over a normal SSH session would cut you off
mid-command. The `.ts.net` URL keeps working: Funnel arrives through Tailscale's
own tunnel rather than an open port.

After this a leaked root password is useless from outside the tailnet, which is
why Password authentication was the right choice rather than a compromise.

---

## When something breaks

```bash
sudo journalctl -u hearth -f          # live logs
sudo systemctl restart hearth
journalctl -u hearth -f
```

**"Could not resolve authentication method"** — the API key is missing or the
service did not reload. Check `.env`, restart.

**"Invalid credentials" on mail** — the app password is wrong, or 2-Step
Verification is not actually on. Regenerate it.

**No notifications** — push needs HTTPS, so it only works over the Tailscale
URL, never over the raw IP. Confirm `HEARTH_URL` is the `.ts.net` one.

**Never regenerate the VAPID keys in `.env`.** It silently unsubscribes every
phone in the house and nothing tells you why. The deploy script refuses to
overwrite them for this reason.

## Updating later

```bash
cd /opt/hearth && git pull && cd server && npm install --omit=dev
sudo systemctl restart hearth
```

`.env` and `server/data/` are never touched by an update.

---

## What you will have

A thing that hibernates, wakes at 07:00, 12:00, 17:00 and 22:00 Eastern, reads
the family inbox and calendar, tells you what matters, and at 22:00 rewrites its
own memory so tomorrow starts smarter than today. Everyone opens the same URL.
It runs whether or not anyone is watching.

Cost: about $6 for the droplet and $7–15 in Claude usage per month.
