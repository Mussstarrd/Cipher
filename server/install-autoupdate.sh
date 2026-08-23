#!/usr/bin/env bash
# One-time: install the timer that keeps Hearth current. Run as root.
set -euo pipefail
DIR="${HEARTH_DIR:-/opt/hearth}"

cat > /etc/systemd/system/hearth-update.service <<UNIT
[Unit]
Description=Pull and apply Hearth updates
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStart=/usr/bin/env bash $DIR/server/autoupdate.sh
UNIT

cat > /etc/systemd/system/hearth-update.timer <<UNIT
[Unit]
Description=Check for Hearth updates every 15 minutes

[Timer]
OnBootSec=3min
OnUnitActiveSec=15min
RandomizedDelaySec=60
Persistent=true

[Install]
WantedBy=timers.target
UNIT

systemctl daemon-reload
systemctl enable --now hearth-update.timer
echo
systemctl list-timers hearth-update --no-pager
echo
echo "Updates now arrive on their own. Watch them with:"
echo "  journalctl -u hearth-update -f"
