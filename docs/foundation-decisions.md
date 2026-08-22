# Foundation decisions

> **PARKED 2026-08-22.** Everything below was written for a multi-tenant
> product. Jeffery's call: build it as a refined tool for one household first.
> Nothing here is wrong, it is just not now — revisit only if another family
> asks to use it.
>
> **What changed for the personal build:**
> - **Gmail is fine, and needs no OAuth.** App Passwords still work in 2026:
>   2FA on the account, generate a 16-character password, connect over
>   `imap.gmail.com:993` and `smtp.gmail.com:465`. No Google Cloud project, no
>   CASA assessment, no verification, nothing that expires. For one household
>   this is strictly better than OAuth.
> - **COPPA does not apply.** It governs commercial services collecting from
>   children. A parent running software for his own household is not that.
> - **No multi-tenancy, no Stripe, no magic links.** One household, one box.
> - Still true and still worth keeping: the append-only event log, memory the
>   family can read and correct, and a real delete path.

Recorded so they are not re-litigated. Full reasoning:
https://claude.ai/code/artifact/65e87f03-5784-4078-991e-fbf5b9a7e1ec

## Decided

1. **Own the email address; do not use the family's Gmail.**
   Each household gets `<name>@<ourdomain>`. Inbound via Postmark or Mailgun,
   outbound from the same address with SPF/DKIM on our domain.
   *Why:* reading a customer's Gmail needs Google restricted scopes → CASA Tier 2
   assessment, $540–$1,000 in lab fees, 4–12+ weeks to approval, and annual
   recertification forever. That is a wall in front of the first customer.
   Owning the address gives the identical product with no OAuth, no Google
   review, no annual audit, and no dependency on a vendor's policy change.
   A household that wants their existing Gmail included just sets a forward.

2. **Multi-tenant from the first migration.** `household_id` on every row.

3. **Append-only `events` are the truth; everything readable is a projection.**
   Nobody edits state directly. The nightly review folds events into what people
   read. Concurrent writes stop conflicting and the audit trail is the storage
   format rather than a feature.

4. **Memory must be readable and correctable by the family**, always rendered as
   plain documents in the app. The first time someone feels watched by it, the
   product is dead in that house.

5. **Design the delete path before the write path.** One button that really
   removes a household, backups included.

6. **COPPA applies** the moment there are customers who are not us — there is a
   two-year-old and a nine-year-old in the data. Verifiable parental consent at
   signup, data minimisation, no behavioural advertising, real deletion.
   **Needs a lawyer before the first paying customer.**

7. **Route models by job.** Haiku answers questions, Sonnet writes check-ins,
   Opus does the nightly review and nothing else — that is where the compounding
   happens and the one place never to economise. Cache the memory prefix; it is
   byte-identical per household on every call.

8. **Launch at $19, not $15.** Naive routing costs ~$24/household/month in model
   spend, which is underwater. Routed plus cached lands ~$7–10. Four dollars is
   invisible to the customer and is the difference between a comfortable margin
   and a nervous one. Meter every household from day one and cap outliers.

9. **Stack:** one DigitalOcean droplet with Docker Compose (app + worker +
   Postgres) until customers exist, then DO Managed Postgres. Node 22 +
   TypeScript, Graphile Worker or pg-boss on Postgres, Stripe Billing,
   magic-link auth to the household address, and the existing PWA as the client.

## Explicitly rejected

- **Gmail OAuth ingestion** — see 1.
- **Growing the prototype into the product.** `server/` is a specification, not
  a codebase. What is valuable in it — the prompts, the memory design, the
  failure handling — is portable. The code was how we found out what to build.
- **Free serverless tiers.** Anything that sleeps on idle defeats a product
  whose entire job is being awake when nobody is asking.
- **Redis, at first.** Not until Postgres actually hurts.
