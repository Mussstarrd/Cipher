#!/usr/bin/env bash
# Pull what the design session pushed, apply it, restart. Runs on a timer.
#
# The first version refused to act on a dirty tree — and Hearth writes
# memory/daily/*.md continuously, so the tree is ALWAYS dirty and it never once
# applied an update. It exited 0 and logged SKIP, which reads like a healthy
# no-op. So: classify the dirt instead of refusing it.
set -uo pipefail

DIR="${HEARTH_DIR:-/opt/hearth}"
BRANCH="${HEARTH_BRANCH:-claude/life-quality-improvement-wii2bd}"
LOCK="/var/lock/hearth-git.lock"
STAMP="/var/lib/hearth-lastskip"

exec 9>"$LOCK" || exit 0
flock -n 9 || exit 0          # the heartbeat holds this too; never interleave git

cd "$DIR" || { echo "[autoupdate] $DIR missing"; exit 1; }
say() { echo "[autoupdate] $*"; }

# A repeated SKIP every 15 minutes trains everyone to ignore the log. Once an
# hour is enough to notice; more than that is noise that hides real failures.
skip() {
  local now last
  now=$(date +%s); last=$(cat "$STAMP" 2>/dev/null || echo 0)
  if [ $((now - last)) -ge 3600 ]; then say "SKIP — $1"; echo "$now" > "$STAMP"; fi
  exit 0
}

DIRT=$(git status --porcelain | awk '{print $2}')
OUTSIDE=$(echo "$DIRT" | grep -v '^memory/' | grep -v '^ops/status/' | grep -v '^$' || true)
[ -n "$OUTSIDE" ] && skip "changes outside memory/ — a human or a session is mid-edit: $(echo $OUTSIDE | head -c 120)"

if [ -n "$DIRT" ]; then
  # Only memory/ is dirty: that is the service doing its job. Commit it — it
  # belongs in git anyway, and this versions it every quarter hour rather than
  # twice a day.
  git add memory/ ops/status/ 2>/dev/null
  git commit -q -m "hearth memory $(date -u +%Y-%m-%dT%H:%MZ)" || true
fi

git fetch origin "$BRANCH" --quiet || skip "fetch failed (token? network?)"

LOCAL=$(git rev-parse HEAD); REMOTE=$(git rev-parse "origin/$BRANCH")
BASE=$(git merge-base HEAD "origin/$BRANCH")
[ "$LOCAL" = "$REMOTE" ] && exit 0

# Unpushed commits are fine if they are only memory snapshots — replaying those
# is safe. Anything else is somebody's work and is not ours to rebase.
if [ "$LOCAL" != "$BASE" ]; then
  CODE=$(git diff --name-only "$BASE" HEAD | grep -v '^memory/' | grep -v '^ops/status/' || true)
  [ -n "$CODE" ] && skip "unpushed commits touch code; push them yourself first"
  git pull --rebase --quiet origin "$BRANCH" || { git rebase --abort 2>/dev/null; skip "rebase failed"; }
else
  git merge --ff-only "origin/$BRANCH" --quiet || skip "fast-forward refused"
fi

say "updated to $(git log -1 --format='%h %s')"

# Status and memory commits arrive every couple of hours; restarting a healthy
# service for a commit that touched no code is downtime for nothing. Only a
# change under server/ warrants a restart (the page and CLAUDE.md are read per
# request; brain prompts and code are not).
if ! git diff --name-only "$LOCAL" HEAD | grep -q '^server/'; then
  say "no server changes — restart skipped"
  git push --quiet origin "$BRANCH" 2>/dev/null && say "memory pushed" || true
  exit 0
fi

if git diff --name-only "$LOCAL" HEAD | grep -q '^server/package.json'; then
  say "dependencies changed — installing"
  (cd server && npm install --omit=dev --no-audit --no-fund >/dev/null 2>&1)
fi

systemctl restart hearth
sleep 3
if systemctl is-active --quiet hearth; then
  say "restarted OK"
  git push --quiet origin "$BRANCH" 2>/dev/null && say "memory pushed" || true
else
  say "RESTART FAILED — rolling back to $LOCAL"
  git reset --hard "$LOCAL" --quiet
  systemctl restart hearth
  say "rolled back; the update is on origin but this box refused it"
fi
