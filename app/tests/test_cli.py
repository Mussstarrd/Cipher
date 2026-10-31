import os
import subprocess
import sys

from tests.helpers import APP_DIR, TempDataCase


class CliTests(TempDataCase):
    def run_cli(self, *args):
        env = dict(os.environ, RELEASELINE_DATA=str(self.data))
        proc = subprocess.run([sys.executable, "-m", "releaseline", *args], cwd=str(APP_DIR), env=env,
                              capture_output=True, text=True, timeout=120)
        return proc.returncode, proc.stdout, proc.stderr

    def test_list_on_empty_db(self):
        code, out, err = self.run_cli("list")
        self.assertEqual(code, 0, err)
        self.assertIn("No tracks yet", out)

    def test_cut_on_missing_track_is_user_error(self):
        code, out, err = self.run_cli("cut", "nope", "--start", "0:10")
        self.assertEqual(code, 1)
        self.assertIn("No track called 'nope'", err)
        self.assertNotIn("Traceback", err)

    def test_new_then_prompts_meta_list_feedback(self):
        code, out, err = self.run_cli("new", "Night Shift", "--genre", "trap", "--mood", "dark", "--energy", "high")
        self.assertEqual(code, 0, err)
        self.assertIn("night-shift", out)
        code, out, err = self.run_cli("prompts", "night-shift", "--n", "3", "--seed", "4", "--preset", "anime")
        self.assertEqual(code, 0, err)
        self.assertIn("Variant 3", out)
        self.assertIn("Seed 4", out)
        code, out2, _ = self.run_cli("prompts", "1", "--n", "3", "--seed", "4", "--preset", "anime")
        self.assertEqual(out, out2)
        code, out, err = self.run_cli("prompts", "1", "--preset", "claymation")
        self.assertEqual(code, 1)
        self.assertIn("not a style preset", err)
        code, out, err = self.run_cli("meta", "1")
        self.assertEqual(code, 0, err)
        self.assertIn("Type Beat", out)
        code, out, err = self.run_cli("list")
        self.assertEqual(code, 0)
        self.assertIn("Night Shift", out)
        self.assertIn("Download from Suno", out)
        code, out, err = self.run_cli("feedback", "cli note")
        self.assertEqual(code, 0, err)
        self.assertIn("cli note", (self.data / "feedback.md").read_text(encoding="utf-8"))

    def test_cut_bad_time_and_missing_source(self):
        self.run_cli("new", "A")
        code, out, err = self.run_cli("cut", "a", "--start", "abc")
        self.assertEqual(code, 1)
        self.assertIn("not a time", err)
        code, out, err = self.run_cli("cut", "a", "--start", "0:10")
        self.assertEqual(code, 1)
        self.assertIn("stage 1", err)
        code, out, err = self.run_cli("cut", "a", "--start", "0:10", "--src", "/no/such file.wav")
        self.assertEqual(code, 1)
        self.assertIn("File not found", err)
        code, out, err = self.run_cli("assemble", "a")
        self.assertEqual(code, 1)
        self.assertIn("Cut the clip first", err)

    def test_new_requires_title_and_help_exits_zero(self):
        code, out, err = self.run_cli("new", "   ")
        self.assertEqual(code, 1)
        self.assertIn("title", err.lower())
        code, out, err = self.run_cli()
        self.assertEqual(code, 0)
        self.assertIn("serve", out)
        code, out, err = self.run_cli("--version")
        self.assertEqual(code, 0)

    def test_unreadable_db_is_exit_1_sentence(self):
        (self.data / "releaseline.db").write_text("garbage", encoding="utf-8")
        code, out, err = self.run_cli("list")
        self.assertEqual(code, 1)
        self.assertIn("releaseline.db", err)
        self.assertNotIn("Traceback", err)
