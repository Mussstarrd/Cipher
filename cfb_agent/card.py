"""Weekly card: a committed markdown report of the plays and the reasoning."""

from datetime import datetime, timezone

from . import config
from .edges import Play


def render_card(season: int, week: int, plays: list[Play], priced: list[dict],
                refresh_status: dict | None = None, demo: bool = False) -> str:
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    lines: list[str] = []
    a = lines.append

    a(f"# Cipher CFB — Week {week} Card ({season})")
    a("")
    if demo:
        a("> **DEMO DATA** — this card was generated from synthetic fixture data to")
        a("> exercise the pipeline. These are not real games or real lines. Do not bet it.")
        a("")
    a(f"Generated {now}. {len(priced)} games priced, {len(plays)} plays.")
    a("")

    if refresh_status:
        a("## Data sources")
        a("")
        for src, st in sorted(refresh_status.items()):
            icon = "✅" if st.startswith("ok") else "⚠️"
            a(f"- {icon} `{src}` — {st}")
        a("")

    if not plays:
        a("## No plays this week")
        a("")
        a(f"Nothing cleared the {config.MIN_EDGE_POINTS}-point edge threshold. Passing is a position.")
    else:
        a("## The card")
        a("")
        a("| Tier | Units | Play | Book | Our # | Market # | Edge |")
        a("|------|-------|------|------|-------|----------|------|")
        for p in plays:
            our_home = -p.predicted_margin
            a(
                f"| {p.tier} | {p.units:g}u | **{p.pick_team} {_fmt(p.pick_spread)}** "
                f"vs {p.away_team if p.pick_team == p.home_team else p.home_team} "
                f"| {p.best_book} ({p.price}) | {_fmt(our_home)} (home) | {_fmt(p.market_spread_home)} (home) "
                f"| {p.edge:.1f} |"
            )
        a("")
        a("## Reasoning")
        a("")
        for p in plays:
            side = "home" if p.pick_team == p.home_team else "away"
            a(f"### {p.pick_team} {_fmt(p.pick_spread)} ({p.units:g}u, Tier {p.tier})")
            a("")
            a(f"- **Matchup:** {p.away_team} @ {p.home_team}"
              + (" (neutral site)" if p.neutral else "") + (f", kickoff {p.kickoff}" if p.kickoff else ""))
            fav = p.home_team if p.predicted_margin >= 0 else p.away_team
            a(f"- **Model:** {fav} by {abs(p.predicted_margin):.1f} "
              f"→ fair home spread {_fmt(-p.predicted_margin)}.")
            a(f"- **Market:** best number for the {side} side is {_fmt(p.pick_spread)} at {p.best_book}; "
              f"that is **{p.edge:.1f} points** better than our fair number.")
            if len(p.all_books) > 1:
                shop = ", ".join(f"{b}: {_fmt(s)}" for b, s in sorted(p.all_books.items()))
                a(f"- **Line shop (home spread by book):** {shop}")
            a("")

    a("## Bankroll notes")
    a("")
    a("- 1 unit = 1% of bankroll. Tier A = 3u, B = 2u, C = 1u. Max exposure this card: "
      f"**{sum(p.units for p in plays):g}u**.")
    a("- Break-even at -110 is 52.38%. Judge the model by closing line value, not by any single week.")
    a("")
    return "\n".join(lines)


def write_card(season: int, week: int, content: str) -> str:
    config.ensure_dirs()
    path = config.REPORTS_DIR / f"{season}-week{week:02d}.md"
    path.write_text(content)
    return str(path)


def _fmt(spread: float) -> str:
    """Format a spread with its sign: -3.5, +7, PK."""
    if abs(spread) < 0.25:
        return "PK"
    return f"{round(spread * 2) / 2:+g}"
