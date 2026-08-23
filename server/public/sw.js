self.addEventListener("push", (e) => {
  const d = (() => { try { return e.data.json(); } catch { return { title: "Hearth", body: "Check-in ready." }; } })();
  // One shared tag made every push REPLACE the one before it — a reminder
  // could erase the morning check-in from the shade, which read as
  // "my notification disappeared". Each kind keeps its own slot now, and a
  // same-kind update announces itself again instead of swapping silently.
  e.waitUntil(self.registration.showNotification(d.title || "Hearth", {
    body: d.body || "", data: { url: d.url || "/" }, icon: "/icon-192.png", badge: "/icon-192.png",
    tag: d.tag || `hearth-${Date.now()}`, renotify: true,
  }));
});
self.addEventListener("notificationclick", (e) => {
  e.notification.close();
  e.waitUntil(clients.matchAll({ type: "window", includeUncontrolled: true }).then((ws) => {
    for (const w of ws) if ("focus" in w) return w.focus();
    return clients.openWindow(e.notification.data?.url || "/");
  }));
});
