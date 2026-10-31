import json
import re
import threading
import urllib.error
import urllib.request

from releaseline import server as server_mod
from tests.helpers import TempDataCase


class ServerTests(TempDataCase):
    def setUp(self):
        super().setUp()
        self.app = server_mod.App(self.data)
        self.srv = server_mod.make_server(self.app, port=0)
        self.port = self.srv.server_address[1]
        self.thread = threading.Thread(target=self.srv.serve_forever, daemon=True)
        self.thread.start()
        self.addCleanup(self._stop)

    def _stop(self):
        self.srv.shutdown()
        self.srv.server_close()

    def req(self, method, path, body=None, raw=None):
        url = f"http://127.0.0.1:{self.port}{path}"
        data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
        r = urllib.request.Request(url, data=data, method=method, headers={"Content-Type": "application/json"} if data else {})
        try:
            with urllib.request.urlopen(r, timeout=30) as resp:
                return resp.status, resp.read(), resp.headers
        except urllib.error.HTTPError as e:
            return e.code, e.read(), e.headers

    def js(self, method, path, body=None, raw=None):
        status, data, headers = self.req(method, path, body, raw)
        self.assertIn("application/json", headers.get("Content-Type", ""), (path, data[:200]))
        return status, json.loads(data.decode("utf-8"))

    def new_track(self, **fields):
        fields.setdefault("title", "Night Shift")
        status, t = self.js("POST", "/api/tracks", fields)
        self.assertEqual(status, 200, t)
        return t

    # ---- static -------------------------------------------------------------------

    def test_index_and_assets_are_local_only(self):
        status, html, headers = self.req("GET", "/")
        self.assertEqual(status, 200)
        self.assertIn("text/html", headers["Content-Type"])
        text = html.decode("utf-8")
        self.assertIn("ReleaseLine organizes your release steps and drafts text from your inputs. "
                      "It cannot predict or promise how a release will perform.", text)
        self.assertIn('id="feedback-open"', text)
        refs = re.findall(r'(?:src|href)="([^"]+)"', text)
        for ref in refs:
            self.assertFalse(ref.startswith(("http://", "https://", "//")), ref)
        for ref in ("/app.css", "/app.js"):
            self.assertIn(ref, refs)
            status, body, headers = self.req("GET", ref)
            self.assertEqual(status, 200, ref)
            self.assertNotIn("http://", body.decode("utf-8").replace("http://127.0.0.1", "").replace("https://", "").replace("http:// or", ""))
        js = self.req("GET", "/app.js")[1].decode("utf-8")
        self.assertNotRegex(js, r"fetch\(\s*[\"']https?://")
        for word in ("viral", "guaranteed", "grow your audience"):
            self.assertNotIn(word, (text + js).lower())

    def test_static_404_and_traversal(self):
        status, body = self.js("GET", "/nope.css")
        self.assertEqual(status, 404)
        status, _ = self.js("GET", "/../releaseline/db.py")
        self.assertEqual(status, 404)
        status, body = self.js("GET", "/api/nope")
        self.assertEqual(status, 404)
        self.assertIn("error", body)

    # ---- every GET endpoint ---------------------------------------------------------

    def test_get_endpoints(self):
        t = self.new_track(genre="trap")
        for path in ("/api/status", "/api/dashboard", "/api/tracks", f"/api/tracks/{t['id']}",
                     "/api/tracks/night-shift", "/api/platforms", "/api/settings", "/api/feedback"):
            status, body = self.js("GET", path)
            self.assertEqual(status, 200, path)
            self.assertNotIn("error", body, path)
        status, body = self.js("GET", "/api/status")
        self.assertIn("ffmpeg", body)
        self.assertIn("found", body["ffmpeg"])
        status, body = self.js("GET", "/api/dashboard")
        self.assertEqual(body["counts"]["total"], 1)
        status, body = self.js("GET", "/api/tracks/1")
        self.assertEqual(len(body["stages"]), 8)
        self.assertEqual(body["current_stage"], "download")
        self.assertEqual(len(body["platforms"]), 5)
        self.assertIn("on_disk", body["assets"])

    def test_empty_dashboard(self):
        status, body = self.js("GET", "/api/dashboard")
        self.assertEqual(status, 200)
        self.assertEqual(body["counts"]["total"], 0)
        self.assertTrue(all(not g["tracks"] for g in body["groups"]))

    # ---- core loop -----------------------------------------------------------------

    def test_create_stage_prompts_meta_feedback(self):
        t = self.new_track(genre="dark trap", mood="menacing", energy="high", bpm="140")
        self.assertEqual(t["slug"], "night-shift")
        self.assertTrue((self.data / "library" / "night-shift" / "video").is_dir())

        status, body = self.js("POST", f"/api/tracks/{t['id']}/stages/download", {"state": "done"})
        self.assertEqual(status, 200)
        self.assertEqual(body["stage"]["state"], "done")
        status, body = self.js("POST", f"/api/tracks/{t['id']}/stages/master", {"state": "skipped"})
        self.assertEqual(body["stage"]["state"], "skipped")
        status, body = self.js("GET", f"/api/tracks/{t['id']}")
        self.assertEqual(body["current_stage"], "clip")

        status, body = self.js("POST", f"/api/tracks/{t['id']}/stages/download", {"data": {"path": "/no/such/file.wav"}})
        self.assertEqual(status, 400)
        self.assertIn("File not found", body["error"])

        status, p = self.js("POST", f"/api/tracks/{t['id']}/prompts", {"preset": "anime", "seed": 11, "n": 3})
        self.assertEqual(status, 200, p)
        self.assertEqual(len(p["variants"]), 3)
        self.assertEqual(p["seed"], 11)
        status, p2 = self.js("POST", f"/api/tracks/{t['id']}/prompts", {"preset": "anime", "seed": 11, "n": 3})
        self.assertEqual(p["variants"], p2["variants"])
        status, body = self.js("POST", f"/api/tracks/{t['id']}/prompts", {"preset": "claymation"})
        self.assertEqual(status, 400)
        status, body = self.js("POST", f"/api/tracks/{t['id']}/prompts/choose", {"variant": p["variants"][1], "seed": 11})
        self.assertEqual(status, 200, body)
        kinds = [d["kind"] for d in body["drafts"] if d["chosen"]]
        self.assertIn("prompt", kinds)
        self.assertIn("shotlist", kinds)

        status, m = self.js("POST", f"/api/tracks/{t['id']}/meta", {"explicit": True})
        self.assertEqual(status, 200)
        self.assertEqual(len(m["titles"]), 3)
        self.assertTrue(any(d["kind"] == "title" for d in m["drafts"]))
        draft = next(d for d in m["drafts"] if d["kind"] == "title")
        status, body = self.js("PATCH", f"/api/tracks/{t['id']}/drafts/{draft['id']}", {"text": "My edited title"})
        self.assertEqual(status, 200)
        self.assertIn("My edited title", body["draft"]["content"])
        status, body = self.js("PATCH", f"/api/tracks/{t['id']}/drafts/999999", {"text": "x"})
        self.assertEqual(status, 404)

        status, body = self.js("POST", "/api/feedback", {"text": "Copy buttons are small", "screen": "track"})
        self.assertEqual(status, 200)
        self.assertIn("Copy buttons are small", (self.data / "feedback.md").read_text(encoding="utf-8"))
        status, body = self.js("GET", "/api/feedback")
        self.assertEqual(body["feedback"][0]["screen"], "track")
        status, body = self.js("POST", "/api/feedback", {"text": "   "})
        self.assertEqual(status, 400)

    def test_posts_release_and_settings(self):
        t = self.new_track()
        status, body = self.js("GET", "/api/platforms")
        plats = body["platforms"]
        status, body = self.js("PUT", "/api/platforms", {"platforms": [{"id": p["id"], "enabled": p["name"] == "YouTube"} for p in plats]})
        self.assertEqual(status, 200)
        status, body = self.js("POST", f"/api/tracks/{t['id']}/posts", {"platform_id": plats[0]["id"], "url": "not a url"})
        self.assertEqual(status, 400)
        status, body = self.js("POST", f"/api/tracks/{t['id']}/posts", {"platform_id": plats[0]["id"], "url": "https://youtu.be/x"})
        self.assertEqual(status, 200, body)
        self.assertTrue(body["released"])
        self.assertEqual(body["track"]["status"], "released")
        t2 = self.new_track(title="Second")
        status, body = self.js("POST", f"/api/tracks/{t2['id']}/release")
        self.assertEqual(body["track"]["status"], "released")
        status, body = self.js("PATCH", f"/api/tracks/{t2['id']}", {"status": "active", "notes": "reopened"})
        self.assertEqual(body["status"], "active")
        status, body = self.js("PUT", "/api/settings", {"artist": "Fenn", "aspect": "16:9", "clip_len": 20})
        self.assertEqual(status, 200)
        self.assertEqual(body["settings"]["artist"], "Fenn")
        status, body = self.js("PUT", "/api/settings", {"aspect": "4:3"})
        self.assertEqual(status, 400)
        status, body = self.js("POST", "/api/ffmpeg/test", {"path": "/definitely/not/here"})
        self.assertEqual(status, 200)
        self.assertFalse(body["found"])

    def test_cut_and_assemble_without_ffmpeg_show_commands(self):
        self.js("PUT", "/api/settings", {"ffmpeg_path": "/definitely/not/here/ffmpeg"})
        t = self.new_track()
        audio = self.tmp / "song.wav"
        audio.write_bytes(b"RIFF")
        self.js("POST", f"/api/tracks/{t['id']}/stages/download", {"data": {"path": str(audio)}})
        status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": "0:30"})
        self.assertEqual(status, 200, body)
        self.assertFalse(body["ran"])
        self.assertIn("ffmpeg", body["commands"][0])
        status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": "x:y"})
        self.assertEqual(status, 400)
        self.assertIn("not a time", body["error"])
        status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {})
        self.assertEqual(status, 400)
        status, body = self.js("POST", f"/api/tracks/{t['id']}/assemble", {})
        self.assertEqual(status, 400)
        self.assertIn("Cut the clip first", body["error"])

    # ---- error paths -----------------------------------------------------------------

    def test_invalid_json_and_missing_track(self):
        status, body = self.js("POST", "/api/tracks", raw=b"{not json")
        self.assertEqual(status, 400)
        self.assertIn("not valid JSON", body["error"])
        status, body = self.js("POST", "/api/tracks", {"title": ""})
        self.assertEqual(status, 400)
        status, body = self.js("POST", "/api/tracks", raw=b"[1,2]")
        self.assertEqual(status, 400)
        for path in ("/api/tracks/999", "/api/tracks/no-such-slug"):
            status, body = self.js("GET", path)
            self.assertEqual(status, 404, path)
            self.assertIn("No track called", body["error"])
        status, body = self.js("POST", "/api/tracks/999/prompts", {})
        self.assertEqual(status, 404)
        status, body = self.js("DELETE", "/api/tracks")
        self.assertEqual(status, 405)

    def test_handler_exception_becomes_json_500_without_traceback(self):
        def boom(**_):
            raise RuntimeError("secret internal detail")
        self.app.add_route("GET", "/api/_boom", boom)
        import io, sys
        old = sys.stderr
        sys.stderr = io.StringIO()
        try:
            status, data, headers = self.req("GET", "/api/_boom")
        finally:
            logged = sys.stderr.getvalue()
            sys.stderr = old
        self.assertEqual(status, 500)
        self.assertIn("application/json", headers["Content-Type"])
        body = json.loads(data)
        self.assertIn("Something went wrong", body["error"])
        self.assertNotIn("Traceback", data.decode())
        self.assertNotIn("secret internal detail", data.decode())
        self.assertIn("Traceback", logged)  # it went to the terminal instead

    def test_unreadable_database_is_a_sentence(self):
        (self.data / "releaseline.db").write_text("this is not a database", encoding="utf-8")
        status, body = self.js("GET", "/api/dashboard")
        self.assertEqual(status, 400)
        self.assertIn("releaseline.db", body["error"])
        self.assertNotIn("Traceback", body["error"])

    def test_port_in_use_is_a_sentence(self):
        from releaseline import UserError
        with self.assertRaises(UserError) as cm:
            server_mod.make_server(self.app, port=self.port)
        self.assertIn("already in use", str(cm.exception))
