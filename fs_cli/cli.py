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
from fs_cli.release import (
    DEPRECATED_VERSION,
    plan_deprecate,
    plan_release,
    run_deprecate,
    run_release,
    unsynced_versions,
)
from fs_cli.remote import OnshapeRemote, Remote, file_name_for
from fs_cli.renames import relative_paths, rename_path_imports, rename_studio_files, renamed
from fs_cli.state import State, StudioState, migrate
from fs_cli.ui import UiError, render_feature, screenshot
from fs_cli.std import StdMetadata, pull_from_mirror, pull_from_onshape
from fs_cli.gen import GenerateError, generate
from fs_cli.versions import VersionType, feature_name_for, parse_version_name
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

# Released tabs are never deleted or recreated (see `load_released`)
RELEASED_HINTS = {
    Status.DELETED_LOCALLY: "restore the file, or fs deprecate it",
    Status.DELETE_CONFLICT: "fs pull --force restores the file, or fs deprecate it",
    Status.DELETED_IN_ONSHAPE: "restore the tab from the document's history",
}
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

    mv_command = command(
        "mv",
        "rename or move a file or folder in the code folder, keeping it synced with its Feature Studios and updating imports of it by path (no API calls)",
        targets=False,
    )
    mv_command.add_argument("source", help="the file or folder to move (or that was moved, if it's already gone)")
    mv_command.add_argument("destination", help="where to move it, or an existing folder to move it into")

    ui_command = command(
        "ui",
        "render a feature's dialog (roughly as Onshape shows it) to a PNG, with headless Chromium (no API calls)",
        targets=False,
    )
    ui_command.add_argument("file", help="the .fs file defining the feature")
    ui_command.add_argument("--feature", help="the feature to render, if the file defines several")
    ui_command.add_argument(
        "--set",
        action="append",
        default=[],
        metavar="NAME=VALUE",
        help="set a parameter, e.g. placement=POINT, transform=true, or (for lookup tables) "
        "'frcNutStrip=REV > 3/8 in. > #10-32'; can be repeated",
    )
    ui_command.add_argument("-o", "--output", help="the PNG to write (default: <feature>.png)")
    ui_command.add_argument("--html", action="store_true", help="also write the dialog's HTML next to the PNG")

    tabs_command = command(
        "tabs",
        "list tabs in Onshape and files which aren't paired, or whose names differ, to match them up by hand (1 API call)",
    )
    tabs_command.add_argument("-a", "--all", action="store_true", help="also list tabs paired with files of the same name")
    tabs_command.add_argument(
        "--rename",
        action="store_true",
        help="rename tabs to match their files, except released ones (2 API calls each)",
    )
    tabs_command.add_argument("-y", "--yes", action="store_true", help="don't ask to confirm renaming")
    dry_run(tabs_command)

    link_command = command(
        "link",
        "sync a file with a tab, e.g. one fs paired with the wrong file, or a file renamed outside fs (1 API call)",
        targets=False,
    )
    link_command.add_argument("file", help="the .fs file (it needn't exist yet; `fs pull` writes it)")
    link_command.add_argument("tab", help="the tab's name or element id")

    unlink_command = command(
        "unlink",
        "stop syncing files with their tabs, which are left alone in Onshape (no API calls)",
        targets=False,
    )
    unlink_command.add_argument("files", nargs="+", metavar="file", help=".fs files or Feature Studio names")

    released_command = command(
        "released",
        "list, mark, or unmark released FeatureScripts, whose tabs fs never deletes, recreates, or renames (no API calls, except --detect)",
        targets=False,
    )
    released_command.add_argument(
        "files", nargs="*", metavar="file", help=".fs files or Feature Studio names to mark released"
    )
    released_command.add_argument("--remove", action="store_true", help="unmark them instead")
    released_command.add_argument(
        "--detect",
        action="store_true",
        help="mark every tab with a release version in the backend document (2 API calls)",
    )

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

    deprecate_command = command(
        "deprecate",
        "retire a released FeatureScript without breaking documents using it: rename it deprecated, point its frontend studio at a last version, then delete it from the backend",
        targets=False,
    )
    deprecate_command.add_argument("script", help="the .fs file or Feature Studio name, e.g. robotFrame")
    deprecate_command.add_argument(
        "-d", "--description", default="", help="a brief, internal-only description of why"
    )
    deprecate_command.add_argument(
        "--publish", action="store_true", help="also create the version in the frontend document"
    )
    deprecate_command.add_argument(
        "--keep-backend",
        action="store_true",
        help="keep its tab in the backend document (and its file), e.g. if something still imports it",
    )
    deprecate_command.add_argument("-y", "--yes", action="store_true", help="don't ask to confirm")
    dry_run(deprecate_command)

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
        if args.command in OFFLINE_COMMANDS and not getattr(args, "detect", False):
            return OFFLINE_COMMANDS[args.command](config, args)
        if remote is None:
            remote = _onshape_remote(args.log)
        state = State.load(config.state_path, config.studios_path)
        try:
            return COMMANDS[args.command](Workspace(config, state, remote), args)
        finally:
            state.save()
    except (ConfigError, UsageError, GenerateError, UiError) as error:
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
        select(studios, Status.LOCAL_ONLY),
    )
    to_push, not_recreated = skip_recreating_released(
        [studio for studio in to_push if studio.local_code is not None]
    )
    apply_import_updates(workspace, studios, args.dry_run)
    pushed = (
        do_push(workspace, to_push, args.dry_run) if to_push or not to_delete else 0
    )
    do_delete(workspace, to_delete, args.dry_run)
    return pushed or not_deleted or not_recreated or skipped


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
        workspace,
        select(studios, Status.DELETED_LOCALLY),
        args.dry_run,
        args.yes,
        select(studios, Status.LOCAL_ONLY),
    )
    to_push, not_recreated = skip_recreating_released(select(studios, *PUSHABLE))
    apply_import_updates(workspace, studios, args.dry_run)
    pulled = do_pull(workspace, select(studios, *PULLABLE), args.dry_run)
    pushed = do_push(workspace, to_push, args.dry_run)
    do_delete(workspace, to_delete, args.dry_run)
    return pulled or pushed or not_deleted or not_recreated or skipped


def status(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.scan(workspace.resolve_targets(args.targets))
    config = workspace.config
    print(
        f"{os.path.relpath(config.code_dir)}/ <-> backend document {path_to_url(workspace.instance)}"
    )
    shown = [
        s
        for s in studios
        if args.all or s.status != Status.IN_SYNC or s.import_updates or s.renamed_from
    ]
    if not shown:
        print("  everything in sync")
    width = max((len(studio.path) for studio in shown), default=0)
    for studio in shown:
        details = [studio.status.value]
        if studio.renamed_from:
            details.append(f"renamed from {studio.renamed_from}")
        hint = HINTS.get(studio.status)
        if studio.released:
            details.append("released")
            hint = RELEASED_HINTS.get(studio.status, hint)
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


# Matching tabs with files


def tabs(workspace: Workspace, args: argparse.Namespace) -> int:
    studios = workspace.match(workspace.resolve_targets(args.targets))
    paired = [s for s in studios if s.remote and s.local_code is not None]
    renamed = [s for s in paired if tab_name_for(s) != s.name]
    sections = [
        (
            "Tabs only in Onshape (`fs pull` adds them, or `fs link FILE TAB` pairs one with a file):",
            [s for s in studios if s.remote and s.local_code is None and not s.saved],
            lambda s: f"{s.name}  ({s.remote.element_id})",
        ),
        (
            "Tabs whose files were deleted (`fs push` deletes them, or `fs link FILE TAB` pairs one with a file):",
            [s for s in studios if s.remote and s.local_code is None and s.saved],
            lambda s: f"{s.name}  ({s.remote.element_id}, was {s.path})",
        ),
        (
            "Files not in Onshape (`fs push` creates them, or `fs link FILE TAB` pairs one with a tab):",
            [s for s in studios if s.remote is None and not s.deleted_in_onshape],
            lambda s: s.path,
        ),
        (
            "Files whose tabs were deleted in Onshape (`fs push` recreates them):",
            [s for s in studios if s.remote is None and s.deleted_in_onshape],
            lambda s: s.path,
        ),
        (
            "Tabs named differently from their files (`fs tabs --rename` renames the tabs):",
            renamed,
            lambda s: f"{s.path}  (tab {s.name})",
        ),
    ]
    if args.all:
        sections.append(
            ("Tabs paired with files of the same name:", [s for s in paired if s not in renamed], lambda s: s.path)
        )
    shown = False
    for title, listed, describe in sections:
        if not listed:
            continue
        shown = True
        print(title)
        for studio in listed:
            notes = ["released"] if studio.released else []
            if studio.renamed_from:
                notes.append(f"renamed from {studio.renamed_from}")
            print(f"  {describe(studio)}{'  [' + ', '.join(notes) + ']' if notes else ''}")
    if not shown:
        print("Every tab is paired with a file of the same name.")
    if args.rename:
        return rename_tabs(workspace, renamed, args)
    return 0


def tab_name_for(studio: Studio) -> str:
    """The name of a studio's tab which matches its file: the file's name, without .fs unless the tab has it."""
    stem = pathlib.PurePosixPath(studio.path).name.removesuffix(".fs")
    return stem + (".fs" if studio.name.endswith(".fs") else "")


def rename_tabs(workspace: Workspace, studios: list[Studio], args: argparse.Namespace) -> int:
    names = collections.Counter(tab_name_for(studio) for studio in studios)
    taken = {tab.name for tab in workspace.listed_studios}
    to_rename = []
    for studio in studios:
        name = tab_name_for(studio)
        if studio.released:
            print(f"Not renaming {studio.name}: it's released, so `fs release` finds its frontend studio by its name.")
        elif name in taken or names[name] > 1:
            print(f"Not renaming {studio.name}: there'd be several tabs named {name}.")
        else:
            to_rename.append(studio)
    if not to_rename:
        print("No tabs to rename.")
        return 0
    verb = "Would rename" if args.dry_run else "Will rename"
    for studio in to_rename:
        print(f"{verb} the {studio.name} tab to {tab_name_for(studio)}")
    if args.dry_run:
        return 0
    if not args.yes:
        confirm(f"Renaming {_plural(len(to_rename), 'tab')} takes {2 * len(to_rename)} API calls.")
    for studio in to_rename:
        assert studio.remote
        workspace.remote.rename(workspace.instance, studio.remote.element_id, tab_name_for(studio))
    print(f"Renamed {_plural(len(to_rename), 'tab')}.")
    return 0


def link(workspace: Workspace, args: argparse.Namespace) -> int:
    path = _code_path(workspace.config, args.file)
    workspace.match()
    tabs = {tab.element_id: tab for tab in workspace.listed_studios}
    matches = [
        tab
        for tab in tabs.values()
        if args.tab in (tab.element_id, tab.name) or file_name_for(tab.name) == file_name_for(args.tab)
    ]
    if not matches:
        raise UsageError(f'No tab is named "{args.tab}" or has that element id (see `fs tabs`).')
    if len(matches) > 1:
        ids = ", ".join(tab.element_id for tab in matches)
        raise UsageError(f'Several tabs are named "{args.tab}"; give one\'s element id instead: {ids}.')
    [tab] = matches
    studios = workspace.state.studios
    for element_id, entry in list(studios.items()):
        if entry.file == path and element_id != tab.element_id:
            del studios[element_id]
            name = tabs[element_id].name if element_id in tabs else element_id
            print(f"{path} is no longer synced with the {name} tab.")
    previous = studios.get(tab.element_id)
    if previous and previous.file != path:
        print(f"The {tab.name} tab is no longer synced with {previous.file}.")
    if not previous or previous.file != path:
        # Not synced on this machine yet, so its file and tab are compared with the file's git history
        studios[tab.element_id] = StudioState(path)
    exists = (workspace.config.code_dir / path).is_file()
    print(
        f"Synced {path} with the {tab.name} tab"
        + ("; see how they differ with `fs status`." if exists else "; `fs pull` writes the file.")
    )
    return 0


def unlink(config: Config, args: argparse.Namespace) -> int:
    state = State.load(config.state_path, config.studios_path)
    for element_id, path in _synced_studios(config, state, args.files):
        del state.studios[element_id]
        released = " It's still released (see `fs released`)." if element_id in state.released else ""
        print(f"{path} is no longer synced with its tab ({element_id}), which is left alone in Onshape.{released}")
    state.save()
    return 0


def released(config: Config, args: argparse.Namespace) -> int:
    state = State.load(config.state_path, config.studios_path)
    if not args.files:
        files = {element_id: entry.file for element_id, entry in state.studios.items()}
        for element_id in sorted(state.released, key=lambda element_id: files.get(element_id, "")):
            print(files.get(element_id, f"{element_id} (not synced with a file)"))
        if not state.released:
            print("Nothing is released. Mark released FeatureScripts with `fs released FILE` or `fs released --detect`.")
        return 0
    for element_id, path in _synced_studios(config, state, args.files):
        if args.remove:
            state.released.discard(element_id)
            print(f"{path} isn't released.")
        else:
            state.released.add(element_id)
            print(f"{path} is released.")
    state.save()
    return 0


def detect_released(workspace: Workspace, args: argparse.Namespace) -> int:
    """Marks every tab with a release version (but no deprecated version) in the backend document."""
    remote = workspace.remote
    feature_names = set()
    deprecated = set()
    for version in remote.versions(workspace.instance):
        parsed = parse_version_name(version.name)
        if parsed:
            feature_names.add(parsed.feature_name)
        elif version.name.endswith(DEPRECATED_VERSION):
            deprecated.add(version.name.removesuffix(DEPRECATED_VERSION))
    files = {element_id: entry.file for element_id, entry in workspace.state.studios.items()}
    marked = 0
    for tab in remote.list_studios(workspace.instance):
        feature_name = feature_name_for(tab.name)
        if feature_name in feature_names - deprecated and tab.element_id not in workspace.state.released:
            workspace.state.released.add(tab.element_id)
            print(f"{files.get(tab.element_id, tab.name)} is released ({feature_name}).")
            marked += 1
    print(f"Marked {_plural(marked, 'FeatureScript')} released.")
    return 0


def _code_path(config: Config, file: str) -> str:
    [path] = relative_paths(config.code_dir, [pathlib.Path(file)])
    if path is None or not path.endswith(".fs"):
        raise UsageError(f'"{file}" isn\'t a .fs file in {_display_path(config.code_dir)}/.')
    return path


def _synced_studios(config: Config, state: State, args: list[str]) -> list[tuple[str, str]]:
    """The (element id, file) of the studios synced with each of args: files, or names of files."""
    result = []
    for arg in args:
        [path] = relative_paths(config.code_dir, [pathlib.Path(arg)])
        matches = [
            (element_id, entry.file)
            for element_id, entry in state.studios.items()
            if entry.file == path or pathlib.PurePosixPath(entry.file).name in (arg, arg + ".fs")
        ]
        if not matches:
            raise UsageError(f'"{arg}" isn\'t synced with a tab (see `fs tabs`).')
        if len(matches) > 1:
            raise UsageError(f'"{arg}" matches several files: {", ".join(file for _, file in matches)}.')
        result.extend(matches)
    return result


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


def deprecate(workspace: Workspace, args: argparse.Namespace) -> int:
    plan = plan_deprecate(workspace, args.script, args.keep_backend)
    studio = plan.studio
    print(f"Deprecating {studio.path}:")
    steps = []
    if plan.code != studio.local_code:
        steps.append('Rename its feature "... (deprecated)", and push it')
    steps += [
        f"Create version {plan.version_name} in the backend document",
        f"Point {plan.frontend_studio.name} in the {plan.target_label} document at that version",
        f"Rename it {plan.frontend_name}",
    ]
    if args.publish:
        steps.append(f"Create version {plan.version_name} in the {plan.target_label} document")
    if not args.keep_backend:
        steps.append(f"Delete its tab in the backend document, and {studio.path}")
    for number, step in enumerate(steps, 1):
        print(f"  {number}. {step}")
    if args.dry_run:
        return 0
    if not args.yes:
        confirm("Backend versions can't be deleted.")
    run_deprecate(workspace, plan, args.description, args.publish)
    print(f"Deprecated {studio.path}.")
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


def mv(config: Config, args: argparse.Namespace) -> int:
    source, destination = pathlib.Path(args.source), pathlib.Path(args.destination)
    if destination.is_dir() and source.exists() and not source.samefile(destination):
        destination = destination / source.name
    old, new = relative_paths(config.code_dir, [source, destination])
    if old is None or new is None:
        raise UsageError(f"Both paths must be in {_display_path(config.code_dir)}/.")
    if source.exists():
        if destination.exists():
            raise UsageError(f"{args.destination} already exists.")
        destination.parent.mkdir(parents=True, exist_ok=True)
        source.rename(destination)
        print(f"Moved {old} to {new}")
    elif not destination.exists():
        raise UsageError(f"Neither {args.source} nor {args.destination} exists.")
    renames = {old: new}
    for file in rename_studio_files(config.studios_path, renames):
        print(f"{file} stays synced with its Feature Studio as {renamed(file, renames)}")
    for file in sorted(config.code_dir.rglob("*.fs")):
        code = file.read_text()
        updated = rename_path_imports(code, renames)
        if updated != code:
            file.write_text(updated, newline="")
            print(f"Updated imports of it in {_display_path(file)}")
    return 0


def ui(config: Config, args: argparse.Namespace) -> int:
    path = pathlib.Path(args.file)
    if not path.is_file():
        raise UsageError(f"{args.file} isn't a file.")
    overrides = {}
    for setting in args.set:
        name, separator, value = setting.partition("=")
        if not separator:
            raise UsageError(f"--set takes NAME=VALUE, not {setting!r}.")
        overrides[name.strip()] = value.strip()
    page, warnings = render_feature(_project(config), config.std_dir, path, args.feature, overrides)
    for warning in warnings:
        print(f"Warning: {warning}")
    output = pathlib.Path(args.output or (args.feature or path.stem) + ".png")
    if args.html:
        output.with_suffix(".html").write_text(page)
    screenshot(page, output)
    print(f"Saved {output}")
    return 0


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
    "mv": mv,
    "ui": ui,
    "deps": deps,
    "unused": unused,
    "refs": refs,
    "gen": gen,
    "unlink": unlink,
    # Online with --detect
    "released": released,
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
    "deprecate": deprecate,
    "sync-versions": sync_versions,
    "tabs": tabs,
    "link": link,
    "released": detect_released,
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
    workspace: Workspace,
    studios: list[Studio],
    dry_run: bool,
    yes: bool,
    new: list[Studio] = [],
) -> tuple[list[Studio], int]:
    """Picks the tabs of deleted files to delete, skipping any another file still imports.

    Lists them and asks for confirmation (unless yes or dry_run), so call this before changing
    anything. If there are `new` files too, which might be the deleted ones renamed, says how to
    keep the deleted ones' tabs. Returns the studios to delete, and 1 if any were skipped.
    """
    to_delete = []
    for studio in studios:
        assert studio.remote
        importers = workspace.importers(studio.remote.element_id)
        if studio.released:
            print(
                f"Skipping deleting {studio.path} in Onshape: it's released, and Part Studios using its feature only "
                "update to versions of the same tab. Restore the file, or retire the feature with `fs deprecate`."
            )
        elif importers:
            print(
                f"Skipping deleting {studio.path} in Onshape: it's still imported by {', '.join(importers)}."
            )
        else:
            to_delete.append(studio)
    skipped = 1 if len(to_delete) < len(studios) else 0
    verb = "Would delete" if dry_run else "Will delete"
    for studio in to_delete:
        print(f"{verb} the {studio.name} tab in Onshape ({studio.path} was deleted)")
    if to_delete and new:
        print(
            f"If {'it was' if len(to_delete) == 1 else 'any were'} renamed to "
            + ", ".join(studio.path for studio in new)
            + " rather than deleted, run `fs mv OLD NEW` first, so the tab (which documents import by id) is kept."
        )
    if to_delete and not dry_run and not yes:
        confirm(
            f"This deletes {_plural(len(to_delete), 'tab')} in Onshape; Part Studios using their features will break."
        )
    return to_delete, skipped


def skip_recreating_released(studios: list[Studio]) -> tuple[list[Studio], int]:
    """Leaves out released studios whose tabs were deleted in Onshape, since recreating them would make new
    tabs (see `load_released`). Returns the rest, and 1 if any were left out."""
    kept = []
    for studio in studios:
        if studio.released and studio.remote is None:
            print(
                f"Skipping {studio.path}: it's released, but its tab was deleted in Onshape, and Part Studios using "
                "its feature can't update to a new tab. Restore the tab from the document's history, or run "
                "`fs released --remove` on it to make a new one anyway."
            )
        else:
            kept.append(studio)
    return kept, 1 if len(kept) < len(studios) else 0


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
