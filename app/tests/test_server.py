import json
import re
import shutil
import threading
import unittest
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from unittest import mock

from releaseline import media, server as server_mod
from tests.helpers import TempDataCase
from tests.test_media import FFMPEG, make_test_media


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

    @unittest.skipUnless(FFMPEG["found"], "ffmpeg is not installed on this machine; the HTTP cut/assemble "
                                          "execution test is skipped (the no-ffmpeg path above still ran).")
    def test_cut_and_assemble_with_ffmpeg_over_http(self):
        media_dir = self.tmp / "media with spaces"
        media_dir.mkdir()
        audio, v1, v2 = make_test_media(media_dir)  # 3 s tone, two 2 s videos
        t = self.new_track()
        self.js("POST", f"/api/tracks/{t['id']}/stages/master", {"data": {"path": str(audio)}})
        clip_dir = self.data / "library" / "night-shift" / "clip"

        # B1: start past the end -> 400 sentence, nothing written, stage still todo
        for start in ("0:10", "3", "0:03"):
            status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": start, "length": 2})
            self.assertEqual(status, 400, body)
            self.assertIn("is past the end of the file (0:03 long). Pick an earlier start.", body["error"])
        self.assertEqual(list(clip_dir.iterdir()), [])
        status, body = self.js("GET", f"/api/tracks/{t['id']}")
        self.assertEqual(next(s for s in body["stages"] if s["key"] == "clip")["state"], "todo")
        self.assertEqual([a["kind"] for a in body["assets"]["recorded"]], ["master"])

        # verification path: ffmpeg "succeeds" but writes a header-only file
        real_run = media.run

        def fake_run(cmd, timeout=600):
            parts = [c for c in cmd if ".part." in str(c)]
            if parts:
                for c in parts:
                    Path(c).write_bytes(b"RIFF" + b"\0" * 74)
                return media.RunResult(True, 0, "", "", 0.1)
            return real_run(cmd, timeout)

        with mock.patch.object(media, "run", fake_run):
            status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": "0:01", "length": 1, "fade": 0.1})
        self.assertEqual(status, 400, body)
        self.assertIn("empty clip", body["error"])
        self.assertEqual(list(clip_dir.iterdir()), [])

        # length past the remaining audio -> 200 with the shorter-clip note
        status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": "0:01", "length": 30, "fade": 0.2})
        self.assertEqual(status, 200, body)
        self.assertTrue(body["ran"])
        self.assertIn("The clip will be 0:02 long because the file ends there.", body["message"])
        self.assertTrue(Path(body["wav"]).is_file() and Path(body["mp3"]).is_file())
        self.assertEqual(next(s for s in body["stages"] if s["key"] == "clip")["state"], "done")
        self.assertEqual(sorted(a["kind"] for a in body["assets"]["recorded"]), ["clip", "clip", "master"])

        # a normal cut inside the file
        self.js("POST", f"/api/tracks/{t['id']}/stages/clip", {"state": "todo"})
        status, body = self.js("POST", f"/api/tracks/{t['id']}/cut", {"start": "0:00.5", "length": 2, "fade": 0.2})
        self.assertEqual(status, 200, body)
        self.assertEqual(body["message"], "Clip saved: night-shift-clip.wav and night-shift-clip.mp3.")

        # assemble from files dropped into video/
        shutil.copy(v1, self.data / "library" / "night-shift" / "video" / "shot-01.mp4")
        shutil.copy(v2, self.data / "library" / "night-shift" / "video" / "shot-02.mp4")
        status, body = self.js("POST", f"/api/tracks/{t['id']}/assemble", {"aspect": "9:16"})
        self.assertEqual(status, 200, body)
        self.assertTrue(body["ran"])
        self.assertTrue(Path(body["final"]).is_file())
        self.assertAlmostEqual(body["duration"], 2.0, delta=0.2)
        self.assertEqual(next(s for s in body["stages"] if s["key"] == "assemble")["state"], "done")

    def test_concurrent_same_title_creates_get_unique_slugs(self):
        def create(_):
            return self.js("POST", "/api/tracks", {"title": "Concurrent"})
        with ThreadPoolExecutor(max_workers=10) as pool:
            results = list(pool.map(create, range(10)))
        self.assertEqual([st for st, _ in results], [200] * 10, [b for st, b in results if st != 200])
        slugs = sorted(b["slug"] for _, b in results)
        self.assertEqual(len(set(slugs)), 10, slugs)
        self.assertIn("concurrent", slugs)
        self.assertIn("concurrent-10", slugs)
        for slug in slugs:
            self.assertTrue((self.data / "library" / slug / "source").is_dir(), slug)
        status, body = self.js("GET", "/api/dashboard")
        self.assertEqual(body["counts"]["total"], 10)

    def test_wrong_type_bodies_are_400_sentences(self):
        t = self.new_track()
        tid = t["id"]
        cases = [
            ("POST", f"/api/tracks/{tid}/prompts", {"preset": 7}),
            ("POST", f"/api/tracks/{tid}/prompts", {"preset": ["anime"]}),
            ("POST", f"/api/tracks/{tid}/prompts", {"setting": {}}),
            ("POST", f"/api/tracks/{tid}/prompts", {"seed": {}}),
            ("POST", f"/api/tracks/{tid}/prompts", {"n": [3]}),
            ("POST", f"/api/tracks/{tid}/prompts", {"aspect": 916}),
            ("POST", f"/api/tracks/{tid}/prompts/choose", {"variant": {"prompt": "x", "shots": "x"}}),
            ("POST", f"/api/tracks/{tid}/prompts/choose", {"variant": {"prompt": "x", "shots": [1, 2]}}),
            ("POST", f"/api/tracks/{tid}/prompts/choose", {"variant": {"prompt": "x"}, "seed": {}}),
            ("POST", f"/api/tracks/{tid}/prompts/choose", {"variant": {"prompt": 5}}),
            ("POST", f"/api/tracks/{tid}/cut", {"start": "0:01", "src": {}}),
            ("POST", f"/api/tracks/{tid}/cut", {"start": {}}),
            ("POST", f"/api/tracks/{tid}/cut", {"start": "0:01", "length": [30]}),
            ("POST", f"/api/tracks/{tid}/assemble", {"aspect": {}}),
            ("POST", "/api/ffmpeg/test", {"path": {}}),
            ("POST", "/api/ffmpeg/test", {"path": "x" * 10000}),
            ("PUT", "/api/platforms", {"platforms": [{"id": "abc"}]}),
            ("PUT", "/api/platforms", {"platforms": [{"id": {}}]}),
            ("PUT", "/api/platforms", {"platforms": [{"id": 1, "upload_url": 5}]}),
            ("PUT", "/api/settings", {"artist": {"a": 1}}),
            ("PUT", "/api/settings", {"library_root": ["x"]}),
            ("PUT", "/api/settings", {"aspect": ["9:16"]}),
            ("POST", "/api/tracks", {"title": 123}),
            ("POST", "/api/tracks", {"title": ["a"]}),
            ("POST", "/api/tracks", {"title": "ok", "notes": {"a": 1}}),
            ("POST", "/api/tracks", {"title": "ok", "notes": "n" * 5000}),
            ("PATCH", f"/api/tracks/{tid}", {"energy": ["high"]}),
            ("POST", f"/api/tracks/{tid}/stages/download", {"data": {"path": "x" * 10000}}),
            ("POST", f"/api/tracks/{tid}/stages/download", {"data": {"path": {"a": 1}}}),
            ("POST", f"/api/tracks/{tid}/stages/video", {"data": {"shots": "x"}}),
            ("POST", f"/api/tracks/{tid}/stages/video", {"data": {"shots": [{}]}}),
            ("POST", f"/api/tracks/{tid}/stages/download", {"state": ["done"]}),
            ("POST", f"/api/tracks/{tid}/posts", {"platform_id": "abc"}),
            ("POST", f"/api/tracks/{tid}/posts", {"platform_id": 1, "url": ["https://x"]}),
            ("POST", f"/api/tracks/{tid}/posts", {"platform_id": 1, "url": "https://" + "x" * 50000}),
            ("POST", "/api/feedback", {"text": ["a"]}),
            ("POST", "/api/feedback", {"text": "ok", "screen": {}}),
        ]
        for method, path, body in cases:
            status, resp = self.js(method, path, body)
            self.assertEqual(status, 400, (method, path, body, resp))
            self.assertNotIn("Something went wrong", resp["error"], (method, path, body))
            self.assertNotIn("Traceback", resp["error"])
        # ids that overflow a SQLite integer are "not found", not a 500
        for method, path, body in [
            ("GET", "/api/tracks/99999999999999999999999", None),
            ("PATCH", f"/api/tracks/{tid}/drafts/99999999999999999999", {"text": "x"}),
            ("POST", f"/api/tracks/{tid}/posts", {"platform_id": "99999999999999999999", "url": "https://x"}),
            ("PUT", "/api/platforms", {"platforms": [{"id": 99999999999999999999}]}),
            ("PUT", "/api/platforms", {"platforms": [{"id": 777}]}),
        ]:
            status, resp = self.js(method, path, body)
            self.assertEqual(status, 404, (method, path, resp))
            self.assertNotIn("Something went wrong", resp["error"])
        # a 10,000-char path is "too long", not an OSError
        status, resp = self.js("POST", f"/api/tracks/{tid}/stages/master", {"data": {"path": "/x/" + "y" * 10000}})
        self.assertEqual(status, 400)
        self.assertIn("too long", resp["error"])
        # bad Content-Length is a sentence, not a dropped connection
        import socket
        with socket.create_connection(("127.0.0.1", self.port), timeout=10) as sock:
            sock.sendall(b"POST /api/tracks HTTP/1.1\r\nHost: x\r\nContent-Length: abc\r\nConnection: close\r\n\r\n")
            raw_resp = b""
            while True:
                chunk = sock.recv(65536)
                if not chunk:
                    break
                raw_resp += chunk
        self.assertTrue(raw_resp.startswith(b"HTTP/1.1 400"), raw_resp[:200])
        self.assertIn(b"Content-Length header was not a number", raw_resp)
        # the track is untouched after all of that
        status, body = self.js("GET", f"/api/tracks/{tid}")
        self.assertEqual(status, 200)
        self.assertEqual(body["track"]["title"], "Night Shift")
        self.assertEqual(body["assets"]["recorded"], [])

    def test_deleted_database_shows_a_notice_about_unlinked_folders(self):
        self.new_track(title="Old One")
        self.new_track(title="Old Two")
        status, body = self.js("GET", "/api/dashboard")
        self.assertIsNone(body["notice"])
        for suffix in ("", "-wal", "-shm"):
            f = self.data / f"releaseline.db{suffix}"
            if f.exists():
                f.unlink()
        status, body = self.js("GET", "/api/dashboard")
        self.assertEqual(status, 200)
        self.assertEqual(body["counts"]["total"], 0)
        self.assertEqual(body["notice"], "The database was recreated; 2 track folders in "
                                         f"{self.data / 'library'} are not linked to any track.")
        # creating a track with one of the old titles links that folder again
        self.new_track(title="Old One")
        status, body = self.js("GET", "/api/dashboard")
        self.assertIn("1 track folder in", body["notice"])
        self.assertIn("is not linked", body["notice"])

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
