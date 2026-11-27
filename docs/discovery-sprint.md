# Discovery Sprint: What to Build With a Fable-Class Claude

Date: 2026-11-27. Nothing here is built yet. The kickoff prompt for the #1 pick is in [`kickoff-prompt-chainproof.md`](kickoff-prompt-chainproof.md).

## Bottom line

Start **ChainProof**: a sanitization-evidence and certificate-of-destruction system for your own data-destruction company first, sold to other small destruction/ITAD shops second. It is the only idea where you are simultaneously the first user, the first paying customer's vendor, and the distribution channel. Sell **idea #2 (the sanitization-program service)** on top of it for the first dollar. Keep **idea #4 (counter-UAS test tooling)** alive as the high-upside option, but only after five buyer conversations.

Assumptions I made instead of asking (correct me if wrong; they change the ranking):

1. Your destruction company already issues certificates of destruction and has repeat clients in defense and healthcare.
2. You can put 8–12 hours a week into this alongside the day job.
3. Your employment agreement has not been checked for moonlighting or IP-assignment clauses. This matters most for idea #3.

---

## 1. Research notes

### 1.1 Where frontier Claude models actually have an edge

I found no independent benchmark of the specific model in this session, so the evidence below is for the Claude Opus line in 2026 and should be read as a proxy.

| Capability | What the evidence says | Product implication |
|---|---|---|
| Scoped code generation | Consistently at or near the top of agentic coding leaderboards (Snorkel: Opus 4.6 at 65.2%; WildClawBench: Opus 4.7 best of 19 models at 62.2%). | A solo operator can ship a real product with Claude Code. This is the strongest, best-evidenced edge. |
| Multi-hour autonomous builds | Still unreliable. SWE-Marathon reportedly topped out at 26% success with a 13.8% reward-hacking rate (secondhand summary); Cognition's own FrontierCode 2.0 has its best model at 45%; Mercor argues repo benchmarks overstate end-to-end product building. | Do not design a product that depends on unattended, unverified agent runs. Build in phases with tests and human checkpoints. |
| Harness sensitivity | Switching the harness alone moved one model by up to 18 points (WildClawBench). | The scaffolding (tests, acceptance criteria, tight task lists) matters as much as the model. Hence the detailed kickoff prompt. |
| Structured extraction from messy documents and images | Not benchmarked in what I found, but this is my direct experience of where these models are dependable: reading labels, PDFs, vendor logs, spreadsheets into a schema, then flagging what does not reconcile. | The core of ideas #1, #2, #3, #4. Always pair with a deterministic cross-check. |
| Long-document reasoning against a standard | Same caveat. Mapping evidence to control language is a good fit; the model drafts, a human signs. | Compliance drafting, never compliance attestation. |
| Price prediction | No evidence of durable edge, and none expected. | I rejected the "prediction engine" as pitched (see idea #6). |
| Audio generation | Claude does not generate audio. A "Suno generator" would be a wrapper around someone else's model and someone else's legal exposure. | Reframed as catalog operations (idea #5). |

### 1.2 Niche scan

**Data destruction / ITAD software.** Makor ERP and RazorERP are the named incumbents; both are full ERPs with quote-only pricing, and RazorERP's few public reviews are mixed. Blancco owns erasure software and its own reports. I found no published per-seat price anywhere in this category, which usually means a sales-led, mid-market focus and an underserved small-shop tier. The timing hook is real: NIST SP 800-88 Rev. 2 went final on 2025-09-26, superseded the 2014 revision, and shifted from technique tables to requiring a sanitization *program*. Vendor summaries say it recommends a per-device certificate recording method, technique, tool and version, verification and signature; I did not confirm that wording in the NIST PDF itself.

**CMMC.** Total Level 2 cost for a small contractor is quoted at $75k–$250k, and the government's own planning figure for assessment alone is about $102k. Media Protection is one control family among many, so nobody buys a platform for it, but every assessed contractor needs sanitization evidence. One caution: sources conflict on the timeline. Secureframe reports Phase 2 was paused on 2026-07-13; a Kiteworks page says C3PAO certification became mandatory on 2026-11-10. Do not build a business that depends on a CMMC deadline.

**HIPAA disposal.** The Security Rule requires disposal policies but mandates no method, so documentation of your reasoning is the defense. Disposal-specific federal fines I found are old (a $125k small-pharmacy settlement for 2012 conduct); recent enforcement centers on risk analysis. State AGs are more active (California, $5M against Quest in 2024, under state law). The fear is real but the enforcement drumbeat is weaker than vendors imply.

**Pharma validation.** Crowded and enterprise-heavy: ValGenesis, Kneat, GoVal, IQVIA SmartSolve, Part11 Solutions' AIValidator (which already generates URS/IQ/OQ and trace matrices). The one price point found is $40k–$120k for an IQ/OQ/PQ engagement, from a vendor. High value, slow sales, and incumbents are already shipping AI.

**Counter-UAS test and evaluation.** RTI's TruFlight benchmarks detection systems against flight truth data; URSA does telemetry analysis. Both aim at federal and military buyers. The new demand is state and local: FEMA's C-UAS grant program put $250M out for FY2026 (11 states plus the National Capital Region) and another $250M for FY2027, with up to 100% federal funding and no required match. Agencies apply through their State Administrative Agency. The FY2027 deadline (2026-11-16) has passed. These buyers are acquiring detection gear with little ability to verify it performs as sold.

**Music.** The ground is moving against pure AI generation: Beatport bans wholly or substantially AI-generated tracks, Deezer reported fully AI tracks above 50% of daily uploads at a June 2026 peak, Udio's label deals restrict downloads, and Universal and Sony are still suing Suno. BeatStars registered its catalog to block AI training. I found no BeatStars rule on selling AI-made beats. Provenance ("human-made, documented") is becoming the scarce thing.

**Trading journals.** Saturated and already AI-equipped: TraderSync $29.95–$79.95/mo with an AI coach, TradeZella from $29/mo, plus TradesViz and Tradervue.

**KDP.** AI-generated content must be disclosed; a reported cap of 3 uploads a day is unverified. Volume plays are dead. Expertise-led titles still work but pay slowly.

**SMB network assessment.** OT/asset-inventory tools are enterprise and quote-priced (Dragos, Claroty, Nozomi, Lansweeper); EmberOT is the one visible price at $149/mo. The MSP assessment-report space is crowded. The only wedge is distribution you already have.

---

## 2. The eight ideas

### #1 ChainProof — web app
**Pitch:** Photo-to-certificate chain of custody for small data-destruction shops, built to the 800-88 Rev. 2 program model.

- **What / who pays:** A technician photographs each drive at intake on a phone. The system reads serial, model and media type, logs every custody event in a tamper-evident chain, ingests erasure-tool logs, reconciles against the client's asset list, and issues a signed per-device certificate with a public verification link. Payers: you (labor saved, higher-priced jobs), then other small destruction and ITAD operators.
- **Why the model is the unlock:** Reading worn, inconsistent drive labels and reconciling them against a client's messy spreadsheet is exactly the extraction-plus-exception-finding work these models do well and rule-based OCR does badly. Claude Code also makes a solo build realistic.
- **Unfair advantage:** You run the workflow daily, own real job data to test against, and can show buyers a working shop rather than a demo.
- **MVP / first dollar:** 4–6 weeks to use on your own jobs. First dollar inside that window by pricing an "audit-ready evidence pack" into existing jobs. First external subscription: 4–6 months.
- **Monetization:** $99–$299/mo per shop by volume tier, or per-certificate pricing for low-volume shops. Unvalidated; the category publishes no prices to anchor against.
- **Biggest risk:** Small market (my guess is low thousands of US shops; I have no verified count) and owners who are happy with Word templates. A misread serial on a legal certificate is a liability, so barcode cross-check and human confirmation are mandatory.
- **Moat:** Operator credibility, the reconciliation workflow tuned on real jobs, and switching cost once a shop's certificate history and client portal live in it. Moderate, not deep.

### #2 Sanitization Program-in-a-Box — agentic service
**Pitch:** A fixed-price, two-week engagement that gives a small contractor or clinic a complete 800-88 Rev. 2 media sanitization program.

- **What / who pays:** Interview plus asset inventory in; out comes policy, procedures, a media-type decision matrix, record templates, and a mapping to the client's CMMC Media Protection controls or HIPAA disposal obligations. Payers: your existing clients' IT or compliance leads.
- **Why the model is the unlock:** Tailoring a document set to one organization's actual inventory and control language is days of consultant time and hours of model time, with you reviewing and signing.
- **Unfair advantage:** You already hold the client relationship and the destruction contract; this is an upsell, not a cold sale.
- **MVP / first dollar:** 2–3 weeks. Sell it before building it, to one existing client.
- **Monetization:** $1,500–$5,000 fixed price, plus recurring destruction work it locks in.
- **Biggest risk:** It is consulting, so it scales with your hours. You are drafting, not certifying; the engagement letter must say so.
- **Moat:** Low as a product, real as a bundle with #1 and your destruction service.

### #3 QualDraft — web app
**Pitch:** First-draft URS, IQ/OQ scripts and trace matrices for lab-instrument integrations, generated from vendor docs and config exports.

- **What / who pays:** Small validation consultancies and instrument integrators who bill per protocol.
- **Why the model is the unlock:** Long-document cross-referencing between requirements, configuration and test steps.
- **Unfair advantage:** You know how integrations actually fail in the field, which is what makes test scripts credible.
- **MVP / first dollar:** 3–4 months to MVP; 9–12 months to revenue given GxP buyer caution.
- **Monetization:** $300–$1,000/mo per consultant seat, or per-protocol pricing against $40k–$120k engagements.
- **Biggest risk:** Conflict of interest with your employer is the first problem, not a footnote. Read your agreement before spending an hour here. Second, incumbents already ship this.
- **Moat:** Domain depth is real, but you cannot use employer materials to build it, which removes most of it.

### #4 RangeTruth — standalone app, hardware-adjacent
**Pitch:** Acceptance testing for counter-drone detection systems: did the kit your agency bought actually see the drone?

- **What / who pays:** Ingests flight truth (ArduPilot/PX4/DJI logs, Remote ID captures from a receiver you build) and the detection system's exported logs, aligns them in time and space, and reports detection range, track continuity, latency and false alarms. Payers: state and local agencies spending FEMA grant money, and small vendors needing an independent test report.
- **Why the model is the unlock:** Every vendor's log format is different and undocumented. Writing and maintaining parsers, and drafting the test report, is where the model earns its keep. The metrics themselves must be deterministic code.
- **Unfair advantage:** C-UAS integration testing is your actual background, and you can build the receiver hardware. Few people have both.
- **MVP / first dollar:** 2–3 months for a tool that works on your own drone and receiver. 6–12 months to a paid test, because government buys slowly.
- **Monetization:** $5k–$20k per acceptance-test engagement; software license later.
- **Biggest risk:** Procurement cycles and credibility against RTI. Detection only: mitigation (jamming, takeover) is legally restricted and out of scope. Check export-control status of anything you ship, and use only public information, nothing from prior service or employers.
- **Moat:** The strongest of the eight if it lands: rare expertise, hardware, and a parser library that compounds.

### #5 PackForge — standalone app / content operations
**Pitch:** Turn a folder of your own stems into finished, tagged, licensed, listed beat packs.

- **What / who pays:** Detects BPM and key, names and organizes files, drafts license PDFs, listing copy and type-beat titles, and keeps a provenance record showing the work is human-made. You first; other producers at $15–$29/mo.
- **Why the model is the unlock:** Orchestrating audio analysis tools and writing the copy. Modest, honestly.
- **Unfair advantage:** A live storefront to test conversion on.
- **MVP / first dollar:** 2 weeks; first dollar is your own faster release cadence.
- **Biggest risk:** Producers pay little for tools, and the market is flooded. This is an internal efficiency tool with a small resale upside.
- **Moat:** Thin. Provenance documentation is the one timely angle.

### #6 Post-Mortem — web app (the reframed "prediction engine")
**Pitch:** An options journal that reconstructs multi-leg campaigns and audits them against your own written rules.

- **Reframe:** I am not proposing a prediction engine. There is no evidence these models forecast prices, and selling signals invites investment-adviser regulation. What a model can do is rebuild rolls and wheel campaigns from broker exports and tell you where you broke your own plan.
- **Who pays:** Retail options traders, $20–$40/mo.
- **MVP / first dollar:** 4–6 weeks; 3+ months to revenue.
- **Biggest risk:** TraderSync and TradeZella already do this with AI at $29–$80/mo and hundreds of broker integrations.
- **Moat:** None. Build it for yourself if you want it; do not build it to sell.

### #7 FieldManual Press — content generation
**Pitch:** A short series of practitioner handbooks from your own expertise, with Claude as researcher, editor and fact-checker.

- **What / who pays:** Titles like a media-sanitization handbook for small contractors, or Security+ lab workbooks. Readers pay $10–$30; the real return is lead generation for #1 and #2.
- **MVP / first dollar:** 3–4 weeks to a first title; royalties are slow and small.
- **Biggest risk:** Low income as a standalone. Disclose AI assistance per KDP policy.
- **Moat:** Your name and experience. Best treated as marketing, not a business.

### #8 NetBrief — standalone CLI
**Pitch:** Turn scan output from a network you are contracted to assess into an executive risk report mapped to cyber-insurance questionnaires.

- **What / who pays:** Takes nmap, config and inventory exports from an authorized engagement and produces prioritized findings. Payers: your existing small-business clients, as a $750–$2,500 assessment.
- **Why the model is the unlock:** Correlating raw findings into plain-language priorities.
- **Unfair advantage:** Distribution through destruction clients who already trust you with security.
- **MVP / first dollar:** 3–4 weeks; first dollar on the first assessment.
- **Biggest risk:** MSPs give assessments away to win contracts. Requires a signed authorization and scope for every engagement, enforced in the tool.
- **Moat:** Thin as software; it is a service line.

**Considered and dropped:** a "planner on steroids" (saturated, no connection to your edge), and a Suno wrapper (legal exposure you do not control, marketplaces turning against it).

---

## 3. Scores

Weights: Usefulness 15%, Marketability 15%, Profitability 20%, Buildability 10%, Defensibility 15%, Time-to-MVP 10%, Operator edge 15%. Scores are my judgment from the research above, not measurements.

| Rank | Idea | Use | Mkt | Profit | Build | Defend | Time | Edge | **Weighted** |
|---|---|---|---|---|---|---|---|---|---|
| 1 | #1 ChainProof | 5 | 4 | 4 | 5 | 4 | 4 | 5 | **4.40** |
| 2 | #2 Program-in-a-Box | 4 | 4 | 4 | 5 | 3 | 5 | 5 | **4.20** |
| 3 | #3 QualDraft | 5 | 2 | 5 | 3 | 4 | 2 | 4 | **3.75** |
| 4 | #4 RangeTruth | 4 | 3 | 4 | 3 | 4 | 2 | 5 | **3.70** |
| 5 | #8 NetBrief | 4 | 3 | 3 | 4 | 2 | 4 | 4 | **3.35** |
| 6 | #5 PackForge | 3 | 3 | 2 | 5 | 2 | 5 | 4 | **3.20** |
| 7 | #7 FieldManual Press | 3 | 3 | 2 | 5 | 2 | 4 | 4 | **3.10** |
| 8 | #6 Post-Mortem | 3 | 3 | 2 | 4 | 1 | 4 | 3 | **2.70** |

### Recommendation

- **Start #1 ChainProof.** It pays for itself inside your own company even if no one else ever buys it, which no other idea can claim.
- **Sell #2 alongside it.** Same rule engine and document templates; it is the fastest cash and it tests whether clients value the evidence.
- **Hold #4 as the option.** QualDraft outscores it by a hair, but I would rank RangeTruth ahead in practice: QualDraft's score ignores the employer conflict, which could zero it out. Before writing RangeTruth code, talk to five agencies or vendors about how they accept-test today.

---

## Sources

Fable-class capability: [Snorkel leaderboard](https://snorkel.ai/leaderboard/category/) · [WildClawBench summary](https://bayesiansapien.github.io/cere-bro/agentic-systems/2026-05-15-wildclawbench-native-runtime-agent-benchmark) · [SWE-Marathon summary](https://digg.com/ai/rpm6i3oc) · [Mercor SWE-Marathon-Ext](https://www.mercor.com/blog/swe-marathon-ext-can-ai-build-a-whole-saas-product/) · [FrontierCode 2.0](https://cognition.com/blog/frontier-code-2.0) · [SWE-AGI](https://arxiv.org/html/2602.09447v2)

Destruction / sanitization: [NIST SP 800-88 Rev. 2](https://csrc.nist.gov/pubs/sp/800/88/r2/final) · [Blancco on Rev. 2](https://blancco.com/resources/blog-nist-800-88-rev-2-updated-standard/) · [Vibrant on Rev. 2](https://vibrant.com/resources/are-you-nist-800-88-compliant-is-now-the-wrong-question/) · [BitRaser on Rev. 2](https://www.bitraser.com/article/nist-800-88-r2) · [Makor ERP on Capterra](https://www.capterra.com/p/184102/Makor-ERP/) · [RazorERP on Capterra](https://www.capterra.com/p/148503/RazorERP/) · [Securis certifications guide](https://securis.com/blog/the-ultimate-guide-to-it-asset-disposition-itad-certifications-and-compliance/) · [human-I-T on certificates](https://www.human-i-t.org/understanding-certificates-of-data-destruction/)

CMMC / HIPAA: [mind-core cost guide](https://mind-core.com/?p=48898) · [PreVeil](https://www.preveil.com/?p=16637) · [Petronella](https://petronellatech.com/compliance/cmmc-level-2-certification-cost/) · [Secureframe on the Phase 2 pause](https://secureframe.com/blog/roi-of-cmmc.md) · [Kiteworks](https://www.kiteworks.com/cmmc-compliance/cmmc-compliance-software-small-defense-contractors/) · [Beancount CMMC guide](https://beancount.io/blog/2026/05/12/cmmc-2-0-nist-800-171-compliance-2026-small-defense-contractors-level-1-level-2-certification-dod-contract-awards-guide) · [HIPAA Journal destruction rules](https://hipaajournal.com/medical-records-destruction-rules) · [HIPAA Journal, pharmacy penalty](https://www.hipaajournal.com/improper-disposal-nets-small-pharmacy-125k-ocr-hipaa-penalty-4532/) · [HIPAA Journal fines](https://www.hipaajournal.com/hipaa-violation-fines/)

Pharma validation: [IntuitionLabs pricing](https://intuitionlabs.ai/services/computer-system-validation/system-validation) · [Part11 AIValidator](https://part11solutions.com/aivalidator-turnkey-agent-solution/) · [GoVal](https://www.itechguides.com/products/goval/) · [ValGenesis](https://www.valgenesis.com/editorial/ensuring-trust-in-ai-the-valgenesis-approach-to-validating-ai-agents-and-functions-for-validation-and-qualification-in-life-sciences)

Counter-UAS: [RTI TruFlight](https://www.rti.org/products/truflight-cuas-drone-assessment-platform) · [Forensic Focus C-UAS analysis](https://www.forensicfocus.com/articles/a-comprehensive-walkthrough-of-counter-uav-analysis/) · [Inside Unmanned Systems on the FEMA program](https://insideunmannedsystems.com/fema-launches-500-million-c-uas-grant-program-for-state-and-local-drone-defense/) · [Missouri FY2026 NOFO](https://dps.mo.gov/dir/programs/ohs/grantstraining/documents/fy-2026-c-uas-notice-of-funding-opportunity.pdf) · [NLC summary](https://www.nlc.org/article/2025/12/09/femas-announces-new-counter-uas-grant-program-to-empower-cities-towns-and-villages/) · [Pierce Aerospace](https://dronelife.com/2025/01/09/pierce-aerospace-secures-10m-contract-for-remote-id-technology/)

Music: [Beatport ban, DJ Mag](https://djmag.com/news/beatport-banning-fully-ai-generated-tracks) · [BeatStars and Sureel](https://www.musicbusinessworldwide.com/beatstars-teams-up-with-sureel-to-block-ai-companies-from-training-on-creator-content) · [Warner and Suno](https://www.musicbusinessworldwide.com/warner-music-group-settles-with-suno/) · [Udio and Kobalt](https://www.musicbusinessworldwide.com/after-universal-warner-and-merlin-deals-now-udio-inks-licensing-agreement-with-kobalt) · [AI music news, April 2026](https://www.opus.pro/blog/ai-music-news-april-2026)

Trading / KDP / network: [TraderSync review](https://traderssecondbrain.com/guides/tradersync-review) · [TradeZella pricing](https://www.tradezella.com/blog/tradezella-pricing) · [KDP content guidelines](https://kdp.amazon.com/en_US/help/topic/G200672410) · [Authors Guild on KDP AI policy](https://authorsguild.org/news/amazon-adds-to-kdp-generative-ai-policy-caps-daily-self-publishing-uploads/) · [ScribeCount KDP guide](https://scribecount.com/author-resource/protecting-books/kdp-ai-content-policy-the-compliance-guide) · [EmberOT](https://www.itechguides.com/products/emberot/) · [Lansweeper OT](https://www.lansweeper.com/solutions/use-cases/ot-security/)
