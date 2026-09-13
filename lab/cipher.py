"""Cipher — the lab's end-user CLI: compress (and optionally encrypt) files
with the research-winning strategies.

Usage:
    python -m lab.cipher pack   <in> <out> [--strategy gen2.auto] [--encrypt]
    python -m lab.cipher unpack <in> <out>

pack   compresses <in> with the chosen strategy (default gen2.auto, the
       best-of-everything router) and writes a self-describing .cph file.
       With --encrypt, the passphrase is read from the CIPHER_KEY environment
       variable, a 32-byte key is derived with scrypt (random 16-byte salt,
       stored in the header), and the COMPRESSED payload is sealed with
       AES-256-GCM via lab.crypto — encrypt-after-compress, never the other
       way around (ciphertext is incompressible).
unpack auto-detects everything from the header (strategy, encrypted flag,
       salt); with an encrypted file it reads the passphrase from CIPHER_KEY.

File format (all integers little-endian):
    offset  size  field
    0       4     magic  b"CPH1"
    4       1     format version (1)
    5       1     flags  (bit 0: payload is AES-256-GCM sealed)
    6       1     strategy name length N
    7       N     strategy name (ascii)
    7+N     16    scrypt salt          (only present when flag bit 0 is set)
    ...           payload: strategy blob, or crypto.seal(strategy blob, key)

Cryptography: only the `cryptography` library's Scrypt KDF (n=2**14, r=8,
p=1, 32-byte key) and lab.crypto's AESGCM seal/unseal.  No home-grown
primitives (ORCHESTRATOR.md rule 4).
"""

from __future__ import annotations

import argparse
import os
import sys

from cryptography.hazmat.primitives.kdf.scrypt import Scrypt

from . import crypto
from .strategies import load_all

MAGIC = b"CPH1"
VERSION = 1
FLAG_ENCRYPTED = 0x01
SALT_LEN = 16

# scrypt parameters (interactive-grade; bump n for archival keys)
_SCRYPT_N = 2 ** 14
_SCRYPT_R = 8
_SCRYPT_P = 1


def _derive_key(passphrase: bytes, salt: bytes) -> bytes:
    kdf = Scrypt(salt=salt, length=32, n=_SCRYPT_N, r=_SCRYPT_R, p=_SCRYPT_P)
    return kdf.derive(passphrase)


def _passphrase() -> bytes:
    pw = os.environ.get("CIPHER_KEY")
    if not pw:
        raise SystemExit("error: CIPHER_KEY environment variable is not set (needed for encryption)")
    return pw.encode("utf-8")


def _build_header(strategy_name: str, encrypted: bool, salt: bytes) -> bytes:
    name = strategy_name.encode("ascii")
    if len(name) > 255:
        raise SystemExit("error: strategy name too long")
    flags = FLAG_ENCRYPTED if encrypted else 0
    header = MAGIC + bytes([VERSION, flags, len(name)]) + name
    if encrypted:
        header += salt
    return header


def _parse_header(blob: bytes) -> tuple[str, bool, bytes, bytes]:
    """-> (strategy_name, encrypted, salt, payload)"""
    if len(blob) < 7 or blob[:4] != MAGIC:
        raise SystemExit("error: not a Cipher file (bad magic)")
    version, flags, name_len = blob[4], blob[5], blob[6]
    if version != VERSION:
        raise SystemExit(f"error: unsupported format version {version}")
    pos = 7
    name = blob[pos:pos + name_len]
    if len(name) != name_len:
        raise SystemExit("error: truncated header")
    pos += name_len
    encrypted = bool(flags & FLAG_ENCRYPTED)
    salt = b""
    if encrypted:
        salt = blob[pos:pos + SALT_LEN]
        if len(salt) != SALT_LEN:
            raise SystemExit("error: truncated header (salt)")
        pos += SALT_LEN
    return name.decode("ascii"), encrypted, salt, blob[pos:]


def pack(in_path: str, out_path: str, strategy_name: str, encrypt: bool) -> None:
    strategies = load_all()
    if strategy_name not in strategies:
        raise SystemExit(
            f"error: unknown strategy {strategy_name!r}; known: {', '.join(sorted(strategies))}"
        )
    with open(in_path, "rb") as f:
        data = f.read()

    payload = strategies[strategy_name].compress(data)

    salt = b""
    if encrypt:
        passphrase = _passphrase()
        salt = os.urandom(SALT_LEN)
        key = _derive_key(passphrase, salt)
        payload = crypto.seal(payload, key)  # encrypt AFTER compress

    header = _build_header(strategy_name, encrypt, salt)
    with open(out_path, "wb") as f:
        f.write(header + payload)

    total = len(header) + len(payload)
    ratio = len(data) / total if total else float("inf")
    print(
        f"packed {in_path} -> {out_path}: {len(data)} -> {total} bytes "
        f"({ratio:.3f}x, strategy={strategy_name}, encrypted={'yes' if encrypt else 'no'})"
    )


def unpack(in_path: str, out_path: str) -> None:
    with open(in_path, "rb") as f:
        blob = f.read()
    strategy_name, encrypted, salt, payload = _parse_header(blob)

    if encrypted:
        key = _derive_key(_passphrase(), salt)
        try:
            payload = crypto.unseal(payload, key)
        except Exception:
            raise SystemExit("error: decryption failed (wrong CIPHER_KEY or corrupted file)")

    strategies = load_all()
    if strategy_name not in strategies:
        raise SystemExit(
            f"error: file needs strategy {strategy_name!r}, which is not registered"
        )
    data = strategies[strategy_name].decompress(payload)
    with open(out_path, "wb") as f:
        f.write(data)
    print(
        f"unpacked {in_path} -> {out_path}: {len(data)} bytes "
        f"(strategy={strategy_name}, encrypted={'yes' if encrypted else 'no'})"
    )


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(prog="python -m lab.cipher", description=__doc__.split("\n\n")[0])
    sub = ap.add_subparsers(dest="cmd", required=True)

    p = sub.add_parser("pack", help="compress (and optionally encrypt) a file")
    p.add_argument("input")
    p.add_argument("output")
    p.add_argument("--strategy", default="gen2.auto", help="strategy name (default: gen2.auto)")
    p.add_argument("--encrypt", action="store_true",
                   help="AES-256-GCM-seal the compressed payload; passphrase from $CIPHER_KEY")

    u = sub.add_parser("unpack", help="restore a packed file (auto-detects everything)")
    u.add_argument("input")
    u.add_argument("output")

    args = ap.parse_args(argv)
    if args.cmd == "pack":
        pack(args.input, args.output, args.strategy, args.encrypt)
    else:
        unpack(args.input, args.output)


if __name__ == "__main__":
    main()
