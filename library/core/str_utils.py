import re
from typing import Iterable, Protocol

__all__ = ["quote"]


def tab_lines(string: str) -> str:
    lines = string.splitlines(keepends=True)
    return "".join(["    " + line for line in lines])


class Str(Protocol):
    def __str__(self) -> str:
        ...


def to_str(
    nodes: Iterable[Str],
    sep: str = "",
    end: str = "",
    tab: bool = False,
) -> str:
    """Converts an iterable of nodes to a tuple of strings.

    sep: The seperator to put in between strings.
    end: A string to append to each node.
    tab: Whether to tab strings over.
    """
    strings = [str(node) + end for node in nodes]
    if tab:
        strings = [tab_lines(string) for string in strings]
    return sep.join(strings)


def quote(string: str) -> str:
    """Adds quotes around string."""
    return '"' + string + '"'


def lower_first(string: str) -> str:
    return string[0].lower() + string[1:]


def camel_case(name: str, capitalize: bool = False) -> str:
    words = name.split(sep="_")
    words = [word.capitalize() for word in words]
    result = "".join(words)
    return result if capitalize else lower_first(result)


def _user_name(words: list[str]) -> str:
    words[0] = words[0].capitalize()
    words[1:] = [word.lower() for word in words[1:]]
    return " ".join(words)


def value_user_name(value: str) -> str:
    return _user_name(value.split(sep="_"))


def user_name(parameter_name: str) -> str:
    """Converts a camel case parameter name (or enum value) to a user facing name in sentence case.

    Examples:
    myEnum -> My enum
    MyValue -> My value
    """
    words = re.findall("[a-zA-Z][^A-Z]*", parameter_name)
    return _user_name(words)
