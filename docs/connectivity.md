# What can actually reach Hearth, and what Hearth can actually reach

Honest capability map. Anything marked **No** is not a matter of effort — it is a
platform restriction, and pretending otherwise would waste a month.

## Getting information IN

| Source | Today? | How |
| --- | --- | --- |
| Gmail | **Yes** | Connector, full read. School mail, invoices, invitations, receipts. |
| Google Calendar | **Yes** | Connector, read and write. |
| Google Drive | **Yes** | Connector. Where filed paper lives. |
| A photo of a document | **Yes** | Vision. This is `paper-trail` — the permission-slip flow. |
| Someone just telling it | **Yes** | Always works. The only input standalone mode needs. |
| Weather, store hours, school site, prices | **Yes** | Server-side web search. Runs on Anthropic's infrastructure, no networking to configure. |
| Stock earnings dates | **Yes** | Brokerage connector. Dates only — never trades. |
| School apps: ClassDojo, Remind, Seesaw, PowerSchool | **Partly** | No API for any of them. **But they all send email notifications, and those emails are readable.** This is the practical route and it covers most of what those apps carry. |
| SMS / iMessage | **No** | No connector exists. Requires a real phone number via a service like Twilio — see below. |
| Facebook, Instagram, social feeds | **No** | Their APIs do not permit reading a personal feed. Where a school posts to a Facebook group, there is no supported way in. Email notifications from those services, if enabled, are readable. |

## Getting information OUT

| Channel | Today? | Notes |
| --- | --- | --- |
| Push to Jeffery's phone | **Yes** | Via the Claude app. Account holder only. |
| Email — including several people at once | **Yes** | The universal door. Works on every phone, needs no install and no account. |
| A shared page everyone edits | **Yes** | Rich, live, multi-person — but writers realistically need accounts. |
| A filled document to send back to school | **Yes** | `.docx` or fillable `.pdf`. |
| **Family group chat (SMS/MMS)** | **Not today** | This is the one that matters most and it is the one that needs building. A Twilio number can be a participant in an SMS/MMS group chat, which is how Milo worked before it shut down. Costs a couple of dollars a month plus per-message. Requires a small always-on service to bridge Twilio to Hearth. Real work, not hard work. |
| iMessage blue-bubble group | **No** | No supported bot participation. A group containing any non-Apple number degrades to MMS, where a Twilio number does work. |
| WhatsApp group | **Restricted** | A Business API exists; group messaging is not generally available for this use. |
| Telegram, Discord, Slack | **Trivial** | Bot APIs are easy. Families do not use them. |

## The two modes

**Connected** — reads Gmail, Calendar and Drive. Knows things nobody typed in.

**Standalone** — no account access at all. Hearth keeps its own calendar, its own
lists, its own memory. Everything arrives by someone telling it or photographing
it. This is not a degraded fallback; it is where most people will start, and every
feature above the ingestion layer must work here: rhythms, open loops, the four
check-ins, paper-trail, meals, reminders, the nightly review.

The rule: **never write a feature that silently requires connected mode.** If
something genuinely needs account access, say so in the moment and offer the
standalone equivalent.

## Sequencing note

The delivery channel is the last mile and the easiest part to swap — the brain
does not care whether its output lands as a push, an email, or a text. Build the
brain first, deliver by email, and add the SMS bridge when the thing is worth
texting.
