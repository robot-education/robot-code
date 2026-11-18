"""FRCDesignLib's parts, for the tables of features declaring them for FRCDesignApp's Auto Assembly.

A definition keeps a snapshot of the library parts it declares (`vendor/frcdesign-*.json`, written by `fetch` from
FRCDesign's public API, never by `fs gen`), and turns each into a `Product` (`core/autoAssembly.fs`) with `product`:
its Part Studio by document and tab, with no version, and its configuration by parameter and option ids. Options are
named as the library names them, so a table reads as the library does.
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
            }
        )
    snapshot = {"source": f"{LIBRARY_API}/configuration/insertable/{{id}}?v={version} ({LIBRARY})", "parts": parts}
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(snapshot, indent=1) + "\n", newline="")
    print(f"Wrote {len(parts)} parts to {path.as_posix()}.")


def load(path: pathlib.Path) -> dict[str, dict]:
    """A snapshot's parts, by name."""
    return {part["name"]: part for part in json.loads(path.read_text())["parts"]}


def _fs_string(value: str) -> str:
    return json.dumps(value)


def product(parts: dict[str, dict], name: str, options: dict[str, str] | None = None, *, label: str | None = None) -> str:
    """A `Product`'s FeatureScript value: the library part `name`, with each of `options` (a parameter's name to an
    option's name, both as the library names them) set, and the rest at their defaults.

    Args:
        label: Its name for stand-ins and messages; the part's own name by default.
    """
    if name not in parts:
        raise LibraryError(f"The snapshot has no {name}; add it to the definition's fetch, and fetch again.")
    part = parts[name]
    configuration = {}
    for parameter_name, option_name in (options or {}).items():
        parameter = next((p for p in part["parameters"] if p["name"] == parameter_name), None)
        if parameter is None:
            raise LibraryError(f"{name} has no parameter {parameter_name}.")
        option = next((o for o in parameter["options"] if o["name"] == option_name), None)
        if option is None:
            offered = ", ".join(o["name"] for o in parameter["options"])
            raise LibraryError(f"{name}'s {parameter_name} has no option {option_name} (it offers {offered}).")
        configuration[parameter["id"]] = option["id"]
    entries = ", ".join(f"{_fs_string(key)} : {_fs_string(value)}" for key, value in configuration.items())
    return (
        f'{{ "documentId" : {_fs_string(part["documentId"])}, "elementId" : {_fs_string(part["elementId"])}, '
        f'"configuration" : {{{" " + entries + " " if entries else ""}}}, "name" : {_fs_string(label or name)} }}'
    )
