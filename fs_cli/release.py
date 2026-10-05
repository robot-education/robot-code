"""Releasing FeatureScripts from the backend document to the public frontend document.

A release:
    1. Creates a version named e.g. "Robot frame - v1.3.0" in the backend document.
    2. Points the frontend document's Feature Studio of the same name at that version, using a
       small generated studio that re-exports the backend studio.
    3. Optionally creates a version in the frontend document too, which publishes the release.

Beta releases go to the separate frontend_beta document instead.
"""

from __future__ import annotations

import dataclasses
from typing import Callable

from fs_cli.remote import Version
from fs_cli.versions import (
    ReleasedVersion,
    VersionType,
    feature_name_for,
    latest_release,
    next_version,
    parse_version_name,
    version_name,
)
from fs_cli.workspace import Status, Studio, UsageError, Workspace
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import ElementPath, InstancePath, path_to_url

RELEASE_TEMPLATE = """FeatureScript {std_version};

/* Automatically generated file -- DO NOT EDIT */

/**
 * {version_name}
 * Source code (public): {source_url}
 * FeatureScript by Alex Kempen.
 */
export import(path : "{import_path}", version : "{microversion_id}");
"""


def release_studio_code(
    version_name: str, std_version: str, studio_path: ElementPath, microversion_id: str
) -> str:
    """Generates a frontend studio which re-exports a released (versioned) backend studio."""
    return RELEASE_TEMPLATE.format(
        std_version=std_version,
        version_name=version_name,
        source_url=path_to_url(studio_path),
        import_path="/".join(
            [studio_path.document_id, studio_path.instance_id, studio_path.element_id]
        ),
        microversion_id=microversion_id,
    )


@dataclasses.dataclass
class ReleasePlan:
    studio: Studio
    feature_name: str
    previous: ReleasedVersion | None
    version_name: str
    target: InstancePath
    target_label: str


def plan_release(
    workspace: Workspace, script: str, bump: VersionType | None, beta: bool
) -> ReleasePlan:
    config, remote = workspace.config, workspace.remote
    target = config.frontend_beta if beta else config.frontend
    target_label = "frontend_beta" if beta else "frontend"
    if target is None:
        raise UsageError(f"Set [tool.fs] {target_label} in pyproject.toml to release.")

    studio = _find_studio(workspace, script)
    if studio.status != Status.IN_SYNC:
        raise UsageError(
            f"{studio.path} is {studio.status.value}; run `fs push` (or `fs status`) before releasing it."
        )
    assert studio.remote

    feature_names = remote.feature_names(workspace.instance, studio.remote.element_id)
    if len(feature_names) != 1:
        found = ", ".join(feature_names) or "none"
        raise UsageError(
            f"{studio.path} must define exactly one custom feature to be released (found {found})."
        )
    is_beta_feature = "beta" in feature_names[0].lower()
    if beta and not is_beta_feature:
        raise UsageError(
            "Beta releases must be of features whose name contains 'beta'."
        )
    if not beta and is_beta_feature:
        raise UsageError(
            "Features whose name contains 'beta' can only be released with --beta."
        )

    feature_name = feature_name_for(studio.remote.name)
    previous = latest_release(feature_name, remote.versions(workspace.instance))
    try:
        version = next_version(previous, bump, beta)
    except ValueError as error:
        raise UsageError(str(error)) from error
    return ReleasePlan(
        studio,
        feature_name,
        previous,
        version_name(feature_name, version),
        target,
        target_label,
    )


def run_release(
    workspace: Workspace,
    plan: ReleasePlan,
    description: str,
    make_version: bool,
    log: Callable[[str], None] = print,
) -> None:
    remote, backend = workspace.remote, workspace.instance
    assert plan.studio.remote
    studio_name = plan.studio.remote.name

    log(f"Creating version {plan.version_name} in the backend document...")
    version = remote.create_version(backend, plan.version_name, description)
    version_instance = InstancePath.from_path(backend, version.id, InstanceType.VERSION)
    released = next(
        (
            studio
            for studio in remote.list_studios(version_instance)
            if studio.element_id == plan.studio.remote.element_id
        ),
        None,
    )
    if released is None:
        raise RuntimeError(
            f"{studio_name} is unexpectedly missing from the new backend version; nothing was published."
        )

    code = release_studio_code(
        plan.version_name,
        remote.latest_std_version(),
        ElementPath.from_path(version_instance, released.element_id),
        released.microversion_id,
    )
    target_studio = next(
        (
            studio
            # Fresh, so a studio created moments ago isn't created again
            for studio in remote.list_studios(plan.target, fresh=True)
            if studio.name == studio_name
        ),
        None,
    )
    if target_studio is None:
        log(f"Creating {studio_name} in the {plan.target_label} document...")
        target_studio = remote.create(plan.target, studio_name)
    log(f"Updating {studio_name} in the {plan.target_label} document...")
    for notice in remote.push(plan.target, target_studio.element_id, code):
        log(f"  {notice}")

    if make_version:
        log(
            f"Creating version {plan.version_name} in the {plan.target_label} document..."
        )
        remote.create_version(plan.target, plan.version_name, description)


def unsynced_versions(workspace: Workspace) -> list[Version]:
    """Returns the release versions in the backend which are missing from the frontend, oldest first."""
    config, remote = workspace.config, workspace.remote
    if config.frontend is None:
        raise UsageError("Set [tool.fs] frontend in pyproject.toml to sync versions.")
    frontend_names = {
        version.name
        for version in remote.versions(config.frontend)
        if parse_version_name(version.name)
    }
    missing = []
    # Walk back from the newest backend version until reaching one already in the frontend
    for version in reversed(remote.versions(workspace.instance)):
        if not parse_version_name(version.name):
            continue
        if version.name in frontend_names:
            break
        missing.append(version)
    return missing[::-1]


def _find_studio(workspace: Workspace, script: str) -> Studio:
    studios = workspace.scan(workspace.resolve_targets([script]))
    if len(studios) != 1:
        names = ", ".join(studio.path for studio in studios)
        raise UsageError(f'"{script}" matches several FeatureScripts: {names}.')
    studio = studios[0]
    if studio.remote is None:
        raise UsageError(
            f"{studio.path} doesn't exist in Onshape yet; run `fs push` first."
        )
    return studio
