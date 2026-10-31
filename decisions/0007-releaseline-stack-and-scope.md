# 0007 — ReleaseLine: stdlib Python with optional ffmpeg; v1 scope cuts

**Date:** 2026-10-31
**Phase:** 4
**Decided by:** architect (studio lead acting as architect for speed after the pivot)
**Status:** accepted

## Context
After decision 0006 the owner's product was clear enough to spec directly.

## Options considered
- Electron/desktop app: heavier; a local page is enough.
- Python + Flask + a JS framework: adds installs and a build step that rot.
- Python standard library + plain HTML, ffmpeg optional: zero installs, offline, and
  ffmpeg is the one tool worth depending on because it is the industry's boring default
  for cut/concat/mux.
- Calling Kling/Suno/YouTube APIs: accounts, keys, terms, and churn; out for v1.

## Decision
Standard library only; ffmpeg optional with command fallback. Scope cuts: no audio
analysis or hook detection, no platform APIs or OAuth uploads, no LLM prompt generation
(optional API key is backlog item one), no burned-in captions.

## Why
Buildability in days and "runs with one command" on a Windows laptop. Delight comes from
the one-click cut and assemble when ffmpeg is present, and from prompt drafts that save
staring at a blank Kling box.

## Consequences
Prompt quality rests on template vocab banks; if the owner finds them flat, the first
`/improve` adds an optional Claude call. Revisit platform APIs only if manual upload is
the step where tracks stall.
