"""ReleaseLine: a local, step-by-step helper that walks a track from download to posted.

Standard library only. See README.md in the app folder.
"""

__version__ = "1.0.0"


class UserError(Exception):
    """A problem the user can fix. The message is a full sentence shown as-is."""

    status = 400


class NotFound(UserError):
    """Something the user asked for does not exist."""

    status = 404
