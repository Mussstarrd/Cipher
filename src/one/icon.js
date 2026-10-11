/* ONE OF ONE — app icon, drawn in pure Node (no dependencies).
   A lime field with the slash from the wordmark ONE/ONE. Full-bleed, so it works as a maskable icon too. */
"use strict";
const zlib = require("zlib");

const CRC = new Int32Array(256);
for (let n = 0; n < 256; n++) {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  CRC[n] = c;
}
const crc32 = (buf) => {
  let c = -1;
  for (const b of buf) c = CRC[(c ^ b) & 0xff] ^ (c >>> 8);
  return (c ^ -1) >>> 0;
};
const chunk = (type, data) => {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
};

const LIME = [0xc8, 0xff, 0x3d];
const INK = [0x0b, 0x0b, 0x0d];

/** Returns a PNG buffer, `size` px square. */
function icon(size) {
  const S = size;
  const raw = Buffer.alloc((S * 3 + 1) * S);
  const half = S * 0.075; // slash half-width
  const lo = S * 0.24, hi = S * 0.76; // slash spans the middle band only
  for (let y = 0; y < S; y++) {
    raw[y * (S * 3 + 1)] = 0; // filter: none
    for (let x = 0; x < S; x++) {
      const cx = x + 0.5, cy = y + 0.5;
      // "/" runs bottom-left to top-right: the anti-diagonal cx + cy = S.
      const d = Math.abs(cx + cy - S) / Math.SQRT2;
      const inBand = cy > lo && cy < hi;
      let a = inBand ? Math.max(0, Math.min(1, half - d + 0.5)) : 0;
      // Soften the band's top and bottom edge by a pixel.
      if (inBand) a *= Math.min(1, cy - lo, hi - cy);
      const o = y * (S * 3 + 1) + 1 + x * 3;
      for (let c = 0; c < 3; c++) raw[o + c] = Math.round(LIME[c] + (INK[c] - LIME[c]) * a);
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(S, 0);
  ihdr.writeUInt32BE(S, 4);
  ihdr[8] = 8; // bit depth
  ihdr[9] = 2; // colour type: RGB
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk("IHDR", ihdr),
    chunk("IDAT", zlib.deflateSync(raw, { level: 9 })),
    chunk("IEND", Buffer.alloc(0)),
  ]);
}

module.exports = { icon };
