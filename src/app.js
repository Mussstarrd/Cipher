/* CIPHER UI — a one-button randomizer over the engine. */
"use strict";
(() => {
  const E = CipherEngine;
  const $ = (id) => document.getElementById(id);
  const rand = (arr) => arr[Math.floor(Math.random() * arr.length)];
  const chance = (p) => Math.random() < p;

  const FAMILIES = [
    { id: "all", label: "Anything" },
    { id: "hiphop", label: "Hip hop" },
    { id: "rnb", label: "R&B" },
  ];
  const state = { family: "all", instrumental: false, profile: "v6", roll: null };
  let pkg;

  // The whole recipe is decided here; the engine then renders it deterministically.
  function rollRecipe() {
    const lanes = E.LANES.filter((l) => state.family === "all" || l.family === state.family);
    const lane = rand(lanes);
    const likes = E.LIKENESS.filter((l) => l.family === lane.family);
    const likeness = chance(0.55) ? rand(likes) : null;
    const others = E.LANES.filter((l) => l.id !== lane.id && l.family === lane.family);
    const edges = Object.keys(E.EDGES);
    return {
      lane: lane.id,
      likeness: likeness ? likeness.id : undefined,
      edge: rand(edges),
      blend: chance(0.3) ? rand(others).id : undefined,
      template: chance(0.25) ? rand(Object.keys(E.TEMPLATES)) : undefined,
      postHook: chance(0.3),
      chordsInTags: chance(0.2),
      vocal: chance(0.7) ? "auto" : rand(["male", "female", "duet"]),
      seed: Math.floor(Math.random() * 1e9),
    };
  }

  function generate() {
    if (!state.roll) state.roll = rollRecipe();
    pkg = E.generate({ ...state.roll, instrumental: state.instrumental, profile: state.profile });
    render();
  }

  function roll() {
    state.roll = rollRecipe();
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
    const laneById = (id) => E.LANES.find((l) => l.id === id);
    const recipe = $("recipe");
    recipe.textContent = "";
    recipe.append(tag(m.laneLabel, true));
    if (m.blend) recipe.append(tag(`+ ${laneById(m.blend).label}`));
    if (m.likenessName) recipe.append(tag(`${m.likenessName} likeness`, true));
    recipe.append(
      tag(E.EDGES[m.edge].label),
      tag(`${m.bpm} BPM`),
      tag(pkg.harmony.key),
      tag(`${E.TEMPLATES[m.template].label} ${m.templateLength}`),
      tag(m.instrumental ? "Instrumental" : E.HOOKS[m.hook].name),
      tag(`roll #${m.seed}`)
    );

    $("out-style").textContent = pkg.styleText;
    $("style-meta").textContent = `${m.styleChars} / ${m.styleLimit} chars · ${m.descriptors} descriptors`;
    $("out-exclude").textContent = pkg.excludeText;
    $("lyrics-hint").textContent = m.instrumental
      ? "Structured instrumental. Paste into the lyrics box and turn Suno's Instrumental toggle on."
      : "Structure tags. Suno writes the words around them; the hook is marked identical so it repeats exactly.";
    $("out-tags").textContent = pkg.lyricsTagsOnly;

    dl($("out-settings"), [
      ["Model", `Suno ${E.PROFILES[m.profile].label}`],
      ["Weirdness", `${pkg.sliders.weirdness[0]}–${pkg.sliders.weirdness[1]}%`],
      ["Style influence", `${pkg.sliders.styleInfluence[0]}–${pkg.sliders.styleInfluence[1]}%`],
      ["Variety", pkg.sliders.variety],
      ["Max Mode", pkg.sliders.maxMode],
    ]);
    $("slider-note").textContent = pkg.sliderNote;

    const h = pkg.harmony;
    $("h-head").textContent = "";
    const hb = document.createElement("strong");
    hb.textContent = `${h.key} · ${h.progression} · ${h.roman}`;
    $("h-head").append(hb);
    dl($("h-grid"), [["Loop", h.chords], ...h.bySection]);
    $("h-cadence").textContent = "";
    const cb = document.createElement("strong");
    cb.textContent = "Cadence. ";
    $("h-cadence").append(cb, h.cadence);
    $("h-plan").textContent = "";
    const pb = document.createElement("strong");
    pb.textContent = `${h.plan.label}. `;
    $("h-plan").append(pb, h.plan.note);

    const bp = pkg.blueprint;
    $("bp-head").textContent = "";
    const bb = document.createElement("strong");
    bb.textContent = `Hook: ${bp.name} · ${bp.map}`;
    $("bp-head").append(bb);
    dl($("bp-grid"), [["Rule", bp.rule], ["Rhythm", bp.rhythm], ["Length", bp.syllables], ["Melody", bp.contour]]);
    const ul = $("bp-rules");
    ul.textContent = "";
    for (const r of bp.rules) {
      const li = document.createElement("li");
      li.textContent = r;
      ul.appendChild(li);
    }

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
        const text = kind === "style" ? pkg.styleText : kind === "exclude" ? pkg.excludeText : pkg.lyricsTagsOnly;
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

  function init() {
    seg("family", FAMILIES, () => state.family, (v) => (state.family = v));
    const profile = $("profile");
    for (const p of Object.values(E.PROFILES)) {
      const o = document.createElement("option");
      o.value = p.id;
      o.textContent = `Suno ${p.label}`;
      profile.appendChild(o);
    }
    profile.value = state.profile;
    profile.onchange = () => {
      state.profile = profile.value;
      generate();
    };
    $("instrumental").onchange = () => {
      state.instrumental = $("instrumental").checked;
      generate();
    };
    $("roll").onclick = roll;
    wireCopy();
    generate();
  }

  init();
})();
