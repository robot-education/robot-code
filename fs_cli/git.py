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


def commit_paths(root: pathlib.Path, paths: list[pathlib.Path], message: str) -> str | None:
    """Commits paths (added, changed, or deleted) as they are now, and nothing else, even what's already staged.

    Returns the commit's short hash, or None if root isn't in a git repository or none of paths changed.
    """
    if _git(root, "rev-parse", "--is-inside-work-tree") is None:
        return None
    relative = sorted({path.resolve().relative_to(root.resolve()).as_posix() for path in paths})
    # git can't add a path which neither exists nor is tracked (a file deleted before it was committed)
    tracked = set((_git(root, "ls-files", "--", *relative) or "").splitlines()) if relative else set()
    relative = [path for path in relative if (root / path).exists() or path in tracked]
    if not relative or _git(root, "add", "--all", "--", *relative) is None:
        return None
    try:
        subprocess.run(["git", "diff", "--cached", "--quiet", "--", *relative], cwd=root, check=True)
        return None
    except subprocess.CalledProcessError:
        pass
    if _git(root, "commit", "--quiet", "--only", "--message", message, "--", *relative) is None:
        raise RuntimeError("git commit failed")
    commit = _git(root, "rev-parse", "--short", "HEAD")
    return commit.strip() if commit else None


def is_repository(root: pathlib.Path) -> bool:
    return _git(root, "rev-parse", "--is-inside-work-tree") is not None


def commit_all(root: pathlib.Path, message: str) -> str | None:
    """Commits every change to the working tree, new files included; the short commit, or None if nothing changed."""
    if _git(root, "add", "--all") is None:
        raise RuntimeError("git add failed")
    if not (_git(root, "diff", "--cached", "--name-only") or "").strip():
        return None
    if _git(root, "commit", "--quiet", "--message", message) is None:
        raise RuntimeError("git commit failed")
    commit = _git(root, "rev-parse", "--short", "HEAD")
    return commit.strip() if commit else None


def current_branch(root: pathlib.Path) -> str | None:
    """The branch checked out; None outside a repository or on a detached HEAD."""
    branch = _git(root, "symbolic-ref", "--quiet", "--short", "HEAD")
    return branch.strip() if branch else None


def head_commit(root: pathlib.Path) -> str | None:
    commit = _git(root, "rev-parse", "HEAD")
    return commit.strip() if commit else None


def is_clean(root: pathlib.Path) -> bool:
    """Whether nothing tracked differs from HEAD."""
    status = _git(root, "status", "--porcelain", "--untracked-files=no")
    return status is not None and not status.strip()


def remote_branches(root: pathlib.Path) -> set[str] | None:
    """The branches on origin; None if it can't be reached."""
    heads = _git(root, "ls-remote", "--heads", "origin")
    if heads is None:
        return None
    return {line.split("refs/heads/", 1)[1] for line in heads.splitlines() if "refs/heads/" in line}


def commit_with_contents(root: pathlib.Path, file: pathlib.Path, contents: str) -> str | None:
    """The newest commit in which file (following it through renames) has contents, as `content_hash` compares them
    (so not counting the versions of same-document imports, which Onshape manages). None if there's none, or root isn't
    in a git repository."""
    relative = file.resolve().relative_to(root.resolve()).as_posix()
    log = _git(root, "log", "--follow", "--format=%x00%H", "--name-only", "--", relative)
    if not log:
        return None
    wanted = content_hash(contents)
    for entry in log.split("\0")[1:]:
        lines = [line for line in entry.splitlines() if line]
        if len(lines) < 2:
            continue
        commit, path = lines[0], lines[-1]
        old = _git(root, "show", f"{commit}:{path}")
        if old is not None and content_hash(old) == wanted:
            return commit
    return None


def has_tag(root: pathlib.Path, name: str) -> bool:
    return _git(root, "rev-parse", "--verify", "--quiet", f"refs/tags/{name}") is not None


def create_tag(root: pathlib.Path, name: str, commit: str, message: str) -> bool:
    """Creates an annotated tag `name` on commit. Whether it worked."""
    return _git(root, "tag", "--annotate", name, commit, "--message", message) is not None


def push_tag(root: pathlib.Path, name: str) -> bool:
    """Pushes tag `name` to origin. Whether it worked."""
    return _git(root, "push", "--quiet", "origin", f"refs/tags/{name}") is not None


def file_at(root: pathlib.Path, revision: str, path: str) -> str | None:
    """A file's contents (its path relative to root) at a revision, or None if it isn't there."""
    return _git(root, "show", f"{revision}:{path}")
