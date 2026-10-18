import json
import math
from pathlib import Path

import pytest

from anom.analysis import analyze, camelot
from anom import catalog as cat
from anom.cli import main
from anom.preview import ffmpeg_available


def _close_tempo(got, want, tol=3.0):
    return any(abs(got - w) <= tol for w in (want, want / 2, want * 2))


def test_bpm_and_key_minor(beat_file):
    a = analyze(beat_file)
    assert _close_tempo(a.bpm, 140), a.bpm
    assert a.key == "C minor", a.key
    assert a.key_camelot == "5A"
    assert a.bpm_confidence > 0.5
    assert math.isfinite(a.lufs) and a.lufs < 0
    assert a.peak_dbfs <= 0


def test_key_major(major_file):
    a = analyze(major_file)
    assert _close_tempo(a.bpm, 96), a.bpm
    assert a.key == "G major", a.key
    assert a.key_camelot == "9B"


def test_hook_lands_in_loud_half(beat_file):
    a = analyze(beat_file, preview_seconds=30)
    # the second half of the synthetic beat is 6 dB louder; the hook must start there
    assert a.hook_start_s >= 30.0, a.hook_start_s
    assert a.hook_end_s - a.hook_start_s == pytest.approx(30.0, abs=0.01)


def test_camelot_table():
    assert camelot(0, "major") == "8B"
    assert camelot(0, "minor") == "5A"
    assert camelot(9, "minor") == "8A"   # A minor is relative of C major


def test_ingest_cli_builds_catalogue(tmp_path, beat_file):
    inbox = tmp_path / "inbox"; inbox.mkdir()
    (inbox / beat_file.name).write_bytes(beat_file.read_bytes())
    out = tmp_path / "out"; catalog = tmp_path / "catalog.json"
    rc = main(["ingest", "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog),
               "--suno-plan", "Pro", "--render-date", "2026-10-18"])
    assert rc == 0
    data = json.loads(catalog.read_text())
    assert len(data["beats"]) == 1
    (bid, e), = data["beats"].items()
    assert bid.startswith("cold-southern-test-")
    assert e["status"] == "analyzed"
    assert e["rights"]["commercial_ok"] is True
    assert e["rights"]["render_date"] == "2026-10-18"
    if ffmpeg_available():
        assert (out / bid / "preview.mp3").exists()
    # second run is idempotent
    rc = main(["ingest", "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog)])
    assert rc == 0
    assert len(json.loads(catalog.read_text())["beats"]) == 1


def test_free_plan_is_not_commercial(tmp_path, beat_file):
    e = cat.new_entry("x", beat_file, {"sha1": "abc"}, "Free", None)
    assert e["rights"]["commercial_ok"] is False
