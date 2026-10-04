"""The `fs` command: keeps the FeatureScripts in this repo and in Onshape in sync.

The repo is the source of truth, so `fs` (or `fs push`) is the everyday command. `fs pull` and
`fs sync` exist for the occasional edit made directly in Onshape.
"""

from __future__ import annotations

import argparse
import difflib
import os
import sys

from fs_cli.config import ConfigError, load_config
from fs_cli.remote import OnshapeRemote, Remote
from fs_cli.state import State
from fs_cli.workspace import (
    HINTS,
    Status,
    Studio,
    UsageError,
    Workspace,
    apply_to_files,
    select,
    update_std_version,
)
from onshape_api.exceptions import ApiError

PUSHABLE = (Status.LOCAL_CHANGES, Status.LOCAL_ONLY)
PULLABLE = (Status.REMOTE_CHANGES, Status.REMOTE_ONLY)


def make_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="fs",
        description="Push FeatureScripts in this repo to Onshape (the default), or pull and sync changes made in Onshape.",
    )
    parser.add_argument(
        "-l", "--log", action="store_true", help="log Onshape API requests"
    )
    subparsers = parser.add_subparsers(dest="command", metavar="command")

    def add_command(name: str, help: str, force_help: str | None = None):
        command = subparsers.add_parser(name, help=help, description=help + ".")
        command.add_argument(
            "targets",
            nargs="*",
            metavar="target",
            help="document names, document folders, or .fs files (default: everything)",
        )
        if force_help:
            command.add_argument("-f", "--force", action="store_true", help=force_help)
            command.add_argument(
                "-n",
                "--dry-run",
                action="store_true",
                help="show what would happen without changing anything",
            )
        return command

    add_command(
        "push",
        "push local changes to Onshape (the default command)",
        "overwrite changes made in Onshape",
    )
    add_command(
        "pull",
        "pull changes made in Onshape into the repo",
        "overwrite local changes",
    )
    add_command(
        "sync",
        "push local changes and pull changes made in Onshape, skipping conflicts",
        None,
    ).add_argument(
        "-n",
        "--dry-run",
        action="store_true",
        help="show what would happen without changing anything",
    )
    add_command("status", "show how the repo differs from Onshape").add_argument(
        "-a", "--all", action="store_true", help="also list studios which are in sync"
    )
    add_command("diff", "show differences between Onshape and the repo")
    add_command(
        "update-std",
        "update FeatureScript versions and std imports to the latest std version",
    ).add_argument(
        "-p", "--push", action="store_true", help="push to Onshape after updating"
    )
    return parser


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    argv = list(sys.argv[1:] if argv is None else argv)
    # Pushing is the default, so `fs`, `fs --force`, and `fs robot` all mean `fs push ...`
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
            from onshape_api.api.key_api import make_key_api

            if args.log:
                os.environ["API_LOGGING"] = "true"
            try:
                remote = OnshapeRemote(make_key_api())
            except KeyError as error:
                # Missing API credentials
                raise ConfigError(error.args[0]) from error
        state = State.load(config.state_path)
        workspace = Workspace(config, state, remote)
        try:
            return COMMANDS[args.command](workspace, args)
        finally:
            state.save()
    except (ConfigError, UsageError) as error:
        print(f"fs: {error}", file=sys.stderr)
        return 2
    except ApiError as error:
        print(f"fs: Onshape request failed: {error}", file=sys.stderr)
        return 1


def push(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    overwritable = (Status.REMOTE_CHANGES, Status.CONFLICT) if args.force else ()
    to_push = select(studios, *PUSHABLE, *overwritable)
    skipped = report_skipped(
        studios,
        {
            Status.REMOTE_CHANGES: "changed in Onshape since it was last synced; run `fs pull`, or `fs push --force` to overwrite",
            Status.CONFLICT: "changed both locally and in Onshape; see `fs diff`, then `fs pull --force` or `fs push --force`",
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
        "deleted locally but not in Onshape; delete the tab in Onshape or run `fs pull --force` to restore it",
    )
    return do_push(workspace, to_push, args.dry_run) or skipped


def pull(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    overwritable = (
        (Status.LOCAL_CHANGES, Status.CONFLICT, Status.DELETED_LOCALLY)
        if args.force
        else ()
    )
    to_pull = select(studios, *PULLABLE, *overwritable)
    skipped = report_skipped(
        studios,
        {
            Status.LOCAL_CHANGES: "has local changes which haven't been pushed; run `fs push`, or `fs pull --force` to discard them",
            Status.CONFLICT: "changed both locally and in Onshape; see `fs diff`, then `fs pull --force` or `fs push --force`",
        },
        skip=overwritable,
    )
    note(
        studios,
        Status.LOCAL_ONLY,
        "doesn't exist in Onshape yet; run `fs push` to create it",
    )
    return do_pull(workspace, to_pull, args.dry_run) or skipped


def sync(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    skipped = report_skipped(
        studios,
        {
            Status.CONFLICT: "changed both locally and in Onshape; see `fs diff`, then `fs pull --force` or `fs push --force`"
        },
    )
    note(
        studios,
        Status.DELETED_LOCALLY,
        "deleted locally but not in Onshape; delete the tab in Onshape or run `fs pull --force` to restore it",
    )
    pulled = do_pull(workspace, select(studios, *PULLABLE), args.dry_run)
    pushed = do_push(workspace, select(studios, *PUSHABLE), args.dry_run)
    return pulled or pushed or skipped


def status(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    studios = workspace.scan(targets)
    for doc in targets.documents:
        doc_studios = [
            studio
            for studio in studios
            if studio.document is doc and (args.all or studio.status != Status.IN_SYNC)
        ]
        relative = os.path.relpath(doc.path)
        print(f"{doc.name} ({relative}) -> {doc.url}")
        if not doc_studios:
            print("  everything in sync")
        width = max((len(studio.file.name) for studio in doc_studios), default=0)
        for studio in sorted(doc_studios, key=lambda studio: studio.file.name):
            hint = HINTS.get(studio.status)
            suffix = f"  ({hint})" if hint else ""
            print(f"  {studio.file.name:<{width}}  {studio.status.value}{suffix}")
    return 0


def diff(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    differing = [studio for studio in studios if studio.status != Status.IN_SYNC]
    for studio in differing:
        remote_code = workspace.fetch(studio) if studio.remote else ""
        local_code = studio.local_code or ""
        sys.stdout.writelines(
            difflib.unified_diff(
                remote_code.splitlines(keepends=True),
                local_code.splitlines(keepends=True),
                fromfile=f"onshape/{studio.label}",
                tofile=f"local/{studio.label}",
            )
        )
    if not differing:
        print("No differences.")
    return 0


def update_std(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    std_version = workspace.remote.latest_std_version()
    studios = workspace.scan(targets)
    changed = apply_to_files(
        studios, lambda code: update_std_version(code, std_version)
    )
    for studio in changed:
        print(f"Updated {studio.label} to FeatureScript {std_version}")
    if not changed:
        print(f"Everything already uses FeatureScript {std_version}.")
    if args.push:
        # Rescan so the updated files are classified as local changes
        return push(
            workspace,
            argparse.Namespace(targets=args.targets, force=False, dry_run=False),
        )
    return 0


COMMANDS = {
    "push": push,
    "pull": pull,
    "sync": sync,
    "status": status,
    "diff": diff,
    "update-std": update_std,
}


def do_push(workspace: Workspace, studios: list[Studio], dry_run: bool) -> int:
    if not studios:
        print("Nothing to push.")
        return 0
    verb = "Would push" if dry_run else "Pushing"
    for studio in studios:
        action = "create" if studio.remote is None else "update"
        print(f"{verb} {studio.label} ({action})")
    if dry_run:
        return 0
    notices = workspace.push_studios(studios)
    errors = False
    for label, messages in notices.items():
        for message in messages:
            errors = errors or message.startswith("error")
            print(f"  {label}: {message}")
    print(f"Pushed {len(studios)} Feature Studio{'s' if len(studios) != 1 else ''}.")
    return 1 if errors else 0


def do_pull(workspace: Workspace, studios: list[Studio], dry_run: bool) -> int:
    if not studios:
        print("Nothing to pull.")
        return 0
    verb = "Would pull" if dry_run else "Pulling"
    for studio in studios:
        print(f"{verb} {studio.label}")
    if not dry_run:
        workspace.pull_studios(studios)
        print(
            f"Pulled {len(studios)} Feature Studio{'s' if len(studios) != 1 else ''}."
        )
    return 0


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
        print(f"Skipping {studio.label}: {reasons[studio.status]}.")
    return 1 if skipped else 0


def note(studios: list[Studio], status: Status, message: str) -> None:
    for studio in select(studios, status):
        print(f"Note: {studio.label} {message}.")


def run() -> None:
    sys.exit(main())


if __name__ == "__main__":
    run()
