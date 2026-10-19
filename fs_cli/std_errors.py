"""Std's errors, with their messages: `ErrorStringEnum`'s values, from `std/errorstringenum.gen.fs`, where each
value's English message is the comment above it.

`python -m fs_cli.std_errors WORD...` lists the errors whose message or name has every word (any case), to find the
std error closest to one of ours (see docs/featurescript-style.md's "Model errors on std's").
"""

import pathlib
import re
import sys

ENUM_FILE = pathlib.Path(__file__).resolve().parent.parent / "std" / "errorstringenum.gen.fs"

_ENTRY = re.compile(r"/\*\s*(?P<message>.*?)\s*\*/\s*(?P<name>[A-Z][A-Z0-9_]*)\s*,?", re.S)


def std_errors(source: str | None = None) -> dict[str, str]:
    """Each std error's name, and its message."""
    if source is None:
        source = ENUM_FILE.read_text(encoding="utf-8")
    body = source[source.index("export enum ErrorStringEnum") :]
    body = body[body.index("{") + 1 : body.rindex("}")]
    return {match["name"]: " ".join(match["message"].split()) for match in _ENTRY.finditer(body)}


def search(words: list[str], errors: dict[str, str] | None = None) -> list[tuple[str, str]]:
    """The errors whose message or name has every one of `words`, ignoring case."""
    errors = std_errors() if errors is None else errors
    lowered = [word.lower() for word in words]
    return [(name, message) for name, message in errors.items() if all(word in f"{name} {message}".lower() for word in lowered)]


def main(argv: list[str]) -> int:
    for name, message in search(argv):
        print(f"{name}: {message}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
