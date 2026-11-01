"""Local HTTP server: JSON API under /api/*, static single-page UI from static/.

Every handler exception becomes {"error": "<sentence>"} with a 4xx or 500 status. A
traceback is logged to the terminal, never sent to the browser.
"""

from __future__ import annotations

import json
import mimetypes
from contextlib import closing
import re
import sys
import threading
import traceback
import webbrowser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

from . import NotFound, UserError, __version__, media, meta, prompts, settings as settings_mod, tracks
from .db import connect

STATIC_DIR = Path(__file__).parent / "static"
MAX_BODY = 2 * 1024 * 1024


def _draft_rows(result: dict) -> dict:
    """Turn a metadata result into draft rows: kind -> list of {label, text}."""
    return {
        "title": [{"label": f"Title {i}", "text": t} for i, t in enumerate(result["titles"], 1)],
        "description": [{"label": "Short description", "text": result["description_short"]},
                        {"label": "Long description", "text": result["description_long"]}],
        "hashtags": [{"label": f"{name.capitalize() if name != 'youtube' else 'YouTube'} hashtags",
                      "text": " ".join(tags)} for name, tags in result["hashtags"].items()],
        "tags": [{"label": "YouTube tags", "text": result["youtube_tags"]},
                 {"label": "Distributor note", "text": result["distributor_note"]}],
    }


class App:
    """Routing and request handling, independent of the socket layer (testable)."""

    def __init__(self, base: Path | None = None):
        self.base = Path(base) if base else settings_mod.data_dir()
        self.routes: list[tuple[str, re.Pattern, callable]] = []
        self._register()

    # -- plumbing ---------------------------------------------------------------

    def add_route(self, method: str, pattern: str, fn) -> None:
        self.routes.append((method, re.compile("^" + pattern + "$"), fn))

    def db(self):
        return closing(connect(self.base))

    def settings(self) -> dict:
        return settings_mod.load(self.base)

    def dispatch(self, method: str, path: str, query: dict, body) -> tuple[int, dict]:
        for m, pat, fn in self.routes:
            match = pat.match(path)
            if match and m == method:
                try:
                    result = fn(query=query, body=body, **match.groupdict())
                    return 200, result
                except UserError as exc:
                    return exc.status, {"error": str(exc)}
                except Exception as exc:  # noqa: BLE001 - last line of defence
                    print(f"[releaseline] unexpected error on {method} {path}:", file=sys.stderr)
                    traceback.print_exc()
                    return 500, {"error": f"Something went wrong on the server ({type(exc).__name__}). "
                                          "Details are in the terminal where ReleaseLine is running."}
        if any(pat.match(path) for _, pat, _ in self.routes):
            return 405, {"error": f"{method} is not allowed on {path}."}
        return 404, {"error": f"There is no API endpoint at {path}."}

    @staticmethod
    def _need(body, *keys):
        if not isinstance(body, dict):
            raise UserError("Send a JSON object.")
        for k in keys:
            if k not in body:
                raise UserError(f"The field '{k}' is required.")
        return body

    @staticmethod
    def _text(body: dict, key: str, default: str = "", max_len: int = tracks.MAX_TEXT) -> str:
        """A string field (None/missing -> default); anything else is a 400 sentence."""
        return tracks.text_value(body.get(key, default), key, max_len) if body.get(key) is not None else default

    @staticmethod
    def _number(body: dict, key: str, default=None):
        """A number-ish field: int, float or a string the generator can parse. No lists,
        objects or booleans."""
        v = body.get(key)
        if v is None or v == "":
            return default
        if isinstance(v, bool) or not isinstance(v, (int, float, str)):
            raise UserError(f"The field '{key}' should be a number.")
        return v

    def _track(self, conn, ident) -> dict:
        return tracks.get_track(conn, ident)

    # -- routes -------------------------------------------------------------------

    def _register(self):
        r = self.add_route
        r("GET", "/api/status", self.status)
        r("GET", "/api/dashboard", self.dashboard)
        r("GET", "/api/tracks", self.list_tracks)
        r("POST", "/api/tracks", self.create_track)
        r("GET", r"/api/tracks/(?P<ident>[^/]+)", self.get_track)
        r("PATCH", r"/api/tracks/(?P<ident>[^/]+)", self.update_track)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/stages/(?P<key>[a-z]+)", self.update_stage)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/prompts", self.gen_prompts)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/prompts/choose", self.choose_prompt)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/cut", self.cut)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/assemble", self.assemble)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/meta", self.gen_meta)
        r("PATCH", r"/api/tracks/(?P<ident>[^/]+)/drafts/(?P<draft_id>\d+)", self.update_draft)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/posts", self.set_post)
        r("POST", r"/api/tracks/(?P<ident>[^/]+)/release", self.release)
        r("GET", "/api/platforms", self.get_platforms)
        r("PUT", "/api/platforms", self.put_platforms)
        r("GET", "/api/settings", self.get_settings)
        r("PUT", "/api/settings", self.put_settings)
        r("POST", "/api/ffmpeg/test", self.ffmpeg_test)
        r("GET", "/api/feedback", self.list_feedback)
        r("POST", "/api/feedback", self.add_feedback)

    def status(self, **_):
        s = self.settings()
        return {"version": __version__, "data_dir": str(self.base),
                "library_root": str(settings_mod.library_root(s, self.base)),
                "ffmpeg": media.ffmpeg_status(s)}

    def dashboard(self, **_):
        with self.db() as conn:
            try:
                s = self.settings()
            except UserError:
                s = dict(settings_mod.DEFAULTS)  # a broken settings.json must not hide the Dashboard
            return tracks.dashboard(conn, s, self.base)

    def list_tracks(self, **_):
        with self.db() as conn:
            return {"tracks": tracks.list_tracks(conn)}

    def create_track(self, body=None, **_):
        self._need(body, "title")
        with self.db() as conn:
            return tracks.create_track(conn, self.settings(), body, self.base)

    def get_track(self, ident, **_):
        s = self.settings()
        with self.db() as conn:
            t = self._track(conn, ident)
            stages = tracks.get_stages(conn, t["id"])
            return {
                "track": t, "stages": stages,
                "current_stage": tracks.current_stage_key(stages),
                "assets": tracks.assets_report(conn, t, s, self.base),
                "drafts": tracks.drafts_for(conn, t["id"]),
                "posts": tracks.posts_for(conn, t["id"]),
                "platforms": tracks.platforms(conn, enabled_only=True),
                "ffmpeg": media.find_ffmpeg(s.get("ffmpeg_path", "")),
                "defaults": {k: s[k] for k in ("aspect", "shot_len", "clip_len", "fade")},
            }

    def update_track(self, ident, body=None, **_):
        self._need(body)
        with self.db() as conn:
            t = self._track(conn, ident)
            return tracks.update_track(conn, t["id"], body, self.settings())

    def update_stage(self, ident, key, body=None, **_):
        body = self._need(body)
        state = body.get("state")
        if state is not None and not isinstance(state, str):
            raise UserError("A stage can be todo, done or skipped.")
        with self.db() as conn:
            t = self._track(conn, ident)
            stage = tracks.update_stage(conn, t["id"], key, state=state, data=body.get("data"))
            return {"stage": stage, "stages": tracks.get_stages(conn, t["id"]),
                    "assets": tracks.assets_report(conn, t, self.settings(), self.base)}

    def gen_prompts(self, ident, body=None, **_):
        body = body or {}
        self._need(body)
        s = self.settings()
        preset = self._text(body, "preset", "cinematic", 100) or "cinematic"
        setting = self._text(body, "setting", "", 500)
        aspect = self._text(body, "aspect", s["aspect"], 10) or s["aspect"]
        shot_len = self._number(body, "shot_len", s["shot_len"]) or s["shot_len"]
        clip_len = self._number(body, "clip_len", s["clip_len"]) or s["clip_len"]
        n = self._number(body, "n", 4) or 4
        with self.db() as conn:
            t = self._track(conn, ident)
            result = prompts.generate(
                t, preset=preset, setting=setting, aspect=aspect, shot_len=shot_len,
                clip_len=clip_len, n=n, seed=self._number(body, "seed"),
                shots=self._number(body, "shots"))
            tracks.update_stage(conn, t["id"], "prompt", data={
                "preset": result["preset"], "setting": result["setting"], "aspect": result["aspect"],
                "shot_len": result["shot_len"], "clip_len": result["clip_len"], "shots": result["shots"],
                "seed": result["seed"]}, verify_paths=False)
            return result

    def choose_prompt(self, ident, body=None, **_):
        body = self._need(body, "variant")
        v = body["variant"]
        if not isinstance(v, dict) or not isinstance(v.get("prompt"), str) or not v["prompt"].strip():
            raise UserError("Pick one of the generated variants to save.")
        seed = self._number(body, "seed")
        if isinstance(seed, str):
            seed = int(seed) if seed.strip().lstrip("-").isdigit() else None
        if isinstance(seed, float):
            seed = int(seed)
        shots = v.get("shots") or []
        bad_shape = UserError("That variant is not in the shape ReleaseLine generates. Press Generate "
                              "again and pick one of the variants shown.")
        if not isinstance(shots, list) or not all(isinstance(sh, dict) for sh in shots):
            raise bad_shape
        for k in ("label", "negative"):
            if not isinstance(v.get(k, ""), str):
                raise bad_shape
        tracks.text_value(v["prompt"], "prompt")
        with self.db() as conn:
            t = self._track(conn, ident)
            tracks.replace_drafts(conn, t["id"], "prompt", [], seed)
            tracks.replace_drafts(conn, t["id"], "shotlist", [], seed)
            conn.execute("DELETE FROM drafts WHERE track_id = ? AND kind IN ('prompt','shotlist')", (t["id"],))
            tracks.save_draft(conn, t["id"], "prompt", {
                "label": f"Prompt: {v.get('label', 'chosen')}", "text": v["prompt"],
                "negative": v.get("negative", ""), "aspect": v.get("aspect"), "duration": v.get("duration")},
                chosen=True, seed=seed)
            if shots:
                text = "\n\n".join(f"Shot {sh.get('n', i + 1)}: {sh.get('prompt', '')}" for i, sh in enumerate(shots))
                tracks.save_draft(conn, t["id"], "shotlist", {"label": "Shot list", "text": text, "shots": shots},
                                  chosen=True, seed=seed)
            tracks.update_stage(conn, t["id"], "prompt", data={"chosen_label": v.get("label", "")},
                                verify_paths=False)
            return {"drafts": tracks.drafts_for(conn, t["id"]), "stages": tracks.get_stages(conn, t["id"])}

    def cut(self, ident, body=None, **_):
        body = self._need(body, "start")
        s = self.settings()
        with self.db() as conn:
            t = self._track(conn, ident)
            result = media.cut_clip(conn, s, t, self._number(body, "start", body["start"]),
                                    self._number(body, "length"), self._number(body, "fade"),
                                    self._text(body, "src"), self.base)
            result["stages"] = tracks.get_stages(conn, t["id"])
            result["assets"] = tracks.assets_report(conn, t, s, self.base)
            return result

    def assemble(self, ident, body=None, **_):
        body = body or {}
        s = self.settings()
        with self.db() as conn:
            t = self._track(conn, ident)
            aspect = self._text(body, "aspect", "", 10) if isinstance(body, dict) else ""
            result = media.assemble(conn, s, t, aspect or None, self.base)
            result["stages"] = tracks.get_stages(conn, t["id"])
            result["assets"] = tracks.assets_report(conn, t, s, self.base)
            return result

    def gen_meta(self, ident, body=None, **_):
        body = body or {}
        explicit = bool(body.get("explicit")) if isinstance(body, dict) else False
        with self.db() as conn:
            t = self._track(conn, ident)
            result = meta.generate(t, self.settings(), explicit=explicit)
            for kind, rows_ in _draft_rows(result).items():
                tracks.replace_drafts(conn, t["id"], kind, rows_)
            tracks.update_stage(conn, t["id"], "meta", data={"explicit": explicit}, verify_paths=False)
            result["drafts"] = tracks.drafts_for(conn, t["id"])
            return result

    def update_draft(self, ident, draft_id, body=None, **_):
        body = self._need(body)
        did = tracks.id_value(draft_id, "draft id")
        text = self._text(body, "text") if "text" in body else None
        with self.db() as conn:
            t = self._track(conn, ident)
            d = tracks.update_draft(conn, t["id"], did, None, None)
            try:
                content = json.loads(d["content"])
            except ValueError:
                content = {"label": d["kind"], "text": d["content"]}
            if not isinstance(content, dict):
                content = {"label": d["kind"], "text": str(content)}
            if text is not None:
                content["text"] = text
            chosen = body.get("chosen")
            d = tracks.update_draft(conn, t["id"], did, json.dumps(content),
                                    None if chosen is None else bool(chosen))
            return {"draft": d, "drafts": tracks.drafts_for(conn, t["id"])}

    def set_post(self, ident, body=None, **_):
        body = self._need(body, "platform_id")
        pid = tracks.id_value(body["platform_id"], "platform_id")
        url = self._text(body, "url")
        with self.db() as conn:
            t = self._track(conn, ident)
            result = tracks.set_post(conn, t["id"], pid, url)
            result["track"] = tracks.get_track(conn, t["id"])
            result["stages"] = tracks.get_stages(conn, t["id"])
            return result

    def release(self, ident, **_):
        with self.db() as conn:
            t = self._track(conn, ident)
            track = tracks.release(conn, t["id"])
            return {"track": track, "stages": tracks.get_stages(conn, t["id"])}

    def get_platforms(self, **_):
        with self.db() as conn:
            return {"platforms": tracks.platforms(conn)}

    def put_platforms(self, body=None, **_):
        items = body.get("platforms") if isinstance(body, dict) else body
        with self.db() as conn:
            return {"platforms": tracks.update_platforms(conn, items)}

    def get_settings(self, **_):
        s = self.settings()
        return {"settings": s, "library_root_resolved": str(settings_mod.library_root(s, self.base)),
                "data_dir": str(self.base), "ffmpeg": media.ffmpeg_status(s)}

    def put_settings(self, body=None, **_):
        body = self._need(body)
        s = settings_mod.save(body, self.base)
        return {"settings": s, "library_root_resolved": str(settings_mod.library_root(s, self.base)),
                "data_dir": str(self.base), "ffmpeg": media.ffmpeg_status(s)}

    def ffmpeg_test(self, body=None, **_):
        path = self._text(body, "path") if isinstance(body, dict) else ""
        return media.ffmpeg_status({"ffmpeg_path": path})

    def list_feedback(self, **_):
        with self.db() as conn:
            return {"feedback": tracks.list_feedback(conn)}

    def add_feedback(self, body=None, **_):
        body = self._need(body, "text")
        with self.db() as conn:
            return tracks.add_feedback(conn, body["text"], body.get("screen", ""), self.base)


def make_handler(app: App):
    class Handler(BaseHTTPRequestHandler):
        server_version = f"ReleaseLine/{__version__}"
        protocol_version = "HTTP/1.1"

        def log_message(self, fmt, *args):  # quiet unless it is an error
            if args and str(args[1:2] and args[1]).startswith(("4", "5")):
                sys.stderr.write("[releaseline] " + (fmt % args) + "\n")

        def _send_json(self, status: int, payload):
            data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(data)

        def _read_body(self):
            try:
                length = int(self.headers.get("Content-Length") or 0)
            except ValueError:
                raise UserError("The Content-Length header was not a number.")
            if length > MAX_BODY:
                raise UserError("That request is too large.")
            raw = self.rfile.read(length) if length else b""
            if not raw.strip():
                return {}
            try:
                return json.loads(raw.decode("utf-8"))
            except (ValueError, UnicodeDecodeError):
                raise UserError("The request body was not valid JSON.")

        def _api(self, method: str):
            parts = urlsplit(self.path)
            query = {k: v[-1] for k, v in parse_qs(parts.query).items()}
            try:
                body = self._read_body() if method in ("POST", "PUT", "PATCH") else None
            except UserError as exc:
                self._send_json(exc.status, {"error": str(exc)})
                return
            status, payload = app.dispatch(method, parts.path, query, body)
            self._send_json(status, payload)

        def _static(self):
            path = urlsplit(self.path).path
            name = "index.html" if path in ("", "/") else path.lstrip("/")
            target = (STATIC_DIR / name).resolve()
            if STATIC_DIR.resolve() not in target.parents or not target.is_file():
                if path.startswith("/api/"):
                    self._send_json(404, {"error": f"There is no API endpoint at {path}."})
                    return
                self._send_json(404, {"error": "Not found. The app lives at /."})
                return
            ctype = mimetypes.guess_type(str(target))[0] or "application/octet-stream"
            if ctype.startswith("text/") or ctype == "application/javascript":
                ctype += "; charset=utf-8"
            data = target.read_bytes()
            self.send_response(200)
            self.send_header("Content-Type", ctype)
            self.send_header("Content-Length", str(len(data)))
            self.send_header("Cache-Control", "no-cache")
            self.end_headers()
            self.wfile.write(data)

        def do_GET(self):
            if urlsplit(self.path).path.startswith("/api/"):
                self._api("GET")
            else:
                self._static()

        def do_POST(self):
            self._api("POST")

        def do_PUT(self):
            self._api("PUT")

        def do_PATCH(self):
            self._api("PATCH")

        def do_DELETE(self):
            self._api("DELETE")

    return Handler


def make_server(app: App, host: str = "127.0.0.1", port: int = 0) -> ThreadingHTTPServer:
    try:
        srv = ThreadingHTTPServer((host, port), make_handler(app))
    except OSError as exc:
        if getattr(exc, "errno", None) in (98, 48, 10048) or "in use" in str(exc).lower():
            raise UserError(f"Port {port} is already in use. Start with --port {port + 1} "
                            "or stop the other program using it.")
        raise UserError(f"Could not start the server on {host}:{port} ({exc.strerror or exc}).")
    srv.daemon_threads = True
    return srv


def lan_addresses() -> list[str]:
    """Best-effort list of this machine's non-loopback IPv4 addresses (for phone access)."""
    import socket
    found: list[str] = []
    try:
        for info in socket.getaddrinfo(socket.gethostname(), None, socket.AF_INET):
            ip = info[4][0]
            if not ip.startswith("127.") and ip not in found:
                found.append(ip)
    except OSError:
        pass
    if not found:
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
                s.connect(("10.255.255.255", 1))  # no packets are sent; picks the LAN interface
                ip = s.getsockname()[0]
                if not ip.startswith("127."):
                    found.append(ip)
        except OSError:
            pass
    return found


def serve(port: int, open_browser: bool = True, base: Path | None = None,
          host: str = "127.0.0.1") -> None:
    app = App(base)
    with app.db():  # fail early with a sentence if the database is unusable
        pass
    srv = make_server(app, host=host, port=port)
    real_port = srv.server_address[1]
    url = f"http://127.0.0.1:{real_port}/"
    print(f"ReleaseLine is running at {url}")
    if host not in ("127.0.0.1", "localhost"):
        ips = lan_addresses()
        if ips:
            print("On your phone (same Wi-Fi), open: " + "  or  ".join(f"http://{ip}:{real_port}/" for ip in ips))
        else:
            print(f"On your phone (same Wi-Fi), open http://<this computer's IP>:{real_port}/")
        print("Anyone on this network can open the app while it runs with --host. Stop it with Ctrl+C when done.")
    print(f"Data folder: {app.base}")
    print("Press Ctrl+C to stop.")
    if open_browser:
        threading.Timer(0.6, lambda: webbrowser.open(url)).start()
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\nStopped.")
    finally:
        srv.server_close()
