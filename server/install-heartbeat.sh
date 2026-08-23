#!/usr/bin/env bash
# One-time: install the timer that reports this box's state into the repo.
# Run as root. Safe to re-run.
set -euo pipefail
DIR="${HEARTH_DIR:-/opt/hearth}"

cat > /etc/systemd/system/hearth-heartbeat.service <<UNIT
[Unit]
Description=Write and push a Hearth droplet status report
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStart=/usr/bin/env bash $DIR/server/heartbeat.sh
UNIT

cat > /etc/systemd/system/hearth-heartbeat.timer <<UNIT
[Unit]
Description=Report Hearth droplet status every 2 hours

[Timer]
OnBootSec=4min
OnUnitActiveSec=2h
RandomizedDelaySec=120
Persistent=true

[Install]
WantedBy=timers.target
UNIT

systemctl daemon-reload
systemctl enable --now hearth-heartbeat.timer

# Prove it works now rather than hoping it works in two hours.
echo
echo "--- first run ---"
systemctl start hearth-heartbeat
sleep 2
journalctl -u hearth-heartbeat -n 20 --no-pager -o cat
echo
DAY=$(TZ=America/New_York date +%F)
if [ -f "$DIR/ops/status/$DAY.md" ]; then
  echo "--- wrote ops/status/$DAY.md ---"
  tail -20 "$DIR/ops/status/$DAY.md"
else
  echo "NO REPORT WRITTEN — the timer is installed but heartbeat.sh did not produce a file."
  exit 1
fi
echo
systemctl list-timers hearth-heartbeat --no-pager
