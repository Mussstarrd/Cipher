"""Generation 0: off-the-shelf codecs at representative levels.

These are the bar every later generation must beat. No preprocessing,
no content awareness — just the raw codec.
"""

import bz2
import gzip
import lzma

import brotli
import zstandard

from . import FuncStrategy, register

register(FuncStrategy("gzip-9", lambda d: gzip.compress(d, 9), gzip.decompress))
register(FuncStrategy("bz2-9", lambda d: bz2.compress(d, 9), bz2.decompress))
register(FuncStrategy("lzma-6", lambda d: lzma.compress(d, preset=6), lzma.decompress))
register(
    FuncStrategy(
        "lzma-9e",
        lambda d: lzma.compress(d, preset=9 | lzma.PRESET_EXTREME),
        lzma.decompress,
    )
)
register(FuncStrategy("brotli-11", lambda d: brotli.compress(d, quality=11), brotli.decompress))

for level in (3, 19, 22):
    register(
        FuncStrategy(
            f"zstd-{level}",
            (lambda lv: lambda d: zstandard.ZstdCompressor(level=lv).compress(d))(level),
            lambda b: zstandard.ZstdDecompressor().decompress(b),
        )
    )
