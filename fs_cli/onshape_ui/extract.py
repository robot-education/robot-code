"""Extracts Onshape's feature dialog styles and icons from a saved Onshape page, for `fs ui` (see fs_cli/ui.py).

    uv run --group onshape-ui python -m fs_cli.onshape_ui.extract featurescripts/uiTestBench/uiTestBench.html

The page is a snapshot of Onshape with a feature's dialog open (with every stylesheet inlined); uiTestBench's dialog
shows every kind of parameter. This keeps the style rules which apply to something in the dialog (judged with the
dialog's own elements, ignoring states like :hover), and Onshape's theme variables, and writes them to dialog.css.
It writes the SVG symbols the page defines for its icons to icons.svg.
"""

from __future__ import annotations

import argparse
import pathlib
import re

import soupsieve
import tinycss2
from bs4 import BeautifulSoup

HERE = pathlib.Path(__file__).parent

# Pseudo-classes and elements which depend on state or rendering, removed before matching a selector
_DYNAMIC = re.compile(
    r"::?(?:hover|focus(?:-within|-visible)?|active|visited|link|checked|before|after|placeholder|selection|"
    r"-webkit-[\w-]+|-moz-[\w-]+|-ms-[\w-]+|first-line|first-letter|marker|backdrop|target|indeterminate|"
    r"placeholder-shown|autofill|focus-ring|scrollbar[\w-]*)(?:\([^)]*\))?"
)
# The saved page lost a few shorthand properties (it expanded them into longhands without values); these restore them
REPAIRS = """
/* Repairs of shorthands lost in the saved page (see extract.py) */
.btn {border: var(--bs-btn-border-width) solid var(--bs-btn-border-color)}
label.os-param-checkbox .os-checkbox-indicator {border: 1px solid var(--os-parameter-checkbox-border--idle)}
label.os-param-checkbox input.os-param-checkbox-input:checked ~ .os-checkbox-indicator {background-color: var(--os-parameter-checkbox-fill--checked); background-repeat: no-repeat; background-position: center; background-size: 100%}
"""

# Variables fs ui's own styles use (see PAGE_STYLE in fs_cli/ui.py), for things Onshape's page didn't show, like the
# tooltips of parameters
OWN_VARIABLES = """
var(--os-tooltip-fill) var(--os-tooltip-text) var(--os-tooltip-padding-top) var(--os-tooltip-padding-bottom)
var(--os-padding-sm) var(--os-radius-xs) var(--os-hover-secondary)
"""

# Rules which apply to the whole page or define variables, kept whatever they match
_GLOBAL = re.compile(r"(?:^|[\s,>])(?::root|html|body|\[data-os-theme[^\]]*\])")


# The attributes Angular scopes components' styles with, which `fs ui`'s markup doesn't have
_ANGULAR_SCOPE = re.compile(r"\[_ng(?:content|host)-[\w-]+\]")


def matching_selectors(prelude: str, soup: BeautifulSoup) -> list[str]:
    """The selectors of a rule's prelude which match something in the page, without Angular's scoping."""
    kept = []
    for selector in prelude.split(","):
        selector = selector.strip()
        if not selector:
            continue
        if _GLOBAL.search(selector):
            kept.append(selector)
            continue
        stripped = _DYNAMIC.sub("", selector).strip()
        if not stripped or stripped.endswith((">", "+", "~")):
            stripped = (stripped + " *").strip()
        try:
            if soup.select_one(stripped) is not None:
                kept.append(_ANGULAR_SCOPE.sub("", selector))
        except Exception:
            # A selector soupsieve can't evaluate; it can't be judged, so it's kept
            kept.append(_ANGULAR_SCOPE.sub("", selector))
    return kept


_VARIABLE = re.compile(r"var\(\s*(--[\w-]+)")


class Rule:
    """A kept style rule: its selectors (or an at-rule's prelude and rules) and declarations."""

    def __init__(self, prelude: str, declarations: list | None = None, rules: list[Rule] | None = None) -> None:
        self.prelude = prelude
        self.declarations = declarations or []
        self.rules = rules

    def walk(self):
        yield self
        for rule in self.rules or []:
            yield from rule.walk()

    def text(self, variables: set[str]) -> str:
        if self.rules is not None:
            inner = [text for rule in self.rules if (text := rule.text(variables))]
            return f"{self.prelude} {{\n" + "\n".join(inner) + "\n}" if inner else ""
        kept = [
            f"{declaration.name}: {value}" + (" !important" if declaration.important else "")
            for declaration in self.declarations
            if (value := tinycss2.serialize(declaration.value).strip())
            and (not declaration.name.startswith("--") or declaration.name in variables)
        ]
        return f"{self.prelude} {{" + "; ".join(kept) + "}" if kept else ""


def prune(rules: list, soup: BeautifulSoup) -> list[Rule]:
    output = []
    for rule in rules:
        if rule.type == "qualified-rule":
            selectors = matching_selectors(tinycss2.serialize(rule.prelude), soup)
            if selectors:
                declarations = [
                    declaration
                    for declaration in tinycss2.parse_declaration_list(rule.content, skip_comments=True, skip_whitespace=True)
                    if declaration.type == "declaration"
                ]
                output.append(Rule(", ".join(selectors), declarations))
        elif rule.type == "at-rule" and rule.lower_at_keyword in ("media", "supports") and rule.content is not None:
            inner = prune(tinycss2.parse_rule_list(rule.content, skip_comments=True, skip_whitespace=True), soup)
            if inner:
                output.append(Rule(f"@{rule.at_keyword} {tinycss2.serialize(rule.prelude).strip()}", rules=inner))
        # @font-face (the fonts aren't available), @keyframes, and @import are left out
    return output


def used_variables(rules: list[Rule], extra: str = "") -> set[str]:
    """The custom properties the rules' other declarations (and `extra`, like the icons' markup) use, directly or
    through other custom properties."""
    definitions: dict[str, set[str]] = {}
    pending = _VARIABLE.findall(extra)
    for rule in rules:
        for each in rule.walk():
            for declaration in each.declarations:
                references = set(_VARIABLE.findall(tinycss2.serialize(declaration.value)))
                if declaration.name.startswith("--"):
                    definitions.setdefault(declaration.name, set()).update(references)
                else:
                    pending.extend(references)
    used: set[str] = set()
    while pending:
        name = pending.pop()
        if name not in used:
            used.add(name)
            pending.extend(definitions.get(name, ()))
    return used


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("page", type=pathlib.Path, help="the saved Onshape page")
    args = parser.parse_args()
    text = args.page.read_text(encoding="utf-8")
    soup = BeautifulSoup(text, "html.parser")

    css = "\n".join(style.get_text() for style in soup.find_all("style"))
    rules = tinycss2.parse_stylesheet(css, skip_comments=True, skip_whitespace=True)
    pruned = prune(rules, soup)
    symbols = soup.find_all("symbol")
    variables = used_variables(pruned, "".join(map(str, symbols)) + OWN_VARIABLES)
    kept = [text for rule in pruned if (text := rule.text(variables))]
    header = f"/* Onshape's feature dialog styles, extracted from {args.page.name} by extract.py. Don't edit. */\n"
    (HERE / "dialog.css").write_text(header + "\n".join(kept) + "\n" + REPAIRS)

    sprite = (
        "<svg xmlns='http://www.w3.org/2000/svg' style='display: none'>\n"
        + "\n".join(str(symbol) for symbol in symbols)
        + "\n</svg>\n"
    )
    (HERE / "icons.svg").write_text(sprite)
    print(f"Kept {len(kept)} of {len(rules)} rules ({len(header) + sum(map(len, kept))} bytes) and {len(symbols)} icons.")


if __name__ == "__main__":
    main()
