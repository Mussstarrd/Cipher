# Study 2026-10-07: simple long-only stock rules for a $2,500 cash account

**Universe:** NVDA, AMD, MU, INTC, SMH, QQQ. **Window:** trade entries 2026-04-15 to 2026-10-06/07 (122 sessions; 121 overnight holds per symbol). **Data:** Robinhood `get_equity_historicals`, 10-minute regular-session bars (split-adjusted), 39 bars every session, no gaps. Read-only. No orders were placed, reviewed or cancelled.
**Costs:** 0.05% slippage per side (0.10% round trip), taken off every trade. All returns below are **after costs**, except the base-rate table.

## How prices were approximated (10-min bars, labels are bar starts in UTC; session is 13:30-20:00 UTC)
| Time (ET) | Proxy |
|---|---|
| 9:30 open | open of 13:30 bar |
| 9:45 | midpoint (open+close)/2 of 13:40 bar |
| 10:00 | open of 14:00 bar |
| 10:30 | open of 15:00 bar |
| 3:15 | midpoint of 19:10 bar |
| 3:30 | open of 19:30 bar |
| 3:45 | midpoint of 19:40 bar |
| 4:00 close | close of 19:50 bar (last trade, not the official closing auction) |

Rule details: "red on the day" = 3:45 price below prior close. "Late dump" = 3:45 price below 3:15 price. "Late ramp" = 3:45 price at least 0.3% above 3:15 price. Rule 3 stop: checked against 10-min lows from 9:50 to 3:40; filled at the stop, or at the bar open if the bar opened below it. Rule 4: +3% target checked against 10-min highs from 10:30 on entry day through day+2; one open position per symbol at a time (no stacking). Signals that need the next day are dropped at the end of the window.

## How to read the tables
- **Pooled (per trade)** treats every symbol-trade as one sample. Trades on the same night are highly correlated (semis move together), so this t-stat **overstates** significance. Compounded total and drawdown are not meaningful here (trades overlap in time) and are shown as n/a.
- **Pooled portfolio (equal split by day)** splits capital equally across all signals on a given day. This is the honest account-level view: n = number of signal days, and its t-stat is the one to trust.
- **Mean ex top-3** = mean per trade after removing the 3 best trades. If it turns ≤ 0, the edge depends on a few outliers.
- t-stat = mean / (sd / sqrt(n)). About 2 is the usual bar, and with this many variants tested (14 rule rows × 6 symbols) some rows will clear 2 by chance.

## Base rate: buy and hold (close-to-close, before costs)

| Symbol | Days | Daily mean (c-c) | Win% | t-stat | Total B&H | Max DD | Sum overnight (c->o) | Sum intraday (o->c) | Last 30 min (3:30->4:00) mean |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 122 | +0.19% | 51% | 0.85 | +21.30% | -19.38% | +38.53% | -12.44% | -0.06% |
| AMD | 122 | +0.88% | 57% | 2.02 | +153.33% | -26.04% | +65.51% | +53.06% | +0.05% |
| MU | 122 | +0.86% | 55% | 1.64 | +133.73% | -39.20% | +117.20% | +7.61% | +0.03% |
| INTC | 122 | +0.62% | 48% | 1.25 | +77.89% | -41.93% | +92.42% | -7.55% | +0.04% |
| SMH | 122 | +0.31% | 61% | 1.18 | +38.32% | -24.64% | +47.82% | -6.43% | -0.02% |
| QQQ | 122 | +0.16% | 53% | 1.34 | +20.55% | -11.34% | +15.99% | +3.93% | -0.02% |
| Equal-weight 6 | 122 | +0.50% | 61% | 1.71 | +72.89% | -25.01% | | | |

Note: in "Last 30 min mean", the 3:30 entry uses the 19:30 bar open. Overnight and intraday sums are compounded log-sums, so overnight and intraday together give the total.

## Results by rule (after 0.10% round-trip cost)

### 1a Overnight 3:45->10:00, every day

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 121 | 49% | +0.09% | -0.02% | +9.12% | -4.39% | -21.76% | 0.56 | -0.03% |
| AMD | 121 | 52% | +0.44% | +0.12% | +55.04% | -8.65% | -21.29% | 1.22 | +0.13% |
| MU | 121 | 58% | +0.55% | +0.55% | +73.11% | -9.30% | -26.16% | 1.38 | +0.27% |
| INTC | 121 | 50% | +0.43% | -0.29% | +49.63% | -10.38% | -30.12% | 1.05 | +0.09% |
| SMH | 121 | 58% | +0.15% | +0.40% | +15.76% | -5.59% | -16.73% | 0.71 | +0.04% |
| QQQ | 121 | 47% | +0.00% | -0.03% | -0.05% | -2.63% | -9.10% | 0.05 | -0.05% |
| **Pooled (per trade)** | 726 | 52% | +0.28% | +0.13% | n/a | -10.38% | n/a | 2.27 | +0.21% |
| **Pooled portfolio (equal split by day)** | 121 | 56% | +0.28% | +0.48% | +34.34% | -5.95% | -17.91% | 1.19 | +0.12% |

### 1b ... only if red on day

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 58 | 41% | -0.02% | -0.23% | -1.76% | -2.99% | -15.62% | -0.07 | -0.27% |
| AMD | 55 | 49% | +0.19% | -0.05% | +6.97% | -8.65% | -25.60% | 0.38 | -0.34% |
| MU | 55 | 56% | +0.49% | +0.52% | +23.04% | -9.27% | -23.63% | 0.75 | -0.15% |
| INTC | 64 | 48% | +0.22% | -0.34% | +8.60% | -10.38% | -28.22% | 0.41 | -0.23% |
| SMH | 48 | 52% | +0.02% | +0.29% | -0.15% | -5.59% | -13.65% | 0.06 | -0.26% |
| QQQ | 56 | 50% | -0.03% | +0.00% | -2.09% | -2.63% | -9.08% | -0.24 | -0.14% |
| **Pooled (per trade)** | 336 | 49% | +0.15% | -0.06% | n/a | -10.38% | n/a | 0.82 | +0.04% |
| **Pooled portfolio (equal split by day)** | 94 | 51% | +0.07% | +0.18% | +2.08% | -7.98% | -25.68% | 0.22 | -0.18% |

### 1c ... only if down last 30 min

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 68 | 53% | +0.16% | +0.16% | +10.31% | -4.39% | -16.95% | 0.70 | -0.05% |
| AMD | 55 | 47% | +0.83% | -0.31% | +51.30% | -6.32% | -9.90% | 1.53 | +0.14% |
| MU | 57 | 53% | -0.22% | +0.32% | -17.18% | -9.27% | -46.52% | -0.35 | -0.86% |
| INTC | 53 | 55% | +0.92% | +0.32% | +52.53% | -7.75% | -31.70% | 1.31 | +0.15% |
| SMH | 53 | 60% | +0.26% | +0.42% | +12.96% | -5.19% | -15.63% | 0.80 | +0.01% |
| QQQ | 50 | 52% | +0.07% | +0.09% | +3.16% | -1.94% | -8.14% | 0.47 | -0.07% |
| **Pooled (per trade)** | 336 | 53% | +0.33% | +0.26% | n/a | -9.27% | n/a | 1.71 | +0.18% |
| **Pooled portfolio (equal split by day)** | 99 | 55% | +0.34% | +0.48% | +33.49% | -6.21% | -20.37% | 1.10 | +0.03% |

### 1d ... skip if late ramp >=+0.3%

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 110 | 51% | +0.13% | +0.07% | +13.01% | -4.39% | -19.22% | 0.76 | -0.01% |
| AMD | 81 | 47% | +0.55% | -0.25% | +47.19% | -8.65% | -13.80% | 1.27 | +0.08% |
| MU | 86 | 58% | +0.40% | +0.68% | +30.35% | -9.27% | -33.15% | 0.86 | +0.00% |
| INTC | 86 | 51% | +0.35% | +0.13% | +23.68% | -8.78% | -31.31% | 0.70 | -0.14% |
| SMH | 105 | 57% | +0.05% | +0.27% | +2.89% | -5.59% | -20.73% | 0.24 | -0.08% |
| QQQ | 117 | 48% | +0.01% | -0.03% | +1.18% | -2.63% | -8.09% | 0.17 | -0.04% |
| **Pooled (per trade)** | 585 | 52% | +0.22% | +0.13% | n/a | -9.27% | n/a | 1.73 | +0.14% |
| **Pooled portfolio (equal split by day)** | 120 | 56% | +0.21% | +0.33% | +23.92% | -5.67% | -19.14% | 0.96 | +0.04% |

### 2a Overnight 3:45->9:30 open, every day

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 121 | 56% | +0.14% | +0.21% | +16.86% | -3.49% | -8.87% | 1.16 | +0.05% |
| AMD | 121 | 57% | +0.41% | +0.25% | +53.88% | -8.16% | -16.36% | 1.35 | +0.15% |
| MU | 121 | 59% | +0.64% | +0.57% | +96.00% | -9.51% | -22.87% | 1.70 | +0.36% |
| INTC | 121 | 54% | +0.52% | +0.32% | +70.91% | -8.30% | -21.20% | 1.41 | +0.17% |
| SMH | 121 | 55% | +0.23% | +0.39% | +28.04% | -5.92% | -11.76% | 1.16 | +0.12% |
| QQQ | 121 | 52% | +0.01% | +0.03% | +0.43% | -2.89% | -7.47% | 0.09 | -0.05% |
| **Pooled (per trade)** | 726 | 55% | +0.33% | +0.22% | n/a | -9.51% | n/a | 2.97 | +0.25% |
| **Pooled portfolio (equal split by day)** | 121 | 56% | +0.33% | +0.39% | +43.20% | -6.02% | -11.89% | 1.51 | +0.17% |

### 2b ... only if red

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 58 | 60% | +0.29% | +0.28% | +17.52% | -2.84% | -5.04% | 1.70 | +0.11% |
| AMD | 55 | 58% | +0.41% | +0.65% | +22.07% | -7.23% | -14.43% | 1.02 | +0.09% |
| MU | 55 | 56% | +0.69% | +0.42% | +38.25% | -7.57% | -17.49% | 1.13 | +0.06% |
| INTC | 64 | 58% | +0.68% | +0.64% | +48.12% | -8.30% | -11.98% | 1.54 | +0.28% |
| SMH | 48 | 52% | +0.28% | +0.52% | +13.51% | -4.41% | -7.73% | 0.97 | +0.01% |
| QQQ | 56 | 55% | +0.05% | +0.18% | +2.65% | -2.89% | -4.60% | 0.38 | -0.06% |
| **Pooled (per trade)** | 336 | 57% | +0.41% | +0.31% | n/a | -8.30% | n/a | 2.62 | +0.29% |
| **Pooled portfolio (equal split by day)** | 94 | 54% | +0.16% | +0.33% | +12.52% | -7.21% | -17.70% | 0.58 | -0.05% |

### 2c ... only if late dump

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 68 | 54% | +0.17% | +0.22% | +11.78% | -3.49% | -8.32% | 1.04 | +0.02% |
| AMD | 55 | 56% | +0.48% | +0.17% | +25.47% | -8.16% | -15.73% | 0.96 | -0.10% |
| MU | 57 | 53% | -0.03% | +0.53% | -6.17% | -9.51% | -42.01% | -0.06 | -0.41% |
| INTC | 53 | 55% | +0.67% | +0.32% | +34.54% | -7.99% | -34.64% | 1.00 | -0.12% |
| SMH | 53 | 60% | +0.30% | +0.61% | +15.97% | -5.16% | -14.28% | 1.03 | +0.09% |
| QQQ | 50 | 56% | +0.03% | +0.08% | +1.44% | -1.82% | -7.73% | 0.25 | -0.08% |
| **Pooled (per trade)** | 336 | 56% | +0.27% | +0.23% | n/a | -9.51% | n/a | 1.55 | +0.12% |
| **Pooled portfolio (equal split by day)** | 99 | 56% | +0.20% | +0.28% | +17.13% | -7.23% | -25.57% | 0.72 | -0.03% |

### 2d ... skip late ramp

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 110 | 57% | +0.15% | +0.25% | +16.55% | -3.49% | -9.91% | 1.18 | +0.05% |
| AMD | 81 | 53% | +0.28% | +0.11% | +19.49% | -8.16% | -24.32% | 0.72 | -0.12% |
| MU | 86 | 59% | +0.45% | +0.55% | +38.36% | -9.51% | -30.88% | 1.10 | +0.20% |
| INTC | 86 | 58% | +0.50% | +0.51% | +42.49% | -7.99% | -28.44% | 1.10 | +0.01% |
| SMH | 105 | 53% | +0.06% | +0.25% | +4.31% | -5.92% | -19.15% | 0.30 | -0.05% |
| QQQ | 117 | 51% | -0.01% | +0.03% | -1.71% | -2.89% | -9.62% | -0.14 | -0.06% |
| **Pooled (per trade)** | 585 | 55% | +0.21% | +0.18% | n/a | -9.51% | n/a | 1.88 | +0.13% |
| **Pooled portfolio (equal split by day)** | 120 | 53% | +0.23% | +0.23% | +28.11% | -6.02% | -15.71% | 1.18 | +0.10% |

### 3 Gap-up >=2% momentum 9:45->3:45

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 5 | 40% | +0.11% | -0.27% | +0.48% | -1.88% | -2.87% | 0.14 | -1.45% |
| AMD | 21 | 71% | +1.45% | +0.94% | +34.66% | -2.18% | -2.83% | 3.27 | +0.99% |
| MU | 20 | 55% | +1.40% | +0.17% | +31.12% | -2.75% | -4.47% | 2.17 | +0.48% |
| INTC | 19 | 63% | +1.11% | +0.89% | +22.48% | -3.02% | -6.06% | 1.66 | +0.25% |
| SMH | 14 | 71% | +0.69% | +1.08% | +9.97% | -1.94% | -3.82% | 1.91 | +0.29% |
| QQQ | 1 | 100% | +0.35% | +0.35% | +0.35% | +0.35% | +0.00% | nan | +nan% |
| **Pooled (per trade)** | 80 | 64% | +1.13% | +0.88% | n/a | -3.02% | n/a | 4.26 | +0.87% |
| **Pooled portfolio (equal split by day)** | 36 | 67% | +1.02% | +0.58% | +43.04% | -2.34% | -3.89% | 3.13 | +0.66% |

### 3s Gap-up + -1.5% stop

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 5 | 40% | +0.16% | -0.27% | +0.77% | -1.60% | -2.59% | 0.22 | -1.30% |
| AMD | 21 | 57% | +0.95% | +0.88% | +21.44% | -1.60% | -5.09% | 1.94 | +0.42% |
| MU | 20 | 45% | +0.87% | -0.28% | +17.77% | -1.60% | -5.65% | 1.25 | -0.15% |
| INTC | 19 | 37% | +0.40% | -1.60% | +6.99% | -1.98% | -11.27% | 0.56 | -0.60% |
| SMH | 14 | 64% | +0.49% | +0.86% | +6.95% | -1.60% | -3.87% | 1.25 | +0.04% |
| QQQ | 1 | 100% | +0.35% | +0.35% | +0.35% | +0.35% | +0.00% | nan | +nan% |
| **Pooled (per trade)** | 80 | 50% | +0.66% | +0.15% | n/a | -1.98% | n/a | 2.35 | +0.39% |
| **Pooled portfolio (equal split by day)** | 36 | 47% | +0.54% | -0.05% | +20.64% | -1.60% | -4.71% | 1.57 | +0.14% |

### 4 Flush buy 10:30 -> +3% or D+2 close

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 6 | 17% | -3.22% | -4.14% | -17.97% | -5.03% | -19.17% | -3.12 | -4.99% |
| AMD | 13 | 62% | -0.32% | +2.90% | -5.58% | -12.28% | -16.87% | -0.23 | -1.45% |
| MU | 13 | 69% | +1.17% | +2.90% | +13.33% | -11.93% | -22.32% | 0.64 | -0.94% |
| INTC | 10 | 80% | +2.17% | +2.90% | +23.17% | -5.02% | -5.02% | 1.87 | +0.96% |
| SMH | 13 | 54% | -0.11% | +0.72% | -2.29% | -7.84% | -11.63% | -0.11 | -1.15% |
| QQQ | 3 | 33% | -0.19% | -0.46% | -0.63% | -2.29% | -2.74% | -0.15 | +nan% |
| **Pooled (per trade)** | 58 | 59% | +0.20% | +2.90% | n/a | -12.28% | n/a | 0.31 | -0.28% |
| **Pooled portfolio (equal split by day)** | 21 | 57% | -0.41% | +1.19% | -10.23% | -11.93% | -26.86% | -0.42 | -1.13% |

### 5 Intraday momentum (up at 10:00) 3:30->4:00

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 71 | 46% | -0.08% | -0.01% | -5.75% | -2.79% | -6.39% | -1.32 | -0.12% |
| AMD | 64 | 50% | +0.12% | +0.03% | +7.43% | -2.30% | -8.01% | 1.07 | +0.02% |
| MU | 68 | 54% | +0.02% | +0.04% | +0.87% | -3.19% | -13.29% | 0.14 | -0.10% |
| INTC | 61 | 51% | -0.01% | +0.02% | -0.79% | -3.44% | -12.71% | -0.06 | -0.11% |
| SMH | 71 | 44% | -0.05% | -0.07% | -3.27% | -1.34% | -6.16% | -0.84 | -0.10% |
| QQQ | 67 | 30% | -0.07% | -0.07% | -4.91% | -0.62% | -5.02% | -2.77 | -0.10% |
| **Pooled (per trade)** | 402 | 46% | -0.01% | -0.03% | n/a | -3.44% | n/a | -0.39 | -0.04% |
| **Pooled portfolio (equal split by day)** | 99 | 48% | -0.02% | -0.01% | -2.11% | -2.02% | -10.08% | -0.33 | -0.06% |

### 5x (control) down at 10:00, 3:30->4:00

| Symbol | n | Win% | Mean | Median | Total (compounded) | Worst | Max DD | t-stat | Mean ex top-3 |
|---|---|---|---|---|---|---|---|---|---|
| NVDA | 51 | 27% | -0.26% | -0.16% | -12.64% | -1.82% | -14.05% | -3.52 | -0.33% |
| AMD | 58 | 29% | -0.23% | -0.15% | -12.50% | -2.74% | -13.33% | -2.06 | -0.32% |
| MU | 54 | 52% | -0.19% | +0.03% | -10.17% | -3.75% | -15.87% | -1.02 | -0.36% |
| INTC | 61 | 49% | -0.12% | -0.01% | -7.41% | -3.81% | -12.77% | -0.88 | -0.23% |
| SMH | 51 | 35% | -0.23% | -0.12% | -11.26% | -2.81% | -12.44% | -2.48 | -0.31% |
| QQQ | 55 | 22% | -0.17% | -0.11% | -8.71% | -1.48% | -8.90% | -4.59 | -0.19% |
| **Pooled (per trade)** | 330 | 36% | -0.20% | -0.11% | n/a | -3.81% | n/a | -4.11 | -0.22% |
| **Pooled portfolio (equal split by day)** | 94 | 39% | -0.14% | -0.08% | -12.66% | -2.73% | -13.93% | -1.86 | -0.20% |
## What the data says (plain English)

1. **This was a strong bull market for semis.** An equal-weight hold of the six names made about +73% (AMD +153%, MU +134%, INTC +78%), with a 25% max drawdown. Almost all of the gain came **overnight**: summing close-to-open gaps gives +38% to +117% for every semi name, while the open-to-close part was flat or negative for NVDA, INTC and SMH. So **an overnight long during this window is mostly market exposure (beta) plus the well-documented overnight drift, not a timing skill.** QQQ, the lowest-beta name, shows almost no overnight edge (mean about 0.00% after costs).
2. **Overnight holds (rules 1 and 2): positive, but weak once correlation is counted.** Selling at the 9:30 open (2a) beats selling at 10:00 (1a) on every name: portfolio mean +0.33% vs +0.28% per night, max drawdown -12% vs -18%. The first 30 minutes of the session gave back part of the overnight gain. At the portfolio level 2a has t = 1.5 and makes +43% vs +73% for buy and hold, with half the drawdown. The edge survives removing the top 3 nights (+0.17%), but it was much stronger in Apr-Jul (+0.47%) than in Aug-Oct (+0.13%). None of the filters (red day, late dump, skip ramp) reliably improved it. "Red on day" (2b) looks good per trade but loses at the portfolio level (+0.16%, ex-top-3 -0.05%). "Late dump" (1c/2c) flips negative in the second half. **Treat the filters as noise.**
3. **Gap-up momentum (rule 3) is the strongest result.** It had 80 trades on 36 different days. Win rate was 64%, mean +1.13% per trade after costs, and the portfolio t-stat was 3.1. It made +43% at the portfolio level with only a -3.9% drawdown and a -3.0% worst trade. It survives removing the top 3 (+0.87%) and held up in both halves (+1.08% and +1.22%). It works on AMD, MU, INTC and SMH, but NVDA (n=5) and QQQ (n=1) rarely trigger. **The -1.5% stop hurt it:** mean fell to +0.66% and the win rate to 50%, because ordinary 10-minute noise stopped out trades that later recovered. Don't use the stop. Caveats: it fires on only about 30% of days, and many triggers are earnings or sector-news days when 9:45 spreads are wider than 0.05%.
4. **Flush buy (rule 4) failed.** There were 58 trades. Per-trade mean was +0.20%, but the portfolio mean was -0.41%, and with the top 3 removed it is -0.28% per trade and -1.13% for the portfolio. NVDA lost on 5 of 6 trades, and single losses reached -12% on AMD and MU. Its 59% win rate comes from capped +3% wins against uncapped losses. **Edge disappears ex top-3. Do not trade.**
5. **Rule 5 (stated before testing):** "Intraday momentum. If a name is up from prior close at 10:00, buy at 3:30 and sell at the 4:00 close" (Gao et al. first-half-hour/last-half-hour effect). **Result: no edge.** Mean was -0.01% after costs (t -0.4). The control group (down at 10:00) was clearly negative (-0.20%, t -4.1 pooled). That suggests the last half hour carries little or no drift on its own, and 0.10% in costs is too much for a 30-minute hold. Do not trade.

### Flags: rules whose edge goes away without the top 3 trades
- Per symbol, nearly every overnight variant on NVDA, SMH and QQQ goes to about 0 or below after removing the top 3.
- Portfolio level: 1b, 2b, 2c and 4 all turn ≤ 0. 1c (+0.03%) and 1d (+0.04%) are close to zero.
- Survive: 1a (+0.12%), 2a (+0.17%), 2d (+0.10%), 3 (+0.66%), 3s (+0.14%).

### Limits of this study
About 6 months of data in one strongly rising semis market. There is no bear-market test, and per-symbol samples are small (n = 5 to 21 for rules 3 and 4). Fills use 10-minute bar proxies, not actual quotes. The 0.05% slippage is a guess and may be low on volatile gap days. More than 80 rows were tested, so some apparent edges are luck. Anything here should be treated as a hypothesis for small-size live testing, not a proven edge.

## Recommended rule set (max 3, ranked)
1. **Gap-up momentum, no stop (rule 3).** If AMD, MU, INTC or SMH (NVDA/QQQ optional) opens ≥ +2% vs prior close and the 9:45 price is above the open, buy at about 9:45 and sell at about 3:45 the same day. On days with several triggers, split capital equally. Backtest: +1.02%/signal-day, 67% win days, t = 3.1, max drawdown -3.9%. Same-day round trips trigger PDT limits only in margin accounts. In a cash account the only constraint is settled funds (T+1), which allows about one full-size round trip per day.
2. **Overnight hold, sell at the open (rule 2a), semis only (AMD, MU, INTC, SMH; skip QQQ).** Buy at about 3:45 every day and sell at the 9:30 open (or the first minutes after it). Portfolio mean +0.33%/night, t = 1.5, max drawdown -12%. **This is mostly leveraged beta and only works while the semis uptrend lasts.** Consider sizing it smaller, or turning it off when SMH closes below its 20-day average (an untested idea). Rule 1 and rule 2 positions can both be held on the same day: sell the overnight position at 9:30, then take a gap trade at 9:45. Under cash-account rules, sale proceeds settle T+1, so plan capital accordingly.
3. **No third rule.** None of the other candidates (filters, stop, flush, intraday momentum) passed the ex-top-3 and portfolio-level checks. Holding cash on days with no gap trigger is the honest default, and for long-term exposure simply buying and holding SMH has been competitive.
