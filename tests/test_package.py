"""Step 3b: the BeatStars package is complete, on-brand and repeatable."""
import json
from pathlib import Path

from PIL import Image

from anom.cli import main
from anom.package import (
    ANOMALY_PLACEHOLDER, BANNED, LANES, build_tags, description, display_bpm,
    lane_key, tier_prices, type_beat_title,
)
from anom.preview import ffmpeg_available

_REF = Path(__file__).resolve().parent.parent / "docs" / "publicity" / "reference"
# v2 is the current sheet but lives only on the owner's machine; v1 is committed and stands in elsewhere.
STILL = next(p for p in (_REF / "persona-three-v2.jpg", _REF / "persona-three-v1.jpg") if p.exists())


def test_title_convention():
    assert type_beat_title("brass alone", "Travis Scott", "cold-southern") == '"BRASS ALONE" | Travis Scott Type Beat'
    assert type_beat_title("brass alone", None, "cold-southern") == '"BRASS ALONE" | Cold Southern Type Beat'
    assert type_beat_title("brass alone", None, "trap", type_beat=False) == '"BRASS ALONE"'


def test_lane_lookup_accepts_labels_and_slugs():
    assert lane_key("Cold Southern") == "cold-southern"
    assert lane_key("trunk_808") == "trunk-808"
    assert lane_key(None) == "trap"
    try:
        lane_key("polka")
    except ValueError as e:
        assert "polka" in str(e)
    else:
        raise AssertionError("unknown lane must raise")


def test_bpm_is_listed_at_the_fast_reading():
    assert display_bpm({"bpm": 66.3, "bpm_alt": 132.6}) == 133
    assert display_bpm({"bpm": 140.0, "bpm_alt": 70.0}) == 140


def test_tiers_carry_default_prices_and_trackout_waits_for_stems():
    t = {r["tier"]: r for r in tier_prices()}
    assert t["mp3"]["price_usd"] == 29.99
    assert t["wav"]["price_usd"] == 49.99
    assert t["trackout"]["price_usd"] == 99.99 and t["trackout"]["available"] is False
    assert t["unlimited"]["price_usd"] == 199.99
    assert t["exclusive"]["price_usd"] is None and t["exclusive"]["available"] is True


def test_description_names_the_anomaly_or_flags_it():
    d = description("Brass Alone", 132, "F# minor", "11A", "cold-southern", "the brass that trips on three", tier_prices())
    assert "The anomaly in this one is the brass that trips on three." in d
    assert "MP3 Lease $29.99" in d and "Exclusive by offer" in d and "Trackout" not in d
    assert d.rstrip().endswith("— ANOM")
    assert "type beat" not in d.lower()          # the handle lives in the title only
    d2 = description("X", 140, "C minor", "5A", "trap", None, tier_prices())
    assert ANOMALY_PLACEHOLDER in d2


def test_tags_are_clean_and_bounded():
    tags = build_tags("x", "Travis Scott", "cold-southern", "F# minor", 132, "the brass that trips on three")
    assert tags[0] == "travis scott type beat"
    assert "brass" in tags and "132 bpm" in tags and "f# minor" in tags
    assert len(tags) <= 10 and len(set(tags)) == len(tags)
    assert not any(b in tags for b in BANNED)
    for lane in LANES:
        assert not any(w in BANNED for w in LANES[lane]["tags"])


def test_package_cli_end_to_end(tmp_path, beat_file):
    inbox = tmp_path / "inbox"; inbox.mkdir()
    (inbox / beat_file.name).write_bytes(beat_file.read_bytes())
    out = tmp_path / "out"; catalog = tmp_path / "catalog.json"
    assert main(["ingest", "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog)]) == 0
    (bid,) = json.loads(catalog.read_text())["beats"]

    rc = main(["package", bid, "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog),
               "--still", str(STILL), "--title", "Cold Test", "--lane", "cold-southern",
               "--anomaly", "the hat that trips on three"])
    assert rc == 0
    e = json.loads(catalog.read_text())["beats"][bid]
    pkg = e["package"]
    assert e["status"] == "packaged"
    assert e["anomaly"] == "the hat that trips on three"
    assert pkg["display_title"] == '"COLD TEST" | Cold Southern Type Beat'
    assert pkg["genre"] == "Trap" and pkg["moods"] == ["cold", "dark", "heavy"]
    assert pkg["key"] == "C minor" and pkg["camelot"] == "5A" and 137 <= pkg["bpm"] <= 143
    assert len(pkg["licenses"]) == 5
    assert "ANOMALY?" not in pkg["flags"]

    pdir = out / bid / "package"
    cover = Image.open(pdir / "cover.jpg")
    assert cover.size == (3000, 3000) and cover.mode == "RGB"
    cf = (pdir / "copy-fields.txt").read_text(encoding="utf-8")
    assert "=== Title ===" in cf and pkg["display_title"] in cf and "MP3 Lease: $29.99" in cf
    assert "Trackout Lease: $99.99  (not listed: no stems yet)" in cf
    md = json.loads((pdir / "metadata.json").read_text(encoding="utf-8"))
    assert md["display_title"] == pkg["display_title"]
    if ffmpeg_available():
        # the source is a WAV, so both deliverables exist and the mp3 is tagged
        assert (pdir / "cold-southern-test.mp3").exists() and (pdir / "cold-southern-test.wav").exists()
        assert pkg["files"]["mp3"].endswith("cold-southern-test.mp3") and pkg["files"]["wav"]
        assert (pdir / "preview.mp3").exists()

    # second run without --force is a no-op; with --force it rebuilds and keeps the anomaly
    assert main(["package", bid, "--out", str(out), "--catalog", str(catalog), "--inbox", str(inbox)]) == 0
    assert main(["package", bid, "--out", str(out), "--catalog", str(catalog), "--inbox", str(inbox),
                 "--still", str(STILL), "--force"]) == 0
    e = json.loads(catalog.read_text())["beats"][bid]
    assert e["anomaly"] == "the hat that trips on three"
    assert "the hat that trips on three" in e["package"]["description"]


def test_ingest_sets_render_date_on_an_existing_entry(tmp_path, beat_file):
    inbox = tmp_path / "inbox"; inbox.mkdir()
    (inbox / beat_file.name).write_bytes(beat_file.read_bytes())
    out = tmp_path / "out"; catalog = tmp_path / "catalog.json"
    main(["ingest", "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog)])
    (e,) = json.loads(catalog.read_text())["beats"].values()
    assert e["rights"]["render_date"] is None
    main(["ingest", "--inbox", str(inbox), "--out", str(out), "--catalog", str(catalog), "--render-date", "2026-10-19"])
    (e,) = json.loads(catalog.read_text())["beats"].values()
    assert e["rights"]["render_date"] == "2026-10-19"
