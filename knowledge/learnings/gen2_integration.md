# Gen 2 — INTEGRATION: gen2.auto router + `lab/cipher.py` CLI

Agent: integration. Benchmarks: `knowledge/results/gen2-auto.jsonl` (tag
`gen2-auto`). New code: `lab/strategies/gen2_auto.py`, `lab/cipher.py`.

## What was built

### 1. `gen2.auto` — best-of-everything router (mode-byte container)

Runs the compressors of the four gen1 winners plus a zstd-19 fallback, keeps
the smallest blob, and prefixes one mode byte:

```
0 zstd-19 raw | 1 gen1.media-auto | 2 gen1.struct-logs | 3 gen1.struct-csv | 4 gen1.text-token
```

The gen1 strategies are called through the shared registry (their modules are
untouched). Before committing to a fancy-path winner, compress() verifies its
roundtrip and drops the candidate on any mismatch/exception, so the strategy
degrades to plain zstd-19 rather than ever being lossy. Harness: lossless on
all 7 corpus files.

### 2. `lab/cipher.py` — end-user CLI

```
python3 -m lab.cipher pack   <in> <out> [--strategy gen2.auto] [--encrypt]
python3 -m lab.cipher unpack <in> <out>
```

File format: `CPH1` magic, version byte, flags byte (bit 0 = encrypted),
strategy-name length + name, then (if encrypted) a 16-byte scrypt salt, then
the payload. With `--encrypt`, passphrase comes from `$CIPHER_KEY`, key =
Scrypt(salt, length=32, n=2^14, r=8, p=1) via the `cryptography` library, and
the payload is `crypto.seal(compressed, key)` — encrypt AFTER compress, using
only lab.crypto's AESGCM. unpack auto-detects everything from the header.

## Measured: gen2.auto vs the per-file leaderboard bests

All numbers from tag `gen2-auto` (single run, same corpus SHAs as gen1):

| file | gen1 per-file best (strategy) | gen2.auto | comp MB/s | decomp MB/s |
|---|---|---|---|---|
| audio.pcm | 1.533x (gen1.media-auto) | **1.533x** | 0.06 | 1.25 |
| code.py | 408.324x (gen1.text-token) | **407.900x** (963→964 B, +1 mode byte) | 0.08 | 43.2 |
| image.rgb | 1.851x (gen1.media-auto/pngf) | **1.851x** | 0.07 | 3.6 |
| logs.jsonl | 8.342x (gen1.struct-logs) | **8.342x** | 0.06 | 68.6 |
| random.bin | 1.000x | **1.000x** (262 146 B, +2 B total) | 0.32 | 3368 |
| series.csv | 11.336x (gen1.struct-csv) | **11.336x** | 0.08 | 8.95 |
| text.txt | 9.047x (gen1.text-token) | **9.046x** | 0.06 | 0.35 |

* As designed, gen2.auto matches every per-file best to within the 1-byte
  mode tag (visible only on the tiny code.py blob and random.bin).
* Whole-corpus: 3 145 728 → 1 043 789 bytes = **3.014x total (66.8% saved)** —
  the new overall #1 (gen1 best overall was gen1.media-auto at 2.740x),
  simply because no single gen1 strategy wins everywhere and the router
  always picks the local winner.

## Measured: the practical cost of the auto router

* **Compression is the sum of all candidates** (media LPC+PNG filters,
  both structured parsers, the tokenizer + its three backends, zstd-19)
  **plus one verification decompress of the winner**: 0.06–0.32 MB/s here,
  i.e. ~5–10 s per 0.5 MB corpus file. This is the price of
  "zero-configuration best-of-everything"; `--strategy zstd-19` (or any
  single strategy) via the CLI is the fast path when speed matters.
* **Decompression pays only the winner's cost** (one mode dispatch): fast
  where the winner is fast (logs 68.6 MB/s, code 43 MB/s, random 3.4 GB/s),
  slow only where the winning gen1 decoder is inherently slow (text.txt
  0.35 MB/s — text-token's order-1 arithmetic coder in pure Python;
  audio.pcm 1.25 MB/s — Python-loop LPC synthesis). A future generation
  could vectorize those decoders; the router adds nothing measurable.
* random.bin: router compresses at 0.32 MB/s (most candidates bail early on
  binary junk) and stores zstd's ~raw framing, +2 bytes total. Acceptable.

## CLI verification (all on this machine, outputs in the session scratchpad)

* **Plain roundtrips** — pack+unpack+`cmp` byte-exact on logs.jsonl
  (524 288→62 864 B, 8.340x), series.csv (393 216→34 705 B, 11.330x),
  text.txt (524 288→57 971 B, 9.044x). Header overhead is 16 bytes for
  strategy name `gen2.auto`.
* **Encrypted roundtrips** (`--encrypt`, `CIPHER_KEY` set) — byte-exact on
  the same three files; fixed +44 B overhead vs plain (16 B salt + 12 B GCM
  nonce + 16 B GCM tag), e.g. logs 62 908 B (8.334x).
* **Encrypted output is incompressible**: zstd-19 over each `.enc.cph`
  *expands* it (1.0002–1.0003x of input, +10 B framing) — consistent with
  ciphertext ≈ random bytes; the compress-then-encrypt ordering is doing its
  job.
* **Negative paths**: wrong `CIPHER_KEY` → clean "decryption failed" error,
  exit 1 (GCM tag rejects; no partial output). Missing `CIPHER_KEY` with
  `--encrypt` → clear error. Unknown `--strategy` lists the 16 registered
  names. unpack of a non-Cipher file → "bad magic". Packing the same file
  twice with the same passphrase yields different ciphertexts (fresh salt +
  fresh nonce per pack — no nonce reuse).
* **Alternate strategy path**: `--strategy zstd-19` pack/unpack roundtrips
  byte-exact (strategy name stored in header, auto-selected on unpack).

## Ops note (environment, not compression)

The container's `cryptography` 41.0.7 in `/usr/lib/python3/dist-packages`
ships `_cffi_backend` built for CPython 3.12 while the lab runs 3.11, so any
`cryptography` import panicked (`ModuleNotFoundError: _cffi_backend` inside
pyo3). Fix: `pip3 install cffi` (installs a 3.11 wheel that shadows the
broken one). If gen3 agents see the same panic, that's the cure — nothing is
wrong with `lab/crypto.py`.

## Guidance for gen3

* The router pattern is O(sum of candidates) at compress time; a cheap
  content sniffer (magic/statistics → try only 1–2 candidates) would recover
  most of the speed at near-zero ratio cost, but keep the try-all router as
  the correctness/ratio reference.
* The two slow decoders (text-token arithmetic coder, media LPC synthesis)
  are now the product's worst UX numbers; vectorizing them helps the
  shipped CLI directly.
* Any new gen3 strategy becomes CLI-usable automatically (registry name goes
  in the header), and can be added to gen2.auto's `_MODES` table by a future
  integration agent with a new mode byte — old files stay decodable as long
  as existing mode numbers are never reassigned.
