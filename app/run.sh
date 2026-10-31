#!/usr/bin/env sh
# ReleaseLine: start the local server. Usage: ./run.sh [--port N] [--no-browser]
cd "$(dirname "$0")" || exit 2
exec python3 -m releaseline serve "$@"
