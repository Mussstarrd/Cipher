/* CIPHER UI — wires the engine to the page. */
"use strict";
(() => {
  const E = CipherEngine;
  const $ = (id) => document.getElementById(id);
  const newSeed = () => Math.floor(Math.random() * 1e9);

  const state = {
    family: "all",
    lane: "dark-trap",
    likeness: "",
    edge: "twitch",
    blend: "",
    vocal: "auto",
    key: "",
    progression: "",
    plan: "",
    template: "",
    hook: "",
    bpm: undefined,
    postHook: false,
    chordsInTags: false,
    // v6 tends to add humming to intros and outros; pre-banned.
    extraBans: ["humming"],
    profile: "v6",
    lyricsMode: "tags",
    lyrics: {},
    seed: newSeed(),
  };
  let pkg;

  const FAMILIES = [
    { id: "all", label: "All" },
    { id: "hiphop", label: "Hip hop" },
    { id: "rnb", label: "R&B" },
  ];
  const laneById = (id) => E.LANES.find((l) => l.id === id);
  const TEMPLATE_OPTIONS = [{ id: "", label: "Auto" }, ...Object.entries(E.TEMPLATES).map(([id, t]) => ({ id, label: t.label }))];

  function generate() {
    pkg = E.generate({
      lane: state.lane,
      likeness: state.likeness || undefined,
      edge: state.edge,
      blend: state.blend || undefined,
      vocal: state.vocal,
      key: state.key || undefined,
      progression: state.progression || undefined,
      plan: state.plan || undefined,
      template: state.template || undefined,
      hook: state.hook || undefined,
      bpm: state.bpm,
      postHook: state.postHook,
      chordsInTags: state.chordsInTags,
      instrumental: state.lyricsMode === "instrumental",
      extraBans: state.extraBans,
      profile: state.profile,
      seed: state.seed,
    });
    render();
  }

  // ------------------------------------------------------------ controls
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

  function fillSelect(id, options, value) {
    const el = $(id);
    el.textContent = "";
    for (const o of options) {
      const opt = document.createElement("option");
      opt.value = o.id;
      opt.textContent = o.label;
      el.appendChild(opt);
    }
    el.value = value;
  }

  function drawLanes() {
    const el = $("lanes");
    el.textContent = "";
    for (const lane of E.LANES) {
      if (state.family !== "all" && lane.family !== state.family) continue;
      const b = document.createElement("button");
      b.type = "button";
      const on = state.lane === lane.id;
      b.className = "chip" + (on ? " on" : "");
      b.setAttribute("aria-pressed", String(on));
      b.append(lane.label);
      if (state.family === "all") {
        const f = document.createElement("span");
        f.className = "fam";
        f.textContent = lane.family === "rnb" ? "R&B" : "Hip hop";
        b.appendChild(f);
      }
      b.onclick = () => selectLane(lane.id);
      el.appendChild(b);
    }
  }

  function drawLikeness() {
    const el = $("likeness");
    el.textContent = "";
    const none = document.createElement("button");
    none.type = "button";
    none.className = "chip" + (state.likeness ? "" : " on");
    none.textContent = "None";
    none.setAttribute("aria-pressed", String(!state.likeness));
    none.onclick = () => {
      state.likeness = "";
      drawLikeness();
      generate();
    };
    el.appendChild(none);
    for (const like of E.LIKENESS) {
      if (state.family !== "all" && like.family !== state.family) continue;
      const on = state.likeness === like.id;
      const b = document.createElement("button");
      b.type = "button";
      b.className = "chip" + (on ? " on" : "");
      b.setAttribute("aria-pressed", String(on));
      b.append(like.name);
      if (state.family === "all") {
        const f = document.createElement("span");
        f.className = "fam";
        f.textContent = like.family === "rnb" ? "R&B" : "Hip hop";
        b.appendChild(f);
      }
      b.onclick = () => {
        state.likeness = like.id;
        state.template = "";
        seg("template", TEMPLATE_OPTIONS, () => state.template, (v) => (state.template = v));
        drawLikeness();
        // Jump to the artist's home lane; the user can move off it afterwards.
        selectLane(like.homeLane);
      };
      el.appendChild(b);
    }
  }

  function selectLane(id) {
    state.lane = id;
    state.progression = "";
    if (state.blend === id) state.blend = "";
    drawLanes();
    drawLaneSelects();
    generate();
  }

  function drawLaneSelects() {
    const lane = laneById(state.lane);
    fillSelect(
      "blend",
      [{ id: "", label: "No blend" }, ...E.LANES.filter((l) => l.id !== lane.id).map((l) => ({ id: l.id, label: l.label }))],
      state.blend
    );
    fillSelect(
      "progression",
      [
        { id: "", label: "Progression: lane pick" },
        ...lane.progressions.map((p) => ({ id: p, label: `${E.PROGRESSIONS[p].name} (${E.PROGRESSIONS[p].roman})` })),
      ],
      state.progression
    );
  }

  function drawBans() {
    const el = $("bans");
    el.textContent = "";
    for (const ban of E.HARD_BANS) {
      const c = document.createElement("span");
      c.className = "chip fixed";
      c.textContent = ban;
      el.appendChild(c);
    }
    const options = [...new Set([...E.OPTIONAL_BANS, ...state.extraBans])];
    for (const ban of options) {
      const on = state.extraBans.includes(ban);
      const b = document.createElement("button");
      b.type = "button";
      b.className = "chip" + (on ? " on" : "");
      b.textContent = on ? `${ban} ✕` : `+ ${ban}`;
      b.setAttribute("aria-pressed", String(on));
      b.onclick = () => {
        state.extraBans = on ? state.extraBans.filter((x) => x !== ban) : [...state.extraBans, ban];
        drawBans();
        generate();
      };
      el.appendChild(b);
    }
  }

  // ------------------------------------------------------------ output
  function lyricsText() {
    return state.lyricsMode === "bars" ? E.renderSections(pkg.sections, state.lyrics) : pkg.lyricsTagsOnly;
  }

  function drawLyrics() {
    const tags = $("out-tags");
    const editor = $("lyrics-editor");
    if (state.lyricsMode === "instrumental") {
      $("lyrics-hint").textContent =
        "Structured instrumental. Paste these tags into the lyrics box and turn Suno's Instrumental toggle on. Each tag says what the beat does; the style field drops the vocal words and the Exclude field adds vocals, rap and singing.";
      tags.hidden = false;
      editor.hidden = true;
      tags.textContent = pkg.lyricsTagsOnly;
      return;
    }
    if (state.lyricsMode === "tags") {
      $("lyrics-hint").textContent =
        "Structure only. Suno writes the words around these tags and keeps the section order. Hooks are marked identical so they repeat exactly.";
      tags.hidden = false;
      editor.hidden = true;
      tags.textContent = pkg.lyricsTagsOnly;
      return;
    }
    tags.hidden = true;
    editor.hidden = false;
    $("lyrics-hint").textContent =
      "Write each section once; a repeated hook reprints the same words. Ad-libs go in (parentheses), 1–3 words. Hyphen-chain-words for fast flow, st-st-stutter for twitch, CAPS for a shouted word.";
    editor.textContent = "";
    const done = new Set();
    for (const s of pkg.sections) {
      const line = document.createElement("div");
      line.className = "tagline";
      line.textContent = s.tag;
      editor.appendChild(line);
      if (!s.slot) continue;
      if (done.has(s.slot)) {
        const rep = document.createElement("span");
        rep.className = "repeat";
        rep.textContent = "  same words repeat here";
        line.appendChild(rep);
        continue;
      }
      done.add(s.slot);
      const ta = document.createElement("textarea");
      ta.className = "lyric-input";
      ta.id = `lyric-${s.slot}`;
      ta.rows = s.slot.startsWith("verse") ? 6 : 4;
      ta.placeholder = s.guide || "";
      ta.value = state.lyrics[s.slot] || "";
      ta.setAttribute("aria-label", `${s.name} lyrics`);
      ta.oninput = () => {
        if (ta.value.trim()) state.lyrics[s.slot] = ta.value;
        else delete state.lyrics[s.slot];
      };
      editor.appendChild(ta);
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

  function render() {
    const m = pkg.meta;
    $("out-style").textContent = pkg.styleText;
    $("style-meta").textContent =
      `${m.styleChars} / ${m.styleLimit} chars · ${m.descriptors} descriptors · ${m.laneLabel}` +
      (m.blend ? ` + ${laneById(m.blend).label}` : "") +
      (m.likenessName ? ` · ${m.likenessName} likeness` : "") +
      ` · ${E.EDGES[m.edge].label} · ${m.bpm} BPM · ${E.TEMPLATES[m.template].label} ${m.templateLength} · roll #${m.seed % 1000}`;
    $("out-exclude").textContent = pkg.excludeText;
    $("vocal").disabled = !!m.instrumental;
    drawLyrics();

    const h = pkg.harmony;
    $("h-key").textContent = h.key;
    $("h-roman").textContent = `${h.progression} · ${h.roman}`;
    dl($("h-grid"), [["Loop", h.chords], ...h.bySection]);
    $("h-cadence").innerHTML = "";
    const cb = document.createElement("strong");
    cb.textContent = "Cadence. ";
    $("h-cadence").append(cb, h.cadence);
    $("h-plan").innerHTML = "";
    const pb = document.createElement("strong");
    pb.textContent = `${h.plan.label}. `;
    $("h-plan").append(pb, h.plan.note);

    const bp = pkg.blueprint;
    $("bp-map").textContent = bp.map;
    $("bp-name").textContent = bp.name;
    dl($("bp-grid"), [
      ["Rule", bp.rule],
      ["Rhythm", bp.rhythm],
      ["Length", bp.syllables],
      ["Melody", bp.contour],
    ]);
    const ul = $("bp-rules");
    ul.textContent = "";
    for (const r of bp.rules) {
      const li = document.createElement("li");
      li.textContent = r;
      ul.appendChild(li);
    }

    const sl = $("out-sliders");
    sl.textContent = "";
    for (const [name, [lo, hi]] of [["Weirdness", pkg.sliders.weirdness], ["Style influence", pkg.sliders.styleInfluence]]) {
      const row = document.createElement("div");
      row.className = "slider-row";
      row.innerHTML = `<span class="name"></span><span class="track"><span class="fill" style="left:${lo}%;width:${hi - lo}%"></span></span><span class="val">${lo}–${hi}%</span>`;
      row.querySelector(".name").textContent = name;
      sl.appendChild(row);
    }
    for (const [name, value] of [["Variety", pkg.sliders.variety], ["Max Mode", pkg.sliders.maxMode]]) {
      const row = document.createElement("div");
      row.className = "slider-row";
      const n = document.createElement("span");
      n.className = "name";
      n.textContent = name;
      const v = document.createElement("span");
      v.className = "text-val";
      v.textContent = value;
      row.append(n, v);
      sl.appendChild(row);
    }
    $("slider-note").textContent = pkg.sliderNote;

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
        const text = kind === "style" ? pkg.styleText : kind === "exclude" ? pkg.excludeText : lyricsText();
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

  // ------------------------------------------------------------ boot
  function init() {
    seg("family", FAMILIES, () => state.family, (v) => {
      state.family = v;
      const lane = laneById(state.lane);
      if (v !== "all" && lane.family !== v) {
        state.lane = E.LANES.find((l) => l.family === v).id;
        state.progression = "";
        drawLaneSelects();
      }
      drawLanes();
      drawLikeness();
    });
    drawLanes();
    drawLikeness();
    seg("edge", Object.entries(E.EDGES).map(([id, e]) => ({ id, label: e.label })), () => state.edge, (v) => (state.edge = v));
    seg(
      "plan",
      [{ id: "", label: "Lane default" }, ...Object.entries(E.HARMONIC_PLANS).map(([id, p]) => ({ id, label: p.label }))],
      () => state.plan,
      (v) => (state.plan = v)
    );
    seg("template", TEMPLATE_OPTIONS, () => state.template, (v) => (state.template = v));
    seg("lyrics-mode", [{ id: "tags", label: "Tags only" }, { id: "bars", label: "Write bars" }, { id: "instrumental", label: "Instrumental" }], () => state.lyricsMode, (v) => (state.lyricsMode = v));
    drawLaneSelects();
    fillSelect("key", [{ id: "", label: "Key: lane pick" }, ...E.keyOptions().map((k) => ({ id: k, label: k }))], "");
    fillSelect("hook", [{ id: "", label: "Hook: lane pick" }, ...Object.entries(E.HOOKS).map(([id, h]) => ({ id, label: h.name }))], "");
    fillSelect("profile", Object.values(E.PROFILES).map((p) => ({ id: p.id, label: `Suno ${p.label}` })), state.profile);
    drawBans();

    const onSelect = (id, key) => {
      $(id).onchange = () => {
        state[key] = $(id).value;
        generate();
      };
    };
    onSelect("blend", "blend");
    onSelect("vocal", "vocal");
    onSelect("key", "key");
    onSelect("progression", "progression");
    onSelect("hook", "hook");
    onSelect("profile", "profile");
    for (const id of ["postHook", "chordsInTags"]) {
      $(id).onchange = () => {
        state[id] = $(id).checked;
        generate();
      };
    }
    $("bpm").onchange = () => {
      const v = Number($("bpm").value);
      state.bpm = $("bpm").value && v >= 55 && v <= 190 ? v : undefined;
      generate();
    };
    $("ban-input").onkeydown = (e) => {
      if (e.key !== "Enter") return;
      const v = $("ban-input").value.trim().toLowerCase();
      if (v && !state.extraBans.includes(v) && !E.HARD_BANS.includes(v)) {
        state.extraBans = [...state.extraBans, v];
        drawBans();
        generate();
      }
      $("ban-input").value = "";
    };
    $("reroll").onclick = () => {
      state.seed = newSeed();
      generate();
    };
    $("randomize").onclick = () => {
      const pool = E.LANES.filter((l) => state.family === "all" || l.family === state.family);
      const lane = pool[Math.floor(Math.random() * pool.length)];
      const others = E.LANES.filter((l) => l.id !== lane.id);
      state.edge = Object.keys(E.EDGES)[Math.floor(Math.random() * 3)];
      state.blend = Math.random() < 0.4 ? others[Math.floor(Math.random() * others.length)].id : "";
      state.key = "";
      state.hook = "";
      state.likeness = Math.random() < 0.5 ? E.LIKENESS.filter((l) => l.family === lane.family)[Math.floor(Math.random() * 99) % E.LIKENESS.filter((l) => l.family === lane.family).length].id : "";
      drawLikeness();
      $("key").value = "";
      $("hook").value = "";
      state.seed = newSeed();
      seg("edge", Object.entries(E.EDGES).map(([id, e]) => ({ id, label: e.label })), () => state.edge, (v) => (state.edge = v));
      selectLane(lane.id);
    };
    wireCopy();
    generate();
  }

  init();
})();
