"""Tiny cached HTTP client (stdlib only, so the repo has zero hard deps).

Every GET is cached to disk keyed by URL+params so re-running the card
doesn't hammer free APIs, and so a week's inputs are reproducible.
"""

import hashlib
import json
import time
import urllib.error
import urllib.parse
import urllib.request
from typing import Any, Optional

from . import config


class FetchError(RuntimeError):
    """A source could not be reached. Callers decide whether it's fatal."""


def _cache_path(url: str, params: Optional[dict]) -> "Any":
    key = url + "?" + urllib.parse.urlencode(sorted((params or {}).items()))
    digest = hashlib.sha256(key.encode()).hexdigest()[:24]
    return config.CACHE_DIR / f"{digest}.json"


def get_json(
    url: str,
    params: Optional[dict] = None,
    headers: Optional[dict] = None,
    ttl: int = config.CACHE_TTL,
) -> Any:
    """GET a JSON endpoint with disk caching. Raises FetchError on failure."""
    config.ensure_dirs()
    path = _cache_path(url, params)
    if path.exists() and (time.time() - path.stat().st_mtime) < ttl:
        with open(path) as f:
            return json.load(f)

    full = url + ("?" + urllib.parse.urlencode(params) if params else "")
    req = urllib.request.Request(full, headers={"User-Agent": "cipher-cfb/0.1", **(headers or {})})
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            data = json.loads(resp.read().decode())
    except (urllib.error.URLError, urllib.error.HTTPError, TimeoutError, OSError) as e:
        # Stale cache beats no data.
        if path.exists():
            with open(path) as f:
                return json.load(f)
        raise FetchError(f"GET {url} failed: {e}") from e

    with open(path, "w") as f:
        json.dump(data, f)
    return data
