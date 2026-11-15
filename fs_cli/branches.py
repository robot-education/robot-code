"""Which git branch may do what with the backend document.

Every branch works in the backend's main workspace: `fs push` from any of them writes it. Releasing (cutting backend
versions) runs only on `main_branch` (set in [tool.fs]), and only once it's what GitHub has: HEAD is origin's
`main_branch`, and what's released is committed (`check_synced_with_origin`). A release then pushes what it needs over
Onshape's (`fs push --release`), so it's what's on GitHub, whatever else the workspace holds. See
docs/fs-cli.md#branches.
"""

from __future__ import annotations

import dataclasses
import enum
import os
import pathlib

from fs_cli.config import Config
from fs_cli.git import changed_paths, current_branch, fetch, head_commit, remote_commit
from onshape_api.paths.paths import InstancePath

# Names the branch where git can't, e.g. on the detached HEAD a CI checkout may leave
BRANCH_VARIABLE = "FS_BRANCH"


class BranchError(Exception):
    pass


class Access(enum.Enum):
    """What a command does with the backend document."""

    # Reads or writes the main workspace, from any branch
    ANY = "any"
    # Releases: runs only on the main branch
    MAIN = "main"


@dataclasses.dataclass
class Backend:
    """The backend workspace a command works in, and the state recording what it last synced with it."""

    instance: InstancePath
    state_path: pathlib.Path


def branch_name(config: Config) -> str | None:
    return os.environ.get(BRANCH_VARIABLE) or current_branch(config.root)


def resolve(config: Config, access: Access) -> Backend:
    """The backend workspace: the main one, for every branch. Refuses a release off the main branch."""
    if access is Access.MAIN and config.main_branch is not None:
        branch = branch_name(config)
        if branch is None:
            raise BranchError(f"fs works on a git branch: check one out, or name it in {BRANCH_VARIABLE}.")
        if branch != config.main_branch:
            raise BranchError(f"This runs only on {config.main_branch}, so that what it releases is what's on GitHub.")
    return Backend(config.backend, config.state_path)


def check_synced_with_origin(config: Config, paths: list[pathlib.Path]) -> None:
    """Refuses to release unless the main branch is what GitHub has: HEAD is origin's `main_branch`, and `paths` (what's
    released) have no changes but what's committed. Nothing's checked without `main_branch`."""
    main = config.main_branch
    if main is None:
        return
    if not fetch(config.root, main):
        raise BranchError(f"Can't fetch {main} from origin, to check this is what GitHub has.")
    head, origin = head_commit(config.root), remote_commit(config.root, main)
    if head is None or head != origin:
        raise BranchError(
            f"{main} here ({(head or 'none')[:7]}) isn't origin's ({(origin or 'none')[:7]}): pull or push so they "
            "match, as releases are cut only from what's on GitHub."
        )
    changed = changed_paths(config.root, paths)
    if changed:
        raise BranchError(f"Commit and push your changes first, as releases are cut only from what's on GitHub: {', '.join(changed)}.")
