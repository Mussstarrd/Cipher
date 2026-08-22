# Misses

Where Hearth was wrong. Predictions that did not match reality, suggestions that
were ignored, notifications nobody acted on.

This is the learning signal. The evening review must ask "what did I get wrong
today?", not only "what happened today?" — a system that records gets fatter, a
system that reviews its own errors gets sharper.

Format: `- [YYYY-MM-DD] Predicted/assumed X. Actually Y. Adjustment: Z.`

_Empty._

- [2026-08-22] Jeffery posted in the app and it looked as though nothing saved.
  It had saved. **The defect was mine:** the page stamps new messages in UTC
  (`toISOString`), while the seeded messages were written as naive local times.
  The renderer read `03:31Z` as 23:31 the previous evening, filed the message
  under a "Fri 21 Aug" heading, and placed it below Saturday's messages. There
  was also no sort at all — order was array order.
  **Adjustment:** all timestamps normalised to UTC with `Z`; rendering forced to
  America/New_York via `Intl` so it stays correct wherever the family is; messages
  sorted by parsed time before render; the header chip now says "saving…" during a
  write so nobody has to guess again.
  **Wider lesson:** silent success looks identical to silent failure. Any write
  the family can see must say what it is doing.

- [2026-08-22] Risk identified before it caused loss: republishing the app from a
  local file **overwrites whatever the page has saved since**. Jeffery's message
  survived only because no republish happened in between.
  **Adjustment — now mandatory:** before any republish, read the live artifact,
  take its state as authoritative, merge local changes into it, then publish.
  Never publish a state built only from the local file.
