/* ONE OF ONE — the page. One button; the four Suno fields in Suno's order. */
"use strict";
(() => {
  const E = OneEngine;
  const $ = (id) => document.getElementById(id);
  const STORE = "oneofone.seen.v1";
  let pkg;

  let seen = new Set();
  try {
    seen = new Set(JSON.parse(localStorage.getItem(STORE) || "[]"));
  } catch {
    seen = new Set();
  }
  const remember = (fp) => {
    seen.add(fp);
    try {
      localStorage.setItem(STORE, JSON.stringify([...seen].slice(-5000)));
    } catch {
      /* storage blocked: the in-memory set still prevents repeats this visit */
    }
  };
  const recentKeys = [];

  function roll() {
    for (let i = 0; i < 40; i++) {
      pkg = E.generate({ seed: Math.floor(Math.random() * 4294967295), avoidKeys: recentKeys });
      if (!seen.has(pkg.fingerprint)) break;
    }
    remember(pkg.fingerprint);
    recentKeys.push(pkg.meta.key);
    if (recentKeys.length > 4) recentKeys.shift();
    render();
  }

  function slider(name, value) {
    const row = document.createElement("div");
    row.className = "slider";
    const label = document.createElement("span");
    label.textContent = name;
    const track = document.createElement("span");
    track.className = "track";
    const fill = document.createElement("span");
    fill.className = "fill";
    fill.style.width = `${value}%`;
    const knob = document.createElement("span");
    knob.className = "knob";
    knob.style.left = `${value}%`;
    track.append(fill, knob);
    const val = document.createElement("span");
    val.className = "val";
    val.textContent = `${value}%`;
    row.append(label, track, val);
    return row;
  }
  function chip(label, value) {
    const c = document.createElement("span");
    c.className = "chip";
    const b = document.createElement("b");
    b.textContent = value;
    c.append(`${label} `, b);
    return c;
  }

  function render() {
    $("roll-id").textContent = `#${pkg.meta.seed.toString(36).toUpperCase()}`;
    const vibe = $("vibe");
    vibe.textContent = "";
    const em = document.createElement("em");
    em.textContent = pkg.meta.title;
    vibe.append(em, ` · ${pkg.meta.bpm} BPM · ${pkg.meta.key}`);
    $("lyrics").textContent = pkg.lyrics;
    $("lyrics-count").textContent = `${pkg.lyrics.length}/5000`;
    $("style").textContent = pkg.style;
    $("style-count").textContent = `${pkg.style.length}/1000`;
    $("exclude").textContent = pkg.exclude;
    const sl = $("sliders");
    sl.textContent = "";
    for (const [name, v] of [["Weirdness", pkg.sliders.weirdness], ["Style Influence", pkg.sliders.styleInfluence]]) sl.appendChild(slider(name, v));
    const chips = $("chips");
    chips.textContent = "";
    chips.append(chip("Model", pkg.settings.model), chip("Variety", pkg.settings.variety), chip("Max Mode", pkg.settings.maxMode), chip("Instrumental", "On"));
  }

  for (const btn of document.querySelectorAll(".copy[data-copy]")) {
    btn.onclick = async () => {
      const text = pkg[btn.dataset.copy];
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
      btn.textContent = "Copied";
      btn.classList.add("done");
      setTimeout(() => {
        btn.textContent = "Copy";
        btn.classList.remove("done");
      }, 1200);
    };
  }

  $("roll").onclick = roll;
  roll();
})();
