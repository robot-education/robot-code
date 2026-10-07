"""Researching COTS parts with FRCDesign's usage data (see docs/cots-research.md).

FRCDesign (https://app.frcdesign.org) is an Onshape app whose libraries hold most of the COTS parts FRC and FTC teams
use. The endpoints used here need no sign-in (and aren't Onshape's, so they don't count against Onshape's API limits);
docs/cots-research.md lists the others:

- `/api/library-version/library/{library}`: the library's current version, which the data below is cached by.
- `/api/library-data/library/{library}?v={version}`: its groups and parts ("insertables").
- `/api/analytics/parts/library/{library}?from=...&to=...`: how often each part was inserted.
- `/api/analytics/insertable/library/{library}/element/{element id}?from=...&to=...`: how often each of a part's
  configuration options was chosen.
- `/api/configuration/insertable/{insertable id}?v={version}`: a part's configuration parameters, and its
  configurations' part numbers, names, and vendor links (each for a `configurationKey` of parameter and option ids).
"""

from __future__ import annotations

import dataclasses
import datetime
import re
from typing import Any

import requests

API = "https://app.frcdesign.org/api"
LIBRARIES = {"frc": "frc-design-lib", "ftc": "ftc-design-lib", "mkcad": "mkcad"}


class CotsError(Exception):
    pass


@dataclasses.dataclass
class Part:
    name: str
    group: str
    vendors: list[str]
    uses: int
    insertable_id: str
    element_id: str


class FrcDesign:
    def __init__(self, library: str, days: int = 365, session: Any = None) -> None:
        self.library = LIBRARIES.get(library, library)
        self.session = session or requests.Session()
        today = datetime.date.today()
        self.period = {"from": (today - datetime.timedelta(days=days)).isoformat(), "to": today.isoformat()}
        self._version: int | None = None

    def get(self, path: str, **query: Any) -> Any:
        response = self.session.get(API + path, params=query, timeout=60)
        if response.status_code != 200:
            raise CotsError(f"FRCDesign returned {response.status_code} for {path}: {response.text[:200]}")
        return response.json()

    @property
    def version(self) -> int:
        if self._version is None:
            self._version = self.get(f"/library-version/library/{self.library}")["version"]
        return self._version

    def parts(self) -> list[Part]:
        """Every part in the library, most used first."""
        data = self.get(f"/library-data/library/{self.library}", v=self.version)
        uses = {
            entry["path"]["elementId"]: entry["insertCount"]
            for entry in self.get(f"/analytics/parts/library/{self.library}", **self.period)
        }
        groups = {group_id: group["name"] for group_id, group in data["groups"].items()}
        parts = [
            Part(
                insertable["name"],
                groups.get(insertable["groupId"], ""),
                insertable.get("vendors") or [],
                uses.get(insertable["elementId"], 0),
                insertable["id"],
                insertable["elementId"],
            )
            for insertable in data["insertables"].values()
        ]
        return sorted(parts, key=lambda part: (-part.uses, part.name))

    def options(self, part: Part) -> list[str]:
        """How often each of a part's configuration options was chosen, as lines of text."""
        usage = self.get(f"/analytics/insertable/library/{self.library}/element/{part.element_id}", **self.period)
        lines = []
        for parameter in usage.get("parameters", []):
            if parameter["type"] != "enum":
                lines.append(f"{parameter['name']} ({parameter['type']})")
                continue
            condition = f" when {' / '.join(parameter['path'])}" if parameter["path"] else ""
            values = sorted(parameter["values"], key=lambda value: -value["count"])
            counts = ", ".join(f"{value['label']} = {value['count']}" for value in values)
            lines.append(f"{parameter['name']}{condition} ({parameter['total']}): {counts}")
        return lines

    def records(self, part: Part) -> list[dict]:
        """A part's configurations with part numbers: maps of `partNumber`, `name`, and `url`, and `options`, the options
        its `configurationKey` chooses (e.g. `Tube Type = 2" x 1" x 0.125"`; options left at their defaults aren't
        in it)."""
        configuration = self.get(f"/configuration/insertable/{part.insertable_id}", v=self.version)
        parameters = {parameter["id"]: parameter for parameter in configuration.get("parameters", [])}
        records = configuration.get("records", [])
        for record in records:
            options = []
            for pair in filter(None, (record.get("configurationKey") or "").split(";")):
                parameter_id, _, value = pair.partition("=")
                parameter = parameters.get(parameter_id, {"name": parameter_id})
                names = {option["id"]: option["name"] for option in parameter.get("options") or []}
                options.append(f"{parameter['name']} = {names.get(value, value)}")
            record["options"] = ", ".join(options)
        return records


def find(parts: list[Part], query: str) -> list[Part]:
    """The parts whose names match query, a regular expression (ignoring case)."""
    pattern = re.compile(query, re.IGNORECASE)
    return [part for part in parts if pattern.search(part.name) or pattern.search(part.group)]
