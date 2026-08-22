/**
 * When is a check-in due?
 *
 * This used to be one line inside the tick: fire if the wall clock reads
 * exactly the slot. That is fine until the minute is missed — a restart, a
 * crash, a wake that overran — and then the check-in simply never happens and
 * nothing anywhere reports it. A thin brief and a missing brief look identical
 * from outside, and the missing one is the more corrosive of the two.
 *
 * So: a slot stays due for a grace window after its minute, a late brief says
 * it is late, and a slot that keeps failing backs off instead of retrying on
 * every tick. Separated from the server so it can be tested without a clock.
 */
export const SLOTS = ["07:00", "12:00", "17:00", "22:00"];

export const minutesOf = (hm) => Number(hm.slice(0, 2)) * 60 + Number(hm.slice(3, 5));

/** How late a check-in may be and still be worth sending. */
export const GRACE_MIN = Number(process.env.WAKE_GRACE_MINUTES || 90);

/** A failing slot must not become a model call every thirty seconds. */
export const MAX_TRIES = 3;
export const RETRY_MIN = 5;

/**
 * The latest check-in that should already have run today and has not.
 * Latest, not earliest: if the machine was off all morning, the family wants
 * where the day is now, not a stale 07:00 brief followed by three more.
 *
 * `now` is "HH:MM" Eastern, `state` is the persisted state, `day` is today's
 * date in Eastern. `nowMs` is injectable so the backoff can be tested.
 */
export function dueSlot(now, state, day, nowMs = Date.now()) {
  let best = null;
  for (const slot of SLOTS) {
    if (state.lastRun?.[slot] === day) continue;

    const late = minutesOf(now) - minutesOf(slot);
    if (late < 0 || late > GRACE_MIN) continue;

    const t = state.wakeTries?.[slot];
    if (t && t.day === day) {
      if (t.n >= MAX_TRIES) continue;                          // gave it a fair run
      if (nowMs - Date.parse(t.at) < RETRY_MIN * 60_000) continue;   // too soon
    }
    if (!best || minutesOf(slot) > minutesOf(best.slot)) best = { slot, late };
  }
  return best;
}
