"""Compares the local .fs files with the Feature Studios in the backend document.

Every Feature Studio is classified into a Status by comparing three things:
    1. The local file.
    2. The Feature Studio's contents in Onshape.
    3. The contents at the time of the last push/pull (recorded in .fs-state.json), or,
       failing that, any recently committed version of the file in git.

If Onshape still matches (3), only the local copy changed and it is safe to push. If the local
file still matches (3), only Onshape changed and it is safe to pull. Otherwise both changed and
the studio is in conflict; --force picks a side.

Studios are tracked by element id, so the local folder structure is the source of truth: files can
be renamed and moved freely, and Onshape's folders are ignored. The only time Onshape's folders matter
is when a studio is pulled into the repo for the first time, where it's placed in the matching folder.
"""

from __future__ import annotations

import collections
import dataclasses
import enum
import pathlib
import re
from concurrent import futures
from typing import Callable, Iterable

from fs_cli import git
from fs_cli.config import Config
from fs_cli.remote import Remote, RemoteStudio, file_name_for, relative_path_for
from onshape_api.exceptions import ApiError
from fs_cli.state import State, StudioState, content_hash

MAX_WORKERS = 8


class Status(enum.Enum):
    IN_SYNC = "in sync"
    LOCAL_CHANGES = "modified locally"
    LOCAL_ONLY = "new locally"
    REMOTE_CHANGES = "changed in Onshape"
    REMOTE_ONLY = "only in Onshape"
    CONFLICT = "changed locally and in Onshape"
    DELETED_LOCALLY = "deleted locally"
    DELETED_IN_ONSHAPE = "deleted in Onshape"


HINTS = {
    Status.LOCAL_CHANGES: "fs push",
    Status.LOCAL_ONLY: "fs push creates it in Onshape",
    Status.REMOTE_CHANGES: "fs pull",
    Status.REMOTE_ONLY: "fs pull",
    Status.CONFLICT: "fs diff, then fs pull --force or fs push --force",
    Status.DELETED_LOCALLY: "delete the tab in Onshape, or fs pull --force to restore it",
    Status.DELETED_IN_ONSHAPE: "fs push recreates it, or delete the file",
}

PUSHABLE = (Status.LOCAL_CHANGES, Status.LOCAL_ONLY, Status.DELETED_IN_ONSHAPE)
PULLABLE = (Status.REMOTE_CHANGES, Status.REMOTE_ONLY)


class UsageError(Exception):
    pass


@dataclasses.dataclass
class Studio:
    """A Feature Studio, as it exists locally and/or in Onshape.

    Attributes:
        path: The local path relative to the code folder (using /), whether or not it exists.
        file: The absolute local path.
        located: False for studios only in Onshape whose local folder hasn't been looked up yet.
    """

    path: str
    file: pathlib.Path
    remote: RemoteStudio | None
    saved: StudioState | None
    local_code: str | None
    deleted_in_onshape: bool = False
    located: bool = True
    remote_code: str | None = None
    status: Status = Status.IN_SYNC

    @property
    def name(self) -> str:
        return (
            self.remote.name if self.remote else pathlib.PurePosixPath(self.path).name
        )


@dataclasses.dataclass
class Targets:
    """The studios a command operates on: files or folders inside the code folder, or studio names."""

    paths: set[str]
    folders: set[str]
    names: set[str]

    def matches(self, target: str, studio: Studio) -> bool:
        if target in self.folders:
            return target == "" or studio.path.startswith(target + "/")
        if target in self.paths:
            return target == studio.path
        name = studio.name
        return target in (name, name.removesuffix(".fs"), file_name_for(name))

    def all(self) -> set[str]:
        return self.paths | self.folders | self.names


class Workspace:
    def __init__(
        self, config: Config, state: State, remote: Remote, use_git: bool = True
    ) -> None:
        self.config = config
        self.state = state
        self.remote = remote
        self.use_git = use_git
        self.instance = config.backend

    def resolve_targets(self, args: Iterable[str]) -> Targets | None:
        """Resolves command line arguments into Targets, or None for everything.

        Arguments may be .fs files or folders inside the code folder, or Feature Studio names.
        """
        args = list(args)
        if not args:
            return None
        targets = Targets(set(), set(), set())
        code_dir = self.config.code_dir.resolve()
        for arg in args:
            path = pathlib.Path(arg).resolve()
            inside = path == code_dir or code_dir in path.parents
            if inside and (path.is_dir() or path.suffix != ".fs"):
                relative = path.relative_to(code_dir).as_posix()
                targets.folders.add("" if relative == "." else relative)
            elif inside:
                targets.paths.add(path.relative_to(code_dir).as_posix())
            elif path.exists():
                raise UsageError(
                    f'"{arg}" is outside {self.config.code_dir.relative_to(self.config.root)}/.'
                )
            else:
                targets.names.add(arg)
        return targets

    # Scanning

    def scan(self, targets: Targets | None = None) -> list[Studio]:
        """Loads and classifies every studio (or just those in targets)."""
        studios = self._match(self.remote.list_studios(self.instance))
        if targets is not None:
            selected = []
            for target in sorted(targets.all()):
                matched = [s for s in studios if targets.matches(target, s)]
                if not matched and target not in targets.folders:
                    raise UsageError(f'No FeatureScript matches "{target}".')
                selected.extend(s for s in matched if s not in selected)
            studios = selected

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            # Fetching code is the slow part, so classify in parallel
            list(executor.map(self._classify, studios))

        for studio in studios:
            if studio.status == Status.IN_SYNC:
                assert studio.remote and studio.local_code is not None
                self._record(studio, studio.local_code, studio.remote.microversion_id)
        return sorted(studios, key=lambda studio: studio.path)

    def _match(self, remote_studios: list[RemoteStudio]) -> list[Studio]:
        """Pairs Feature Studios with local files.

        Synced studios keep the file recorded in the state, following it if it's moved or renamed
        locally. Otherwise (e.g. on a fresh clone) a studio is paired with the one local file of
        the same name.
        """
        saved = self.state.studios
        remote_ids = {remote.element_id for remote in remote_studios}
        deleted_in_onshape = set()
        for element_id in list(saved):
            if element_id not in remote_ids:
                deleted_in_onshape.add(saved.pop(element_id).file)

        code_dir = self.config.code_dir
        local = (
            {
                path.relative_to(code_dir).as_posix(): path
                for path in code_dir.rglob("*.fs")
                if path.is_file()
            }
            if code_dir.is_dir()
            else {}
        )
        studios: list[Studio] = []
        claimed: set[str] = set()

        def add(path: str, remote: RemoteStudio | None, located: bool = True) -> None:
            claimed.add(path)
            entry = saved.get(remote.element_id) if remote else None
            studios.append(
                Studio(
                    path,
                    code_dir / path,
                    remote,
                    entry,
                    _read(local.get(path)),
                    deleted_in_onshape=remote is None and path in deleted_in_onshape,
                    located=located,
                )
            )

        unmatched = []
        for remote in remote_studios:
            entry = saved.get(remote.element_id)
            if entry and entry.file in local and entry.file not in claimed:
                add(entry.file, remote)
            else:
                unmatched.append(remote)

        by_name = collections.defaultdict(list)
        for path in local:
            if path not in claimed:
                by_name[pathlib.PurePosixPath(path).name].append(path)
        wanted = collections.Counter(file_name_for(r.name) for r in unmatched)
        for remote in unmatched:
            name = file_name_for(remote.name)
            candidates = by_name.get(name, [])
            entry = saved.get(remote.element_id)
            if entry and not (len(candidates) == 1 and wanted[name] == 1):
                # Renamed locally: an untracked file still holds the last synced contents
                candidates = [
                    path
                    for path in local
                    if path not in claimed
                    and content_hash(_read(local[path]) or "") == entry.hash
                ]
            if len(candidates) == 1 and (entry or wanted[name] == 1):
                add(candidates[0], remote)
                if entry:
                    # Moved or renamed locally
                    entry.file = candidates[0]
            elif entry:
                add(entry.file, remote)  # Deleted locally
            else:
                add(relative_path_for(remote.name), remote, located=False)

        for path in sorted(local):
            if path not in claimed:
                add(path, None)
        return studios

    def _classify(self, studio: Studio) -> None:
        studio.status = self._compute_status(studio)

    def _compute_status(self, studio: Studio) -> Status:
        remote, saved, local = studio.remote, studio.saved, studio.local_code
        if remote is None:
            return (
                Status.DELETED_IN_ONSHAPE
                if studio.deleted_in_onshape
                else Status.LOCAL_ONLY
            )
        if local is None:
            return Status.DELETED_LOCALLY if saved else Status.REMOTE_ONLY

        local_hash = content_hash(local)
        if saved and saved.microversion_id == remote.microversion_id:
            # Untouched in Onshape since we last synced; no need to download it
            remote_hash = saved.hash
        else:
            remote_hash = content_hash(self.fetch(studio))

        if local_hash == remote_hash:
            return Status.IN_SYNC
        if (saved and saved.hash == remote_hash) or remote_hash in self._history(
            studio
        ):
            return Status.LOCAL_CHANGES
        if saved and saved.hash == local_hash:
            return Status.REMOTE_CHANGES
        return Status.CONFLICT

    def _history(self, studio: Studio) -> set[str]:
        if not self.use_git:
            return set()
        return git.committed_hashes(self.config.root, studio.file)

    def fetch(self, studio: Studio) -> str:
        """Returns (and caches) the studio's contents in Onshape."""
        assert studio.remote
        if studio.remote_code is None:
            studio.remote_code = self.remote.pull(
                self.instance, studio.remote.element_id
            )
        return studio.remote_code

    def _record(self, studio: Studio, code: str, microversion_id: str) -> None:
        assert studio.remote
        self.state.studios[studio.remote.element_id] = StudioState(
            studio.path, content_hash(code), microversion_id
        )

    # Actions

    def push_studios(self, studios: list[Studio]) -> dict[str, list[str]]:
        """Pushes studios to Onshape, creating any which don't exist yet.

        New studios are created at the top level of the document, since the API can't place
        them in folders. Returns a dict mapping studio paths to any notices Onshape reported.
        """

        def push(studio: Studio) -> list[str]:
            assert studio.local_code is not None
            if studio.remote is None:
                studio.remote = self.remote.create(self.instance, studio.name)
            return self.remote.push(
                self.instance, studio.remote.element_id, studio.local_code
            )

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            notices = dict(
                zip((studio.path for studio in studios), executor.map(push, studios))
            )

        # Pushing changes each studio's microversion; fetch the new ones so the next
        # command knows Onshape hasn't been modified since
        current = {
            remote.element_id: remote
            for remote in self.remote.list_studios(self.instance)
        }
        for studio in studios:
            assert studio.remote and studio.local_code is not None
            studio.remote = current.get(studio.remote.element_id, studio.remote)
            self._record(studio, studio.local_code, studio.remote.microversion_id)
        return notices

    def pull_studios(self, studios: list[Studio]) -> None:
        """Writes the Onshape contents of studios to their local files."""
        self._locate([studio for studio in studios if not studio.located])
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(self.fetch, studios))
        for studio in studios:
            assert studio.remote and studio.remote_code is not None
            studio.file.parent.mkdir(parents=True, exist_ok=True)
            studio.file.write_text(studio.remote_code, newline="")
            studio.local_code = studio.remote_code
            self._record(studio, studio.remote_code, studio.remote.microversion_id)

    def _locate(self, studios: list[Studio]) -> None:
        """Places studios new to the repo in the folders they're in in Onshape.

        This takes an extra API call, so it's only done when such a studio is actually pulled.
        """
        if not studios:
            return
        try:
            folders = self.remote.studio_folders(self.instance)
        except ApiError as error:
            print(
                f"Warning: couldn't read the backend document's folders, so new files go in the top level folder ({error})."
            )
            folders = {}
        for studio in studios:
            assert studio.remote
            path = relative_path_for(
                studio.remote.name, folders.get(studio.remote.element_id, ())
            )
            if not (self.config.code_dir / path).exists():
                studio.path, studio.file = path, self.config.code_dir / path
            studio.located = True


def select(studios: list[Studio], *statuses: Status) -> list[Studio]:
    return [studio for studio in studios if studio.status in statuses]


def _read(path: pathlib.Path | None) -> str | None:
    if path is None or not path.is_file():
        return None
    with path.open(newline="") as file:
        return file.read()


# Updating the FeatureScript version

VERSION_PATTERN = re.compile(r'version : "(\d{2,7})\.0"|FeatureScript (\d{2,7});')


def update_std_version(code: str, std_version: str) -> str:
    """Updates the FeatureScript version header and all std imports to std_version."""

    def replace(match: re.Match) -> str:
        return re.sub(r"\d{2,7}", std_version, match.group(0), count=1)

    return VERSION_PATTERN.sub(replace, code)


def apply_to_files(
    studios: list[Studio], transform: Callable[[str], str]
) -> list[Studio]:
    """Applies transform to each local file. Returns the studios which changed."""
    changed = []
    for studio in studios:
        if studio.local_code is None:
            continue
        new_code = transform(studio.local_code)
        if new_code != studio.local_code:
            studio.file.write_text(new_code, newline="")
            studio.local_code = new_code
            changed.append(studio)
    return changed
