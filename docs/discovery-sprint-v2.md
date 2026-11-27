# Discovery Sprint v2: Open Field

Date: 2026-11-27. This replaces the ranking in `discovery-sprint.md`. That first pass treated the operator profile as a boundary. This one ignores it when choosing spaces and uses it only as a tiebreaker.

The kickoff prompt for the #1 pick is in [`kickoff-prompt-parity.md`](kickoff-prompt-parity.md).

## The trade you are making

Your original brief asked for a moat built on your own domain access. Dropping that constraint opens bigger markets and removes most of the defensibility. None of the eight ideas below has a strong moat on day one. What separates them is whether demand is forced, whether the work is repeatable, and whether the output can be verified.

## What the second scan found

**The pattern that is working for solo operators is selling the outcome of a service, not a seat of software.** The clearest example I found: a designer who used agents to go from 3–4 concurrent clients to 15–20 and from $150K to $720K in a year (single newsletter source). The same sources say distribution, not building, is the bottleneck, and that agents are eroding seat-based pricing for thin tools.

**Regulatory shocks create short, lucrative windows, and you have to be early.** The IEEPA tariff refunds are the cautionary case. $166B became refundable through a new CBP system on 2026-04-20, small importers were failing the process, and a prep tool would have been the best idea of the year in April. By now the window is mostly shut: finally liquidated entries are refundable only to importers with their own court order, and the 180-day protest clock on many entries runs out this week.

**One forced-demand window is still open.** The ADA Title II web rule requires state and local government web content, including PDFs, to meet WCAG 2.1 AA. An April 2026 interim rule moved the deadlines to April 26, 2027 for entities of 50,000+ and April 26, 2028 for smaller ones. Human remediation runs $4–$30 per page. One university estimates over $1M a year at the cheapest rates for its 200,000 annual PDFs. Caveat: DOJ has signaled the rule may change again, and it has slipped once already.

**Legacy desktop apps are a growing, unglamorous backlog.** One migration vendor's inquiry data shows Microsoft Access requests up 6.3x and VB6 up 4.7x (vendor data, small base, not a census). Visual FoxPro has been unsupported since 2015. Incumbents (GAPVelocity, Wizmo, WSI) are tool-plus-services firms aimed at .NET shops; I found no published small-business pricing.

**AI agent security testing is priced for enterprises.** Engagements are quoted at $6K–$45K, big consultancies at $40K–$150K+, and platforms are almost all quote-only. Indirect prompt injection is described as the most common production compromise. A fixed-price tier for small teams shipping agents is thin.

**Spaces I checked and found taken:**

| Space | Why not |
|---|---|
| Insurance denial appeals | Counterforce Health is free; Claimable charges about $50 |
| Local-government meeting intelligence | Hamlet, Quorum, FiscalNote and Prophecy already parse tens of thousands of meetings |
| Pre-RFP buying signals | Starbridge, Pursuit, GovSpend; only the small-vendor price tier is open |
| Small-business acquisition diligence | LoanBud plus a stream of beta tools on Searchfunder |
| AI home inventory for insurance | At least eight App Store apps, $40/yr price ceiling |
| Construction paperwork | Procore, Fieldwire, Jobber and many AI entrants |

---

## The eight ideas

### #1 Parity — agentic service with its own tooling
**Pitch:** Fixed-price migration of a business's Access, Excel or VB6 application to a hosted web app, with a report proving the new system returns the same answers as the old one.

- **Who pays:** Small and mid-sized businesses whose operations run on a desktop database one person understands. Often triggered by that person retiring, a Windows or Office upgrade, or a need for remote access.
- **Why the model is the unlock:** Scoped code generation is the best-evidenced strength of current Claude models. Reading VBA and saved queries, writing a spec, and producing a conventional web app is exactly that. The part models are bad at, knowing when they are done, is handled by a deterministic parity harness.
- **MVP / first dollar:** 4–6 weeks to a toolkit that fully migrates a sample database. First paid job 6–10 weeks, depending on how fast you find one owner.
- **Price:** $8K–$40K per application, plus $200–$500 a month for hosting and changes. Unvalidated; nobody in this market publishes prices.
- **Biggest risk:** Every job hides undocumented business rules, and you support what you ship. Finding owners is the real work; the realistic channels are managed service providers, bookkeepers and retiring Access consultants.
- **Moat:** Weak at first. It grows from the harness, a library of translation patterns, and recurring hosting revenue.

### #2 Tripwire — agentic service plus CLI
**Pitch:** A fixed-price security assessment for small teams shipping AI agents: prompt injection, tool abuse, data leakage, with a regression suite they keep.

- **Who pays:** Startups and small software teams whose customers or insurers are asking how their agent was tested.
- **Why the model is the unlock:** Generating and adapting attack cases against a specific agent's tools, and reading transcripts to judge whether an attack landed.
- **MVP / first dollar:** 3–4 weeks; first engagement inside two months.
- **Price:** $3K–$8K per assessment, $300–$1,000 a month for continuous testing.
- **Biggest risk:** Crowded with funded vendors moving down-market, and the model providers keep shipping their own safeguards. Authorized testing only, under a signed scope. This model carries extra safeguards on dual-use security work and may decline some tasks.
- **Moat:** Low. Reputation and a private attack corpus.

### #3 ClearDocs — web app
**Pitch:** Crawl a city or school district's website, show them how many PDFs they must fix before the ADA deadline, and fix them for a fraction of the per-page rate.

- **Who pays:** Small governments, school districts and special districts facing April 2027 or April 2028.
- **Why the model is the unlock:** Reading order, table structure and image descriptions need visual understanding; automated checkers reportedly catch only 20–30% of issues. The larger saving is triage: deciding what to archive, delete or convert to a web page instead of remediating.
- **MVP / first dollar:** 2–3 months to a credible pipeline; government purchasing adds months.
- **Price:** $1–$3 per page against a $4–$30 market, plus a monthly plan for new documents.
- **Biggest risk:** The rule may be weakened or delayed again. Writing correct PDF tag structures is technically hard, and real quality control needs screen-reader expertise you would have to hire or learn. AI entrants already exist (accessiBe, Venngage, Equidox).
- **Moat:** Moderate. The buyer list is public, and a free audit of their own site is a strong sales tool, but competitors can do the same.

### #4 Witness — developer tool
**Pitch:** An independent checker that re-runs the acceptance criteria on AI-built software and reports where the agent faked it.

- **Who pays:** Agencies and businesses commissioning AI-built code who cannot audit it themselves.
- **Why now:** One long-horizon benchmark reported a 13.8% reward-hacking rate. Buyers have no way to see it.
- **MVP / first dollar:** 4–6 weeks; 3+ months to revenue.
- **Price:** $50–$300 a month, or per-audit.
- **Biggest risk:** The model vendors and evaluation platforms will build this in. Developers rarely pay for tools from unknown solo vendors.
- **Moat:** Low.

### #5 DealProbe — web app
**Pitch:** First-pass diligence on a small-business acquisition from the seller's documents.

- **Who pays:** Individual buyers using SBA loans, $200–$1,000 per deal.
- **Biggest risk:** LoanBud and a line of beta tools already do it, and a wrong answer costs someone their savings.
- **Moat:** None.

### #6 BidEarly — data feed
**Pitch:** Pre-RFP alerts for one narrow vendor category, priced for firms that cannot afford the enterprise platforms.

- **Who pays:** Small vendors selling to local government, $100–$300 a month.
- **Biggest risk:** Starbridge tracks 320,000 entities and could add a cheap tier any day.
- **Moat:** None beyond picking a niche too small for them to bother with.

### #7 RefundPrep — web app
**Pitch:** Tariff refund claim preparation for small importers.

- **Status:** Included as the lesson, not a recommendation. Right idea, seven months late. Filing for others also requires a licensed customs broker.

### #8 Roomscan — mobile app
**Pitch:** Video walkthrough to itemized home inventory for insurance claims.

- **Biggest risk:** Saturated, consumers pay about $40 a year, and it needs paid acquisition.
- **Moat:** None.

---

## Scores

Same weights as before: Usefulness 15%, Marketability 15%, Profitability 20%, Buildability 10%, Defensibility 15%, Time-to-MVP 10%, Operator edge 15%. Operator edge now means general technical fit, not domain history. Scores are judgment, not measurement.

| Rank | Idea | Use | Mkt | Profit | Build | Defend | Time | Edge | **Weighted** |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Parity (legacy app migration) | 5 | 3 | 5 | 4 | 2 | 4 | 4 | **3.90** |
| 2 | Tripwire (agent security testing) | 4 | 4 | 4 | 4 | 2 | 4 | 4 | **3.70** |
| 3 | ClearDocs (PDF accessibility) | 5 | 4 | 4 | 3 | 3 | 3 | 3 | **3.65** |
| 4 | Witness (AI-build auditor) | 4 | 3 | 3 | 4 | 2 | 4 | 3 | **3.20** |
| 5 | DealProbe (acquisition diligence) | 4 | 3 | 3 | 4 | 1 | 4 | 2 | **2.90** |
| 6 | BidEarly (pre-RFP feed) | 3 | 3 | 3 | 4 | 2 | 3 | 2 | **2.80** |
| 7 | RefundPrep (tariff refunds) | 4 | 2 | 2 | 4 | 1 | 4 | 2 | **2.55** |
| 8 | Roomscan (home inventory) | 4 | 3 | 2 | 3 | 1 | 3 | 1 | **2.35** |

For comparison, the first pass's top pick scored 4.40 on the same weights, almost entirely from Defensibility and Operator edge. That gap is the cost of leaving your own territory. It is a legitimate choice; it should be a deliberate one.

## Recommendation

**Start Parity.** Three reasons:

1. It uses the capability with the strongest independent evidence, and the harness covers the model's main weakness.
2. It depends on no regulator, platform or deadline.
3. One job pays more than a year of most subscriptions on this list.

**ClearDocs is the better product and the worse bet.** It is more repeatable and has a deadline forcing demand, but it hinges on a rule DOJ is openly reconsidering and on accessibility expertise you would be acquiring from zero.

**Tripwire is the fastest to a first invoice** if you want cash before committing to a build.

## Sources

Solo-operator patterns: [Demand Curve newsletter](https://gp.demandcurve.com/newsletters/growth-newsletter-328) · [Redwerk](https://redwerk.com/blog/micro-saas-ideas-that-print-money/)

Tariff refunds: [CBP IEEPA duty refunds](https://www.cbp.gov/trade/programs-administration/trade-remedies/ieepa-duty-refunds) · [US Chamber FAQ](https://www.uschamber.com/economy/tariff-refunds-faq-what-small-businesses-need-to-know-after-supreme-courts-ruling) · [Frazier & Deeter CAPE guide](https://www.frazierdeeter.com/insights/article/how-to-file-for-your-ieepa-tariff-refund-through-the-cape-system/) · [CAPE Phase 3](https://strixsmart.com/resources/blog/cape-phase-3-liquidated-entries) · [Refund tracker](https://attorneywu.com/ieepa-refund-tracker/) · [Cato](https://www.cato.org/blog/ieepa-refunds-update-good-progress-still-ways-go) · [Maryland Matters](https://marylandmatters.org/2026/04/20/repub/show-me-the-money-businesses-line-up-for-166-billion-in-refunds-from-trumps-illegal-tariffs/)

ADA Title II and PDF remediation: [Federal Register interim final rule](https://www.govinfo.gov/content/pkg/FR-2026-04-20/html/2026-07663.htm) · [UNC School of Government](https://canons.sog.unc.edu/blog/2026/04/20/deadlines-to-comply-with-ada-web-accessibility-requirements-extended-by-one-year/) · [League of Minnesota Cities](https://www.lmc.org/news-publications/news/all/doj-extends-web-accessibility-compliance-deadlines-for-local-governments/) · [ADA.gov rule page](https://www.ada.gov/title-ii-web-rule/) · [Venngage cost guide](https://venngage.com/blog/pdf-accessibility-cost/) · [Harvard vendor list](https://accessibility.huit.harvard.edu/doc-vendors) · [UIC Equalify sources](https://equalify.uic.edu/EqualifyEverything/equalify-docs/blob/main/reflow/sources.md) · [Equidox on pricing](https://equidox.co/blog/pdf-remediation-pricing-am-i-getting-a-good-deal/) · [Accessible.org pricing](https://accessible.org/pricing/)

Legacy migration: [GAPVelocity legacy code report](https://www.gapvelocity.ai/blog/legacy-code-research-report) · [GAPVelocity Access migration](https://www.gapvelocity.ai/migrate/ms-access) · [Wizmo FoxPro services](https://www.barchart.com/story/news/35566581/wizmo-inc-announces-microsoft-foxpro-modernization-services-for-missioncritical-legacy-apps) · [WSI](https://www.winningsolutionsinc.com/Referrer-Blog/NET-Articles2.aspx)

Agent security: [SecurityWall audit cost](https://securitywall.co/blog/llm-security-audit-cost) · [Aona red-teaming cost](https://aona.ai/blog/ai-red-teaming-cost-roi-enterprise-budget-2026/) · [Startup Defense playbook](https://startupdefense.io/blog/agentic-ai-red-teaming-startup-playbook) · [Mend services roundup](https://www.mend.io/blog/best-ai-red-teaming-services-top-6-services)

Spaces found taken: [PYMNTS on appeals](https://www.pymnts.com/artificial-intelligence-2/2026/insurance-denials-meet-their-match-in-ai-powered-appeals/) · [Counterforce Health](https://en.wikipedia.org/wiki/Counterforce_Health) · [Semafor on Hamlet](https://www.semafor.com/article/10/21/2026/data-center-firms-monitor-city-councils-meetings-to-track-local-sentiment) · [Quorum Local](https://www.quorum.us/products/local-government/) · [Starbridge signal engine](https://starbridge.ai/blog/pre-rfp-signal-engine) · [Pursuit on Starbridge](https://www.pursuit.us/blog/starbridge-alternatives-and-competitors) · [LoanBud](https://briefglance.com/articles/loanbud-launches-ai-platform-to-de-risk-small-business-acquisitions) · [NerdWallet home inventory apps](https://www.nerdwallet.com/insurance/homeowners/learn/home-inventory-app-template) · [NBC San Diego on Bevel](https://www.nbcsandiego.com/nbc-7-responds-2/new-app-uses-ai-to-help-people-track-their-belongs-in-case-of-a-disaster-heres-how/3905836/) · [Fieldwire guide](https://www.fieldwire.com/blog/construction-document-management-software/)

Model capability evidence is in the sources section of `discovery-sprint.md`.
