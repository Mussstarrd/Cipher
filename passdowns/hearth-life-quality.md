# Pass-down — Hearth design session ("life quality" thread), as of Sun 2026-08-23 evening ET

Written on request relayed from Jeffery via his trading-desk session.

## 1 · What this agent was built to do

Build and operate **Hearth**: a self-hosted household assistant for the Fenn
family (Jeffery, Suzan, Aiden 9, Abby 2) that hibernates and wakes four times a
day (07:00/12:00/17:00/22:00 ET), reads its own markdown memory, briefs the
family, answers anyone in a shared web app, and rewrites its memory nightly so
it starts each day smarter. This session is the DESIGN side: it writes code and
pushes; a DigitalOcean droplet applies every push automatically within 15
minutes, restarts itself, and reports back through committed status files. The
operating charter is CLAUDE.md in this repo; the standing improvement list is
ops/backlog.md; the machine self-reviews daily via ops/deepscan.md (05:30 ET).

## 2 · What has been built so far

- **The pipeline**: git push → droplet auto-applies → restarts → proves it by
  pushing memory/status back. Heartbeat status report every 2h; daily deep scan
  that has already caught real bugs by reading (e.g. adults-room briefings were
  being silently discarded).
- **The app** (PWA, pastel "Easter nest" theme, phone-first): family/adults/
  private-per-person rooms enforced server-side; device-bound identity (server
  stamps authorship, never trusts the client); photo intake that reads school
  paperwork and opens tracked loops; a To do tab with tick boxes over
  memory/open-loops.md; timed reminders that push at the named minute to the
  named person's phone; instant search over all memory; meal planning drafted
  by the Sunday review; weather (open-meteo); message folding; text-to-speech
  playback; per-person reading level (Aiden: fifth grade, per Jeffery);
  build/version stamp + "notifications reach:" truth line in the footer.
- **Memory**: layered markdown in git — facts/rhythms/loops/corrections/misses/
  reference + topics/ filed by subject, daily logs, nightly self-review with a
  JSON-repair fallback (a whole day was once lost to one stray bracket).
- **Safety/privacy design**: private scratchpads have exactly two exits — an
  explicit "post this to family," and genuine safety concerns, which alert the
  parents only (parent-tagged push subs). Corrections from humans are the
  highest memory authority. Nothing outward (send/buy/register) without a human.
- **The trading lab** (granted 2026-08-23): research/strategy.md + morning
  research notes (this session, weekdays 08:45 ET, web-sweep + grading of
  yesterday's reasoning) feeding Hearth's bounded auto paper-trader at the
  17:00 wake (watchlist-only, ≤3 trades/day, 25% position cap, 10% cash floor,
  enforced in code). $100k pretend. The desk agent's pass-down is committed at
  research/passdowns/ and its tested rules adopted into strategy.md.

## 3 · Signals and sources watched

Droplet status blocks and memory commits on the repo (the droplet's only
voice); Gmail for the assistant account (fennassistant, IMAP app-password);
calendar ICS feeds (currently only the assistant's own — the family's real
calendars are still not connected); open-meteo weather; stooq quotes for the
paper book; usage.json token accounting (per-call, reported daily by the
heartbeat); self-scheduled triggers: daily deep scan 09:30 UTC, weekday lab
research 12:45 UTC.

## 4 · Right and wrong, concretely

Right: the two-session git protocol works unattended (dozens of push→apply→
verify cycles today, zero human couriering since the heartbeat landed); the
deep scan's first run found three real bugs including the vanishing adults
briefings; the coffee-reminder post-mortem correctly ran storage-truth
("confirmations must state what was STORED, not what the model promised"),
which is now a design rule; screenshot-verifying UI before pushing caught bugs
users never saw.

Wrong, with costs: told Jeffery to run a cleanup that missed .gitattributes and
silently froze auto-updates for hours; shipped B1 timed reminders with the due
field unwired on the chat path AND a parser that dropped bare times AND a
dedupe that ate retries — three stacked bugs, and reminders had never fired
once until the droplet's own records proved it; let the page cache stale so
Jeffery reported bugs already fixed; the notify chip trusted a phone-side flag
while the server pruned dead subscriptions ("nobody is getting notifications");
assumed third-grade level for Aiden (corrected: fifth); three background
watchers woke on this session's own pushes — wasted turns, replaced by
scheduled checks. Pattern to carry: every "it works" claim must come from the
droplet's records or a rendered screenshot, not from intention.

## 5 · Credentials and connections held — names only

On the droplet (in server/.env, never in git): Anthropic API key; Gmail app
password for the assistant account; calendar ICS secret URLs; VAPID push key
pair; family passphrase; adults passphrase (server/data/adult-pass). This
session: GitHub push access to mussstarrd/cipher (branch
claude/life-quality-improvement-wii2bd); Claude Code Remote MCP
(self-scheduling, triggers); connector MCPs available in-session: Gmail,
Google Calendar/Drive, GitHub, Robinson Trading (read-only use only, per
charter — order placement exists and is never touched); WebSearch/WebFetch.
Tailscale runs the droplet's private admin path; the app is public via Funnel.

## 6 · Open questions for the next shift

- Tonight 22:00 ET: FIRST Sunday review with topics, meal-plan drafting, and
  weekly compaction — verify tomorrow that memory got smaller, topics filed,
  and the plan drafted (backlog B17; tomorrow's deep scan checks).
- Push delivery end-to-end still unproven on any phone since the subscription
  self-heal shipped; footer "notifications reach:" is the instrument. Suzan's
  iPhone needs the Home-Screen install; Aiden's phone newly claimed.
- Real calendars (J1) and TeamSnap (J2, before Tuesday's first practice)
  remain the two highest-value human-blocked items; backup off-machine (B5)
  still unset — all memory lives on one disk.
- Trading lab day one is Monday: research note 08:45 ET, watch-only trader run
  at 17:00. "Intl"→INTC and "spxc"→SPCX readings await Jeffery's confirmation.
- Whether the adults passphrase value is what Jeffery believes it is — a
  reassignment loop today suggested it may differ; recovery is deleting
  server/data/adult-pass and re-setting from the app.
- The family passphrase recovery path (grep .env) was given today; consider an
  adult-facing rotate flow in-app (candidate backlog item).
