import os
import shutil
import subprocess
import unittest
from pathlib import Path

from releaseline import UserError, media, tracks
from tests.helpers import TempDataCase

FFMPEG = media.find_ffmpeg()


def make_test_media(folder: Path):
    """3-second sine WAV and two 2-second colour videos via ffmpeg's lavfi sources."""
    ff = FFMPEG["path"]
    audio = folder / "sine tone.wav"
    v1, v2 = folder / "shot 01.mp4", folder / "shot 02.mp4"
    cmds = [
        [ff, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=3",
         "-c:a", "pcm_s16le", str(audio)],
        [ff, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i",
         "color=c=red:size=640x480:rate=25:duration=2", "-pix_fmt", "yuv420p", str(v1)],
        [ff, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i",
         "color=c=blue:size=320x640:rate=24:duration=2", "-pix_fmt", "yuv420p", str(v2)],
    ]
    for c in cmds:
        subprocess.run(c, check=True, capture_output=True, timeout=120)
    return audio, v1, v2


class TimeParsingTests(unittest.TestCase):
    def test_formats(self):
        self.assertEqual(media.parse_time("45"), 45.0)
        self.assertEqual(media.parse_time("12.5"), 12.5)
        self.assertEqual(media.parse_time("1:30"), 90.0)
        self.assertEqual(media.parse_time("01:05.5"), 65.5)
        self.assertEqual(media.parse_time("1:02:03"), 3723.0)
        self.assertEqual(media.parse_time(30), 30.0)
        self.assertEqual(media.parse_time(" 0:45 "), 45.0)

    def test_bad_input_is_a_sentence(self):
        for bad in ("abc", "1:xx", "", None, "1:75", "-5", "1:2:3:4", "0:45s"):
            with self.assertRaises(UserError, msg=repr(bad)) as cm:
                media.parse_time(bad)
            self.assertNotIn("Traceback", str(cm.exception))
        with self.assertRaises(UserError):
            media.parse_time(-1)

    def test_format_time(self):
        self.assertEqual(media.format_time(90), "1:30")
        self.assertEqual(media.format_time(90.5), "1:30.5")
        self.assertEqual(media.format_time(3723), "1:02:03")


class CommandBuildingTests(unittest.TestCase):
    """These never run ffmpeg, so they run everywhere."""

    def test_cut_cmd(self):
        src = r"C:\Users\me\Music\my song.wav" if os.name == "nt" else "/home/me/Music/my song.wav"
        cmd = media.build_cut_cmd(src, 90, 30, 0.5, "/out/c.wav", "/out/c.mp3", ffmpeg="ffmpeg")
        self.assertEqual(cmd[0], "ffmpeg")
        self.assertIn(src, cmd)
        self.assertEqual(cmd[cmd.index("-ss") + 1], "90")
        self.assertEqual(cmd[cmd.index("-t") + 1], "30")
        self.assertEqual(cmd.count("-af"), 2)
        self.assertIn("afade=t=in:st=0:d=0.5,afade=t=out:st=29.5:d=0.5", cmd)
        self.assertIn("libmp3lame", cmd)
        self.assertIn("320k", cmd)
        self.assertEqual(cmd[-1], "/out/c.mp3")
        self.assertTrue(cmd.index("/out/c.wav") < cmd.index("/out/c.mp3"))
        joined = media.shell_join(cmd)
        self.assertIn("my song.wav", joined)

    def test_cut_cmd_without_fade_and_bad_values(self):
        cmd = media.build_cut_cmd("a.wav", 0, 30, 0, "o.wav", "o.mp3")
        self.assertNotIn("-af", cmd)
        with self.assertRaises(UserError):
            media.build_cut_cmd("a.wav", 0, 0, 0.5, "o.wav", "o.mp3")
        with self.assertRaises(UserError):
            media.build_cut_cmd("a.wav", 0, 1, 0.6, "o.wav", "o.mp3")
        with self.assertRaises(UserError):
            media.build_cut_cmd("a.wav", 0, 30, -1, "o.wav", "o.mp3")

    def test_assemble_plan_for_each_aspect(self):
        shots = ["/v/shot 01.mp4", "/v/shot's 02.mp4", "/v/shot-03.mov"]
        for aspect, (w, h) in media.ASPECT_SIZES.items():
            plan = media.build_assemble_cmd(shots, "/c/clip.wav", aspect, "/f/out.mp4", "/f/.work", ffmpeg="ffmpeg")
            self.assertEqual(len(plan.commands), len(shots) + 2)
            self.assertEqual(plan.size, (w, h))
            for i, shot in enumerate(shots):
                cmd = plan.commands[i]
                self.assertIn(shot, cmd)
                vf = cmd[cmd.index("-vf") + 1]
                self.assertIn(f"scale={w}:{h}:force_original_aspect_ratio=decrease", vf)
                self.assertIn(f"pad={w}:{h}:(ow-iw)/2:(oh-ih)/2", vf)
                self.assertIn("fps=30", vf)
                self.assertIn("libx264", cmd)
                self.assertIn("-an", cmd)
            concat = plan.commands[-2]
            self.assertEqual(concat[concat.index("-f") + 1], "concat")
            self.assertIn(plan.listfile, concat)
            self.assertEqual(plan.listfile_text.count("file '"), 3)
            self.assertEqual(media._concat_entry("/v/shot's 02.mp4"), "file '/v/shot'\\''s 02.mp4'")
            final = plan.commands[-1]
            self.assertEqual(final[final.index("-stream_loop") + 1], "-1")
            self.assertIn("-shortest", final)
            self.assertIn("/c/clip.wav", final)
            self.assertEqual(final[final.index("-b:a") + 1], "192k")
            self.assertEqual(final[final.index("-c:a") + 1], "aac")
            self.assertEqual(final[-1], "/f/out.mp4")

    def test_assemble_plan_bad_inputs(self):
        with self.assertRaises(UserError):
            media.build_assemble_cmd([], "a.wav", "9:16", "o.mp4")
        with self.assertRaises(UserError):
            media.build_assemble_cmd(["s.mp4"], "a.wav", "4:3", "o.mp4")

    def test_find_ffmpeg_bad_path_is_not_found(self):
        info = media.find_ffmpeg("/definitely/not/here/ffmpeg")
        self.assertFalse(info["found"])
        self.assertIn("not found", info["message"])
        self.assertIn("Install ffmpeg", info["message"])

    def test_run_handles_missing_binary(self):
        res = media.run(["/definitely/not/here/ffmpeg", "-version"])
        self.assertFalse(res.ok)
        self.assertIn("not an executable", res.stderr_tail)


@unittest.skipUnless(FFMPEG["found"], "ffmpeg is not installed on this machine; execution tests skipped "
                                     "(command-building tests above still ran).")
class ExecutionTests(TempDataCase):
    def setUp(self):
        super().setUp()
        self.media_dir = self.tmp / "test media with spaces"
        self.media_dir.mkdir()
        self.audio, self.v1, self.v2 = make_test_media(self.media_dir)
        self.conn = self.connect()
        self.s = self.settings()
        self.track = tracks.create_track(self.conn, self.s, {"title": "Exec Test"}, self.data)

    def test_cut_then_assemble_end_to_end(self):
        # cut: no source recorded yet -> sentence
        with self.assertRaises(UserError) as cm:
            media.cut_clip(self.conn, self.s, self.track, "0:00", 2, 0.2, None, self.data)
        self.assertIn("stage 1", str(cm.exception))
        tracks.update_stage(self.conn, self.track["id"], "download", data={"path": str(self.audio)})
        with self.assertRaises(UserError):
            media.cut_clip(self.conn, self.s, self.track, "bad", 2, 0.2, None, self.data)
        r = media.cut_clip(self.conn, self.s, self.track, "0:01", 2, 0.2, None, self.data)
        self.assertTrue(r["ran"])
        wav, mp3 = Path(r["wav"]), Path(r["mp3"])
        self.assertTrue(wav.is_file() and mp3.is_file())
        self.assertEqual(wav.name, "exec-test-clip.wav")
        self.assertAlmostEqual(media.probe_duration(wav, FFMPEG), 2.0, delta=0.15)
        self.assertAlmostEqual(media.probe_duration(mp3, FFMPEG), 2.0, delta=0.15)
        stage = next(s for s in tracks.get_stages(self.conn, self.track["id"]) if s["key"] == "clip")
        self.assertEqual(stage["state"], "done")
        self.assertFalse(any(p.name.endswith(".part.wav") for p in wav.parent.iterdir()))

        # assemble: no shots -> sentence
        with self.assertRaises(UserError) as cm:
            media.assemble(self.conn, self.s, self.track, None, self.data)
        self.assertIn("No shots found", str(cm.exception))
        video_dir = self.data / "library" / "exec-test" / "video"
        shutil.copy(self.v1, video_dir / "shot-01.mp4")
        shutil.copy(self.v2, video_dir / "shot-02.mp4")
        r = media.assemble(self.conn, self.s, self.track, "9:16", self.data)
        self.assertTrue(r["ran"])
        out = Path(r["final"])
        self.assertTrue(out.is_file())
        self.assertEqual(out.name, "exec-test-final.mp4")
        # 2 s + 2 s of video over a 2 s clip -> trimmed to the audio
        self.assertAlmostEqual(media.probe_duration(out, FFMPEG), 2.0, delta=0.2)
        self.assertEqual(r["shots"], [str(video_dir / "shot-01.mp4"), str(video_dir / "shot-02.mp4")])
        self.assertFalse(list(out.parent.glob(".work*")), "work folder should be cleaned up")
        if FFMPEG["ffprobe"]:
            probe = subprocess.run([FFMPEG["ffprobe"], "-v", "error", "-select_streams", "v:0", "-show_entries",
                                    "stream=width,height,codec_name", "-of", "csv=p=0", str(out)],
                                   capture_output=True, text=True).stdout.strip()
            self.assertEqual(probe, "h264,1080,1920")

    def test_assemble_loops_short_video_over_longer_audio(self):
        tracks.update_stage(self.conn, self.track["id"], "download", data={"path": str(self.audio)})
        media.cut_clip(self.conn, self.s, self.track, 0, 3, 0.1, None, self.data)  # 3 s clip
        video_dir = self.data / "library" / "exec-test" / "video"
        shutil.copy(self.v1, video_dir / "shot-01.mp4")  # only 2 s of video
        r = media.assemble(self.conn, self.s, self.track, "1:1", self.data)
        self.assertAlmostEqual(media.probe_duration(r["final"], FFMPEG), 3.0, delta=0.2)

    def test_recorded_shot_paths_with_spaces_are_used_in_order(self):
        tracks.update_stage(self.conn, self.track["id"], "download", data={"path": str(self.audio)})
        media.cut_clip(self.conn, self.s, self.track, 0, 2, 0.1, None, self.data)
        tracks.update_stage(self.conn, self.track["id"], "video", data={"shots": [str(self.v2), str(self.v1)]})
        r = media.assemble(self.conn, self.s, self.track, "16:9", self.data)
        self.assertEqual(r["shots"], [str(self.v2), str(self.v1)])
        self.assertTrue(Path(r["final"]).is_file())

    def test_ffmpeg_failure_is_a_sentence_and_leaves_no_partial(self):
        bad = self.media_dir / "not audio.wav"
        bad.write_text("this is not audio")
        tracks.update_stage(self.conn, self.track["id"], "download", data={"path": str(bad)})
        with self.assertRaises(UserError) as cm:
            media.cut_clip(self.conn, self.s, self.track, 0, 2, 0.1, None, self.data)
        self.assertIn("ffmpeg could not cut the clip", str(cm.exception))
        clip_dir = self.data / "library" / "exec-test" / "clip"
        self.assertEqual([p.name for p in clip_dir.iterdir()], [])

    def test_ffmpeg_status_reports_version(self):
        st = media.ffmpeg_status({"ffmpeg_path": ""})
        self.assertTrue(st["found"])
        self.assertIn("ffmpeg", st.get("version", "").lower())

    def test_without_ffmpeg_commands_are_shown(self):
        tracks.update_stage(self.conn, self.track["id"], "download", data={"path": str(self.audio)})
        s = dict(self.s, ffmpeg_path="/definitely/not/here/ffmpeg")
        r = media.cut_clip(self.conn, s, self.track, "0:01", 2, 0.2, None, self.data)
        self.assertFalse(r["ran"])
        self.assertIn("not found", r["message"])
        self.assertEqual(len(r["commands"]), 1)
        self.assertIn("sine tone.wav", r["commands"][0])
        self.assertFalse((self.data / "library" / "exec-test" / "clip" / "exec-test-clip.wav").exists())
