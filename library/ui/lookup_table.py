from __future__ import annotations

import dataclasses
from typing import Self
from library.core import map


class LookupPayload(map.Map):
    def __init__(
        self, key: str, value: map.Map, next: LookupNode | None = None
    ) -> None:
        self.key = key
        self.value = value
        self.next = next


class LookupNode(map.Map):
    name = str

    def __init__(
        self,
        name: str,
        display_name: str | None = None,
        default: str | None = None,
        entries: list[LookupPayload] = dataclasses.field(default_factory=list),
    ) -> None:
        self.name = name
        self.display_name = display_name
        self.default = default
        self.entries = entries

    def set_next(self, next: Self):
        for entry in self.entries:
            entry.next = next
