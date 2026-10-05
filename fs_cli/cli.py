"""The `fs` command: keeps the FeatureScripts in this repo in sync with the backend document, and
releases them to the frontend document.

The repo is the source of truth, so `fs` (or `fs push`) is the everyday command. `fs pull` and
`fs sync` exist for the occasional edit made directly in Onshape.
"""

from __future__ import annotations

import argparse
import difflib
import os
import sys

from fs_cli.config import ConfigError, load_config
from fs_cli.release import plan_release, run_release, unsynced_versions
from fs_cli.remote import OnshapeRemote, Remote
from fs_cli.state import State
from fs_cli.versions import VersionType
from fs_cli.workspace import (
    HINTS,
    PULLABLE,
    PUSHABLE,
    Status,
    Studio,
    UsageError,
    Workspace,
    apply_to_files,
    select,
    update_std_version,
)
from onshape_api.exceptions import ApiError
from onshape_api.paths.paths import path_to_url

CONFLICT = "changed both locally and in Onshape; see `fs diff`, then `fs pull --force` or `fs push --force`"


def make_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="fs",
        description="Push the FeatureScripts in this repo to the backend document in Onshape (the default command), pull changes made in Onshape, and release FeatureScripts to the frontend document.",
    )
    parser.add_argument(
        "-l", "--log", action="store_true", help="log Onshape API requests"
    )
    commands = parser.add_subparsers(dest="command", metavar="command")

    def command(name: str, help: str, targets: bool = True) -> argparse.ArgumentParser:
        subparser = commands.add_parser(name, help=help, description=help + ".")
        if targets:
            subparser.add_argument(
                "targets",
                nargs="*",
                metavar="target",
                help=".fs files, folders, or Feature Studio names (default: everything)",
            )
        return subparser

    def dry_run(subparser: argparse.ArgumentParser) -> None:
        subparser.add_argument(
            "-n",
            "--dry-run",
            action="store_true",
            help="show what would happen without changing anything",
        )

    push = command("push", "push local changes to Onshape (the default command)")
    push.add_argument(
        "-f", "--force", action="store_true", help="overwrite changes made in Onshape"
    )
    dry_run(push)

    pull = command("pull", "pull changes made in Onshape into the repo")
    pull.add_argument(
        "-f", "--force", action="store_true", help="overwrite local changes"
    )
    dry_run(pull)

    dry_run(
        command(
            "sync", "push local changes and pull Onshape changes, skipping conflicts"
        )
    )

    command("status", "show how the repo differs from Onshape").add_argument(
        "-a", "--all", action="store_true", help="also list studios which are in sync"
    )
    command("diff", "show differences between Onshape and the repo")
    command(
        "update-std",
        "update FeatureScript versions and std imports to the latest std version",
    ).add_argument(
        "-p", "--push", action="store_true", help="push to Onshape after updating"
    )

    release = command(
        "release", "release a FeatureScript to the frontend document", targets=False
    )
    release.add_argument(
        "script", help="the .fs file or Feature Studio name, e.g. robotFrame"
    )
    bump = release.add_mutually_exclusive_group()
    for version_type in VersionType:
        bump.add_argument(
            f"--{version_type}",
            dest="bump",
            action="store_const",
            const=version_type,
            help=f"release a new {version_type} version",
        )
    release.add_argument(
        "-b",
        "--beta",
        action="store_true",
        help="make a beta release to the frontend_beta document",
    )
    release.add_argument(
        "-d",
        "--description",
        default="",
        help="a brief, internal-only description of the changes",
    )
    release.add_argument(
        "--publish",
        action="store_true",
        help="also create the version in the frontend document, publishing the release immediately",
    )
    release.add_argument(
        "-y", "--yes", action="store_true", help="don't ask to confirm"
    )
    dry_run(release)

    sync_versions = command(
        "sync-versions",
        "create release versions which are in the backend document but missing from the frontend document",
        targets=False,
    )
    sync_versions.add_argument(
        "-y", "--yes", action="store_true", help="don't ask to confirm"
    )
    dry_run(sync_versions)
    return parser


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    argv = list(sys.argv[1:] if argv is None else argv)
    # Pushing is the default, so `fs`, `fs --force`, and `fs robotFrame.fs` all mean `fs push ...`
    index = 0
    while index < len(argv) and argv[index] in ("-l", "--log"):
        index += 1
    if index == len(argv) or argv[index] not in (*COMMANDS, "-h", "--help"):
        argv.insert(index, "push")
    return make_parser().parse_args(argv)


def main(argv: list[str] | None = None, remote: Remote | None = None) -> int:
    args = parse_args(argv)
    try:
        config = load_config()
        if remote is None:
            remote = _onshape_remote(args.log)
        state = State.load(config.state_path)
        try:
            return COMMANDS[args.command](Workspace(config, state, remote), args)
        finally:
            state.save()
    except (ConfigError, UsageError) as error:
        print(f"fs: {error}", file=sys.stderr)
        return 2
    except ApiError as error:
        print(f"fs: Onshape request failed: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("fs: interrupted", file=sys.stderr)
        return 130


def _onshape_remote(log: bool) -> OnshapeRemote:
    from onshape_api.api.key_api import make_key_api

    if log:
        os.environ["API_LOGGING"] = "true"
    try:
        return OnshapeRemote(make_key_api())
    except KeyError as error:
        # Missing API credentials
        raise ConfigError(error.args[0]) from error


# Syncing


def push(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    overwritable = (Status.REMOTE_CHANGES, Status.CONFLICT) if args.force else ()
    skipped = report_skipped(
        studios,
        {
            Status.REMOTE_CHANGES: "changed in Onshape since it was last synced; run `fs pull`, or `fs push --force` to overwrite",
            Status.CONFLICT: CONFLICT,
        },
        skip=overwritable,
    )
    note(
        studios,
        Status.REMOTE_ONLY,
        "only exists in Onshape; run `fs pull` to add it to the repo",
    )
    note(
        studios,
        Status.DELETED_LOCALLY,
        "was deleted locally but not in Onshape; delete the tab in Onshape, or run `fs pull --force` to restore it",
    )
    apply_import_updates(workspace, studios, args.dry_run)
    return (
        do_push(workspace, select(studios, *PUSHABLE, *overwritable), args.dry_run)
        or skipped
    )


def pull(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    overwritable = (
        (Status.LOCAL_CHANGES, Status.CONFLICT, Status.DELETED_LOCALLY)
        if args.force
        else ()
    )
    skipped = report_skipped(
        studios,
        {
            Status.LOCAL_CHANGES: "has local changes which haven't been pushed; run `fs push`, or `fs pull --force` to discard them",
            Status.CONFLICT: CONFLICT,
        },
        skip=overwritable,
    )
    note(
        studios,
        Status.LOCAL_ONLY,
        "doesn't exist in Onshape yet; run `fs push` to create it",
    )
    note(
        studios,
        Status.DELETED_IN_ONSHAPE,
        "was deleted in Onshape; delete the file, or run `fs push` to recreate it",
    )
    to_pull = select(studios, *PULLABLE, *overwritable)
    apply_import_updates(workspace, studios, args.dry_run)
    return do_pull(workspace, to_pull, args.dry_run) or skipped


def sync(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    skipped = report_skipped(studios, {Status.CONFLICT: CONFLICT})
    note(
        studios,
        Status.DELETED_LOCALLY,
        "was deleted locally but not in Onshape; delete the tab in Onshape, or run `fs pull --force` to restore it",
    )
    apply_import_updates(workspace, studios, args.dry_run)
    pulled = do_pull(workspace, select(studios, *PULLABLE), args.dry_run)
    pushed = do_push(workspace, select(studios, *PUSHABLE), args.dry_run)
    return pulled or pushed or skipped


def status(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    config = workspace.config
    print(
        f"{os.path.relpath(config.code_dir)}/ <-> backend document {path_to_url(workspace.instance)}"
    )
    shown = [
        s for s in studios if args.all or s.status != Status.IN_SYNC or s.import_updates
    ]
    if not shown:
        print("  everything in sync")
    width = max((len(studio.path) for studio in shown), default=0)
    for studio in shown:
        details = [studio.status.value]
        hint = HINTS.get(studio.status)
        if studio.import_updates:
            details.append("import versions updated in Onshape")
            hint = hint or "fs push or fs pull applies them"
        suffix = f"  ({hint})" if hint else ""
        print(f"  {studio.path:<{width}}  {', '.join(details)}{suffix}")
    return 0


def diff(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    differing = [studio for studio in studios if studio.status != Status.IN_SYNC]
    for studio in differing:
        remote_code = workspace.fetch(studio) if studio.remote else ""
        sys.stdout.writelines(
            difflib.unified_diff(
                remote_code.splitlines(keepends=True),
                (studio.local_code or "").splitlines(keepends=True),
                fromfile=f"onshape/{studio.path}",
                tofile=f"local/{studio.path}",
            )
        )
    if not differing:
        print("No differences.")
    return 0


def update_std(workspace: Workspace, args: argparse.Namespace) -> int:
    std_version = workspace.remote.latest_std_version()
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    changed = apply_to_files(
        studios, lambda code: update_std_version(code, std_version)
    )
    for studio in changed:
        print(f"Updated {studio.path} to FeatureScript {std_version}")
    if not changed:
        print(f"Everything already uses FeatureScript {std_version}.")
    if args.push:
        # Rescan so the updated files are classified as local changes
        return push(
            workspace,
            argparse.Namespace(targets=args.targets, force=False, dry_run=False),
        )
    return 0


# Releasing


def release(workspace: Workspace, args: argparse.Namespace) -> int:
    plan = plan_release(workspace, args.script, args.bump, args.beta)
    previous = plan.previous.name if plan.previous else "none"
    print(
        f"Releasing {plan.studio.path} as {plan.version_name} (previous release: {previous})"
    )
    print(f"  1. Create version {plan.version_name} in the backend document")
    print(
        f"  2. Point {plan.studio.name} in the {plan.target_label} document at that version"
    )
    if args.publish:
        print(
            f"  3. Create version {plan.version_name} in the {plan.target_label} document"
        )
    if args.dry_run:
        return 0
    if not args.yes:
        confirm("Backend versions can't be deleted.")
    run_release(workspace, plan, args.description, args.publish)
    print(f"Released {plan.version_name}.")
    return 0


def sync_versions(workspace: Workspace, args: argparse.Namespace) -> int:
    missing = unsynced_versions(workspace)
    if not missing:
        print("The frontend document already has every release version.")
        return 0
    print("Versions missing from the frontend document:")
    for version in missing:
        description = f": {version.description}" if version.description else ""
        print(f"  {version.name}{description}")
    if args.dry_run:
        return 0
    if not args.yes:
        confirm("Versions can't be deleted.")
    assert workspace.config.frontend
    for version in missing:
        workspace.remote.create_version(
            workspace.config.frontend, version.name, version.description
        )
    print(f"Created {len(missing)} version{'s' if len(missing) != 1 else ''}.")
    return 0


COMMANDS = {
    "push": push,
    "pull": pull,
    "sync": sync,
    "status": status,
    "diff": diff,
    "update-std": update_std,
    "release": release,
    "sync-versions": sync_versions,
}


# Helpers


def do_push(workspace: Workspace, studios: list[Studio], dry_run: bool) -> int:
    if not studios:
        print("Nothing to push.")
        return 0
    verb = "Would push" if dry_run else "Pushing"
    for studio in studios:
        print(f"{verb} {studio.path}{' (new)' if studio.remote is None else ''}")
    if dry_run:
        return 0
    workspace.push_studios(studios)
    print(f"Pushed {_plural(len(studios), 'Feature Studio')}.")
    return 0


def do_pull(workspace: Workspace, studios: list[Studio], dry_run: bool) -> int:
    if not studios:
        print("Nothing to pull.")
        return 0
    if dry_run:
        for studio in studios:
            print(f"Would pull {studio.path}")
        return 0
    workspace.pull_studios(studios)
    for studio in studios:
        print(f"Pulled {studio.path}")
    print(f"Pulled {_plural(len(studios), 'Feature Studio')}.")
    return 0


def apply_import_updates(
    workspace: Workspace, studios: list[Studio], dry_run: bool
) -> None:
    if dry_run:
        for studio in studios:
            if studio.import_updates:
                print(f"Would update import versions in {studio.path}")
        return
    for studio in workspace.apply_import_updates(studios):
        print(f"Updated import versions in {studio.path} to match Onshape")


def report_skipped(
    studios: list[Studio], reasons: dict[Status, str], skip: tuple[Status, ...] = ()
) -> int:
    """Reports studios which were skipped. Returns 1 if anything was skipped."""
    skipped = [
        studio
        for studio in studios
        if studio.status in reasons and studio.status not in skip
    ]
    for studio in skipped:
        print(f"Skipping {studio.path}: {reasons[studio.status]}.")
    return 1 if skipped else 0


def note(studios: list[Studio], status: Status, message: str) -> None:
    for studio in select(studios, status):
        print(f"Note: {studio.path} {message}.")


def confirm(warning: str) -> None:
    if not sys.stdin.isatty():
        raise UsageError("Refusing to continue without confirmation; pass --yes.")
    answer = input(f"{warning} Continue? [y/N] ").strip().lower()
    if answer != "y":
        raise UsageError("Aborted.")


def _plural(count: int, noun: str) -> str:
    return f"{count} {noun}{'s' if count != 1 else ''}"


def run() -> None:
    sys.exit(main())


if __name__ == "__main__":
    run()
