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

## Results

See `knowledge/LEADERBOARD.md` for current standings and
`knowledge/learnings/` for what each generation figured out.
