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

STATE_VERSION = 1


def content_hash(code: str) -> str:
    """Hashes Feature Studio contents, ignoring differences in line endings."""
    return hashlib.sha256(code.replace("\r\n", "\n").encode()).hexdigest()


@dataclasses.dataclass
class StudioState:
    """What a Feature Studio looked like the last time it was pushed or pulled.

    Attributes:
        file: The name of the local file the studio is mirrored to.
        hash: The content hash of the studio when it was last synced.
        microversion_id: The element microversion in Onshape when it was last synced.
    """

    file: str
    hash: str
    microversion_id: str


class State:
    def __init__(
        self, path: pathlib.Path, documents: dict[str, dict[str, StudioState]]
    ) -> None:
        self.path = path
        # document name -> element id -> state
        self.documents = documents

    @classmethod
    def load(cls, path: pathlib.Path) -> State:
        if not path.is_file():
            return cls(path, {})
        try:
            data = json.loads(path.read_text())
        except json.JSONDecodeError:
            print(f"Warning: ignoring unreadable state file {path}.")
            return cls(path, {})
        documents = {
            document: {
                element_id: StudioState(**studio)
                for element_id, studio in studios.items()
            }
            for document, studios in data.get("documents", {}).items()
        }
        return cls(path, documents)

    def save(self) -> None:
        data = {
            "version": STATE_VERSION,
            "documents": {
                document: {
                    element_id: dataclasses.asdict(studio)
                    for element_id, studio in sorted(studios.items())
                }
                for document, studios in sorted(self.documents.items())
            },
        }
        self.path.write_text(json.dumps(data, indent=2) + "\n")

    def studios(self, document: str) -> dict[str, StudioState]:
        return self.documents.setdefault(document, {})
