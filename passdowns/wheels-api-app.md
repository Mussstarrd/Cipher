# Pass-down: Wheels API Phone App agent (Cipher repo)

**Date:** 2026-08-24
**Session branch:** `claude/phone-app-wheels-api-1w57hs` in `mussstarrd/cipher`
**Requested by:** Jeffery (relayed via his Robinhood-trading-MCP session)

## 1. What this agent was built to do

This session was started to answer one question from Jeffery: *"Would we be able to
make an app that runs solely on my phone that plugs into unusual wheels API and
possibly other APIs?"* — i.e., scope out and potentially build a mobile app for the
Unusual Whales (options-flow/market-data) API, self-contained on a phone with no
backend server. The Cipher repository is the intended home for that app.

## 2. What it has built / done so far

Honestly: **no code has been built yet.** The full activity log of this session is:

- Inspected the Cipher repo and found it empty (a single `# Cipher` README, one
  initial commit).
- Delivered a feasibility assessment: yes, a phone-only app is viable because
  Unusual Whales exposes a standard HTTPS REST API plus WebSockets, so the device
  can call it directly with an API key held in OS secure storage. Recommended
  React Native + Expo for a single cross-platform codebase, flagged that
  Unusual Whales API access is a paid tier separate from the web subscription,
  and noted the one real architectural limit: reliable push alerts while the app
  is closed would need a small cloud job — foreground/WebSocket data does not.
- Wrote this pass-down document.

Nothing has been scaffolded, no dependencies installed, no API calls made.

## 3. What signals or sources it watches

**None.** This agent has no live watches, subscriptions, schedules, or polling
loops. It is not connected to the Unusual Whales API, any brokerage, or any
market-data feed. It is not subscribed to any GitHub PR activity. It acts only
when messaged.

## 4. What it has gotten right and wrong

Limited track record given one substantive exchange, but concretely:

- **Right:** Checked the actual repo state before answering (confirming it was
  empty rather than assuming existing code). Correctly identified that
  "unusual wheels" almost certainly meant Unusual Whales and said so as an
  explicit assumption rather than silently proceeding. The feasibility points
  (direct-from-device API calls, key storage caveat, iOS background-alert
  limitation, paid API tier) are accurate to my knowledge.
- **Wrong / unverified:** Nothing demonstrably wrong yet, but be aware the
  assessment was written from training knowledge — I did **not** hit the
  Unusual Whales API docs or endpoints live to verify current pricing, rate
  limits, or WebSocket availability. Those claims should be re-verified against
  https://unusualwhales.com before building. The "unusual wheels = Unusual
  Whales" reading was also never explicitly confirmed by Jeffery.

## 5. Credentials / connections held (names only, no values)

- **GitHub access** scoped to the `mussstarrd/cipher` repository only, via the
  Claude Code remote environment's GitHub integration (git push credentials and
  GitHub MCP tools). No other repositories are reachable.
- **Claude Code Remote MCP server** (session/environment management tools).
- **Outbound HTTPS agent proxy** provided by the managed environment.
- Knowledge of Jeffery's email address for commit attribution purposes.

**Not held:** no Unusual Whales API key, no Robinhood or any brokerage
credentials, no exchange/market-data keys, no cloud-provider credentials, no
tokens of any kind beyond the environment-managed GitHub scope above.

## 6. Open questions

1. Is "unusual wheels API" definitely the Unusual Whales API, or something else
   (e.g., a wheel-strategy tool)?
2. Which Unusual Whales data matters most: flow alerts, dark pool prints,
   screeners, options chains, congressional trades?
3. Which "other APIs" are in scope — Robinhood (given the relaying session),
   Polygon, Alpaca, Tradier, something else? Robinhood notably has no official
   public API, which affects the design.
4. Target platform: iPhone, Android, or both? This decides the sideloading
   story (iOS personal installs need weekly re-signing or a $99/yr developer
   account).
5. Does Jeffery's Unusual Whales subscription include API access, and what are
   its actual current rate limits?
6. Are push notifications while the app is closed a requirement? If yes, the
   "solely on my phone" constraint softens — a small hosted job would be needed.
7. Read-only data app, or should it eventually place trades? Trading would raise
   the security bar considerably (credentials, confirmation flows).
