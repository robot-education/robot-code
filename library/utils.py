import re
from typing import Iterable
from library import base


def export(export: bool) -> str:
    """Returns "export " if export is true. Otherwise, returns ""."""
    return "export " if export else ""


def definition(definition: str, parameter_name: str) -> str:
    return "{}.{}".format(definition, parameter_name)


def tab_lines(string: str) -> str:
    lines = string.splitlines(keepends=True)
    return "".join(["    " + line for line in lines])


def to_str(
    nodes: Iterable[base.Node | str],
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


def user_name(parameter_name: str) -> str:
    """Converts a camel case parameter name to a user facing name in sentence case.

    Example: myEnum (or MyEnum) -> My enum
    """
    words = re.findall("[a-zA-Z][^A-Z]*", parameter_name)
    words[0] = words[0].capitalize()
    words[1:] = [word.lower() for word in words[1:]]
    return " ".join(words)
