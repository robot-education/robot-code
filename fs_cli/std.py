"""Keeps the copy of the Onshape std library in std/ up to date.

std/std.json records the std version and, when the std was pulled from Onshape, the microversion of
every std Feature Studio, so later pulls only download the files that changed. Note that every std
release bumps the version numbers in every std file, so pulling a new release from Onshape costs
one API call per file (~265) regardless; the GitHub mirror (the default) costs none.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re
import shutil
import subprocess
import tempfile
from concurrent import futures
from typing import Callable

from fs_cli.remote import Remote, file_name_for
from onshape_api.model.constants import STD_PATH
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import InstancePath, path_to_url

MIRROR_URL = "https://github.com/javawizard/onshape-std-library-mirror"
MIRROR_BRANCH = "main"
METADATA_FILE = "std.json"
MAX_WORKERS = 8

README = """# Onshape std library

The [Onshape FeatureScript standard library](https://cad.onshape.com/documents/12312312345abcabcabcdeff),
version **{version}**, checked in for reference and for the language server's stdlib indexes. It's MIT
licensed; see `LICENSE.txt`.

Source: {source}

Don't edit these files. Update them with `uv run fs pull-std`.
"""


@dataclasses.dataclass
class StdMetadata:
    """
    Attributes:
        version: The std version, e.g. "2960.0".
        source: Where the files came from.
        microversions: Maps file names to their Onshape microversions, when known.
    """

    version: str | None = None
    source: str = ""
    microversions: dict[str, str] = dataclasses.field(default_factory=dict)

    @classmethod
    def load(cls, std_dir: pathlib.Path) -> StdMetadata:
        path = std_dir / METADATA_FILE
        if not path.is_file():
            return cls()
        return cls(**json.loads(path.read_text()))

    def save(self, std_dir: pathlib.Path) -> None:
        std_dir.mkdir(parents=True, exist_ok=True)
        (std_dir / METADATA_FILE).write_text(
            json.dumps(dataclasses.asdict(self), indent=2, sort_keys=True) + "\n"
        )
        (std_dir / "README.md").write_text(
            README.format(version=self.version, source=self.source)
        )

    @property
    def number(self) -> str | None:
        """The version as used in FeatureScript, e.g. "2960"."""
        return self.version.removesuffix(".0") if self.version else None


@dataclasses.dataclass
class StdUpdate:
    version: str
    changed: list[str]
    removed: list[str]
    api_calls: int = 0


def pull_from_mirror(
    std_dir: pathlib.Path, dry_run: bool, log: Callable[[str], None] = print
) -> StdUpdate:
    """Updates std_dir to the latest std on the GitHub mirror. Makes no Onshape API calls."""
    metadata = StdMetadata.load(std_dir)
    with tempfile.TemporaryDirectory() as temp:
        clone = pathlib.Path(temp) / "std"
        log(f"Fetching the latest std from {MIRROR_URL}...")
        subprocess.run(
            [
                "git",
                "clone",
                "--quiet",
                "--depth",
                "1",
                "--branch",
                MIRROR_BRANCH,
                MIRROR_URL,
                str(clone),
            ],
            check=True,
        )
        commit, subject = _git(clone, "log", "-1", "--format=%H%n%s").splitlines()
        match = re.search(r"Version (\S+)", subject)
        version = match[1] if match else subject
        files = {path.name: path.read_text() for path in clone.glob("*.fs")}
        update = _diff(std_dir, version, files)
        if not dry_run:
            _write(std_dir, files, update)
            shutil.copyfile(clone / "LICENSE.txt", std_dir / "LICENSE.txt")
            if update.changed or update.removed or metadata.version != version:
                # Onshape microversions aren't known for mirrored files
                metadata = StdMetadata(version, f"{MIRROR_URL}/tree/{commit}", {})
            metadata.save(std_dir)
    return update


def pull_from_onshape(
    std_dir: pathlib.Path,
    remote: Remote,
    dry_run: bool,
    confirm: Callable[[int], None],
    log: Callable[[str], None] = print,
) -> StdUpdate:
    """Updates std_dir to the latest std version in Onshape, downloading only changed files.

    Args:
        confirm: Called with the number of files to download before downloading them.
    """
    metadata = StdMetadata.load(std_dir)
    latest = remote.versions(STD_PATH)[-1]
    instance = InstancePath.from_path(STD_PATH, latest.id, InstanceType.VERSION)
    studios = {file_name_for(s.name): s for s in remote.list_studios(instance)}
    api_calls = 2
    to_download = [
        name
        for name, studio in studios.items()
        if metadata.microversions.get(name) != studio.microversion_id
        or not (std_dir / name).is_file()
    ]
    log(
        f"Onshape std {latest.name}: {len(to_download)} of {len(studios)} files changed since the last pull."
    )
    if to_download and not dry_run:
        confirm(len(to_download))

    files = {
        name: (std_dir / name).read_text()
        for name in studios
        if name not in to_download and (std_dir / name).is_file()
    }
    if not dry_run:
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            downloaded = executor.map(
                lambda name: remote.pull(instance, studios[name].element_id),
                to_download,
            )
            files.update(zip(to_download, downloaded))
        api_calls += len(to_download)
    else:
        files.update({name: "" for name in to_download})

    update = _diff(std_dir, latest.name, files)
    update.changed = sorted(set(update.changed) | set(to_download))
    update.api_calls = api_calls
    if not dry_run:
        _write(std_dir, files, update)
        StdMetadata(
            latest.name,
            path_to_url(instance),
            {name: studio.microversion_id for name, studio in studios.items()},
        ).save(std_dir)
    return update


def _diff(std_dir: pathlib.Path, version: str, files: dict[str, str]) -> StdUpdate:
    existing = (
        {path.name for path in std_dir.glob("*.fs")} if std_dir.is_dir() else set()
    )
    changed = [
        name
        for name, code in files.items()
        if name not in existing or (std_dir / name).read_text() != code
    ]
    removed = sorted(existing - files.keys())
    return StdUpdate(version, sorted(changed), removed)


def _write(std_dir: pathlib.Path, files: dict[str, str], update: StdUpdate) -> None:
    std_dir.mkdir(parents=True, exist_ok=True)
    for name in update.changed:
        (std_dir / name).write_text(files[name])
    for name in update.removed:
        (std_dir / name).unlink()


def _git(directory: pathlib.Path, *args: str) -> str:
    return subprocess.run(
        ["git", *args], cwd=directory, check=True, capture_output=True, text=True
    ).stdout
