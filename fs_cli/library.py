"""FRCDesignLib's parts, as a definition reads them: a snapshot of the library parts it needs
(`vendor/frcdesign-*.json`, written by `fetch` from FRCDesign's public API, never by `fs gen`), with each part's
configuration parameters and the configurations the library indexes (`records`).
"""

from __future__ import annotations

import json
import pathlib
import urllib.request

LIBRARY_API = "https://app.frcdesign.org/api"
LIBRARY = "frc-design-lib"


class LibraryError(Exception):
    pass


def _get_json(url: str):
    request = urllib.request.Request(url, headers={"User-Agent": "robot-code fs_cli.library"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)


def fetch(path: pathlib.Path, names: list[str]) -> None:
    """Writes the library parts named (exactly, as FRCDesign lists them) to a snapshot at `path`: each one's
    document, tab and configuration parameters. Fails on a name the library doesn't have."""
    version = _get_json(f"{LIBRARY_API}/library-version/library/{LIBRARY}")["version"]
    library = _get_json(f"{LIBRARY_API}/library-data/library/{LIBRARY}?v={version}")
    by_name = {insertable["name"]: (insertable_id, insertable) for insertable_id, insertable in library["insertables"].items()}
    missing = [name for name in names if name not in by_name]
    if missing:
        raise LibraryError(f"FRCDesignLib has no {', '.join(missing)}.")
    parts = []
    for name in names:
        insertable_id, insertable = by_name[name]
        configuration = _get_json(f"{LIBRARY_API}/configuration/insertable/{insertable_id}?v={version}")
        parts.append(
            {
                "name": name,
                "documentId": insertable["documentId"],
                "elementId": insertable["elementId"],
                "elementType": insertable["elementType"],
                "parameters": [
                    {
                        "id": parameter["id"],
                        "name": parameter["name"],
                        "type": parameter["type"],
                        "default": parameter.get("default"),
                        "options": [{"id": option["id"], "name": option["name"]} for option in parameter.get("options", [])],
                    }
                    for parameter in configuration.get("parameters") or []
                ],
                # Each configuration the library indexed, by the options it sets apart from the defaults
                "records": [
                    {key: record[key] for key in ("values", "name", "partNumber") if record.get(key) is not None}
                    for record in configuration.get("records") or []
                ],
            }
        )
    snapshot = {"source": f"{LIBRARY_API}/configuration/insertable/{{id}}?v={version} ({LIBRARY})", "parts": parts}
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(snapshot, indent=1) + "\n", newline="")
    print(f"Wrote {len(parts)} parts to {path.as_posix()}.")


def load(path: pathlib.Path) -> dict[str, dict]:
    """A snapshot's parts, by name."""
    return {part["name"]: part for part in json.loads(path.read_text())["parts"]}


def records(part: dict) -> list[dict]:
    """A part's indexed configurations, each with every parameter's option named (`options`) beside its name and part
    number."""
    found = []
    for record in part.get("records", []):
        options = {}
        for parameter in part["parameters"]:
            if parameter["type"] != "enum":
                continue
            option_id = record.get("values", {}).get(parameter["id"], parameter["default"])
            option = next((o for o in parameter["options"] if o["id"] == option_id), None)
            if option is not None:
                options[parameter["name"]] = option["name"]
        found.append({"name": record["name"], "partNumber": record.get("partNumber"), "options": options})
    return found
