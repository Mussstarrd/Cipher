# Cipher Lab — multi-agent compression research loop

This repo is an automated experimentation loop in which multiple agents
cooperate to discover more efficient ways to compress (and then encrypt)
files: text, structured data, audio, images.

## Honest framing

This is **not** unbounded recursive self-improvement. Lossless compression is
bounded below by the entropy of the source (Shannon), and general-purpose
codecs (zstd, brotli, LZMA) are within a few percent of practical limits for
generic byte streams. What a learning loop *can* do — and what this lab
demonstrates — is search the space of **content-aware pipelines**
(preprocessing transform + codec + parameters) far faster than a human,
accumulate transferable findings in a knowledge base, and converge on
per-content-type winners. That is the same playbook that gives PNG its filter
step, FLAC its linear prediction, and Parquet its columnar layout.

Encryption is a separate, fixed stage: compress first, then AES-256-GCM
(`lab/crypto.py`). Ciphertext is incompressible, so the order is forced, and
inventing novel ciphers is a non-goal (using anything but reviewed, standard
cryptography would make security *worse*, not better).

## The loop

```
        ┌────────────────────────────────────────────────┐
        │                 ORCHESTRATOR                   │
        │  picks focus areas, spawns agents, merges      │
        │  results, updates leaderboard, decides next gen│
        └───────┬───────────────┬───────────────┬────────┘
        spawns  │               │               │   (parallel)
        ┌───────▼─────┐ ┌───────▼─────┐ ┌───────▼─────┐
        │ SPECIALIST  │ │ SPECIALIST  │ │ SPECIALIST  │
        │  agent A    │ │  agent B    │ │  agent C    │
        └───────┬─────┘ └───────┬─────┘ └───────┬─────┘
                │ each: read knowledge base →   │
                │ hypothesize → implement →     │
                │ benchmark → write learnings   │
        ┌───────▼───────────────▼───────────────▼────────┐
        │                KNOWLEDGE BASE                  │
        │  knowledge/results/*.jsonl  (measurements)     │
        │  knowledge/learnings/*.md   (prose findings)   │
        │  knowledge/LEADERBOARD.md   (rendered summary) │
        └────────────────────────────────────────────────┘
                │ next generation reads all of the above
                ▼
        generation N+1 refines winners / combines ideas
```

The "learning" is the knowledge base: every generation's agents start by
reading all prior measurements and findings, so hypotheses compound instead
of restarting.

## Rules every agent must follow

1. **Lossless or disqualified.** The harness byte-compares the roundtrip;
   a mismatch marks the strategy FAILED. A strategy whose transform only
   applies to some content must detect that and fall back (self-describing
   container with a mode byte).
2. **Own files only.** Each agent writes one new module
   `lab/strategies/<gen>_<specialty>.py`, one learnings file
   `knowledge/learnings/<gen>_<specialty>.md`, and benchmarks with its own
   `--tag <gen>-<specialty>`. Never edit another agent's module, the harness,
   corpus, or crypto stage.
3. **Measure, don't claim.** Every stated finding must cite harness numbers.
   Negative results (things that didn't help, and why) go in learnings too —
   they save the next generation from repeating them.
4. **Real cryptography only.** No home-grown ciphers, ever.

## Running

```
python -m lab.corpus                    # (re)build deterministic corpus
python -m lab.harness --tag gen0        # benchmark all registered strategies
python -m lab.harness --tag gen1-x --only my-strategy --agent me
python -m lab.knowledge                 # render knowledge/LEADERBOARD.md
```

## Generation protocol (what the orchestrator does)

1. Render the leaderboard; identify weakest per-file results and untested ideas.
2. Spawn N specialist agents in parallel, each with a focus area and the
   rules above.
3. When all report back, re-render the leaderboard, sanity-check surprising
   results (re-run the winner once), and merge learnings.
4. Repeat with the next generation seeded by the updated knowledge base,
   until a generation yields < ~1% improvement on every file class (diminishing
   returns — stop) or a target is met.
