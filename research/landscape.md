# Landscape

Scout report, 2026-10-31. Covers the five pain areas in `research/interview.md`, ranked A–E.
Every factual claim links to its source. My own judgments are marked "(scout's inference)".

**Method note.** This session could not resolve vendor domains (tradervue.com, tradesviz.com,
edgewonk.com, robinhood.com, trustpilot.com, hn.algolia.com, reddit mirrors). Where a vendor
page is cited, the text came through a search-engine summary of that page, not a direct fetch.
Prices below are therefore "as reported by <source> on <date>" and should be re-checked on the
vendor's page before anyone quotes them to the owner. GitHub pages did resolve and were read
directly.

---

## A. Trading-research workflow: setup → trade → outcome → scorecard

### What exists

**Commercial journals (all cloud, all subscription):**

| Tool | Price as reported | Options support | Robinhood import | Source / date |
|---|---|---|---|---|
| TraderSync | $29.95–$79.95/mo; no free plan, 7-day trial | "automatic options spread detection"; strong parsing | yes (700+ brokers claimed) | [NexusFi](https://nexusfi.com/d/journals/tradersync), [DayTradingToolkit review, checked 9 Oct 2026](https://daytradingtoolkit.com/reviews/tradersync-review), [Money Crashers](https://www.moneycrashers.com/tradersync-review/) |
| Tradervue | Free tier (stocks/ETFs only, capped), Silver $29.95/mo, Gold $49.95/mo | paid tiers only; "35+ options brokers" | CSV import documented | [DayTradingZ](https://daytradingz.com/tradervue-review), [StockBrokers.com](https://www.stockbrokers.com/review/tools/tradervue), [Tradervue Robinhood CSV guide](https://www.tradervue.com/blog/how-to-import-trades-from-a-robinhood-csv-into-tradervue) |
| TradesViz | Free Basic (stocks only, 3,000 executions/mo); Pro ~$14.99–$19.99/mo; Platinum ~$22.49/mo | Pro+; auto spread grouping, manual multi-leg input | CSV or auto-sync | [TradesViz pricing via search](https://www.tradesviz.com/pricing), [TradesViz Robinhood page](https://www.tradesviz.com/brokers/Robinhood), [multi-leg input post](https://www.tradesviz.com/blog/option-spread-input) |
| TradeZella | Essential $35, Pro $59, Ultra $99/mo (3-tier change in 2026) | yes; one competitor claims options gated to top tier | n/a | [TradeZella pricing blog](https://www.tradezella.com/blog/tradezella-pricing-guide-2026), [PricingSaaS](https://pricingsaas.com/companies/tradezella) |
| Edgewonk 3 | moved from one-time ~$169 to annual (~$169–$197/yr) when it went cloud | manual-first journal | no | [Edgewonk 3 launch post](https://edgewonk.com/blog/edgewonk-3-launch), [TheGodFunded](https://thegodfunded.com/en/tools/edgewonk/) |
| Kinfo | Free + PRO $9.99/mo; social/verified-performance app, iOS updated Jul 2026 | — | — | [NexusFi](https://nexusfi.com/d/journals/kinfo), [App Store](https://apps.apple.com/app/id1220075825) |
| Stonk Journal | Free, donation-funded; manual entry only | manual | **no import** (2-star Trustpilot review) | [Trustpilot](https://uk.trustpilot.com/review/stonkjournal.com), [About](https://stonkjournal.com/about) |

**Open-source / self-hosted (all found on GitHub, read directly 2026-10-31):**

- [LuxAlgo/trade-journal](https://github.com/LuxAlgo/trade-journal) — MIT, ~551 stars, SQLite, Next.js 15, Node 22+. Rebuilds fills into round trips; has **tags and "playbooks" (named setups with rule checklists, scored per trade, followed-vs-broken performance)**. Syncs 20+ brokers including Robinhood *Crypto* only; "option-contract history is unsupported for MAE/MFE". Self-described early release, schema may change.
- [GeneBO98/tradetally](https://github.com/GeneBO98/tradetally) — Apache-2.0, ~347 stars, **PostgreSQL + Docker**, Node 24+. Options and futures supported. Imports Schwab, ThinkorSwim, IBKR, E*TRADE, Lightspeed, ProjectX; **Robinhood not listed**. Needs a Finnhub key for quotes.
- [Snoe0/rrmetrics](https://github.com/Snoe0/rrmetrics) — Apache-2.0, 5 stars, single SQLite file, no login, `localhost:8459`. Syncs Tradovate, TopstepX, Webull, **Robinhood**. Options support not stated. Tracks "strategy rules" per trade and day; no per-tag hit rate described.
- Spreadsheets: free Excel/Sheets templates are abundant ([OptionsTradingIQ](https://optionstradingiq.com/the-best-option-trading-journal-spreadsheet/), [MarketXLS](https://marketxls.com/blog/options-trading-log-excel-template-free)); a free Google Sheets add-on "Premium Tracker" targets covered calls / CSPs ([Workspace Marketplace](https://workspace.google.com/marketplace/app/premium_tracker/87170287711)).

**Robinhood data access (critical for the owner):**

- Official: Robinhood's help center says you can generate a custom **account activity report** downloadable as CSV covering "all transactions within investing and retirement accounts"; generation takes ~2 hours, up to 24; futures, crypto, and spending activity excluded ([Robinhood support](https://robinhood.com/us/en/support/articles/finding-your-account-documents)). A 2022 Tradervue post said no CSV existed then ([Tradervue, Sep 2022](https://www.tradervue.com/site/2022/09/16/robinhood-csv)), so this is a newer feature; the exact column layout for options rows is unverified here.
- Unofficial: [robin_stocks](https://pypi.org/project/robin-stocks/1.7.1) (Python, undocumented API, needs login + TOTP) exposes `export_completed_option_orders`. A Chrome extension scrapes the history page and warns its "data may be incorrect" ([Chrome Web Store](https://chrome.google.com/webstore/detail/robinhood-history-downloa/cfpnhaccjfaaianelddfckidjlnmoeab)).

### What people complain about

- **Cost and bloat.** TraderSync is "super complex with a rather poor UX" per Trustpilot aggregations, and reviewers call it "bloated" with unused backtesting ([DayTradingToolkit comparison](https://daytradingtoolkit.com/comparisons/tradersync-vs-tradervue)). A 2022 r/RealDayTrading thread splits between "I use an excel spreadsheet. Does all I need" and people who pay because they refuse manual entry ([r/RealDayTrading, Aug 2022](https://redlib.hackliberty.org/r/RealDayTrading/comments/wp8xql/paying_for_a_journal)). The sub's TraderSync endorsement carries a 50% referral link ([r/RealDayTrading, Feb 2022](https://redlib.hackliberty.org/r/RealDayTrading/comments/si71em/tradersync)).
- **Billing.** Tradervue has "documented billing complaints" and charges after cancellation ([DayTradingToolkit](https://daytradingtoolkit.com/reviews/tradervue-review)).
- **Options legs split apart.** A general guide warns that "several otherwise capable tools treat each leg as a separate trade and break your strategy-level analytics" ([StockBrokers.com guide](https://stockbrokers.com/guides/best-trading-journals)); TradesViz's own IBKR guide tells users to enable "Attempt advanced grouping of options", implying grouping is not automatic ([TradesViz IBKR](https://www.tradesviz.com/brokers/InteractiveBrokers)). A wheel-strategy vendor complains Tradervue "treats every trade as an independent event" ([QuantWheel, vendor](https://quantwheel.com/learn/quantwheel-vs-competitors/)).
- **Broker sync fragility.** TradeZella users report "Schwab API token expiry requiring frequent re-authentication" ([LuxAlgo review](https://www.luxalgo.com/blog/tradezella-review-journaling-and-backtesting-platform/)).
- **Journals record what, not why.** "Most retail trading journals track what happened (entry, exit, P/L) but not why" ([Traders Second Brain](https://traderssecondbrain.com/guides/trading-setup-performance-breakdown)); Babypips: "you won't even know how much potential profits you've lost unless you've tracked them on a journal" ([Babypips](https://www.babypips.com/trading/how-to-minimize-missed-trades)).

### Failed or abandoned attempts

- Edgewonk abandoned its **local, one-time-purchase** model (Edgewonk 2 stored data locally) for a cloud subscription ([launch post](https://edgewonk.com/blog/edgewonk-3-launch)). The one local-first commercial option moved to the cloud.
- Splice-style collapse of free tiers: TalkingParents (area C) and Tradervue both tightened free tiers; Tradervue's free tier is now stocks/ETFs only ([DayTradingZ](https://daytradingz.com/tradervue-review)).
- Searches for shut-down trading-journal startups, Show HN posts, and Indie Hackers post-mortems returned **nothing specific** — only active launches ([Indie Hackers investlog](https://www.indiehackers.com/product/investlog)). Absence noted; the category seems to churn via small apps quietly going stale rather than announced shutdowns (scout's inference).

### Communities

- r/options ~1.4M, r/thetagang ~332–342k, r/Daytrading ~5.1M, r/algotrading ~1.9M members (third-party trackers, Jun–Jul 2026 snapshots; [GummySearch r/thetagang](https://gummysearch.com/r/thetagang/), [r/options](https://gummysearch.com/r/options/)). r/RealDayTrading maintains a wiki recommending journals. NexusFi hosts a journals directory ([NexusFi](https://nexusfi.com/d/journals/tradersync)). Tone: skeptical of subscriptions, affiliate-aware, spreadsheet-proud.

### What is clearly missing (scout's inference)

1. **No tool links a *screener hit* to a *decision* to an *outcome*.** Every product above starts at the fill. Nothing ingests "the screener flagged X at 9:40" and later asks "did you take it, and what happened to X anyway?" The search for this workflow returned only education blogs saying you *should* do it by hand.
2. **No local-first options journal with a Robinhood path.** LuxAlgo has playbooks but weak options; TradeTally has options but Postgres/Docker and no Robinhood; rrmetrics has Robinhood but is a 5-star project with unstated options support.
3. **No "is my pipeline working" scorecard.** Hit rate by *signal* (not by trade tag) over rolling windows is absent everywhere; the owner's weekly email report is already closer to this than any product.
4. Everything is cloud + account + subscription; the one local product (Edgewonk 2) was retired.

---

## B. Email as a failed system of record

### What exists

- **SaneBox**: server-side, works via IMAP/OAuth on any provider; ~$7–$9/mo entry, 14-day trial, no free plan; claims header-only analysis ([Dupple, Aug 2026](https://dupple.com/reviews/sanebox), [Asian Efficiency, Jul 2026](https://www.asianefficiency.com/email-management/sanebox-review/)).
- **Superhuman** ~$30/user/mo Starter ([Carly](https://www.usecarly.com/blog/superhuman-review/)); **Shortwave** Free / Pro $18 / Business $30 ([CostBench](https://costbench.com/compare/shortwave-vs-superhuman/)); **HEY** $99/yr flat, does not import Gmail ([TechCrunch](https://techcrunch.com/?p=2003191)).
- **Unroll.me / Clean Email**: Unroll.me sold inbox data to Uber (2017) and settled with the FTC in 2019 ([FTC statement](https://www.ftc.gov/system/files/documents/public_statements/1539865/phillips_-_unrollme_statement_8-8-19.pdf), [BetaNews](https://betanews.com/article/unroll-me-sells-user-data)).
- **Open source**: [elie222/inbox-zero](https://github.com/elie222/inbox-zero) (~11.7k stars, self-hostable, AI rules, "open source alternative to Fyxer"); [MailDigest](https://alternativeto.net/software/maildigest/about/) (MIT, IMAP digest, works with local models or no model); Thunderbird [Ollama Summarizer](https://services.addons.thunderbird.net/FA/thunderbird/addon/ollama-summarizer); Gmail's own filters/labels and Gmail-IMAP.

### What people complain about

Price and lock-in: "$30/month cost is the top complaint" for Superhuman, with cancellation friction ([Carly](https://www.usecarly.com/blog/superhuman-review/)); "I don't like giving access to third parties" for SaneBox ([Capterra](https://capterra.com/p/146115/SaneBox/)); Clean Email has auto-renewal billing complaints ([Leave Me Alone, competitor](https://leavemealone.com/blog/unroll-me-vs-clean-email/)).

### Failed or abandoned attempts

**Mailpile** (8.8k stars, AGPL) — README: "Development on this codebase has halted, until the Python3 rewrite has completed" ([GitHub](https://github.com/mailpile/Mailpile)). A local-first mail client with a decade of effort stalled on a rewrite. Unroll.me survived its scandal only by changing its disclosures.

### Communities

r/productivity, r/gmail, r/selfhosted (inbox-zero and MailDigest surface there); Hacker News reliably upvotes local-mail tools (scout's inference from Mailpile's star count).

### What is clearly missing (scout's inference)

A **read-only local digest** over Gmail IMAP that never writes to the mailbox and needs no cloud account. inbox-zero is the closest but is a full AI assistant with a hosted-first posture. The real blocker is **Google OAuth**: an app in "Testing" status gets refresh tokens that die after 7 days ([Unipile](https://www.unipile.com/google-oauth-refresh-token/), [gmail-tester issue #73](https://github.com/levz0r/gmail-tester/issues/73)); "In production" apps must complete verification for sensitive/restricted scopes ([Google dev forum](https://discuss.google.dev/t/oauth2-refresh-token-expiration-and-youtube-api-v3/160874)). For a one-user local tool, **Gmail IMAP with an app password** sidesteps OAuth entirely and is the boring path.

---

## C. Household / school logistics by email

### What exists

- **Cozi**: free with ads; Gold ~$29.99–$39/yr; shared colour-coded calendar; acquired by OurFamilyWizard May 2022 ([MakeTechEasier](https://maketecheasier.com/?p=458416), [Dealroom](https://app.dealroom.co/companies/the_ourfamilywizard_website)).
- **OurFamilyWizard**: from $99/parent/yr, co-parent included free; court-oriented ([OFW pricing](https://www.ourfamilywizard.com/pricing)). **TalkingParents** ended its free tier 30 Mar 2026; $7/$16/$32/mo ([SeniorSimple, 2026](https://seniorsimple.org/content/best-co-parenting-apps-2026)). **FamilyWall** Premium $7.99/mo or $44.99/yr (App Store snapshot 15 May 2026, [AppPricingLab](https://www.apppricinglab.com/iap/apple/496889629)). Google Family auto-creates a shared "family" calendar.
- **Email-to-calendar for families** is a live category: FamilyHero (Gmail/iCloud connect), Nori (forward-to-address), Kindrly, Plancove (iOS), Sense, Ohai, plus an [n8n workflow](https://n8n.io/workflows/11529-organize-school-emails-with-ai-google-calendar-and-drive-auto-triage-system/) ([Nori](https://heynori.com/appfeatures/email-to-calendar), [Plancove](https://www.producthunt.com/products/plancove), [FamilyHero](https://alternativeto.net/software/familyhero/about)).
- Schools consolidating on **ParentSquare** (acquired Remind; 22M students claimed) ([MCPS](https://www.montgomeryschoolsmd.org/news/mcps-news/2025/08/communication-platforms/)).

### What people complain about

"Volume of notifications can be overwhelming, leading to information fatigue" ([G2 ParentSquare](https://www.g2.com/products/ParentSquare/reviews)); "ridiculously difficult to get rid of alert notifications" ([App Store](https://apps.apple.com/app/id908126679)); Cozi "doesn't offer tools like messaging, document sharing" and ads are "a bit annoying" ([MakeTechEasier](https://maketecheasier.com/?p=458416)).

### Failed or abandoned attempts

None found specifically; several email-to-calendar entrants are pre-launch or waitlist (School Calendar Assistant) — a crowded, early, VC-style field (scout's inference).

### Communities

r/coparenting, r/Parenting, r/daddit; school-district FAQ pages are where most troubleshooting lives.

### What is clearly missing (scout's inference)

All email-to-calendar tools are **cloud, account-based, and ask for Gmail access or forwarding to a third party**. Nothing runs locally, and co-parent sharing always routes through the vendor. The genuine gap is narrow: a local parser producing an `.ics` the co-parent can subscribe to.

---

## D. Work-travel expense preparation

### What exists

Expensify (free individual tier, ~25 SmartScans/mo; Collect ~$5, Control ~$9/user/mo) ([CostBench, Jun 2026](https://costbench.com/software/expense-management/expensify/free-plan)); SAP Concur (enterprise, employer-mandated; email-in receipts) ([Concur community](https://community.concur.com/t5/Concur-Expense-Forum/Forwarding-receipts-to-Concur/m-p/115200)); Ramp (free, business card platform) ([CostBench](https://costbench.com/software/expense-management/ramp/free-plan)); open source: [Receipt Wrangler](https://selfhostyourself.com/services/receipt-wrangler) (AGPL, Docker, OCR+AI), [Paperless-ngx](https://akashrajpurohit.com/blog/selfhost-paperless-ngx-for-document-management/) (GPL; OCR but "Total is a custom field").

### What people complain about

Forwarded receipts vanish: "When they forwarded Uber receipts this way, the receipts vanished... workaround was printing a PDF" ([Concur community](https://community.concur.com/t5/Concur-Expense-Forum/Entering-Uber-receipts-by-email/m-p/33036)); emailed receipts not appearing ([Concur](https://community.concur.com/t5/Concur-Expense-Forum/Emailed-Receipts-Not-Appearing-in-Concur/m-p/100970)); Concur accepts only **one profile per verified email**, so personal-address forwards fail unless verified ([Concur](https://community.concur.com/t5/Concur-Expense-Forum/Forwarding-receipts-to-Concur/m-p/115200)); Expensify requires the personal address as a secondary login ([Expensify community](https://community.expensify.com/discussion/comment/11054/)).

### Failed or abandoned attempts

None found. Field-service-specific capture is only an "idea" page ([Trend-Seeker](https://trend-seeker.app/ideas/managed-expense-capture-and-job-cost-bookkeeping-for-small-field-crews)).

### Communities

Concur/Expensify vendor forums; r/sysadmin and r/fieldservice tangentially.

### What is clearly missing (scout's inference)

Nothing structural: the owner's real fix is **verifying the personal address in the employer's Concur/Expensify profile** so forwards land. A pre-submission staging folder is a weekend script, not a product. The employer tool is mandatory, so anything built is a feeder, which caps pain removed.

---

## E. Personal music-production catalog

### What exists

Sample managers: Sononym ~$99 ([Puremix](https://www.puremix.com/blog/jimmy-douglass-the-importance-of-good-samples)), ADSR Sample Manager free with credits ([ADSR](https://www.adsrsounds.com/samples/pricing/)), Loopcloud from €6.99/mo ([Dubspot, Jun 2026](https://blog.dubspot.com/best-sample-pack-sites-2026)), XO ~€129 — **all built for samples, not WIPs**. Self-hosted libraries: Navidrome (lightweight, "relies entirely on metadata"), Jellyfin, beets (MusicBrainz tagging; "a bit complex for a CLI tool", poor for "singles and mixes") ([selfhosting.sh](https://selfhosting.sh/best/music-streaming/), [Lemmy](https://lemmy.world/comment/24549803)). Beat-selling catalogs: SendBeatsTo, BeatStars, Taskade templates.

### What people complain about

"Hunting through 10 different versions of 'fire_beat_34_final_final.als'" ([Gearspace](https://gearspace.com/board/life-hacks/1445960-how-organize-your-daw-projects-like-pro-step-step-guide.html)); advice is folder-per-beat and spreadsheet catalogs with BPM/key/mood ([HipHopMakers](https://hiphopmakers.com/organize-your-beats)).

### Failed or abandoned attempts

**Splice Studio** (free DAW-project version control, 2014) shut down May 2023; "hadn't been a focus since 2017" ([Sonicstate](https://sonicstate.com/news/2023/03/10/splice-to-end-studio-feature-/), [MusicRadar](https://musicradar.com/news/splice-studio-closing)). **Blend.io** (2013, Ableton-centric collaboration) and **Endlesss** closed May–June 2024 after ROLI acquisition ([CDM](https://cdm.link/2024/05/endlesss-discontinued/)). Three version-control-for-music products died; all were cloud-hosted.

### Communities

r/makinghiphop, r/WeAreTheMusicMakers, Gearspace, KVR, Elektronauts.

### What is clearly missing (scout's inference)

A local index of **one's own WIPs** (bounces + project files + collaborator folder) with version lineage. Sample managers ignore it; Navidrome/beets assume finished, tagged releases; the cloud attempts all died. Real gap, but episodic use.

---

## Cross-cutting observations

1. **The whole field is cloud-first.** Across all five areas, the only local-first commercial tool found (Edgewonk 2) moved to the cloud; the local-first open-source efforts (Mailpile, Splice Studio, Blend) stalled or died. Local-first is a genuine differentiator precisely because it is commercially unattractive (scout's inference).
2. **Free tiers are contracting.** Tradervue (options removed from free), TalkingParents (free tier ended Mar 2026), Expensify (plan removed Jun 2026), TradeZella (new tiers 2026).
3. **Review ecosystems are polluted.** Most "reviews" of trade journals are competitor pages or affiliate sites; r/RealDayTrading's own recommendation is a referral link. Sellability signals should be read from GitHub stars and forum tone rather than review scores.
4. **The owner already owns the hard part for A.** The screener thesis and daily reports exist; the missing piece is a durable store and a join. That is the rarest shape in this landscape: nobody sells "screener → decision → outcome".

## Constraints on what can be built

- **Brokerage data**: Robinhood has no public API; the official path is a CSV activity report generated on demand (2–24 h) with options rows of unverified layout ([Robinhood](https://robinhood.com/us/en/support/articles/finding-your-account-documents)). Unofficial `robin_stocks` requires storing credentials and breaks without notice. Design for **paste/CSV first**, as the interview already demands.
- **Gmail**: OAuth in Testing status expires tokens weekly; production requires scope verification. Use **IMAP + app password** or Gmail's native "export via Takeout"; avoid OAuth for a local tool.
- **Honesty rule**: anything in A must compute hit rates from the owner's own entries and label them "based on your entries". No forward-looking language.
- **Co-parent sharing (C)**: any shared artifact leaves the owner's machine; `.ics` subscription or a copied file are the only account-free options.
- **Employer lock-in (D)**: Concur/Expensify are mandatory; a tool can only stage, never submit.
- **Licensing**: LuxAlgo trade-journal is MIT (reusable); TradeTally/rrmetrics Apache-2.0; Mailpile and Receipt Wrangler are AGPL (copyleft, avoid vendoring).
