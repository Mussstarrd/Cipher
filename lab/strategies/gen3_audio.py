"""Generation 3 — AUDIO specialist (single question: audio.pcm prediction headroom).

Gen2's convergence analysis left one open question: audio.pcm had ~3-4%
measured headroom reachable only through better *prediction* (gen1 showed the
entropy stage was already at the residual's empirical entropy). Gen1's
per-block least-squares LPC (order <= 16) left residual std ~316 vs the
generator's noise floor sigma ~= 262.

The answer (measured, see knowledge/learnings/gen3_audio.md): model the
signal as what it actually is. The corpus audio is two sinusoids whose
frequencies re-drift every 4410 samples, plus white Gaussian noise. Per
aligned 4410-sample block, estimate both frequencies (windowed zero-padded
FFT peaks + coordinate-descent refinement minimizing joint least-squares
residual power), solve linearly for the 4 cos/sin amplitudes, quantize
(freq -> u32 turns, amplitudes -> i16), and subtract the integer-rounded
tone prediction. Residual std drops to 262.5 = the noise floor exactly, and
the residual is white (|autocorr| < 0.004 at lags 1-10) — the prediction
ceiling is *reached*, not just approached.

The whiter residual defeats brotli's context modeling (byte planes leave
0.9% vs its entropy), so the residual is coded with a *static discretized-
Gaussian arithmetic coder* (gen2's parametric-AC insight, gen1_text's coder
primitives): fit sigma, build an integer pmf over [-R, R], code each value
directly. This lands within ~0.1% of the residual's empirical entropy.

Container (first byte = mode):
  0/1/2: gen1_media's container modes (zstd-19 fallback / audio LPC / image
         row filter) — decode delegates to gen1_media.
  3:     gen3 sinusoid model, this module.
Compression tries every applicable transform and keeps the smallest blob;
mode-3 compression additionally self-verifies its own roundtrip and
withdraws (returns None) on any mismatch, so losslessness never depends on
floating-point trig reproducibility across environments.
"""

from __future__ import annotations

import math
import struct

import numpy as np

from . import FuncStrategy, register
from .gen1_media import (
    _audio_compress as _g1_audio,
    _image_compress as _g1_image,
    _decompress as _g1_decompress,
    _pick_smallest,
)
from .gen1_text import _AEnc, _ADec

_BLK = 4410           # generator re-drifts tone frequencies every 4410 samples
_NTONES = 2
_TOT = 1 << 18        # arithmetic-coder frequency-table total
_MIN_FIT = 64         # min samples to least-squares fit a block
_TWO32 = float(1 << 32)


# ------------------------------------------------------------ sinusoid fitting

def _resid_power(xf: np.ndarray, t: np.ndarray, ws: list[float]):
    """LS-fit cos/sin at frequencies ws; return (residual power, coefs)."""
    cols = []
    for w in ws:
        cols.append(np.cos(w * t))
        cols.append(np.sin(w * t))
    A = np.stack(cols, axis=1)
    G = A.T @ A
    b = A.T @ xf
    try:
        coef = np.linalg.solve(G, b)
    except np.linalg.LinAlgError:
        coef, *_ = np.linalg.lstsq(A, xf, rcond=None)
    r = xf - A @ coef
    return float(r @ r), coef


def _fit_block(x: np.ndarray):
    """Estimate _NTONES sinusoids: FFT peak init + coordinate-descent refine."""
    N = len(x)
    t = np.arange(N, dtype=np.float64)
    xf = x.astype(np.float64)
    F = np.abs(np.fft.rfft(xf * np.hanning(N), 8 * N))
    F[0:8] = 0.0
    p1 = int(np.argmax(F))
    ws = [2 * math.pi * p1 / (8 * N)]
    F2 = F.copy()
    F2[max(0, p1 - 24):p1 + 24] = 0.0
    p2 = int(np.argmax(F2))
    ws.append(2 * math.pi * p2 / (8 * N))
    best_p, _ = _resid_power(xf, t, ws)
    for rnd in range(2):
        for idx in range(_NTONES):
            span = 2 * math.pi / N if rnd == 0 else 2 * math.pi / (8 * N)
            bw = ws[idx]
            for _ in range(16):
                for c in (bw - span, bw - span / 2, bw + span / 2, bw + span):
                    trial = list(ws)
                    trial[idx] = c
                    p, _ = _resid_power(xf, t, trial)
                    if p < best_p:
                        best_p, bw = p, c
                span *= 0.5
            ws[idx] = bw
    _, coef = _resid_power(xf, t, ws)
    return ws, coef, best_p


def _block_pred(wq: list[int], cq: list[int], N: int) -> np.ndarray:
    """Integer tone prediction from quantized params (u32 freq, i16 coefs).

    Used identically by compressor and decompressor, so the residual the
    compressor stores is defined against exactly this reconstruction.
    """
    t = np.arange(N, dtype=np.float64)
    pred = np.zeros(N, dtype=np.float64)
    for k in range(_NTONES):
        w = wq[k] * (2.0 * math.pi) / _TWO32
        pred += float(cq[2 * k]) * np.cos(w * t) + float(cq[2 * k + 1]) * np.sin(w * t)
    return np.round(pred).astype(np.int64)


# ------------------------------------------------- static Gaussian entropy coder

def _gauss_table(sigma_q: int, R: int) -> np.ndarray:
    """Cumulative freq table (len 2R+2, sum _TOT) of discretized N(0, sigma_q/16)."""
    sigma = sigma_q / 16.0
    k = np.arange(-R, R + 1, dtype=np.float64)
    rt2 = sigma * math.sqrt(2.0)
    erf = np.vectorize(math.erf)
    p = 0.5 * (erf((k + 0.5) / rt2) - erf((k - 0.5) / rt2))
    p /= p.sum()
    f = np.maximum(1, np.floor(p * _TOT).astype(np.int64))
    diff = _TOT - int(f.sum())
    order = np.argsort(-p, kind="stable")
    i = 0
    while diff != 0:
        j = order[i % len(order)]
        if diff > 0:
            f[j] += 1
            diff -= 1
        elif f[j] > 1:
            f[j] -= 1
            diff += 1
        i += 1
    cum = np.zeros(len(f) + 1, dtype=np.int64)
    np.cumsum(f, out=cum[1:])
    return cum


def _ac_encode(res: np.ndarray, cum: np.ndarray, R: int) -> bytes:
    idx = res + R
    enc = _AEnc()
    encode = enc.encode
    for cl, ch in zip(cum[idx].tolist(), cum[idx + 1].tolist()):
        encode(cl, ch, _TOT)
    return enc.finish()


def _ac_decode(blob: bytes, cum: np.ndarray, R: int, count: int) -> np.ndarray:
    dec = _ADec(blob)
    out = np.empty(count, dtype=np.int64)
    target = dec.target
    consume = dec.consume
    search = np.searchsorted
    for i in range(count):
        t = target(_TOT)
        j = int(search(cum, t, side="right")) - 1
        consume(int(cum[j]), int(cum[j + 1]), _TOT)
        out[i] = j - R
    return out


# ------------------------------------------------------------------ mode 3 codec

def _sin_transform(data: bytes) -> bytes | None:
    if len(data) < 4 * _BLK:
        return None
    odd = len(data) % 2
    s = np.frombuffer(data[:len(data) - odd], dtype="<i2").astype(np.int64)
    n = len(s)
    nb = (n + _BLK - 1) // _BLK

    # cheap tonality gate: fit the first block; if two sinusoids don't
    # explain most of the variance this is not our kind of signal.
    x0 = s[:_BLK].astype(np.float64)
    p0 = float(x0 @ x0)
    if p0 <= 0.0:
        return None
    _, _, rp0 = _fit_block(s[:_BLK])
    if rp0 > 0.5 * p0:
        return None

    params = bytearray()
    res_all = np.empty(n, dtype=np.int64)
    for b in range(nb):
        lo, hi = b * _BLK, min((b + 1) * _BLK, n)
        N = hi - lo
        if N >= _MIN_FIT:
            ws, coef, _ = _fit_block(s[lo:hi])
            wq = [int(round((w % (2 * math.pi)) / (2 * math.pi) * (1 << 32))) & 0xFFFFFFFF
                  for w in ws]
            cq = [int(v) for v in np.clip(np.round(coef), -32768, 32767).astype(np.int64)]
        else:
            wq = [0] * _NTONES
            cq = [0] * (2 * _NTONES)
        params += struct.pack("<IIhhhh", wq[0], wq[1], *cq)
        res_all[lo:hi] = s[lo:hi] - _block_pred(wq, cq, N)

    R = int(np.abs(res_all).max())
    if R == 0 or R > 20000:
        return None  # degenerate or model failed; other modes will handle it
    sigma_q = min(0xFFFF, max(8, int(round(float(res_all.std()) * 16))))
    cum = _gauss_table(sigma_q, R)
    body = _ac_encode(res_all, cum, R)
    return (
        b"\x03"
        + struct.pack("<IBHH", n, odd, sigma_q, R)
        + (data[-1:] if odd else b"")
        + bytes(params)
        + body
    )


def _sin_untransform(blob: bytes) -> bytes:
    n, odd, sigma_q, R = struct.unpack_from("<IBHH", blob, 0)
    off = 9
    tail = blob[off:off + odd]
    off += odd
    nb = (n + _BLK - 1) // _BLK
    psz = struct.calcsize("<IIhhhh")
    cum = _gauss_table(sigma_q, R)
    res = _ac_decode(blob[off + nb * psz:], cum, R, n)
    out = np.empty(n, dtype=np.int64)
    for b in range(nb):
        lo, hi = b * _BLK, min((b + 1) * _BLK, n)
        w1, w2, c1, c2, c3, c4 = struct.unpack_from("<IIhhhh", blob, off + b * psz)
        out[lo:hi] = res[lo:hi] + _block_pred([w1, w2], [c1, c2, c3, c4], hi - lo)
    return out.astype("<i2").tobytes() + bytes(tail)


def _sin_compress(data: bytes) -> bytes | None:
    """Mode-3 transform with mandatory self-verification (rule 1 insurance)."""
    try:
        blob = _sin_transform(data)
        if blob is None:
            return None
        if _sin_untransform(memoryview(blob)[1:]) != data:
            return None
    except Exception:
        return None
    return blob


# ------------------------------------------------------------------- container

def _decompress(blob: bytes) -> bytes:
    if blob[0] == 3:
        return _sin_untransform(bytes(blob[1:]))
    return _g1_decompress(blob)


register(FuncStrategy(
    "gen3.audio-sin",
    lambda d: _pick_smallest(d, (_sin_compress,)),
    _decompress,
))
register(FuncStrategy(
    "gen3.audio-auto",
    lambda d: _pick_smallest(d, (_sin_compress, _g1_audio, _g1_image)),
    _decompress,
))
