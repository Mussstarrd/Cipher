"""Generation 1 — MEDIA specialist strategies.

Targets audio.pcm (raw signed 16-bit LE mono PCM) and image.rgb (raw 8-bit
RGB rows, width 512). Generic LZ can't exploit numeric smoothness, so both
strategies apply a lossless predictive transform first and entropy-code the
residuals; both are wrapped in a self-describing container whose first byte
is the mode, with a plain-zstd fallback so every strategy is lossless (and
never much worse than zstd) on every corpus file.

Measured rationale (see knowledge/learnings/gen1_media.md for numbers):

* audio: per-block quantized least-squares LPC (FLAC-style, order chosen per
  4096-sample block from {1,2,4,8,16}) beats fixed low-order deltas because
  the corpus tones sit on a white-noise floor — high-order LS prediction
  removes the tonal part without amplifying the noise the way second/third
  differences do. Residuals are zig-zag mapped to uint16 and split into
  byte planes (low bytes are near-uniform noise, high bytes are near-zero),
  then the best of zstd-19 / lzma-6 / brotli-11 is picked.
* image: PNG-style per-row filtering (none/sub/up/average/Paeth by minimum
  sum of absolute signed residuals) over 1536-byte rows, then best-of
  entropy stage. Filtered bytes are within ~1% of their empirical entropy
  bound, so codec choice, not further transform tuning, is the lever.

Container format (first byte = mode):
  0: zstd-19 of the raw input (universal fallback)
  1: audio LPC   — [codec u8][zstd/lzma/brotli payload]
  2: image filter— [codec u8][zstd/lzma/brotli payload]
Compression tries every applicable mode and keeps the smallest blob, so the
transform can never lose to the fallback by more than 0 bytes.
"""

from __future__ import annotations

import lzma
import struct

import brotli
import numpy as np
import zstandard

from . import FuncStrategy, register

# ---------------------------------------------------------------- entropy stage

_CODECS = {
    0: (lambda b: zstandard.ZstdCompressor(level=19).compress(b),
        lambda b: zstandard.ZstdDecompressor().decompress(b)),
    1: (lambda b: lzma.compress(b, preset=6),
        lzma.decompress),
    2: (lambda b: brotli.compress(b, quality=11),
        brotli.decompress),
}


def _entropy_best(payload: bytes) -> bytes:
    """Try all entropy codecs, return [codec u8][compressed]."""
    best = None
    for cid, (enc, _) in _CODECS.items():
        blob = enc(payload)
        if best is None or len(blob) < len(best[1]):
            best = (cid, blob)
    return bytes([best[0]]) + best[1]


def _entropy_open(blob: bytes) -> bytes:
    return _CODECS[blob[0]][1](bytes(blob[1:]))


def _fallback(data: bytes) -> bytes:
    return b"\x00" + zstandard.ZstdCompressor(level=19).compress(data)


# ---------------------------------------------------------------- audio LPC

_A_BLOCK = 4096
_A_QSHIFT = 12
_A_ORDERS = (1, 2, 4, 8, 16)


def _lpc_fit(sig: np.ndarray, hist: np.ndarray, order: int):
    """Least-squares LPC coeffs (quantized int16) and integer residual."""
    x = np.concatenate([hist, sig])
    N = len(sig)
    A = np.stack([x[order - i - 1:order - i - 1 + N] for i in range(order)], axis=1)
    coef, *_ = np.linalg.lstsq(A.astype(np.float64), sig.astype(np.float64), rcond=None)
    q = np.clip(np.round(coef * (1 << _A_QSHIFT)), -(1 << 15), (1 << 15) - 1).astype(np.int64)
    pred = (A @ q) >> _A_QSHIFT
    return q, sig - pred


def _audio_compress(data: bytes) -> bytes | None:
    if len(data) < 4 * _A_ORDERS[-1]:
        return None
    odd = len(data) % 2
    s = np.frombuffer(data[:len(data) - odd], dtype="<i2").astype(np.int64)
    n = len(s)
    nb = (n + _A_BLOCK - 1) // _A_BLOCK
    orders = bytearray()
    coeffs = bytearray()
    res_all = np.empty(n, dtype=np.int64)
    for b in range(nb):
        lo, hi = b * _A_BLOCK, min((b + 1) * _A_BLOCK, n)
        best = None
        for order in _A_ORDERS:
            hist = s[max(0, lo - order):lo]
            if len(hist) < order:
                hist = np.concatenate([np.zeros(order - len(hist), dtype=np.int64), hist])
            try:
                q, res = _lpc_fit(s[lo:hi], hist, order)
            except np.linalg.LinAlgError:
                continue
            cost = int(np.abs(res).sum())
            if best is None or cost < best[0]:
                best = (cost, order, q, res)
        if best is None:
            return None
        _, order, q, res = best
        orders.append(order)
        coeffs += q.astype("<i8").astype("<i2").tobytes()
        res_all[lo:hi] = res
    zz = (res_all << 1) ^ (res_all >> 63)
    if int(zz.max()) >= 1 << 16:
        return None  # residuals too large for the uint16 plane format
    zz16 = zz.astype(np.uint16)
    payload = (
        struct.pack("<IIB", n, _A_BLOCK, odd)
        + (data[-1:] if odd else b"")
        + bytes(orders)
        + bytes(coeffs)
        + (zz16 & 0xFF).astype(np.uint8).tobytes()
        + (zz16 >> 8).astype(np.uint8).tobytes()
    )
    return b"\x01" + _entropy_best(payload)


def _audio_decompress(blob: bytes) -> bytes:
    payload = _entropy_open(blob)
    n, block, odd = struct.unpack_from("<IIB", payload, 0)
    off = 9
    tail = payload[off:off + odd]
    off += odd
    nb = (n + block - 1) // block
    orders = payload[off:off + nb]
    off += nb
    n_coef = sum(orders)
    coeffs = np.frombuffer(payload, dtype="<i2", count=n_coef, offset=off).astype(np.int64)
    off += 2 * n_coef
    lo_p = np.frombuffer(payload, dtype=np.uint8, count=n, offset=off)
    hi_p = np.frombuffer(payload, dtype=np.uint8, count=n, offset=off + n)
    zz = lo_p.astype(np.int64) | (hi_p.astype(np.int64) << 8)
    res = (zz >> 1) ^ -(zz & 1)
    out = np.zeros(n, dtype=np.int64)
    cpos = 0
    for b in range(nb):
        lo, hi = b * block, min((b + 1) * block, n)
        order = orders[b]
        q = coeffs[cpos:cpos + order]
        cpos += order
        # history: previously decoded samples, zero-padded at signal start
        hist = [0] * order
        for k in range(min(order, lo)):
            hist[order - 1 - k] = int(out[lo - 1 - k])
        qs = [int(v) for v in q]
        r = res[lo:hi]
        dec = out
        shift = _A_QSHIFT
        for i in range(lo, hi):
            pred = 0
            for j in range(order):
                pred += qs[j] * hist[order - 1 - j]
            pred >>= shift
            v = int(r[i - lo]) + pred
            dec[i] = v
            hist.pop(0)
            hist.append(v)
    pcm = out.astype("<i2").tobytes()
    return pcm + tail


# ---------------------------------------------------------------- image row filter

_I_WIDTH = 512
_I_BPP = 3
_I_STRIDE = _I_WIDTH * _I_BPP


def _paeth(a: np.ndarray, b: np.ndarray, c: np.ndarray) -> np.ndarray:
    p = a + b - c
    pa, pb, pc = np.abs(p - a), np.abs(p - b), np.abs(p - c)
    return np.where((pa <= pb) & (pa <= pc), a, np.where(pb <= pc, b, c))


def _image_compress(data: bytes) -> bytes | None:
    nrows = len(data) // _I_STRIDE
    if nrows < 4:
        return None
    body = np.frombuffer(data, dtype=np.uint8, count=nrows * _I_STRIDE)
    tail = data[nrows * _I_STRIDE:]
    img = body.reshape(nrows, _I_STRIDE).astype(np.int16)
    zeros = np.zeros(_I_STRIDE, dtype=np.int16)
    choices = bytearray()
    filt = np.empty((nrows, _I_STRIDE), dtype=np.uint8)
    prev = zeros
    for y in range(nrows):
        row = img[y]
        left = np.concatenate([zeros[:_I_BPP], row[:-_I_BPP]])
        up = prev
        ul = np.concatenate([zeros[:_I_BPP], prev[:-_I_BPP]])
        cands = (
            row,
            row - left,
            row - up,
            row - ((left + up) >> 1),
            row - _paeth(left, up, ul),
        )
        best = None
        for fid, cand in enumerate(cands):
            sv = cand.astype(np.int8)  # residual mod 256, signed view
            cost = int(np.abs(sv.astype(np.int16)).sum())
            if best is None or cost < best[0]:
                best = (cost, fid, cand)
        _, fid, cand = best
        choices.append(fid)
        filt[y] = cand.astype(np.uint8)
        prev = row
    payload = (
        struct.pack("<II", nrows, len(tail))
        + bytes(choices)
        + filt.tobytes()
        + tail
    )
    return b"\x02" + _entropy_best(payload)


def _image_decompress(blob: bytes) -> bytes:
    payload = _entropy_open(blob)
    nrows, tail_len = struct.unpack_from("<II", payload, 0)
    off = 8
    choices = payload[off:off + nrows]
    off += nrows
    filt = np.frombuffer(payload, dtype=np.uint8, count=nrows * _I_STRIDE, offset=off)
    filt = filt.reshape(nrows, _I_STRIDE).astype(np.int16)
    tail = payload[off + nrows * _I_STRIDE:off + nrows * _I_STRIDE + tail_len]
    out = np.empty((nrows, _I_STRIDE), dtype=np.uint8)
    prev = [0] * _I_STRIDE
    bpp = _I_BPP
    for y in range(nrows):
        fid = choices[y]
        f = filt[y]
        row = [0] * _I_STRIDE
        if fid == 0:
            row = (f & 0xFF).tolist()
        elif fid == 1:
            for i in range(_I_STRIDE):
                left = row[i - bpp] if i >= bpp else 0
                row[i] = (int(f[i]) + left) & 0xFF
        elif fid == 2:
            row = ((f + np.asarray(prev, dtype=np.int16)) & 0xFF).tolist()
        elif fid == 3:
            for i in range(_I_STRIDE):
                left = row[i - bpp] if i >= bpp else 0
                row[i] = (int(f[i]) + ((left + prev[i]) >> 1)) & 0xFF
        else:  # Paeth
            for i in range(_I_STRIDE):
                left = row[i - bpp] if i >= bpp else 0
                ul = prev[i - bpp] if i >= bpp else 0
                up = prev[i]
                p = left + up - ul
                pa, pb, pc = abs(p - left), abs(p - up), abs(p - ul)
                if pa <= pb and pa <= pc:
                    pred = left
                elif pb <= pc:
                    pred = up
                else:
                    pred = ul
                row[i] = (int(f[i]) + pred) & 0xFF
        out[y] = row
        prev = row
    return out.tobytes() + bytes(tail)


# ---------------------------------------------------------------- containers

def _pick_smallest(data: bytes, transforms) -> bytes:
    best = _fallback(data)
    for fn in transforms:
        blob = fn(data)
        if blob is not None and len(blob) < len(best):
            best = blob
    return best


def _decompress(blob: bytes) -> bytes:
    mode = blob[0]
    rest = bytes(blob[1:])
    if mode == 0:
        return zstandard.ZstdDecompressor().decompress(rest)
    if mode == 1:
        return _audio_decompress(rest)
    if mode == 2:
        return _image_decompress(rest)
    raise ValueError(f"unknown container mode {mode}")


register(FuncStrategy(
    "gen1.media-lpc",
    lambda d: _pick_smallest(d, (_audio_compress,)),
    _decompress,
))
register(FuncStrategy(
    "gen1.media-pngf",
    lambda d: _pick_smallest(d, (_image_compress,)),
    _decompress,
))
register(FuncStrategy(
    "gen1.media-auto",
    lambda d: _pick_smallest(d, (_audio_compress, _image_compress)),
    _decompress,
))
