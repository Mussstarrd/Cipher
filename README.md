# Cipher — a multi-agent, self-improving compression lab

An experiment in whether a system of cooperating AI agents can iteratively
discover more efficient ways to compress (and then encrypt) files — text,
structured data, audio, images — by running a closed loop of
**hypothesize → implement → benchmark → record → refine**.

## What this is (and isn't)

Lossless compression has a hard floor: the entropy of the source. General
codecs (zstd, brotli, LZMA) sit close to practical limits for generic byte
streams, so no loop "recursively self-improves" past physics. What a loop
*can* do is search the space of **content-aware pipelines** (preprocessing
transform × codec × parameters) much faster than a human, keep every
measurement and negative result in a shared knowledge base, and converge on
per-content-type winners — the same playbook behind PNG's filters, FLAC's
prediction, and Parquet's columnar layout. This repo demonstrates that loop
actually running, with real measured results.

Encryption is deliberately boring: compress first, then AES-256-GCM
(`lab/crypto.py`). Ciphertext is incompressible so the order is forced, and
inventing ciphers is a non-goal.

## Layout

```
agents/ORCHESTRATOR.md      the loop protocol and the rules agents follow
lab/corpus.py               deterministic 7-file test corpus (text, code,
                            JSONL logs, CSV series, PCM audio, RGB image,
                            random control)
lab/strategies/             strategy plugins; baselines.py is generation 0,
                            gen1_*.py etc. are agent-contributed
lab/harness.py              roundtrip-verified benchmark runner
lab/knowledge.py            aggregates results, renders the leaderboard
lab/crypto.py               the encrypt-after-compress stage
knowledge/results/*.jsonl   every measurement ever taken
knowledge/learnings/*.md    per-agent findings, including negative results
knowledge/LEADERBOARD.md    generated summary
```

## Quick start

```bash
pip install -r requirements.txt
python -m lab.corpus                 # build the corpus
python -m lab.harness --tag mytest   # benchmark every registered strategy
python -m lab.knowledge              # render knowledge/LEADERBOARD.md
```

Add a strategy: create `lab/strategies/my_idea.py`, call `register(...)` with
an object exposing `name`, `compress(bytes) -> bytes`,
`decompress(bytes) -> bytes`. The harness disqualifies anything that isn't
byte-exact on the roundtrip.

## Results (loop ran to convergence, 4 generations)

Per-file bests discovered by the loop, versus the best off-the-shelf codec
for that file (all lossless, byte-verified):

| file | best baseline (gen 0) | lab best | bytes saved vs baseline |
|---|---|---|---|
| text.txt | bz2-9, 7.99x | gen1.text-token, 9.05x | 12% |
| code.py | bz2-9, 182x | gen1.text-token, 408x | 55% |
| logs.jsonl | bz2-9, 6.65x | gen2.x-struct, 8.38x | 21% |
| series.csv | lzma-9e, 6.04x | gen2.x-struct, 11.55x | 48% |
| audio.pcm | brotli-11, 1.18x | gen3.audio-sin, 1.58x | 25% |
| image.rgb | brotli-11, 1.37x | gen1.media-pngf, 1.85x | 26% |
| random.bin | 1.00x | 1.00x | 0% (control — as theory requires) |

Whole corpus: the best single baseline (brotli-11) manages 2.35x; the lab's
content-routing ensemble (`gen2.x-auto`) reaches **3.02x**, with the gen3
audio result closing the last measured gap after that ensemble was built.

The loop stopped by its own criterion: after generation 3, every file class
measures at or within ~1% of its empirical entropy floor (each generation's
learnings quantify the floor it hit and the ideas that *didn't* work). Full
standings in `knowledge/LEADERBOARD.md`; the story of what each generation
learned from the previous one is in `knowledge/learnings/`.

Try the end product:

```bash
python -m lab.cipher pack corpus/logs.jsonl /tmp/logs.cph            # ~8x smaller
CIPHER_KEY=secret python -m lab.cipher pack corpus/logs.jsonl /tmp/logs.cph.enc --encrypt
python -m lab.cipher unpack /tmp/logs.cph.enc /tmp/logs.out          # byte-identical
```
