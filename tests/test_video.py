"""The renderer must find a font on whatever machine runs it (Linux cloud, Windows Lenovo)."""
from pathlib import Path

import pytest

from anom.video import FONT_BOLD_CANDIDATES, FONT_CANDIDATES, find_font, fonts


def test_fonts_resolve_on_this_machine():
    bold, regular = fonts()
    assert Path(bold).is_file() and bold.lower().endswith(".ttf")
    assert Path(regular).is_file() and regular.lower().endswith(".ttf")


def test_env_override_wins(monkeypatch, tmp_path):
    f = tmp_path / "custom.ttf"
    f.write_bytes(b"not really a font")
    monkeypatch.setenv("ANOM_FONT_BOLD", str(f))
    monkeypatch.setenv("ANOM_FONT", str(f))
    assert fonts() == (str(f), str(f))


def test_missing_fonts_raise_clearly():
    with pytest.raises(RuntimeError, match="ANOM_FONT"):
        find_font(("/nope/one.ttf", "/nope/two.ttf"))
