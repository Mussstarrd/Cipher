"""ffmpeg helpers: detection, time parsing, command building (pure) and execution.

Command builders only return argv lists so they can be shown to the user when ffmpeg is
missing and tested without running anything. Outputs are written to a part-file and
renamed into place, so a failed run never leaves a half-written clip or video.
"""

from __future__ import annotations

import os
import re
import shlex
import shutil
import subprocess
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path

from . import UserError
from .tracks import get_stages, track_dir, update_stage

ASPECT_SIZES = {"9:16": (1080, 1920), "16:9": (1920, 1080), "1:1": (1080, 1080)}
FPS = 30
VIDEO_EXTS = (".mp4", ".mov", ".webm", ".mkv", ".m4v")
INSTALL_HINT = ("Install ffmpeg (Windows: 'winget install Gyan.FFmpeg'; macOS: 'brew install ffmpeg'; "
                "Debian/Ubuntu: 'sudo apt install ffmpeg') or set its path in Settings.")


# ---- detection -----------------------------------------------------------------

def find_ffmpeg(configured: str = "") -> dict:
    """Locate ffmpeg (configured path first, then PATH). Never raises."""
    configured = (configured or "").strip()
    path = None
    if configured:
        p = Path(configured).expanduser()
        if p.is_file():
            path = str(p)
        elif p.is_dir():
            for name in ("ffmpeg", "ffmpeg.exe"):
                if (p / name).is_file():
                    path = str(p / name)
                    break
            if path is None and (p / "bin" / "ffmpeg.exe").is_file():
                path = str(p / "bin" / "ffmpeg.exe")
        else:
            path = shutil.which(configured)
    if path is None and not configured:
        path = shutil.which("ffmpeg")
    if path is None:
        msg = (f"ffmpeg was not found at '{configured}'." if configured
               else "ffmpeg was not found on this computer.")
        return {"found": False, "path": None, "ffprobe": None, "message": f"{msg} {INSTALL_HINT}"}
    probe = None
    candidate = Path(path).with_name("ffprobe" + Path(path).suffix)
    if candidate.is_file():
        probe = str(candidate)
    else:
        probe = shutil.which("ffprobe")
    return {"found": True, "path": path, "ffprobe": probe, "message": f"ffmpeg found at {path}."}


def ffmpeg_version(path: str) -> str | None:
    res = run([path, "-version"], timeout=20)
    if not res.ok:
        return None
    first = (res.stdout or "").splitlines()[:1]
    return first[0].strip() if first else "ffmpeg"


def ffmpeg_status(settings: dict) -> dict:
    info = find_ffmpeg(settings.get("ffmpeg_path", ""))
    if info["found"]:
        ver = ffmpeg_version(info["path"])
        if ver is None:
            info = {"found": False, "path": info["path"], "ffprobe": None,
                    "message": f"Found {info['path']} but it did not run. {INSTALL_HINT}"}
        else:
            info["version"] = ver
    return info


# ---- time parsing --------------------------------------------------------------

def parse_time(text) -> float:
    """Accept seconds ('90', '12.5'), m:ss ('1:30'), or h:mm:ss ('0:01:30'). Returns seconds."""
    if isinstance(text, (int, float)) and not isinstance(text, bool):
        if text < 0:
            raise UserError("Time cannot be negative.")
        return float(text)
    raw = str(text or "").strip()
    if not raw:
        raise UserError("Enter a time like 0:45 (minutes:seconds) or 45 (seconds).")
    if not re.fullmatch(r"(\d+:)?(\d+:)?\d+(\.\d+)?", raw):
        raise UserError(f"'{raw}' is not a time. Use m:ss like 1:05, h:mm:ss, or plain seconds.")
    parts = raw.split(":")
    total = 0.0
    for i, part in enumerate(parts):
        val = float(part)
        if i > 0 and val >= 60:
            raise UserError(f"'{raw}' is not a time: minutes and seconds must be below 60.")
        total = total * 60 + val
    return total


def format_time(seconds: float) -> str:
    seconds = max(0.0, float(seconds))
    m, s = divmod(seconds, 60)
    h, m = divmod(int(m), 60)
    sec = f"{s:05.2f}".rstrip("0").rstrip(".") if s != int(s) else f"{int(s):02d}"
    if h:
        return f"{h}:{m:02d}:{sec}"
    return f"{m}:{sec}"


def _num(x: float) -> str:
    return f"{float(x):.3f}".rstrip("0").rstrip(".")


# ---- command building (pure) ---------------------------------------------------

def build_cut_cmd(src, start: float, length: float, fade: float, out_wav, out_mp3,
                  ffmpeg: str = "ffmpeg") -> list[str]:
    """One ffmpeg call that writes a WAV and an MP3 320 of [start, start+length) with fades."""
    start, length, fade = float(start), float(length), float(fade)
    if length <= 0:
        raise UserError("Clip length must be more than 0 seconds.")
    if fade < 0:
        raise UserError("Fade cannot be negative.")
    if fade * 2 >= length:
        raise UserError("The fades are longer than the clip. Use a shorter fade or a longer clip.")
    filters = []
    if fade > 0:
        filters = [f"afade=t=in:st=0:d={_num(fade)}",
                   f"afade=t=out:st={_num(length - fade)}:d={_num(fade)}"]
    af = ["-af", ",".join(filters)] if filters else []
    return [
        ffmpeg, "-hide_banner", "-nostdin", "-y",
        "-ss", _num(start), "-t", _num(length), "-i", str(src),
        *af, "-c:a", "pcm_s16le", str(out_wav),
        *af, "-c:a", "libmp3lame", "-b:a", "320k", str(out_mp3),
    ]


@dataclass
class AssemblePlan:
    commands: list = field(default_factory=list)   # list of argv lists, run in order
    labels: list = field(default_factory=list)     # one label per command
    listfile: str = ""                             # concat list file path
    listfile_text: str = ""                        # its content (caller writes it)
    intermediates: list = field(default_factory=list)
    size: tuple = (1080, 1920)


def _concat_entry(path: str) -> str:
    p = str(path)
    if os.name == "nt":
        p = p.replace("\\", "/")
    p = p.replace("\\", "\\\\").replace("'", "'\\''")
    return f"file '{p}'"


def build_assemble_cmd(shots, audio, aspect: str, out, workdir=None,
                       ffmpeg: str = "ffmpeg") -> AssemblePlan:
    """Plan: normalize each shot -> concat via list file -> loop/trim to the audio and mux."""
    if aspect not in ASPECT_SIZES:
        raise UserError("Aspect ratio must be 9:16, 16:9 or 1:1.")
    shots = [str(s) for s in shots]
    if not shots:
        raise UserError("There are no video shots to assemble.")
    w, h = ASPECT_SIZES[aspect]
    out = Path(out)
    work = Path(workdir) if workdir else out.parent / ".work"
    vf = (f"scale={w}:{h}:force_original_aspect_ratio=decrease,"
          f"pad={w}:{h}:(ow-iw)/2:(oh-ih)/2,setsar=1,fps={FPS},format=yuv420p")
    plan = AssemblePlan(size=(w, h))
    norm_paths = []
    for i, shot in enumerate(shots, 1):
        norm = work / f"norm-{i:02d}.mp4"
        norm_paths.append(norm)
        plan.commands.append([
            ffmpeg, "-hide_banner", "-nostdin", "-y", "-i", shot, "-an", "-vf", vf,
            "-r", str(FPS), "-c:v", "libx264", "-preset", "veryfast", "-crf", "20",
            "-pix_fmt", "yuv420p", str(norm),
        ])
        plan.labels.append(f"normalize shot {i}")
    concat = work / "concat.mp4"
    plan.listfile = str(work / "shots.txt")
    plan.listfile_text = "\n".join(_concat_entry(str(p)) for p in norm_paths) + "\n"
    plan.commands.append([
        ffmpeg, "-hide_banner", "-nostdin", "-y", "-f", "concat", "-safe", "0",
        "-i", plan.listfile, "-c", "copy", str(concat),
    ])
    plan.labels.append("concatenate shots")
    # Loop the concatenated video endlessly and stop at the end of the audio (-shortest):
    # a visual shorter than the audio repeats, a longer one is trimmed.
    plan.commands.append([
        ffmpeg, "-hide_banner", "-nostdin", "-y", "-stream_loop", "-1", "-i", str(concat),
        "-i", str(audio), "-map", "0:v:0", "-map", "1:a:0",
        "-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-pix_fmt", "yuv420p",
        "-r", str(FPS), "-c:a", "aac", "-b:a", "192k", "-shortest",
        "-fflags", "+shortest", "-max_interleave_delta", "100M",
        "-movflags", "+faststart", str(out),
    ])
    plan.labels.append("mux audio and write final video")
    plan.intermediates = [str(p) for p in norm_paths] + [str(concat), plan.listfile]
    return plan


def shell_join(cmd: list[str]) -> str:
    if os.name == "nt":
        return subprocess.list2cmdline(cmd)
    return shlex.join(cmd)


# ---- execution -----------------------------------------------------------------

@dataclass
class RunResult:
    ok: bool
    returncode: int
    stdout: str = ""
    stderr_tail: str = ""
    seconds: float = 0.0


def run(cmd: list[str], timeout: int = 600) -> RunResult:
    """Run a command, capture output, never raise. stderr_tail holds the last few lines."""
    t0 = time.time()
    try:
        proc = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout,
                              errors="replace", stdin=subprocess.DEVNULL)
    except FileNotFoundError:
        return RunResult(False, 127, "", f"'{cmd[0]}' is not an executable file.", time.time() - t0)
    except subprocess.TimeoutExpired:
        return RunResult(False, 124, "", f"Gave up after {timeout} seconds.", time.time() - t0)
    except OSError as exc:
        return RunResult(False, 126, "", str(exc), time.time() - t0)
    tail = "\n".join(line for line in proc.stderr.strip().splitlines()[-4:])
    return RunResult(proc.returncode == 0, proc.returncode, proc.stdout, tail, time.time() - t0)


def probe_duration(path, tools: dict) -> float | None:
    """Media duration in seconds via ffprobe, falling back to parsing ffmpeg -i output."""
    if tools.get("ffprobe"):
        res = run([tools["ffprobe"], "-v", "error", "-show_entries", "format=duration",
                   "-of", "default=noprint_wrappers=1:nokey=1", str(path)], timeout=60)
        if res.ok:
            try:
                return float(res.stdout.strip().splitlines()[0])
            except (ValueError, IndexError):
                pass
    if tools.get("path"):
        proc = subprocess.run([tools["path"], "-hide_banner", "-i", str(path)],
                              capture_output=True, text=True, errors="replace", timeout=60)
        m = re.search(r"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", proc.stderr)
        if m:
            return int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))
    return None


def _cleanup(paths) -> None:
    for p in paths:
        try:
            Path(p).unlink()
        except OSError:
            pass


# ---- orchestration -------------------------------------------------------------

def _stage_data(conn, track_id: int) -> dict:
    return {s["key"]: s for s in get_stages(conn, track_id)}


def cut_clip(conn, settings: dict, track: dict, start, length=None, fade=None,
             src: str | None = None, base: Path | None = None) -> dict:
    """Cut the clip for a track. Runs ffmpeg if available, otherwise returns the command."""
    start_s = parse_time(start)
    length_s = parse_time(settings.get("clip_len", 30) if length in (None, "") else length)
    fade_s = parse_time(settings.get("fade", 0.5) if fade in (None, "") else fade)
    stages = _stage_data(conn, track["id"])
    src = (src or "").strip() or stages["master"]["data"].get("path") or stages["download"]["data"].get("path")
    if not src:
        raise UserError("Record the mastered file (stage 2) or the Suno download (stage 1) first, "
                        "so there is something to cut.")
    if not Path(src).expanduser().is_file():
        raise UserError(f"File not found: {src}. Check the path and try again.")
    src = str(Path(src).expanduser())
    folder = track_dir(settings, track["slug"], base) / "clip"
    out_wav = folder / f"{track['slug']}-clip.wav"
    out_mp3 = folder / f"{track['slug']}-clip.mp3"
    part_wav = folder / f".{track['slug']}-clip.part.wav"
    part_mp3 = folder / f".{track['slug']}-clip.part.mp3"
    tools = find_ffmpeg(settings.get("ffmpeg_path", ""))
    ffmpeg = tools["path"] or "ffmpeg"
    shown = build_cut_cmd(src, start_s, length_s, fade_s, out_wav, out_mp3, ffmpeg)
    inputs = {"start": format_time(start_s), "length": length_s, "fade": fade_s, "source": src}
    if not tools["found"]:
        update_stage(conn, track["id"], "clip", data=inputs, verify_paths=False)
        return {"ran": False, "message": tools["message"], "commands": [shell_join(shown)],
                "inputs": inputs}
    try:
        folder.mkdir(parents=True, exist_ok=True)
    except OSError as exc:
        raise UserError(f"Could not create {folder}: {exc.strerror or exc}.")
    cmd = build_cut_cmd(src, start_s, length_s, fade_s, part_wav, part_mp3, ffmpeg)
    res = run(cmd)
    if not res.ok:
        _cleanup([part_wav, part_mp3])
        raise UserError(f"ffmpeg could not cut the clip. It said: {res.stderr_tail or 'nothing'}")
    try:
        os.replace(part_wav, out_wav)
        os.replace(part_mp3, out_mp3)
    except OSError as exc:
        _cleanup([part_wav, part_mp3])
        raise UserError(f"Could not save the clip into {folder}: {exc.strerror or exc}.")
    data = dict(inputs, wav=str(out_wav), mp3=str(out_mp3))
    update_stage(conn, track["id"], "clip", state="done", data=data)
    return {"ran": True, "message": f"Clip saved: {out_wav.name} and {out_mp3.name}.",
            "commands": [shell_join(shown)], "wav": str(out_wav), "mp3": str(out_mp3),
            "seconds": round(res.seconds, 1), "inputs": inputs}


def find_shots(conn, settings: dict, track: dict, base: Path | None = None) -> list[str]:
    """Recorded shot paths from stage 5, else video files found in video/ sorted by name."""
    stages = _stage_data(conn, track["id"])
    recorded = [p for p in stages["video"]["data"].get("shots", []) if p]
    if recorded:
        missing = [p for p in recorded if not Path(p).is_file()]
        if missing:
            raise UserError(f"File not found: {missing[0]}. Re-record the shot paths in stage 5.")
        return recorded
    folder = track_dir(settings, track["slug"], base) / "video"
    if not folder.is_dir():
        return []
    return sorted(str(p) for p in folder.iterdir()
                  if p.is_file() and p.suffix.lower() in VIDEO_EXTS and not p.name.startswith("."))


def assemble(conn, settings: dict, track: dict, aspect: str | None = None,
             base: Path | None = None) -> dict:
    stages = _stage_data(conn, track["id"])
    clip = stages["clip"]["data"]
    audio = clip.get("wav") or clip.get("mp3")
    if not audio:
        raise UserError("Cut the clip first (stage 3), so there is audio to lay the video over.")
    if not Path(audio).is_file():
        raise UserError(f"File not found: {audio}. Cut the clip again in stage 3.")
    shots = find_shots(conn, settings, track, base)
    video_dir = track_dir(settings, track["slug"], base) / "video"
    if not shots:
        raise UserError(f"No shots found. Save the Kling downloads into {video_dir} as shot-01.mp4, "
                        "shot-02.mp4 ... or record their paths in stage 5.")
    aspect = aspect or stages["prompt"]["data"].get("aspect") or settings.get("aspect", "9:16")
    folder = track_dir(settings, track["slug"], base) / "final"
    out = folder / f"{track['slug']}-final.mp4"
    part = folder / f".{track['slug']}-final.part.mp4"
    tools = find_ffmpeg(settings.get("ffmpeg_path", ""))
    ffmpeg = tools["path"] or "ffmpeg"
    shown = build_assemble_cmd(shots, audio, aspect, out, folder / ".work", ffmpeg)
    if not tools["found"]:
        update_stage(conn, track["id"], "assemble", data={"aspect": aspect}, verify_paths=False)
        return {"ran": False, "message": tools["message"], "aspect": aspect, "shots": shots,
                "listfile": shown.listfile, "listfile_text": shown.listfile_text,
                "commands": [shell_join(c) for c in shown.commands]}
    try:
        folder.mkdir(parents=True, exist_ok=True)
        work = Path(tempfile.mkdtemp(prefix=".work-", dir=str(folder)))
    except OSError as exc:
        raise UserError(f"Could not create {folder}: {exc.strerror or exc}.")
    plan = build_assemble_cmd(shots, audio, aspect, part, work, ffmpeg)
    try:
        Path(plan.listfile).write_text(plan.listfile_text, encoding="utf-8")
        t0 = time.time()
        for label, cmd in zip(plan.labels, plan.commands):
            res = run(cmd)
            if not res.ok:
                raise UserError(f"ffmpeg failed while trying to {label}. It said: "
                                f"{res.stderr_tail or 'nothing'}")
        try:
            os.replace(part, out)
        except OSError as exc:
            raise UserError(f"Could not save the video into {folder}: {exc.strerror or exc}.")
    except UserError:
        _cleanup([part])
        raise
    finally:
        shutil.rmtree(work, ignore_errors=True)
    duration = probe_duration(out, tools)
    data = {"final": str(out), "aspect": aspect, "shots": shots, "audio": audio}
    update_stage(conn, track["id"], "assemble", state="done", data=data)
    return {"ran": True, "message": f"Final video saved: {out.name}.", "final": str(out),
            "aspect": aspect, "shots": shots, "duration": duration,
            "seconds": round(time.time() - t0, 1),
            "commands": [shell_join(c) for c in shown.commands]}
