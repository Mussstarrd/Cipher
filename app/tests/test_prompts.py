import math

from releaseline import UserError, prompts
from tests.helpers import TempDataCase

TRACK = {"title": "Night Shift", "genre": "dark trap", "mood": "menacing, cold", "energy": "high"}


class PromptTests(TempDataCase):
    def test_same_seed_same_output(self):
        a = prompts.generate(TRACK, preset="cinematic", seed=42, n=4)
        b = prompts.generate(TRACK, preset="cinematic", seed=42, n=4)
        self.assertEqual(a, b)

    def test_different_seeds_differ(self):
        a = prompts.generate(TRACK, seed=1, n=3)
        b = prompts.generate(TRACK, seed=2, n=3)
        self.assertNotEqual([v["prompt"] for v in a["variants"]], [v["prompt"] for v in b["variants"]])
        # and variants within one result differ from each other
        self.assertEqual(len({v["prompt"] for v in a["variants"]}), 3)

    def test_random_seed_when_none(self):
        r = prompts.generate(TRACK, n=3)
        self.assertIsInstance(r["seed"], int)

    def test_shot_list_sizing(self):
        self.assertEqual(prompts.shot_count(30, 5), 6)
        self.assertEqual(prompts.shot_count(30, 10), 3)
        self.assertEqual(prompts.shot_count(32, 5), 7)
        self.assertEqual(prompts.shot_count(3, 10), 1)
        r = prompts.generate(TRACK, seed=3, n=3, clip_len=45, shot_len=10)
        self.assertEqual(r["shots"], math.ceil(45 / 10))
        for v in r["variants"]:
            self.assertEqual(len(v["shots"]), 5)
            self.assertEqual([s["n"] for s in v["shots"]], [1, 2, 3, 4, 5])
        r = prompts.generate(TRACK, seed=3, n=3, clip_len=30, shot_len=5, shots=2)
        self.assertEqual(len(r["variants"][0]["shots"]), 2)
        with self.assertRaises(UserError):
            prompts.shot_count(30, 7)
        with self.assertRaises(UserError):
            prompts.generate(TRACK, seed=1, n=3, shots=0)

    def test_shot_list_is_continuous_and_varied(self):
        r = prompts.generate(TRACK, seed=5, n=3, clip_len=30, shot_len=5)
        v = r["variants"][0]
        for s in v["shots"]:
            self.assertIn(v["subject"].lower(), s["prompt"].lower())
            self.assertIn(v["setting"], s["prompt"])
            self.assertIn(v["lighting"].lower(), s["prompt"].lower())
        self.assertGreater(len({s["camera"] for s in v["shots"]}), 1)
        self.assertEqual(len({s["moment"] for s in v["shots"]}), len(v["shots"]))

    def test_every_preset_energy_mood_and_genre_family_works(self):
        fields = ["subject", "setting", "scene", "camera", "lighting", "style", "palette", "negative", "prompt"]
        for preset in prompts.PRESETS:
            for energy in ("low", "mid", "high"):
                r = prompts.generate({"genre": "house", "mood": "happy", "energy": energy}, preset=preset, seed=9, n=5)
                self.assertEqual(len(r["variants"]), 5)
                for v in r["variants"]:
                    for f in fields:
                        self.assertTrue(v[f], (preset, energy, f))
                    self.assertEqual(len(v["mood_words"]), 3)
                    for s in v["shots"]:
                        self.assertTrue(s["prompt"] and s["camera"] and s["moment"])
        for _, words in prompts.GENRE_FAMILY_KEYWORDS:
            r = prompts.generate({"genre": words[0], "mood": "", "energy": "mid"}, seed=1, n=3)
            self.assertNotEqual(r["genre_family"], "default", words[0])
        for cat, words in prompts.MOOD_KEYWORDS:
            self.assertEqual(prompts.mood_category(words[0]), cat)
        self.assertEqual(prompts.genre_family("polka"), "default")
        self.assertEqual(prompts.mood_category(""), "neutral")

    def test_banks_are_deep_enough(self):
        for banks in (prompts.GENRE_BANKS, prompts.MOOD_BANKS, prompts.ENERGY_BANKS, prompts.PRESET_BANKS):
            for name, bank in banks.items():
                for key, items in bank.items():
                    self.assertGreaterEqual(len(items), 8, (name, key))
                    self.assertEqual(len(set(items)), len(items), (name, key))

    def test_preset_aliases_and_bad_inputs(self):
        self.assertEqual(prompts.normalize_preset("VHS/lo-fi"), "vhs")
        self.assertEqual(prompts.normalize_preset("3D render"), "3d")
        with self.assertRaises(UserError):
            prompts.normalize_preset("claymation")
        with self.assertRaises(UserError):
            prompts.generate(TRACK, seed=1, n=2)
        with self.assertRaises(UserError):
            prompts.generate(TRACK, seed=1, n=3, aspect="4:3")
        with self.assertRaises(UserError):
            prompts.generate(TRACK, seed="abc", n=3)

    def test_free_text_setting_is_used(self):
        r = prompts.generate(TRACK, seed=1, n=3, setting="a rainy rooftop in Tokyo")
        for v in r["variants"]:
            self.assertIn("a rainy rooftop in Tokyo", v["prompt"])
            self.assertEqual(v["setting"], "a rainy rooftop in Tokyo")

    def test_aspect_and_duration_in_prompt(self):
        r = prompts.generate(TRACK, seed=1, n=3, aspect="16:9", shot_len=10)
        self.assertIn("16:9, 10 seconds", r["variants"][0]["prompt"])
        self.assertIn("10 seconds", r["variants"][0]["shots"][0]["prompt"])
        text = prompts.as_text(r)
        self.assertIn("Variant 1", text)
