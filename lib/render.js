'use strict';
/** Render a block object as the copy-ready markdown block with the eight labeled sections. */
function renderBlock(b) {
  const L = [];
  const m = b.meta;
  L.push(`# suno-forge prompt block: ${m.laneLabel}`);
  L.push('');
  L.push(`Lane \`${m.lane}\` | ${m.bpm} BPM${m.halfTime ? ' (half-time feel)' : ''} | ${m.key} (${m.roman}: ${m.chords}) | layout: ${m.layoutLabel} (${m.mode}) | about ${b.plan.totalFormatted}, full beat at ${b.plan.hookAt} | seed ${m.seed}`);
  L.push('');
  L.push('## 1. MODEL');
  L.push(`**Pick:** ${b.model.pick}`);
  L.push(`**Why:** ${b.model.rationale}`);
  L.push(`**Wild/experimental variant:** ${b.model.wild}`);
  L.push(`**Free-plan fallback:** ${b.model.mini}`);
  L.push('');
  L.push('## 2. MODE/TOGGLES');
  L.push(`- **Custom Mode:** ON (${b.toggles.modeName})`);
  L.push('- **Instrumental:** ON');
  L.push(`- **Max Mode:** ${b.toggles.maxMode ? 'ON' : 'OFF'}. ${b.toggles.maxModeNote}`);
  L.push(`- **Variety:** ${b.toggles.variety}. ${b.toggles.varietyNote}`);
  L.push(`- **Persona:** ${b.toggles.persona ? 'YES' : 'NO'}. ${b.toggles.personaNote}`);
  L.push(`- **Duration:** ${b.toggles.duration}`);
  L.push(`- **Song Title:** ${b.toggles.title}`);
  L.push('');
  L.push(`## 3. STYLE FIELD (${b.style.length}/${b.limits.style} chars; head tags first, then lead / bass / drums / counter)`);
  L.push('```text'); L.push(b.style); L.push('```');
  L.push('');
  L.push(`## 4. LYRICS FIELD (${b.lyrics.length}/${b.limits.lyrics} chars; section tags only, no words)`);
  L.push('```text'); L.push(b.lyrics); L.push('```');
  L.push('');
  L.push(`## 5. EXCLUDE STYLES FIELD (${b.exclude.length}/${b.limits.exclude} chars)`);
  L.push('```text'); L.push(b.exclude); L.push('```');
  L.push('');
  L.push('## 6. SLIDERS');
  L.push(`- **Weirdness:** ${b.sliders.weirdness}% — ${b.sliders.weirdnessWhy}`);
  L.push(`- **Style Influence:** ${b.sliders.styleInfluence}% — ${b.sliders.styleInfluenceWhy}`);
  L.push(`- **Audio Influence:** ${b.sliders.audioInfluence}`);
  L.push(`- **Variety:** ${b.sliders.variety}`);
  L.push('');
  L.push('## 7. VARIATIONS');
  L.push(`**A. Safer** (${b.variations.safer.model}, Weirdness ${b.variations.safer.sliders.weirdness}%, Style Influence ${b.variations.safer.sliders.styleInfluence}%; ${b.variations.safer.style.length} chars). ${b.variations.safer.note}`);
  L.push('```text'); L.push(b.variations.safer.style); L.push('```');
  L.push(`**B. Weirder** (${b.variations.weirder.model}, Weirdness ${b.variations.weirder.sliders.weirdness}%, Style Influence ${b.variations.weirder.sliders.styleInfluence}%; ${b.variations.weirder.style.length} chars). ${b.variations.weirder.note}`);
  L.push('```text'); L.push(b.variations.weirder.style); L.push('```');
  L.push('');
  L.push('## 8. POST-GEN NOTES');
  for (const n of b.postGen) L.push(`- ${n}`);
  L.push('');
  L.push('<details><summary>Arrangement estimate</summary>');
  L.push('');
  L.push('| # | Section | Bars | Starts |');
  L.push('|---|---------|------|--------|');
  b.plan.sections.forEach((s, i) => L.push(`| ${i + 1} | ${s.tag} | ${s.bars} | ${s.start} |`));
  L.push('');
  L.push('</details>');
  return L.join('\n');
}

/** Render a batch as a markdown table. */
function renderBatchTable(blocks) {
  const rows = ['| # | Seed | BPM | Key | Layout | Lead | Weird | Style Inf | Style field (head tags) |', '|---|------|-----|-----|--------|------|-------|-----------|--------------------------|'];
  blocks.forEach((b, i) => rows.push(`| ${i + 1} | ${b.meta.seed} | ${b.meta.bpm} | ${b.meta.key} | ${b.meta.layout} | ${b.parts.lead.handle} | ${b.sliders.weirdness}% | ${b.sliders.styleInfluence}% | ${b.style.split('. lead:')[0].replace(/\|/g, '/')} |`));
  rows.push('', 'Full fields per row:');
  blocks.forEach((b, i) => rows.push('', `### Row ${i + 1} (seed ${b.meta.seed}, ${b.meta.layout})`, '', 'STYLES', '```text', b.style, '```', 'LYRICS', '```text', b.lyrics, '```', 'EXCLUDE STYLES', '```text', b.exclude, '```'));
  return rows.join('\n');
}

module.exports = { renderBlock, renderBatchTable };
