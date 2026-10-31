"""Title, description, hashtag and tag drafts from the track's own fields.

Everything here is a template filled from what the owner typed. It describes the track;
it never claims how the release will perform.
"""

from __future__ import annotations

import re

from .prompts import genre_family

BANNED = ["viral", "guaranteed", "grow your", "blow up", "go viral", "promise"]
TYPE_BEAT_FAMILIES = {"hiphop", "drill", "rnb", "lofi"}
YOUTUBE_TAG_LIMIT = 500
TITLE_LIMIT = 100


def _tag(text: str) -> str:
    return re.sub(r"[^a-z0-9]", "", (text or "").lower())


def _words(text: str) -> list[str]:
    return [w for w in re.split(r"[,/;&+|]+|\s+and\s+", text or "") if w.strip()]


def _title_case(text: str) -> str:
    return " ".join(w[:1].upper() + w[1:] for w in (text or "").split())


def _trim(text: str, limit: int) -> str:
    text = text.strip()
    return text if len(text) <= limit else text[: limit - 1].rstrip() + "…"


def _dedupe(items) -> list:
    return list(dict.fromkeys(i for i in items if i))


def uses_type_beat(genre: str) -> bool:
    return genre_family(genre) in TYPE_BEAT_FAMILIES


def generate(track: dict, settings: dict | None = None, explicit: bool = False) -> dict:
    settings = settings or {}
    title = (track.get("title") or "Untitled").strip()
    artist = (track.get("artist") or settings.get("artist") or "").strip()
    genre = (track.get("genre") or "").strip()
    mood = (track.get("mood") or "").strip()
    energy = (track.get("energy") or "").strip()
    bpm = (track.get("bpm") or "").strip()
    key = (track.get("key") or "").strip()
    hook = (track.get("hook") or "").strip().splitlines()
    hook = hook[0].strip() if hook else ""
    genre_words = _words(genre)
    mood_words = _words(mood)
    genre_main = _title_case(genre_words[0]) if genre_words else ""
    mood_main = _title_case(mood_words[0]) if mood_words else ""
    year = (track.get("created_at") or "")[:4]
    type_beat = uses_type_beat(genre)

    # ---- titles ----
    plain = f"{title} - {artist}" if artist else title
    if type_beat:
        label = " ".join(x for x in (mood_main, genre_main) if x) or "Beat"
        second = f'{title} | {label} Type Beat' + (f" {year}" if year else "")
    else:
        desc = " ".join(x for x in (mood_main, genre_main) if x)
        second = f"{title} ({desc})" if desc else f"{title} (Official Audio)"
    if hook:
        third = f'"{_trim(hook, 60)}" - {title}'
    else:
        tech = ", ".join(x for x in (f"{bpm} BPM" if bpm else "", key) if x)
        third = f"{title} [{tech}]" if tech else (f"{title} | new from {artist}" if artist else f"{title} | {genre_main or 'new track'}")
    titles = [_trim(t, TITLE_LIMIT) for t in _dedupe([plain, second, third])]

    # ---- descriptions ----
    facts = []
    if genre:
        facts.append(genre)
    if mood:
        facts.append(mood)
    if energy:
        facts.append(f"{energy} energy")
    if bpm:
        facts.append(f"{bpm} BPM")
    if key:
        facts.append(key)
    fact_line = " · ".join(facts)
    by = f" by {artist}" if artist else ""
    short_parts = [f"{title}{by}."]
    if fact_line:
        short_parts.append(fact_line + ".")
    if hook:
        short_parts.append(f'"{hook}"')
    description_short = _trim(" ".join(short_parts), 280)

    long_lines = [f"{title}{by}"]
    if fact_line:
        long_lines.append(fact_line)
    long_lines.append("")
    if hook:
        long_lines += [f'"{hook}"', ""]
    if type_beat:
        long_lines.append(f"{genre_main or 'Beat'} instrumental{(' by ' + artist) if artist else ''}. "
                          "Add your licensing terms or contact here if the beat is for sale or free use.")
    else:
        long_lines.append(f"Written and produced{(' by ' + artist) if artist else ''}.")
    long_lines += ["", "Listen on: [add your streaming or distributor links here]"]
    if track.get("suno_url"):
        long_lines.append(f"Original: {track['suno_url']}")
    long_lines += ["", f"Explicit: {'yes' if explicit else 'no'}"]
    description_long = "\n".join(long_lines).strip()

    # ---- hashtags ----
    g_tags = [_tag(w) for w in genre_words]
    g_joined = _tag(genre) if len(genre_words) > 1 else ""
    m_tags = [_tag(w) for w in mood_words[:2]]
    a_tag = _tag(artist)
    core = _dedupe(g_tags + [g_joined] + (["typebeat"] if type_beat else []))
    youtube = _dedupe(core[:2] + (["typebeat"] if type_beat else ["newmusic"]) + [a_tag])[:4]
    tiktok = _dedupe(core + m_tags + ["newmusic", "musicproducer", "producer", "beats"] + [a_tag])[:8]
    instagram = _dedupe(core + m_tags + [
        "newmusic", "musicproducer", "producer", "beats", "beatmaker", "independentartist",
        "originalmusic", "musicrelease", "homestudio", "nowplaying",
        (genre_main.lower().replace(" ", "") + "music") if genre_main else "",
    ] + [a_tag])[:15]
    hashtags = {
        "youtube": ["#" + t for t in youtube if t],
        "tiktok": ["#" + t for t in tiktok if t],
        "instagram": ["#" + t for t in instagram if t],
    }

    # ---- YouTube tags (comma separated, capped) ----
    candidates = [title] + ([artist] if artist else []) + genre_words
    if genre_main:
        candidates += [f"{genre_main.lower()} beat", f"{genre_main.lower()} instrumental"]
        if type_beat:
            candidates += [f"{genre_main.lower()} type beat", "type beat",
                           f"{mood_main.lower()} {genre_main.lower()} type beat" if mood_main else ""]
        if mood_main:
            candidates.append(f"{mood_main.lower()} {genre_main.lower()}")
    if len(genre_words) > 1:
        candidates.append(f"{genre.lower()} beat")
    candidates += mood_words
    if bpm:
        candidates.append(f"{bpm} bpm")
    if key:
        candidates.append(key)
    candidates += ["new music", "instrumental", "beats", "music producer"]
    if year:
        candidates.append(f"new music {year}")
    tags = []
    for c in _dedupe(c.strip().lower() for c in candidates if c and c.strip()):
        c = c.replace(",", " ")
        if len(", ".join(tags + [c])) <= YOUTUBE_TAG_LIMIT:
            tags.append(c)
    youtube_tags = ", ".join(tags)

    # ---- distributor one-liner ----
    distributor = " | ".join(x for x in [
        f"Title: {title}", f"Artist: {artist}" if artist else "",
        f"Genre: {genre}" if genre else "Genre: (set genre)",
        f"Mood: {mood}" if mood else "", f"BPM: {bpm}" if bpm else "", f"Key: {key}" if key else "",
        f"Explicit: {'yes' if explicit else 'no'}",
    ] if x)

    return {
        "titles": titles,
        "description_short": description_short,
        "description_long": description_long,
        "hashtags": hashtags,
        "youtube_tags": youtube_tags,
        "youtube_tags_length": len(youtube_tags),
        "distributor_note": distributor,
        "type_beat": type_beat,
        "explicit": bool(explicit),
    }


def all_text(result: dict) -> str:
    """Every string in the result, joined, for checks and for the CLI."""
    parts = list(result["titles"]) + [result["description_short"], result["description_long"],
                                      result["youtube_tags"], result["distributor_note"]]
    for tags in result["hashtags"].values():
        parts.append(" ".join(tags))
    return "\n".join(parts)


def as_text(result: dict) -> str:
    lines = ["Titles:"] + [f"  {i}. {t}" for i, t in enumerate(result["titles"], 1)]
    lines += ["", "Short description:", result["description_short"], "", "Long description:",
              result["description_long"], ""]
    for platform, tags in result["hashtags"].items():
        lines.append(f"Hashtags ({platform}): {' '.join(tags)}")
    lines += ["", f"YouTube tags ({result['youtube_tags_length']}/{YOUTUBE_TAG_LIMIT} chars):",
              result["youtube_tags"], "", "Distributor note:", result["distributor_note"]]
    return "\n".join(lines)
