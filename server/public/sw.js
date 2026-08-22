self.addEventListener("push", (e) => {
  const d = (() => { try { return e.data.json(); } catch { return { title: "Hearth", body: "Check-in ready." }; } })();
  e.waitUntil(self.registration.showNotification(d.title || "Hearth", {
    body: d.body || "", data: { url: d.url || "/" }, icon: "/icon.png", badge: "/icon.png", tag: "hearth",
  }));
});
self.addEventListener("notificationclick", (e) => {
  e.notification.close();
  e.waitUntil(clients.matchAll({ type: "window", includeUncontrolled: true }).then((ws) => {
    for (const w of ws) if ("focus" in w) return w.focus();
    return clients.openWindow(e.notification.data?.url || "/");
  }));
});
