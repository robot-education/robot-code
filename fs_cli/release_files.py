"""Releases asked for by files in the repo, which run once their branch is merged into main.

A branch asks for a release by adding a file to `releases/` (see docs/fs-cli.md#release-files), reviewed with its code:

    # releases/frame-hubs.toml
    script = "robotFrame"       # a FeatureScript, as `fs release` takes it; or
    part_set = "fasteners"      # a part set, as `fs parts SET --release` takes it
    bump = "minor"              # major, minor, or patch
    description = "Adds hubs"   # internal, as `--description`
    beta = false                # FeatureScripts: release to the beta document
    publish = false             # FeatureScripts: finalize it at once, and tag it (not a first release)

`fs run-releases`, on main, runs each, deletes its file, and commits what the releases recorded. A FeatureScript's
release isn't final (its version in the frontend document) until `fs sync-versions`, unless it's `publish`ed, so its
frontend examples can be updated first; a first release is never published at once.
"""

from __future__ import annotations

import dataclasses
import pathlib
import tomllib

from fs_cli.config import Config
from fs_cli.git import added_in
from fs_cli.state import load_released, load_studio_files
from fs_cli.versions import VersionType

RELEASES_FOLDER = "releases"
_KEYS = {"script", "part_set", "bump", "description", "beta", "publish"}


class ReleaseFileError(Exception):
    pass


@dataclasses.dataclass
class ReleaseFile:
    path: pathlib.Path
    bump: VersionType | None
    description: str
    script: str | None = None
    part_set: str | None = None
    beta: bool = False
    publish: bool = False
    # The commit that added it, so a file of the same name asking for a later release isn't taken for this one
    added: str | None = None

    @property
    def target(self) -> str:
        return self.script or self.part_set or ""

    @property
    def marker(self) -> str:
        """Ends the description of what it releases, so a release Onshape already has is found again."""
        added = f" {self.added[:12]}" if self.added else ""
        return f"[{RELEASES_FOLDER}/{self.path.name}{added}]"

    @property
    def described(self) -> str:
        return f"{self.description} {self.marker}".strip()


def folder(config: Config) -> pathlib.Path:
    return config.root / RELEASES_FOLDER


def pending(config: Config) -> list[ReleaseFile]:
    """Every release file, in name order, which is the order they run in."""
    if not folder(config).is_dir():
        return []
    return [load(config, path) for path in sorted(folder(config).glob("*.toml"))]


def is_first_release(config: Config, script: str) -> bool:
    """Whether `script` (a FeatureScript, by its name or file, as `fs release` takes it) has never been released, by
    fs-studios.json's released tabs (no API calls). A script it doesn't know isn't, here: releasing it says why."""
    released = load_released(config.studios_path)
    matches = [
        element_id
        for element_id, file in load_studio_files(config.studios_path).items()
        if pathlib.PurePosixPath(file).stem == script or script.replace("\\", "/").endswith(file)
    ]
    return bool(matches) and not any(element_id in released for element_id in matches)


def load(config: Config, path: pathlib.Path) -> ReleaseFile:
    where = path.relative_to(config.root).as_posix()
    try:
        data = tomllib.loads(path.read_text())
    except tomllib.TOMLDecodeError as error:
        raise ReleaseFileError(f"{where} isn't valid TOML: {error}") from error
    unknown = set(data) - _KEYS
    if unknown:
        raise ReleaseFileError(f"{where} has unknown keys: {', '.join(sorted(unknown))} (it takes {', '.join(sorted(_KEYS))}).")
    if ("script" in data) == ("part_set" in data):
        raise ReleaseFileError(f"{where} must name one script (a FeatureScript) or one part_set.")
    bump = data.get("bump")
    if bump is not None and bump not in [version_type.value for version_type in VersionType]:
        raise ReleaseFileError(f"{where}: bump is major, minor, or patch, not {bump!r}.")
    if "part_set" in data:
        if not any(part_set.name == data["part_set"] for part_set in config.parts):
            raise ReleaseFileError(f"{where}: no part set named {data['part_set']} in [tool.fs.parts].")
        if "beta" in data or "publish" in data:
            raise ReleaseFileError(f"{where}: a part set's release has no beta or publish.")
    if data.get("publish", False) and is_first_release(config, data["script"]):
        raise ReleaseFileError(
            f"{where}: {data['script']} has never been released, so its first release isn't published at once: leave out "
            "publish, and `fs sync-versions` publishes it once its frontend examples are ready."
        )
    return ReleaseFile(
        path,
        VersionType(bump) if bump is not None else None,
        str(data.get("description", "")),
        data.get("script"),
        data.get("part_set"),
        bool(data.get("beta", False)),
        bool(data.get("publish", False)),
        added_in(config.root, path),
    )
