"""Fixes for `fs check`'s problems which have one right answer: the language server offers them as quick fixes, and
`fs check --fix` applies them.

- An annotation key Onshape doesn't know, with a close match ("annotation-key"): the match.
- An import nothing is used from ("unused-import"): deleted.
- A name nothing declares ("undefined") which one file in the project exports: an import of that file, by its path
  (which `fs push` resolves to its element id).
"""

from __future__ import annotations

import dataclasses
import re
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from fs_lsp.project import Module, Problem, Project

_SUGGESTION = re.compile(r'Did you mean "([^"]+)"\?')


@dataclasses.dataclass
class Fix:
    title: str
    problem: Problem
    # Replacements in the module's source: (start, end, text)
    edits: list[tuple[int, int, str]]


def quick_fixes(project: Project, module: Module, problems: list[Problem]) -> list[Fix]:
    fixes = []
    source = module.parsed.source
    for problem in problems:
        if problem.code == "annotation-key" and (match := _SUGGESTION.search(problem.message)):
            fixes.append(Fix(f'Change to "{match.group(1)}"', problem, [(problem.start, problem.end, f'"{match.group(1)}"')]))
        elif problem.code == "unused-import":
            end = problem.end
            if source[end : end + 1] == ";":
                end += 1
            # The rest of its line, if nothing else is on it
            rest = re.match(r"[ \t]*(?://[^\n]*)?\n", source[end:])
            start = source.rfind("\n", 0, problem.start) + 1
            if rest and not source[start : problem.start].strip():
                end += rest.end()
            else:
                start = problem.start
            fixes.append(Fix("Remove the unused import", problem, [(start, end, "")]))
        elif problem.code == "undefined":
            name = source[problem.start : problem.end]
            exporters = _exporters(project, module, name)
            if len(exporters) == 1:
                fixes.append(Fix(f"Import {exporters[0]}", problem, [_import_edit(module, exporters[0])]))
    return fixes


def _exporters(project: Project, module: Module, name: str) -> list[str]:
    """The code folder paths of the project's files (other than module, and local ones) which export name."""
    found = []
    for other in project.modules():
        if other.path == module.path or other.relative.startswith("onshape/") or ".local." in other.path.name:
            continue
        if any(other._is_exported(declaration) for declaration in other.top_level.get(name, [])):
            found.append(other.relative)
    return found


def _import_edit(module: Module, relative: str) -> tuple[int, int, str]:
    """An import of a file by its path, on the line after the module's last import (or its FeatureScript line)."""
    source = module.parsed.source
    after = max((imported.end for imported in module.imports), default=None)
    if after is None:
        header = re.match(r"\s*FeatureScript\s+\d+\s*;[^\n]*\n?", source)
        after = header.end() if header else 0
        return (after, after, f'import(path : "{relative}", version : "");\n')
    # The start of the next line, so it doesn't overlap removing that import
    newline = source.find("\n", after)
    after = len(source) if newline == -1 else newline + 1
    return (after, after, f'import(path : "{relative}", version : "");\n')


def apply_fixes(source: str, fixes: list[Fix]) -> tuple[str, int]:
    """Source with fixes applied (skipping any which overlap one applied already), and how many were."""
    applied: list[tuple[int, int, str]] = []
    count = 0
    for fix in fixes:
        if all(edit in applied for edit in fix.edits):
            continue  # The same fix for another problem, like a second use of an undefined name
        if all(end <= other_start or start >= other_end for start, end, _ in fix.edits for other_start, other_end, _ in applied):
            applied.extend(fix.edits)
            count += 1
    for start, end, text in sorted(applied, key=lambda edit: edit[0], reverse=True):
        source = source[:start] + text + source[end:]
    return source, count
