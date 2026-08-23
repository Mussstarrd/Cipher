#!/usr/bin/env bash
# Pull whatever the design session pushed, and restart Hearth if anything moved.
# Runs on a timer so nobody has to be the courier.
#
# Refuses to touch the repo if the droplet has its own work in flight — a
# convenience updater must never eat a session's uncommitted or unpushed
# commits. It skips and says why.
set -uo pipefail

DIR="${HEARTH_DIR:-/opt/hearth}"
BRANCH="${HEARTH_BRANCH:-claude/life-quality-improvement-wii2bd}"
cd "$DIR" || { echo "autoupdate: $DIR missing"; exit 1; }

say() { echo "[autoupdate] $*"; }

if [ -n "$(git status --porcelain)" ]; then
  say "SKIP — uncommitted changes on the droplet. Not touching them."
  exit 0
fi

git fetch origin "$BRANCH" --quiet || { say "SKIP — fetch failed (token? network?)"; exit 0; }

LOCAL=$(git rev-parse HEAD)
REMOTE=$(git rev-parse "origin/$BRANCH")
BASE=$(git merge-base HEAD "origin/$BRANCH")

[ "$LOCAL" = "$REMOTE" ] && exit 0                       # nothing new, stay quiet

if [ "$LOCAL" != "$BASE" ]; then
  say "SKIP — the droplet has unpushed commits. Push them first; not rewriting history."
  exit 0
fi

say "updating $(git rev-parse --short HEAD) -> $(git rev-parse --short "origin/$BRANCH")"
git merge --ff-only "origin/$BRANCH" --quiet || { say "SKIP — fast-forward refused"; exit 0; }

# Only reinstall when the dependency list actually changed; npm install is slow
# and this runs every quarter hour.
if git diff --name-only "$LOCAL" HEAD | grep -q '^server/package.json'; then
  say "package.json changed — installing"
  (cd server && npm install --omit=dev --no-audit --no-fund >/dev/null 2>&1)
fi

systemctl restart hearth
sleep 3
if systemctl is-active --quiet hearth; then
  say "restarted OK on $(git log -1 --format='%h %s')"
else
  say "RESTART FAILED — rolling back to $LOCAL"
  git reset --hard "$LOCAL" --quiet
  systemctl restart hearth
  say "rolled back. The update is on origin but this box refused it."
fi
