"""Extracts the doc comment written above a declaration (skipping any annotations in between)."""

from __future__ import annotations

import re

_ANNOTATION_START = re.compile(r"^\s*annotation\b")


def leading_doc_comment(lines: list[str], declaration_line: int) -> str | None:
    line = _line_before_annotations(lines, declaration_line - 1)
    if line < 0:
        return None
    line = _skip_blank_lines(lines, line)
    if line < 0:
        return None
    trimmed = lines[line].strip()
    if trimmed.startswith("//"):
        return _line_comment(lines, line)
    if trimmed.endswith("*/"):
        return _block_comment(lines, line)
    return None


def _line_before_annotations(lines: list[str], start_line: int) -> int:
    line = _skip_blank_lines(lines, start_line)
    while line >= 0:
        trimmed = lines[line].strip()
        if trimmed.endswith("}"):
            annotation_start = _find_annotation_start(lines, line)
            if annotation_start is None:
                return line
            line = _skip_blank_lines(lines, annotation_start - 1)
            continue
        if trimmed.startswith("annotation"):
            line = _skip_blank_lines(lines, line - 1)
            continue
        return line
    return line


def _find_annotation_start(lines: list[str], end_line: int) -> int | None:
    for line in range(end_line, -1, -1):
        if _ANNOTATION_START.match(lines[line]):
            return line
    return None


def _line_comment(lines: list[str], end_line: int) -> str | None:
    collected = []
    for line in range(end_line, -1, -1):
        trimmed = lines[line].strip()
        if not trimmed.startswith("//"):
            break
        collected.append(re.sub(r"^///?\s?", "", trimmed))
    return _normalize(collected[::-1])


def _block_comment(lines: list[str], end_line: int) -> str | None:
    collected = []
    for line in range(end_line, -1, -1):
        collected.append(lines[line])
        if "/*" in lines[line]:
            break
    if not collected or "/*" not in collected[-1]:
        return None
    normalized = []
    for text in reversed(collected):
        text = re.sub(r"^\s*/\*\*?", "", text)
        text = re.sub(r"\*/\s*$", "", text)
        text = re.sub(r"^\s*\*\s?", "", text)
        normalized.append(text.rstrip())
    return _normalize(normalized)


def _normalize(lines: list[str]) -> str | None:
    text = "\n".join(lines).strip()
    return text or None


def _skip_blank_lines(lines: list[str], start_line: int) -> int:
    line = min(start_line, len(lines) - 1)
    while line >= 0 and lines[line].strip() == "":
        line -= 1
    return line
