// Bundles web/main.ts (engine included) and inlines it into web/template.html,
// producing dist/cipher.html — a single self-contained file that runs offline
// in any browser, phone included.
import { build } from "esbuild";
import { mkdir, readFile, writeFile } from "node:fs/promises";

const result = await build({
  entryPoints: ["web/main.ts"],
  bundle: true,
  format: "iife",
  minify: true,
  write: false,
  // Older mobile Safari compatibility: transpile ?., ??, spread, etc.
  target: "es2017",
});

const js = result.outputFiles[0].text;
const template = await readFile("web/template.html", "utf8");
const marker = "/*__CIPHER_BUNDLE__*/";
if (!template.includes(marker)) throw new Error("bundle marker missing from template");
// script-tag safety: an IIFE bundle shouldn't contain "</script>", but guard anyway.
// Function replacement: with a string, String.replace treats $& etc. as special
// patterns and corrupts any bundle containing them.
const html = template.replace(marker, () => js.replaceAll("</script>", "<\\/script>"));
if (html.includes("__CIPHER_BUNDLE__")) throw new Error("marker text leaked into output — corruption");

await mkdir("dist", { recursive: true });
await writeFile("dist/cipher.html", html);
console.log(`dist/cipher.html written (${(html.length / 1024).toFixed(1)} KB)`);
