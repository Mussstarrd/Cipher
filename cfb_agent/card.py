"""Weekly card: a committed markdown report of the plays and the reasoning."""

from datetime import datetime, timedelta, timezone
from typing import Optional

from . import config
from .edges import Play

# US Eastern is UTC-4 during the football season (EDT through early November).
ET = timezone(timedelta(hours=-4))


def render_card(season: int, week: int, plays: list[Play], quarantined: list[Play],
                priced: list[dict], refresh_status: Optional[dict] = None,
                demo: bool = False, mode: str = "PAPER") -> str:
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
    min_edge = config.min_edge_for_week(week)
    lines: list[str] = []
    a = lines.append

    a(f"# LineHawk — Week {week} Card ({season}) — {mode}")
    a("")
    if demo:
        a("> **DEMO DATA** — generated from synthetic fixture data to exercise the")
        a("> pipeline. Not real games or real lines. Do not bet it.")
        a("")
    elif mode == "PAPER":
        a(f"> ## 🧪 TEST / PAPER CARD — ZERO DOLLARS")
        a("> ")
        a(f"> Weeks 1–{config.PAPER_ONLY_THROUGH_WEEK} are paper only, no exceptions. These")
        a("> plays are recorded to measure closing line value and pipeline latency.")
        a("> **No money is to be placed on anything below.**")
        a("")

    a(f"Generated {now}. {len(priced)} games priced, {len(plays)} plays, "
      f"{len(quarantined)} quarantined.")
    a("")
    a(f"Model: HFA **{config.HOME_FIELD_ADVANTAGE:g}** flat · minimum edge "
      f"**{min_edge:g} pts** (week {week}) · tiers A≥{config.TIER_A_EDGE:g} / "
      f"B≥{config.TIER_B_EDGE:g} / C≥{min_edge:g}.")
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
        a(f"Nothing cleared the {min_edge:g}-point edge threshold. Passing is a position.")
        a("")
    else:
        a("## The card")
        a("")
        a("| Tier | Units | Play | Kickoff (ET) | Book | Our # | Consensus | Edge |")
        a("|------|-------|------|--------------|------|-------|-----------|------|")
        for p in plays:
            opp = p.away_team if p.pick_team == p.home_team else p.home_team
            a(f"| {p.tier} | {p.units:g}u | **{p.pick_team} {_fmt(p.pick_spread)}** vs {opp} "
              f"| {_kick(p.kickoff)} | {p.best_book} ({p.price}) "
              f"| {_fmt(-p.predicted_margin)} (home) "
              f"| {_fmt(p.consensus_spread_home)} (home) | {p.edge:.1f} |")
        a("")
        today = [p for p in plays if _is_today(p.kickoff)]
        a(f"**{len(today)} of these {len(plays)} plays kick off today**; the rest are later in "
          f"the same scheduling week (the providers' \"week 1\" spans Aug 29 - Sep 7). Numbers "
          f"on the later games will move materially before kickoff — the line-freeze rule "
          f"applies to each play at its own kickoff, not to the card as a whole.")
        a("")
        a("## Reasoning")
        a("")
        for p in plays:
            a(_reasoning(p))

    if quarantined:
        a("## ⛔ Quarantined — presumed data defects, NOT plays")
        a("")
        a(f"These cleared the edge threshold but disagree with the market by "
          f"{config.ABSURD_EDGE_POINTS:g}+ points. A gap that size is far more often a "
          f"broken rating or a mis-joined game than a real edge, so they are held off "
          f"the card pending inspection.")
        a("")
        a("| Would-be play | Our # | Consensus | Gap | Why held |")
        a("|---------------|-------|-----------|-----|----------|")
        for p in quarantined:
            gap = abs(-p.predicted_margin - p.consensus_spread_home)
            a(f"| {p.pick_team} {_fmt(p.pick_spread)} ({p.away_team} @ {p.home_team}) "
              f"| {_fmt(-p.predicted_margin)} | {_fmt(p.consensus_spread_home)} "
              f"| {gap:.1f} | {p.quarantine} |")
        a("")

    a("## Bankroll notes")
    a("")
    a(f"- 1 unit = 1% of bankroll. Tier A = 3u, B = 2u, C = 1u. Max exposure this card: "
      f"**{sum(p.units for p in plays):g}u**"
      + (" — **on paper; nothing is staked.**" if mode == "PAPER" else "."))
    a("- Break-even at -110 is 52.38%. Judge the model by closing line value, not by any single week.")
    a("- Never chase: if the market moves 1.0+ points toward our number before placement, the play is cancelled.")
    a("")
    return "\n".join(lines)


def _reasoning(p: Play) -> str:
    side = "home" if p.pick_team == p.home_team else "away"
    fav = p.home_team if p.predicted_margin >= 0 else p.away_team
    out = [
        f"### {p.pick_team} {_fmt(p.pick_spread)} ({p.units:g}u, Tier {p.tier})",
        "",
        f"- **Matchup:** {p.away_team} @ {p.home_team}"
        + (" (neutral site)" if p.neutral else "")
        + (f", kickoff {p.kickoff}" if p.kickoff else ""),
        f"- **Model:** {fav} by {abs(p.predicted_margin):.1f} → fair home spread "
        f"{_fmt(-p.predicted_margin)}.",
        f"- **Market:** {p.n_books}-book consensus home spread {_fmt(p.consensus_spread_home)}; "
        f"best number for the {side} side is {_fmt(p.pick_spread)} at {p.best_book}, "
        f"**{p.edge:.1f} points** better than our fair number.",
    ]
    if len(p.all_books) > 1:
        shop = ", ".join(f"{b}: {_fmt(s)}" for b, s in sorted(p.all_books.items()))
        out.append(f"- **Line shop (home spread by book):** {shop}")
    out.append("")
    return "\n".join(out)


def write_card(season: int, week: int, content: str, mode: str = "PAPER") -> str:
    config.ensure_dirs()
    suffix = "-paper" if mode == "PAPER" else ""
    path = config.REPORTS_DIR / f"{season}-week{week:02d}{suffix}.md"
    path.write_text(content, encoding="utf-8")
    return str(path)


def _kick(kickoff: str) -> str:
    dt = _parse(kickoff)
    return dt.astimezone(ET).strftime("%a %m/%d %I:%M%p").replace(" 0", " ") if dt else "?"


def _is_today(kickoff: str) -> bool:
    dt = _parse(kickoff)
    return bool(dt) and dt.astimezone(ET).date() == datetime.now(ET).date()


def _parse(kickoff: str) -> Optional[datetime]:
    if not kickoff:
        return None
    try:
        dt = datetime.fromisoformat(kickoff.replace("Z", "+00:00"))
    except ValueError:
        return None
    return dt if dt.tzinfo else dt.replace(tzinfo=timezone.utc)


def _fmt(spread: float) -> str:
    """Format a spread with its sign: -3.5, +7, PK."""
    if abs(spread) < 0.25:
        return "PK"
    return f"{round(spread * 2) / 2:+g}"
