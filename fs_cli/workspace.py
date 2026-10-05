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
from fs_cli.renames import most_similar
from fs_cli.remote import Remote, RemoteStudio, file_name_for, relative_path_for
from onshape_api.exceptions import ApiError
from fs_cli.state import (
    State,
    StudioState,
    apply_import_versions,
    content_hash,
    import_versions,
    path_imports,
    resolve_path_imports,
)

MAX_WORKERS = 8


class Status(enum.Enum):
    IN_SYNC = "in sync"
    LOCAL_CHANGES = "modified locally"
    LOCAL_ONLY = "new locally"
    REMOTE_CHANGES = "changed in Onshape"
    REMOTE_ONLY = "only in Onshape"
    CONFLICT = "changed locally and in Onshape"
    DELETED_LOCALLY = "deleted locally"
    DELETE_CONFLICT = "deleted locally, changed in Onshape"
    DELETED_IN_ONSHAPE = "deleted in Onshape"


HINTS = {
    Status.LOCAL_CHANGES: "fs push",
    Status.LOCAL_ONLY: "fs push creates it in Onshape",
    Status.REMOTE_CHANGES: "fs pull",
    Status.REMOTE_ONLY: "fs pull",
    Status.CONFLICT: "fs diff, then fs pull --force or fs push --force",
    Status.DELETED_LOCALLY: "fs push deletes the tab, or fs pull --force restores it",
    Status.DELETE_CONFLICT: "fs push --force deletes the tab, or fs pull --force restores it",
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
        located: False for studios only in Onshape whose folder hasn't been looked up yet.
        import_updates: Same-document import versions which Onshape changed and the local file
            doesn't have yet, by element id. These aren't treated as edits; see content_hash.
    """

    path: str
    file: pathlib.Path
    remote: RemoteStudio | None
    saved: StudioState | None
    local_code: str | None
    deleted_in_onshape: bool = False
    located: bool = True
    import_updates: dict[str, str] = dataclasses.field(default_factory=dict)
    remote_code: str | None = None
    status: Status = Status.IN_SYNC
    # The path it was synced with, if its file has been renamed or moved since
    renamed_from: str | None = None

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
        self.listed_microversions: dict[str, str] = {}

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
        remote_studios = self.remote.list_studios(self.instance)
        # Every studio's microversion before anything is pushed (see push_studios)
        self.listed_microversions = {
            remote.element_id: remote.microversion_id for remote in remote_studios
        }
        studios = self._match(remote_studios)
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
            # Studios with import updates are recorded once the updates are applied, so they're
            # checked again until then
            if studio.status == Status.IN_SYNC and not studio.import_updates:
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

        # Files synced with other studios aren't candidates for renamed ones
        mapped = {entry.file for entry in saved.values()}
        by_name = collections.defaultdict(list)
        for path in local:
            if path not in claimed:
                by_name[pathlib.PurePosixPath(path).name].append(path)
        wanted = collections.Counter(file_name_for(r.name) for r in unmatched)
        for remote in unmatched:
            name = file_name_for(remote.name)
            candidates = by_name.get(name, [])
            entry = saved.get(remote.element_id)
            remote_code = None
            if entry and not (len(candidates) == 1 and wanted[name] == 1):
                # Renamed locally: an untracked file still holds the last synced contents
                unclaimed = [path for path in local if path not in claimed and path not in mapped]
                candidates = [
                    path for path in unclaimed if content_hash(_read(local[path]) or "") == entry.hash
                ]
                if len(candidates) != 1 and unclaimed:
                    # Renamed and edited: the new file most like what it was
                    previous = self._committed(entry.file)
                    if previous is None:
                        previous = remote_code = self.remote.pull(self.instance, remote.element_id)
                    similar = most_similar(previous, {path: _read(local[path]) or "" for path in unclaimed})
                    candidates = [similar] if similar else []
            if len(candidates) == 1 and (entry or wanted[name] == 1):
                add(candidates[0], remote)
                studios[-1].remote_code = remote_code
                if entry and entry.file != candidates[0]:
                    # Moved or renamed locally
                    studios[-1].renamed_from = entry.file
                    entry.file = candidates[0]
            elif entry:
                add(entry.file, remote)  # Deleted locally
            else:
                # Placed in its Onshape folder when it's pulled
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
            if not saved:
                return Status.REMOTE_ONLY
            if (
                saved.microversion_id != remote.microversion_id
                and content_hash(self.fetch(studio)) != saved.hash
            ):
                return Status.DELETE_CONFLICT
            return Status.DELETED_LOCALLY

        local_hash = content_hash(local)
        if saved and saved.microversion_id == remote.microversion_id:
            # Untouched in Onshape since we last synced; no need to download it
            remote_hash = saved.hash
        else:
            remote_hash = content_hash(self.fetch(studio))

        if studio.remote_code is not None:
            local_versions = import_versions(local)
            studio.import_updates = {
                element_id: version
                for element_id, version in import_versions(studio.remote_code).items()
                if local_versions.get(element_id, version) != version
            }
        if local_hash == remote_hash:
            return Status.IN_SYNC
        if (saved and saved.hash == remote_hash) or remote_hash in self._history(
            studio
        ):
            return Status.LOCAL_CHANGES
        if saved and saved.hash == local_hash:
            return Status.REMOTE_CHANGES
        return Status.CONFLICT

    def _committed(self, path: str) -> str | None:
        """The last committed contents of a file in the code folder, if it's been committed."""
        if not self.use_git:
            return None
        return git.committed_contents(self.config.root, self.config.code_dir / path)

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

    def apply_import_updates(self, studios: list[Studio]) -> list[Studio]:
        """Writes import versions Onshape changed into local files. Makes no API calls.

        Returns the studios which were updated.
        """
        updated = []
        for studio in studios:
            if not studio.import_updates or studio.local_code is None:
                continue
            if studio.status not in (Status.IN_SYNC, Status.LOCAL_CHANGES):
                continue
            code = apply_import_versions(studio.local_code, studio.import_updates)
            studio.file.write_text(code, newline="")
            studio.local_code = code
            studio.import_updates = {}
            if studio.status == Status.IN_SYNC:
                assert studio.remote
                self._record(studio, code, studio.remote.microversion_id)
            updated.append(studio)
        return updated

    def path_import_problems(self, studios: list[Studio]) -> list[str]:
        """Checks that the imports by path in studios (about to be pushed) can be resolved.

        A path must be a file in the code folder which is in Onshape, or is being pushed too.
        Makes no API calls.
        """
        synced = {entry.file for entry in self.state.studios.values()}
        pushing = {studio.path for studio in studios}
        problems = []
        for studio in studios:
            for path in path_imports(studio.local_code or ""):
                if not (self.config.code_dir / path).is_file():
                    problems.append(f"{studio.path} imports {path}, which doesn't exist.")
                elif path not in synced and path not in pushing:
                    problems.append(
                        f"{studio.path} imports {path}, which isn't in Onshape yet; push it too."
                    )
        return problems

    def push_studios(self, studios: list[Studio]) -> None:
        """Pushes studios to Onshape, creating any which don't exist yet.

        New studios are created at the top level of the document, since the API can't place
        them in folders. Imports by path (see path_import_problems) are then replaced with
        imports by element id, in the local files too.
        """

        def create(studio: Studio) -> None:
            studio.remote = self.remote.create(self.instance, studio.name)

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(create, [s for s in studios if s.remote is None]))

        targets = {
            entry.file: (element_id, entry.microversion_id)
            for element_id, entry in self.state.studios.items()
        }
        for studio in studios:
            assert studio.remote
            targets[studio.path] = (studio.remote.element_id, studio.remote.microversion_id)

        def push(studio: Studio) -> None:
            assert studio.local_code is not None and studio.remote
            code = resolve_path_imports(studio.local_code, targets)
            if studio.remote_code is not None:
                # Never push import versions older than the ones Onshape has
                code = apply_import_versions(code, import_versions(studio.remote_code))
            if code != studio.local_code:
                studio.file.write_text(code, newline="")
                studio.local_code = code
            self.remote.push(self.instance, studio.remote.element_id, code)

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(push, studios))

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

        # Onshape updates the versions of imports of the studios just pushed, which changes the
        # microversions of the studios importing them. Studios which matched their last sync before
        # the push only changed for that reason (barring edits made during the push), and the
        # versions of imports aren't part of the content hash, so record their new microversions
        # rather than downloading them next time
        pushed = {studio.remote.element_id for studio in studios if studio.remote}
        for element_id, entry in self.state.studios.items():
            before = self.listed_microversions.get(element_id)
            after = current.get(element_id)
            if element_id in pushed or before is None or after is None:
                continue
            if entry.microversion_id == before != after.microversion_id:
                entry.microversion_id = after.microversion_id

    def delete_studios(self, studios: list[Studio]) -> None:
        """Deletes the tabs of studios whose files were deleted locally."""

        def delete(studio: Studio) -> None:
            assert studio.remote
            self.remote.delete(self.instance, studio.remote.element_id)

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(delete, studios))
        for studio in studios:
            assert studio.remote
            self.state.studios.pop(studio.remote.element_id, None)

    def importers(self, element_id: str) -> list[str]:
        """Returns the local files (relative to the code folder) which import a tab. Makes no API calls."""
        pattern = re.compile(
            rf'^\s*(export\s+)?import\s*\(\s*path\s*:\s*"{re.escape(element_id)}"',
            re.MULTILINE,
        )
        code_dir = self.config.code_dir
        return sorted(
            path.relative_to(code_dir).as_posix()
            for path in code_dir.rglob("*.fs")
            if path.is_file() and pattern.search(_read(path) or "")
        )

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
            studio.import_updates = {}
            self._record(studio, studio.remote_code, studio.remote.microversion_id)

    def _locate(self, studios: list[Studio]) -> None:
        """Places studios new to the repo in the folders they're in in Onshape.

        This takes an extra API call, so it's only done when such studios are pulled.
        """
        if not studios:
            return
        try:
            folders = self.remote.studio_folders(self.instance)
        except ApiError as error:
            print(
                f"Warning: couldn't read the document's folders, so new files go in the top level folder ({error})."
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
