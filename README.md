# Cipher

Self-hosted home situational-awareness dashboard for **Blink** cameras.

Cipher talks to Blink's cloud through the community [`blinkpy`](https://github.com/fronzbot/blinkpy)
library (Blink has no official public API), aggregates all your cameras into one
web dashboard, auto-refreshes snapshots, and streams **live video** on demand.

- 🟢 **Snapshots** — reliable, low-battery-impact grid that always works.
- 🔴 **Live view** — on-demand MPEG-TS livestream → HLS → browser (needs `ffmpeg`).
- 🔋 Per-camera battery / wifi / temperature / motion state.

> **Access model.** Cipher uses *your own* Blink account credentials to reach
> *your own* cameras — the same access the Blink app has. There is no official
> API, so this rides the private endpoints `blinkpy` maintains; Amazon can change
> them at any time. Run it on hardware you control.

---

## Important: where this runs

Cipher must run on a machine **you control that can reach the internet** — a
home server, a mini PC, a Raspberry Pi, a NAS, or your laptop. It does **not**
need to be on the same LAN as the sync module (Blink is cloud-relayed), but the
one-time login sends a **2FA code to your email/phone**, so a human has to be
present for that first step.

Do **not** run the authentication step on a shared/untrusted host — the saved
`blink_session.json` holds account tokens.

---

## Setup (once)

```bash
# 1. Install Python deps (Python 3.11+)
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt

# 2. Install ffmpeg (required for live view only)
#    macOS:  brew install ffmpeg
#    Debian: sudo apt-get install ffmpeg

# 3. Authenticate to Blink (handles 2FA, saves session tokens)
python authenticate.py
```

`authenticate.py` will prompt for your Blink email + password, then the 2FA
code Blink sends you. On success it writes `blink_session.json` (gitignored) and
lists the cameras it found — you should see your three.

## Run

```bash
./run.sh
# then open http://localhost:8000
# from another device on your network: http://<this-host-ip>:8000
```

---

## How it works

```
authenticate.py ──► blink_session.json (refreshable tokens)
                          │
                          ▼
        app/blink_manager.py ── blinkpy ──► Blink cloud
          • camera list + state                 │
          • snapshots (get_media)               │
                          │                     │
        app/livestream_manager.py               │
          • camera.init_livestream()  ◄─────────┘  immis:// TS server
          • blinkpy runs a local TCP server emitting MPEG-TS
          • ffmpeg  tcp://… ─► HLS segments
                          │
        app/main.py (FastAPI)  ──►  app/static/index.html (dashboard)
                                     • snapshot grid + hls.js live player
```

### API

| Method | Path | Purpose |
|--------|------|---------|
| GET  | `/api/health` | readiness + camera names |
| GET  | `/api/cameras` | camera list + state |
| POST | `/api/refresh` | force state refresh |
| GET  | `/api/cameras/{name}/snapshot.jpg?fresh=0\|1` | JPEG snapshot |
| POST | `/api/cameras/{name}/live/start` | start live → `{playlist_url}` |
| POST | `/api/live/{stream_id}/stop` | stop a live session |
| GET  | `/live/{stream_id}/index.m3u8` | HLS playlist/segments |

### Tuning (env vars)

| Var | Default | Meaning |
|-----|---------|---------|
| `CIPHER_PORT` | `8000` | HTTP port |
| `CIPHER_FRESH_WAIT` | `5.0` | seconds to wait after a forced capture |
| `CIPHER_LIVE_IDLE_TIMEOUT` | `20` | stop live after N s with no viewer |
| `CIPHER_LIVE_MAX_DURATION` | `180` | hard cap per live session (battery guard) |
| `BLINK_SESSION_FILE` | `blink_session.json` | session token path |

---

## Notes & limits

- **Battery cameras**: live view and forced (`fresh`) snapshots wake the camera
  and use battery. The dashboard defaults to cached thumbnails on a 15s cadence;
  live sessions auto-stop when you navigate away / after `CIPHER_LIVE_MAX_DURATION`.
- **Live view support** depends on the camera returning an `immis://` stream. If
  a model doesn't, the API responds `409` and you fall back to snapshots.
- **One live session per camera** at a time (Blink-side constraint).
- This is a personal-use project against an unofficial API — expect occasional
  breakage when Blink changes things, and update `blinkpy` when that happens.

## Security

- `blink_session.json` = account tokens. Gitignored. Don't commit or share it.
- Cipher has no auth of its own — bind it to your LAN, or put it behind a
  reverse proxy / VPN / auth layer before exposing it beyond your network.
