"""Generation 1 — text/code specialist.

Strategies
----------
gen1.text-token
    Word-level tokenization front-end + best-of back-ends, in a
    self-describing container (first byte = mode):

      mode 0  raw store                      (safety valve)
      mode 1  bz2 -9 of the raw bytes        (fallback)
      mode 2  raw LZMA2 9e (pb=0, lc=0)      (fallback)
      mode 4  brotli quality 11              (fallback)
      mode 3  tokenized: the input is losslessly partitioned into maximal
              runs of letters ([A-Za-z]+ -> word dictionary), digits
              ([0-9]+ -> a NUM symbol + side stream), and everything else
              (separator dictionary).  The token-id stream is then coded by
              whichever of these is smallest for this input:
                sub 1  LZMA2 over the id bytes   (wins on repetitive code)
                sub 2  bz2 over the id bytes     (BWT groups repeated chunks)
                sub 3  adaptive order-1 arithmetic coder over ids
                       (wins on natural-language text: ~entropy of the
                        word distribution, conditioned on previous token)
              The digit side stream is routed into per-context sub-streams
              (context = id of the preceding token; the decoder re-derives
              the routing from the decoded id stream, so it costs no
              metadata).  Each sub-stream is stored either as LZMA'd
              literals or, when every number is canonical, as zigzag-delta
              varints (sequential counters and constants collapse to
              almost nothing).

    compress() evaluates every applicable mode and returns the smallest,
    then verifies its own roundtrip and falls back to mode 1/2 on any
    mismatch — so the strategy is lossless on arbitrary bytes (audio, RGB,
    random) by construction.

gen1.text-lzmax
    Raw LZMA2 preset 9|EXTREME with pb=0, lc=0 (headerless).  Cheap tuned
    baseline; identical to the container's mode 2, which quantifies the
    container overhead and gives a general-purpose floor.
"""

from __future__ import annotations

import bz2
import lzma

import brotli

from collections import Counter
from itertools import accumulate

from . import FuncStrategy, register

# --------------------------------------------------------------------------
# byte classes
_LETTER = bytes(1 if (65 <= b <= 90 or 97 <= b <= 122) else 0 for b in range(256))
_DIGIT = bytes(1 if 48 <= b <= 57 else 0 for b in range(256))

_LZ_FILTERS = [dict(id=lzma.FILTER_LZMA2, preset=9 | lzma.PRESET_EXTREME, pb=0, lc=0)]
_LZ_TXT_FILTERS = [dict(id=lzma.FILTER_LZMA2, preset=9 | lzma.PRESET_EXTREME, pb=0, lc=3)]


def _lz(data: bytes, filters=_LZ_FILTERS) -> bytes:
    return lzma.compress(data, format=lzma.FORMAT_RAW, filters=filters)


def _unlz(blob: bytes, filters=_LZ_FILTERS) -> bytes:
    return lzma.decompress(blob, format=lzma.FORMAT_RAW, filters=filters)


def _varint(x: int) -> bytes:
    out = bytearray()
    while True:
        b = x & 0x7F
        x >>= 7
        out.append(b | (0x80 if x else 0))
        if not x:
            return bytes(out)


def _read_varint(buf: bytes, pos: int) -> tuple[int, int]:
    x = 0
    shift = 0
    while True:
        b = buf[pos]
        pos += 1
        x |= (b & 0x7F) << shift
        if not b & 0x80:
            return x, pos
        shift += 7


# --------------------------------------------------------------------------
# tokenizer: exact partition of the input into word / number / separator runs

def _tokenize(data: bytes) -> list[tuple[str, bytes]]:
    toks = []
    i, n = 0, len(data)
    while i < n:
        b = data[i]
        if _LETTER[b]:
            j = i + 1
            while j < n and _LETTER[data[j]]:
                j += 1
            toks.append(("w", data[i:j]))
        elif _DIGIT[b]:
            j = i + 1
            while j < n and _DIGIT[data[j]]:
                j += 1
            toks.append(("n", data[i:j]))
        else:
            j = i + 1
            while j < n and not _LETTER[data[j]] and not _DIGIT[data[j]]:
                j += 1
            toks.append(("s", data[i:j]))
        i = j
    return toks


# --------------------------------------------------------------------------
# adaptive order-1 arithmetic coder over token ids
#
# 32-bit arithmetic coder (Witten/Neal/Cleary style with pending-bit
# renormalisation).  Model: per-context symbol counts, context = previous
# symbol id (A = start-of-stream context); counts += _INC after each symbol,
# halved when the context total exceeds _CAP.

_TOP = 0xFFFFFFFF
_HALF = 0x80000000
_QTR = 0x40000000
_INC = 32
_CAP = 1 << 16


class _AEnc:
    def __init__(self):
        self.low = 0
        self.high = _TOP
        self.pending = 0
        self.out = bytearray()
        self.acc = 0
        self.nacc = 0

    def _emit(self, bit: int) -> None:
        self.acc = (self.acc << 1) | bit
        self.nacc += 1
        if self.nacc == 8:
            self.out.append(self.acc)
            self.acc = 0
            self.nacc = 0

    def _bit_pending(self, bit: int) -> None:
        self._emit(bit)
        while self.pending:
            self._emit(bit ^ 1)
            self.pending -= 1

    def encode(self, cl: int, ch: int, tot: int) -> None:
        rng = self.high - self.low + 1
        self.high = self.low + rng * ch // tot - 1
        self.low = self.low + rng * cl // tot
        while True:
            if self.high < _HALF:
                self._bit_pending(0)
            elif self.low >= _HALF:
                self._bit_pending(1)
                self.low -= _HALF
                self.high -= _HALF
            elif self.low >= _QTR and self.high < 3 * _QTR:
                self.pending += 1
                self.low -= _QTR
                self.high -= _QTR
            else:
                break
            self.low <<= 1
            self.high = (self.high << 1) | 1

    def finish(self) -> bytes:
        self.pending += 1
        self._bit_pending(0 if self.low < _QTR else 1)
        while self.nacc:
            self._emit(0)
        return bytes(self.out)


class _ADec:
    def __init__(self, blob: bytes):
        self.blob = blob
        self.nbits = len(blob) * 8
        self.pos = 0
        self.low = 0
        self.high = _TOP
        self.code = 0
        for _ in range(32):
            self.code = (self.code << 1) | self._bit()

    def _bit(self) -> int:
        p = self.pos
        self.pos = p + 1
        if p >= self.nbits:
            return 0
        return (self.blob[p >> 3] >> (7 - (p & 7))) & 1

    def target(self, tot: int) -> int:
        rng = self.high - self.low + 1
        return ((self.code - self.low + 1) * tot - 1) // rng

    def consume(self, cl: int, ch: int, tot: int) -> None:
        rng = self.high - self.low + 1
        self.high = self.low + rng * ch // tot - 1
        self.low = self.low + rng * cl // tot
        while True:
            if self.high < _HALF:
                pass
            elif self.low >= _HALF:
                self.low -= _HALF
                self.high -= _HALF
                self.code -= _HALF
            elif self.low >= _QTR and self.high < 3 * _QTR:
                self.low -= _QTR
                self.high -= _QTR
                self.code -= _QTR
            else:
                break
            self.low <<= 1
            self.high = (self.high << 1) | 1
            self.code = (self.code << 1) | self._bit()


def _rc_encode(ids: list[int], alpha: int) -> bytes:
    enc = _AEnc()
    ctx: dict[int, list] = {}
    prev = alpha
    for s in ids:
        c = ctx.get(prev)
        if c is None:
            c = ctx[prev] = [1] * alpha + [alpha]  # counts + running total
        cl = sum(c[:s])
        tot = c[alpha]
        enc.encode(cl, cl + c[s], tot)
        c[s] += _INC
        c[alpha] += _INC
        if c[alpha] > _CAP:
            tot = 0
            for i in range(alpha):
                c[i] = (c[i] + 1) >> 1
                tot += c[i]
            c[alpha] = tot
        prev = s
    return enc.finish()


def _rc_decode(blob: bytes, alpha: int, count: int) -> list[int]:
    dec = _ADec(blob)
    ctx: dict[int, list] = {}
    ids = []
    prev = alpha
    from bisect import bisect_right

    for _ in range(count):
        c = ctx.get(prev)
        if c is None:
            c = ctx[prev] = [1] * alpha + [alpha]
        tot = c[alpha]
        tgt = dec.target(tot)
        cums = list(accumulate(c[i] for i in range(alpha)))
        s = bisect_right(cums, tgt)
        cl = cums[s] - c[s]
        dec.consume(cl, cums[s], tot)
        ids.append(s)
        c[s] += _INC
        c[alpha] += _INC
        if c[alpha] > _CAP:
            tot = 0
            for i in range(alpha):
                c[i] = (c[i] + 1) >> 1
                tot += c[i]
            c[alpha] = tot
        prev = s
    return ids


# --------------------------------------------------------------------------
# number side stream

def _encode_numbers(nums: list[bytes]) -> bytes:
    """flag(0=literal lzma, 1=zigzag-delta varint lzma) + varint len + blob."""
    lit = _lz(b",".join(nums), _LZ_TXT_FILTERS)
    best = b"\x00" + _varint(len(lit)) + lit
    if nums and all(v == b"0" or (v[0:1] != b"0") for v in nums):
        prev = 0
        ba = bytearray()
        for v in nums:
            x = int(v)
            d = x - prev
            ba += _varint((d << 1) ^ (d >> 63) if -(1 << 62) < d < (1 << 62)
                          else ((-d << 1) - 1 if d < 0 else d << 1))
            prev = x
        delta = _lz(bytes(ba))
        cand = b"\x01" + _varint(len(delta)) + delta
        if len(cand) < len(best):
            best = cand
    return bytes(best)


def _decode_numbers(buf: bytes, pos: int, count: int) -> tuple[list[bytes], int]:
    flag = buf[pos]
    pos += 1
    ln, pos = _read_varint(buf, pos)
    blob = buf[pos:pos + ln]
    pos += ln
    if count == 0:
        return [], pos
    if flag == 0:
        return _unlz(blob, _LZ_TXT_FILTERS).split(b","), pos
    raw = _unlz(blob)
    nums = []
    prev = 0
    p = 0
    for _ in range(count):
        z, p = _read_varint(raw, p)
        d = (z >> 1) ^ -(z & 1)
        prev += d
        nums.append(str(prev).encode())
    return nums, pos


# numbers are split into per-context streams; a context (= id of the token
# preceding the NUM token, or `alpha` at stream start) gets its own stream
# when it accounts for at least _MIN_CTX_NUMS numbers, everything else goes
# to a shared misc stream.  The routing is a pure function of the id stream,
# so the decoder reconstructs it without any stored metadata.
_MIN_CTX_NUMS = 16


def _num_contexts(ids: list[int], alpha: int, num_sym: int) -> list[int]:
    ctxs = []
    prev = alpha
    for s in ids:
        if s == num_sym:
            ctxs.append(prev)
        prev = s
    return ctxs


def _split_contexts(ctxs: list[int]) -> list[int]:
    counts = Counter(ctxs)
    return sorted(c for c, n in counts.items() if n >= _MIN_CTX_NUMS)


def _encode_numsec(ids: list[int], nums: list[bytes], alpha: int, num_sym: int) -> bytes:
    ctxs = _num_contexts(ids, alpha, num_sym)
    qualified = _split_contexts(ctxs)
    qset = set(qualified)
    streams = {c: [] for c in qualified}
    misc = []
    for c, v in zip(ctxs, nums):
        (streams[c] if c in qset else misc).append(v)
    out = bytearray()
    out += _varint(len(qualified))
    out += _encode_numbers(misc)
    for c in qualified:
        out += _encode_numbers(streams[c])
    return bytes(out)


def _decode_numsec(buf: bytes, pos: int, ids: list[int], alpha: int,
                   num_sym: int) -> tuple[list[bytes], int]:
    ctxs = _num_contexts(ids, alpha, num_sym)
    qualified = _split_contexts(ctxs)
    qset = set(qualified)
    counts = Counter(ctxs)
    nq, pos = _read_varint(buf, pos)
    if nq != len(qualified):
        raise ValueError("number-stream routing mismatch")
    misc_count = sum(n for c, n in counts.items() if c not in qset)
    misc, pos = _decode_numbers(buf, pos, misc_count)
    streams = {}
    for c in qualified:
        streams[c], pos = _decode_numbers(buf, pos, counts[c])
    nums = []
    idx = {c: 0 for c in qualified}
    mi = 0
    for c in ctxs:
        if c in qset:
            nums.append(streams[c][idx[c]])
            idx[c] += 1
        else:
            nums.append(misc[mi])
            mi += 1
    return nums, pos


# --------------------------------------------------------------------------
# tokenized mode (mode 3)

_MAX_DICT = 30000       # skip tokenized modes above this many distinct tokens
_MAX_RC_ALPHA = 400     # order-1 coder only for small alphabets (speed)
_MIN_ALNUM_FRAC = 0.30  # quick reject for binary-ish inputs


def _build_tokenized(data: bytes) -> bytes | None:
    alnum = sum(map(_LETTER.__getitem__, data)) + sum(map(_DIGIT.__getitem__, data))
    if alnum < _MIN_ALNUM_FRAC * len(data):
        return None
    toks = _tokenize(data)
    words = Counter(v for k, v in toks if k == "w")
    seps = Counter(v for k, v in toks if k == "s")
    if len(words) + len(seps) > _MAX_DICT:
        return None
    wlist = [w for w, _ in words.most_common()]
    slist = [s for s, _ in seps.most_common()]
    wid = {w: i for i, w in enumerate(wlist)}
    sid = {s: i for i, s in enumerate(slist)}
    nw, ns = len(wlist), len(slist)
    num_sym = nw + ns
    alpha = num_sym + 1

    ids = []
    nums = []
    for k, v in toks:
        if k == "w":
            ids.append(wid[v])
        elif k == "s":
            ids.append(nw + sid[v])
        else:
            ids.append(num_sym)
            nums.append(v)

    # dictionary blob: varint counts, then length-prefixed entries; lzma'd
    dblob = bytearray()
    for lst in (wlist, slist):
        dblob += _varint(len(lst))
        for e in lst:
            dblob += _varint(len(e)) + e
    dcomp = _lz(bytes(dblob), _LZ_TXT_FILTERS)

    numsec = _encode_numsec(ids, nums, alpha, num_sym)

    if alpha <= 256:
        idbytes = bytes(ids)
    else:
        ba = bytearray()
        for x in ids:
            ba += x.to_bytes(2, "little")
        idbytes = bytes(ba)

    cands = [(1, _lz(idbytes)), (2, bz2.compress(idbytes, 9))]
    if alpha <= _MAX_RC_ALPHA:
        cands.append((3, _rc_encode(ids, alpha)))
    sub, payload = min(cands, key=lambda t: len(t[1]))

    out = bytearray(b"\x03")
    out += _varint(len(toks))
    out += _varint(len(dcomp))
    out += dcomp
    out.append(sub)
    out += _varint(len(payload))
    out += payload
    out += numsec
    return bytes(out)


def _decode_tokenized(blob: bytes) -> bytes:
    pos = 1
    ntoks, pos = _read_varint(blob, pos)
    dlen, pos = _read_varint(blob, pos)
    draw = _unlz(blob[pos:pos + dlen], _LZ_TXT_FILTERS)
    pos += dlen
    p = 0
    lists = []
    for _ in range(2):
        cnt, p = _read_varint(draw, p)
        lst = []
        for _ in range(cnt):
            ln, p = _read_varint(draw, p)
            lst.append(draw[p:p + ln])
            p += ln
        lists.append(lst)
    wlist, slist = lists
    nw, ns = len(wlist), len(slist)
    num_sym = nw + ns
    alpha = num_sym + 1

    sub = blob[pos]
    pos += 1
    plen, pos = _read_varint(blob, pos)
    payload = blob[pos:pos + plen]
    pos += plen
    if sub == 3:
        ids = _rc_decode(payload, alpha, ntoks)
    else:
        idbytes = _unlz(payload) if sub == 1 else bz2.decompress(payload)
        if alpha <= 256:
            ids = list(idbytes)
        else:
            ids = [int.from_bytes(idbytes[i:i + 2], "little")
                   for i in range(0, len(idbytes), 2)]

    nums, pos = _decode_numsec(blob, pos, ids, alpha, num_sym)

    out = []
    ni = 0
    for s in ids:
        if s < nw:
            out.append(wlist[s])
        elif s < num_sym:
            out.append(slist[s - nw])
        else:
            out.append(nums[ni])
            ni += 1
    return b"".join(out)


# --------------------------------------------------------------------------
# container strategy

def _compress(data: bytes) -> bytes:
    cands = [b"\x00" + data,
             b"\x01" + bz2.compress(data, 9),
             b"\x02" + _lz(data),
             b"\x04" + brotli.compress(data, quality=11)]
    try:
        tok = _build_tokenized(data)
        if tok is not None:
            cands.append(tok)
    except Exception:
        pass
    best = min(cands, key=len)
    if best[0] == 3:  # verify the fancy path; fall back if it misbehaves
        try:
            if _decode_tokenized(best) != data:
                raise ValueError("tokenized roundtrip mismatch")
        except Exception:
            cands = [c for c in cands if c[0] != 3]
            best = min(cands, key=len)
    return best


def _decompress(blob: bytes) -> bytes:
    mode = blob[0]
    if mode == 0:
        return blob[1:]
    if mode == 1:
        return bz2.decompress(blob[1:])
    if mode == 2:
        return _unlz(blob[1:])
    if mode == 3:
        return _decode_tokenized(blob)
    if mode == 4:
        return brotli.decompress(blob[1:])
    raise ValueError(f"bad mode byte {mode}")


register(FuncStrategy("gen1.text-token", _compress, _decompress))
register(FuncStrategy("gen1.text-lzmax", _lz, _unlz))
