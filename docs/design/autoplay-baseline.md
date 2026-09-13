# The autoplay baseline

`-exodus-autoplay <seconds>` plays a position with nobody at the controller. It exists because
every headless run before it watched wave one and nothing else: with no player nothing dies, wave
one never clears, and wave two never starts. The whole back half of every mission had never
executed outside a human session, and neither had two archetypes — a Spitter needs a turret to
hunt, and a Sapper needs a wall worth breaching.

## What it is, and what it is not

It builds through the real `BuildModel`, so cost, legality and the seal rules all apply. It does
**not** shoot, dodge, repair, retreat, re-site a gun, or spend a skill point. The hero is one of the
largest damage sources in the game and the bot has switched it off.

So these numbers are a **floor**: what a position does to a defence that was built once and then
abandoned. A position that holds here is not proven easy. A position that collapses here has not
been proven broken. What the table is good for is **comparison between positions**, because every
one of them faces the identical incompetent defender.

## The table (2026-09-13, 3 turrets + 8 barricades per setup, 420s budget)

| # | Position | Outcome | At | Waves cleared |
|---|----------|---------|----|---------------|
| 1 | The Gate | held, stalemate | 420s | 0 |
| 2 | The Service Road | truck lost | 70s | 0 |
| 3 | The Pump House | truck lost | 52s | 0 |
| 4 | The Gate Falls | held, stalemate | 420s | 0 |
| 5 | The Fairway | truck lost | 42s, **1 kill** | 0 |
| 6 | Crowbar | truck lost | 116s | **1** |

Three shapes, and each one says something:

**Stalemate (1, 4).** The corridor holds — the bot's two turrets are never overrun — but it cannot
kill fast enough to clear a wave in seven minutes. These positions are not testing the player's
defence so much as their damage. That is a defensible thing for an opening position to do and a
much less defensible thing for position 4 to still be doing.

**Fast loss (2, 3, 5).** The Fairway dying in 42 seconds with a single kill is not a bug: it is the
authored spike, the one position with `pillarExceptions` on record, "an open fairway with nothing to
funnel with at all". It is behaving exactly as written. Positions 2 and 3 have no such excuse on
file and are the two worth a human's attention first.

**Crowbar (6)** is the only position in the campaign that cleared a wave against this bot, which was
the opposite of what I expected from a centred objective with six approaches. Worth keeping in mind
before anyone "fixes" its difficulty.

## Known blind spots

- **The bot cannot afford a wall.** It spends its $400 opening on turrets and has roughly five
  barricades left — not enough to span a street on any heading. So player-wall Sapper behaviour is
  still unobserved, and mission 6's premise remains unverified by machine.
- **Sappers plant only when breaching SAVES WALKING** (`PlanSapper` scores walk-to-wall plus the far
  side's integration cost against the route already available). A barricade you can walk around is
  one no Sapper will look at. Crowbar's director chose three Sappers and every one of them targeted
  nothing, which is correct behaviour against a wall that blocks nothing.
- **Spitters only appeared in Crowbar**, because the other positions' `spitterFirstAt` lands after
  their wave-one spawn window closes. See CLAUDE.md on spawn windows: these thresholds are sampled
  on spawn events, never between waves.
