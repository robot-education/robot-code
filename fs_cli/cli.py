"""The `fs` command: keeps the FeatureScripts in this repo in sync with the backend document, and
releases them to the frontend document.

The repo is the source of truth, so `fs` (or `fs push`) is the everyday command. `fs pull` and
`fs sync` exist for the occasional edit made directly in Onshape. Images (for icons) are synced with
image tabs the same way (see fs_cli/images.py).
"""

from __future__ import annotations

import argparse
import collections
import difflib
import html
import os
import pathlib
import re
import sys

from fs_lsp.formatter import format_source, is_generated
from fs_lsp.project import Module, Project

from fs_cli.config import Config, ConfigError, load_config
from fs_cli.gen import GenerateError, generate
from fs_cli.images import Image, Images
from fs_cli.release import (
    DEPRECATED,
    deprecated_name,
    find_studio,
    plan_deprecate,
    plan_release,
    run_deprecate,
    run_release,
    unsynced_versions,
)
from fs_cli.remote import OnshapeRemote, Remote, file_name_for, is_local
from fs_cli.renames import (
    relative_paths,
    rename_path_imports,
    rename_studio_files,
    renamed,
)
from fs_cli.state import State, StudioState, migrate, state_lock
from fs_cli.std import StdMetadata, pull_from_mirror, pull_from_onshape
from fs_cli.strings import user_strings
from fs_cli.ui import UiError, render_feature, screenshot
from fs_cli.versions import (
    VersionType,
    feature_name_for,
    latest_release,
    parse_version_name,
)
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
from onshape_api.paths.instance_type import InstanceType
from onshape_api.paths.paths import InstancePath, path_to_url

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
                help=".fs files, images, folders, or Feature Studio or image names (default: everything)",
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
    strings_command = command(
        "strings",
        "list the strings a FeatureScript shows users (names, descriptions, errors, ...), with those of what it imports",
        targets=False,
    )
    strings_command.add_argument("script", help="the .fs file or Feature Studio name, e.g. robotShaft")
    changes_command = command(
        "changes",
        "show what changed in a FeatureScript, and the Feature Studios it imports, since its last release",
        targets=False,
    )
    changes_command.add_argument("script", help="the .fs file or Feature Studio name, e.g. robotShaft")
    changes_command.add_argument(
        "--stat", action="store_true", help="only list the files which changed, with how many lines did"
    )
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
    format_command = command(
        "format",
        "format FeatureScripts like std (indentation and spacing; not generated .gen.fs files) (no API calls)",
    )
    format_command.add_argument(
        "--check", action="store_true", help="list the files formatting would change, without changing them"
    )
    eval_command = command(
        "eval",
        "evaluate a FeatureScript expression locally, with the evaluator the FeatureScript tests use (no API calls)",
        targets=False,
    )
    eval_command.add_argument("expression", help='e.g. "getSprocketRadius(0.25 * inch, 16) / inch"')
    eval_command.add_argument(
        "-m",
        "--module",
        default="onshape/std/common.fs",
        help="the file whose names (exported or not) the expression can use, e.g. chain/robotChain.fs (default: std)",
    )
    table_command = command(
        "table",
        "show lookup tables as flat rows, one per option path, with their values, as the evaluator builds them (no API calls)",
    )
    table_command.add_argument("-n", "--name", action="append", default=[], help="only tables whose names contain this; can be repeated")
    table_output = table_command.add_mutually_exclusive_group()
    table_output.add_argument("--md", action="store_true", help="print Markdown tables")
    table_output.add_argument("--html", metavar="FILE", help="write one HTML page of them all, with a filter")
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
        "'frcNutStrip=REV > 3/8 in. > #10-32', or (for arrays) how many items to show; can be repeated",
    )
    ui_command.add_argument(
        "-o", "--output", help="the PNG to write (default: <feature>.png), or - to print the dialog's HTML instead"
    )
    ui_command.add_argument("--html", action="store_true", help="also write the dialog's HTML next to the PNG")
    ui_command.add_argument("--theme", choices=["dark", "light"], default="dark", help="Onshape's theme (default: dark)")

    audit_command = command(
        "audit",
        "write one page to audit a feature: its dialog, which works (choices are pre-rendered), its writeup, and its problems (no API calls)",
        targets=False,
    )
    audit_command.add_argument("file", help="the .fs file defining the feature")
    audit_command.add_argument("--feature", help="the feature, if the file defines several")
    audit_command.add_argument("-o", "--output", help="the HTML to write (default: .fs-audit/<feature>.html)")
    audit_command.add_argument("--theme", choices=["dark", "light"], default="dark", help="the dialog's Onshape theme (default: dark)")
    audit_command.add_argument(
        "--max-states", type=int, default=200, help="how many of the dialog's states to pre-render at most (default: 200)"
    )

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

    cots_command = command(
        "cots",
        "rank COTS parts by how often teams use them, from FRCDesign's libraries, or show one part's configuration usage and part numbers (no Onshape API calls)",
        targets=False,
    )
    cots_command.add_argument("query", nargs="?", default="", help="a regular expression matching parts' names or groups, e.g. 'hex shaft'")
    cots_command.add_argument(
        "-l", "--library", default="frc", help="frc, ftc, or mkcad (FRCDesign's FRC, FTC, and MKCad libraries)"
    )
    cots_command.add_argument("--days", type=int, default=365, help="how many days of usage to count (default: 365)")
    cots_command.add_argument("-n", "--limit", type=int, default=40, help="how many parts to list (default: 40)")
    cots_command.add_argument(
        "-d", "--details", action="store_true", help="also show each part's configuration usage and part numbers"
    )

    step_command = command(
        "step",
        "trim a vendor's STEP file of an extrusion or tube down to the cross section it's read for (see fs_cli/step.py), to keep in the repo",
        targets=False,
    )
    step_command.add_argument("file", help="the STEP file")
    step_command.add_argument("output", help="where to save the trimmed file (may be the same file)")
    step_command.add_argument(
        "--z", type=float, help="the height (in inches) of the face perpendicular to Z to keep (default: the highest)"
    )

    icons_command = command(
        "icons",
        "save a picture of icons side by side, big and as Onshape shows them, on light and dark (no API calls)",
        targets=False,
    )
    icons_command.add_argument("icons", nargs="+", help="the icons' .svg files (e.g. onshape_icons/feature/rib.svg)")
    icons_command.add_argument("-o", "--output", default="icons.png", help="where to save the picture (default: icons.png)")

    gen_command = command(
        "gen",
        "regenerate the .gen.fs files (lookup tables, sketch profiles) and icons from their Python definitions (no API calls)",
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
        "retire a released FeatureScript without breaking documents using it: rename it DEPRECATED, point its frontend studio at a last version, then delete it from the backend",
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
        if args.command in OFFLINE_COMMANDS and args.command not in STATE_COMMANDS and not getattr(args, "detect", False):
            return OFFLINE_COMMANDS[args.command](config, args)
        # Commands which change the state hold its lock, so two at once don't lose each other's changes
        with state_lock(config.studios_path):
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
    targets = workspace.resolve_targets(args.targets)
    studios = workspace.scan(targets)
    images = Images(workspace)
    image_list = images.scan(targets, discover=False)
    everything = [*studios, *image_list]
    overwritable = (
        (Status.REMOTE_CHANGES, Status.CONFLICT, Status.DELETE_CONFLICT)
        if args.force
        else ()
    )
    skipped = report_skipped(
        everything,
        {
            Status.REMOTE_CHANGES: "changed in Onshape since it was last synced; run `fs pull`, or `fs push --force` to overwrite",
            Status.CONFLICT: CONFLICT,
            Status.DELETE_CONFLICT: DELETE_CONFLICT,
        },
        skip=overwritable,
    )
    note(
        everything,
        Status.REMOTE_ONLY,
        "only exists in Onshape; run `fs pull` to add it to the repo",
    )
    to_push = select(studios, *PUSHABLE, *overwritable)
    images_to_push = select(image_list, *PUSHABLE, *overwritable)
    check_path_imports(
        workspace,
        [s for s in to_push if s.local_code is not None],
        [image.path for image in images_to_push if image.local_data is not None],
    )
    to_delete, not_deleted = plan_deletes(
        workspace,
        [item for item in [*to_push, *images_to_push] if not _exists_locally(item)]
        + select(everything, Status.DELETED_LOCALLY),
        args.dry_run,
        args.yes,
        select(everything, Status.LOCAL_ONLY),
    )
    to_push, not_recreated = skip_recreating_released(
        [studio for studio in to_push if studio.local_code is not None]
    )
    images_to_push = [image for image in images_to_push if image.local_data is not None]
    apply_import_updates(workspace, studios, args.dry_run)
    to_push += do_push_images(images, images_to_push, studios, to_push, args.dry_run)
    pushed = (
        do_push(workspace, to_push, args.dry_run, quiet=bool(images_to_push))
        if to_push or not to_delete
        else 0
    )
    do_delete(workspace, to_delete, args.dry_run, images)
    return pushed or not_deleted or not_recreated or skipped


def pull(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    studios = workspace.scan(targets)
    images = Images(workspace)
    image_list = images.scan(targets)
    everything = [*studios, *image_list]
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
        everything,
        {
            Status.LOCAL_CHANGES: "has local changes which haven't been pushed; run `fs push`, or `fs pull --force` to discard them",
            Status.CONFLICT: CONFLICT,
            Status.DELETE_CONFLICT: DELETE_CONFLICT,
        },
        skip=overwritable,
    )
    if not args.force:
        note(
            everything,
            Status.DELETED_LOCALLY,
            "was deleted locally; run `fs push` to delete the tab, or `fs pull --force` to restore it",
        )
    note(
        everything,
        Status.LOCAL_ONLY,
        "doesn't exist in Onshape yet; run `fs push` to create it",
    )
    note(
        everything,
        Status.DELETED_IN_ONSHAPE,
        "was deleted in Onshape; delete the file, or run `fs push` to recreate it",
    )
    to_pull = select(studios, *PULLABLE, *overwritable)
    apply_import_updates(workspace, studios, args.dry_run)
    images_to_pull = select(image_list, *PULLABLE, *overwritable)
    # Studios first, so new images are placed beside the studios importing them
    pulled = do_pull(workspace, to_pull, args.dry_run, quiet=bool(images_to_pull))
    do_pull_images(images, images_to_pull, args.dry_run)
    return pulled or skipped


def sync(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    studios = workspace.scan(targets)
    images = Images(workspace)
    image_list = images.scan(targets)
    everything = [*studios, *image_list]
    skipped = report_skipped(
        everything,
        {Status.CONFLICT: CONFLICT, Status.DELETE_CONFLICT: DELETE_CONFLICT},
    )
    images_to_push = select(image_list, *PUSHABLE)
    check_path_imports(workspace, select(studios, *PUSHABLE), [image.path for image in images_to_push])
    to_delete, not_deleted = plan_deletes(
        workspace,
        select(everything, Status.DELETED_LOCALLY),
        args.dry_run,
        args.yes,
        select(everything, Status.LOCAL_ONLY),
    )
    to_push, not_recreated = skip_recreating_released(select(studios, *PUSHABLE))
    apply_import_updates(workspace, studios, args.dry_run)
    images_to_pull = select(image_list, *PULLABLE)
    pulled = do_pull(workspace, select(studios, *PULLABLE), args.dry_run, quiet=bool(images_to_pull))
    do_pull_images(images, images_to_pull, args.dry_run)
    to_push += do_push_images(images, images_to_push, studios, to_push, args.dry_run)
    pushed = do_push(workspace, to_push, args.dry_run, quiet=bool(images_to_push))
    do_delete(workspace, to_delete, args.dry_run, images)
    return pulled or pushed or not_deleted or not_recreated or skipped


def status(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    studios: list[Studio | Image] = [*workspace.scan(targets), *Images(workspace).scan(targets)]
    studios.sort(key=lambda studio: studio.path)
    config = workspace.config
    print(
        f"{os.path.relpath(config.code_dir)}/ <-> backend document {path_to_url(workspace.instance)}"
    )
    shown = [
        s
        for s in studios
        if args.all or s.status != Status.IN_SYNC or getattr(s, "import_updates", None) or s.renamed_from
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
        if getattr(studio, "import_updates", None):
            details.append("import versions updated in Onshape")
            hint = hint or "fs push or fs pull applies them"
        suffix = f"  ({hint})" if hint else ""
        print(f"  {studio.path:<{width}}  {', '.join(details)}{suffix}")
    return 0


def diff(workspace: Workspace, args: argparse.Namespace) -> int:
    targets = workspace.resolve_targets(args.targets)
    studios = workspace.scan(targets)
    differing_images = [image for image in Images(workspace).scan(targets) if image.status != Status.IN_SYNC]
    for image in differing_images:
        print(f"Image {image.path} differs ({image.status.value})")
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
    if not differing and not differing_images:
        print("No differences.")
    return 0


def imported_modules(project: Project, module: Module) -> list[Module]:
    """A module and every module in the repo it imports, directly or not, in the order they're found."""
    modules = [module]
    for current in modules:
        for imported in current.imports:
            target = None if imported.is_std or imported.namespace else project.resolve(imported)
            if target is not None and target not in modules:
                modules.append(target)
    return modules


def strings(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    selected = _select_modules(project, [args.script])
    if len(selected) != 1:
        raise UsageError(f'"{args.script}" should match one FeatureScript, not {len(selected)}.')
    for module in imported_modules(project, selected[0]):
        found = user_strings(module.path.read_text(encoding="utf-8"))
        if not found:
            continue
        print(module.relative)
        for user_string in found:
            print(f"  {user_string.line:>5}  {user_string.kind:<20}  {user_string.text}")
    return 0


def changes(workspace: Workspace, args: argparse.Namespace) -> int:
    """Diffs a FeatureScript, and each Feature Studio in the repo it imports (directly or not), against their code in
    the backend document's version of its last release. Studios are as released, since `fs release` requires them to
    be pushed and in sync."""
    studio = find_studio(workspace, args.script)
    assert studio.remote is not None
    versions = workspace.remote.versions(workspace.instance)
    released = latest_release(feature_name_for(studio.remote.name), versions)
    if released is None:
        raise UsageError(f"{studio.path} hasn't been released.")
    version = next(version for version in reversed(versions) if version.name == released.name)
    at_release = InstancePath(workspace.instance.document_id, version.id, InstanceType.VERSION)

    project = _project(workspace.config)
    element_ids = {path: element_id for element_id, path in project.element_ids().items()}
    module = project.module(workspace.config.code_dir / studio.path)
    if module is None:
        raise UsageError(f"Couldn't read {studio.path}.")
    modules = imported_modules(project, module)

    print(f"Changes since {released.name}:")
    unchanged = []
    for current in modules:
        element_id = element_ids.get(current.path)
        local = current.path.read_text(encoding="utf-8")
        try:
            old = workspace.remote.pull(at_release, element_id) if element_id else None
        except ApiError:
            old = None
        if old is None:
            print(f"{current.relative}: new since the release")
            continue
        diff = list(
            difflib.unified_diff(
                old.splitlines(keepends=True),
                local.splitlines(keepends=True),
                fromfile=f"released/{current.relative}",
                tofile=f"local/{current.relative}",
            )
        )
        if not diff:
            unchanged.append(current.relative)
        elif args.stat:
            added = sum(1 for line in diff if line.startswith("+") and not line.startswith("+++"))
            removed = sum(1 for line in diff if line.startswith("-") and not line.startswith("---"))
            print(f"{current.relative}: +{added} -{removed}")
        else:
            sys.stdout.writelines(diff)
    if unchanged:
        print("Unchanged: " + ", ".join(unchanged))
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
        elif version.name.startswith(DEPRECATED):
            deprecated.add(version.name.removeprefix(DEPRECATED))
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
        steps.append(f'Rename its feature "{DEPRECATED}...", and push it')
    steps += [
        f"Create version {plan.version_name} in the backend document",
        f"Point {plan.frontend_studio.name} in the {plan.target_label} document at that version",
        f"Rename it {plan.frontend_name}",
    ]
    if args.publish:
        steps.append(f"Create version {plan.version_name} in the {plan.target_label} document")
    if args.keep_backend:
        assert studio.remote
        steps.append(f"Rename its tab in the backend document {deprecated_name(studio.remote.name)}")
    else:
        steps.append(f"Delete its tab in the backend document, and {studio.path}")
    # The API can't rename folders
    steps.append(f"By hand: rename any folders holding its tabs {DEPRECATED}..., so there's no confusion")
    for number, step in enumerate(steps, 1):
        print(f"  {number}. {step}")
    if args.dry_run:
        return 0
    if not args.yes:
        confirm("Backend versions can't be deleted.")
    run_deprecate(workspace, plan, args.description, args.publish)
    print(f"Deprecated {studio.path}. Now rename any folders holding its tabs by hand (step {len(steps)}).")
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


def icons(config: Config, args: argparse.Namespace) -> int:
    from fs_cli.icons import screenshot_preview

    paths = [pathlib.Path(icon) for icon in args.icons]
    missing = [str(path) for path in paths if not path.is_file()]
    if missing:
        print(f"No such icons: {', '.join(missing)}")
        return 1
    screenshot_preview(paths, pathlib.Path(args.output))
    print(f"Saved {args.output}")
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


def format_files(config: Config, args: argparse.Namespace) -> int:
    project = _project(config)
    changed = []
    for module in _select_modules(project, args.targets):
        if is_generated(module.path.name):
            continue
        source = module.path.read_text(encoding="utf-8")
        formatted = format_source(source)
        if formatted == source:
            continue
        changed.append(module)
        if not args.check:
            module.path.write_text(formatted, encoding="utf-8")
        print(_display_path(module.path))
    if args.check:
        print(f"{_plural(len(changed), 'file')} would be formatted." if changed else "Everything is formatted.")
        return 1 if changed else 0
    print(f"Formatted {_plural(len(changed), 'file')}." if changed else "Everything is formatted.")
    return 0


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
        print(f"{file} stays synced with its tab as {renamed(file, renames)}")
    for file in sorted(config.code_dir.rglob("*.fs")):
        code = file.read_text()
        updated = rename_path_imports(code, renames)
        if updated != code:
            file.write_text(updated, newline="")
            print(f"Updated imports of it in {_display_path(file)}")
    return 0


def cots(config: Config, args: argparse.Namespace) -> int:
    from fs_cli.cots import CotsError, FrcDesign, find

    library = FrcDesign(args.library, args.days)
    try:
        parts = find(library.parts(), args.query)[: args.limit]
        if not parts:
            print("No parts match.")
            return 1
        width = max(len(str(part.uses)) for part in parts)
        for rank, part in enumerate(parts, 1):
            vendors = f" [{', '.join(part.vendors)}]" if part.vendors else ""
            print(f"{part.uses:>{width}}  {part.name} ({part.group}){vendors}")
            if args.details:
                for line in library.options(part):
                    print(f"{'':>{width}}    {line}")
                for record in library.records(part):
                    if record.get("partNumber"):
                        options = f" [{record['options']}]" if record["options"] else ""
                        print(f"{'':>{width}}    {record['partNumber']}: {record.get('name')}{options} {record.get('url') or ''}".rstrip())
    except CotsError as error:
        raise UsageError(str(error)) from error
    return 0


def step(config: Config, args: argparse.Namespace) -> int:
    from fs_cli.step import StepError, StepFile

    try:
        trimmed = StepFile(pathlib.Path(args.file)).trimmed(args.z)
    except (OSError, StepError) as error:
        raise UsageError(str(error)) from error
    pathlib.Path(args.output).write_text(trimmed)
    print(f"Saved {args.output} ({len(trimmed) // 1024} KB)")
    return 0


def evaluate(config: Config, args: argparse.Namespace) -> int:
    from fs_eval import Evaluator, FSError, to_display

    try:
        print(to_display(Evaluator(config.root).eval(args.expression, args.module)))
    except FSError as e:
        print(f"Error: {e}", file=sys.stderr)
        return 1
    return 0


def audit(config: Config, args: argparse.Namespace) -> int:
    from fs_cli.audit import audit_page, explore_dialog, local_variants, lookup_tree

    path = pathlib.Path(args.file)
    if not path.is_file():
        raise UsageError(f"{args.file} isn't a file.")
    project = _project(config)
    feature = args.feature

    def render(settings: dict[str, str]) -> str:
        return render_feature(project, config.std_dir, path, feature, settings, args.theme)[0]

    # Choices which change only their own parameter: lookup tables (which conditions can't read), and enums and
    # booleans no condition reads (outside arrays' items, whose labels can show them)
    info: dict = {}
    render_feature(project, config.std_dir, path, feature, {}, args.theme, info=info)
    lookups, values = {}, {}
    for key, parameter in info["parameters"].items():
        if "." in key:
            continue
        if parameter.kind == "lookup" and isinstance(parameter.annotation.get("Lookup Table"), dict):
            tree = lookup_tree(parameter.annotation["Lookup Table"])
            if tree is not None:
                lookups[key] = {"name": parameter.name, "tree": tree}
        elif key not in info["conditions"] and parameter.kind == "boolean":
            values[key] = ["true", "false"]
        elif key not in info["conditions"] and parameter.kind == "enum" and parameter.enum is not None:
            values[key] = list(parameter.enum.values)
    shell, states, truncated = explore_dialog(render, args.max_states, frozenset(lookups) | frozenset(values))
    variants = local_variants(render, states, values)
    title = re.search(r"<span class='ns-dialog-title'>(.*?) 1</span>", shell)
    name = html.unescape(title.group(1)) if title else path.stem
    writeup_path = path.with_suffix(".md")
    writeup = writeup_path.read_text() if writeup_path.is_file() else None
    module = project.module(path.resolve())
    problems = []
    if module is not None:
        for problem in project.check(module):
            line, character = module.position(problem.start)
            problems.append(f"{line + 1}:{character + 1}: {problem.severity}: {problem.message} [{problem.code}]")
    source = os.path.relpath(path.resolve(), config.root)
    tables = _feature_tables(config, project, module) if module is not None else []
    page = audit_page(name, source, shell, states, truncated, writeup, problems, args.theme, tables, variants, lookups)
    output = pathlib.Path(args.output) if args.output else config.root / ".fs-audit" / f"{args.feature or path.stem}.html"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(page)
    more = " (the limit; raise it with --max-states)" if truncated else ""
    print(f"Wrote {_display_path(output)}: {len(states)} dialog states{more}, {len(problems)} problems.")
    return 0


def _feature_tables(config: Config, project: Project, module: Module) -> list:
    """The lookup tables a file's parameters use (theirs, or those of files it imports), in the order they're named."""
    from fs_cli.lookup_tables import find_tables, find_uses, lookup_parameters, source_evaluator
    from fs_eval import Evaluator

    modules = [module]
    seen = {module.path}
    for current in modules:
        for imported in current.imports:
            if imported.is_std or imported.namespace:
                continue
            target = project.resolve(imported)
            if target is not None and target.path not in seen:
                seen.add(target.path)
                modules.append(target)
    names: list[str] = []
    for current in modules:
        for table_name, _ in lookup_parameters(current.path.read_text()):
            if table_name not in names:
                names.append(table_name)
    if not names:
        return []
    evaluator = Evaluator(config.root)
    found = {}
    for current in modules:
        if '"entries"' in current.path.read_text():
            source = source_evaluator(project, config.std_dir, current.path)
            for found_table in find_tables(evaluator, current.path, config.code_dir, source):
                found.setdefault(found_table.name, found_table)
    tables = [found[name] for name in names if name in found]
    find_uses(tables, config.code_dir)
    return tables


def table(config: Config, args: argparse.Namespace) -> int:
    from fs_cli.lookup_tables import find_tables, find_uses, source_evaluator, to_html, to_markdown, to_text
    from fs_eval import Evaluator

    project = _project(config)
    evaluator = Evaluator(config.root)
    tables = []
    for module in _select_modules(project, args.targets):
        # Only files which could hold one, as evaluating every constant everywhere is slow
        if '"entries"' not in module.path.read_text():
            continue
        source = source_evaluator(project, config.std_dir, module.path)
        tables += [
            found
            for found in find_tables(evaluator, module.path, config.code_dir, source)
            if not args.name or any(name.lower() in found.name.lower() for name in args.name)
        ]
    if not tables:
        print("No lookup tables found.")
        return 1
    find_uses(tables, config.code_dir)
    if args.html:
        output = pathlib.Path(args.html)
        output.write_text(to_html(tables))
        print(f"Wrote {len(tables)} tables to {_display_path(output)}")
    elif args.md:
        print(to_markdown(tables))
    else:
        print(to_text(tables))
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
    page, warnings = render_feature(_project(config), config.std_dir, path, args.feature, overrides, args.theme)
    if args.output == "-":
        # For the VS Code extension's preview
        for warning in warnings:
            print(f"Warning: {warning}", file=sys.stderr)
        print(page)
        return 0
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
    with project.snapshot():
        found = project.references_to_name(args.name) or project.std_references_to_name(args.name)
    if not found:
        print(f"Nothing called {args.name} is declared at the top level of a FeatureScript or std.")
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
    return Project(config.root, config.code_dir, config.studios_path, config.std_dir)


def _select_modules(project: Project, targets: list[str]) -> list[Module]:
    """The modules matching targets (.fs files, folders, or file names), or every module. Local files (like
    `example.local.fs`, kept for reference) are only included when they're named."""
    modules = project.modules()
    if not targets:
        return [module for module in modules if not is_local(module.path.name)]
    code_dir = project.code_dir.resolve()
    selected: list[Module] = []
    for target in targets:
        path = pathlib.Path(target).resolve()
        if path == code_dir or code_dir in path.parents:
            matched = [
                module
                for module in modules
                if module.path == path or (path in module.path.parents and not is_local(module.path.name))
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
# Offline commands which change fs-studios.json or .fs-state.json, so hold their lock (see `state_lock`)
STATE_COMMANDS = {"mv", "unlink", "released"}

OFFLINE_COMMANDS = {
    "check": check,
    "format": format_files,
    "mv": mv,
    "ui": ui,
    "eval": evaluate,
    "table": table,
    "audit": audit,
    "deps": deps,
    "strings": strings,
    "unused": unused,
    "refs": refs,
    "gen": gen,
    "icons": icons,
    "cots": cots,
    "step": step,
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
    "changes": changes,
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


def do_push(workspace: Workspace, studios: list[Studio], dry_run: bool, quiet: bool = False) -> int:
    """Pushes studios. quiet leaves out "Nothing to push." (when images were pushed)."""
    if not studios:
        if not quiet:
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


def do_push_images(
    images: Images, to_push: list[Image], studios: list[Studio], pushing: list[Studio], dry_run: bool
) -> list[Studio]:
    """Pushes images, then points their imports at the versions just pushed (and at the new tabs of images whose
    tabs were deleted in Onshape). Returns the studios this changed which weren't being pushed, to push too."""
    if not to_push:
        return []
    verb = "Would push" if dry_run else "Pushing"
    for image in to_push:
        print(f"{verb} {image.path}{' (new)' if image.remote is None else ''}")
    if dry_run:
        return []
    imports = images.push(to_push)
    print(f"Pushed {_plural(len(to_push), 'image')}.")
    # Onshape may keep the old versions until they're pushed (see `Workspace.push_studios`)
    images.workspace.updated_imports.update(element_id for element_id, _ in imports.values())
    scanned = {studio.path: studio for studio in studios}
    extra = []
    for path in images.retarget(imports):
        studio = scanned.get(path)
        if studio is None or studio.local_code is None:
            print(f"Note: updated the imports of images in {path}; run `fs push` on it to push them.")
            continue
        with studio.file.open(newline="") as file:
            studio.local_code = file.read()
        if studio not in pushing and studio.status == Status.IN_SYNC:
            extra.append(studio)
    return extra


def do_pull_images(images: Images, to_pull: list[Image], dry_run: bool) -> None:
    if not to_pull:
        return
    if dry_run:
        for image in to_pull:
            print(f"Would pull {image.path}{'' if image.saved else ' (new)'}")
        return
    images.pull(to_pull)
    for image in to_pull:
        print(f"Pulled {image.path}")
    print(f"Pulled {_plural(len(to_pull), 'image')}.")


def _exists_locally(item: Studio | Image) -> bool:
    return (item.local_code if isinstance(item, Studio) else item.local_data) is not None


def check_path_imports(workspace: Workspace, studios: list[Studio], images: list[str] = []) -> None:
    problems = workspace.path_import_problems(studios, images)
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


def do_delete(
    workspace: Workspace, studios: list[Studio | Image], dry_run: bool, images: Images | None = None
) -> None:
    if dry_run or not studios:
        return
    workspace.delete_studios([studio for studio in studios if isinstance(studio, Studio)])
    if images is not None:
        images.delete([image for image in studios if isinstance(image, Image)])
    print(f"Deleted {_plural(len(studios), 'tab')}.")


def do_pull(workspace: Workspace, studios: list[Studio], dry_run: bool, quiet: bool = False) -> int:
    """Pulls studios. quiet leaves out "Nothing to pull." (when images were pulled)."""
    if not studios:
        if not quiet:
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
