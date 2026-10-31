import json

from releaseline import UserError, NotFound, settings as settings_mod, tracks
from tests.helpers import TempDataCase


class SlugTests(TempDataCase):
    def test_slugify_basic_and_unicode(self):
        self.assertEqual(tracks.slugify("Night Shift"), "night-shift")
        self.assertEqual(tracks.slugify("  Café ÉLAN!! (v2) "), "cafe-elan-v2")
        self.assertEqual(tracks.slugify("???"), "track")
        self.assertLessEqual(len(tracks.slugify("x" * 200)), 60)

    def test_duplicate_titles_get_unique_slugs_and_folders(self):
        conn = self.connect()
        s = self.settings()
        a = tracks.create_track(conn, s, {"title": "Same"}, self.data)
        b = tracks.create_track(conn, s, {"title": "Same"}, self.data)
        self.assertEqual(a["slug"], "same")
        self.assertEqual(b["slug"], "same-2")
        for slug in ("same", "same-2"):
            for folder in tracks.LIBRARY_FOLDERS:
                self.assertTrue((self.data / "library" / slug / folder).is_dir(), folder)


class CreateTests(TempDataCase):
    def test_title_required(self):
        conn = self.connect()
        with self.assertRaises(UserError) as cm:
            tracks.create_track(conn, self.settings(), {"title": "   "}, self.data)
        self.assertIn("title", str(cm.exception).lower())

    def test_artist_defaults_from_settings_and_validation(self):
        conn = self.connect()
        settings_mod.save({"artist": "Fenn"}, self.data)
        t = tracks.create_track(conn, self.settings(), {"title": "A", "bpm": "140", "energy": "HIGH"}, self.data)
        self.assertEqual(t["artist"], "Fenn")
        self.assertEqual(t["energy"], "high")
        with self.assertRaises(UserError):
            tracks.create_track(conn, self.settings(), {"title": "B", "bpm": "fast"}, self.data)
        with self.assertRaises(UserError):
            tracks.create_track(conn, self.settings(), {"title": "B", "energy": "max"}, self.data)
        with self.assertRaises(UserError):
            tracks.create_track(conn, self.settings(), {"title": "B", "suno_url": "suno.com/x"}, self.data)

    def test_eight_stages_created_todo(self):
        conn = self.connect()
        t = tracks.create_track(conn, self.settings(), {"title": "A"}, self.data)
        stages = tracks.get_stages(conn, t["id"])
        self.assertEqual([s["key"] for s in stages], tracks.STAGE_KEYS)
        self.assertTrue(all(s["state"] == "todo" for s in stages))
        self.assertEqual(tracks.current_stage_key(stages), "download")

    def test_get_by_id_or_slug_and_not_found(self):
        conn = self.connect()
        t = tracks.create_track(conn, self.settings(), {"title": "Night Shift"}, self.data)
        self.assertEqual(tracks.get_track(conn, t["id"])["id"], t["id"])
        self.assertEqual(tracks.get_track(conn, "night-shift")["id"], t["id"])
        with self.assertRaises(NotFound) as cm:
            tracks.get_track(conn, "nope")
        self.assertIn("No track called 'nope'", str(cm.exception))

    def test_library_root_unwritable_is_a_sentence(self):
        conn = self.connect()
        blocker = self.data / "blocker"
        blocker.write_text("not a dir")
        settings_mod.save({"library_root": str(blocker)}, self.data)
        with self.assertRaises(UserError) as cm:
            tracks.create_track(conn, self.settings(), {"title": "A"}, self.data)
        self.assertIn("library folder", str(cm.exception).lower())


class StageTests(TempDataCase):
    def setUp(self):
        super().setUp()
        self.conn = self.connect()
        self.s = self.settings()
        self.t = tracks.create_track(self.conn, self.s, {"title": "A"}, self.data)

    def test_advance_and_skip(self):
        st = tracks.update_stage(self.conn, self.t["id"], "download", state="done")
        self.assertEqual(st["state"], "done")
        self.assertIsNotNone(st["done_at"])
        st = tracks.update_stage(self.conn, self.t["id"], "master", state="skipped")
        self.assertEqual(st["state"], "skipped")
        stages = tracks.get_stages(self.conn, self.t["id"])
        self.assertEqual(tracks.current_stage_key(stages), "clip")
        st = tracks.update_stage(self.conn, self.t["id"], "download", state="todo")
        self.assertIsNone(st["done_at"])
        with self.assertRaises(UserError):
            tracks.update_stage(self.conn, self.t["id"], "download", state="finished")
        with self.assertRaises(UserError):
            tracks.update_stage(self.conn, self.t["id"], "nope", state="done")

    def test_data_merges_and_path_is_verified(self):
        f = self.data / "my song.wav"
        f.write_bytes(b"RIFF")
        st = tracks.update_stage(self.conn, self.t["id"], "download", data={"path": str(f), "checks": {"0": True}})
        self.assertEqual(st["data"]["path"], str(f))
        st = tracks.update_stage(self.conn, self.t["id"], "download", data={"notes": "x"})
        self.assertEqual(st["data"]["path"], str(f))  # merged, not replaced
        self.assertEqual(st["data"]["checks"], {"0": True})
        with self.assertRaises(UserError) as cm:
            tracks.update_stage(self.conn, self.t["id"], "master", data={"path": r"C:\Users\me\Music\missing file.wav"})
        self.assertIn("File not found", str(cm.exception))
        report = tracks.assets_report(self.conn, self.t, self.s, self.data)
        self.assertEqual(len(report["recorded"]), 1)
        self.assertEqual(report["recorded"][0]["kind"], "source")
        self.assertTrue(report["recorded"][0]["exists"])
        f.unlink()
        report = tracks.assets_report(self.conn, self.t, self.s, self.data)
        self.assertFalse(report["recorded"][0]["exists"])

    def test_video_shots_verified_and_listed(self):
        vid = self.data / "library" / "a" / "video"
        (vid / "shot-02.mp4").write_bytes(b"0")
        (vid / "shot-01.mp4").write_bytes(b"0")
        report = tracks.assets_report(self.conn, self.t, self.s, self.data)
        self.assertEqual(report["on_disk"]["video"], ["shot-01.mp4", "shot-02.mp4"])
        with self.assertRaises(UserError):
            tracks.update_stage(self.conn, self.t["id"], "video", data={"shots": [str(vid / "shot-01.mp4"), str(vid / "shot-09.mp4")]})

    def test_failed_write_leaves_db_consistent(self):
        # a bad path raises before anything is written; the stage is untouched
        before = tracks.get_stages(self.conn, self.t["id"])
        with self.assertRaises(UserError):
            tracks.update_stage(self.conn, self.t["id"], "download", state="done", data={"path": "/no/such/file.wav"})
        self.assertEqual(tracks.get_stages(self.conn, self.t["id"]), before)


class DashboardAndReleaseTests(TempDataCase):
    def test_dashboard_groups_and_release_flow(self):
        conn = self.connect()
        s = self.settings()
        a = tracks.create_track(conn, s, {"title": "A"}, self.data)
        b = tracks.create_track(conn, s, {"title": "B"}, self.data)
        tracks.update_stage(conn, b["id"], "download", state="done")
        dash = tracks.dashboard(conn)
        groups = {g["key"]: [t["title"] for t in g["tracks"]] for g in dash["groups"]}
        self.assertEqual(groups["download"], ["A"])
        self.assertEqual(groups["master"], ["B"])
        self.assertEqual(dash["counts"]["active"], 2)
        # posting a URL to every enabled platform releases the track
        plats = tracks.platforms(conn, enabled_only=True)
        self.assertEqual(len(plats), 5)
        tracks.update_platforms(conn, [{"id": p["id"], "enabled": False} for p in plats[1:]])
        with self.assertRaises(UserError):
            tracks.set_post(conn, a["id"], plats[0]["id"], "youtube.com/x")
        r = tracks.set_post(conn, a["id"], plats[0]["id"], "https://youtube.com/x")
        self.assertTrue(r["released"])
        self.assertEqual(tracks.get_track(conn, a["id"])["status"], "released")
        dash = tracks.dashboard(conn)
        self.assertEqual([t["title"] for t in dash["released"]], ["A"])
        self.assertEqual(dash["released"][0]["posts"][0]["platform"], "YouTube")
        # release anyway
        tracks.release(conn, b["id"])
        self.assertEqual(tracks.get_track(conn, b["id"])["status"], "released")
        self.assertEqual(next(st for st in tracks.get_stages(conn, b["id"]) if st["key"] == "upload")["state"], "done")

    def test_list_empty(self):
        conn = self.connect()
        self.assertEqual(tracks.list_tracks(conn), [])
        self.assertEqual(tracks.dashboard(conn)["counts"]["total"], 0)


class DraftAndFeedbackTests(TempDataCase):
    def test_drafts_chosen_first_and_edit_persists(self):
        conn = self.connect()
        t = tracks.create_track(conn, self.settings(), {"title": "A"}, self.data)
        tracks.replace_drafts(conn, t["id"], "title", [{"label": "Title 1", "text": "A"}, {"label": "Title 2", "text": "B"}])
        d = tracks.save_draft(conn, t["id"], "prompt", {"label": "P", "text": "x"}, chosen=True, seed=7)
        lst = tracks.drafts_for(conn, t["id"])
        self.assertEqual(lst[0]["id"], d["id"])
        self.assertEqual(lst[0]["seed"], 7)
        tracks.update_draft(conn, t["id"], lst[1]["id"], content=json.dumps({"label": "Title 1", "text": "edited"}))
        self.assertIn("edited", tracks.drafts_for(conn, t["id"])[1]["content"])
        tracks.replace_drafts(conn, t["id"], "title", [{"label": "Title 1", "text": "C"}])
        kinds = [x["kind"] for x in tracks.drafts_for(conn, t["id"])]
        self.assertEqual(kinds.count("title"), 1)
        self.assertEqual(kinds.count("prompt"), 1)
        with self.assertRaises(NotFound):
            tracks.update_draft(conn, t["id"], 9999, content="x")

    def test_feedback_persists_to_db_and_markdown(self):
        conn = self.connect()
        with self.assertRaises(UserError):
            tracks.add_feedback(conn, "   ", "dashboard", self.data)
        fb = tracks.add_feedback(conn, "The copy button is tiny", "track #/track/1", self.data)
        self.assertEqual(fb["screen"], "track #/track/1")
        md = (self.data / "feedback.md").read_text(encoding="utf-8")
        self.assertIn("The copy button is tiny", md)
        self.assertIn(fb["created_at"], md)
        self.assertEqual(tracks.list_feedback(conn)[0]["text"], "The copy button is tiny")


class SettingsTests(TempDataCase):
    def test_defaults_save_validate_and_corrupt_file(self):
        s = settings_mod.load(self.data)
        self.assertEqual(s["aspect"], "9:16")
        self.assertEqual(settings_mod.library_root(s, self.data), self.data / "library")
        s = settings_mod.save({"artist": "Fenn", "clip_len": "20", "fade": "1", "unknown": 1}, self.data)
        self.assertEqual(s["clip_len"], 20)
        self.assertNotIn("unknown", s)
        for bad in ({"aspect": "4:3"}, {"shot_len": 7}, {"clip_len": "abc"}, {"fade": -1}):
            with self.assertRaises(UserError):
                settings_mod.save(bad, self.data)
        settings_mod.settings_path(self.data).write_text("{not json", encoding="utf-8")
        with self.assertRaises(UserError) as cm:
            settings_mod.load(self.data)
        self.assertIn("settings.json", str(cm.exception))
