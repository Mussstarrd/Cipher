# Backing up memory

The droplet can be rebuilt from this repo in ten minutes. The API key can be
reissued. The Gmail app password can be regenerated. **Memory cannot.** It is
months of watching one family, and losing it does not just cost the data — it
costs every question Hearth then has to ask again, which is the one thing the
brief says never to do.

## Where it stands right now

| | |
| --- | --- |
| Local git history | **working** — `/opt/hearth-backup`, a commit after every wake |
| Off-machine copy | **not configured** — `BACKUP_GIT_REMOTE` is unset |

Local history is not a backup. It is on the same disk as the thing it is
protecting; one droplet failure takes both. What it *does* buy is a rollback
when the 22:00 review rewrites a memory layer badly, which is the more likely
accident of the two — and it means that the moment a remote is configured,
every commit made up to that point pushes at once. Nothing is lost by having
started early.

Check it any time:

```bash
cd /opt/hearth/server && npm run backuptest
```

## Finishing it — off-machine

Three steps. Two of them are Jeffery's: **Hearth does not create repositories or
issue tokens on anyone's behalf**, and a fine-grained token can only be minted
in a browser anyway.

**1. Create a private repo.** github.com/new → name it `hearth-memory` → set it
to **Private**. Do not initialise it with anything; the first push brings its
own history.

> It must be private, and it must not be this repo. This repo is public and
> holds the code. The backup holds the family's address, the children's schools,
> the inbox and the reference sheet. Those never share a repository.

**2. Mint a fine-grained token.** Settings → Developer settings → Personal
access tokens → Fine-grained tokens → Generate new token.

- **Repository access:** Only select repositories → `hearth-memory`
- **Permissions:** Repository permissions → **Contents: Read and write**
- Nothing else. No org access, no other repos, no Actions.
- Set an expiry you will actually notice. `backuptest` names an expired token as
  the cause, and the 22:00 check-in raises a failed backup on its own — but a
  calendar reminder is cheaper than finding out in the check-in.

**3. Point Hearth at it.** In `server/.env`:

```
BACKUP_GIT_REMOTE=https://<token>@github.com/<you>/hearth-memory.git
```

Then:

```bash
systemctl restart hearth
cd /opt/hearth/server && npm run backuptest
```

You are looking for `RESULT: memory is backed up off this machine.` Anything
else prints the actual cause rather than a generic failure.

## What is in the backup, and what is deliberately not

**In:** every file under `memory/` — facts, rhythms, open loops, corrections,
misses, reference, and every daily log. Plus `state.json`, so the family channel
and the check-in history survive a rebuild.

**Not in:** push subscriptions. They are per-device tokens, they are worthless
on a restored machine, and a backup is not a place to accumulate credentials
that nothing will ever use. `backuptest` will show `subs` absent from the copied
`state.json`; that is correct, not a bug.

**Never in:** `server/.env`. The backup repo is private, but "private" and
"the right place for the API key" are different claims. Secrets are replaceable;
that is the whole reason they do not need backing up.

## Restoring

```bash
git clone https://<token>@github.com/<you>/hearth-memory.git /tmp/restore
cp -r /tmp/restore/memory/. /opt/hearth/memory/
cp /tmp/restore/state.json /opt/hearth/server/data/state.json
systemctl restart hearth
```

Rolling back one bad layer rather than the whole thing — the common case, and
the reason this is git and not a tarball:

```bash
cd /opt/hearth-backup
git log --oneline -- memory/facts.md
git show <commit>:memory/facts.md > /opt/hearth/memory/facts.md
```

Everyone will then be logged out of push and will need to re-add the app to
their home screen, because the subscriptions were not restored. That is the
trade, and it is the right one.
