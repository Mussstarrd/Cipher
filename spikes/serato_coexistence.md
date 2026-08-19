# Spike B — Serato coexistence

## Environment
Chrome UA: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36
Platform: Win32 · cores: 32
OS: Windows 11 Home 25H2, build 26200.9168 (Lenovo Legion Pro 5 16IRX9, i9-14900HX, 16 GB RAM, RTX 4060)
Serato DJ Lite version: 4.0.9 (owner confirmed it was actively controlling the deck before the capture: yes)
Chrome: 151.0.7922.170 (64-bit)

## Result (harness capture, Serato open) — 2026-08-19, owner-run on real hardware
MIDI inputs visible to Chrome: DDJ-REV1
Events received in 10 s: 1306
Status-byte breakdown: {"91":4,"95":2,"97":16,"99":4,"b1":701,"b0":577,"b6":2}
VERDICT: Chrome RECEIVES MIDI alongside Serato (coexistence works at this layer)

Interpretation: b0/b1 = ch1/ch2 CCs (knobs/faders being wiggled), 9x = note-ons
across four channels (pads/buttons), b6 = ch7 CC (consistent with crossfader).
Coherent event stream, not partial leakage.

## Native probe
Not needed — Chrome path works.

## Open item
- [ ] Confirm Serato remained responsive to the controller DURING the Chrome
      capture window (simultaneous, not sequential). Owner to verify with a
      30-second two-app wiggle check.

## Conclusion
- [x] Coexistence works in Chrome on Windows → **Branch 1 (scoring overlay) viable**
- [ ] Blocked in Chrome, native sees events → MIDI relay bridge
- [ ] Blocked at OS level → Branch 2 or re-plan

Consequences per brief §2: Serato is the audio engine; we are a pure scoring
overlay. JUCE/ASIO phase deleted. Spike C is downgraded from existential to
informative (fallback path only). macOS remains untested — expected to work
(CoreMIDI multi-client) but verify before shipping to Mac users.
