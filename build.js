// Inlines each engine and its UI into one self-contained page in dist/.
"use strict";
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

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
  "one.html": read("one/template.html")
    .replace("/*DATA*/", () => [read("one/guard.js"), read("one/theory.js"), read("one/grammar.js"), read("one/genres.js")].map(strip).join("\n"))
    .replace("/*ENGINE*/", () => strip(read("one/engine.js")))
    .replace("/*APP*/", () => read("one/app.js")),
};

fs.mkdirSync(path.join(__dirname, "dist"), { recursive: true });
const out = (name, data) => {
  fs.writeFileSync(path.join(__dirname, "dist", name), data);
  console.log(`dist/${name}  ${(data.length / 1024).toFixed(1)} KB`);
};
for (const [name, html] of Object.entries(pages)) out(name, html);

// ONE OF ONE is an installable PWA: manifest, service worker (stamped with the page hash so a new
// build replaces the offline copy) and icons drawn by src/one/icon.js. All served next to one.html.
const version = crypto.createHash("sha1").update(pages["one.html"]).digest("hex").slice(0, 10);
out("one.webmanifest", read("one/manifest.webmanifest"));
out("one-sw.js", read("one/sw.js").replace("/*VERSION*/", version));
const { icon } = require("./src/one/icon.js");
out("one-icon-192.png", icon(192));
out("one-icon-512.png", icon(512));
