"""Strategy registry with auto-discovery.

A strategy is any object with:
    name: str                        unique id, e.g. "zstd-19" or "gen1.delta-audio"
    compress(data: bytes) -> bytes   must produce a self-contained blob
    decompress(blob: bytes) -> bytes must exactly invert compress

Strategies must be lossless: the harness rejects any strategy whose roundtrip
is not byte-identical. Modules in this package register strategies by calling
`register(...)` at import time; `load_all()` imports every module here.
"""

from __future__ import annotations

import importlib
import pkgutil

_REGISTRY: dict[str, object] = {}


def register(strategy) -> None:
    if strategy.name in _REGISTRY:
        raise ValueError(f"duplicate strategy name: {strategy.name}")
    _REGISTRY[strategy.name] = strategy


def load_all() -> dict[str, object]:
    pkg = __name__
    for mod in pkgutil.iter_modules(__path__):
        if not mod.name.startswith("_"):
            importlib.import_module(f"{pkg}.{mod.name}")
    return dict(_REGISTRY)


class FuncStrategy:
    """Convenience wrapper to register a pair of functions as a strategy."""

    def __init__(self, name, compress, decompress):
        self.name = name
        self._c = compress
        self._d = decompress

    def compress(self, data: bytes) -> bytes:
        return self._c(data)

    def decompress(self, blob: bytes) -> bytes:
        return self._d(blob)
