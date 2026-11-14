"""What a release keeps in the frontend document beside each feature's studio, for FRCDesignApp's FeatureScripts tab.

The app reads one file, `featurescripts.json` at the frontend document's top level (the manifest), for everything it
shows of a feature but two things it takes from the document's layout: its category (the folder its studio is in), and
its example Part Studio (see `find_example`, which is the app's rule too). A release writes the feature's entry and its
icon (`<Feature Type Name> icon.svg`), and creates an empty example if it has none; `fs deprecate` takes them back out.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re

MANIFEST_NAME = "featurescripts.json"
# Bumped when a field's meaning changes; the app refuses a manifest of a version it doesn't know
MANIFEST_SCHEMA_VERSION = 1

PART_STUDIO = "PARTSTUDIO"


@dataclasses.dataclass
class FrontendTab:
    """A tab in the frontend document.

    Attributes:
        folders: The folders holding it, outermost first.
        placed: Whether it's in the document's folder tree, so its place is known; Onshape leaves some tabs out.
    """

    element_id: str
    name: str
    element_type: str
    folders: tuple[str, ...] = ()
    placed: bool = True


def tabs_in_order(elements: list[dict], root: dict | None) -> list[FrontendTab]:
    """Every tab in tab-bar order with its folders, given the elements endpoint's tabs and the contents endpoint's folder
    tree (None when it couldn't be read); tabs the tree leaves out come last, unplaced."""
    by_id = {element["id"]: element for element in elements}
    ordered: list[FrontendTab] = []

    def walk(group: dict, folders: tuple[str, ...]) -> None:
        for node in group.get("groups") or []:
            if not isinstance(node, dict):
                continue
            element = by_id.pop(node["elementId"], None) if node.get("elementId") else None
            if element is not None:
                ordered.append(FrontendTab(element["id"], element["name"], element["elementType"], folders))
            elif "groups" in node or "groupName" in node:
                walk(node, folders + (node.get("groupName") or "",))

    if isinstance(root, dict):
        walk(root, ())
    unplaced = [
        FrontendTab(element["id"], element["name"], element["elementType"], placed=False) for element in by_id.values()
    ]
    return ordered + unplaced


def _is_deprecated(tab: FrontendTab) -> bool:
    return any("deprecated" in name.lower() for name in (tab.name, *tab.folders))


def find_example(tabs: list[FrontendTab], studio_element_id: str | None, feature_name: str) -> FrontendTab | None:
    """A feature's example: the Part Studio named for it (in any case), preferring one after its studio in the same
    folder; else the first Part Studio after its studio in its folder, which features sharing an example sit before.
    Deprecated tabs, and tabs in deprecated folders, are skipped. `tabs` are in tab-bar order."""
    tabs = [tab for tab in tabs if not _is_deprecated(tab)]
    studio_index = next(
        (index for index, tab in enumerate(tabs) if tab.element_id == studio_element_id and tab.placed), None
    )
    after = []
    if studio_index is not None:
        folders = tabs[studio_index].folders
        after = [
            tab
            for tab in tabs[studio_index + 1 :]
            if tab.placed and tab.folders == folders and tab.element_type == PART_STUDIO
        ]
    wanted = feature_name.strip().lower()

    def is_named(tab: FrontendTab) -> bool:
        return tab.element_type == PART_STUDIO and tab.name.strip().lower() == wanted

    return (
        next((tab for tab in after if is_named(tab)), None)
        or next((tab for tab in tabs if is_named(tab)), None)
        or (after[0] if after else None)
    )


def icon_tab_name(feature_name: str) -> str:
    return f"{feature_name} icon.svg"


_FEATURE_TYPE = re.compile(r"export\s+const\s+(\w+)\s*=\s*defineFeature\b")
_STRING = r'"((?:[^"\\]|\\.)*)"'
_TYPE_NAME = re.compile(r'"Feature Type Name"\s*:\s*' + _STRING)
_DESCRIPTION = re.compile(r'"Feature Type Description"\s*:\s*' + _STRING)
_ICON = re.compile(r'"Icon"\s*:\s*(\w+)::BLOB_DATA')


def _unescape(literal: str) -> str:
    return re.sub(r"\\(.)", r"\1", literal)


def feature_type(code: str) -> str | None:
    """The constant `defineFeature` is exported as, which documents store as the feature's type."""
    match = _FEATURE_TYPE.search(code)
    return match.group(1) if match else None


def feature_type_name(code: str) -> str | None:
    match = _TYPE_NAME.search(code)
    return _unescape(match.group(1)) if match else None


def feature_description(code: str) -> str:
    """The `"Feature Type Description"` literal alone, so the `~ CREDIT` after it is cut, with each `<br>` a line
    break."""
    match = _DESCRIPTION.search(code)
    if not match:
        return ""
    return re.sub(r"\s*<br\s*/?>\s*", "\n", _unescape(match.group(1))).strip()


def icon_import(code: str) -> str | None:
    """What the feature's `"Icon"` namespace imports: an image tab's element id, or a path in the code folder."""
    icon = _ICON.search(code)
    if not icon:
        return None
    imported = re.search(rf'\b{re.escape(icon.group(1))}::import\(\s*path\s*:\s*"([^"]+)"', code)
    return imported.group(1) if imported else None


def icon_file(code: str, code_dir: pathlib.Path, image_files: dict[str, str]) -> pathlib.Path | None:
    """The local `.svg` a feature's `"Icon"` imports, given the synced images' files by element id."""
    imported = icon_import(code)
    if imported is None:
        return None
    relative = image_files.get(imported, imported)
    path = code_dir / relative
    return path if path.suffix.lower() == ".svg" and path.is_file() else None


def manifest_entry(
    *,
    code: str,
    studio_element_id: str,
    fallback_name: str,
    version: str,
    icon_element_id: str | None,
    source_url: str | None,
) -> dict | None:
    """A feature's entry in the manifest; None when its code exports no `defineFeature`, which the app can't add."""
    constant = feature_type(code)
    if constant is None:
        return None
    entry = {
        "studioElementId": studio_element_id,
        "featureType": constant,
        "name": feature_type_name(code) or fallback_name,
        "description": feature_description(code),
        "version": version,
        "iconElementId": icon_element_id,
    }
    if source_url:
        entry["sourceUrl"] = source_url
    return entry


def parse_manifest(data: bytes | None) -> dict:
    """The manifest as stored, or an empty one; refuses one of another schema version rather than overwrite it."""
    if not data:
        return {"schemaVersion": MANIFEST_SCHEMA_VERSION, "features": []}
    manifest = json.loads(data)
    if manifest.get("schemaVersion") != MANIFEST_SCHEMA_VERSION:
        raise ValueError(
            f"{MANIFEST_NAME} is schema version {manifest.get('schemaVersion')}; this fs writes version "
            f"{MANIFEST_SCHEMA_VERSION}. Update fs."
        )
    return manifest


def with_entry(manifest: dict, entry: dict) -> dict:
    """The manifest with `entry` in place of the one for the same studio, or added at the end."""
    features = [
        entry if feature["studioElementId"] == entry["studioElementId"] else feature
        for feature in manifest["features"]
    ]
    if entry not in features:
        features.append(entry)
    return {**manifest, "features": features}


def without_studio(manifest: dict, studio_element_id: str) -> dict:
    return {
        **manifest,
        "features": [feature for feature in manifest["features"] if feature["studioElementId"] != studio_element_id],
    }


def dump_manifest(manifest: dict) -> bytes:
    return (json.dumps(manifest, indent=4) + "\n").encode()
