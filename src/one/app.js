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
  const GENRE_KEY = "oneofone.genre.v1";
  let genre = "hiphop";
  try {
    const saved = localStorage.getItem(GENRE_KEY);
    if (saved && E.genres().some((g) => g.id === saved)) genre = saved;
  } catch {
    /* default genre */
  }

  function renderGenres() {
    const row = $("genres");
    row.textContent = "";
    row.hidden = E.genres().length < 2;
    if (!E.genres().some((g) => g.id === genre)) genre = E.genres()[0].id;
    for (const g of E.genres()) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "genre";
      b.setAttribute("role", "radio");
      b.setAttribute("aria-checked", String(g.id === genre));
      b.textContent = g.label;
      b.onclick = () => {
        genre = g.id;
        try {
          localStorage.setItem(GENRE_KEY, genre);
        } catch {
          /* fine */
        }
        renderGenres();
        roll();
      };
      row.appendChild(b);
    }
    $("blurb").textContent = E.genres().find((g) => g.id === genre).blurb;
  }

  // Suno collapses similar descriptions into one sound, so the page also keeps the
  // lead family and the bass type from repeating across the last three rolls.
  const recentSounds = [];
  function roll() {
    for (let i = 0; i < 60; i++) {
      pkg = E.generate({ seed: Math.floor(Math.random() * 4294967295), genre, avoidKeys: recentKeys });
      const fam = pkg.parts.lead.family, bass = pkg.parts.bass.source, d = pkg.parts.drums;
      const clash = recentSounds.some((s) => s.fam === fam || s.bass === bass || s.kick === d.kick || s.snare === d.snare || s.hats === d.hats);
      if (!seen.has(pkg.fingerprint) && (!clash || i > 40)) break;
    }
    recentSounds.push({ fam: pkg.parts.lead.family, bass: pkg.parts.bass.source, kick: pkg.parts.drums.kick, snare: pkg.parts.drums.snare, hats: pkg.parts.drums.hats });
    if (recentSounds.length > 3) recentSounds.shift();
    remember(pkg.fingerprint);
    recentKeys.push(pkg.meta.key);
    if (recentKeys.length > 4) recentKeys.shift();
    render();
    resetSequence();
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
    vibe.append(em, ` · ${pkg.meta.lane} · ${pkg.meta.layout || { loop: "hypnotic loop", switch: "beat switch", straight: "locked pocket" }[pkg.meta.mode]} · ${pkg.meta.bpm} BPM · ${pkg.meta.key}`);
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
    chips.append(chip("Model", pkg.settings.model), chip("Variety", pkg.settings.variety), chip("Max Mode", pkg.settings.maxMode), chip("Instrumental", pkg.settings.instrumental));
  }

  async function copyText(text) {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      const ta = document.createElement("textarea");
      ta.value = text;
      ta.setAttribute("readonly", "");
      ta.style.position = "fixed";
      ta.style.opacity = "0";
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
    }
  }
  const flash = (btn, label, idle, ms = 1200) => {
    btn.textContent = label;
    btn.classList.add("done");
    setTimeout(() => {
      btn.textContent = idle;
      btn.classList.remove("done");
    }, ms);
  };

  for (const btn of document.querySelectorAll(".copy[data-copy]")) {
    btn.onclick = async () => {
      await copyText(pkg[btn.dataset.copy]);
      flash(btn, "Copied", "Copy");
    };
  }

  /* ---- Paste sequence ------------------------------------------------------------------------
     Suno's create screen takes Lyrics, then Styles, then Exclude styles, then the sliders. In
     split-screen beside Suno, one tap here copies the next field; the user pastes it over there and
     comes back. The bar always says which field is on the clipboard and which comes next. */
  const STEPS = [
    { key: "lyrics", label: "LYRICS", where: "the Lyrics box" },
    { key: "style", label: "STYLES", where: "the Styles box" },
    { key: "exclude", label: "EXCLUDE STYLES", where: "Exclude styles (under Advanced Options)" },
  ];
  let step = 0; // index of the next field to copy; STEPS.length means all three are on their way
  let pasted = new Set();

  const sliderLine = () => `Weirdness ${pkg.sliders.weirdness}% · Style Influence ${pkg.sliders.styleInfluence}%`;
  const settingsLine = () => `Model ${pkg.settings.model} · Variety ${pkg.settings.variety} · Max Mode ${pkg.settings.maxMode} · Instrumental ${pkg.settings.instrumental}`;

  function block() {
    return [
      "LYRICS", pkg.lyrics, "",
      "STYLES", pkg.style, "",
      "EXCLUDE STYLES", pkg.exclude, "",
      "SLIDERS", sliderLine(), settingsLine(),
    ].join("\n");
  }

  function renderBar() {
    const btn = $("next");
    const kicker = $("next-kicker");
    const label = $("next-label");
    const count = $("next-step");
    for (const sec of document.querySelectorAll(".field[data-field]")) {
      const k = sec.dataset.field;
      sec.classList.toggle("active", step < STEPS.length ? STEPS[step].key === k : k === "sliders");
      sec.classList.toggle("pasted", pasted.has(k));
    }
    if (step < STEPS.length) {
      const s = STEPS[step];
      const prev = step > 0 ? STEPS[step - 1] : null;
      btn.classList.remove("done");
      kicker.textContent = prev ? `${prev.label} is on the clipboard. Paste it into ${prev.where} in Suno, then:` : "Tap, switch to Suno, paste. Repeat for each field.";
      label.textContent = `COPY ${s.label}`;
      count.textContent = `${step + 1} / ${STEPS.length}`;
      btn.setAttribute("aria-label", `Copy ${s.label.toLowerCase()} for Suno, step ${step + 1} of ${STEPS.length}`);
    } else {
      btn.classList.add("done");
      kicker.textContent = `EXCLUDE STYLES is on the clipboard. Paste it, then set the sliders by hand:`;
      label.textContent = sliderLine();
      count.textContent = settingsLine();
      btn.setAttribute("aria-label", "All three fields copied. Set the sliders, then tap to start over.");
    }
  }

  function advance() {
    if (step >= STEPS.length) {
      step = 0;
      pasted = new Set();
      renderBar();
      return;
    }
    const s = STEPS[step];
    copyText(pkg[s.key]);
    pasted.add(s.key);
    step++;
    const btn = $("next");
    btn.classList.add("flash");
    setTimeout(() => btn.classList.remove("flash"), 350);
    renderBar();
  }

  function resetSequence() {
    step = 0;
    pasted = new Set();
    renderBar();
  }

  $("next").onclick = advance;
  $("restart").onclick = resetSequence;
  $("copy-all").onclick = async () => {
    await copyText(block());
    flash($("copy-all"), "Copied the whole block", "Copy all as one block", 1500);
  };

  // Web Share: hands the whole block to any Android app (Keep, a note, or the ONE/ONE filler app in android/).
  if (navigator.share) {
    $("share").hidden = false;
    $("share").onclick = () => navigator.share({ title: `ONE/ONE ${pkg.meta.title}`, text: block() }).catch(() => {});
  }

  // PWA: offline copy + "Install app" when Chrome offers it (otherwise: browser menu -> Add to Home screen).
  if ("serviceWorker" in navigator && location.protocol.startsWith("http")) {
    navigator.serviceWorker.register("one-sw.js").catch(() => {});
  }
  let installEvent = null;
  window.addEventListener("beforeinstallprompt", (e) => {
    e.preventDefault();
    installEvent = e;
    $("install").hidden = false;
  });
  $("install").onclick = async () => {
    if (!installEvent) return;
    installEvent.prompt();
    await installEvent.userChoice.catch(() => {});
    installEvent = null;
    $("install").hidden = true;
  };
  window.addEventListener("appinstalled", () => {
    $("install").hidden = true;
  });

  $("roll").onclick = roll;
  renderGenres();
  roll();
})();
