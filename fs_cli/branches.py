"""Which git branch may do what.

Every branch works in the backend's main workspace: `fs push` from any of them writes it. Releasing to a frontend
document runs only on `main_branch` (set in [tool.fs]), and only once it's what GitHub has: HEAD is origin's
`main_branch`, and what's released is committed (`check_synced_with_origin`). Every release first pushes what it
releases over Onshape's, so it's what's in the repo, whatever else the workspace holds. A release to a library document
runs on any branch. See docs/fs-cli.md#branches.
"""

from __future__ import annotations

import os
import pathlib

from fs_cli.config import Config
from fs_cli.git import changed_paths, current_branch, fetch, head_commit, remote_commit

# Names the branch where git can't, e.g. on the detached HEAD a CI checkout may leave
BRANCH_VARIABLE = "FS_BRANCH"


class BranchError(Exception):
    pass


def branch_name(config: Config) -> str | None:
    return os.environ.get(BRANCH_VARIABLE) or current_branch(config.root)


def check_main_branch(config: Config) -> None:
    """Refuses a release to a frontend document off the main branch (when [tool.fs] sets one)."""
    if config.main_branch is None:
        return
    branch = branch_name(config)
    if branch is None:
        raise BranchError(f"fs works on a git branch: check one out, or name it in {BRANCH_VARIABLE}.")
    if branch != config.main_branch:
        raise BranchError(
            f"This runs only on {config.main_branch}, so that what it releases is what's on GitHub (a release to a "
            "library document, `--to DOC`, runs on any branch)."
        )


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
