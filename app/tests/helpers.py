"""Shared test scaffolding: each test gets its own temp RELEASELINE_DATA dir."""

import os
import shutil
import sys
import tempfile
import unittest
from pathlib import Path

APP_DIR = Path(__file__).resolve().parent.parent
if str(APP_DIR) not in sys.path:
    sys.path.insert(0, str(APP_DIR))


class TempDataCase(unittest.TestCase):
    def setUp(self):
        # a path with a space, so Windows-style paths with spaces are exercised everywhere
        self.tmp = Path(tempfile.mkdtemp(prefix="releaseline test "))
        self.data = self.tmp / "data"
        self.data.mkdir()
        self._old_env = os.environ.get("RELEASELINE_DATA")
        os.environ["RELEASELINE_DATA"] = str(self.data)
        self.addCleanup(self._cleanup)

    def _cleanup(self):
        if self._old_env is None:
            os.environ.pop("RELEASELINE_DATA", None)
        else:
            os.environ["RELEASELINE_DATA"] = self._old_env
        shutil.rmtree(self.tmp, ignore_errors=True)

    def connect(self):
        from releaseline.db import connect
        conn = connect(self.data)
        self.addCleanup(conn.close)
        return conn

    def settings(self):
        from releaseline import settings
        return settings.load(self.data)
