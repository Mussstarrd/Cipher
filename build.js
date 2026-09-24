// Inlines the engine and UI into one self-contained page: dist/cipher.html
"use strict";
const fs = require("fs");
const path = require("path");

const read = (f) => fs.readFileSync(path.join(__dirname, "src", f), "utf8");
const html = read("template.html")
  .replace("/*ENGINE*/", () => [read("likeness.js"), read("engine.js")].map((s) => s.replace(/\nif \(typeof module[^\n]*\n?$/, "\n")).join("\n"))
  .replace("/*APP*/", () => read("app.js"));

fs.mkdirSync(path.join(__dirname, "dist"), { recursive: true });
fs.writeFileSync(path.join(__dirname, "dist", "cipher.html"), html);
console.log(`dist/cipher.html  ${(html.length / 1024).toFixed(1)} KB`);
