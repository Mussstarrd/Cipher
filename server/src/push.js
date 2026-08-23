/** Web push. Works from a phone home screen with no app store involved. */
import webpush from "web-push";
import { appendDaily } from "./memory.js";

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
export async function notify(subs, msg) {
  if (!ok) return subs;
  const alive = [];
  await Promise.all(
    subs.map(async (s) => {
      try {
        await webpush.sendNotification(s, JSON.stringify(msg));
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

  // A subscription dying while the app is closed used to die SILENTLY — the
  // phone showed "notifying", the server sent to nobody, and the first sign
  // was a missed reminder. Now the death is announced: in the daily log (so
  // the check-ins carry it) and to every phone still reachable, so a human
  // hears "X dropped off" from a device that still works.
  const dropped = subs.filter((s) => !alive.includes(s));
  if (dropped.length) {
    const names = dropped.map((d) => d.who || "an unnamed phone").join(", ");
    try { appendDaily(`- PUSH DROPPED: ${names} — the push service expired the subscription. That phone must open Hearth once to re-register. Say this in the next check-in.`); } catch { /* the log line is best-effort */ }
    const note = JSON.stringify({
      title: "Hearth", tag: "hearth-sub-lost",
      body: `${names} stopped receiving notifications. Have them open Hearth once to fix it.`,
    });
    await Promise.all(alive.map((s) => webpush.sendNotification(s, note).catch(() => {})));
  }
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
