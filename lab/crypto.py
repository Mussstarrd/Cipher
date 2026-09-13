"""Encryption stage for the pipeline.

Key fact the lab enforces: encrypt AFTER compressing, never before. Good
ciphertext is indistinguishable from random bytes, so nothing compresses
after encryption — the ordering is not a preference, it's information theory.
(Also note compress-then-encrypt leaks plaintext-dependent length; protocols
that mix attacker-controlled and secret data in one compressed stream must
mitigate that, e.g. CRIME/BREACH. For file storage with a single trust
domain, compress-then-encrypt is the standard design.)

seal() output layout: 12-byte nonce || AES-256-GCM ciphertext+tag.
"""

from __future__ import annotations

import os

from cryptography.hazmat.primitives.ciphers.aead import AESGCM


def seal(compressed: bytes, key: bytes) -> bytes:
    if len(key) != 32:
        raise ValueError("key must be 32 bytes (AES-256)")
    nonce = os.urandom(12)
    return nonce + AESGCM(key).encrypt(nonce, compressed, None)


def unseal(blob: bytes, key: bytes) -> bytes:
    return AESGCM(key).decrypt(blob[:12], blob[12:], None)
