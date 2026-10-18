"""Synthetic beats with known answers, so the suite runs without real renders."""
from __future__ import annotations

import numpy as np
import pytest
import soundfile as sf

SR = 22050


def _tone(freq: float, n: int, sr: int = SR, amp: float = 0.2) -> np.ndarray:
    t = np.arange(n) / sr
    return (amp * np.sin(2 * np.pi * freq * t)).astype(np.float32)


def _midi(m: float) -> float:
    return 440.0 * 2 ** ((m - 69) / 12)


def make_beat(bpm: float, seconds: float, tonic_midi: int, minor: bool, sr: int = SR) -> np.ndarray:
    """Kick on every beat, hat on 8ths, a sustained triad-plus-colour chord in the key,
    and a loud 'hook' section in the second half so the hook finder has something to find."""
    n = int(seconds * sr)
    y = np.zeros(n, dtype=np.float32)
    beat = 60.0 / bpm
    # drums
    k = int(0.08 * sr)
    kick = (_tone(55, k, sr, 0.9) * np.exp(-np.arange(k) / (0.02 * sr))).astype(np.float32)
    h = int(0.02 * sr)
    hat = (np.random.default_rng(0).standard_normal(h) * 0.15 * np.exp(-np.arange(h) / (0.004 * sr))).astype(np.float32)
    t = 0.0
    while t < seconds:
        i = int(t * sr)
        y[i:i + k] += kick[: max(0, min(k, n - i))]
        j = int((t + beat / 2) * sr)
        if j < n:
            y[j:j + h] += hat[: max(0, min(h, n - j))]
        y[i:i + h] += hat[: max(0, min(h, n - i))]
        t += beat
    # harmony: 1, b3/3, 5, b7/7, plus the 6th to pin the mode
    third = 3 if minor else 4
    seventh = 10 if minor else 11
    sixth = 8 if minor else 9
    for deg, amp in ((0, 0.12), (third, 0.10), (7, 0.10), (seventh, 0.06), (sixth, 0.05), (12, 0.05)):
        y += _tone(_midi(tonic_midi + deg), n, sr, amp)
    # hook: second half is 6 dB louder
    half = n // 2
    y[half:] *= 2.0
    y = y / (np.max(np.abs(y)) + 1e-9) * 0.9
    return y


@pytest.fixture(scope="session")
def beat_file(tmp_path_factory):
    p = tmp_path_factory.mktemp("audio") / "cold-southern-test.wav"
    y = make_beat(bpm=140, seconds=70, tonic_midi=48, minor=True)  # C minor, 140 bpm
    sf.write(p, y, SR)
    return p


@pytest.fixture(scope="session")
def major_file(tmp_path_factory):
    p = tmp_path_factory.mktemp("audio") / "bounce-test.wav"
    y = make_beat(bpm=96, seconds=40, tonic_midi=55, minor=False)  # G major, 96 bpm
    sf.write(p, y, SR)
    return p
