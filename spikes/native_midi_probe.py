# Spike B step 2 (OPTIONAL) - run ONLY if Chrome received no events while
# Serato was open. Diagnostic run manually; nothing native ships.
#   pip install mido python-rtmidi && python3 native_midi_probe.py
import mido, time
names = mido.get_input_names()
print("MIDI inputs visible natively:", names)
name = next((n for n in names if "DDJ" in n.upper() or "REV" in n.upper()), names[0] if names else None)
if not name:
    print("No inputs at all -> OS-level block or device asleep."); raise SystemExit
print(f'Listening on "{name}" - touch the controller. Ctrl-C to stop.')
count = 0
with mido.open_input(name) as port:
    last = time.time()
    for msg in port:
        count += 1
        if count % 25 == 1: print(msg)
        if time.time() - last > 2: print(count, "events so far"); last = time.time()
