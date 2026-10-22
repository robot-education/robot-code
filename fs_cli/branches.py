"""Each git branch's own workspace in the backend document.

With `main_branch` set in [tool.fs], the backend's main workspace holds that branch's FeatureScript and changes only
by merging (see `fs sync-main`); every other branch pushes to a workspace of its own, named after it. See
docs/fs-cli.md#branches.
"""

from __future__ import annotations

import dataclasses
import enum
import os
import pathlib
from collections.abc import Callable

from fs_cli.config import Config
from fs_cli.git import current_branch
from fs_cli.remote import Remote, RemoteWorkspace
from fs_cli.state import State, load_image_files, load_studio_files
from onshape_api.paths.paths import InstancePath
from onshape_api.types import MergeStrategy

# Names the branch where git can't, e.g. on the detached HEAD a CI checkout may leave
BRANCH_VARIABLE = "FS_BRANCH"


class BranchError(Exception):
    pass


class Access(enum.Enum):
    """What a command does with the backend document."""

    # Reads its branch's workspace, or the main workspace's until it has one
    READ = "read"
    # Writes its branch's workspace, which it creates
    WRITE = "write"
    # Runs only on the main branch, against the main workspace: releasing, and landing main
    MAIN = "main"


@dataclasses.dataclass
class Backend:
    """The backend workspace a command works in, and the state recording what it last synced with it."""

    instance: InstancePath
    state_path: pathlib.Path
    branch: str | None = None
    # The branch's workspace's id, which its state records; None for the main workspace
    workspace_id: str | None = None


def branch_name(config: Config) -> str | None:
    return os.environ.get(BRANCH_VARIABLE) or current_branch(config.root)


def resolve(config: Config, remote: Remote, access: Access, log: Callable[[str], None] = print) -> Backend:
    """The backend workspace for the git branch checked out."""
    main = Backend(config.backend, config.state_path)
    if config.main_branch is None:
        return main
    branch = branch_name(config)
    if branch is None:
        raise BranchError(f"fs works on a git branch: check one out, or name it in {BRANCH_VARIABLE}.")
    if branch == config.main_branch:
        if access is Access.WRITE:
            raise BranchError(
                f"The backend's main workspace holds {branch}, and changes only by merging: push from a branch of "
                f"your own, which gets a workspace of its own, and `fs sync-main` lands {branch} once it's merged."
            )
        return dataclasses.replace(main, branch=branch)
    if access is Access.MAIN:
        raise BranchError(f"This runs only on {config.main_branch}, so that what it releases is what was merged.")
    workspace = find_workspace(config, remote, branch)
    if workspace is None:
        if access is Access.READ:
            log(f"{branch} has no workspace in Onshape yet, so this reads the main workspace; `fs push` makes one.")
            return dataclasses.replace(main, branch=branch)
        workspace = create_workspace(config, remote, branch, log)
        start_state(config, branch, workspace.id)
    instance = InstancePath(config.backend.document_id, workspace.id)
    if access is Access.WRITE:
        catch_up(config, remote, instance, branch, log)
    return Backend(instance, config.state_path_for(branch), branch, workspace.id)


def find_workspace(config: Config, remote: Remote, name: str) -> RemoteWorkspace | None:
    found = [
        workspace
        for workspace in remote.workspaces(config.backend.document_id)
        if workspace.name == name and workspace.id != config.backend.instance_id
    ]
    if len(found) > 1:
        raise BranchError(f"The backend document has {len(found)} workspaces named {name}; delete all but one.")
    return found[0] if found else None


def create_workspace(config: Config, remote: Remote, name: str, log: Callable[[str], None] = print) -> RemoteWorkspace:
    """Branches a workspace from the main workspace's latest version, as Onshape's UI branches only from versions."""
    latest = remote.versions(config.backend)[-1]
    log(f"Creating a workspace for {name}, from version {latest.name}...")
    return remote.create_workspace(config.backend.document_id, latest.id, name)


def start_state(config: Config, branch: str, workspace_id: str) -> None:
    """Starts a new workspace's state as the main workspace's, whose tabs it starts with: a tab whose code is what main's
    state records needs no git history to show it's safe to push over."""
    state = State.load(config.state_path, config.studios_path)
    state.path = config.state_path_for(branch)
    state.workspace = workspace_id
    state.save()


def catch_up(
    config: Config, remote: Remote, instance: InstancePath, branch: str, log: Callable[[str], None] = print
) -> None:
    """Merges the main workspace into a branch's when the branch lacks a tab fs-studios.json tracks which main has,
    so that the branch keeps its id. That's a tab another branch added, which this one has from git since merging it;
    pushing would otherwise make it again, under a new id. Tabs changed in both keep the branch's code, which a push
    then replaces with the files'."""
    tracked = load_studio_files(config.studios_path).keys() | load_image_files(config.studios_path).keys()
    missing = (tracked & remote.element_ids(config.backend)) - remote.element_ids(instance)
    if not missing:
        return
    log(f"Merging the main workspace into {branch}'s, for {len(missing)} tab(s) it lacks...")
    merge(remote, instance, config.backend.instance_id, MergeStrategy.KEEP)


def merge(remote: Remote, target: InstancePath, source_workspace_id: str, strategy: MergeStrategy) -> None:
    try:
        remote.merge(target, source_workspace_id, strategy)
    except ValueError as error:
        raise BranchError(str(error)) from error
