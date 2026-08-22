/**
 * Tests for the one decision that determines whether the family hears from
 * Hearth at all. Run: npm test
 */
import { test } from "node:test";
import assert from "node:assert/strict";
import { dueSlot } from "./schedule.js";

const DAY = "2026-08-22";
const clean = { lastRun: {}, wakeTries: {} };

test("fires on the exact minute", () => {
  assert.deepEqual(dueSlot("07:00", clean, DAY), { slot: "07:00", late: 0 });
});

test("does not fire before the minute", () => {
  assert.equal(dueSlot("06:59", clean, DAY), null);
});

test("still fires after a missed minute — this is the whole point", () => {
  // The service was restarting at 07:00. The old code lost the day silently.
  assert.deepEqual(dueSlot("07:12", clean, DAY), { slot: "07:00", late: 12 });
});

test("gives up once the brief would be worse than nothing", () => {
  assert.equal(dueSlot("09:00", clean, DAY), null);   // 120 min > 90 grace
});

test("does not repeat a slot already run today", () => {
  assert.equal(dueSlot("07:05", { ...clean, lastRun: { "07:00": DAY } }, DAY), null);
});

test("yesterday's run does not satisfy today's slot", () => {
  assert.deepEqual(
    dueSlot("07:00", { ...clean, lastRun: { "07:00": "2026-08-21" } }, DAY),
    { slot: "07:00", late: 0 },
  );
});

test("after a long outage, sends the latest slot only — not a backlog of four", () => {
  // Machine off all day, back at 17:20. The family wants where the day is now,
  // not a 07:00 brief, then a 12:00 brief, then a 17:00 brief.
  assert.deepEqual(dueSlot("17:20", clean, DAY), { slot: "17:00", late: 20 });
});

test("backs off instead of retrying on every 30-second tick", () => {
  const t0 = Date.parse("2026-08-22T11:00:00Z");
  const state = { lastRun: {}, wakeTries: { "07:00": { day: DAY, n: 1, at: new Date(t0).toISOString() } } };
  assert.equal(dueSlot("07:02", state, DAY, t0 + 60_000), null, "1 min later: too soon");
  assert.deepEqual(dueSlot("07:08", state, DAY, t0 + 6 * 60_000), { slot: "07:00", late: 8 },
    "6 min later: worth another try");
});

test("stops after three tries rather than burning the grace window", () => {
  const t0 = Date.parse("2026-08-22T11:00:00Z");
  const state = { lastRun: {}, wakeTries: { "07:00": { day: DAY, n: 3, at: new Date(t0).toISOString() } } };
  assert.equal(dueSlot("07:40", state, DAY, t0 + 40 * 60_000), null);
});

test("yesterday's exhausted tries do not block today", () => {
  const state = { lastRun: {}, wakeTries: { "07:00": { day: "2026-08-21", n: 3, at: "2026-08-21T11:00:00Z" } } };
  assert.deepEqual(dueSlot("07:00", state, DAY), { slot: "07:00", late: 0 });
});

test("survives a state file with no scheduling fields at all", () => {
  assert.deepEqual(dueSlot("12:00", {}, DAY), { slot: "12:00", late: 0 });
});
