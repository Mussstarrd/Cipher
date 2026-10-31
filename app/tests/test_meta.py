import unittest

from releaseline import meta

TRACKS = [
    {"title": "Night Shift", "artist": "Fenn", "genre": "dark trap", "mood": "menacing, cold", "energy": "high",
     "bpm": "140", "key": "F minor", "hook": "we don't sleep tonight", "created_at": "2026-10-31T00:00:00+00:00",
     "suno_url": "https://suno.com/song/x"},
    {"title": "Glass Ocean", "artist": "", "genre": "synthwave", "mood": "nostalgic", "energy": "mid",
     "bpm": "", "key": "", "hook": "", "created_at": ""},
    {"title": "Untitled 7", "artist": "A", "genre": "", "mood": "", "energy": "", "bpm": "", "key": "", "hook": ""},
    {"title": "Long one " * 20, "artist": "Someone With A Long Name", "genre": "lo-fi hip hop, chillhop, jazzhop, boom bap",
     "mood": "calm, dreamy, nostalgic, warm", "energy": "low", "bpm": "85", "key": "C major",
     "hook": "a very long hook line that goes on and on and on and keeps going past sixty characters for sure"},
]


class MetaTests(unittest.TestCase):
    def test_no_performance_claims_ever(self):
        for t in TRACKS:
            for explicit in (False, True):
                text = meta.all_text(meta.generate(t, {}, explicit=explicit)).lower()
                for word in meta.BANNED:
                    self.assertNotIn(word, text, (t["title"][:20], word))
        # the banned list itself is what the spec names
        for w in ("viral", "guaranteed", "grow your", "blow up", "go viral", "promise"):
            self.assertIn(w, meta.BANNED)

    def test_three_titles_under_100_chars(self):
        for t in TRACKS:
            r = meta.generate(t, {})
            self.assertEqual(len(r["titles"]), 3, t["title"][:20])
            for title in r["titles"]:
                self.assertLessEqual(len(title), 100)
                self.assertTrue(title.strip())

    def test_type_beat_rule(self):
        for genre in ("trap", "Dark Trap", "hip hop", "drill", "R&B", "lo-fi", "boom bap"):
            self.assertTrue(meta.uses_type_beat(genre), genre)
            r = meta.generate(dict(TRACKS[1], genre=genre), {})
            self.assertTrue(any("Type Beat" in x for x in r["titles"]), genre)
            self.assertIn("#typebeat", r["hashtags"]["youtube"])
        for genre in ("synthwave", "house", "folk", "", "orchestral"):
            self.assertFalse(meta.uses_type_beat(genre), genre)
            r = meta.generate(dict(TRACKS[1], genre=genre), {})
            self.assertFalse(any("Type Beat" in x for x in r["titles"]), genre)

    def test_hook_led_title_and_plain_title(self):
        r = meta.generate(TRACKS[0], {})
        self.assertEqual(r["titles"][0], "Night Shift - Fenn")
        self.assertIn("we don't sleep tonight", r["titles"][2])
        self.assertIn("Night Shift", r["titles"][2])

    def test_youtube_tags_capped_at_500(self):
        for t in TRACKS:
            r = meta.generate(t, {})
            self.assertLessEqual(len(r["youtube_tags"]), 500)
            self.assertEqual(r["youtube_tags_length"], len(r["youtube_tags"]))
            self.assertNotIn(",,", r["youtube_tags"])
        huge = dict(TRACKS[3], title="T" * 400, genre=", ".join(f"genre number {i}" for i in range(40)))
        r = meta.generate(huge, {})
        self.assertLessEqual(len(r["youtube_tags"]), 500)
        self.assertGreater(len(r["youtube_tags"]), 400)  # it fills up rather than giving up

    def test_descriptions_hashtags_and_distributor_note(self):
        r = meta.generate(TRACKS[0], {}, explicit=True)
        self.assertIn("140 BPM", r["description_short"])
        self.assertIn("Explicit: yes", r["description_long"])
        self.assertIn("https://suno.com/song/x", r["description_long"])
        self.assertIn("Explicit: yes", r["distributor_note"])
        self.assertIn("Genre: dark trap", r["distributor_note"])
        self.assertEqual(set(r["hashtags"]), {"youtube", "tiktok", "instagram"})
        for tags in r["hashtags"].values():
            self.assertTrue(tags)
            for tag in tags:
                self.assertRegex(tag, r"^#[a-z0-9]+$")
        self.assertLessEqual(len(r["hashtags"]["youtube"]), 5)
        self.assertLessEqual(len(r["hashtags"]["instagram"]), 15)
        r2 = meta.generate(dict(TRACKS[2], artist=""), {"artist": "Studio Default"})
        self.assertIn("Studio Default", r2["description_short"])
        self.assertIn("Explicit: no", r2["distributor_note"])
        self.assertIn("Genre: (set genre)", r2["distributor_note"])
        self.assertIn("Titles:", meta.as_text(r2))
