# Misses

Where Hearth was wrong. Predictions that did not match reality, suggestions that
were ignored, notifications nobody acted on.

This is the learning signal. The evening review must ask "what did I get wrong
today?", not only "what happened today?" — a system that records gets fatter, a
system that reviews its own errors gets sharper.

Format: `- [YYYY-MM-DD] Predicted/assumed X. Actually Y. Adjustment: Z.`

- [2026-08-23] **Named the oven model with false confidence.** Told Jeffery the
  Breville was "the BOV900" because the dial has Air Fry, Dehydrate and Super
  Convection, and said that combination "only comes on that one." Untrue — the
  BOV860 Smart Oven Air Fryer carries all three. I then wrote the wrong model into
  an open loop as settled, which would have produced the wrong calibration button
  sequence for him to hold down.
  **Adjustment:** a model number is read off the rating sticker, never inferred
  from a feature list. And "only X has this" is a claim about every other product
  in a range — I do not have that catalogue and must stop making it.

- [2026-08-23] **Claimed I could not tell the time, then told the time.** At 06:31
  I said I had no clock reading and could not work out five minutes from now. Two
  minutes later I answered "06:33 Sunday morning" unprompted by any clock Jeffery
  gave me. The first answer was a false claim of incapacity that made him do work
  he did not need to do.
  **Adjustment:** I have the session timestamp. Read it and answer.

- [2026-08-23] **Said "push set for 06:38."** I cannot set a push. I record a loop
  with a fire time and something else delivers it, and I have no visibility into
  whether it left. Saying "set" made Jeffery trust a buzz that never came.
  **Adjustment:** "Recorded, due 06:38. I can't see whether it reaches your phone."

- [2026-08-23] **Sent Jeffery to the wrong fixer.** When pushes failed I told him
  it was his Claude Code handler's problem. The actual cause was his phone's push
  subscription expiring, fixed by opening Hearth on that phone once. Hours of
  "check the coffee yourself" that a device-side line would have shortened.
  **Adjustment:** on any missed push, the first question is whether that phone has
  opened Hearth recently. Escalate second, not first.

- [2026-08-23] **Resolved an attribution I should have left open.** "Tell Jeff the
  palworld server is down" arrived from a device attributed to Jeffery. I answered
  as though Aiden had sent it from his dad's phone — I decided who was typing.
  That is exactly the thing Jeffery corrected on 22 Aug.
  **Adjustment:** when the content contradicts the device, name the contradiction
  and ask. Never pick a person.

- [2026-08-23] **Attributed a memory line to Suzan without evidence.** Told her
  "you told me on the 22nd" about Abby's dance. The 22 Aug entry records no
  speaker.
  **Adjustment:** memory lines carry provenance, not authorship. Say "I already
  had this from the 22nd," not "you told me."

- [2026-08-23] **Opened the 22:00 check-in with "Monday." on a Sunday.** Correct
  in intent — it was tomorrow's shape — but it reads as though I have lost the
  date, which is the single worst impression a scheduling assistant can give.
  **Adjustment:** "Tomorrow, Monday."

- [2026-08-23] **Asked the wrong question about food, three times.** "What's for
  dinner tonight?" got no answer at 12:00, 17:00 or in between. Meals are an
  explicit grant and I have not recorded a single thing this family eats. Asking
  about tonight repeatedly is nagging; asking who cooks and what nobody will eat
  is useful once.
  **Adjustment:** logged as an open loop. Ask one question, at a moment that makes
  sense, and write the answer down.

- [2026-08-22] Jeffery posted in the app and it looked as though nothing saved.
  It had saved. **The defect was mine:** the page stamped new messages in UTC
  while seeded messages were naive local times, so ordering broke.
  **Adjustment:** all timestamps normalised to UTC with `Z`; rendering forced to
  America/New_York; messages sorted by parsed time; a "saving…" chip during writes.
  **Wider lesson:** silent success looks identical to silent failure. Any write
  the family can see must say what it is doing.

- [2026-08-22] Risk identified before it caused loss: republishing the app from a
  local file **overwrites whatever the page has saved since**.
  **Adjustment — mandatory:** before any republish, read the live artifact, take
  its state as authoritative, merge into it, then publish.

- [2026-08-22] **The first scheduled run half-failed, and the prompt anticipated
  the wrong failure.** It handled a missing Gmail connector gracefully but could
  not commit and push at all, so nothing reached the branch.
  **Adjustment:** an unattended run must verify its own write access *first* and
  report that failure loudly, before doing work it cannot save.
  **Second adjustment:** anything Hearth sends must be identifiable as coming from
  Hearth, or it gets confused with harness noise.
