const P = require("./genre-hits.js");
const G = require("/home/user/Cipher/src/one/guard.js");
const allow = P.allow ? new RegExp(P.allow, "i") : null;
let n = 0, bad = 0;
const EVERY = /every \d+(st|nd|rd|th)? bars?|every (other|4th|8th) bar/i;
const HATED = /\b(roll\w*|fill\w*|build\w*|rise\w*|rising|transition\w*|shuffl\w*|swing\w*|lilt\w*|waltz\w*|whistl\w*|ukulele|bells?|pads?|organ|church|choir|gospel|chop\w*|drops?|memphis|phonk|shrill|piercing)\b/i;
const T = (s, where) => {
  n++;
  const h = G.check(s, allow);
  if (h.length) { bad++; console.log("FAIL", where, JSON.stringify(h.map((x) => x.rule + ":" + x.word)), "|", s); }
  const m = s.match(HATED);
  if (m) { bad++; console.log("HATED", where, m[0], "|", s); }
};
const list = (k, a, min, max) => {
  if (!Array.isArray(a)) { bad++; console.log("NOT LIST", k); return; }
  if (min && (a.length < min || a.length > max)) { bad++; console.log("LEN", k, a.length, "want", min + "-" + max); }
  a.forEach((s, i) => T(s, k + "[" + i + "]"));
  const e = a.filter((s) => EVERY.test(s)).length;
  if (e > 1) { bad++; console.log("EVERY>1", k, e); }
};
T(P.identity, "identity"); T(P.sliders, "sliders"); T(P.title, "title");
list("anchors", P.anchors, 8, 16); list("bpm", P.bpm, 3, 16);
for (const [f, v] of Object.entries(P.families)) {
  list("families." + f + ".src", v.src, 8, 8); list("families." + f + ".ch", v.ch, 7, 7);
  v.src.forEach((s, i) => { if (!/\|the [^|]+$/.test(s)) { bad++; console.log("HANDLE", f, i, s); } });
}
for (const k of ["bass", "btone", "kick", "snare", "hats", "color", "sync", "human", "motif", "artic"]) list(k, P[k], 6, 6);
list("structure", P.structure, 8, 10);
if (P.structure.filter((s) => s.startsWith("[Beat Switch")).length !== 1) { bad++; console.log("SWITCH count wrong"); }
if (P.structure.filter((s) => s.includes("HANDLE")).length < 6) { bad++; console.log("HANDLE count low"); }
if (P.lanes.length < 8 || P.lanes.length > 12) { bad++; console.log("LEN lanes", P.lanes.length); }
const ids = new Set();
for (const L of P.lanes) {
  if (ids.has(L.id)) { bad++; console.log("DUP lane id", L.id); } ids.add(L.id);
  T(L.label, "lane." + L.id + ".label");
  list("lane." + L.id + ".anchors", L.anchors, 4, 4);
  for (const f of Object.keys(L.families)) if (!P.families[f]) { bad++; console.log("lane family missing", L.id, f); }
  if (!L.tempo || L.tempo.length !== 2 || !L.modes || L.modes.length !== 3) { bad++; console.log("lane shape", L.id); }
  if (!L.drums) { bad++; console.log("lane drums missing", L.id); } else for (const d of ["kick", "snare", "hats"]) list("lane." + L.id + ".drums." + d, L.drums[d], 4, 6);
  list("lane." + L.id + ".bassPattern", L.bassPattern, 4, 6);
  if (L.switchAxes) { if (L.switchAxes.length !== 2) { bad++; console.log("switchAxes len", L.id); } L.switchAxes.forEach((a, i) => { T(a.tag, L.id + ".axis.tag" + i); T(a.cue, L.id + ".axis.cue" + i); T(a.after, L.id + ".axis.after" + i); }); }
  if (L.mixTag) T(L.mixTag, "lane." + L.id + ".mixTag");
  if (L.majorShare != null && (L.majorShare < 0 || L.majorShare > 1)) { bad++; console.log("majorShare", L.id); }
}
for (const k of ["id", "title", "identity", "anchors", "bpm", "families", "bass", "btone", "kick", "snare", "hats", "color", "sync", "human", "structure", "motif", "artic", "sliders", "exclude", "ban", "names", "allow", "allowWhy", "lanes"]) if (!(k in P)) { bad++; console.log("MISSING KEY", k); }
new RegExp("\\b(" + P.ban.split(/,\s*/).join("|") + ")\\b", "i");
// no vocal words anywhere in prompt text, even allow-listed
const vocal = /\b(sing|sings|singing|singer|sung|vocal\w*|choir|hum|humming|hummed|chant\w*|harmonies|rap|rapper|rapping)\b/i;
const { exclude, ban, names, allowWhy, ...promptText } = P; const all = JSON.stringify(promptText);
if (vocal.test(all)) { bad++; console.log("FAIL vocal word:", all.match(vocal)[0]); }
// country / banjo only where allowWhy says
const cl = all.match(/\bcountry\b/gi) || [], bl = all.match(/\bbanjo\b/gi) || [];
console.log("country uses:", cl.length, "banjo uses:", bl.length);
console.log("phrases tested:", n, "failed:", bad);
