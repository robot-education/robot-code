"""Compares local .fs files with the Feature Studios in Onshape and pushes or pulls the differences.

Every Feature Studio is classified into a Status by comparing three things:
    1. The local file.
    2. The Feature Studio's contents in Onshape.
    3. The contents at the time of the last push/pull (recorded in .fs-state.json), or,
       failing that, any recently committed version of the file in git.

If Onshape still matches (3), only the local copy changed and it is safe to push. If the local
file still matches (3), only Onshape changed and it is safe to pull. Otherwise both changed and
the studio is in conflict; --force picks a side.
"""

from __future__ import annotations

import dataclasses
import enum
import pathlib
import re
from concurrent import futures
from typing import Callable, Iterable

from fs_cli import git
from fs_cli.config import Config, DocumentConfig
from fs_cli.remote import Remote, RemoteStudio
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


HINTS = {
    Status.LOCAL_CHANGES: "fs push",
    Status.LOCAL_ONLY: "fs push creates it in Onshape",
    Status.REMOTE_CHANGES: "fs pull",
    Status.REMOTE_ONLY: "fs pull",
    Status.CONFLICT: "fs diff, then fs pull --force or fs push --force",
    Status.DELETED_LOCALLY: "delete the tab in Onshape, or fs pull --force to restore it",
}


class UsageError(Exception):
    pass


def file_name_for(studio_name: str) -> str:
    """Returns the local file name used for a Feature Studio."""
    name = studio_name.replace("/", "_")
    return name if name.endswith(".fs") else name + ".fs"


@dataclasses.dataclass
class Studio:
    """A Feature Studio, as it exists locally and/or in Onshape."""

    document: DocumentConfig
    file: pathlib.Path
    remote: RemoteStudio | None
    saved: StudioState | None
    local_code: str | None
    remote_code: str | None = None
    status: Status = Status.IN_SYNC

    @property
    def label(self) -> str:
        return f"{self.document.name}/{self.file.name}"


@dataclasses.dataclass
class Targets:
    """The documents and files a command operates on.

    Attributes:
        documents: Every document which needs to be scanned.
        whole: The names of documents whose studios are all included.
        files: Individual files to include from the remaining documents.
    """

    documents: list[DocumentConfig]
    whole: set[str]
    files: set[pathlib.Path] = dataclasses.field(default_factory=set)

    def includes(self, studio: Studio) -> bool:
        return studio.document.name in self.whole or studio.file.resolve() in self.files


class Workspace:
    def __init__(
        self, config: Config, state: State, remote: Remote, use_git: bool = True
    ) -> None:
        self.config = config
        self.state = state
        self.remote = remote
        self.use_git = use_git

    # Target resolution

    def resolve_targets(self, args: Iterable[str]) -> Targets:
        """Resolves command line arguments (document names, folders, or .fs files) into Targets."""
        if not self.config.documents:
            raise UsageError(
                "No documents are configured. Add one to featurescripts.toml first."
            )
        args = list(args)
        if not args:
            docs = list(self.config.documents)
            return Targets(docs, {doc.name for doc in docs})

        targets = Targets([], set())
        for arg in args:
            document = self.config.document(arg)
            path = pathlib.Path(arg).resolve()
            if document is not None or (
                (document := self.config.document_for_path(path)) is not None
                and path == document.path.resolve()
            ):
                targets.whole.add(document.name)
            elif document is None:
                raise UsageError(
                    f'"{arg}" is not a configured document or a path inside one.'
                )
            elif path.suffix != ".fs":
                raise UsageError(f'"{arg}" is not a .fs file.')
            else:
                targets.files.add(path)
            if document not in targets.documents:
                targets.documents.append(document)
        return targets

    # Scanning

    def scan(self, targets: Targets) -> list[Studio]:
        """Loads and classifies every studio in targets."""
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            listed = list(
                executor.map(
                    lambda doc: (doc, self.remote.list_studios(doc.instance)),
                    targets.documents,
                )
            )
            studios = [
                studio
                for doc, remote_studios in listed
                for studio in self._match(doc, remote_studios)
                if targets.includes(studio)
            ]
            # Fetching code is the slow part, so classify in parallel
            list(executor.map(self._classify, studios))

        for studio in studios:
            if studio.status == Status.IN_SYNC:
                assert studio.remote and studio.local_code is not None
                self._record(studio, studio.local_code, studio.remote.microversion_id)
        return studios

    def _match(
        self, doc: DocumentConfig, remote_studios: list[RemoteStudio]
    ) -> list[Studio]:
        saved = self.state.studios(doc.name)
        remote_ids = {remote.element_id for remote in remote_studios}
        # Forget studios which no longer exist in Onshape
        for element_id in list(saved):
            if element_id not in remote_ids:
                del saved[element_id]

        local_files = (
            {path.name: path for path in doc.path.glob("*.fs") if path.is_file()}
            if doc.path.is_dir()
            else {}
        )
        studios = []
        claimed: set[str] = set()
        for remote in remote_studios:
            entry = saved.get(remote.element_id)
            file_name = entry.file if entry else file_name_for(remote.name)
            if file_name in claimed:
                print(
                    f'Warning: skipping Feature Studio "{remote.name}" in {doc.name} since another studio already maps to {file_name}.'
                )
                continue
            claimed.add(file_name)
            path = doc.path / file_name
            studios.append(
                Studio(doc, path, remote, entry, _read(local_files.get(file_name)))
            )

        for file_name, path in sorted(local_files.items()):
            if file_name not in claimed:
                studios.append(Studio(doc, path, None, None, _read(path)))
        return studios

    def _classify(self, studio: Studio) -> None:
        studio.status = self._compute_status(studio)

    def _compute_status(self, studio: Studio) -> Status:
        remote, saved, local = studio.remote, studio.saved, studio.local_code
        if remote is None:
            return Status.LOCAL_ONLY
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
                studio.document.instance, studio.remote.element_id
            )
        return studio.remote_code

    def _record(self, studio: Studio, code: str, microversion_id: str) -> None:
        assert studio.remote
        self.state.studios(studio.document.name)[studio.remote.element_id] = (
            StudioState(studio.file.name, content_hash(code), microversion_id)
        )

    # Actions

    def push_studios(self, studios: list[Studio]) -> dict[str, list[str]]:
        """Pushes studios to Onshape, creating any which don't exist yet.

        Returns a dict mapping studio labels to any notices Onshape reported.
        """

        def push(studio: Studio) -> list[str]:
            assert studio.local_code is not None
            instance = studio.document.instance
            if studio.remote is None:
                studio.remote = self.remote.create(instance, studio.file.name)
            return self.remote.push(
                instance, studio.remote.element_id, studio.local_code
            )

        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            notices = dict(
                zip(
                    (studio.label for studio in studios),
                    executor.map(push, studios),
                )
            )

        # Pushing changes each studio's microversion; fetch the new ones so the next
        # command knows Onshape hasn't been modified since
        for doc in {
            studio.document.name: studio.document for studio in studios
        }.values():
            microversions = {
                remote.element_id: remote.microversion_id
                for remote in self.remote.list_studios(doc.instance)
            }
            for studio in studios:
                if studio.document is doc and studio.remote:
                    assert studio.local_code is not None
                    microversion_id = microversions.get(
                        studio.remote.element_id, studio.remote.microversion_id
                    )
                    self._record(studio, studio.local_code, microversion_id)
        return notices

    def pull_studios(self, studios: list[Studio]) -> None:
        with futures.ThreadPoolExecutor(MAX_WORKERS) as executor:
            list(executor.map(self.fetch, studios))
        for studio in studios:
            assert studio.remote and studio.remote_code is not None
            studio.file.parent.mkdir(parents=True, exist_ok=True)
            studio.file.write_text(studio.remote_code, newline="")
            studio.local_code = studio.remote_code
            self._record(studio, studio.remote_code, studio.remote.microversion_id)


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


def select(studios: list[Studio], *statuses: Status) -> list[Studio]:
    return [studio for studio in studios if studio.status in statuses]


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
