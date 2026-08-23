#!/usr/bin/env bash
# Write a status report the design session can read, and push it. Runs on a
# timer. No model tokens are spent here — this is instrumentation, not thinking.
#
# Why it exists: the design session cannot see this box. Without a report it is
# guessing, and a guess about whether a service is running is worse than no
# answer. This is the droplet's half of the conversation.
#
# It never prints a secret. Credentials are reported as "set" or "unset" only.
set -uo pipefail

DIR="${HEARTH_DIR:-/opt/hearth}"
BRANCH="${HEARTH_BRANCH:-claude/life-quality-improvement-wii2bd}"
LOCK="/var/lock/hearth-git.lock"

cd "$DIR" || { echo "[heartbeat] $DIR missing"; exit 1; }

DAY=$(TZ=America/New_York date +%F)
OUT="ops/status/$DAY.md"
mkdir -p ops/status

exec 9>"$LOCK" || exit 0
flock -w 120 9 || { echo "[heartbeat] git lock busy for 2min; skipping this report"; exit 0; }

# --- collect ------------------------------------------------------------
NOW_UTC=$(date -u +%Y-%m-%dT%H:%MZ)
NOW_ET=$(TZ=America/New_York date "+%a %d %b %H:%M")

unit() { systemctl is-active "$1" 2>/dev/null || echo "missing"; }
since() { systemctl show "$1" -p ActiveEnterTimestamp --value 2>/dev/null | cut -c1-25; }

# .env: report presence, never the value.
envset() {
  local v
  v=$(grep -E "^$1=" server/.env 2>/dev/null | head -1 | cut -d= -f2-)
  v="${v//[[:space:]]/}"
  case "$v" in
    ""|change-me|change-me-too|sk-ant-...|xxxxxxxxxxxxxxxx) echo "unset" ;;
    *) echo "set (${#v} chars)" ;;
  esac
}

STATE=server/data/state.json
js() { [ -f "$STATE" ] && node -e '
  const s=JSON.parse(require("fs").readFileSync(process.argv[1],"utf8"));
  const f=process.argv[2];
  if(f==="counts") console.log(`messages ${(s.messages||[]).length} · reports ${(s.reports||[]).length} · push subs ${(s.subs||[]).length} · mail ${(s.mail||[]).length} · lastUid ${s.lastUid||0}`);
  if(f==="runs"){
    // A slot that never fired must be VISIBLE, not absent: expected-vs-fired,
    // not a list of successes. This line once counted a three-miss day as one.
    const et=new Intl.DateTimeFormat("en-CA",{timeZone:"America/New_York",year:"numeric",month:"2-digit",day:"2-digit",hour:"2-digit",minute:"2-digit",hour12:false}).formatToParts(new Date()).reduce((a,x)=>(a[x.type]=x.value,a),{});
    const today=`${et.year}-${et.month}-${et.day}`, now=`${et.hour}:${et.minute}`;
    const slots=["07:00","12:00","17:00","22:00"].filter(t=>t<=now);
    const fired=slots.filter(t=>(s.lastRun||{})[t]===today);
    const missed=slots.filter(t=>(s.lastRun||{})[t]!==today);
    console.log(slots.length?`${fired.length}/${slots.length} due so far today fired${missed.length?` — MISSED: ${missed.join(", ")}`:""}`:"none due yet today");
  }
  if(f==="last") { const m=(s.messages||[]).slice(-1)[0]; console.log(m?`${m.who}: ${String(m.text).slice(0,80)}`:"none"); }
' "$STATE" "$1" 2>/dev/null || echo "unreadable"; }

GIT_HEAD=$(git log -1 --format='%h %s' 2>/dev/null)
git fetch origin "$BRANCH" --quiet 2>/dev/null
AHEAD=$(git rev-list --count "origin/$BRANCH..HEAD" 2>/dev/null || echo "?")
BEHIND=$(git rev-list --count "HEAD..origin/$BRANCH" 2>/dev/null || echo "?")

ERRS=$(journalctl -u hearth --since "-6h" -p warning --no-pager -q 2>/dev/null | tail -12)
UPD=$(journalctl -u hearth-update --since "-6h" --no-pager -q -o cat 2>/dev/null | grep -E '^\[autoupdate\]' | tail -4)
DISK=$(df -h / | awk 'NR==2{print $4" free of "$2" ("$5" used)"}')
MEMF=$(free -m | awk '/^Mem:/{print $7"MB available of "$2"MB"}')

TODO=$(ls ops/instructions/*.md 2>/dev/null | grep -v README | while read -r f; do
  grep -qiE '^\**status:[[:space:]]*done' "$f" || echo "  - ${f#ops/instructions/}: $(grep -m1 '^# ' "$f" | sed 's/^# //')"
done)

# --- write --------------------------------------------------------------
[ -f "$OUT" ] || printf '# Droplet status — %s\n\nAppend-only. Written by server/heartbeat.sh. Read by the design session.\n' "$DAY" > "$OUT"

{
  echo
  echo "## $NOW_ET ET  ($NOW_UTC)"
  echo
  echo "- service: hearth **$(unit hearth)** since $(since hearth)"
  echo "- timers: update **$(unit hearth-update.timer)** · heartbeat **$(unit hearth-heartbeat.timer)**"
  echo "- git: \`$GIT_HEAD\` — $AHEAD ahead / $BEHIND behind origin"
  echo "- wakes: $(js runs)"
  echo "- state: $(js counts)"
  echo "- last message: $(js last)"
  # The adults passphrase can live in .env OR in the file a parent sets from the
  # app; reporting "unset" while the app file exists was a false alarm.
  ADULT_STATE=$(envset HEARTH_ADULT_PASSPHRASE)
  [ "$ADULT_STATE" = "unset" ] && [ -s server/data/adult-pass ] && ADULT_STATE="set (via app)"
  echo "- credentials: api $(envset ANTHROPIC_API_KEY) · gmail $(envset GMAIL_APP_PASSWORD) · calendar $(envset CALENDAR_ICS_URLS) · backup $(envset BACKUP_GIT_REMOTE) · adult room $ADULT_STATE · vapid $(envset VAPID_PRIVATE)"
  echo "- host: $DISK · $MEMF · load$(cut -d' ' -f1-3 /proc/loadavg | sed 's/^/ /')"
  [ -n "$TODO" ] && { echo "- open instructions:"; echo "$TODO"; }
  [ -n "$UPD" ] && { echo; echo "\`\`\`"; echo "$UPD"; echo "\`\`\`"; }
  if [ -n "$ERRS" ]; then
    echo; echo "Warnings and errors, last 6h:"; echo '```'; echo "$ERRS"; echo '```'
  else
    echo "- no warnings or errors in 6h"
  fi
} >> "$OUT"

# --- publish ------------------------------------------------------------
git add "$OUT" ops/status 2>/dev/null
git diff --cached --quiet && exit 0
git commit -q -m "status $NOW_UTC" || exit 0

if ! git push --quiet origin "$BRANCH" 2>/dev/null; then
  # ops/status/*.md is merge=union in .gitattributes, so this resolves itself.
  git pull --rebase --quiet origin "$BRANCH" 2>/dev/null || {
    git rebase --abort 2>/dev/null
    echo "[heartbeat] push blocked and rebase failed — report is committed locally"
    exit 0
  }
  git push --quiet origin "$BRANCH" 2>/dev/null || echo "[heartbeat] push still refused"
fi
echo "[heartbeat] $NOW_ET reported"
