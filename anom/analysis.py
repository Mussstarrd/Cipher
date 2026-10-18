"""Audio analysis: BPM, key, loudness and hook window.

librosa only (no essentia: it has no reliable Windows wheel). Accuracy is
good enough to fill BeatStars fields; every number carries a confidence so
the daily queue can flag the doubtful ones for a human ear.
"""
from __future__ import annotations

import hashlib
from dataclasses import asdict, dataclass
from pathlib import Path

import librosa
import numpy as np
import pyloudnorm as pyln

SR = 22050

# Krumhansl–Kessler key profiles.
_MAJOR = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88])
_MINOR = np.array([6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17])
_NOTES = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"]
# Conventional spellings: minor keys on D# and A# are written Eb and Bb; F#, C#, G# minor keep sharps.
_FLAT_MINOR = {"D#": "Eb", "A#": "Bb"}
_FLAT_MAJOR = {"C#": "Db", "D#": "Eb", "G#": "Ab", "A#": "Bb"}


@dataclass
class Analysis:
    sha1: str
    duration_s: float
    bpm: float
    bpm_alt: float          # the half- or double-time reading
    bpm_confidence: float   # 0..1, beat-tracker agreement
    key: str                # e.g. "C minor"
    key_camelot: str        # e.g. "5A"
    key_confidence: float   # 0..1, margin over the runner-up
    lufs: float             # integrated loudness
    peak_dbfs: float
    hook_start_s: float
    hook_end_s: float

    def as_dict(self) -> dict:
        d = asdict(self)
        for k, v in d.items():
            if isinstance(v, float):
                d[k] = round(v, 3)
        return d


def sha1_of(path: Path) -> str:
    h = hashlib.sha1()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def load(path: Path) -> tuple[np.ndarray, int]:
    y, sr = librosa.load(str(path), sr=SR, mono=True)
    return y, sr


def detect_bpm(y: np.ndarray, sr: int) -> tuple[float, float, float, np.ndarray]:
    """Returns (bpm, alt_bpm, confidence, beat_times).

    The default tracker has a 120 BPM prior that aliases trap tempos (140
    reads as 93, a 2:3 error). So: take tempo candidates from the global
    tempogram, track beats at each, and keep the grid that catches the
    most onset energy. Hip hop sits in two readable tempos (70 vs 140); we
    report the winner and the other one, and packaging picks which to print.
    """
    hop = 512
    onset = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop)
    tg = librosa.feature.tempogram(onset_envelope=onset, sr=sr, hop_length=hop)
    freqs = librosa.tempo_frequencies(tg.shape[0], sr=sr, hop_length=hop)
    strength = tg.mean(axis=1)
    cands = []
    for i in range(1, len(freqs) - 1):
        f = freqs[i]
        if 55 <= f <= 200 and strength[i] >= strength[i - 1] and strength[i] >= strength[i + 1]:
            cands.append((float(strength[i]), float(f)))
    cands.sort(reverse=True)
    cands = [c for _, c in cands[:6]] or [120.0]
    best = None
    for c in cands:
        _, beats = librosa.beat.beat_track(onset_envelope=onset, sr=sr, hop_length=hop, start_bpm=c, trim=False)
        if len(beats) < 4:
            continue
        bt = librosa.frames_to_time(beats, sr=sr, hop_length=hop)
        ibi = np.diff(bt)
        bpm = 60.0 / float(np.median(ibi))
        hit = float(np.mean(onset[beats])) / (float(np.mean(onset)) + 1e-9)
        cv = float(np.std(ibi) / (np.mean(ibi) + 1e-9))
        score = hit * (1.0 - min(cv, 0.5))
        if best is None or score > best[0]:
            best = (score, bpm, cv, bt)
    if best is None:
        return 120.0, 60.0, 0.0, np.array([])
    _, bpm, cv, beat_times = best
    conf = float(np.clip(1.0 - cv * 4.0, 0.0, 1.0))
    alt = bpm * 2 if bpm < 100 else bpm / 2
    return bpm, alt, conf, beat_times


def detect_key(y: np.ndarray, sr: int) -> tuple[str, str, float]:
    # Key on the harmonic component only; kicks and hats smear the chroma.
    chroma = librosa.feature.chroma_cqt(y=librosa.effects.harmonic(y), sr=sr)
    profile = chroma.mean(axis=1)
    profile = profile / (np.linalg.norm(profile) + 1e-9)
    scores = []
    for tonic in range(12):
        for mode, ref in (("major", _MAJOR), ("minor", _MINOR)):
            r = np.roll(ref, tonic)
            r = r / np.linalg.norm(r)
            scores.append((float(np.dot(profile, r)), tonic, mode))
    scores.sort(reverse=True)
    best, second = scores[0], scores[1]
    # Real mixes separate the top two keys by a few thousandths; 0.02 is a clear call.
    conf = float(np.clip((best[0] - second[0]) * 50.0, 0.0, 1.0))
    tonic, mode = best[1], best[2]
    name = _NOTES[tonic]
    name = (_FLAT_MINOR if mode == "minor" else _FLAT_MAJOR).get(name, name)
    return f"{name} {mode}", camelot(tonic, mode), conf


def camelot(tonic: int, mode: str) -> str:
    # Camelot wheel: 8B = C major, 5A = C minor; +7 semitones = +1 on the wheel.
    major = {0: 8, 7: 9, 2: 10, 9: 11, 4: 12, 11: 1, 6: 2, 1: 3, 8: 4, 3: 5, 10: 6, 5: 7}
    if mode == "major":
        return f"{major[tonic]}B"
    return f"{major[(tonic + 3) % 12]}A"


def loudness(y: np.ndarray, sr: int) -> tuple[float, float]:
    meter = pyln.Meter(sr)
    lufs = float(meter.integrated_loudness(y.astype(np.float64)))
    peak = float(20 * np.log10(np.max(np.abs(y)) + 1e-12))
    return lufs, peak


def find_hook(y: np.ndarray, sr: int, beat_times: np.ndarray, seconds: float = 30.0) -> tuple[float, float]:
    """The loudest, densest `seconds` window that starts on a beat.

    'Hook' for an instrumental means the fullest section. We score each
    candidate window by mean RMS energy plus onset density, and snap the
    start to a beat (every 4th beat preferred, which approximates the bar).
    """
    duration = len(y) / sr
    if duration <= seconds:
        return 0.0, duration
    hop = 512
    rms = librosa.feature.rms(y=y, hop_length=hop)[0]
    onset = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop)
    rms_n = rms / (rms.max() + 1e-9)
    on_n = onset / (onset.max() + 1e-9)
    frames_per_window = int(seconds * sr / hop)
    starts = beat_times[::4] if len(beat_times) > 8 else beat_times
    if len(starts) == 0:
        starts = np.arange(0, duration - seconds, 1.0)
    best, best_t = -1.0, 0.0
    for t in starts:
        if t + seconds > duration:
            break
        f0 = int(t * sr / hop)
        f1 = min(f0 + frames_per_window, len(rms_n))
        score = 0.7 * float(rms_n[f0:f1].mean()) + 0.3 * float(on_n[f0:f1].mean())
        if score > best:
            best, best_t = score, float(t)
    return best_t, min(best_t + seconds, duration)


def analyze(path: Path, preview_seconds: float = 30.0) -> Analysis:
    y, sr = load(path)
    duration = float(len(y) / sr)
    bpm, alt, bconf, beats = detect_bpm(y, sr)
    key, cam, kconf = detect_key(y, sr)
    lufs, peak = loudness(y, sr)
    h0, h1 = find_hook(y, sr, beats, preview_seconds)
    return Analysis(
        sha1=sha1_of(path), duration_s=duration,
        bpm=bpm, bpm_alt=alt, bpm_confidence=bconf,
        key=key, key_camelot=cam, key_confidence=kconf,
        lufs=lufs, peak_dbfs=peak, hook_start_s=h0, hook_end_s=h1,
    )
