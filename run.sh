#!/usr/bin/env bash
# Start the Cipher dashboard.
#   ./run.sh            -> http://localhost:8000  (LAN: http://<host-ip>:8000)
# Prereqs: python authenticate.py has been run, ffmpeg installed (for live view).
set -euo pipefail
cd "$(dirname "$0")"

HOST="${CIPHER_HOST:-0.0.0.0}"
PORT="${CIPHER_PORT:-8000}"

if [ ! -f blink_session.json ]; then
  echo "!! No blink_session.json found. Run:  python authenticate.py" >&2
  exit 1
fi

exec uvicorn app.main:app --host "$HOST" --port "$PORT"
