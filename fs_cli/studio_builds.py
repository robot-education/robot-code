"""Building a definition's configured Part Studios (see configured_studios.py): in the backend document, to try them
with the FeatureScript as pushed, and in a library's document as its FeatureScript is released there."""

from __future__ import annotations

import pathlib
import re
import runpy
from collections.abc import Callable

from fs_cli.config import Config, Library
from fs_cli.configured_studios import Built, ConfiguredStudio, configured_studios
from fs_cli.remote import Remote, RemoteStudio
from fs_cli.workspace import UsageError
from onshape_api.paths.paths import InstancePath


def defines(code: str, feature: str) -> bool:
    """Whether a Feature Studio's code defines a feature, by the constant it's exported as."""
    return re.search(rf"\bexport\s+const\s+{re.escape(feature)}\s*=\s*defineFeature\b", code) is not None


def definition_studios(path: pathlib.Path) -> list[ConfiguredStudio]:
    return configured_studios(runpy.run_path(str(path)))


def library_definitions(config: Config, library: Library, code: str) -> list[tuple[pathlib.Path, list[ConfiguredStudio]]]:
    """The definitions in a library's folders whose studios' features a Feature Studio's code defines, each with its
    studios."""
    found = []
    for folder in library.folders:
        for path in sorted((config.code_dir / folder).rglob("*.py")):
            studios = definition_studios(path)
            if studios and all(defines(code, studio.feature) for studio in studios):
                found.append((path, studios))
    return found


def build_studios(
    remote: Remote,
    instance: InstancePath,
    studios: list[ConfiguredStudio],
    feature_studio: RemoteStudio,
    log: Callable[[str], None] = print,
) -> dict[str, Built]:
    """Builds each studio in a workspace from a Feature Studio there. A feature that fails to regenerate in its default
    configuration stops the build."""
    built: dict[str, Built] = {}
    for studio in studios:
        log(f"Building {studio.tab_name}...")
        result = remote.build_studio(instance, studio, feature_studio.element_id, feature_studio.microversion_id)
        if result.status == "ERROR":
            raise UsageError(
                f"{studio.tab_name}'s {studio.feature} fails to regenerate in its default configuration; "
                "its Part Studio says why."
            )
        built[studio.name] = result
    return built


def write_exports(path: pathlib.Path) -> list[pathlib.Path]:
    """Writes a definition's EXPORTS again, now that its studios' locations changed; answers those that changed."""
    definition = runpy.run_path(str(path))
    changed = []
    for name, text in definition.get("EXPORTS", {}).items():
        exported = path.with_name(name)
        if not exported.is_file() or exported.read_text() != text:
            exported.write_text(text, newline="")
            changed.append(exported)
    return changed
