"""Deterministic test-corpus generation.

Every file is generated from a fixed seed so results are comparable across
generations and machines. Files model real content classes:

  text        natural-language prose (repetitive vocabulary, sentence structure)
  code        source code (high token repetition, indentation)
  logs        JSON-lines server logs (structured, timestamps, enums)
  csv         numeric time series (trend + noise, ASCII-encoded floats)
  audio       raw 16-bit PCM, mixed tones + noise (smooth waveform)
  image       raw 8-bit RGB bitmap, gradients + texture (2-D smoothness)
  random      cryptographically-random bytes (incompressible control)
"""

from __future__ import annotations

import math
import os
import random
import struct

CORPUS_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "corpus")

WORDS = (
    "the quick brown fox jumps over a lazy dog while compression research "
    "continues to explore entropy coding dictionary matching context modeling "
    "and transform based preprocessing for text audio image and structured data "
    "systems that store or transmit information benefit from smaller payloads "
    "because bandwidth latency and storage cost real money in production"
).split()

SERVICES = ["auth", "billing", "search", "ingest", "gateway", "worker"]
LEVELS = ["DEBUG", "INFO", "INFO", "INFO", "WARN", "ERROR"]

PY_SNIPPET = '''\
def process_batch(records, *, validate=True, max_retries=3):
    """Process a batch of records with optional validation."""
    results = []
    for record in records:
        if validate and not record.get("id"):
            raise ValueError(f"missing id in {record!r}")
        for attempt in range(max_retries):
            try:
                results.append(transform(record))
                break
            except TransientError:
                if attempt == max_retries - 1:
                    raise
    return results
'''


def gen_text(rng: random.Random, size: int) -> bytes:
    out = []
    n = 0
    while n < size:
        sent = " ".join(rng.choices(WORDS, k=rng.randint(6, 14))).capitalize() + ". "
        out.append(sent)
        n += len(sent)
    return "".join(out).encode()[:size]


def gen_code(rng: random.Random, size: int) -> bytes:
    out = []
    n = 0
    i = 0
    while n < size:
        chunk = PY_SNIPPET.replace("process_batch", f"process_batch_{i}").replace(
            "transform", rng.choice(["transform", "normalize", "enrich", "redact"])
        )
        out.append(chunk + "\n\n")
        n += len(chunk) + 2
        i += 1
    return "".join(out).encode()[:size]


def gen_logs(rng: random.Random, size: int) -> bytes:
    out = []
    n = 0
    ts = 1726200000.0
    while n < size:
        ts += rng.expovariate(50.0)
        line = (
            '{"ts": %.3f, "level": "%s", "service": "%s", "request_id": "%032x", '
            '"latency_ms": %.2f, "status": %d, "msg": "%s"}\n'
            % (
                ts,
                rng.choice(LEVELS),
                rng.choice(SERVICES),
                rng.getrandbits(128),
                rng.lognormvariate(2.5, 0.8),
                rng.choice([200, 200, 200, 201, 404, 500]),
                " ".join(rng.choices(WORDS, k=4)),
            )
        )
        out.append(line)
        n += len(line)
    return "".join(out).encode()[:size]


def gen_csv(rng: random.Random, size: int) -> bytes:
    out = ["timestamp,sensor_a,sensor_b,sensor_c\n"]
    n = len(out[0])
    t = 0
    a, b, c = 20.0, 101.3, 0.5
    while n < size:
        a += rng.gauss(0, 0.05)
        b += rng.gauss(0, 0.02)
        c = max(0.0, c + rng.gauss(0, 0.01))
        line = f"{1726200000 + t},{a:.3f},{b:.3f},{c:.4f}\n"
        out.append(line)
        n += len(line)
        t += 10
    return "".join(out).encode()[:size]


def gen_audio(rng: random.Random, size: int) -> bytes:
    """Raw signed 16-bit little-endian PCM, 44.1 kHz mono: tones + soft noise."""
    n_samples = size // 2
    samples = []
    phase1 = phase2 = 0.0
    f1, f2 = 220.0, 331.0
    for i in range(n_samples):
        if i % 4410 == 0:  # drift the "melody" every 100 ms
            f1 = max(80.0, min(1200.0, f1 * rng.choice([1.0, 1.0, 0.94, 1.06])))
            f2 = max(80.0, min(1800.0, f2 * rng.choice([1.0, 1.0, 0.94, 1.06])))
        phase1 += 2 * math.pi * f1 / 44100.0
        phase2 += 2 * math.pi * f2 / 44100.0
        v = 0.45 * math.sin(phase1) + 0.25 * math.sin(phase2) + rng.gauss(0, 0.008)
        samples.append(max(-32767, min(32767, int(v * 32767))))
    return struct.pack("<%dh" % n_samples, *samples)


def gen_image(rng: random.Random, size: int) -> bytes:
    """Raw 8-bit RGB rows: smooth gradients with texture noise (like a photo)."""
    width = 512
    row_bytes = width * 3
    rows = []
    n = 0
    y = 0
    while n < size:
        row = bytearray(row_bytes)
        for x in range(width):
            base_r = int(127 + 120 * math.sin(x / 97.0 + y / 61.0))
            base_g = int(127 + 120 * math.sin(x / 53.0 - y / 71.0))
            base_b = int(127 + 120 * math.cos(x / 83.0 + y / 43.0))
            row[3 * x] = max(0, min(255, base_r + rng.randint(-6, 6)))
            row[3 * x + 1] = max(0, min(255, base_g + rng.randint(-6, 6)))
            row[3 * x + 2] = max(0, min(255, base_b + rng.randint(-6, 6)))
        rows.append(bytes(row))
        n += row_bytes
        y += 1
    return b"".join(rows)[:size]


def gen_random(rng: random.Random, size: int) -> bytes:
    return rng.randbytes(size)


FILES = {
    "text.txt": (gen_text, 512 * 1024),
    "code.py": (gen_code, 384 * 1024),
    "logs.jsonl": (gen_logs, 512 * 1024),
    "series.csv": (gen_csv, 384 * 1024),
    "audio.pcm": (gen_audio, 512 * 1024),
    "image.rgb": (gen_image, 512 * 1024),
    "random.bin": (gen_random, 256 * 1024),
}


def build(force: bool = False) -> str:
    os.makedirs(CORPUS_DIR, exist_ok=True)
    for name, (fn, size) in FILES.items():
        path = os.path.join(CORPUS_DIR, name)
        if force or not os.path.exists(path):
            rng = random.Random(f"cipher-corpus:{name}")
            with open(path, "wb") as f:
                f.write(fn(rng, size))
    return CORPUS_DIR


if __name__ == "__main__":
    d = build(force=True)
    for name in sorted(FILES):
        print(f"{name:12s} {os.path.getsize(os.path.join(d, name)):>8d} bytes")
