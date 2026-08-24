# Rhythms

Everything that recurs and has to **fire**. Distinct from `facts.md`: a fact is a
static truth, a rhythm has a clock attached.

This is the layer that makes Hearth worth having. Anyone can be told "soccer is
Tuesdays." Noticing in August that registration is about to open again, and asking
before the window closes, is the thing no calendar does.

## Rules

1. **Never transact.** A rhythm firing produces a message and a link. Jeffery
   presses the button.
2. **Lead time is the whole point.** Firing on the day is useless.
3. **Record the link when it happens.** Next season that link is most of the value.
4. **Cash-flow timing beats amounts.** Track whether a debit lands before or after
   a paycheck; flag the collisions.
5. **Decay unconfirmed rhythms.** Expected to fire and nothing happened, and nobody
   mentioned it? Lower confidence, log to `misses.md`, do not nag forever.

---

## Daily — the weekday shape

- 06:00 Suzan wakes.
- 07:00 Suzan leaves, Abby with her, dropped at Merit on the way.
- 08:00 Suzan at Geico, Fredericksburg.
- 08:30 Aiden leaves for the bus.
- 16:30 Suzan off work; collects Abby; home roughly 17:05–17:10.
- 16:35 Aiden home from the bus.
- Jeffery home all day unless travelling.
- confidence: told 2026-08-23 by Jeffery. Watch whether it holds; promote to
  observed once a week of days matches it.
- action: **any weekday plan before 07:00 or between 07:00 and 16:30 involves only
  Jeffery.** Never propose something that needs Suzan inside her workday.

---

## Weekly

### Abby — dance class
- who: Abby
- cadence: weekly, **Saturdays 08:30**
- confidence: told 2026-08-22, confirmed again by Suzan 2026-08-23 — settled.
- **still unknown: where, how long, who drives.** Asked 23 Aug, unanswered.
- next: Sat 2026-08-29.

### Aiden — soccer practice
- who: Aiden
- cadence: weekly, **Tuesdays and Thursdays 17:30–18:30**
- where: **Sweetbriar Park, Lake of the Woods**
- season: fall 2026, first practice **Tue 2026-08-25**
- confidence: told twice — Coach Nate 17 Aug, Jeffery 23 Aug. Settled.
- action: Tuesday and Thursday 17:00 check-ins carry it. Dinner on those nights
  must be ready before 17:15 or after 18:45 — see `topics/meals.md`.
- first practice is introductions, positions and expectations. No kit note given.

---

## Monthly / cash flow

_No bills supplied yet. Category is authorised; Jeffery has not given any._

### Paydays — alternating
- cadence: **both biweekly on Fridays, offset by a week.** Jeffery paid Fri
  2026-08-21; Suzan paid Fri 2026-08-28.
- so: money lands **every** Friday, from one of them alternately.
- confidence: told 2026-08-22
- action: the useful output is not the payday, it is whether a bill's due date
  falls in Jeffery's week or Suzan's. Never state an amount unless Jeffery
  supplied it.
- **unknown: which of the two weeks is tighter.** Do not assume.

---

## Seasonal and annual

### Aiden — Orange County Soccer Association registration
- who: Aiden — U10 for fall 2026
- cadence: seasonal. Fall confirmed; assume a spring season also runs —
  **unverified, do not state as fact.**
- lead: 3 weeks before the window closes
- last: fall 2026 — already registered before Hearth existed
- next expected: spring 2027 (confidence: inferred, unverified)
- action: raise it, ask whether to sign Aiden up, hand over the link. Never register
  and never pay.
- contact: Coach Nate Woodruff — natefbd@gmail.com, c 703-586-2025.
- **registration link: not yet captured.** Get it when spring signup opens; that
  link is the whole point of this entry.

### Aiden — soccer team season setup
- cadence: every season, once
- recurring steps: accept the TeamSnap invite → wait for the team name vote →
  wait for the game schedule to be published → get the schedule into Hearth.
- 2026 fall status: invite accepted 2026-08-23. Team name still "TBD".
  **Game schedule not yet published.**
- watch for: the schedule appearing. When it does, every game becomes a dated
  commitment and should reach the check-ins.

---

## Hearth's own maintenance

### The 07:00 trigger's clock will drift at the DST change
- trigger: `trig_01E5jDRY32mqVmaf2PNK8RKR`, cron `0 11 * * *` — evaluated in **UTC**.
- right now (EDT, UTC-4) 11:00 UTC is 07:00 Eastern. Correct.
- **when DST ends on Sun 2026-11-01**, Eastern becomes UTC-5 and the same cron
  fires at **06:00 local** — an hour early, every day, silently.
- lead: raise this in the week before 2026-11-01.
- action: change the cron to `0 12 * * *`. Reverse it when DST resumes on
  Sun 2027-03-14 (back to `0 11 * * *`).
- confidence: certain — arithmetic, not observation.

### Push subscriptions expire
- observed 2026-08-23: Jeffery's push subscription expired mid-day. Reminders were
  stored correctly and simply never left. The fix is device-side — that phone must
  open Hearth once to re-register.
- action: **on any report of a missed push, check this first**, before sending
  anyone to the handler. I got that order wrong on 23 Aug.
- watch: if it expires again, note the interval. If there is a pattern, it becomes
  a scheduled "open the app" nudge rather than a surprise.
