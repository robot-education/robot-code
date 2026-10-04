"""Local (untracked) bookkeeping about the last time each Feature Studio was synced.

The state lets the CLI tell apart "I changed this file locally" from "someone changed this
Feature Studio in Onshape". It lives in .fs-state.json, which is not checked in; when it is
missing (e.g. on a fresh clone) the CLI falls back to comparing against git history.
"""

from __future__ import annotations

import dataclasses
import hashlib
import json
import pathlib

STATE_VERSION = 2


def content_hash(code: str) -> str:
    """Hashes Feature Studio contents, ignoring differences in line endings."""
    return hashlib.sha256(code.replace("\r\n", "\n").encode()).hexdigest()


@dataclasses.dataclass
class StudioState:
    """What a Feature Studio looked like the last time it was pushed or pulled.

    Attributes:
        file: The path of the local file, relative to the code folder (using /).
        hash: The content hash of the studio when it was last synced.
        microversion_id: The element microversion in Onshape when it was last synced.
    """

    file: str
    hash: str
    microversion_id: str


class State:
    def __init__(self, path: pathlib.Path, studios: dict[str, StudioState]) -> None:
        self.path = path
        # element id -> state
        self.studios = studios

    @classmethod
    def load(cls, path: pathlib.Path) -> State:
        if not path.is_file():
            return cls(path, {})
        try:
            data = json.loads(path.read_text())
            if data.get("version") != STATE_VERSION:
                return cls(path, {})
            studios = {
                element_id: StudioState(**studio)
                for element_id, studio in data["studios"].items()
            }
        except (json.JSONDecodeError, KeyError, TypeError):
            print(f"Warning: ignoring unreadable state file {path}.")
            return cls(path, {})
        return cls(path, studios)

    def save(self) -> None:
        data = {
            "version": STATE_VERSION,
            "studios": {
                element_id: dataclasses.asdict(studio)
                for element_id, studio in sorted(self.studios.items())
            },
        }
        self.path.write_text(json.dumps(data, indent=2) + "\n")
