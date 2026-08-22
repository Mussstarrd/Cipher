#!/usr/bin/env bash
# Put Hearth on an always-on Debian/Ubuntu box and keep it running.
#
#   curl -fsSL <raw-url>/server/deploy.sh | bash -s -- <your-anthropic-key>
#
# or, on a box where you've already cloned the repo:
#   sudo bash server/deploy.sh <your-anthropic-key>
#
# It installs Node 22, sets Hearth up as a systemd service so it survives
# reboots and crashes, and generates the push keys. It does NOT open any ports —
# see the tunnel step it prints at the end.
set -euo pipefail

KEY="${1:-}"
REPO="${HEARTH_REPO:-https://github.com/Mussstarrd/Cipher}"
BRANCH="${HEARTH_BRANCH:-claude/life-quality-improvement-wii2bd}"
DIR="${HEARTH_DIR:-/opt/hearth}"
USER_="${SUDO_USER:-$(whoami)}"

say() { printf '\n\033[1m%s\033[0m\n' "$*"; }

[ -n "$KEY" ] || { echo "Usage: bash deploy.sh <anthropic-api-key>"; exit 1; }

say "1/5  Node 22"
if ! command -v node >/dev/null || [ "$(node -p 'process.versions.node.split(".")[0]')" -lt 20 ]; then
  curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
  sudo apt-get install -y nodejs
fi
node --version

say "2/5  Code at $DIR"
if [ -d "$DIR/.git" ]; then
  sudo git -C "$DIR" fetch origin "$BRANCH" && sudo git -C "$DIR" checkout "$BRANCH" && sudo git -C "$DIR" pull origin "$BRANCH"
else
  sudo mkdir -p "$DIR" && sudo chown "$USER_" "$DIR"
  git clone --branch "$BRANCH" "$REPO" "$DIR"
fi
sudo chown -R "$USER_" "$DIR"
cd "$DIR/server" && npm install --omit=dev --no-audit --no-fund

say "3/5  Config"
if [ ! -f "$DIR/server/.env" ]; then
  VAPID=$(node -e "import('web-push').then(w=>{const k=w.default.generateVAPIDKeys();console.log(k.publicKey+'\n'+k.privateKey)})")
  PUB=$(echo "$VAPID" | head -1); PRIV=$(echo "$VAPID" | tail -1)
  cat > "$DIR/server/.env" <<ENV
ANTHROPIC_API_KEY=$KEY
PORT=8787
HEARTH_URL=http://localhost:8787
HEARTH_PASSPHRASE=$(head -c 9 /dev/urandom | base64 | tr -d '/+=')
VAPID_PUBLIC=$PUB
VAPID_PRIVATE=$PRIV
VAPID_SUBJECT=mailto:jeffery.fenn@gmail.com
ENV
  chmod 600 "$DIR/server/.env"
  echo "wrote $DIR/server/.env"
else
  echo "keeping existing .env (push keys must never be regenerated)"
fi

say "4/5  Service"
sudo tee /etc/systemd/system/hearth.service >/dev/null <<UNIT
[Unit]
Description=Hearth — household assistant
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=$USER_
WorkingDirectory=$DIR/server
ExecStart=/usr/bin/node --env-file=$DIR/server/.env src/server.js
Restart=always
RestartSec=5
StandardOutput=append:$DIR/server/data/hearth.log
StandardError=append:$DIR/server/data/hearth.log

[Install]
WantedBy=multi-user.target
UNIT
mkdir -p "$DIR/server/data"
sudo systemctl daemon-reload && sudo systemctl enable --now hearth
sleep 2 && sudo systemctl --no-pager status hearth | head -6

say "5/5  Let the family in"
cat <<'NEXT'
Hearth is running on port 8787 but is not reachable from outside yet.
Web push needs HTTPS, so you need a real URL. Easiest, free, no domain:

  curl -fsSL https://tailscale.com/install.sh | sh
  sudo tailscale up
  sudo tailscale funnel 8787

That prints a permanent https://<machine>.ts.net URL. Put it in
server/.env as HEARTH_URL, then:  sudo systemctl restart hearth

Send the family that URL. On a phone: open it, Add to Home Screen, tap
"notify me". The passphrase is in server/.env — give it to them, nobody else.

  logs:     tail -f /opt/hearth/server/data/hearth.log
  restart:  sudo systemctl restart hearth
NEXT
