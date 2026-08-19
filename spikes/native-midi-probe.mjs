// Spike B step 2 (OPTIONAL) — run ONLY if Chrome received no events while
// Serato was open. Diagnostic script run manually; nothing native ships.
//   npm install easymidi && node native-midi-probe.mjs   (with Serato open)
import easymidi from 'easymidi';
const inputs = easymidi.getInputs();
console.log('MIDI inputs visible natively:', inputs);
const name = inputs.find(n => /ddj|rev/i.test(n)) || inputs[0];
if (!name) { console.log('No inputs at all -> OS-level block or device asleep.'); process.exit(0); }
console.log(`Listening on "${name}" — touch the controller. Ctrl-C to stop.`);
const input = new easymidi.Input(name);
let count = 0;
for (const kind of ['noteon', 'noteoff', 'cc']) {
  input.on(kind, m => { count++; if (count % 25 === 1) console.log(kind, m); });
}
setInterval(() => console.log(`${count} events so far`), 2000);
