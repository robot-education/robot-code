"""Git helpers used to recognize Onshape contents which already exist in source control."""

import pathlib
import subprocess

from fs_cli.state import content_hash, image_hash

MAX_HISTORY = 50


def _git(root: pathlib.Path, *args: str) -> str | None:
    try:
        result = subprocess.run(
            ["git", *args], cwd=root, capture_output=True, text=True, check=True
        )
    except (OSError, subprocess.CalledProcessError):
        return None
    return result.stdout


def committed_hashes(root: pathlib.Path, file: pathlib.Path) -> set[str]:
    """Returns the content hashes of the most recent committed versions of file.

    Returns an empty set if root is not a git repository or file has never been committed.
    """
    relative = file.resolve().relative_to(root.resolve()).as_posix()
    log = _git(root, "log", f"--max-count={MAX_HISTORY}", "--format=%H", "--", relative)
    if not log:
        return set()
    hashes = set()
    for commit in log.split():
        contents = _git(root, "show", f"{commit}:{relative}")
        if contents is not None:
            hashes.add(content_hash(contents))
    return hashes


def committed_contents(root: pathlib.Path, file: pathlib.Path) -> str | None:
    """Returns the contents of file in the last commit, or None if it isn't in it."""
    relative = file.resolve().relative_to(root.resolve()).as_posix()
    return _git(root, "show", f"HEAD:{relative}")


def committed_image_hashes(root: pathlib.Path, file: pathlib.Path) -> set[str]:
    """Returns the hashes (see `image_hash`) of the most recent committed versions of an image."""
    relative = file.resolve().relative_to(root.resolve()).as_posix()
    log = _git(root, "log", f"--max-count={MAX_HISTORY}", "--format=%H", "--", relative)
    if not log:
        return set()
    hashes = set()
    for commit in log.split():
        try:
            result = subprocess.run(
                ["git", "show", f"{commit}:{relative}"], cwd=root, capture_output=True, check=True
            )
        except (OSError, subprocess.CalledProcessError):
            continue
        hashes.add(image_hash(result.stdout))
    return hashes
