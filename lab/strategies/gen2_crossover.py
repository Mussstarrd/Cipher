"""Generation 2 — CROSSOVER specialist.

Combines the measured winners from gen1 across specialties:

  * gen1_structured's field-splitting transforms for logs.jsonl / series.csv
    (imported and reused, not reimplemented) provide the per-field streams.
  * The per-stream codec menu is extended beyond gen1's static
    {raw, zstd-19, lzma-6, brotli-11} with two entropy-coder back-ends built
    on gen1_text's arithmetic-coder primitives:

      codec 4  reduced-alphabet adaptive order-0 AC
               (dict of present byte values + adaptive counts; wins on
               small-alphabet index streams where brotli's block overhead
               dominates: enum rows, msg word indices)
      codec 6  PARAMETRIC STATIC AC over uvarint-decoded values
               (fits Gaussian / exponential / lognormal, quantizes the
               parameters into the header, codes against the model's
               quantized pmf; the media agent's "model the residual
               distribution" idea applied to the structured agent's
               varint residual streams.  Wins on the Gaussian CSV delta
               columns, the exponential ts inter-arrivals, and the
               lognormal latency stream, where any count-based adaptive
               model pays more in adaptation than the parametric model
               pays in header bytes.)

Strategies
----------
gen2.x-struct  logs.jsonl / series.csv via gen1 transforms + extended codec
               menu; zstd-19 fallback elsewhere.
gen2.x-auto    best-of-lab ensemble: min over {x-struct modes,
               gen1.text-token container, gen1.media transforms, zstd-19},
               one mode byte of overhead.  Every candidate is itself
               self-describing, and compress() verifies its own roundtrip,
               falling back to plain zstd on any mismatch.

Measured rationale in knowledge/learnings/gen2_crossover.md.
"""

from __future__ import annotations

from bisect import bisect_right

import numpy as np

from . import FuncStrategy, register
from . import gen1_media as gm
from . import gen1_structured as gs
from . import gen1_text as gt

MODE_RAWZ = 0    # zstd-19 fallback
MODE_LOGS = 1    # gen1 logs streams, repacked with extended codec menu
MODE_CSV = 2     # gen1 csv streams, repacked
MODE_TEXT = 3    # wrapped gen1.text-token blob
MODE_MEDIA = 4   # wrapped gen1.media blob

# --------------------------------------------------------------------------
# codec 4: reduced-alphabet adaptive order-0 arithmetic coding
# (same INC/CAP adaptation schedule as gen1_text's order-1 coder)

_INC = 32
_CAP = 1 << 16
_AO0_MAX_WORK = 8_000_000   # n_symbols * alphabet cap (pure-Python speed)


def _ao0_encode(syms: list[int], alpha: int) -> bytes:
    enc = gt._AEnc()
    c = [1] * alpha
    tot = alpha
    for s in syms:
        cl = sum(c[:s])
        enc.encode(cl, cl + c[s], tot)
        c[s] += _INC
        tot += _INC
        if tot > _CAP:
            tot = 0
            for i in range(alpha):
                c[i] = (c[i] + 1) >> 1
                tot += c[i]
    return enc.finish()


def _ao0_decode(blob: bytes, alpha: int, count: int) -> list[int]:
    dec = gt._ADec(blob)
    c = [1] * alpha
    tot = alpha
    out = []
    for _ in range(count):
        tgt = dec.target(tot)
        acc = 0
        s = 0
        while acc + c[s] <= tgt:
            acc += c[s]
            s += 1
        dec.consume(acc, acc + c[s], tot)
        out.append(s)
        c[s] += _INC
        tot += _INC
        if tot > _CAP:
            tot = 0
            for i in range(alpha):
                c[i] = (c[i] + 1) >> 1
                tot += c[i]
    return out


def _c4_encode(s: bytes) -> bytes | None:
    present = sorted(set(s))
    nd = len(present)
    if not 1 <= nd <= 255:
        return None
    if len(s) * nd > _AO0_MAX_WORK:
        return None
    idx = {b: i for i, b in enumerate(present)}
    body = _ao0_encode([idx[b] for b in s], nd)
    out = bytearray([nd])
    out += bytes(present)
    gs.uvarint(len(s), out)
    out += body
    return bytes(out)


def _c4_decode(payload: bytes) -> bytes:
    nd = payload[0]
    present = payload[1 : 1 + nd]
    n, pos = gs.read_uvarint(payload, 1 + nd)
    syms = _ao0_decode(payload[pos:], nd, n)
    return bytes(present[i] for i in syms)


# --------------------------------------------------------------------------
# codec 6: parametric static AC over uvarint-decoded values
#
# payload: [interp u8][family u8][zz-varint lo][zz-varint hi]
#          [family params as zz-varints][uvarint n][uvarint n_esc]
#          [uvarint side_len][side bytes: n_esc zz-varints][AC blob]
# interp: 0 = raw uvarint values, 1 = unzigzag to signed
# family: 0 = gaussian(mu16, sd16)  1 = exponential(mean16)
#         2 = lognormal(mu4096, sd4096) over (v - lo + 1)
# Values outside [lo, hi] (e.g. the absolute first value that gen1's delta
# streams carry) are coded via an ESCAPE symbol + verbatim side stream, so
# a handful of outliers cannot wreck the model range.

_C6_PREC = 1 << 17
_C6_MAX_SPAN = 1 << 16
_C6_MIN_N = 64
_C6_MAX_ESC_FRAC = 50   # reject if more than n/50 escapes


def _parse_uvarints(buf: bytes) -> list[int] | None:
    vals = []
    pos = 0
    n = len(buf)
    while pos < n:
        try:
            v, pos = gs.read_uvarint(buf, pos)
        except IndexError:
            return None
        if v >= 1 << 62:
            return None
        vals.append(v)
    return vals


def _c6_pmf(lo: int, hi: int, family: int, params: list[int],
            n: int, n_esc: int) -> tuple[np.ndarray, list[int], int]:
    """Deterministic freq table from quantized params (+escape symbol)."""
    xs = np.arange(lo, hi + 1, dtype=np.float64)
    if family == 0:
        mu = params[0] / 16.0
        sd = max(params[1], 1) / 16.0
        p = np.exp(-0.5 * ((xs - mu) / sd) ** 2)
    elif family == 1:
        mean = max(params[0], 1) / 16.0
        p = np.exp(-(xs - lo) / mean)
    else:
        mu = params[0] / 4096.0
        sd = max(params[1], 1) / 4096.0
        t = xs - lo + 1.0
        p = np.exp(-0.5 * ((np.log(t) - mu) / sd) ** 2) / t
    p = np.maximum(p, 1e-300)
    f = np.maximum(1, np.round(p / p.sum() * (_C6_PREC - len(p)))).astype(np.int64)
    if n_esc > 0:
        f_esc = max(1, (n_esc * _C6_PREC) // max(n, 1))
        f = np.concatenate((f, [f_esc]))
    cum = [0]
    acc = 0
    for v in f.tolist():
        acc += v
        cum.append(acc)
    return f, cum, acc


def _c6_fit(inr: np.ndarray, lo: int, family: int) -> list[int]:
    if family == 0:
        return [round(float(inr.mean()) * 16), max(1, round(float(inr.std()) * 16))]
    if family == 1:
        return [max(1, round(float((inr - lo).mean()) * 16))]
    t = np.log((inr - lo).astype(np.float64) + 1.0)
    return [round(float(t.mean()) * 4096), max(1, round(float(t.std()) * 4096))]


def _zz_varint(v: int, out: bytearray) -> None:
    gs.uvarint(gs.zigzag(v), out)


def _read_zz(buf: bytes, pos: int) -> tuple[int, int]:
    z, pos = gs.read_uvarint(buf, pos)
    return gs.unzigzag(z), pos


def _c6_encode(s: bytes) -> bytes | None:
    raw = _parse_uvarints(s)
    if raw is None or len(raw) < _C6_MIN_N:
        return None
    n = len(raw)
    best = None
    for interp in (0, 1):
        vals = np.array([gs.unzigzag(v) for v in raw] if interp else raw, dtype=np.int64)
        sv = np.sort(vals)
        ranges = set()
        for k in (0, 1, 2, 4, max(1, n // 500)):
            if 2 * k < n:
                ranges.add((int(sv[k]), int(sv[-1 - k])))
        for lo, hi in ranges:
            if not 2 <= hi - lo + 1 <= _C6_MAX_SPAN:
                continue
            mask = (vals >= lo) & (vals <= hi)
            n_esc = int(n - mask.sum())
            if n_esc > n // _C6_MAX_ESC_FRAC:
                continue
            inr = vals[mask]
            for family in (0, 1, 2):
                params = _c6_fit(inr, lo, family)
                f, cum, tot = _c6_pmf(lo, hi, family, params, n, n_esc)
                est = float(np.log2(tot / f[inr - lo]).sum()) / 8
                if n_esc:
                    est += n_esc * (np.log2(tot / f[-1]) / 8 + 5)
                if best is None or est < best[0]:
                    best = (est, interp, family, params, lo, hi, vals, cum, tot, n_esc)
    if best is None:
        return None
    _, interp, family, params, lo, hi, vals, cum, tot, n_esc = best
    span = hi - lo + 1
    enc = gt._AEnc()
    side = bytearray()
    for v in vals.tolist():
        if lo <= v <= hi:
            i = v - lo
        else:
            i = span  # escape symbol
            _zz_varint(v, side)
        enc.encode(cum[i], cum[i + 1], tot)
    body = enc.finish()
    out = bytearray([interp, family])
    _zz_varint(lo, out)
    _zz_varint(hi, out)
    for p in params:
        _zz_varint(p, out)
    gs.uvarint(n, out)
    gs.uvarint(n_esc, out)
    gs.uvarint(len(side), out)
    out += side
    out += body
    return bytes(out)


_C6_NPARAMS = {0: 2, 1: 1, 2: 2}


def _c6_decode(payload: bytes) -> bytes:
    interp, family = payload[0], payload[1]
    pos = 2
    lo, pos = _read_zz(payload, pos)
    hi, pos = _read_zz(payload, pos)
    params = []
    for _ in range(_C6_NPARAMS[family]):
        p, pos = _read_zz(payload, pos)
        params.append(p)
    n, pos = gs.read_uvarint(payload, pos)
    n_esc, pos = gs.read_uvarint(payload, pos)
    side_len, pos = gs.read_uvarint(payload, pos)
    side = payload[pos : pos + side_len]
    pos += side_len
    esc_vals = []
    sp = 0
    for _ in range(n_esc):
        v, sp = _read_zz(side, sp)
        esc_vals.append(v)
    _, cum, tot = _c6_pmf(lo, hi, family, params, n, n_esc)
    span = hi - lo + 1
    dec = gt._ADec(payload[pos:])
    out = bytearray()
    ei = 0
    for _ in range(n):
        tgt = dec.target(tot)
        i = bisect_right(cum, tgt) - 1
        dec.consume(cum[i], cum[i + 1], tot)
        if i == span:
            v = esc_vals[ei]
            ei += 1
        else:
            v = lo + i
        gs.uvarint(gs.zigzag(v) if interp else v, out)
    return bytes(out)


# --------------------------------------------------------------------------
# extended per-stream packing (superset of gen1_structured's codecs 0-3)

_C4_MAX_LEN = 1 << 16


def _pack_stream2(s: bytes) -> bytes:
    best = gs._pack_stream(s)          # codecs 0-3: raw/zstd/lzma/brotli
    extra = []
    if 0 < len(s) <= _C4_MAX_LEN:
        extra.append((4, _c4_encode(s)))
    extra.append((6, _c6_encode(s)))
    for codec, payload in extra:
        if payload is None:
            continue
        cand = bytearray([codec])
        gs.uvarint(len(payload), cand)
        cand += payload
        if len(cand) < len(best):
            best = bytes(cand)
    return best


def _unpack_stream2(buf: bytes, pos: int) -> tuple[bytes, int]:
    codec = buf[pos]
    if codec <= 3:
        return gs._unpack_stream(buf, pos)
    length, pos = gs.read_uvarint(buf, pos + 1)
    payload = buf[pos : pos + length]
    pos += length
    if codec == 4:
        return _c4_decode(payload), pos
    if codec == 6:
        return _c6_decode(payload), pos
    raise ValueError(f"unknown stream codec {codec}")


def _repack(mode: int, streams: list[bytes]) -> bytes:
    out = bytearray([mode])
    gs.uvarint(len(streams), out)
    for s in streams:
        out += _pack_stream2(s)
    return bytes(out)


def _unrepack(blob: bytes) -> list[bytes]:
    n, pos = gs.read_uvarint(blob, 1)
    streams = []
    for _ in range(n):
        s, pos = _unpack_stream2(blob, pos)
        streams.append(s)
    return streams


# --------------------------------------------------------------------------
# strategies

_ZC = gs._ZC
_ZD = gs._ZD


def _struct_candidates(data: bytes) -> list[bytes]:
    cands = []
    for transform, mode in ((gs._compress_logs, MODE_LOGS), (gs._compress_csv, MODE_CSV)):
        try:
            g1 = transform(data)
        except Exception:
            g1 = None
        if g1 is None:
            continue
        _, streams = gs._open_container(g1)   # recover the raw field streams
        cands.append(_repack(mode, streams))
    return cands


def _decompress(blob: bytes) -> bytes:
    mode = blob[0]
    if mode == MODE_RAWZ:
        return _ZD.decompress(blob[1:])
    if mode == MODE_LOGS:
        return gs._decompress_logs(_unrepack(blob))
    if mode == MODE_CSV:
        return gs._decompress_csv(_unrepack(blob))
    if mode == MODE_TEXT:
        return gt._decompress(blob[1:])
    if mode == MODE_MEDIA:
        return gm._decompress(blob[1:])
    raise ValueError(f"unknown mode {mode}")


def _finish(data: bytes, cands: list[bytes]) -> bytes:
    """Pick the smallest candidate; verify its roundtrip; fall back to zstd."""
    fallback = bytes([MODE_RAWZ]) + _ZC.compress(data)
    cands = cands + [fallback]
    for blob in sorted(cands, key=len):
        try:
            if _decompress(blob) == data:
                return blob
        except Exception:
            continue
    return fallback  # unreachable: the fallback always verifies


def _compress_struct(data: bytes) -> bytes:
    return _finish(data, _struct_candidates(data))


def _compress_auto(data: bytes) -> bytes:
    cands = _struct_candidates(data)
    try:
        cands.append(bytes([MODE_TEXT]) + gt._compress(data))
    except Exception:
        pass
    try:
        media = gm._pick_smallest(data, (gm._audio_compress, gm._image_compress))
        cands.append(bytes([MODE_MEDIA]) + media)
    except Exception:
        pass
    return _finish(data, cands)


register(FuncStrategy("gen2.x-struct", _compress_struct, _decompress))
register(FuncStrategy("gen2.x-auto", _compress_auto, _decompress))
