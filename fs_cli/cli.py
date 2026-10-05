"""The `fs` command: keeps the FeatureScripts in this repo in sync with the backend document, and
releases them to the frontend document.

The repo is the source of truth, so `fs` (or `fs push`) is the everyday command. `fs pull` and
`fs sync` exist for the occasional edit made directly in Onshape.
"""

from __future__ import annotations

import argparse
import collections
import difflib
import os
import pathlib
import sys

from fs_cli.config import Config, ConfigError, load_config
from fs_cli.release import plan_release, run_release, unsynced_versions
from fs_cli.remote import OnshapeRemote, Remote
from fs_cli.state import State, migrate
from fs_cli.std import StdMetadata, pull_from_mirror, pull_from_onshape
from fs_cli.gen import GenerateError, generate
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
from fs_lsp.project import Module, Project
from onshape_api.exceptions import ApiError
from onshape_api.paths.paths import path_to_url

CONFLICT = "changed both locally and in Onshape; see `fs diff`, then `fs pull --force` or `fs push --force`"
DELETE_CONFLICT = "was deleted locally but changed in Onshape; `fs push --force` deletes the tab, or `fs pull --force` restores the file"


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

    def yes_to_deletes(subparser: argparse.ArgumentParser) -> None:
        subparser.add_argument(
            "-y",
            "--yes",
            action="store_true",
            help="don't ask to confirm deleting tabs whose files were deleted",
        )

    push = command(
        "push",
        "push local changes to Onshape, deleting the tabs of deleted files (the default command)",
    )
    push.add_argument(
        "-f", "--force", action="store_true", help="overwrite changes made in Onshape"
    )
    dry_run(push)
    yes_to_deletes(push)

    pull = command("pull", "pull changes made in Onshape into the repo")
    pull.add_argument(
        "-f", "--force", action="store_true", help="overwrite local changes"
    )
    dry_run(pull)

    sync = command(
        "sync", "push local changes and pull Onshape changes, skipping conflicts"
    )
    dry_run(sync)
    yes_to_deletes(sync)

    command("status", "show how the repo differs from Onshape").add_argument(
        "-a", "--all", action="store_true", help="also list studios which are in sync"
    )
    command("diff", "show differences between Onshape and the repo")
    update_std = command(
        "update-std",
        "update FeatureScript versions and std imports to the std version in std/",
    )
    update_std.add_argument(
        "--latest",
        action="store_true",
        help="use the latest std version in Onshape instead (1 API call)",
    )
    update_std.add_argument(
        "-p", "--push", action="store_true", help="push to Onshape after updating"
    )

    pull_std = command(
        "pull-std",
        "update std/ to the latest Onshape std library, and the language server's index of it",
        targets=False,
    )
    pull_std.add_argument(
        "--from-onshape",
        action="store_true",
        help="download changed files from Onshape (1 API call each) instead of the GitHub mirror",
    )
    pull_std.add_argument(
        "-y", "--yes", action="store_true", help="don't ask to confirm API usage"
    )
    dry_run(pull_std)

    command(
        "check",
        "check FeatureScripts for syntax errors, undefined names, and unused or unknown imports (no API calls)",
    )
    command(
        "deps",
        "show what FeatureScripts import, and what imports them (no API calls)",
    )
    unused = command(
        "unused",
        "list exported functions, constants, etc. which nothing uses, or only their own file does (no API calls)",
    )
    unused.add_argument(
        "--local",
        action="store_true",
        help="also list exports only used in their own file (which needn't be exported)",
    )
    refs = command(
        "refs",
        "find where a function, constant, enum, etc. is defined and used (no API calls)",
        targets=False,
    )
    refs.add_argument("name", help="the name to look up, e.g. cleanup")

    gen_command = command(
        "gen",
        "regenerate the .gen.fs files (lookup tables, sketch profiles) from their Python definitions (no API calls)",
        targets=False,
    )
    dry_run(gen_command)

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
    if index == len(argv) or argv[index] not in (
        *COMMANDS,
        *OFFLINE_COMMANDS,
        "-h",
        "--help",
    ):
        argv.insert(index, "push")
    return make_parser().parse_args(argv)


def main(argv: list[str] | None = None, remote: Remote | None = None) -> int:
    args = parse_args(argv)
    try:
        config = load_config()
        migrate(config.state_path, config.studios_path)
        if args.command in OFFLINE_COMMANDS:
            return OFFLINE_COMMANDS[args.command](config, args)
        if remote is None:
            remote = _onshape_remote(args.log)
        state = State.load(config.state_path, config.studios_path)
        try:
            return COMMANDS[args.command](Workspace(config, state, remote), args)
        finally:
            state.save()
    except (ConfigError, UsageError, GenerateError) as error:
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
    overwritable = (
        (Status.REMOTE_CHANGES, Status.CONFLICT, Status.DELETE_CONFLICT)
        if args.force
        else ()
    )
    skipped = report_skipped(
        studios,
        {
            Status.REMOTE_CHANGES: "changed in Onshape since it was last synced; run `fs pull`, or `fs push --force` to overwrite",
            Status.CONFLICT: CONFLICT,
            Status.DELETE_CONFLICT: DELETE_CONFLICT,
        },
        skip=overwritable,
    )
    note(
        studios,
        Status.REMOTE_ONLY,
        "only exists in Onshape; run `fs pull` to add it to the repo",
    )
    to_push = select(studios, *PUSHABLE, *overwritable)
    check_path_imports(workspace, [s for s in to_push if s.local_code is not None])
    to_delete, not_deleted = plan_deletes(
        workspace,
        [studio for studio in to_push if studio.local_code is None]
        + select(studios, Status.DELETED_LOCALLY),
        args.dry_run,
        args.yes,
    )
    to_push = [studio for studio in to_push if studio.local_code is not None]
    apply_import_updates(workspace, studios, args.dry_run)
    pushed = (
        do_push(workspace, to_push, args.dry_run) if to_push or not to_delete else 0
    )
    do_delete(workspace, to_delete, args.dry_run)
    return pushed or not_deleted or skipped


def pull(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    overwritable = (
        (
            Status.LOCAL_CHANGES,
            Status.CONFLICT,
            Status.DELETED_LOCALLY,
            Status.DELETE_CONFLICT,
        )
        if args.force
        else ()
    )
    skipped = report_skipped(
        studios,
        {
            Status.LOCAL_CHANGES: "has local changes which haven't been pushed; run `fs push`, or `fs pull --force` to discard them",
            Status.CONFLICT: CONFLICT,
            Status.DELETE_CONFLICT: DELETE_CONFLICT,
        },
        skip=overwritable,
    )
    if not args.force:
        note(
            studios,
            Status.DELETED_LOCALLY,
            "was deleted locally; run `fs push` to delete the tab, or `fs pull --force` to restore it",
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
    skipped = report_skipped(
        studios,
        {Status.CONFLICT: CONFLICT, Status.DELETE_CONFLICT: DELETE_CONFLICT},
    )
    check_path_imports(workspace, select(studios, *PUSHABLE))
    to_delete, not_deleted = plan_deletes(
        workspace, select(studios, Status.DELETED_LOCALLY), args.dry_run, args.yes
    )
    apply_import_updates(workspace, studios, args.dry_run)
    pulled = do_pull(workspace, select(studios, *PULLABLE), args.dry_run)
    pushed = do_push(workspace, select(studios, *PUSHABLE), args.dry_run)
    do_delete(workspace, to_delete, args.dry_run)
    return pulled or pushed or not_deleted or skipped


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
    if args.latest:
        std_version = workspace.remote.latest_std_version()
    else:
        std_version = StdMetadata.load(workspace.config.std_dir).number
        if std_version is None:
            raise UsageError(
                "std/ has no recorded version; run `fs pull-std` first, or pass --latest."
            )
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
            argparse.Namespace(targets=args.targets, force=False, dry_run=False, yes=False),
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


def pull_std(workspace: Workspace, args: argparse.Namespace) -> int:
    std_dir = workspace.config.std_dir
    before = StdMetadata.load(std_dir).version
    if args.from_onshape:

        def confirm_download(count: int) -> None:
            if not args.yes:
                confirm(
                    f"Downloading {_plural(count, 'file')} takes {count} API calls."
                )

        update = pull_from_onshape(
            std_dir, workspace.remote, args.dry_run, confirm_download
        )
    else:
        update = pull_from_mirror(std_dir, args.dry_run)
    verb = "Would update" if args.dry_run else "Updated"
    if update.changed or update.removed:
        print(
            f"{verb} std/ from {before or 'nothing'} to {update.version}: {len(update.changed)} files changed, {len(update.removed)} removed."
        )
    else:
        print(f"std/ is already up to date ({update.version}).")
    if not args.dry_run:
        from fs_lsp.tools.update_stdlib import regenerate

        print(f"Regenerated the language server's stdlib index: {regenerate(std_dir)}.")
        if update.version != before:
            print(
                f"Run `fs update-std` to move your FeatureScripts to {update.version}, then `fs push`."
            )
    return 0


def gen(config: Config, args: argparse.Namespace) -> int:
    # Imports need a version, which is only known for studios synced on this machine
    synced = {
        entry.file: (element_id, entry.microversion_id)
        for element_id, entry in State.load(config.state_path, config.studios_path).studios.items()
        if entry.microversion_id
    }
    generated = generate(config.code_dir, StdMetadata.load(config.std_dir).number, synced)
    changed = [result for result in generated if result.changed]
    for result in changed:
        path = os.path.relpath(result.output)
        if args.dry_run:
            print(f"Would update {path}")
        else:
            result.output.write_text(result.code, newline="")
            print(f"Updated {path}")
    if not generated:
        print("No definitions found.")
    elif not changed:
        print("Every generated file is up to date.")
    elif not args.dry_run:
        print("Run `fs push` to push them.")
    return 0


def check(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    counts = collections.Counter()
    files = 0
    for module in _select_modules(project, args.targets):
        problems = project.check(module)
        files += 1 if problems else 0
        for problem in problems:
            line, character = module.position(problem.start)
            print(
                f"{_display_path(module.path)}:{line + 1}:{character + 1}: {problem.severity}: {problem.message} [{problem.code}]"
            )
            counts[problem.severity] += 1
    if not counts:
        print("No problems found.")
        return 0
    summary = " and ".join(
        _plural(counts[severity], severity) for severity in ("error", "warning") if counts[severity]
    )
    print(f"{summary} in {_plural(files, 'file')}.")
    return 1


def deps(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    for module in _select_modules(project, args.targets):
        print(module.relative)
        print("  imports:")
        for imported in module.imports:
            if imported.is_std:
                description = imported.path
            elif imported.namespace:
                description = f"{imported.namespace}:: {imported.path} (not a Feature Studio)"
            else:
                target = project.resolve(imported)
                description = target.relative if target else f"{imported.path} (unknown)"
            print(f"    {'export ' if imported.exported else ''}{description}")
        importers = project.importers(module)
        print("  imported by:" + ("" if importers else " nothing"))
        for importer in importers:
            print(f"    {importer.relative}")
    return 0


def unused(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    found = 0
    for usage in project.usages(_select_modules(project, args.targets)):
        if not usage.exported or usage.other_files or usage.named_in_strings:
            continue
        if usage.local_uses and not args.local:
            continue
        line, character = usage.module.position(usage.declaration.token.offset)
        where = f"only used in {usage.module.relative}" if usage.local_uses else "unused"
        print(
            f"{_display_path(usage.module.path)}:{line + 1}:{character + 1}: {usage.declaration.name} ({usage.declaration.kind}) is {where}"
        )
        found += 1
    if not found:
        print("Every export is used.")
    return 1 if found else 0


def refs(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    found = project.references_to_name(args.name)
    if not found:
        print(f"Nothing called {args.name} is declared at the top level of a FeatureScript.")
        return 1
    for module, declaration, references in found:
        line, character = module.position(declaration.token.offset)
        print(
            f"{declaration.name} ({declaration.kind}) defined at {_display_path(module.path)}:{line + 1}:{character + 1}"
        )
        for owner, token in references:
            line, character = owner.position(token.offset)
            print(
                f"  {_display_path(owner.path)}:{line + 1}:{character + 1}: {owner.line_text(line).strip()}"
            )
        if not references:
            print("  (unused)")
    return 0


def _project(config: Config) -> Project:
    return Project(config.root, config.code_dir, config.studios_path)


def _select_modules(project: Project, targets: list[str]) -> list[Module]:
    """The modules matching targets (.fs files, folders, or file names), or every module."""
    modules = project.modules()
    if not targets:
        return modules
    code_dir = project.code_dir.resolve()
    selected: list[Module] = []
    for target in targets:
        path = pathlib.Path(target).resolve()
        if path == code_dir or code_dir in path.parents:
            matched = [
                module
                for module in modules
                if module.path == path or path in module.path.parents
            ]
        else:
            matched = [
                module
                for module in modules
                if target in (module.path.name, module.path.stem)
            ]
        if not matched:
            raise UsageError(f'No FeatureScript matches "{target}".')
        selected.extend(module for module in matched if module not in selected)
    return selected


def _display_path(path: pathlib.Path) -> str:
    return os.path.relpath(path)


# Commands which don't need Onshape
OFFLINE_COMMANDS = {
    "check": check,
    "deps": deps,
    "unused": unused,
    "refs": refs,
    "gen": gen,
}

COMMANDS = {
    "push": push,
    "pull": pull,
    "sync": sync,
    "status": status,
    "diff": diff,
    "update-std": update_std,
    "pull-std": pull_std,
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


def check_path_imports(workspace: Workspace, studios: list[Studio]) -> None:
    problems = workspace.path_import_problems(studios)
    if problems:
        raise UsageError("Can't push:\n  " + "\n  ".join(problems))


def plan_deletes(
    workspace: Workspace, studios: list[Studio], dry_run: bool, yes: bool
) -> tuple[list[Studio], int]:
    """Picks the tabs of deleted files to delete, skipping any another file still imports.

    Lists them and asks for confirmation (unless yes or dry_run), so call this before changing
    anything. Returns the studios to delete, and 1 if any were skipped.
    """
    to_delete = []
    for studio in studios:
        assert studio.remote
        importers = workspace.importers(studio.remote.element_id)
        if importers:
            print(
                f"Skipping deleting {studio.path} in Onshape: it's still imported by {', '.join(importers)}."
            )
        else:
            to_delete.append(studio)
    skipped = 1 if len(to_delete) < len(studios) else 0
    verb = "Would delete" if dry_run else "Will delete"
    for studio in to_delete:
        print(f"{verb} the {studio.name} tab in Onshape ({studio.path} was deleted)")
    if to_delete and not dry_run and not yes:
        confirm(
            f"This deletes {_plural(len(to_delete), 'tab')} in Onshape; Part Studios using their features will break."
        )
    return to_delete, skipped


def do_delete(workspace: Workspace, studios: list[Studio], dry_run: bool) -> None:
    if dry_run or not studios:
        return
    workspace.delete_studios(studios)
    print(f"Deleted {_plural(len(studios), 'tab')}.")


def do_pull(workspace: Workspace, studios: list[Studio], dry_run: bool) -> int:
    if not studios:
        print("Nothing to pull.")
        return 0
    if dry_run:
        for studio in studios:
            print(f"Would pull {studio.path}{'' if studio.located else ' (new)'}")
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
