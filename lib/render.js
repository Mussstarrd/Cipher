'use strict';
/** Render a block object as the copy-ready markdown block with the eight labeled sections. */
function renderBlock(b, opts = {}) {
  const L = [];
  const h = opts.compact ? '##' : '##';
  L.push(`# suno-forge prompt block: ${b.meta.laneLabel}`);
  L.push('');
  L.push(`Lane \`${b.meta.lane}\` | ${b.meta.bpm} BPM${b.meta.halfTime ? ' (half-time feel)' : ''} | ${b.meta.key} | arc: ${b.meta.arcLabel} | planned ${b.plan.totalFormatted}, hook at ${b.plan.hookAt}${b.meta.seed !== null && b.meta.seed !== undefined ? ` | seed ${b.meta.seed}` : ''}`);
  L.push('');
  L.push(`${h} 1. MODEL`);
  L.push(`**Pick:** ${b.model.pick}`);
  L.push(`**Why:** ${b.model.rationale}`);
  L.push(`**Wild/experimental variant:** ${b.model.wild}`);
  if (b.model.mini) L.push(`**Free-plan fallback:** ${b.model.mini}`);
  L.push('');
  L.push(`${h} 2. MODE/TOGGLES`);
  L.push(`- **Custom Mode:** ON (${b.toggles.modeName})`);
  L.push(`- **Instrumental:** ON`);
  L.push(`- **Max Mode:** ${b.toggles.maxMode ? 'ON' : 'OFF'}. ${b.toggles.maxModeNote}`);
  L.push(`- **Variety:** ${b.toggles.variety}. ${b.toggles.varietyNote}`);
  L.push(`- **Persona:** ${b.toggles.persona ? 'YES' : 'NO'}. ${b.toggles.personaNote}`);
  L.push(`- **Duration:** ${b.toggles.duration}`);
  L.push(`- **Song Title:** ${b.toggles.title}`);
  L.push('');
  L.push(`${h} 3. STYLE FIELD (${b.style.length}/${b.limits.style} chars, most important descriptors first)`);
  L.push('```text');
  L.push(b.style);
  L.push('```');
  L.push('');
  L.push(`${h} 4. LYRICS FIELD (${b.lyrics.length}/${b.limits.lyrics} chars, section tags only, no words)`);
  L.push('```text');
  L.push(b.lyrics);
  L.push('```');
  L.push('');
  L.push(`${h} 5. EXCLUDE STYLES FIELD (${b.exclude.length}/${b.limits.exclude} chars)`);
  L.push('```text');
  L.push(b.exclude);
  L.push('```');
  L.push('');
  L.push(`${h} 6. SLIDERS`);
  L.push(`- **Weirdness:** ${b.sliders.weirdness}% — ${b.sliders.weirdnessWhy}`);
  L.push(`- **Style Influence:** ${b.sliders.styleInfluence}% — ${b.sliders.styleInfluenceWhy}`);
  L.push(`- **Audio Influence:** ${b.sliders.audioInfluence}`);
  L.push(`- **Variety:** ${b.sliders.variety}`);
  L.push('');
  L.push(`${h} 7. VARIATIONS`);
  L.push(`**A. Safer** (Weirdness ${b.variations.safer.sliders.weirdness}%, Style Influence ${b.variations.safer.sliders.styleInfluence}%; ${b.variations.safer.style.length} chars)`);
  L.push('```text');
  L.push(b.variations.safer.style);
  L.push('```');
  L.push(`**B. Weirder** (Weirdness ${b.variations.weirder.sliders.weirdness}%, Style Influence ${b.variations.weirder.sliders.styleInfluence}%; ${b.variations.weirder.style.length} chars)`);
  L.push('```text');
  L.push(b.variations.weirder.style);
  L.push('```');
  L.push('');
  L.push(`${h} 8. POST-GEN NOTES`);
  for (const n of b.postGen) L.push(`- ${n}`);
  L.push('');
  L.push(`<details><summary>Arrangement plan</summary>`);
  L.push('');
  L.push('| # | Section | Bars | Starts | Energy |');
  L.push('|---|---------|------|--------|--------|');
  b.plan.sections.forEach((s, i) => L.push(`| ${i + 1} | ${s.type}${s.variant ? ` (${s.variant})` : ''} | ${s.bars} | ${s.start} | ${s.energy}/10 |`));
  L.push('');
  L.push(`</details>`);
  return L.join('\n');
}

/** Render a batch as a markdown table. */
function renderBatchTable(blocks) {
  const rows = ['| # | Seed | BPM | Key | Arc | Weird | Style Inf | Style field |', '|---|------|-----|-----|-----|-------|-----------|-------------|'];
  blocks.forEach((b, i) => rows.push(`| ${i + 1} | ${b.meta.seed} | ${b.meta.bpm} | ${b.meta.key} | ${b.meta.arc} | ${b.sliders.weirdness}% | ${b.sliders.styleInfluence}% | ${b.style.replace(/\|/g, '/')} |`));
  const sameArc = blocks.every((b) => b.meta.arc === blocks[0].meta.arc && b.meta.bpm === blocks[0].meta.bpm);
  if (sameArc) rows.push('', 'Shared Lyrics field (identical across the batch so the A/B isolates the Style wording):', '', '```text', blocks[0].lyrics, '```');
  else { rows.push('', 'Lyrics fields differ per row (arcs or BPMs vary):'); blocks.forEach((b, i) => rows.push('', `Row ${i + 1} (${b.meta.arc}, ${b.meta.bpm} BPM):`, '', '```text', b.lyrics, '```')); }
  rows.push('', 'Shared Exclude Styles field:', '', '```text', blocks[0].exclude, '```');
  return rows.join('\n');
}

module.exports = { renderBlock, renderBatchTable };
