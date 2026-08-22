---
name: evening-review
description: Hearth's nightly self-review. Reads the day's raw log, extracts what is now settled, updates recurring rhythms, records corrections and misses, carries open loops forward, and writes it all into memory so tomorrow starts smarter. Runs unattended each evening; on Sundays it also compacts memory. Use when running the evening pass, the nightly summary, or the weekly memory compaction.
---

# Evening review

Runs late, unattended. Nobody reads the output — this is Hearth talking to its
future self. The quality of every morning brief depends on this pass being done
honestly rather than flatteringly.

## The one rule

Ask **"what did I get wrong today?"** — not "what happened today?"

A review that only records events makes memory fatter. A review that examines its
own errors makes it sharper. If you finish a pass having logged no miss and no
uncertainty, you were not looking hard enough; say so rather than inventing one,
but look again first.

## Steps

1. **Read the day.** `memory/daily/<today>.md`, plus today's calendar and any
   household mail you handled.

2. **Find the misses.** For each thing you predicted, suggested or assumed: did it
   hold? Departure times, how long errands took, meals proposed and not eaten,
   notifications nobody acted on, questions you answered wrong. Write each to
   `memory/misses.md` with the adjustment it implies. Notification accuracy matters
   most — if people are ignoring you, you are interrupting too often.

3. **Record corrections.** Anything a human explicitly corrected today goes to
   `memory/corrections.md`. Never soften these and never let a later inference
   quietly overwrite one.

4. **Update the rhythms.** This is the step that compounds, and the easiest to skip:
   - Did anything recurring happen today? Update its `last` and recompute `next
     expected`. Raise its confidence a step.
   - Did anything recurring *fail* to happen when expected? Lower confidence, log
     it to `misses.md`, do not just repeat the reminder next week.
   - Did anything new repeat for the second or third time? Promote it from
     `facts.md` to `rhythms.md` with a cadence — this is how the week's true shape
     gets learned rather than told.
   - Did a link get used to register, book or pay for something recurring? Capture
     it. Next season that link is most of the value.
   - Is anything due to fire within its lead time? Queue it for tomorrow's brief.

5. **Promote settled facts.** Anything observed enough times to be reliable goes to
   `memory/facts.md` with today's date and an observation count. One occurrence is
   not a fact. When a new fact contradicts an old one, keep the newer and note the
   change — do not leave both.

6. **Carry open loops.** Update `memory/open-loops.md`: close what was finished, add
   anything newly promised, flag anything untouched for more than a week. Nothing is
   removed for being old.

7. **Write tomorrow's setup.** Three or four lines at the end of the daily file:
   what tomorrow needs, what to watch for, what to ask about. The morning brief
   starts from this.

8. **Commit.** `git add memory/ && git commit` with a one-line message naming the
   date. The memory only survives because it is committed.

## Sunday — compaction

Additionally, once a week: merge duplicate facts, delete ones that have stopped
being true, resolve contradictions, archive resolved loops, and re-sort rhythms by
what fires next. Rewrite `facts.md` as a clean document rather than appending to it
forever. Without this step memory grows until it is too large and too contradictory
to be useful.

## Privacy

Do not carry anything about health, money beyond the timing Jeffery authorised, or
conflict between household members into long-term memory. When in doubt, leave it in
the daily log where it ages out, and do not promote it.
