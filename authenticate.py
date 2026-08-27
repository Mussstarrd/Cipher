#!/usr/bin/env python3
"""
One-time interactive Blink login.

Run this ONCE, on the machine that will host Cipher (your home network or any
box you control). It logs into your Blink account, handles the 2FA PIN that
Amazon emails/texts you, and saves refreshable session tokens to
`blink_session.json`. After that, the app reloads that file and refreshes
tokens on its own — you should not need to enter a PIN again unless the
session is revoked.

    python authenticate.py

The saved file contains account tokens. It is gitignored. Treat it like a
password: keep it on the host, don't share it, don't commit it.
"""

import asyncio
import getpass
import os
import sys

from aiohttp import ClientSession

from blinkpy.blinkpy import Blink
from blinkpy.auth import Auth, BlinkTwoFARequiredError, LoginError

SESSION_FILE = os.environ.get("BLINK_SESSION_FILE", "blink_session.json")


async def main() -> int:
    print("Cipher — Blink authentication\n" + "=" * 32)
    username = input("Blink account email: ").strip()
    password = getpass.getpass("Blink account password: ")

    async with ClientSession() as session:
        blink = Blink(session=session)
        blink.auth = Auth(
            {"username": username, "password": password},
            no_prompt=True,  # we drive the 2FA prompt ourselves, below
            session=session,
        )

        try:
            await blink.start()
        except BlinkTwoFARequiredError:
            print(
                "\nBlink sent a 2FA code to your email/phone."
                "\n(Check spam if you don't see it within a minute.)"
            )
            code = input("Enter the 2FA code: ").strip()
            ok = await blink.send_2fa_code(code)
            if not ok:
                print("2FA verification failed. Double-check the code and retry.")
                return 1
        except LoginError as exc:
            print(f"Login failed: {exc}")
            print("Check the email/password and try again.")
            return 1

        if not blink.available:
            print("Could not complete Blink setup. Please re-run and try again.")
            return 1

        await blink.save(SESSION_FILE)

        cams = list(blink.cameras.keys())
        print(f"\n Session saved to {SESSION_FILE}")
        print(f" Found {len(cams)} camera(s): {', '.join(cams) if cams else '(none)'}")
        print("\nNext: start the app with  ./run.sh   (or see README.md)")
        return 0


if __name__ == "__main__":
    try:
        sys.exit(asyncio.run(main()))
    except KeyboardInterrupt:
        print("\nCancelled.")
        sys.exit(130)
