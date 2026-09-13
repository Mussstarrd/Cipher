"""Generation 1 — STRUCTURED-DATA specialist.

Content-aware columnar transforms for the two structured corpus files:

  gen1.struct-logs   logs.jsonl  — per-field streams: timestamps as integer
                     millisecond deltas, level/service/status as enum indices,
                     request_id hex packed to raw 16-byte binary (kept OUT of
                     the LZ window of the text streams), latency as integer
                     centi-ms deltas-free varints, msg text as its own stream.
  gen1.struct-csv    series.csv  — generic numeric CSV: per-column fixed-point
                     integers, zigzag-delta varint encoded, one stream per
                     column.

Correctness strategy (rule 1 of ORCHESTRATOR.md):
  * every parsed line is RE-RENDERED and byte-compared during compression;
    any line that does not round-trip exactly goes verbatim into an
    "exceptions" stream (this covers the truncated final line of both files,
    odd float formatting, etc.);
  * if fewer than 90% of lines conform, the whole input is stored under a
    plain-zstd fallback mode — the container is self-describing via a mode
    byte, so the strategies are lossless on every corpus file;
  * each stream is compressed independently with the best of
    {raw, zstd-19, lzma-6, brotli-11}, tagged per stream.
"""

from __future__ import annotations

import lzma
import re

import brotli
import zstandard

from . import register

MAGIC = b"CS1"
MODE_RAW = 0
MODE_LOGS = 1
MODE_CSV = 2

# --------------------------------------------------------------------------
# varint / zigzag helpers
# --------------------------------------------------------------------------


def uvarint(n: int, out: bytearray) -> None:
    while True:
        b = n & 0x7F
        n >>= 7
        if n:
            out.append(b | 0x80)
        else:
            out.append(b)
            return


def read_uvarint(buf: bytes, pos: int) -> tuple[int, int]:
    n = 0
    shift = 0
    while True:
        b = buf[pos]
        pos += 1
        n |= (b & 0x7F) << shift
        if not b & 0x80:
            return n, pos
        shift += 7


def zigzag(n: int) -> int:
    return (n << 1) ^ (n >> 63) if n < 0 else n << 1


def unzigzag(n: int) -> int:
    return (n >> 1) ^ -(n & 1)


# --------------------------------------------------------------------------
# per-stream adaptive codec
# --------------------------------------------------------------------------

_ZC = zstandard.ZstdCompressor(level=19)
_ZD = zstandard.ZstdDecompressor()


def _pack_stream(data: bytes) -> bytes:
    """codec byte + uvarint length + payload; picks the smallest codec."""
    candidates = [(0, data)]
    try:
        candidates.append((1, _ZC.compress(data)))
    except Exception:
        pass
    try:
        candidates.append((2, lzma.compress(data, preset=6)))
    except Exception:
        pass
    try:
        candidates.append((3, brotli.compress(data, quality=11)))
    except Exception:
        pass
    codec, payload = min(candidates, key=lambda c: len(c[1]))
    out = bytearray([codec])
    uvarint(len(payload), out)
    out += payload
    return bytes(out)


def _unpack_stream(buf: bytes, pos: int) -> tuple[bytes, int]:
    codec = buf[pos]
    length, pos = read_uvarint(buf, pos + 1)
    payload = buf[pos : pos + length]
    pos += length
    if codec == 0:
        return payload, pos
    if codec == 1:
        return _ZD.decompress(payload), pos
    if codec == 2:
        return lzma.decompress(payload), pos
    if codec == 3:
        return brotli.decompress(payload), pos
    raise ValueError(f"unknown stream codec {codec}")


def _container(mode: int, streams: list[bytes]) -> bytes:
    out = bytearray(MAGIC)
    out.append(mode)
    uvarint(len(streams), out)
    for s in streams:
        out += _pack_stream(s)
    return bytes(out)


def _open_container(blob: bytes) -> tuple[int, list[bytes]]:
    if blob[:3] != MAGIC:
        raise ValueError("bad magic")
    mode = blob[3]
    n, pos = read_uvarint(blob, 4)
    streams = []
    for _ in range(n):
        s, pos = _unpack_stream(blob, pos)
        streams.append(s)
    return mode, streams


def _split_lines(data: bytes) -> tuple[list[bytes], bool]:
    """Split on \\n; returns (lines, ends_with_newline)."""
    parts = data.split(b"\n")
    if parts and parts[-1] == b"":
        return parts[:-1], True
    return parts, False


def _join_lines(lines: list[bytes], ends_nl: bool) -> bytes:
    out = b"\n".join(lines)
    return out + b"\n" if ends_nl else out


def _encode_exceptions(exceptions: list[tuple[int, bytes]]) -> bytes:
    out = bytearray()
    uvarint(len(exceptions), out)
    prev = 0
    for idx, raw in exceptions:
        uvarint(idx - prev, out)  # indices are increasing
        prev = idx
        uvarint(len(raw), out)
        out += raw
    return bytes(out)


def _decode_exceptions(buf: bytes) -> list[tuple[int, bytes]]:
    n, pos = read_uvarint(buf, 0)
    out = []
    prev = 0
    for _ in range(n):
        d, pos = read_uvarint(buf, pos)
        idx = prev + d
        prev = idx
        ln, pos = read_uvarint(buf, pos)
        out.append((idx, buf[pos : pos + ln]))
        pos += ln
    return out


# --------------------------------------------------------------------------
# logs.jsonl transform
# --------------------------------------------------------------------------

_LOG_RE = re.compile(
    rb'^\{"ts": (\d+\.\d{3}), "level": "([A-Za-z]+)", "service": "([a-z]+)", '
    rb'"request_id": "([0-9a-f]{32})", "latency_ms": (\d+\.\d{2}), '
    rb'"status": (\d+), "msg": "([^"\\]*)"\}$'
)


def _render_log(ts_ms: int, level: bytes, service: bytes, rid: bytes, lat_c: int, status: bytes, msg: bytes) -> bytes:
    return (
        b'{"ts": %d.%03d, "level": "%s", "service": "%s", "request_id": "%s", '
        b'"latency_ms": %d.%02d, "status": %s, "msg": "%s"}'
        % (ts_ms // 1000, ts_ms % 1000, level, service, rid.hex().encode(), lat_c // 100, lat_c % 100, status, msg)
    )


class _Enum:
    """Order-of-first-appearance token dictionary + per-row index bytes."""

    def __init__(self):
        self.tokens: list[bytes] = []
        self.index: dict[bytes, int] = {}
        self.rows = bytearray()

    def add(self, tok: bytes) -> bool:
        i = self.index.get(tok)
        if i is None:
            if len(self.tokens) >= 255:
                return False
            i = len(self.tokens)
            self.index[tok] = i
            self.tokens.append(tok)
        self.rows.append(i)
        return True

    def dict_stream(self) -> bytes:
        return b"\n".join(self.tokens)


def _compress_logs(data: bytes) -> bytes | None:
    lines, ends_nl = _split_lines(data)
    if not lines:
        return None

    ts_deltas = bytearray()
    prev_ts = 0
    levels, services, statuses = _Enum(), _Enum(), _Enum()
    rids = bytearray()
    lats = bytearray()
    msgs: list[bytes] = []
    row_of_line = bytearray()  # per line: 1 = structured row, 0 = exception
    exceptions: list[tuple[int, bytes]] = []

    for i, line in enumerate(lines):
        m = _LOG_RE.match(line)
        ok = False
        if m:
            ts_txt, level, service, rid_hex, lat_txt, status, msg = m.groups()
            ts_ms = int(ts_txt.replace(b".", b""))
            lat_c = int(lat_txt.replace(b".", b""))
            rid = bytes.fromhex(rid_hex.decode())
            if (
                _render_log(ts_ms, level, service, rid, lat_c, status, msg) == line
                and levels.add(level)
                and services.add(service)
                and statuses.add(status)
            ):
                uvarint(zigzag(ts_ms - prev_ts), ts_deltas)
                prev_ts = ts_ms
                rids += rid
                uvarint(lat_c, lats)
                msgs.append(msg)
                ok = True
        row_of_line.append(1 if ok else 0)
        if not ok:
            exceptions.append((i, line))

    if len(exceptions) > 0.1 * len(lines):
        return None

    # msg word tokenization: 4 words from a tiny vocabulary compress far
    # better as dictionary indices than as running text.
    msg_flag = 1
    vocab: list[bytes] = []
    vocab_ix: dict[bytes, int] = {}
    msg_counts = bytearray()
    msg_indices = bytearray()
    for msg in msgs:
        toks = msg.split(b" ")
        if len(toks) > 127:
            msg_flag = 0
            break
        for t in toks:
            i = vocab_ix.get(t)
            if i is None:
                if len(vocab) >= 255:
                    msg_flag = 0
                    break
                i = len(vocab)
                vocab_ix[t] = i
                vocab.append(t)
            msg_indices.append(i)
        else:
            msg_counts.append(len(toks))
            continue
        break
    if msg_flag:
        msg_streams = [b"\n".join(vocab), bytes(msg_counts), bytes(msg_indices)]
    else:
        msg_streams = [b"\n".join(msgs), b"", b""]

    header = bytearray()
    uvarint(len(lines), header)
    header.append(1 if ends_nl else 0)
    header.append(msg_flag)

    streams = [
        bytes(header),
        bytes(row_of_line),
        bytes(ts_deltas),
        levels.dict_stream(),
        bytes(levels.rows),
        services.dict_stream(),
        bytes(services.rows),
        bytes(rids),
        bytes(lats),
        statuses.dict_stream(),
        bytes(statuses.rows),
        *msg_streams,
        _encode_exceptions(exceptions),
    ]
    return _container(MODE_LOGS, streams)


def _decompress_logs(streams: list[bytes]) -> bytes:
    (header, row_of_line, ts_deltas, lvl_dict, lvl_rows, svc_dict, svc_rows,
     rids, lats, st_dict, st_rows, msg_a, msg_counts, msg_indices, exc_blob) = streams

    n_lines, pos = read_uvarint(header, 0)
    ends_nl = header[pos] == 1
    msg_flag = header[pos + 1]
    levels = lvl_dict.split(b"\n")
    services = svc_dict.split(b"\n")
    statuses = st_dict.split(b"\n")
    if msg_flag:
        vocab = msg_a.split(b"\n")
        msgs = []
        mp = 0
        for cnt in msg_counts:
            msgs.append(b" ".join(vocab[i] for i in msg_indices[mp : mp + cnt]))
            mp += cnt
    else:
        msgs = msg_a.split(b"\n") if msg_a else []
    exceptions = dict(_decode_exceptions(exc_blob))

    lines: list[bytes] = []
    ts_pos = lat_pos = 0
    prev_ts = 0
    row = 0
    for i in range(n_lines):
        if not row_of_line[i]:
            lines.append(exceptions[i])
            continue
        d, ts_pos = read_uvarint(ts_deltas, ts_pos)
        prev_ts += unzigzag(d)
        lat_c, lat_pos = read_uvarint(lats, lat_pos)
        rid = rids[16 * row : 16 * row + 16]
        lines.append(
            _render_log(
                prev_ts,
                levels[lvl_rows[row]],
                services[svc_rows[row]],
                rid,
                lat_c,
                statuses[st_rows[row]],
                msgs[row],
            )
        )
        row += 1
    return _join_lines(lines, ends_nl)


# --------------------------------------------------------------------------
# series.csv transform (generic fixed-point numeric CSV)
# --------------------------------------------------------------------------

_NUM_RE = re.compile(rb"^-?\d+(?:\.\d+)?$")


def _parse_fixed(field: bytes) -> tuple[int, int] | None:
    """'19.984' -> (19984, 3); '1726200000' -> (1726200000, 0)."""
    if not _NUM_RE.match(field):
        return None
    if b"." in field:
        intpart, frac = field.split(b".")
        return int(intpart + frac) if intpart != b"-" else -int(frac), len(frac)
    return int(field), 0


def _render_fixed(v: int, dec: int) -> bytes:
    if dec == 0:
        return b"%d" % v
    sign = b"-" if v < 0 else b""
    a = abs(v)
    return sign + b"%d.%0*d" % (a // 10**dec, dec, a % 10**dec)


def _compress_csv(data: bytes) -> bytes | None:
    lines, ends_nl = _split_lines(data)
    if len(lines) < 3:
        return None

    header_line = lines[0]
    n_cols = header_line.count(b",") + 1
    if not 2 <= n_cols <= 64:
        return None

    # detect per-column decimal places from the first conforming data row
    decimals: list[int] | None = None
    for line in lines[1:20]:
        fields = line.split(b",")
        if len(fields) != n_cols:
            continue
        parsed = [_parse_fixed(f) for f in fields]
        if all(p is not None for p in parsed):
            decimals = [p[1] for p in parsed]  # type: ignore[index]
            break
    if decimals is None:
        return None

    col_deltas = [bytearray() for _ in range(n_cols)]
    prev = [0] * n_cols
    row_of_line = bytearray([0])  # header is an exception by construction
    exceptions: list[tuple[int, bytes]] = [(0, header_line)]

    for i, line in enumerate(lines[1:], start=1):
        fields = line.split(b",")
        ok = False
        if len(fields) == n_cols:
            vals = []
            for f, dec in zip(fields, decimals):
                p = _parse_fixed(f)
                if p is None or p[1] != dec or _render_fixed(p[0], dec) != f:
                    break
                vals.append(p[0])
            else:
                for c, v in enumerate(vals):
                    uvarint(zigzag(v - prev[c]), col_deltas[c])
                    prev[c] = v
                ok = True
        row_of_line.append(1 if ok else 0)
        if not ok:
            exceptions.append((i, line))

    if len(exceptions) > 0.1 * len(lines):
        return None

    header = bytearray()
    uvarint(len(lines), header)
    header.append(1 if ends_nl else 0)
    header.append(n_cols)
    for d in decimals:
        header.append(d)

    streams = [bytes(header), bytes(row_of_line)]
    streams += [bytes(cd) for cd in col_deltas]
    streams.append(_encode_exceptions(exceptions))
    return _container(MODE_CSV, streams)


def _decompress_csv(streams: list[bytes]) -> bytes:
    header = streams[0]
    n_lines, pos = read_uvarint(header, 0)
    ends_nl = header[pos] == 1
    n_cols = header[pos + 1]
    decimals = list(header[pos + 2 : pos + 2 + n_cols])
    row_of_line = streams[1]
    col_deltas = streams[2 : 2 + n_cols]
    exceptions = dict(_decode_exceptions(streams[2 + n_cols]))

    col_pos = [0] * n_cols
    prev = [0] * n_cols
    lines: list[bytes] = []
    for i in range(n_lines):
        if not row_of_line[i]:
            lines.append(exceptions[i])
            continue
        fields = []
        for c in range(n_cols):
            d, col_pos[c] = read_uvarint(col_deltas[c], col_pos[c])
            prev[c] += unzigzag(d)
            fields.append(_render_fixed(prev[c], decimals[c]))
        lines.append(b",".join(fields))
    return _join_lines(lines, ends_nl)


# --------------------------------------------------------------------------
# strategies
# --------------------------------------------------------------------------


class _Structured:
    def __init__(self, name: str, transform):
        self.name = name
        self._transform = transform

    def compress(self, data: bytes) -> bytes:
        try:
            blob = self._transform(data)
        except Exception:
            blob = None
        if blob is not None:
            return blob
        return MAGIC + bytes([MODE_RAW]) + _ZC.compress(data)

    def decompress(self, blob: bytes) -> bytes:
        if blob[:3] != MAGIC:
            raise ValueError("bad magic")
        if blob[3] == MODE_RAW:
            return _ZD.decompress(blob[4:])
        mode, streams = _open_container(blob)
        if mode == MODE_LOGS:
            return _decompress_logs(streams)
        if mode == MODE_CSV:
            return _decompress_csv(streams)
        raise ValueError(f"unknown mode {mode}")


register(_Structured("gen1.struct-logs", _compress_logs))
register(_Structured("gen1.struct-csv", _compress_csv))
