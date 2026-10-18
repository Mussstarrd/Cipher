# Platform and tool research (October 2026)

Prices and limits as found on 18 Oct 2026. Where sources conflicted I say so. Official pages could not be fetched directly from this environment (network policy), so figures come from search snippets of official pages plus secondary guides. **Verify anything marked ⚠ before paying.**

## Summary table

| Area | Pick | Cost | Blocker / needs a tap |
|---|---|---|---|
| Source audio | Suno Pro (have) | $10/mo | Pro is capped at **20 downloads/month** since 3 Sep 2026; Premier gets 60 |
| Beat store | BeatStars Starter | $19.99/yr, annual only | No public API. Upload is a daily-queue tap |
| Analysis + packaging | Python: librosa, essentia, pyloudnorm, ffmpeg, Pillow | $0 | none |
| Visualizer video | ffmpeg + Python | $0 | none |
| Persona face | Hedra Character-3 API | pay-as-you-go, ~5¢/s at 720p | wallet must be funded; your yes needed |
| Persona voice | ElevenLabs Starter | $6/mo (first month $1) | commercial license starts at Starter |
| B-roll (phase 2) | Kling via fal.ai or Veo 3.1 Lite | ~$0.03–0.10/s | optional |
| Scheduling | Postiz hosted | $29/mo | or $0 self-hosted but then you do each platform's app review yourself |
| YouTube | Data API v3 direct | $0 | 10,000 units/day = 6 uploads/day |
| Instagram | Graph API direct | $0 | Business account + Meta app review (weeks) |
| TikTok | Content Posting API | $0 | unaudited = private-only posts; audit needed |
| Analytics | YouTube Analytics API, IG Insights, Postiz, BeatStars CSV | $0 | BeatStars and TikTok numbers partly manual |

Estimated run cost at launch: **$60–80/month** including what you already pay Suno. Nothing gets bought without your yes.

## Posting platforms

### TikTok Content Posting API
- Unaudited clients can only post with `SELF_ONLY` (private) visibility and serve at most 5 users in 24 h. The audit lifts this. [Content Sharing Guidelines](https://developers.tiktok.com/docs/en/content-sharing-guidelines), [Direct Post reference](https://developers.tiktok.com/docs/en/content-posting-api-reference-direct-post)
- Audit requires a working integration first, specific UX (title field, privacy selector, user consent before upload) and ToS compliance. TikTok gives no official timeline; app review "may take several days to two weeks" and is manual. [App Review FAQ](https://developers.tiktok.com/docs/en/getting-started-faq), [Developer Guidelines](https://developers.tiktok.com/docs/en/our-guidelines-developer-guidelines)
- Per-account daily post cap exists for everyone; the number is not published. ⚠ Third-party guides guess 15–25/day.
- **Plan:** go through Postiz (their audited app) on day one; apply for our own audit in parallel so we can drop the subscription later.

### Instagram Graph API
- **100 API-published posts per account per rolling 24 h** (carousels count as one; the 25 figure is from the 2021 launch and is obsolete). 400 containers/24 h. Check usage at `content_publishing_limit`. [Content Publishing](https://developers.facebook.com/docs/instagram-platform/content-publishing/)
- Reels need an Instagram **Business** account, a public `video_url`, 9:16, up to 90 s via API. Scopes `instagram_business_basic` and `instagram_business_content_publish` need app review. [Reels guide (third party)](https://postproxy.dev/blog/instagram-reels-api-publishing-guide/)
- ⚠ One Meta docs path still says 50/day. Either is far above our cadence.

### YouTube Data API v3
- Default quota 10,000 units/day; `videos.insert` costs 1,600 units, so **6 uploads/day** before a quota extension. [Quota cost table](https://developers.google.com/youtube/v3/determine_quota_cost)
- **Changed since training:** from 1 June 2026 YouTube moved to a granular quota, starting with `videos.insert` and `search.list`, which is meant to make targeted increases easier. [Revision history](https://developers.google.com/youtube/data/revision_history)
- Shorts are ordinary uploads under 3 min in vertical format; include `#Shorts` is optional now.

### BeatStars
- No public upload API found anywhere. Bulk upload exists in the Studio UI (CEO tutorial video); bulk price editing exists. [Bulk edit price](https://help.beatstars.com/hc/en-us/articles/22374743491355-How-Do-I-Bulk-Edit-The-Price-Of-Tracks), [BeatStars blog](https://blog.beatstars.com/categories/features-and-tutorials)
- Plans: Starter $19.99/yr (annual only, unlimited MP3/WAV uploads, 0% commission), Growth $79.99/yr, Professional $14.99/mo billed $179.88/yr. ⚠ A help article lists different monthly rates. [Pricing](https://www.beatstars.com/pricing), [How much does BeatStars cost](https://help.beatstars.com/hc/en-us/articles/7797168579227-How-Much-Does-BeatStars-Cost)
- **Least manual path:** the pipeline writes a per-beat package folder (WAV, tagged MP3, 30 s preview, cover, `metadata.json`, description text) and a one-click "copy fields" page in the daily queue. You drag the folder into Studio bulk upload and paste. Browser automation of their site is not planned until their ToS is read; most marketplaces forbid it.

### Suno (rights)
- Pro $10/mo (500 songs), Premier $30/mo (2,000 songs), 20% off annual. Songs made while subscribed carry commercial rights and keep them after cancelling; free-tier songs never gain them retroactively. [Does Suno own my music](https://help.suno.com/en/articles/2416769), [Copyright FAQ](https://help.suno.com/en/articles/2746945)
- **Changed since training (3 Sep 2026):** Pro gets 20 downloads/month, Premier 60, Premier + Studio unlimited. Trial downloads are personal-use only. [Suno ToS update](https://suno.com/blog/suno-updates-tos)
- Catalogue must record: plan tier at render date, render date, download date, Suno song ID.

## Disclosure rules

| Platform | Rule | How we comply |
|---|---|---|
| TikTok | Must label AI or edited content depicting realistic people or scenes; AIGC toggle or clear caption/sticker. Auto-labels C2PA content; auto label cannot be removed. Label does not affect reach. Realistic private individuals' likeness banned even if labelled. [AI-generated content](https://www.tiktok.com/tns-inapp/pages/ai-generated-content), [Community Guidelines](https://www.tiktok.com/community-guidelines/en-GB/integrity-authenticity) | Set AIGC flag on every persona video; burn "AI character" into the first second of the overlay. |
| YouTube | Disclose realistic altered/synthetic content via the "AI use" upload setting. **Changed May 2026:** Shorts show the label as an on-video overlay; undisclosed photorealistic AI gets auto-labelled. Label does not limit reach or monetisation. [Help](https://support.google.com/youtube/answer/14328491), [Blog](https://blog.youtube/news-and-events/improving-ai-labels-viewers-creators/) | Set `containsSyntheticMedia` on upload (verify API field name at build time); same burned-in text. |
| Instagram / Meta | Must use the AI disclosure tool for photorealistic video or realistic audio that was created or altered; repeat failures penalised; "AI info" label. [Meta approach](https://about.fb.com/news/2024/04/metas-approach-to-labeling-ai-generated-content-and-manipulated-media/), [Labeling AI content](https://transparency.meta.com/governance/tracking-impact/labeling-ai-content/) | Toggle on every persona Reel; the API path for the toggle must be confirmed at build time ⚠. |
| FTC (US) | No new 2026 AI rule confirmed. 2023 Endorsement Guides cover virtual personas; Consumer Review Rule (2024) bans fake reviews including AI ones. ⚠ Blogs claiming a "double disclosure" rule with $53,088 fines cite no FTC document. [Dynamis LLP](https://dynamisllp.com/knowledge/ai-disclosure-in-2026-recent-developments-and-practical-steps-for-brands-and-influencers) | Persona never endorses as a person, never posts testimonials or sales numbers; bio says it is a character of the company. |
| EU AI Act Art. 50 | Machine-readable marking of synthetic content from 2 Aug 2026 if content reaches the EU. | Embed C2PA/Content Credentials when the toolchain supports it; TikTok and Meta already read it. |

## "Type beat" convention
- Still standard on BeatStars and YouTube; artist name is descriptive use, not a licence to imply endorsement. No 2026 YouTube policy against it was found. [RouteNote](https://create.routenote.com/blog/how-to-use-type-beats-to-boost-your-beat-sales/), [Music Production Wiki](https://musicproductionwiki.com/articles/how-to-sell-type-beats)
- Rule for us: artist name only in the title and tags, in the form `"<Artist> Type Beat"`, never in the caption voice, never "official", never an artist's artwork. Re-check platform rules quarterly.

## Persona face (AI avatar)

| Service | Price | Commercial | Fit |
|---|---|---|---|
| **Hedra Character-3 API** | Prepaid USD wallet, no subscription. 540p 2.5¢/s, 720p 5¢/s, 1080p 6.25¢/s. Estimate endpoint is free. [Pricing](https://www.hedra.com/pricing), [Character-3](https://www.hedra.com/develop/models/video/hedra-character-3) | Studio Basic $15/mo includes commercial use; API is commercial by nature ⚠ verify ToS | **Best fit.** Drives any still image we design from your references with any audio, including singing, up to 10 min. A 10 s hook clip ≈ $0.50. |
| HeyGen API | Pay-as-you-go wallet. Avatar IV photo avatar $2.31/min, Avatar IV digital twin $4.83/min, Avatar V $7.20/min. ⚠ Blog figures differ. [API pricing explained](https://help.heygen.com/en/articles/10060327-heygen-api-pricing-explained) | Paid plans yes; Free/Creator restrictions ⚠ | Good, pricier, more "corporate presenter" look. |
| D-ID API | Build $14.40/mo (64 credits), Launch $35/mo (180), Scale $138.60/mo. ⚠ Sources conflict. [Creatify guide](https://creatify.ai/blog/d-id-pricing-(2026)-plans-credits-and-what-you-ll-actually-pay) | Commercial API from Launch | Weakest motion of the four. |
| Synthesia | Creator $89/mo or $59/mo annual, 360 min/yr, API only on Creator+. [Arcade](https://www.arcade.software/post/synthesia-pricing) | Yes | Built for corporate training video; wrong look for this brand. |

## Persona voice
- **ElevenLabs** Free is non-commercial with attribution. Starter $6/mo (30k credits, commercial license, instant voice cloning). Creator $22/mo (121k credits, professional voice cloning). Voice Design lets us prompt a synthetic original voice instead of cloning anyone. [Pricing](https://elevenlabs.io/pricing), [Voice Design](https://elevenlabs.io/voice-design)
- Open alternatives (XTTS, Fish Speech, Kokoro) are $0 and run locally; quality is good but slur and timing control is worse. Start with ElevenLabs Starter.

## B-roll generators (phase 2, optional)
Per-second API rates, all ⚠ secondary sources: Veo 3.1 Lite $0.03–0.08/s (720p, no audio), Kling 3.0 $0.08–0.13/s, Luma Ray 3 ≈ $0.10/s, Runway Gen-4.5 $0.12–0.25/s. **Sora 2 API shut down 24 Sep 2026.** [Atlas Cloud](https://www.atlascloud.ai/blog/cheapest-ai-video-generation-api-2026), [Rangy](https://rangy.ai/blog/ai-video-pricing-explained). Kling through fal.ai is the value pick when we get there.

## Rendering stack
- **ffmpeg** filters `showwaves`, `showspectrum`, `showcqt` plus Pillow-rendered text frames cover every format in the brief. Runs on your machine or a $6 VPS. $0.
- **Remotion** is free for individuals and companies ≤ 3 employees; company license $25/seat or $0.01/render with $100/mo minimum. [License](https://www.remotion.dev/docs/license). Not needed; it adds a Node toolchain for no gain here.
- Audio analysis: `librosa` (BPM, onsets), `essentia` (key, better than librosa's chroma), `pyloudnorm` (LUFS), hook detection by energy + repetition over bars. All free.

## Scheduling services

| Service | Price | Notes |
|---|---|---|
| **Postiz hosted** | $29/$39/$49/$99 per mo (20% off annual), 7-day trial | Public REST API + webhooks on every plan; TikTok, IG, YouTube supported; their platform apps already approved. API limit 30 req/h ⚠. [Pricing](https://postiz.com/pricing) |
| Postiz self-hosted | $0, AGPL-3.0 | Same API, but you create your own Meta/TikTok/YouTube apps and go through their reviews ("more than a month"). [GitHub](https://github.com/gitroomhq/postiz-app) |
| Zernio (formerly Late) | from $6/account/mo | Cheapest if we only need 3 accounts. [Post Bridge roundup](https://www.post-bridge.com/best-social-media-scheduling-api) |
| Buffer | free 3 channels, $5–6/channel | API still beta, personal key only. |
| Ayrshare | from $149/mo | Overkill. |

**Recommendation:** Postiz hosted ($29) for the first 60 days while our own TikTok audit and Meta app review run; YouTube direct from day one since it needs no review beyond OAuth.

## Analytics
- YouTube Analytics API: free, same OAuth app.
- Instagram Insights: free with the same Graph API app once reviewed.
- TikTok: no free analytics API for creators; use Postiz's analytics or the weekly CSV export from TikTok Studio (a tap in the queue).
- BeatStars: no API. Weekly sales CSV export is a tap in the queue; the pipeline parses it.

## Changed since my training (flag list)
1. Suno download caps per plan (3 Sep 2026).
2. YouTube granular quota (1 Jun 2026) and Shorts AI-label overlay (May 2026).
3. Instagram API limit is 100/day, not 25.
4. Hedra merged Avatar and Omnia into Character-3 (Jul 2026); ⚠ a tracker reports a Sep 2026 top-tier price rise to $100/mo.
5. HeyGen Avatar V and wallet-based API billing.
6. Sora 2 API retired (Sep 2026).
7. EU AI Act Article 50 in force (2 Aug 2026).
8. Late renamed Zernio.
