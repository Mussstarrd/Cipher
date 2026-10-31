/* ReleaseLine single-page UI. Plain JS, no build, no network beyond the local server. */
(function () {
  "use strict";

  // ---- helpers ------------------------------------------------------------------
  const $ = (sel, root) => (root || document).querySelector(sel);
  const view = $("#view");
  let currentScreen = "dashboard";

  function el(tag, attrs, ...children) {
    const node = document.createElement(tag);
    if (attrs) {
      for (const [k, v] of Object.entries(attrs)) {
        if (v === null || v === undefined || v === false) continue;
        if (k === "class") node.className = v;
        else if (k === "text") node.textContent = v;
        else if (k === "html") node.innerHTML = v;
        else if (k.startsWith("on") && typeof v === "function") node.addEventListener(k.slice(2), v);
        else if (v === true) node.setAttribute(k, "");
        else node.setAttribute(k, v);
      }
    }
    for (const c of children.flat(Infinity)) {
      if (c === null || c === undefined || c === false) continue;
      node.appendChild(typeof c === "string" ? document.createTextNode(c) : c);
    }
    return node;
  }

  async function api(method, path, body) {
    let res;
    try {
      res = await fetch(path, {
        method, headers: body !== undefined ? { "Content-Type": "application/json" } : {},
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });
    } catch (e) {
      throw new Error("Could not reach ReleaseLine. Is the server still running in the terminal?");
    }
    let data = {};
    try { data = await res.json(); } catch (e) { data = { error: "The server sent an unreadable reply." }; }
    if (!res.ok) throw new Error(data.error || ("Request failed (" + res.status + ")."));
    return data;
  }

  let toastTimer = null;
  function toast(msg, bad) {
    const t = $("#toast");
    t.textContent = msg; t.className = "toast" + (bad ? " bad" : ""); t.hidden = false;
    clearTimeout(toastTimer); toastTimer = setTimeout(() => { t.hidden = true; }, bad ? 6000 : 2500);
  }

  async function copyText(text, btn) {
    let ok = false;
    try { if (navigator.clipboard && window.isSecureContext) { await navigator.clipboard.writeText(text); ok = true; } } catch (e) { ok = false; }
    if (!ok) {
      const ta = document.createElement("textarea");
      ta.value = text; ta.setAttribute("readonly", ""); ta.style.position = "fixed"; ta.style.left = "-9999px";
      document.body.appendChild(ta); ta.select();
      try { ok = document.execCommand("copy"); } catch (e) { ok = false; }
      document.body.removeChild(ta);
    }
    if (btn) { const old = btn.textContent; btn.textContent = ok ? "Copied" : "Select and copy manually"; setTimeout(() => { btn.textContent = old; }, 1500); }
    if (!ok) toast("Copy did not work in this browser. Select the text and copy it by hand.", true);
  }

  function copyBtn(getText, label) {
    return el("button", { class: "btn btn-small", type: "button", onclick: (e) => copyText(typeof getText === "function" ? getText() : getText, e.currentTarget) }, label || "Copy");
  }

  function relTime(iso) {
    if (!iso) return "";
    const d = new Date(iso); const diff = (Date.now() - d.getTime()) / 1000;
    if (isNaN(diff)) return iso;
    if (diff < 60) return "just now";
    if (diff < 3600) return Math.floor(diff / 60) + " min ago";
    if (diff < 86400) return Math.floor(diff / 3600) + " h ago";
    if (diff < 86400 * 7) return Math.floor(diff / 86400) + " d ago";
    return d.toLocaleDateString();
  }

  function field(label, input, hint) {
    const wrap = el("div", { class: "field" }, el("label", { text: label }), input);
    if (hint) wrap.appendChild(el("div", { class: "hint", text: hint }));
    const err = el("div", { class: "field-error" }); err.hidden = true; wrap.appendChild(err);
    wrap.setError = (msg) => { err.textContent = msg || ""; err.hidden = !msg; input.classList.toggle("invalid", !!msg); };
    return wrap;
  }

  function input(attrs) { return el("input", Object.assign({ type: "text" }, attrs)); }
  function select(options, value, attrs) {
    const s = el("select", attrs || {});
    for (const [v, label] of options) s.appendChild(el("option", { value: v, text: label, selected: String(v) === String(value) || null }));
    return s;
  }

  function errorCard(msg) { return el("div", { class: "card" }, el("p", { class: "error", text: msg })); }
  function busy(btn, on, label) {
    btn.disabled = on;
    if (on) { if (label !== undefined) { btn.dataset.old = btn.textContent; btn.textContent = label; } }
    else if (btn.dataset.old !== undefined) { btn.textContent = btn.dataset.old; delete btn.dataset.old; }
  }

  // ---- ffmpeg pill ---------------------------------------------------------------
  function setPill(info) {
    const pill = $("#ffmpeg-pill");
    if (!info) { pill.className = "pill pill-muted"; pill.textContent = "ffmpeg: unknown"; return; }
    if (info.found) { pill.className = "pill pill-ok"; pill.textContent = "ffmpeg found"; pill.title = info.message || ""; }
    else { pill.className = "pill pill-warn"; pill.textContent = "ffmpeg not found — commands shown instead"; pill.title = info.message || ""; }
  }
  async function refreshStatus() {
    try { const s = await api("GET", "/api/status"); setPill(s.ffmpeg); } catch (e) { setPill(null); }
  }

  // ---- feedback ------------------------------------------------------------------
  function setupFeedback() {
    const modal = $("#feedback-modal"), text = $("#feedback-text"), err = $("#feedback-error");
    const open = () => { modal.hidden = false; err.hidden = true; text.value = ""; setTimeout(() => text.focus(), 30); };
    const close = () => { modal.hidden = true; };
    $("#feedback-open").addEventListener("click", open);
    $("#feedback-cancel").addEventListener("click", close);
    modal.addEventListener("click", (e) => { if (e.target === modal) close(); });
    document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !modal.hidden) close(); });
    $("#feedback-send").addEventListener("click", async () => {
      err.hidden = true;
      if (!text.value.trim()) { err.textContent = "Write a sentence or two first, then save."; err.hidden = false; return; }
      try {
        await api("POST", "/api/feedback", { text: text.value, screen: currentScreen + " " + location.hash });
        close(); toast("Note saved to data/feedback.md.");
      } catch (e) { err.textContent = e.message; err.hidden = false; }
    });
  }

  // ---- routing -------------------------------------------------------------------
  function route() {
    const hash = location.hash || "#/";
    const parts = hash.replace(/^#\/?/, "").split("/").filter(Boolean);
    document.querySelectorAll(".nav a").forEach((a) => a.classList.remove("active"));
    if (parts.length === 0) { currentScreen = "dashboard"; mark("dashboard"); return renderDashboard(); }
    if (parts[0] === "new") { currentScreen = "new"; mark("new"); return renderNew(); }
    if (parts[0] === "settings") { currentScreen = "settings"; mark("settings"); return renderSettings(); }
    if (parts[0] === "track" && parts[1]) { currentScreen = "track"; return renderTrack(parts[1]); }
    view.replaceChildren(errorCard("That page does not exist."), el("p", {}, el("a", { href: "#/", text: "Back to the Dashboard" })));
  }
  function mark(name) { const a = $(`.nav a[data-nav="${name}"]`); if (a) a.classList.add("active"); }

  // ---- dashboard -----------------------------------------------------------------
  async function renderDashboard() {
    view.replaceChildren(el("p", { class: "muted", text: "Loading…" }));
    let data;
    try { data = await api("GET", "/api/dashboard"); } catch (e) { view.replaceChildren(el("h1", { text: "Dashboard" }), errorCard(e.message)); return; }
    const c = data.counts;
    const head = el("div", { class: "row between gap" },
      el("div", {}, el("h1", { text: "Dashboard" }),
        el("p", { class: "muted", text: `${c.active} in progress · ${c.released} released` + (c.archived ? ` · ${c.archived} archived` : "") })),
      el("a", { class: "btn btn-primary", href: "#/new", text: "New track" }));
    const nodes = [head];
    if (data.notice) nodes.push(el("div", { class: "card notice" }, el("p", { text: data.notice }), el("p", { class: "muted small", text: "Nothing was deleted from disk. Create a track with the same title to get a matching folder, or move the old folders out of library/." })));
    if (c.total === 0) {
      nodes.push(el("div", { class: "card empty" }, el("p", { text: "No tracks yet. Add the one you are working on." }), el("a", { class: "btn btn-primary", href: "#/new", text: "New track" })));
    }
    for (const g of data.groups) {
      if (!g.tracks.length) continue;
      nodes.push(el("h2", { text: g.title + " (" + g.tracks.length + ")" }));
      nodes.push(el("div", { class: "grid" }, g.tracks.map(trackCard)));
    }
    if (data.released.length) {
      nodes.push(el("h2", { text: "Released (" + data.released.length + ")" }));
      nodes.push(el("div", { class: "card", style: "overflow-x:auto" }, el("table", {},
        el("thead", {}, el("tr", {}, el("th", { text: "Track" }), el("th", { text: "Posts" }), el("th", { text: "Last touched" }))),
        el("tbody", {}, data.released.map((t) => el("tr", {},
          el("td", {}, el("a", { href: "#/track/" + t.id, text: t.title }), t.genre ? el("div", { class: "muted small", text: t.genre }) : null),
          el("td", {}, t.posts && t.posts.length ? t.posts.map((p, i) => [i ? " · " : null, el("a", { href: p.url, target: "_blank", rel: "noopener", text: p.platform })]) : el("span", { class: "muted", text: "no links recorded" })),
          el("td", { class: "muted small", text: relTime(t.updated_at) })))))));
    }
    if (data.archived.length) {
      nodes.push(el("h2", { text: "Archived (" + data.archived.length + ")" }));
      nodes.push(el("div", { class: "grid" }, data.archived.map(trackCard)));
    }
    view.replaceChildren(...nodes);
  }

  function trackCard(t) {
    return el("a", { class: "card track-card", href: "#/track/" + t.id },
      el("h3", { text: t.title }),
      el("div", { class: "muted small", text: [t.artist, t.genre].filter(Boolean).join(" · ") || "no genre yet" }),
      el("p", { class: "small", text: t.next_action }),
      el("div", { class: "row between" }, el("span", { class: "muted small", text: `${t.done_count}/8 stages` }), el("span", { class: "muted small", text: "last touched " + relTime(t.updated_at) })));
  }

  // ---- new track -----------------------------------------------------------------
  function trackForm(values, onSubmit, submitLabel) {
    values = values || {};
    const f = {
      title: input({ value: values.title || "", placeholder: "Required", maxlength: 200 }),
      artist: input({ value: values.artist || "", placeholder: "Default from Settings" }),
      genre: input({ value: values.genre || "", placeholder: "e.g. dark trap, lo-fi, synthwave" }),
      mood: input({ value: values.mood || "", placeholder: "e.g. menacing, nostalgic, dreamy" }),
      energy: select([["low", "low"], ["mid", "mid"], ["high", "high"]], values.energy || "mid"),
      bpm: input({ value: values.bpm || "", placeholder: "e.g. 140", inputmode: "numeric" }),
      key: input({ value: values.key || "", placeholder: "e.g. F minor" }),
      hook: el("textarea", { rows: 2, placeholder: "The line people will remember", text: values.hook || "" }),
      suno_url: input({ value: values.suno_url || "", placeholder: "https://suno.com/song/…", inputmode: "url", spellcheck: "false" }),
      notes: el("textarea", { rows: 3, text: values.notes || "" }),
    };
    const fields = {
      title: field("Title", f.title, "The only required field. The library folder name comes from it."),
      artist: field("Artist", f.artist), genre: field("Genre / style tags", f.genre, "Drives the prompt vocabulary and the 'type beat' title rule."),
      mood: field("Mood words", f.mood), energy: field("Energy", f.energy), bpm: field("BPM (optional)", f.bpm),
      key: field("Key (optional)", f.key), hook: field("Hook line or lyric excerpt", f.hook),
      suno_url: field("Suno link (optional)", f.suno_url), notes: field("Notes", f.notes),
    };
    fields.hook.classList.add("full"); fields.notes.classList.add("full");
    const err = el("p", { class: "error" }); err.hidden = true;
    const submit = el("button", { class: "btn btn-primary", type: "submit", text: submitLabel || "Create track" });
    const form = el("form", { class: "card", onsubmit: async (e) => {
      e.preventDefault(); err.hidden = true;
      Object.values(fields).forEach((x) => x.setError(""));
      let bad = false;
      if (!f.title.value.trim()) { fields.title.setError("Give the track a title."); bad = true; }
      if (f.bpm.value.trim() && !/^\d{2,3}(\.\d+)?$/.test(f.bpm.value.trim())) { fields.bpm.setError("BPM should be a number like 140, or blank."); bad = true; }
      if (f.suno_url.value.trim() && !/^https?:\/\//.test(f.suno_url.value.trim())) { fields.suno_url.setError("Links start with http:// or https://."); bad = true; }
      if (bad) return;
      const body = {}; for (const k of Object.keys(f)) body[k] = f[k].value;
      busy(submit, true, "Saving…");
      try { await onSubmit(body); } catch (e2) { err.textContent = e2.message; err.hidden = false; }
      busy(submit, false);
    } },
      el("div", { class: "grid-form" }, Object.values(fields)), err, el("div", { class: "stage-actions" }, submit));
    return form;
  }

  function renderNew() {
    view.replaceChildren(el("h1", { text: "New track" }), el("p", { class: "muted", text: "Only the title is required. The rest feeds the prompt and tag generators and can be edited later." }),
      trackForm({}, async (body) => { const t = await api("POST", "/api/tracks", body); toast("Track created. Library folder: " + t.slug + "/"); location.hash = "#/track/" + t.id; }));
  }

  // ---- track page ----------------------------------------------------------------
  const STAGE_OPEN = {}; // track id -> stage key currently expanded

  async function renderTrack(ident) {
    view.replaceChildren(el("p", { class: "muted", text: "Loading…" }));
    let d;
    try { d = await api("GET", "/api/tracks/" + encodeURIComponent(ident)); } catch (e) { view.replaceChildren(el("h1", { text: "Track" }), errorCard(e.message), el("p", {}, el("a", { href: "#/", text: "Back to the Dashboard" }))); return; }
    setPill(d.ffmpeg);
    const t = d.track;
    const reload = () => renderTrack(ident);
    const open = STAGE_OPEN[t.id] || d.current_stage || "upload";

    const header = el("div", { class: "card" },
      el("div", { class: "row between gap" },
        el("div", {}, el("h1", { text: t.title }),
          el("div", { class: "muted" }, [t.artist, t.genre, t.mood, t.energy ? t.energy + " energy" : "", t.bpm ? t.bpm + " BPM" : "", t.key].filter(Boolean).join(" · ") || "No details yet.")),
        el("span", { class: "badge " + (t.status === "released" ? "released" : ""), text: t.status })),
      t.hook ? el("p", { class: "small" }, el("span", { class: "muted", text: "Hook: " }), "“" + t.hook + "”") : null,
      t.suno_url ? el("p", { class: "small" }, el("a", { href: t.suno_url, target: "_blank", rel: "noopener", text: "Open on Suno" })) : null,
      el("div", { class: "stage-actions" },
        el("button", { class: "btn btn-small", type: "button", onclick: (e) => { const box = $("#edit-box"); box.hidden = !box.hidden; } }, "Edit details"),
        t.status !== "released" ? el("button", { class: "btn btn-small", type: "button", onclick: async () => {
          if (!confirm("Mark this track as released, even though not every platform has a URL?")) return;
          try { await api("POST", `/api/tracks/${t.id}/release`); toast("Released."); reload(); } catch (e) { toast(e.message, true); }
        } }, "Release anyway") : null,
        t.status === "released" ? el("button", { class: "btn btn-small", type: "button", onclick: async () => { try { await api("PATCH", `/api/tracks/${t.id}`, { status: "active" }); reload(); } catch (e) { toast(e.message, true); } } }, "Reopen") : null),
      (() => { const box = el("div", { id: "edit-box" }); box.hidden = true;
        box.appendChild(trackForm(t, async (body) => { await api("PATCH", `/api/tracks/${t.id}`, body); toast("Saved."); reload(); }, "Save details")); return box; })());

    const rail = el("div", { class: "rail" }, d.stages.map((s, i) => el("button", {
      type: "button", class: s.key === open ? "current" : "", title: s.title,
      onclick: () => { STAGE_OPEN[t.id] = s.key; reload(); } },
      el("span", { class: "tick " + (s.state === "done" ? "done" : s.state === "skipped" ? "skipped" : s.key === d.current_stage ? "current" : ""), text: s.state === "done" ? "✓" : s.state === "skipped" ? "–" : String(i + 1) }),
      el("span", { text: s.title }))));

    const stages = d.stages.map((s, i) => stageBlock(d, s, i, s.key === open, reload));

    const notes = el("div", { class: "card" }, el("h2", { text: "Notes" }),
      (() => { const ta = el("textarea", { rows: 3, text: t.notes || "", placeholder: "Anything about this track." });
        const save = el("button", { class: "btn btn-small", type: "button", onclick: async () => { try { await api("PATCH", `/api/tracks/${t.id}`, { notes: ta.value }); toast("Notes saved."); } catch (e) { toast(e.message, true); } } }, "Save notes");
        return el("div", {}, ta, el("div", { class: "stage-actions" }, save)); })());

    const a = d.assets;
    const assets = el("div", { class: "card" }, el("h2", { text: "Assets" }),
      el("p", { class: "small" }, el("span", { class: "muted", text: "Library folder: " }), el("code", { text: a.root }), a.root_exists ? null : el("span", { class: "missing", text: " (folder missing)" })),
      el("div", { class: "grid" }, Object.entries(a.on_disk).map(([folder, files]) => el("div", {}, el("h3", { text: folder + "/" }),
        files.length ? el("ul", { class: "files" }, files.map((f) => el("li", { text: f }))) : el("p", { class: "muted small", text: "empty" })))),
      a.recorded.length ? el("div", {}, el("h3", { text: "Recorded paths" }), el("ul", { class: "files" }, a.recorded.map((r) => el("li", { class: r.exists ? "" : "missing" }, el("span", { class: "muted", text: r.kind + ": " }), r.path, r.exists ? "" : " — file not found")))) : null);

    view.replaceChildren(el("p", { class: "small" }, el("a", { href: "#/", text: "← Dashboard" })), header, rail, ...stages, notes, assets, draftsCard(d, reload));
  }

  const META_KINDS = ["title", "description", "hashtags", "tags"];

  // The Drafts card shows the chosen prompt and shot list. Titles, descriptions and tags
  // are edited in stage 7 only, so there is one copy of each box on the page.
  function draftsCard(d, reload) {
    const promptDrafts = d.drafts.filter((x) => !META_KINDS.includes(x.kind));
    const metaCount = d.drafts.length - promptDrafts.length;
    const openStage = (key) => el("a", { href: "#/track/" + d.track.id, onclick: (e) => { e.preventDefault(); STAGE_OPEN[d.track.id] = key; reload(); }, text: key === "meta" ? "stage 7" : "stage 4" });
    return el("div", { class: "card", id: "drafts-card" }, el("h2", { text: "Drafts" }),
      promptDrafts.length ? draftList(d, promptDrafts, reload) : el("p", { class: "muted" }, "No prompt chosen yet. Generate variants in ", openStage("prompt"), " and press Use this."),
      metaCount ? el("p", { class: "muted small" }, `Titles, descriptions, hashtags and tags (${metaCount} boxes) are edited in `, openStage("meta"), ".") : null);
  }

  function refreshDrafts(d, drafts, reload) {
    d.drafts = drafts;
    const old = $("#drafts-card");
    if (old) old.replaceWith(draftsCard(d, reload));
  }

  function parseDraft(dr) {
    try { const c = JSON.parse(dr.content); if (c && typeof c === "object") return Object.assign({ label: dr.kind, text: "" }, c); } catch (e) { /* plain text */ }
    return { label: dr.kind, text: dr.content };
  }

  function draftBox(d, dr, reload, extra) {
    const c = parseDraft(dr);
    const ta = el("textarea", { rows: Math.min(10, Math.max(2, (c.text || "").split("\n").length + 1)), text: c.text });
    let saved = c.text;
    ta.addEventListener("blur", async () => {
      if (ta.value === saved) return;
      try { await api("PATCH", `/api/tracks/${d.track.id}/drafts/${dr.id}`, { text: ta.value }); saved = ta.value; toast("Edit saved."); } catch (e) { toast(e.message, true); }
    });
    return el("div", { class: "draft" + (dr.chosen ? " chosen" : "") },
      el("div", { class: "draft-head" }, el("strong", { text: c.label }), dr.chosen ? el("span", { class: "badge done", text: "chosen" }) : null, copyBtn(() => ta.value), extra || null),
      ta, c.negative ? el("p", { class: "small" }, el("span", { class: "muted", text: "Negative: " }), c.negative, " ", copyBtn(c.negative)) : null);
  }

  function draftList(d, list, reload) {
    return el("div", {}, list.map((dr) => draftBox(d, dr, reload)));
  }

  // ---- stage blocks --------------------------------------------------------------
  function checklist(d, s, items) {
    const checks = (s.data && s.data.checks) || {};
    return el("ul", { class: "checklist" }, items.map((label, i) => {
      const cb = el("input", { type: "checkbox", checked: checks[i] ? true : null });
      const li = el("li", { class: checks[i] ? "checked" : "" }, cb, el("span", { text: label }));
      cb.addEventListener("change", async () => {
        const next = Object.assign({}, checks, { [i]: cb.checked });
        li.classList.toggle("checked", cb.checked);
        try { await api("POST", `/api/tracks/${d.track.id}/stages/${s.key}`, { data: { checks: next } }); s.data.checks = next; } catch (e) { toast(e.message, true); }
      });
      return li;
    }));
  }

  function pathField(d, s, key, label, hint, reload) {
    const inp = input({ value: (s.data && s.data[key]) || "", placeholder: "Full path to the file", spellcheck: "false" });
    const fld = field(label, inp, hint);
    const save = el("button", { class: "btn btn-small", type: "button", onclick: async () => {
      fld.setError("");
      const had = !!((s.data && s.data[key]) || "");
      if (!inp.value.trim() && !had) { fld.setError("Enter a path first."); return; }
      try { await api("POST", `/api/tracks/${d.track.id}/stages/${s.key}`, { data: { [key]: inp.value } }); toast(inp.value.trim() ? "Path saved." : "Path cleared."); reload(); } catch (e) { fld.setError(e.message); }
    } }, "Save path");
    return el("div", {}, fld, save);
  }

  function stageActions(d, s, reload) {
    const act = async (state) => { try { await api("POST", `/api/tracks/${d.track.id}/stages/${s.key}`, { state }); STAGE_OPEN[d.track.id] = null; reload(); } catch (e) { toast(e.message, true); } };
    const items = [];
    if (s.state === "todo") {
      items.push(el("button", { class: "btn btn-primary", type: "button", onclick: () => act("done") }, "Mark done"));
      items.push(el("button", { class: "btn", type: "button", onclick: () => act("skipped") }, "Skip"));
    } else {
      items.push(el("span", { class: "badge " + s.state, text: (s.state === "done" ? "Done" : "Skipped") + (s.done_at ? " · " + relTime(s.done_at) : "") }));
      items.push(el("button", { class: "btn btn-small", type: "button", onclick: () => act("todo") }, "Reopen"));
    }
    return el("div", { class: "stage-actions" }, items);
  }

  function stageBlock(d, s, i, isOpen, reload) {
    const head = el("button", { class: "stage-head", type: "button", onclick: () => { STAGE_OPEN[d.track.id] = isOpen ? "__none__" : s.key; reload(); } },
      el("span", { class: "tick " + (s.state === "done" ? "done" : s.state === "skipped" ? "skipped" : s.key === d.current_stage ? "current" : ""), text: s.state === "done" ? "✓" : s.state === "skipped" ? "–" : String(i + 1) }),
      el("h2", { text: `${i + 1}. ${s.title}` }),
      s.key === d.current_stage && s.state === "todo" ? el("span", { class: "badge", text: "current" }) : null,
      el("span", { class: "muted small", text: isOpen ? "▲" : "▼" }));
    const card = el("div", { class: "card stage" }, head);
    if (isOpen) {
      const body = el("div", { class: "stage-body" });
      const builder = STAGE_BUILDERS[s.key];
      body.appendChild(builder(d, s, reload));
      body.appendChild(stageActions(d, s, reload));
      card.appendChild(body);
    }
    return card;
  }

  function cmdBlock(commands, note) {
    return el("div", {}, note ? el("p", { class: "small muted", text: note }) : null,
      commands.map((c) => el("div", {}, el("pre", { text: c }), copyBtn(c, "Copy command"))));
  }

  const STAGE_BUILDERS = {
    download(d, s, reload) {
      const root = d.assets.root;
      return el("div", {},
        el("p", { text: "Get the finished track out of Suno and into this track's folder." }),
        checklist(d, s, [
          "Open the song in Suno and choose Download.",
          "Pick WAV if it is offered; otherwise the highest-quality MP3.",
          `Save it into ${root}/source/`,
          "Paste the full path to the file below and press Save path.",
        ]),
        d.track.suno_url ? el("p", { class: "small" }, el("a", { href: d.track.suno_url, target: "_blank", rel: "noopener", text: "Open the Suno link" })) : null,
        pathField(d, s, "path", "Downloaded file path", "The server checks that the file exists.", reload));
    },
    master(d, s, reload) {
      const notes = el("textarea", { rows: 2, text: (s.data && s.data.notes) || "", placeholder: "Settings you used, anything to remember next time." });
      const saveNotes = el("button", { class: "btn btn-small", type: "button", onclick: async () => { try { await api("POST", `/api/tracks/${d.track.id}/stages/master`, { data: { notes: notes.value } }); toast("Notes saved."); } catch (e) { toast(e.message, true); } } }, "Save notes");
      return el("div", {},
        el("p", { text: "Master in FL Studio so the track sits at a sensible level and translates to phone speakers." }),
        checklist(d, s, [
          "Import the download into a fresh FL Studio project.",
          "Trim silence at the start and end.",
          "EQ: clean up low rumble and any harsh band.",
          "Compressor: gentle glue, a few dB of gain reduction at most.",
          "Limiter on the master. Around -14 LUFS integrated is a commonly used streaming target, not a rule; louder is fine for beats if it still sounds clean.",
          `Export WAV 24-bit / 44.1 kHz and MP3 320 kbps into ${d.assets.root}/master/`,
        ]),
        pathField(d, s, "path", "Mastered file path (the WAV)", "This file is what the clip is cut from.", reload),
        field("Mastering notes", notes), saveNotes);
    },
    clip(d, s, reload) {
      const data = s.data || {}; const def = d.defaults;
      const start = input({ value: data.start || "", placeholder: "m:ss, e.g. 0:45", inputmode: "numeric" });
      const len = input({ value: data.length != null ? data.length : def.clip_len, inputmode: "decimal" });
      const fade = input({ value: data.fade != null ? data.fade : def.fade, inputmode: "decimal" });
      const fStart = field("Start time", start, "Tip: start on the hook. The first second decides if people stay."), fLen = field("Length (seconds)", len), fFade = field("Fade in/out (seconds)", fade);
      const out = el("div", {});
      const run = el("button", { class: "btn btn-primary", type: "button", onclick: async () => {
        fStart.setError(""); out.replaceChildren(); busy(run, true, d.ffmpeg.found ? "Cutting…" : "Building…");
        try {
          const r = await api("POST", `/api/tracks/${d.track.id}/cut`, { start: start.value, length: len.value, fade: fade.value });
          if (r.ran) { toast(r.message); STAGE_OPEN[d.track.id] = null; reload(); return; }
          out.replaceChildren(cmdBlock(r.commands, "Run this in a terminal once ffmpeg is installed; it writes the WAV and the MP3."));
        } catch (e) { fStart.setError(e.message); }
        busy(run, false);
      } }, d.ffmpeg.found ? "Cut clip" : "Show ffmpeg command");
      return el("div", {},
        el("p", { text: "Cut the part of the track the video will run under. It is saved as WAV and MP3 in clip/." }),
        el("div", { class: "grid-form" }, fStart, fLen, fFade),
        el("div", { class: "stage-actions" }, run),
        d.ffmpeg.found ? null : el("p", { class: "small muted", text: d.ffmpeg.message }),
        data.wav ? el("p", { class: "small ok" }, "Clip: ", el("code", { text: data.wav }), " ", copyBtn(data.wav, "Copy path")) : null,
        out);
    },
    prompt(d, s, reload) {
      const data = s.data || {}; const def = d.defaults;
      const preset = select([["cinematic", "Cinematic"], ["anime", "Anime"], ["vhs", "VHS / lo-fi"], ["3d", "3D render"], ["documentary", "Street documentary"], ["abstract", "Abstract / visualizer"], ["surreal", "Surreal"]], data.preset || "cinematic");
      const setting = input({ value: data.setting || "", placeholder: "Leave blank to let the genre pick, or e.g. 'rainy rooftop in Tokyo'" });
      const aspect = select([["9:16", "9:16 (vertical)"], ["16:9", "16:9"], ["1:1", "1:1"]], data.aspect || def.aspect);
      const shotLen = select([["5", "5 s"], ["10", "10 s"]], String(data.shot_len || def.shot_len));
      const clipLen = (d.stages.find((x) => x.key === "clip").data || {}).length || def.clip_len;
      const shots = input({ value: data.shots || Math.ceil(clipLen / (data.shot_len || def.shot_len)), inputmode: "numeric" });
      const n = select([["3", "3"], ["4", "4"], ["5", "5"]], "4");
      shotLen.addEventListener("change", () => { shots.value = Math.ceil(clipLen / Number(shotLen.value)); });
      const seedInfo = el("span", { class: "muted small" });
      const out = el("div", {});
      let lastSeed = data.seed || null;
      const chosenNow = el("p", { class: "small muted" });
      if (data.chosen_label && d.drafts.some((x) => x.kind === "prompt" && x.chosen)) chosenNow.textContent = `Chosen: ${data.chosen_label} (saved in Drafts below). Press Generate (same seed) to see the variants again.`;
      else chosenNow.hidden = true;
      const gen = async (seed) => {
        out.replaceChildren(el("p", { class: "muted", text: "Generating…" }));
        try {
          const r = await api("POST", `/api/tracks/${d.track.id}/prompts`, { preset: preset.value, setting: setting.value, aspect: aspect.value, shot_len: Number(shotLen.value), clip_len: clipLen, shots: shots.value, n: Number(n.value), seed });
          lastSeed = r.seed; seedInfo.textContent = `seed ${r.seed} · ${r.genre_family} vocabulary · ${r.mood_category} mood · ${r.shots} shots × ${r.shot_len} s`;
          chosenNow.hidden = true;
          const chosenLabel = (r.seed === data.seed && data.chosen_label) || null;
          const onChosen = (label, drafts) => {
            data.chosen_label = label; data.seed = r.seed;
            out.querySelectorAll(".variant").forEach((node) => node.markChosen(node.dataset.label === label));
            refreshDrafts(d, drafts, reload);
          };
          out.replaceChildren(...r.variants.map((v, i) => variantBox(d, v, i, r.seed, onChosen, v.label === chosenLabel)));
        } catch (e) { out.replaceChildren(el("p", { class: "error", text: e.message })); }
      };
      return el("div", {},
        el("p", { text: "Draft Kling prompts from this track's genre, mood and energy. Templates only: edit anything that reads flat, then paste into Kling." }),
        el("div", { class: "grid-form" }, field("Style preset", preset), field("Setting (optional)", setting), field("Aspect ratio", aspect), field("Kling clip length", shotLen), field("Number of shots", shots, "Default = clip length ÷ shot length, rounded up."), field("Variants", n)),
        el("div", { class: "stage-actions" },
          el("button", { class: "btn btn-primary", type: "button", onclick: () => gen(lastSeed) }, lastSeed ? "Generate (same seed)" : "Generate"),
          el("button", { class: "btn", type: "button", onclick: () => gen(null) }, "Shuffle"), seedInfo),
        chosenNow, out);
    },
    video(d, s, reload) {
      const files = d.assets.on_disk.video || [];
      const ta = el("textarea", { rows: 4, text: ((s.data && s.data.shots) || []).join("\n"), placeholder: "One full path per line, in order.", spellcheck: "false" });
      const fld = field("Shot paths (one per line, in playback order)", ta, "Optional if the files are already in video/: they are picked up in name order.");
      const save = el("button", { class: "btn btn-small", type: "button", onclick: async () => { fld.setError("");
        const list = ta.value.split("\n").map((x) => x.trim()).filter(Boolean);
        const had = ((s.data && s.data.shots) || []).length > 0;
        if (!list.length && !had) { fld.setError("Enter at least one path first, or leave this empty and use the files in video/."); return; }
        try { await api("POST", `/api/tracks/${d.track.id}/stages/video`, { data: { shots: list } }); toast(list.length ? "Paths saved." : "Paths cleared."); reload(); } catch (e) { fld.setError(e.message); } } }, "Save paths");
      return el("div", {},
        el("p", { text: "Generate each shot in Kling with the chosen prompts, then download them here." }),
        checklist(d, s, [
          "Open Kling and paste shot 1's prompt (and its negative prompt, if there is a field).",
          "Set the aspect ratio and duration to match the prompt.",
          "Generate; re-run if the result drifts from the look.",
          `Download and save as shot-01.mp4, shot-02.mp4 … into ${d.assets.root}/video/`,
          "Repeat for every shot in the list.",
        ]),
        el("h3", { text: "Found in video/" }),
        files.length ? el("ul", { class: "files" }, files.map((f) => el("li", { text: f }))) : el("p", { class: "muted small", text: "Nothing yet. Files saved there show up on reload." }),
        fld, save);
    },
    assemble(d, s, reload) {
      const data = s.data || {};
      const out = el("div", {});
      const run = el("button", { class: "btn btn-primary", type: "button", onclick: async () => {
        out.replaceChildren(el("p", { class: "muted", text: d.ffmpeg.found ? "Assembling… this can take a minute." : "Building commands…" })); busy(run, true);
        try {
          const r = await api("POST", `/api/tracks/${d.track.id}/assemble`, {});
          if (r.ran) { toast(r.message); STAGE_OPEN[d.track.id] = null; reload(); return; }
          out.replaceChildren(
            el("p", { class: "small" }, "1. Save this as ", el("code", { text: r.listfile }), ":"), el("pre", { text: r.listfile_text }), copyBtn(r.listfile_text, "Copy list"),
            el("p", { class: "small", text: "2. Run these in order:" }), cmdBlock(r.commands));
        } catch (e) { out.replaceChildren(el("p", { class: "error", text: e.message })); }
        busy(run, false);
      } }, d.ffmpeg.found ? "Assemble" : "Show ffmpeg commands");
      return el("div", {},
        el("p", { text: "Normalize every shot to the chosen aspect (1080×1920 for 9:16, 1920×1080 for 16:9, 1080×1080 for 1:1; 30 fps; H.264), join them in order, loop or trim to the audio clip, add the clip as AAC 192k, and write final/<slug>-final.mp4." }),
        el("div", { class: "stage-actions" }, run),
        d.ffmpeg.found ? null : el("div", {}, el("p", { class: "small muted", text: d.ffmpeg.message }),
          el("h3", { text: "Or by hand in CapCut / DaVinci Resolve" }),
          checklist(d, s, ["New project at the chosen aspect, 30 fps.", "Import the shots and the audio clip from clip/.", "Place shots in order; trim or repeat so the picture covers the audio.", "Scale each shot to fill the frame (no stretching).", "Export MP4 (H.264, AAC) into final/ and record the path below."])),
        data.final ? el("p", { class: "small ok" }, "Final: ", el("code", { text: data.final }), " ", copyBtn(data.final, "Copy path")) : null,
        out,
        pathField(d, s, "final", "Final video path", "Filled automatically after Assemble; set it yourself if you edited by hand.", reload));
    },
    meta(d, s, reload) {
      const explicit = el("input", { type: "checkbox", checked: s.data && s.data.explicit ? true : null });
      const out = el("div", {});
      const kinds = ["title", "description", "hashtags", "tags"];
      const mine = d.drafts.filter((x) => kinds.includes(x.kind));
      const gen = el("button", { class: "btn btn-primary", type: "button", onclick: async () => { busy(gen, true, "Generating…");
        try { await api("POST", `/api/tracks/${d.track.id}/meta`, { explicit: explicit.checked }); toast("Drafts generated."); reload(); } catch (e) { out.replaceChildren(el("p", { class: "error", text: e.message })); busy(gen, false); } } },
        mine.length ? "Regenerate (keeps nothing you edited)" : "Generate");
      return el("div", {},
        el("p", { text: "Titles, descriptions, hashtags and a YouTube tag list, built from this track's fields. Edit in place; edits are saved when you leave the box." }),
        el("div", { class: "row gap" }, el("label", { class: "row gap", style: "margin:0" }, explicit, " Explicit lyrics"), gen), out,
        mine.length ? el("div", {}, kinds.map((k) => { const list = mine.filter((x) => x.kind === k); return list.length ? el("div", {}, el("h3", { text: { title: "Titles", description: "Descriptions", hashtags: "Hashtags", tags: "YouTube tags and distributor note" }[k] }), draftList(d, list, reload)) : null; })) : el("p", { class: "muted small", text: "Nothing generated yet." }));
    },
    upload(d, s, reload) {
      const posts = {}; for (const p of d.posts) posts[p.platform_id] = p;
      if (!d.platforms.length) return el("div", {}, el("p", { class: "muted", text: "No platforms are enabled. Turn some on in Settings." }));
      const rows = d.platforms.map((p) => {
        const inp = input({ value: (posts[p.id] && posts[p.id].url) || "", placeholder: "https://… after posting", type: "url" });
        const err = el("div", { class: "field-error" }); err.hidden = true;
        const save = el("button", { class: "btn btn-small", type: "button", onclick: async () => { err.hidden = true;
          const had = !!(posts[p.id] && posts[p.id].url);
          if (!inp.value.trim() && !had) { err.textContent = "Paste the posted URL first."; err.hidden = false; return; }
          try { const r = await api("POST", `/api/tracks/${d.track.id}/posts`, { platform_id: p.id, url: inp.value }); toast(r.released ? "All platforms have a link. Released." : inp.value.trim() ? "Link saved." : "Link cleared."); reload(); } catch (e) { err.textContent = e.message; err.hidden = false; } } }, "Save");
        return el("tr", {},
          el("td", {}, el("strong", { text: p.name }), el("br"), p.upload_url ? el("a", { href: p.upload_url, target: "_blank", rel: "noopener", class: "small", text: "Open upload page" }) : el("span", { class: "muted small", text: "No upload URL set (Settings)" })),
          el("td", { class: "small muted", text: p.notes || "" }),
          el("td", {}, el("div", { class: "row gap url-row" }, inp, save), err));
      });
      return el("div", {},
        el("p", { text: "One row per enabled platform. Paste the public URL after posting; the track becomes Released when every row has one." }),
        el("div", { style: "overflow-x:auto" }, el("table", { class: "upload-table" }, el("thead", {}, el("tr", {}, el("th", { text: "Platform" }), el("th", { text: "Format reminders" }), el("th", { text: "Posted URL" }))), el("tbody", {}, rows))),
        d.track.status !== "released" ? el("p", { class: "small muted", text: "Not every platform applies to every track: use Release anyway (top of the page) to finish without filling all rows." }) : null);
    },
  };

  function variantBox(d, v, i, seed, onChosen, isChosen) {
    const shots = el("details", {}, el("summary", { text: `Shot list (${v.shots.length} shots × ${v.duration} s)` }),
      v.shots.map((sh) => el("div", { class: "draft" }, el("div", { class: "draft-head" }, el("strong", { text: `Shot ${sh.n} — ${sh.moment}` }), copyBtn(sh.prompt)), el("p", { class: "small", text: sh.prompt }))),
      copyBtn(() => v.shots.map((sh) => `Shot ${sh.n}: ${sh.prompt}`).join("\n\n"), "Copy all shots"));
    const badge = el("span", { class: "badge done", text: "chosen" });
    const use = el("button", { class: "btn btn-primary btn-small", type: "button", onclick: async () => { busy(use, true, "Saving…");
      let r = null;
      try { r = await api("POST", `/api/tracks/${d.track.id}/prompts/choose`, { variant: v, seed }); } catch (e) { toast(e.message, true); }
      busy(use, false);  // restore the label first, then mark, so "Chosen" is not overwritten
      if (r) { toast("Prompt and shot list saved to the track."); onChosen(v.label, r.drafts); } } }, "Use this");
    const box = el("div", { class: "draft variant", "data-label": v.label },
      el("div", { class: "draft-head" }, el("strong", { text: `Variant ${i + 1}: ${v.label}` }), badge, copyBtn(v.prompt), use),
      el("p", { text: v.prompt }),
      el("dl", { class: "kv" }, el("dt", { text: "Scene" }), el("dd", { text: v.scene }), el("dt", { text: "Camera" }), el("dd", { text: v.camera }), el("dt", { text: "Lighting" }), el("dd", { text: v.lighting }),
        el("dt", { text: "Style" }), el("dd", { text: v.style }), el("dt", { text: "Mood" }), el("dd", { text: v.mood_words.join(", ") }), el("dt", { text: "Negative" }), el("dd", {}, v.negative, " ", copyBtn(v.negative))),
      shots);
    box.markChosen = (on) => { box.classList.toggle("chosen", on); badge.hidden = !on; use.textContent = on ? "Chosen" : "Use this"; };
    box.markChosen(!!isChosen);
    return box;
  }

  // ---- settings ------------------------------------------------------------------
  async function renderSettings() {
    view.replaceChildren(el("p", { class: "muted", text: "Loading…" }));
    let s, plats;
    try { s = await api("GET", "/api/settings"); plats = (await api("GET", "/api/platforms")).platforms; } catch (e) { view.replaceChildren(el("h1", { text: "Settings" }), errorCard(e.message)); return; }
    setPill(s.ffmpeg);
    const v = s.settings;
    const f = {
      artist: input({ value: v.artist }), library_root: input({ value: v.library_root, placeholder: s.library_root_resolved, spellcheck: "false" }),
      ffmpeg_path: input({ value: v.ffmpeg_path, placeholder: s.ffmpeg.found && s.ffmpeg.path ? s.ffmpeg.path + " (auto-detected)" : "e.g. C:\\ffmpeg\\bin\\ffmpeg.exe", spellcheck: "false" }),
      aspect: select([["9:16", "9:16 (vertical)"], ["16:9", "16:9"], ["1:1", "1:1"]], v.aspect), shot_len: select([["5", "5 s"], ["10", "10 s"]], String(v.shot_len)),
      clip_len: input({ value: v.clip_len, inputmode: "decimal" }), fade: input({ value: v.fade, inputmode: "decimal" }),
    };
    const ffStatus = el("p", { class: "small " + (s.ffmpeg.found ? "ok" : "muted"), text: s.ffmpeg.found ? s.ffmpeg.message + (s.ffmpeg.version ? " (" + s.ffmpeg.version.split(" Copyright")[0] + ")" : "") : s.ffmpeg.message });
    const test = el("button", { class: "btn btn-small", type: "button", onclick: async () => { busy(test, true, "Testing…");
      try { const r = await api("POST", "/api/ffmpeg/test", { path: f.ffmpeg_path.value }); ffStatus.textContent = r.found ? r.message + (r.version ? " (" + r.version.split(" Copyright")[0] + ")" : "") : r.message; ffStatus.className = "small " + (r.found ? "ok" : "error"); } catch (e) { ffStatus.textContent = e.message; ffStatus.className = "small error"; }
      busy(test, false); } }, "Test");
    const fields = {
      artist: field("Artist name (default for new tracks)", f.artist),
      library_root: field("Library root", f.library_root, "Where track folders are created. Blank = " + s.library_root_resolved),
      ffmpeg_path: field("ffmpeg path", f.ffmpeg_path, "Blank = auto-detect on PATH. Point at ffmpeg.exe (or its bin folder) if it is installed elsewhere."),
      aspect: field("Default aspect ratio", f.aspect), shot_len: field("Default Kling shot length", f.shot_len),
      clip_len: field("Default clip length (seconds)", f.clip_len), fade: field("Default fade (seconds)", f.fade),
    };
    const err = el("p", { class: "error" }); err.hidden = true;
    const save = el("button", { class: "btn btn-primary", type: "button", onclick: async () => { err.hidden = true; Object.values(fields).forEach((x) => x.setError("")); busy(save, true, "Saving…");
      try {
        const body = {}; for (const k of Object.keys(f)) body[k] = f[k].value;
        const items = plats.map((p) => ({ id: p.id, enabled: $(`#plat-en-${p.id}`).checked, upload_url: $(`#plat-url-${p.id}`).value }));
        await api("PUT", "/api/settings", body); await api("PUT", "/api/platforms", { platforms: items });
        toast("Settings saved."); renderSettings(); refreshStatus();
      } catch (e) { err.textContent = e.message; err.hidden = false; }
      busy(save, false); } }, "Save settings");
    view.replaceChildren(el("h1", { text: "Settings" }),
      el("div", { class: "card" }, el("h2", { text: "You and your files" }), el("div", { class: "grid-form" }, fields.artist, fields.library_root),
        el("p", { class: "small muted" }, "Data folder: ", el("code", { text: s.data_dir }), ". Back up by copying that folder.")),
      el("div", { class: "card" }, el("h2", { text: "ffmpeg" }), el("div", { class: "row gap" }, el("div", { style: "flex:1;min-width:240px" }, fields.ffmpeg_path), test), ffStatus),
      el("div", { class: "card" }, el("h2", { text: "Defaults" }), el("div", { class: "grid-form" }, fields.aspect, fields.shot_len, fields.clip_len, fields.fade)),
      el("div", { class: "card" }, el("h2", { text: "Platforms" }), el("p", { class: "small muted", text: "Enabled platforms get a row in every track's Upload stage." }),
        el("div", { style: "overflow-x:auto" }, el("table", {}, el("thead", {}, el("tr", {}, el("th", { text: "On" }), el("th", { text: "Platform" }), el("th", { text: "Upload page URL" }))),
          el("tbody", {}, plats.map((p) => el("tr", {}, el("td", {}, el("input", { type: "checkbox", id: `plat-en-${p.id}`, checked: p.enabled ? true : null })), el("td", { text: p.name }), el("td", {}, input({ id: `plat-url-${p.id}`, value: p.upload_url, type: "url", placeholder: "https://…" })))))))),
      err, el("div", { class: "stage-actions" }, save));
  }

  // ---- boot ----------------------------------------------------------------------
  window.addEventListener("hashchange", route);
  setupFeedback();
  refreshStatus();
  route();
})();
