import { buildPackage } from "../src/engine.ts";
import { ARTISTS, resolveArtist } from "../src/artists.ts";
import { PROFILES } from "../src/profiles.ts";
import { assembleLyrics } from "../src/structure.ts";
import type { BuildType, ProfileId, ScaffoldSection, SunoPackage, TemplateId } from "../src/types.ts";

type BuildChoice = "auto" | BuildType;
type SlotId = NonNullable<ScaffoldSection["slot"]>;

const state = {
  dominant: "xxxtentacion",
  accents: new Map<string, number>(),
  template: "hook-first" as TemplateId,
  build: "auto" as BuildChoice,
  profile: "v5.5" as ProfileId,
  bans: [] as string[],
  laneGuards: true,
  bpm: undefined as number | undefined,
  verses: 2 as 2 | 3,
  lyricsMode: "tags" as "tags" | "bars",
  lyrics: new Map<SlotId, string>(),
  // Fresh seed per session: same DNA picks give a new prompt with the same
  // vibe each visit; Reroll draws another.
  seed: Math.floor(Math.random() * 1e9),
};

const ACCENT_STEPS = [0.3, 0.45];
const TEMPLATES: { id: TemplateId; label: string }[] = [
  { id: "hook-first", label: "Hook-first" },
  { id: "hook-first-beat-switch", label: "Beat switch" },
];
const VERSES: { id: "2" | "3"; label: string }[] = [
  { id: "2", label: "2 verses (~2:30)" },
  { id: "3", label: "3 verses (longer)" },
];
const LYRICS_MODES: { id: "tags" | "bars"; label: string }[] = [
  { id: "tags", label: "Tags only" },
  { id: "bars", label: "Write bars" },
];
const BUILDS: { id: BuildChoice; label: string }[] = [
  { id: "auto", label: "Auto" },
  { id: "faithful", label: "Faithful" },
  { id: "balanced", label: "Balanced" },
  { id: "fusion", label: "Fusion" },
];

const $ = <T extends HTMLElement>(id: string): T => document.getElementById(id) as T;

let current: SunoPackage;

function rebuild(): void {
  current = buildPackage({
    fusion: {
      dominant: state.dominant,
      accents: [...state.accents.entries()].map(([artist, weight]) => ({ artist, weight })),
    },
    template: state.template,
    build: state.build === "auto" ? undefined : state.build,
    profile: state.profile,
    ban: state.bans,
    laneGuards: state.laneGuards,
    bpm: state.bpm,
    seed: state.seed,
    verses: state.verses,
  });
  renderOutput();
}

function renderDominant(): void {
  const box = $("dominant");
  box.textContent = "";
  for (const a of ARTISTS) {
    const b = document.createElement("button");
    b.className = "chip" + (state.dominant === a.id ? " on" : "");
    b.textContent = a.displayName;
    b.setAttribute("aria-pressed", String(state.dominant === a.id));
    b.onclick = () => {
      state.dominant = a.id;
      state.accents.delete(a.id);
      renderDominant();
      renderAccents();
      rebuild();
    };
    box.appendChild(b);
  }
}

function renderAccents(): void {
  const box = $("accents");
  box.textContent = "";
  for (const a of ARTISTS) {
    const weight = state.accents.get(a.id);
    const b = document.createElement("button");
    b.className = "chip" + (weight ? " on" : "");
    b.disabled = a.id === state.dominant;
    b.setAttribute("aria-pressed", String(Boolean(weight)));
    b.innerHTML = "";
    b.append(a.displayName);
    if (weight) {
      b.append(" ");
      const w = document.createElement("span");
      w.className = "w";
      w.textContent = weight.toFixed(2);
      b.appendChild(w);
    }
    b.onclick = () => {
      const idx = weight ? ACCENT_STEPS.indexOf(weight) + 1 : 0;
      if (idx >= ACCENT_STEPS.length) state.accents.delete(a.id);
      else state.accents.set(a.id, ACCENT_STEPS[idx]!);
      renderAccents();
      rebuild();
    };
    box.appendChild(b);
  }
}

function renderSeg<T extends string>(
  id: string,
  options: { id: T; label: string }[],
  get: () => T,
  set: (v: T) => void,
): void {
  const box = $(id);
  box.textContent = "";
  for (const opt of options) {
    const b = document.createElement("button");
    b.className = get() === opt.id ? "on" : "";
    b.textContent = opt.label;
    b.setAttribute("aria-pressed", String(get() === opt.id));
    b.onclick = () => {
      set(opt.id);
      renderSeg(id, options, get, set);
      rebuild();
    };
    box.appendChild(b);
  }
}

function renderBans(): void {
  const box = $("bans");
  box.textContent = "";

  // Lane-guard toggle: default exclusions guarding the genre lane.
  const guards = resolveArtist(state.dominant)?.laneGuards ?? [];
  if (guards.length) {
    const g = document.createElement("button");
    g.className = "chip" + (state.laneGuards ? " on" : "");
    g.textContent = `lane guards: ${guards.join(", ")}`;
    g.setAttribute("aria-pressed", String(state.laneGuards));
    g.onclick = () => {
      state.laneGuards = !state.laneGuards;
      renderBans();
      rebuild();
    };
    box.appendChild(g);
  }

  const suggestions = ["saxophone", "edm", "pop", "piano", "autotune"];
  const shown = [...new Set([...state.bans, ...suggestions])];
  for (const ban of shown) {
    const active = state.bans.includes(ban);
    const b = document.createElement("button");
    b.className = "chip" + (active ? " on" : "");
    b.textContent = active ? `${ban} ✕` : ban;
    b.setAttribute("aria-pressed", String(active));
    b.onclick = () => {
      state.bans = active ? state.bans.filter((x) => x !== ban) : [...state.bans, ban];
      renderBans();
      rebuild();
    };
    box.appendChild(b);
  }
}

function assembledLyrics(): string {
  const slotTexts: Partial<Record<SlotId, string>> = {};
  for (const [slot, text] of state.lyrics) slotTexts[slot] = text;
  return assembleLyrics(current.lyricsSections, slotTexts);
}

function renderLyricsEditor(): void {
  const tagsPre = $<HTMLPreElement>("out-tags");
  const box = $("lyrics-editor");

  if (state.lyricsMode === "tags") {
    $("lyrics-hint").textContent =
      "Paste-ready structure control — Suno fills in the vocals and instrumental around these tags. " +
      "Section order is respected; the beat-switch template steers the swap with [Breakdown]/[Build]/[Drop].";
    tagsPre.hidden = false;
    tagsPre.textContent = current.lyricsTagsOnly;
    box.hidden = true;
    box.textContent = "";
    return;
  }

  tagsPre.hidden = true;
  box.hidden = false;
  const dna = resolveArtist(state.dominant);
  $("lyrics-hint").textContent =
    `Write your bars in each box — Copy gives finished, paste-ready lyrics. ` +
    `Flow: ${dna?.lyricNotes?.[0] ?? "keep bar cadence consistent with the groove"}. ` +
    `Ad-libs go in (parens), 1–3 words. ` +
    `Delivery tricks: hyphen-chain-words-like-this for fast tight flow, split syl-la-bles to stretch, ` +
    `CAPS + ! shouts (bleeds into next lines), ellipsis… drags.`;

  box.textContent = "";
  const seen = new Set<SlotId>();
  for (const section of current.lyricsSections) {
    const tagline = document.createElement("div");
    tagline.className = "tagline";
    tagline.textContent = section.tag;
    if (section.slot && seen.has(section.slot)) {
      tagline.append(" ");
      const hint = document.createElement("span");
      hint.className = "adlib-hint";
      hint.textContent = "— same hook repeats here";
      tagline.appendChild(hint);
      box.appendChild(tagline);
      continue;
    }
    if (section.slot && section.adlib) {
      tagline.append(" ");
      const hint = document.createElement("span");
      hint.className = "adlib-hint";
      hint.textContent = `ad-lib: (${section.adlib})`;
      tagline.appendChild(hint);
    }
    box.appendChild(tagline);
    if (!section.slot) continue;

    seen.add(section.slot);
    const slot = section.slot;
    const ta = document.createElement("textarea");
    ta.className = "lyric-input";
    ta.placeholder = section.guide ?? "";
    ta.value = state.lyrics.get(slot) ?? "";
    ta.rows = slot.startsWith("verse") ? 5 : 3;
    ta.setAttribute("aria-label", `${section.tag} lyrics`);
    ta.oninput = () => {
      const v = ta.value;
      if (v.trim()) state.lyrics.set(slot, v);
      else state.lyrics.delete(slot);
    };
    box.appendChild(ta);
  }
}

function renderOutput(): void {
  const profile = PROFILES[state.profile];
  $("out-style").textContent = current.styleText;
  const words = current.styleText.split(/\s+/).length;
  $("style-meta").textContent =
    `${current.styleText.length} / ${profile.styleCharLimit} chars · ${words} words · ` +
    `${current.meta.build} build @ ${current.meta.bpm} BPM · roll #${current.meta.seed % 1000}`;
  $("out-exclude").textContent =
    current.excludeText || "(empty — tap a ban chip or turn lane guards on)";
  renderLyricsEditor();

  const sliders = $("out-sliders");
  sliders.textContent = "";
  const rows: [string, { min: number; max: number }][] = [
    ["Weirdness", current.sliders.weirdness],
    ["Style influence", current.sliders.styleInfluence],
  ];
  for (const [name, range] of rows) {
    const row = document.createElement("div");
    row.className = "slider-row";
    const fillLeft = range.min;
    const fillWidth = range.max - range.min;
    row.innerHTML =
      `<span class="name">${name}</span>` +
      `<span class="track"><span class="fill" style="left:${fillLeft}%;width:${fillWidth}%"></span></span>` +
      `<span class="val">~${range.min}–${range.max}%</span>`;
    sliders.appendChild(row);
  }
  const note = document.createElement("p");
  note.className = "slider-note";
  note.textContent = current.sliders.note;
  sliders.appendChild(note);

  const notesCard = $("notes-card");
  const notes = $("out-notes");
  notes.textContent = "";
  notesCard.hidden = current.warnings.length === 0;
  for (const w of current.warnings) {
    const li = document.createElement("li");
    const badge = document.createElement("span");
    badge.className = `badge ${w.level}`;
    badge.textContent = w.level;
    li.appendChild(badge);
    li.append(w.message);
    notes.appendChild(li);
  }
}

function copyText(kind: string): string {
  if (kind === "style") return current.styleText;
  if (kind === "exclude") return current.excludeText;
  return state.lyricsMode === "tags" ? current.lyricsTagsOnly : assembledLyrics();
}

function wireCopy(): void {
  for (const btn of document.querySelectorAll<HTMLButtonElement>(".copy[data-copy]")) {
    btn.onclick = async () => {
      const text = copyText(btn.dataset.copy ?? "");
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
      const original = btn.textContent;
      btn.textContent = "Copied";
      btn.classList.add("done");
      setTimeout(() => {
        btn.textContent = original;
        btn.classList.remove("done");
      }, 1200);
    };
  }
}

function init(): void {
  renderDominant();
  renderAccents();
  renderSeg("template", TEMPLATES, () => state.template, (v) => (state.template = v));
  renderSeg("verses", VERSES, () => String(state.verses) as "2" | "3", (v) => (state.verses = Number(v) as 2 | 3));
  renderSeg("build", BUILDS, () => state.build, (v) => (state.build = v));
  renderSeg("lyrics-mode", LYRICS_MODES, () => state.lyricsMode, (v) => (state.lyricsMode = v));
  renderBans();

  $("reroll").onclick = () => {
    state.seed = Math.floor(Math.random() * 1e9);
    rebuild();
  };

  // Randomize = new DNA combo (dominant, maybe one accent) + fresh seed.
  // Reroll only re-words; this reshuffles the deck.
  $("randomize").onclick = () => {
    const pick = ARTISTS[Math.floor(Math.random() * ARTISTS.length)]!;
    state.dominant = pick.id;
    state.accents.clear();
    if (Math.random() < 0.6) {
      const others = ARTISTS.filter((a) => a.id !== pick.id);
      const accent = others[Math.floor(Math.random() * others.length)]!;
      state.accents.set(accent.id, ACCENT_STEPS[Math.floor(Math.random() * ACCENT_STEPS.length)]!);
    }
    state.seed = Math.floor(Math.random() * 1e9);
    renderDominant();
    renderAccents();
    renderBans();
    rebuild();
  };

  const profileSel = $<HTMLSelectElement>("profile");
  for (const id of Object.keys(PROFILES)) {
    const opt = document.createElement("option");
    opt.value = id;
    opt.textContent = id === "v4.5-all" ? "v4.5-all (free tier)" : id;
    profileSel.appendChild(opt);
  }
  profileSel.value = state.profile;
  profileSel.onchange = () => {
    state.profile = profileSel.value as ProfileId;
    rebuild();
  };

  const bpmInput = $<HTMLInputElement>("bpm");
  bpmInput.onchange = () => {
    const n = Number(bpmInput.value);
    state.bpm = bpmInput.value && n >= 60 && n <= 200 ? n : undefined;
    rebuild();
  };

  const banInput = $<HTMLInputElement>("ban-input");
  banInput.onkeydown = (e) => {
    if (e.key !== "Enter") return;
    const v = banInput.value.trim().toLowerCase();
    if (v && !state.bans.includes(v)) {
      state.bans = [...state.bans, v];
      banInput.value = "";
      renderBans();
      rebuild();
    }
  };

  wireCopy();
  rebuild();
}

init();
