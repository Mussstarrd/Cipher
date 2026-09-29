/* ANOMALY UI — one button over the engine. */
"use strict";
(() => {
  const E = AnomalyEngine;
  const $ = (id) => document.getElementById(id);
  const state = { family: "", mode: "radio", seed: null };
  const recentKeys = [];
  let pkg;

  const FAMILIES = [
    { id: "", label: "Anything" },
    { id: "hiphop", label: "Hip hop" },
    { id: "rnb", label: "R&B" },
  ];
  const MODES = [
    { id: "radio", label: "Radio" },
    { id: "wild", label: "Experimental" },
  ];

  function generate() {
    if (state.seed === null) state.seed = Math.floor(Math.random() * 1e9);
    pkg = E.generate({ seed: state.seed, family: state.family || undefined, wild: state.mode === "wild", avoidKeys: recentKeys });
    if (recentKeys[recentKeys.length - 1] !== pkg.harmony.key) recentKeys.push(pkg.harmony.key);
    if (recentKeys.length > 4) recentKeys.shift();
    render();
  }
  function roll() {
    state.seed = Math.floor(Math.random() * 1e9);
    generate();
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
        roll();
      };
      el.appendChild(b);
    }
  }

  function dl(el, rows) {
    el.textContent = "";
    for (const [k, v, mono] of rows) {
      const dt = document.createElement("dt");
      dt.textContent = k;
      const dd = document.createElement("dd");
      if (mono) dd.className = "mono";
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
      tag(m.worldLabel, true),
      tag(`${m.bpm} BPM`),
      tag(pkg.harmony.key),
      tag(`${m.structureLabel} ${m.length}`),
      tag(`roll #${m.seed}`)
    );
    $("out-style").textContent = pkg.styleText;
    $("style-meta").textContent = `${m.styleChars} / 1000 chars · ${m.descriptors} descriptors`;
    $("out-lyrics").textContent = pkg.lyricsText;
    $("out-exclude").textContent = pkg.excludeText;
    dl($("sig-grid"), [
      ["Anomaly", pkg.signature],
      ["Clash", pkg.clash],
      ["Technical flex", pkg.flex],
      ["Twitch", pkg.glitch],
      ["Beat switch", pkg.beatSwitch],
    ]);
    const s = pkg.settings;
    dl($("out-settings"), [
      ["Model", s.model],
      ["Instrumental", "On"],
      ["Weirdness", s.weirdness, true],
      ["Style influence", s.styleInfluence, true],
      ["Variety", s.variety],
      ["Max Mode", s.maxMode],
    ]);
    $("settings-note").textContent = s.note;
    const h = pkg.harmony;
    dl($("h-grid"), [
      ["Key", h.key, true],
      ["Progression", `${h.progression} · ${h.roman}`],
      ["Chords", h.chords],
    ]);
    $("h-cadence").textContent = h.cadence;
    const notes = $("out-notes");
    notes.textContent = "";
    $("notes-card").hidden = pkg.warnings.length === 0;
    for (const w of pkg.warnings) {
      const li = document.createElement("li");
      const b = document.createElement("span");
      b.className = `badge ${w.level}`;
      b.textContent = w.level;
      li.append(b, w.message);
      notes.appendChild(li);
    }
  }

  function wireCopy() {
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
  }

  seg("family", FAMILIES, () => state.family, (v) => (state.family = v));
  seg("mode", MODES, () => state.mode, (v) => (state.mode = v));
  $("roll").onclick = roll;
  wireCopy();
  generate();
})();
