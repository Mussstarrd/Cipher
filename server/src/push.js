/** Web push. Works from a phone home screen with no app store involved. */
import webpush from "web-push";

const ok = process.env.VAPID_PUBLIC && process.env.VAPID_PRIVATE;
if (ok) {
  webpush.setVapidDetails(
    process.env.VAPID_SUBJECT || "mailto:hearth@example.com",
    process.env.VAPID_PUBLIC,
    process.env.VAPID_PRIVATE,
  );
}

export const pushReady = () => Boolean(ok);

/** Returns the subscriptions that are still alive; dead ones are dropped. */
export async function notify(subs, { title, body, url }) {
  if (!ok) return subs;
  const alive = [];
  await Promise.all(
    subs.map(async (s) => {
      try {
        await webpush.sendNotification(s, JSON.stringify({ title, body, url }));
        alive.push(s);
      } catch (e) {
        // 404/410 mean the browser threw the subscription away. Anything else
        // is transient — keep it rather than silently losing a family member.
        // Either way say so: a push that fails silently looks exactly like a
        // push that arrived, and that ambiguity cost a morning of debugging.
        let host = "?"; try { host = new URL(s.endpoint).host; } catch { /* leave ? */ }
        console.error(`[push] ${e?.statusCode || e?.message || e} from ${host}`);
        if (e?.statusCode !== 404 && e?.statusCode !== 410) alive.push(s);
      }
    }),
  );
  return alive;
}

/** Same send, but returns per-subscription outcomes instead of pruning. */
export async function notifyVerbose(subs, msg) {
  const out = [];
  await Promise.all(subs.map(async (s) => {
    let host = "?"; try { host = new URL(s.endpoint).host; } catch { /* leave ? */ }
    if (!ok) { out.push({ ok: false, host, error: "vapid keys not set" }); return; }
    try {
      await webpush.sendNotification(s, JSON.stringify(msg));
      out.push({ ok: true, host });
    } catch (e) {
      out.push({ ok: false, host, status: e?.statusCode, error: e?.body?.slice?.(0, 120) || e?.message });
    }
  }));
  return out;
}
