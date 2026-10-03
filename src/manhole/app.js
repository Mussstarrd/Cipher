/* MANHOLE UI — one button, optional lead DNA, and a fingerprint memory. */
"use strict";
(() => {
  const E = ManholeEngine;
  const D = E.D;
  const $ = (id) => document.getElementById(id);
  const STORE = "manhole.fingerprints.v1";
  const state = { dna: "", mode: "radio", seed: null };
  let pkg;

  // Fingerprints rolled on this device. Browser storage may be unavailable;
  // the page still works, it just forgets between visits.
  let seen = new Set();
  try {
    seen = new Set(JSON.parse(localStorage.getItem(STORE) || "[]"));
  } catch {
    seen = new Set();
  }
  function remember(fp) {
    seen.add(fp);
    try {
      localStorage.setItem(STORE, JSON.stringify([...seen].slice(-5000)));
    } catch {
      /* storage blocked: keep the in-memory set */
    }
  }

  const newSeed = () => Math.floor(Math.random() * 4294967295);
  function generate() {
    const opts = () => ({ seed: state.seed, dna: state.dna || undefined, wild: state.mode === "wild" });
    pkg = E.generate(opts());
    render();
  }
  function roll() {
    for (let i = 0; i < 40; i++) {
      state.seed = newSeed();
      pkg = E.generate({ seed: state.seed, dna: state.dna || undefined, wild: state.mode === "wild" });
      if (!seen.has(pkg.fingerprint)) break;
    }
    remember(pkg.fingerprint);
    render();
  }

  function seg(id, options, get, set) {
    const el = $(id);
    el.textContent = "";
    for (const o of options) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = o.label;
      const on = get() === o.id;
      b.className = on ? "on" : "";
      b.setAttribute("aria-pressed", String(on));
      b.onclick = () => {
        set(o.id);
        seg(id, options, get, set);
        generate();
      };
      el.appendChild(b);
    }
  }

  function drawDna() {
    const el = $("dna");
    el.textContent = "";
    const all = [{ id: "", label: "Any", refs: "random cross" }, ...D.DNA];
    for (const d of all) {
      const b = document.createElement("button");
      b.type = "button";
      const on = state.dna === d.id;
      b.className = "chip" + (on ? " on" : "");
      b.setAttribute("aria-pressed", String(on));
      const t = document.createElement("span");
      t.textContent = d.label;
      const s = document.createElement("small");
      s.textContent = d.refs;
      b.append(t, s);
      b.onclick = () => {
        state.dna = d.id;
        drawDna();
        roll();
      };
      el.appendChild(b);
    }
  }

  function dl(el, rows) {
    el.textContent = "";
    for (const [k, v] of rows) {
      const dt = document.createElement("dt");
      dt.textContent = k;
      const dd = document.createElement("dd");
      if (Array.isArray(v)) {
        for (const c of v) {
          const s = document.createElement("span");
          s.className = "chord";
          s.textContent = c;
          dd.appendChild(s);
        }
      } else dd.textContent = v;
      el.append(dt, dd);
    }
  }
  function tag(text, hot) {
    const s = document.createElement("span");
    s.className = "tag" + (hot ? " hot" : "");
    s.textContent = text;
    return s;
  }

  function render() {
    const m = pkg.meta;
    const recipe = $("recipe");
    recipe.textContent = "";
    recipe.append(
      tag(`${m.dnaLabel} · ${m.dnaRefs}`, true),
      tag(`× ${m.secondLabel} · ${m.secondRefs}`),
      tag(`${m.bpm} BPM`),
      tag(pkg.harmony.key),
      tag(`${m.structureLabel} ${m.length}`)
    );
    $("out-style").textContent = pkg.styleText;
    $("style-meta").textContent = `${m.styleChars} / 1000 characters`;
    $("out-lyrics").textContent = pkg.lyricsText;
    $("out-exclude").textContent = pkg.excludeText;
    const p = pkg.parts;
    dl($("print-grid"), [
      ["House rule", p.rule],
      ["Waltz cadence", p.waltz],
      ["On / off the beat", p.onOff],
      ["Beat switch", p.beatSwitch],
      ["Lead", p.lead],
      ["Borrowed trait", p.trait],
    ]);
    $("print-meta").textContent = `roll #${m.seed} · ${seen.size} one-of-ones rolled on this device`;
    const s = pkg.settings;
    dl($("out-settings"), [
      ["Model", s.model],
      ["Instrumental", "On"],
      ["Weirdness", s.weirdness],
      ["Style influence", s.styleInfluence],
      ["Variety", s.variety],
      ["Max Mode", s.maxMode],
    ]);
    $("settings-note").textContent = s.note;
    dl($("h-grid"), [["Key", pkg.harmony.key], ["Loop", `${pkg.harmony.roman}`], ["Chords", pkg.harmony.chords]]);
    $("h-cadence").textContent = pkg.harmony.cadence;
    const notes = $("out-notes");
    notes.textContent = "";
    $("notes-card").hidden = pkg.warnings.length === 0;
    for (const w of pkg.warnings) {
      const li = document.createElement("li");
      li.textContent = `${w.level}: ${w.message}`;
      notes.appendChild(li);
    }
  }

  for (const btn of document.querySelectorAll(".copy[data-copy]")) {
    btn.onclick = async () => {
      const kind = btn.dataset.copy;
      const text = kind === "style" ? pkg.styleText : kind === "exclude" ? pkg.excludeText : pkg.lyricsText;
      try {
        await navigator.clipboard.writeText(text);
      } catch {
        const ta = document.createElement("textarea");
        ta.value = text;
        document.body.appendChild(ta);
        ta.select();
        document.execCommand("copy");
        ta.remove();
      }
      const label = btn.textContent;
      btn.textContent = "Copied";
      btn.classList.add("done");
      setTimeout(() => {
        btn.textContent = label;
        btn.classList.remove("done");
      }, 1200);
    };
  }

  drawDna();
  seg("mode", [{ id: "radio", label: "Radio" }, { id: "wild", label: "Wild" }], () => state.mode, (v) => (state.mode = v));
  $("roll").onclick = roll;
  roll();
})();
