import os
import subprocess
import sys
import unittest
from pathlib import Path

from tests.helpers import APP_DIR, TempDataCase
from tests.test_media import FFMPEG, make_test_media


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

    def test_serve_host_flag_is_accepted_and_blank_host_is_refused(self):
        code, out, err = self.run_cli("serve", "--host", "   ", "--no-browser", "--port", "8477")
        self.assertEqual(code, 1)
        self.assertIn("--host should be an address", err)
        self.assertNotIn("Traceback", err)
        # A real bind on all interfaces, port 0, then stop it quickly.
        import socket
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            free_port = sock.getsockname()[1]
        env = dict(os.environ, RELEASELINE_DATA=str(self.data))
        proc = subprocess.Popen([sys.executable, "-m", "releaseline", "serve", "--host", "0.0.0.0",
                                 "--port", str(free_port), "--no-browser"], cwd=str(APP_DIR), env=env,
                                stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        try:
            import time
            deadline = time.time() + 10
            first = ""
            while time.time() < deadline and not first:
                first = proc.stdout.readline()
            self.assertIn("ReleaseLine is running at", first)
            phone_line = proc.stdout.readline()
            self.assertIn("On your phone", phone_line)
        finally:
            proc.terminate()
            proc.wait(timeout=10)

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

    def test_argument_errors_exit_1_with_a_sentence(self):
        for args in (("new",), ("prompts", "1", "--seed", "abc"), ("prompts", "1", "--aspect", "4:3"),
                     ("dance",), ("serve", "--port", "abc"), ("cut", "1")):
            code, out, err = self.run_cli(*args)
            self.assertEqual(code, 1, args)
            self.assertIn("Error:", err, args)
            self.assertIn("usage:", err, args)
            self.assertNotIn("Traceback", err)

    def test_old_python_is_refused_with_a_sentence(self):
        env = dict(os.environ, RELEASELINE_DATA=str(self.data))
        proc = subprocess.run([sys.executable, "-c",
                               "import sys; sys.version_info = (3, 8, 0, 'final', 0); import runpy; "
                               "runpy.run_module('releaseline', run_name='__main__')", "list"],
                              cwd=str(APP_DIR), env=env, capture_output=True, text=True, timeout=60)
        self.assertEqual(proc.returncode, 1)
        self.assertEqual(proc.stderr.strip(), "ReleaseLine needs Python 3.10 or newer. You have 3.8.")
        self.assertNotIn("Traceback", proc.stderr)

    @unittest.skipUnless(FFMPEG["found"], "ffmpeg is not installed on this machine; the CLI cut execution "
                                          "test is skipped (the CLI error-path tests above still ran).")
    def test_cut_with_ffmpeg_past_end_clamped_and_ok(self):
        media_dir = self.tmp / "qa media"
        media_dir.mkdir()
        audio, _, _ = make_test_media(media_dir)  # 3 s tone
        self.run_cli("new", "Night Shift")
        clip_dir = self.data / "library" / "night-shift" / "clip"
        code, out, err = self.run_cli("cut", "1", "--start", "0:10", "--len", "2", "--src", str(audio))
        self.assertEqual(code, 1, out)
        self.assertEqual(err.strip(), "Error: 0:10 is past the end of the file (0:03 long). Pick an earlier start.")
        code, out, err = self.run_cli("cut", "1", "--start", "3", "--len", "2", "--src", str(audio))
        self.assertEqual(code, 1, out)
        self.assertIn("is past the end", err)
        self.assertEqual(list(clip_dir.iterdir()), [])
        code, out, err = self.run_cli("list")
        self.assertIn("Download from Suno", out)  # stage 3 is still to do; nothing advanced
        code, out, err = self.run_cli("cut", "1", "--start", "0:02", "--len", "30", "--src", str(audio))
        self.assertEqual(code, 0, err)
        self.assertIn("Clip saved: night-shift-clip.wav and night-shift-clip.mp3.", out)
        self.assertIn("The clip will be 0:01 long because the file ends there.", out)
        self.assertTrue((clip_dir / "night-shift-clip.wav").stat().st_size > 1024)
        code, out, err = self.run_cli("cut", "1", "--start", "0:00", "--len", "2", "--src", str(audio))
        self.assertEqual(code, 0, err)
        self.assertEqual(out.strip(), "Clip saved: night-shift-clip.wav and night-shift-clip.mp3.")
