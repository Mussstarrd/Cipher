// Inlines each engine and its UI into one self-contained page in dist/.
"use strict";
const fs = require("fs");
const path = require("path");

const read = (f) => fs.readFileSync(path.join(__dirname, "src", f), "utf8");
const strip = (s) => s.replace(/\nif \(typeof module[^\n]*\n?$/, "\n");

const pages = {
  "cipher.html": read("template.html")
    .replace("/*ENGINE*/", () => [read("likeness.js"), read("engine.js")].map(strip).join("\n"))
    .replace("/*APP*/", () => read("app.js")),
  "anomaly.html": read("anomaly/template.html")
    .replace("/*DATA*/", () => [read("shared/vibe.js"), read("anomaly/blueprints.js"), read("anomaly/data.js")].map(strip).join("\n"))
    .replace("/*ENGINE*/", () => strip(read("anomaly/engine.js")))
    .replace("/*APP*/", () => read("anomaly/app.js")),
  "manhole.html": read("manhole/template.html")
    .replace("/*DATA*/", () => [read("shared/vibe.js"), read("manhole/data.js")].map(strip).join("\n"))
    .replace("/*ENGINE*/", () => strip(read("manhole/engine.js")))
    .replace("/*APP*/", () => read("manhole/app.js")),
};

fs.mkdirSync(path.join(__dirname, "dist"), { recursive: true });
for (const [name, html] of Object.entries(pages)) {
  fs.writeFileSync(path.join(__dirname, "dist", name), html);
  console.log(`dist/${name}  ${(html.length / 1024).toFixed(1)} KB`);
}
