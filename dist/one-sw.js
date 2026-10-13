/* ONE OF ONE — service worker. The page is one self-contained file, so offline is just: keep that file.
   The build stamps VERSION so a new build replaces the cache on the next load. */
"use strict";
const VERSION = "ea0d2039d1";
const CACHE = `oneofone-${VERSION}`;
const SHELL = ["./one.html", "./one.webmanifest", "./one-icon-192.png", "./one-icon-512.png"];

self.addEventListener("install", (e) => {
  e.waitUntil(caches.open(CACHE).then((c) => c.addAll(SHELL)).then(() => self.skipWaiting()));
});

self.addEventListener("activate", (e) => {
  e.waitUntil(
    caches.keys().then((keys) => Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)))).then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (e) => {
  const req = e.request;
  if (req.method !== "GET") return;
  const url = new URL(req.url);
  const isShell = url.origin === self.location.origin;
  const isFont = url.hostname === "fonts.googleapis.com" || url.hostname === "fonts.gstatic.com";
  if (!isShell && !isFont) return;
  // Shell: network first so a new build shows up on the next load; cache keeps it working offline.
  // Fonts: cache first; they are versioned by URL and never change.
  e.respondWith(
    isFont
      ? caches.match(req).then((hit) => hit || fetch(req).then((res) => cache(req, res)))
      : fetch(req).then((res) => cache(req, res)).catch(() => caches.match(req, { ignoreSearch: true }).then((hit) => hit || caches.match("./one.html")))
  );
});

function cache(req, res) {
  if (res && res.ok) {
    const copy = res.clone();
    caches.open(CACHE).then((c) => c.put(req, copy));
  }
  return res;
}
