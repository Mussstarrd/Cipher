# Study 2026-10-08: growing the agentic account faster without blowing it up

**Question set:** (1) does gap-up momentum hold on a wider liquid universe, and at 1.5% / 3% thresholds; (2) SOXL (3x semis) for the overnight hold and for SMH gap days; (3) position sizing (Kelly, settlement-aware equity simulation, 20% max-drawdown cap); (4) one new rule, stated before testing. Ends with a proposed **rule set v4**.

**Window and data:** same as `study-2026-10-07-agentic-rules.md`. Trade entries from 2026-04-15 to 2026-10-08 (123 sessions). Robinhood `get_equity_historicals`, 10-minute regular-session bars (split-adjusted), plus daily bars. Read-only: no order, review or cancel tool was called. The Oct 8 session was pulled at about 3:50 pm ET. The 3:45 proxy (19:40 bar) exists for every symbol, but the 4:00 bar was missing or partial on some, so no overnight trade enters on Oct 8.
**Price proxies (unchanged):** 9:30 open = open of the 13:30 UTC bar. 9:45 = midpoint of the 13:40 bar. 3:45 = midpoint of the 19:40 bar. Prior close = close of the 19:50 bar. Stops and intraday lows are checked against 10-minute lows from 9:50 to 3:40.
**Costs (round trip, deducted from every trade):** 0.10% for every name except TSM, PLTR and MRVL, which are charged 0.20%. Their regular-hours dollar volume is $1.7B, $2.3B and $2.9B a day, against $5.5B to $16.8B for AMD, INTC, NVDA and MU. SMH ($2.1B) stays at 0.10% because it is an ETF with a one-cent spread on a roughly $500 price, as in the prior study. SOXL ($3.1B) is charged 0.10%, with a 0.20% sensitivity row.
**Honest metric:** the portfolio view, with n = signal days. "Split" divides capital equally across that day's signals. "Largest" takes only the largest gap that day, which is what v3 actually trades. **H1** = 2026-04-15 to 2026-07-13 (61 sessions). **H2** = 2026-07-14 to 2026-10-08 (62 sessions). "Mean ex top-3" removes the 3 best trades or days.
**Reproducibility:** scripts and raw JSON are in the session scratchpad (`g2/`). The original 5 symbols at a 2% gap reproduce the prior study exactly: 36 days, +1.02% a day, t = 3.13.

---

## Bottom line

1. **The gap rule is real in this sample, but it is a signal effect, not a name effect.** On all 16 names at a 2% gap, taking the largest gap each day gives 55 days, +0.92% a day, t = 2.84, and it holds in both halves (+0.83% / +1.06%). **Choosing names on one half did not predict the other half.** In 4 of 6 out-of-sample tests, the names *not* picked did better than the picked ones, including every test that picked on H1 and tested on H2. The new large caps rarely gap 2% (AAPL, QQQ, TSLA, MSFT, GOOGL, META and AMZN: 1 to 4 times each in 6 months), so the extra 11 names mostly add days, not edge. Days added only by them average +0.45% (t = 1.0, ex-top-3 −0.11%).
2. **Thresholds:** 1.5%, 2% and 3% all work. A higher threshold gives a larger edge per trade on fewer days, and compounded totals are similar (orig 5, largest: +60% / +57% / +51%). Keep 2%. It was fixed before this study, and choosing the "best" threshold would be curve-fitting.
3. **Excluding earnings days costs nothing.** Only 3 of 79 orig-5 gap trades were earnings-reaction days, and they averaged −0.16%. With them excluded: 35 days, 71% wins, +1.40% a day, t = 3.36, ex-top-3 +0.91%, H1 +1.55% / H2 +1.21%.
4. **SOXL is mostly leveraged beta with heavy tails.** Overnight SOXL averages +0.86% a night but has t = 1.27, a −20% worst night, a −38% max drawdown, and H2 is flat (+0.16%, t 0.19). SOXL on SMH gap days: +2.56% a trade but t = 2.0, n = 14, H2 n = 4. At the 1.5% threshold one trade lost −16% in a day. Volatility decay is visible: SMH was +6.8% in H2 while SOXL was −3.9%. **Do not use SOXL.**
5. **Sizing:** the gap rule's Kelly fraction is far above 1 even after heavy haircuts, so half-Kelly is capped at **100% of equity** (a cash account cannot lever). **Settlement is what kills the overnight rule:** a dollar bought at 3:45 and sold at 9:30 does not settle until the following morning, so it is locked for two sessions and blocks the next morning's gap trade. **Adding any overnight sleeve lowered final equity once the gap rule ran at full size.** Gap-only at 100% grows $2,400 to **$3,870 (+61%) with a −6.7% max drawdown**, including intraday lows. The current v3 setup reaches $2,999 (+25%).
6. **The new rule (SMH 20-day trend hold) failed:** +7.7% against +33.9% buy-and-hold, with a *worse* drawdown (−29% vs −23%). Rejected.

---

## 1. Gap-up momentum on a wider universe

Rule: the 9:30 open is at least the threshold above the prior close, AND the 9:45 price is above the open. Buy at 9:45, sell at 3:45. No stop in these tables. Earnings days are included here; they are split out below. Per-symbol rows are per trade, and each symbol's max drawdown compounds its own trades in sequence.

#### Gap >= 1.5%, per symbol

| Symbol | Cost | n | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean | H2 n, mean |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| NVDA | 0.10% | 10 | 60% | +0.46% | +0.64% | 0.84 | +4.52% | -1.95% | -2.87% | -0.36% | 4, +1.77% | 6, -0.42% |
| AMD | 0.10% | 23 | 65% | +1.30% | +0.88% | 3.12 | +33.95% | -2.18% | -3.06% | +0.87% | 14, +1.41% | 9, +1.13% |
| MU | 0.10% | 22 | 50% | +1.18% | +0.01% | 1.94 | +28.30% | -2.75% | -4.47% | +0.31% | 11, +1.71% | 11, +0.64% |
| INTC | 0.10% | 23 | 61% | +1.10% | +0.89% | 1.68 | +27.25% | -3.02% | -7.99% | +0.21% | 11, +1.25% | 12, +0.96% |
| SMH | 0.10% | 19 | 68% | +0.46% | +1.05% | 1.13 | +8.84% | -4.66% | -4.66% | +0.10% | 13, +0.51% | 6, +0.36% |
| AVGO | 0.10% | 14 | 57% | +0.40% | +0.24% | 1.18 | +5.66% | -1.47% | -1.47% | -0.07% | 4, +0.74% | 10, +0.27% |
| MRVL | 0.20% | 23 | 57% | +0.56% | +0.07% | 1.01 | +12.91% | -2.41% | -7.38% | -0.26% | 13, +0.91% | 10, +0.11% |
| TSM | 0.20% | 10 | 40% | -0.07% | -0.29% | -0.22 | -0.72% | -1.23% | -2.27% | -0.60% | 9, -0.25% | 1, +1.59% |
| PLTR | 0.20% | 16 | 62% | +1.29% | +1.59% | 2.13 | +22.26% | -2.02% | -2.32% | +0.50% | 11, +0.60% | 5, +2.81% |
| TSLA | 0.10% | 4 | 75% | +0.31% | +0.65% | 0.54 | +1.24% | -1.26% | -1.26% | -1.26% | 1, -1.26% | 3, +0.84% |
| META | 0.10% | 5 | 40% | +1.06% | -0.32% | 0.76 | +5.21% | -0.81% | -1.86% | -0.78% | 3, -0.14% | 2, +2.86% |
| AMZN | 0.10% | 7 | 43% | +0.27% | -0.11% | 0.66 | +1.84% | -1.31% | -1.88% | -0.52% | 4, -0.11% | 3, +0.77% |
| MSFT | 0.10% | 6 | 33% | -0.38% | -0.20% | -0.64 | -2.33% | -2.88% | -3.52% | -1.38% | 3, -1.08% | 3, +0.31% |
| GOOGL | 0.10% | 6 | 67% | +1.03% | +0.21% | 1.55 | +6.26% | -0.07% | -0.07% | -0.02% | 3, +0.60% | 3, +1.46% |
| AAPL | 0.10% | 1 | 0% | -0.73% | -0.73% | nan | -0.73% | -0.73% | -0.73% | n/a | 1, -0.73% | 0, - |
| QQQ | 0.10% | 2 | 100% | +0.60% | +0.60% | 2.37 | +1.21% | +0.35% | +0.00% | n/a | 1, +0.35% | 1, +0.86% |

Portfolio views (equal split by signal day; 'largest' = only the largest gap that day):

| Set | n days | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean, t | H2 n, mean, t |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Original 5 (v3) split | 43 | 56% | +0.84% | +0.46% | 2.41 | +41.99% | -4.66% | -4.97% | +0.46% | 24, +1.27%, 2.41 | 19, +0.31%, 0.74 |
| Original 5 (v3) largest | 43 | 60% | +1.13% | +0.67% | 2.71 | +59.74% | -4.66% | -4.66% | +0.68% | 24, +1.28%, 2.15 | 19, +0.95%, 1.61 |
| New 11 split | 54 | 52% | +0.57% | +0.09% | 2.01 | +34.15% | -2.41% | -7.39% | +0.24% | 32, +0.57%, 1.36 | 22, +0.56%, 1.64 |
| New 11 largest | 54 | 52% | +0.60% | +0.04% | 1.82 | +35.90% | -2.41% | -9.25% | +0.23% | 32, +0.38%, 0.87 | 22, +0.92%, 1.85 |
| All 16 split | 66 | 55% | +0.60% | +0.25% | 2.49 | +46.65% | -4.66% | -5.06% | +0.37% | 38, +0.80%, 2.24 | 28, +0.32%, 1.11 |
| All 16 largest | 66 | 56% | +0.84% | +0.32% | 2.70 | +69.88% | -4.66% | -6.82% | +0.53% | 38, +0.92%, 2.08 | 28, +0.72%, 1.71 |

#### Gap >= 2.0%, per symbol

| Symbol | Cost | n | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean | H2 n, mean |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| NVDA | 0.10% | 5 | 40% | +0.11% | -0.27% | 0.14 | +0.48% | -1.88% | -2.87% | -1.45% | 1, +1.32% | 4, -0.20% |
| AMD | 0.10% | 21 | 71% | +1.45% | +0.94% | 3.27 | +34.66% | -2.18% | -2.83% | +0.99% | 13, +1.53% | 8, +1.31% |
| MU | 0.10% | 20 | 55% | +1.40% | +0.17% | 2.17 | +31.12% | -2.75% | -4.47% | +0.48% | 11, +1.71% | 9, +1.02% |
| INTC | 0.10% | 19 | 63% | +1.11% | +0.89% | 1.66 | +22.48% | -3.02% | -6.06% | +0.25% | 9, +0.92% | 10, +1.29% |
| SMH | 0.10% | 14 | 71% | +0.69% | +1.08% | 1.91 | +9.97% | -1.94% | -3.82% | +0.29% | 10, +0.91% | 4, +0.15% |
| AVGO | 0.10% | 7 | 57% | +0.18% | +0.13% | 0.37 | +1.22% | -1.47% | -1.72% | -0.67% | 2, -0.56% | 5, +0.48% |
| MRVL | 0.20% | 22 | 59% | +0.68% | +0.11% | 1.18 | +15.16% | -2.41% | -5.53% | -0.17% | 12, +1.15% | 10, +0.11% |
| TSM | 0.20% | 7 | 29% | -0.21% | -0.30% | -0.60 | -1.47% | -1.23% | -3.01% | -0.75% | 6, -0.51% | 1, +1.59% |
| PLTR | 0.20% | 11 | 45% | +1.15% | -0.17% | 1.30 | +12.89% | -2.02% | -2.32% | -0.19% | 7, +0.02% | 4, +3.12% |
| TSLA | 0.10% | 3 | 67% | +0.04% | +0.16% | 0.05 | +0.10% | -1.26% | -1.26% | n/a | 1, -1.26% | 2, +0.69% |
| META | 0.10% | 4 | 50% | +1.40% | -0.05% | 0.81 | +5.55% | -0.81% | -1.54% | -0.81% | 2, -0.05% | 2, +2.86% |
| AMZN | 0.10% | 4 | 50% | +0.46% | +0.49% | 0.99 | +1.83% | -0.47% | -0.47% | -0.47% | 1, -0.47% | 3, +0.77% |
| MSFT | 0.10% | 3 | 33% | +0.31% | -0.11% | 0.61 | +0.94% | -0.29% | -0.29% | n/a | 0, - | 3, +0.31% |
| GOOGL | 0.10% | 3 | 100% | +1.49% | +0.41% | 1.16 | +4.48% | +0.02% | +0.00% | n/a | 1, +0.02% | 2, +2.22% |
| AAPL | 0.10% | 1 | 0% | -0.73% | -0.73% | nan | -0.73% | -0.73% | -0.73% | n/a | 1, -0.73% | 0, - |
| QQQ | 0.10% | 1 | 100% | +0.35% | +0.35% | nan | +0.35% | +0.35% | +0.00% | n/a | 1, +0.35% | 0, - |

Portfolio views (equal split by signal day; 'largest' = only the largest gap that day):

| Set | n days | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean, t | H2 n, mean, t |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Original 5 (v3) split | 36 | 67% | +1.02% | +0.58% | 3.13 | +42.96% | -2.34% | -3.89% | +0.66% | 20, +1.33%, 2.98 | 16, +0.62%, 1.33 |
| Original 5 (v3) largest | 36 | 67% | +1.30% | +0.83% | 3.08 | +57.39% | -2.75% | -4.47% | +0.81% | 20, +1.31%, 2.36 | 16, +1.28%, 1.92 |
| New 11 split | 44 | 50% | +0.52% | -0.03% | 1.55 | +24.54% | -2.41% | -6.10% | +0.12% | 27, +0.41%, 0.84 | 17, +0.71%, 1.66 |
| New 11 largest | 44 | 52% | +0.66% | +0.04% | 1.71 | +32.01% | -2.41% | -6.45% | +0.22% | 27, +0.35%, 0.70 | 17, +1.17%, 1.88 |
| All 16 split | 55 | 56% | +0.72% | +0.20% | 2.88 | +46.89% | -2.02% | -6.56% | +0.49% | 32, +0.76%, 2.14 | 23, +0.67%, 1.92 |
| All 16 largest | 55 | 60% | +0.92% | +0.39% | 2.84 | +63.39% | -2.75% | -8.56% | +0.59% | 32, +0.83%, 1.86 | 23, +1.06%, 2.19 |

#### Gap >= 3.0%, per symbol

| Symbol | Cost | n | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean | H2 n, mean |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| NVDA | 0.10% | 1 | 100% | +2.38% | +2.38% | nan | +2.38% | +2.38% | +0.00% | n/a | 0, - | 1, +2.38% |
| AMD | 0.10% | 16 | 69% | +1.31% | +0.83% | 2.65 | +22.81% | -2.18% | -2.83% | +0.71% | 10, +1.10% | 6, +1.66% |
| MU | 0.10% | 14 | 64% | +1.72% | +1.05% | 2.06 | +26.25% | -2.75% | -4.47% | +0.38% | 8, +1.76% | 6, +1.68% |
| INTC | 0.10% | 15 | 67% | +0.95% | +0.89% | 1.61 | +14.76% | -3.02% | -3.66% | +0.25% | 7, -0.30% | 8, +2.04% |
| SMH | 0.10% | 6 | 83% | +0.73% | +1.04% | 1.73 | +4.42% | -1.20% | -1.20% | +0.01% | 4, +0.36% | 2, +1.47% |
| AVGO | 0.10% | 1 | 0% | -1.47% | -1.47% | nan | -1.47% | -1.47% | -1.47% | n/a | 1, -1.47% | 0, - |
| MRVL | 0.20% | 15 | 60% | +1.06% | +0.20% | 1.33 | +16.35% | -2.41% | -3.86% | -0.19% | 9, +1.62% | 6, +0.21% |
| TSM | 0.20% | 3 | 67% | +0.52% | +0.26% | 0.92 | +1.55% | -0.30% | -0.30% | n/a | 2, -0.02% | 1, +1.59% |
| PLTR | 0.20% | 4 | 75% | +2.99% | +2.94% | 1.71 | +12.30% | -1.24% | -1.24% | -1.24% | 3, +1.54% | 1, +7.31% |
| TSLA | 0.10% | 0 |
| META | 0.10% | 1 | 0% | -0.74% | -0.74% | nan | -0.74% | -0.74% | -0.74% | n/a | 1, -0.74% | 0, - |
| AMZN | 0.10% | 1 | 100% | +1.18% | +1.18% | nan | +1.18% | +1.18% | +0.00% | n/a | 0, - | 1, +1.18% |
| MSFT | 0.10% | 1 | 100% | +1.35% | +1.35% | nan | +1.35% | +1.35% | +0.00% | n/a | 0, - | 1, +1.35% |
| GOOGL | 0.10% | 0 |
| AAPL | 0.10% | 0 |
| QQQ | 0.10% | 0 |

Portfolio views (equal split by signal day; 'largest' = only the largest gap that day):

| Set | n days | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean, t | H2 n, mean, t |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Original 5 (v3) split | 27 | 70% | +1.28% | +0.78% | 3.07 | +40.02% | -2.47% | -4.02% | +0.78% | 16, +1.15%, 2.04 | 11, +1.46%, 2.30 |
| Original 5 (v3) largest | 27 | 74% | +1.58% | +0.88% | 3.10 | +51.40% | -2.75% | -4.47% | +0.95% | 16, +1.20%, 1.86 | 11, +2.14%, 2.55 |
| New 11 split | 21 | 62% | +1.13% | +0.26% | 1.84 | +25.66% | -2.41% | -4.07% | +0.31% | 14, +1.37%, 1.65 | 7, +0.65%, 0.77 |
| New 11 largest | 21 | 62% | +1.22% | +0.26% | 1.83 | +27.73% | -2.41% | -4.52% | +0.31% | 14, +1.34%, 1.59 | 7, +0.97%, 0.83 |
| All 16 split | 34 | 68% | +1.19% | +0.92% | 3.40 | +48.49% | -2.47% | -5.47% | +0.83% | 21, +1.24%, 2.70 | 13, +1.10%, 1.99 |
| All 16 largest | 34 | 68% | +1.33% | +0.88% | 2.91 | +54.75% | -2.75% | -6.67% | +0.80% | 21, +1.22%, 2.08 | 13, +1.50%, 1.99 |

**Reading it.** At 2%, AMD (t 3.3), MU (2.2), INTC (1.7) and SMH (1.9) carry the original result. Among the new names, MRVL (+0.68%, but H2 +0.11%) and PLTR (+1.15%, carried by a few big H2 days) are the only ones with ten or more trades, and neither is convincing ex-top-3. TSM is negative. The mega caps barely trigger. "Largest gap" beats "equal split" in every block. The biggest gap carries the most continuation.

### Out of sample: pick names on one half, test on the other

Selection rule: a name is picked if it had at least 3 trades in the training half and a mean above +0.5% after costs.

| Test | n days | Win% | Mean | Median | t | Max DD | Mean ex top-3 |
|---|---|---|---|---|---|---|---|
| 1.5% pick H1 -> test H2 split: picked NVDA,AMD,MU,INTC,SMH,AVGO,MRVL,PLTR,GOOGL | 28 | 46% | +0.44% | -0.12% | 1.37 | -5.40% | +0.06% |
| 1.5% pick H1 -> test H2 split: not picked | 7 | 71% | +0.96% | +1.14% | 2.04 | -0.29% | +0.22% |
| 1.5% pick H1 -> test H2 split: all 16 | 28 | 50% | +0.32% | -0.02% | 1.11 | -4.71% | -0.01% |
| 1.5% pick H1 -> test H2 largest: picked NVDA,AMD,MU,INTC,SMH,AVGO,MRVL,PLTR,GOOGL | 28 | 57% | +0.87% | +0.25% | 1.97 | -4.65% | +0.34% |
| 1.5% pick H1 -> test H2 largest: not picked | 7 | 71% | +1.56% | +1.18% | 1.80 | -0.29% | +0.46% |
| 1.5% pick H1 -> test H2 largest: all 16 | 28 | 57% | +0.72% | +0.25% | 1.71 | -4.65% | +0.19% |
| 1.5% pick H2 -> test H1 split: picked AMD,MU,INTC,PLTR,TSLA,AMZN,GOOGL | 28 | 68% | +1.17% | +0.39% | 2.60 | -4.31% | +0.54% |
| 1.5% pick H2 -> test H1 split: not picked | 28 | 46% | +0.25% | -0.19% | 0.67 | -5.35% | -0.20% |
| 1.5% pick H2 -> test H1 split: all 16 | 38 | 58% | +0.80% | +0.47% | 2.24 | -4.82% | +0.41% |
| 1.5% pick H2 -> test H1 largest: picked AMD,MU,INTC,PLTR,TSLA,AMZN,GOOGL | 28 | 61% | +1.15% | +0.57% | 2.30 | -2.99% | +0.47% |
| 1.5% pick H2 -> test H1 largest: not picked | 28 | 50% | +0.46% | +0.01% | 0.95 | -5.53% | -0.21% |
| 1.5% pick H2 -> test H1 largest: all 16 | 38 | 55% | +0.92% | +0.37% | 2.08 | -4.66% | +0.42% |
| 2.0% pick H1 -> test H2 split: picked AMD,MU,INTC,SMH,MRVL | 14 | 57% | +0.63% | +0.27% | 1.45 | -1.86% | +0.03% |
| 2.0% pick H1 -> test H2 split: not picked | 16 | 62% | +0.89% | +0.17% | 1.83 | -4.76% | +0.19% |
| 2.0% pick H1 -> test H2 split: all 16 | 23 | 57% | +0.67% | +0.20% | 1.92 | -3.93% | +0.29% |
| 2.0% pick H1 -> test H2 largest: picked AMD,MU,INTC,SMH,MRVL | 14 | 71% | +0.84% | +0.38% | 1.51 | -2.75% | +0.04% |
| 2.0% pick H1 -> test H2 largest: not picked | 16 | 62% | +1.32% | +0.80% | 2.01 | -4.76% | +0.29% |
| 2.0% pick H1 -> test H2 largest: all 16 | 23 | 70% | +1.06% | +0.46% | 2.19 | -3.11% | +0.44% |
| 2.0% pick H2 -> test H1 split: picked AMD,MU,INTC,PLTR,AMZN | 23 | 61% | +1.16% | +0.39% | 2.38 | -2.95% | +0.48% |
| 2.0% pick H2 -> test H1 split: not picked | 22 | 45% | +0.37% | -0.17% | 0.91 | -4.09% | -0.20% |
| 2.0% pick H2 -> test H1 split: all 16 | 32 | 56% | +0.76% | +0.23% | 2.14 | -5.05% | +0.34% |
| 2.0% pick H2 -> test H1 largest: picked AMD,MU,INTC,PLTR,AMZN | 23 | 61% | +1.05% | +0.67% | 1.99 | -3.08% | +0.29% |
| 2.0% pick H2 -> test H1 largest: not picked | 22 | 50% | +0.62% | -0.19% | 1.12 | -5.53% | -0.23% |
| 2.0% pick H2 -> test H1 largest: all 16 | 32 | 53% | +0.83% | +0.19% | 1.86 | -5.97% | +0.26% |
| 3.0% pick H1 -> test H2 split: picked AMD,MU,MRVL,PLTR | 9 | 67% | +1.01% | +0.30% | 1.36 | -2.47% | -0.23% |
| 3.0% pick H1 -> test H2 split: not picked | 10 | 80% | +1.84% | +2.26% | 3.13 | -1.64% | +1.06% |
| 3.0% pick H1 -> test H2 split: all 16 | 13 | 69% | +1.10% | +1.18% | 1.99 | -2.47% | +0.41% |
| 3.0% pick H1 -> test H2 largest: picked AMD,MU,MRVL,PLTR | 9 | 78% | +1.45% | +0.88% | 1.51 | -2.75% | -0.00% |
| 3.0% pick H1 -> test H2 largest: not picked | 10 | 80% | +1.88% | +2.30% | 3.12 | -1.64% | +1.09% |
| 3.0% pick H1 -> test H2 largest: all 16 | 13 | 77% | +1.50% | +1.18% | 1.99 | -2.75% | +0.42% |
| 3.0% pick H2 -> test H1 split: picked AMD,MU,INTC | 16 | 62% | +1.09% | +0.39% | 1.89 | -1.72% | +0.13% |
| 3.0% pick H2 -> test H1 split: not picked | 15 | 60% | +1.23% | +0.55% | 1.70 | -3.86% | +0.20% |
| 3.0% pick H2 -> test H1 split: all 16 | 21 | 67% | +1.24% | +0.65% | 2.70 | -3.08% | +0.64% |
| 3.0% pick H2 -> test H1 largest: picked AMD,MU,INTC | 16 | 69% | +1.20% | +0.73% | 1.86 | -2.41% | +0.14% |
| 3.0% pick H2 -> test H1 largest: not picked | 15 | 60% | +1.31% | +0.67% | 1.68 | -4.52% | +0.13% |
| 3.0% pick H2 -> test H1 largest: all 16 | 21 | 62% | +1.22% | +0.67% | 2.08 | -4.03% | +0.36% |

**Verdict:** name selection does not carry over. Picking on H1 and testing on H2 failed at every threshold: the unpicked names did better. Picking on H2 and testing on H1 helped at 1.5% and 2% but not at 3%. The honest conclusion is that the edge belongs to *large opening gaps in liquid, high-beta names that keep going in the first 15 minutes*, not to a lucky list of tickers. The original five are still the best-supported core because they gap most often and have the most trades. Adding names mainly fills empty days at a lower edge.

### Earnings days, and the disaster stop

| Set (2% gap) | Earnings-reaction days | Non-earnings days |
|---|---|---|
| Orig 5, per trade | 3 trades, −0.16% | 76 trades, +1.19% (t 4.35) |
| Orig 5, largest per day | 3 days, −0.16% | 35 days, +1.40% (t 3.36), ex-top-3 +0.91% |
| All 16, largest per day | 7 days, +1.23% (t 1.1), ex-top-3 −0.60% | 54 days, +0.96% (t 3.10), ex-top-3 +0.64% |

Skip earnings-reaction days. It costs nothing and avoids the widest spreads.

Intraday adverse excursion on the 79 orig-5 trades: 18 dipped 2% or more below entry, 8 dipped 3% or more, 3 dipped 4% or more, and the worst dip was −5.3%. Most then recovered by 3:45, since no closed trade lost more than 3.0%. A stop costs edge: no stop gives +1.14% a trade, a −5% stop +1.06%, −4% +0.97% and −3% +0.93%. **v4 moves the disaster stop from −4% to −5%.** It exists for gap-down or news shocks the sample does not contain, not to improve the backtest.

## 2. Leveraged ETF (SOXL)

#### Overnight 3:45 -> 9:30 open
| Rule | n | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean, t | H2 n, mean, t |
|---|---|---|---|---|---|---|---|---|---|---|---|
| SMH | 122 | 54% | +0.21% | +0.38% | 1.09 | +26.03% | -5.92% | -11.76% | +0.10% | 61, +0.35%, 1.16 | 61, +0.07%, 0.30 |
| SOXL | 122 | 57% | +0.86% | +1.22% | 1.27 | +101.50% | -20.00% | -38.05% | +0.46% | 61, +1.55%, 1.47 | 61, +0.16%, 0.19 |
| SOXL @0.20% cost | 122 | 56% | +0.76% | +1.12% | 1.12 | +78.41% | -20.10% | -39.68% | +0.36% | 61, +1.45%, 1.37 | 61, +0.06%, 0.07 |

### SMH gap signal, trade SMH vs SOXL (9:45 -> 3:45)

| Rule | n | Win% | Mean | Median | t | Total | Worst | Max DD | Mean ex top-3 | H1 n, mean, t | H2 n, mean, t |
|---|---|---|---|---|---|---|---|---|---|---|---|
| SMH gap >= 1.5%, trade SMH | 19 | 68% | +0.46% | +1.05% | 1.13 | +8.84% | -4.66% | -4.66% | +0.10% | 13, +0.51%, 0.97 | 6, +0.36%, 0.53 |
| SMH gap >= 1.5%, trade SOXL | 19 | 68% | +1.77% | +4.50% | 1.25 | +34.79% | -16.04% | -16.04% | +0.58% | 13, +1.98%, 1.06 | 6, +1.32%, 0.61 |
| SMH gap >= 2.0%, trade SMH | 14 | 71% | +0.69% | +1.08% | 1.91 | +9.97% | -1.94% | -3.82% | +0.29% | 10, +0.91%, 2.30 | 4, +0.15%, 0.18 |
| SMH gap >= 2.0%, trade SOXL | 14 | 71% | +2.56% | +3.95% | 2.03 | +40.49% | -5.96% | -12.52% | +1.22% | 10, +3.25%, 2.28 | 4, +0.84%, 0.31 |
| SMH gap >= 3.0%, trade SMH | 6 | 83% | +0.73% | +1.04% | 1.73 | +4.42% | -1.20% | -1.20% | +0.01% | 4, +0.36%, 0.65 | 2, +1.47%, 22.40 |
| SMH gap >= 3.0%, trade SOXL | 6 | 83% | +2.59% | +3.91% | 1.69 | +16.19% | -4.34% | -4.34% | -0.08% | 4, +1.25%, 0.62 | 2, +5.28%, 3979.90 |

**Volatility decay and tail risk (close-to-close, 2026-04-15 to 10-07, 122 days):**

| | SMH | SOXL |
|---|---|---|
| Buy and hold | +38.3% | +86.1% |
| 3x SMH, rebalanced daily, no fees (theoretical) | | +94.5% |
| Naive "3x the SMH return" | | +114.9% |
| Daily volatility | 2.85% | 9.95% (beta to SMH 3.4) |
| Max drawdown | −24.6% | **−69.4%** (about $300 to $92, June to July) |
| Worst day | −9.4% | **−30.7%** |
| H1 / H2 | +29.5% / +6.8% | +93.7% / **−3.9%** |

- **Decay:** at SMH's daily volatility, a daily-reset 3x fund loses about 3σ², roughly 0.24% a day, compared with three times SMH's log return. That is the gap between +115% (naive 3x) and +94.5% (3x daily). Real SOXL lost a further ~8 points to fees and to tracking the SOX index rather than SMH. In the choppy H2, SMH still rose 6.8% while SOXL fell.
- **Tails:** the five worst SOXL overnights were −20.0%, −16.7%, −16.7%, −14.6% and −14.5%. One gap-day SOXL trade (1.5% threshold) lost 16% before 3:45. Neither stop helps here: the overnight losses happen at the open, below any stop, and the intraday one exceeds the −5% disaster stop. At full size, a single bad night uses up most of the 20% drawdown budget. Simulated at 100% of equity, overnight-only SOXL had a **−27% max drawdown over the full window and −43% in H2 alone** (table in section 3).
- **Kelly** on SOXL overnight is 1.5x with full-sample means, 0.3x with the mean cut by one standard error, 1.0x with one −15% night per 50 added, and **0** with both haircuts. Even the gap-day variant (t = 2.0) rests on 4 H2 trades. **Verdict: no SOXL in v4.**

## 3. Sizing

### Kelly fractions (fraction of equity per trade or night)

Kelly is f* = argmax E[log(1+f·r)] over the empirical per-day returns. Stressed versions: (a) every return shifted down by one standard error of the mean; (b) one −15% loss added per 50 trades, a tail event the sample does not contain; (c) both.

| Rule | n | Mean | SD | Kelly (raw) | Kelly (mean − 1 SE) | Kelly (+ tail) | Kelly (both) | **Half-Kelly (both), capped at 1.0** |
|---|---|---|---|---|---|---|---|---|
| Gap, orig 5, largest gap | 35 | +1.40% | +2.47% | 29.40 | 20.50 | 4.15 | 2.95 | 1.00 |
| Gap, all 16, largest gap | 54 | +0.96% | +2.27% | 25.00 | 15.80 | 4.15 | 2.85 | 1.00 |
| Overnight SMH | 122 | +0.21% | +2.15% | 4.45 | 0.40 | 0.00 | 0.00 | 0.00 |
| Overnight SOXL | 122 | +0.86% | +7.47% | 1.50 | 0.30 | 1.00 | 0.00 | 0.00 |

The gap rule's raw Kelly (25 to 29x) is meaningless. It only says the sample's worst day was −2.75%. Even fully stressed, Kelly is about 2.9x, so half-Kelly is still above 1 and the cash account caps it at **100% of equity**. The SMH overnight rule's Kelly falls from 4.5x to 0.4x with a one-SE haircut (its t is only 1.1), and to 0 with a tail event. The "half-Kelly" policy below therefore uses **gap 100% + overnight 20%**, which is half of the 0.4x one-SE figure.

### Settlement-aware equity simulation ($2,400 start)

How the simulation works. Each morning, yesterday's sale proceeds settle. At 9:30 the overnight position is sold, and those proceeds stay unsettled until the next morning. At 9:45 the gap buy uses settled cash only. At 3:45 the gap position is sold (proceeds settle the next morning), and the overnight buy uses whatever settled cash is left. Consequences: a gap dollar can be reused every day. **An overnight dollar is locked for two sessions** (bought day d, sold day d+1 at 9:30, settled day d+2). It also blocks the day d+1 gap trade. Max drawdown is marked at each trade's worst 10-minute low as well as at exits and the 4:00 close. Earnings-reaction gap days are skipped. No loss limits are applied, so the drawdowns are raw. "Gap days short of cash" counts gap days on which the wanted size exceeded settled cash.

| Policy | Final $ (from $2,400) | Return | Max DD (incl. intraday lows) | Gap trades | Overnights | Gap days short of cash | H1 return / DD | H2 return / DD |
|---|---|---|---|---|---|---|---|---|
| v3 current ($800 caps, $400 reserve), orig 5 + SMH overnight | $2,999 | +24.95% | -3.91% | 35 | 122 | 0 | +16.14% / -2.95% | +7.10% / -3.36% |
| 50% / trade: gap 50% + overnight 50%, orig 5 | $3,408 | +42.00% | -6.43% | 35 | 94 | 26 | +25.25% / -5.01% | +11.26% / -6.42% |
| 100% / trade: gap 100% + overnight 100%, orig 5 | $3,364 | +40.16% | -12.71% | 25 | 49 | 10 | +18.85% / -7.05% | +17.93% / -8.58% |
| Half-Kelly (stressed): gap 100% + overnight 20%, orig 5 | $3,769 | +57.02% | -6.02% | 35 | 87 | 25 | +31.87% / -4.76% | +18.17% / -5.19% |
| Gap only 50%, orig 5 | $3,058 | +27.41% | -3.38% | 35 | 0 | 0 | +16.57% / -2.64% | +9.30% / -2.52% |
| Gap only 100%, orig 5 | $3,870 | +61.26% | -6.68% | 35 | 0 | 0 | +35.40% / -5.28% | +19.10% / -5.00% |
| Overnight only 100% SMH | $3,082 | +28.41% | -7.89% | 0 | 61 | 0 | +11.84% / -6.56% | -6.47% / -14.36% |
| Gap only 50%, all 16 | $3,097 | +29.03% | -5.46% | 54 | 0 | 0 | +16.66% / -3.23% | +10.60% / -2.52% |
| Gap only 100%, all 16 | $3,965 | +65.20% | -10.67% | 54 | 0 | 0 | +35.40% / -6.38% | +22.01% / -5.00% |
| Gap 100%, all 16, top-2 split, max 60%/name | $3,480 | +44.99% | -9.40% | 54 | 0 | 0 | +25.02% / -5.35% | +15.97% / -4.53% |
| Gap 100%, orig 5, top-2 split, max 60%/name | $3,449 | +43.71% | -6.81% | 35 | 0 | 0 | +27.86% / -3.79% | +12.39% / -4.53% |
| Gap 100% + overnight 20%, all 16 | $3,854 | +60.56% | -9.32% | 54 | 69 | 28 | +31.59% / -5.70% | +21.09% / -5.19% |
| Overnight only 100% SOXL | $5,054 | +110.60% | -27.34% | 0 | 61 | 0 | +48.97% / -21.69% | -22.07% / -42.62% |
| Overnight only 20% SOXL | $2,917 | +21.55% | -8.55% | 0 | 122 | 0 | +16.57% / -6.85% | +1.43% / -6.45% |
| Gap 100% orig5 + overnight 20% SOXL | $4,240 | +76.66% | -7.11% | 35 | 87 | 25 | +41.44% / -6.85% | +21.49% / -7.11% |

Gap fraction (rows) × overnight fraction (columns), orig 5. Each cell is the final $ / max drawdown:

| gap \ overnight | 0 | 10% | 20% | 30% | 50% | 100% |
|---|---|---|---|---|---|---|
| 0 | 2,400 / 0% | 2,462 / −1.4% | 2,525 / −2.7% | 2,588 / −4.1% | 2,714 / −6.7% | 3,082 / −7.9% |
| 25% | 2,711 / −1.7% | 2,782 / −1.9% | 2,853 / −2.8% | 2,923 / −4.2% | 3,048 / −6.2% | 3,161 / −8.1% |
| 50% | 3,058 / −3.4% | 3,137 / −3.4% | 3,217 / −3.7% | 3,288 / −4.0% | 3,408 / −6.4% | 3,239 / −8.6% |
| 75% | 3,443 / −5.0% | 3,532 / −5.1% | 3,607 / −5.2% | 3,611 / −5.1% | 3,508 / −5.2% | 3,303 / −9.7% |
| **100%** | **3,870 / −6.7%** | 3,820 / −6.4% | 3,769 / −6.0% | 3,716 / −5.7% | 3,610 / −5.5% | 3,364 / −12.7% |

**Answer to Q3.** In this backtest, growth peaks at **gap = 100% of equity, overnight = 0**: $3,870, max drawdown −6.7%, well inside the 20% cap. At 100% gap size, every added overnight sleeve lowers the final value because it takes settled cash from the next morning's gap trade. A tiered universe (orig 5 at 100%, the other names at 50% only on days with no orig-5 signal) reached $4,035 with a −7.2% drawdown (H1 $3,211 / −5.3%, H2 $3,016 / −5.0%). That variant was built after seeing the data, so it is weakly supported. With costs doubled (0.20% / 0.40%), gap-only at 100% still reaches $3,739 (orig 5) or $3,688 (all 16). The only policy here that breaks the 20% cap is SOXL overnight at 100% (−27%, and −43% in H2). **Why not just run 100% from day one:** the backtest drawdown comes from 35 trades in a bull market with no left tail. One −10% single-name shock at 100% size uses half the drawdown budget. v4 therefore ramps up from 50% and caps single stocks below ETFs.

## 4. One new rule, stated before testing (one shot)

**Hypothesis, written to `g2/q4_hypothesis.txt` at 19:50 UTC before any test was run:** "SMH trend hold. At 3:45, if SMH's 3:45 price is above the 20-day simple average of daily closes, hold SMH (buy if flat); if it is below, sell and hold cash. Positions carry across days. 0.10% per round trip. Expectation: it captures most of the trend with a smaller drawdown than buy-and-hold, and beats the nightly overnight rule on return per unit of drawdown."

| Period | Sessions | Days in market | Round trips | Win% | Mean per trip | Worst trip | Total | Max DD | SMH B&H (3:45 to 3:45) | B&H max DD |
|---|---|---|---|---|---|---|---|---|---|---|
| Full | 123 | 79 (64%) | 10 | 30% | +1.15% | -6.89% | +7.72% | -29.47% | +33.89% | -23.04% |
| H1 | 61 | 49 (80%) | 5 | 40% | +3.29% | -6.89% | +13.64% | -19.88% | +29.80% | -13.17% |
| H2 | 62 | 30 (48%) | 5 | 20% | -0.98% | -4.66% | -5.21% | -14.63% | +0.63% | -14.87% |

**Result: rejected.** It whipsawed. Its 10 round trips were 3 wins and 7 losses (+27.9% on the first, then mostly −2% to −7%). It made +7.7% against +33.9% for buy-and-hold, and its max drawdown was *worse* (−29% vs −23%). In H2 it lost 5.2% while SMH was flat. A 20-day moving average is too slow for SMH's 2.9% daily volatility in this regime. The idea of using it as a filter on the overnight rule, suggested in the prior study, is therefore not supported either.

---

## Proposed rule set v4 (replaces v3 when Jeffery approves)

**What changes:** the account trades one rule (gap-up momentum) at a size that scales with equity. Rule O (the overnight hold) is retired: its H2 edge was +0.07% a night (t 0.3), and in a cash account it takes settled cash from the gap rule. SOXL is out.

### Universe
- **Tier A (full size):** NVDA, AMD, MU, INTC, SMH.
- **Tier B (half size, only on days when no Tier A name qualifies):** AVGO, MRVL, PLTR, TSLA, META, AMZN, MSFT, GOOGL, AAPL, QQQ. TSM is excluded: its price discovery happens in Taiwan hours, it costs 0.20%, and it was negative here. Tier B is on probation: drop it if its first 10 live trades average below 0.
- Skip any name on its earnings-reaction day (the session after an after-close report, or the day of a pre-open report), and skip any name with earnings due within 2 sessions.

### Signal (9:45 am check)
- The 9:30 open is at least **+2.0%** above the prior close, AND the 9:45 price is above the 9:30 open.
- Take **only the largest qualifying gap** (Tier A first). One gap position a day.
- Skip if the 9:45 bid-ask spread is over 0.15% of price, or if settled buying power is under $200.

### Sizing (fraction of current equity = settled cash + positions)
- **Ramp:** 50% of equity for the first 10 live v4 trades. Move to 75% for trades 11 to 20 if the live average is at least 0 and no limit below has been hit. After that, **100% for SMH and 85% for a single stock**. Tier B always gets half of the Tier A size.
- Never more than settled buying power minus $25. Never use unsettled proceeds.
- Hybrid execution as in v3: whole shares with a limit at the ask, the remainder as a fractional market order.

### Stops and exit
- **Disaster stop at entry × 0.95** (stop_market GTC on the whole shares).
- **Add a 12:30 pm check:** if the position is at or below −5% (which would include the fractional piece that cannot carry a stop), sell everything at market.
- **3:45 pm:** sell everything no matter what (cancel the stop first; whole shares with a limit at the bid, fractional piece at market). No overnight holds.

### Loss limits (as % of equity, so they scale with size)
- **Per trade:** max planned loss is about 5% × position size, at most 4.25% of equity.
- **Daily:** only one trade a day, so the per-trade limit covers it.
- **Weekly:** −6% of equity from Monday's open: no new entries until Jeffery's Sunday review.
- **Losing streak:** 4 losses in a row (the backtest maximum was 2): cut size to 50% and report. 6 in a row: stop the rule.
- **Drawdown:** −10% from the equity high-water mark: size back to 50% until a new high. **−15% from the high-water mark: kill switch.** Close everything, stop all trading, and hand over for a full review. This leaves 5 points of slack below the 20% hard cap for gaps through stops.
- **Live edge check:** after 20 live trades, if the mean live return after real fills is ≤ 0, stop and review. Also after 20 trades: if average live slippage exceeds 0.25% round trip, raise the threshold to 3% or stop.

### Schedule (CRON_TZ=America/New_York, Mon to Fri)
- 9:45 am: gap entry. 12:30 pm: −5% check. 3:45 pm: exit and daily summary. (The 9:31 overnight exit and 3:45 overnight entry are removed.)

**Backtest of the core (Tier A, largest gap, earnings days skipped):** 35 days (28% of sessions), 71% wins, +1.40% a day, median +0.87%, t = 3.36, worst day −2.75%, ex-top-3 +0.91%, H1 +1.55% (t 3.0) / H2 +1.21% (t 1.7). Simulated at 100% of equity: $2,400 to $3,870, max drawdown −6.7%. With Tier B at half size: $4,035, −7.2%. Treat these as an upper bound, not a forecast.

## What could go wrong (read this before approving)

This is a 6-month window inside a powerful semiconductor bull run: AMD +153%, MU +134%, SMH +38%. Opening gaps that keep going are exactly what a momentum market produces. In a bear market or a choppy range, gap-ups are more often sold into, and the rule could easily have a negative mean. The core result rests on **35 trading days**, with no single closed loss worse than −3%. That is not evidence the left tail is thin; it is evidence that it has not shown up yet. One gap-up followed by a news reversal (a guidance cut, an export ban, a halt) could cost 5 to 10% at full size, or more if it gaps through the stop, and two of those would trigger the kill switch. Across this study and the last, well over 100 rule, threshold, universe and sizing variants were tested, so a t of about 3 is weaker than it looks. Some of the edge is surely selection: the original five were picked after seeing the data, and the tiered universe, the −5% stop and the 85% single-stock cap were all chosen with these results in view. The price proxies (bar midpoints) and the 0.10% cost assume calm fills. Real 9:45 fills on gap days in volatile names may be worse. Doubling costs still left a profit here, but tripling them in a weaker market would not. Finally, 100% sizing turns every one of these errors into a full-account event. That is why v4 ramps up from 50%, checks the live edge after 20 trades, and stops at −15% from the high-water mark rather than waiting for −20%. Expected outcome: real live results somewhere between flat and half of the backtest, with an actual chance of hitting the kill switch within the first few months.
