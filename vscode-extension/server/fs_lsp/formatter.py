"""Formats FeatureScript source, in the style of Onshape's std library.

Line breaks are kept as written. What's fixed is the structure:

- Blocks are indented 4 spaces past the line with their `{`, and their `}` lines up with it. A block's `{` on a line
  of its own (Allman style, as std's always are) lines up with its header: the line its `if (...)`, `for (...)`,
  function signature, `else`, `precondition`, or the like starts on.
- Lines continuing a statement (inside its parentheses, a map or array, or a long expression) keep their indentation
  relative to the statement's first line, as written, since std indents them several ways (its calls with a map
  argument are indented both +12/+8 and +8/+4, for example). Blocks inside them, like a function literal's body, are
  still indented as blocks.
- A comment on a line of its own is indented like the code it's in; a block comment's other lines move with its first.
- Within a line, spacing follows the rules std keeps (at least 98% of the time in its hand-written files; see
  `_gap`): one space after commas and none before commas and semicolons, none just inside parentheses and brackets,
  none between a function's name and its `(`, one between `if`, `for`, `while`, `catch`, or `switch` and its `(`, one
  around assignment, comparison, and logical operators and map colons, and one just inside a map's braces
  (`{ "Name" : "Width" }`) and before a `{` (except after `(` or `[`). Other runs of spaces become one, but space
  before a trailing comment, or lining up operators or colons in columns, is kept. Arithmetic operators (where `-` may
  be unary) and `->` are left as written.
- Trailing whitespace is removed, tabs in indentation become 4 spaces, runs of more than `MAX_BLANK_LINES` blank lines
  are shortened, and the file ends with one newline.

Formatting std changes almost nothing (see the tests), and formatting is idempotent.
"""

from __future__ import annotations

import dataclasses

from fs_lsp.scanner import _TOKEN_PATTERN

INDENT = 4
MAX_BLANK_LINES = 2

# Operators with one space either side
_SPACED_OPERATORS = frozenset(
    ["=", "==", "!=", "<=", ">=", "&&", "||", "?", "+=", "-=", "*=", "/=", "^=", "%=", "~=", "??", "??=", "||=", "&&="]
)
# Keywords followed by a space before their `(`
_SPACED_KEYWORDS = frozenset(["if", "for", "while", "catch", "switch"])
# Words which aren't function names before a `(`
_NOT_CALLS = frozenset(["if", "for", "while", "catch", "switch", "return", "in", "is", "as", "throw", "function", "else",
                        "precondition", "annotation", "typecheck", "typeconvert", "returns", "new", "case", "silent",
                        "try"])

# What can come before a `{` which opens a map (rather than a block)
_LITERAL_BEFORE = frozenset(["=", "(", ",", ":", "[", "?", "return", "annotation", "throw", "{", "=>"])
# Operators: a `{` after one is a map too
_OPERATOR_KINDS = frozenset(["operator"])


@dataclasses.dataclass
class _Token:
    kind: str
    value: str
    start: int
    end: int
    line: int


@dataclasses.dataclass
class _Statement:
    """The statement being formatted: its first line's indentation as written and as formatted."""

    written: int
    formatted: int
    # Whether it's still going (so the next line continues it)
    open: bool = False
    # Unclosed parentheses, brackets, and map braces in it (in the current block)
    depth: int = 0
    # Whether it's an annotation (whose map ends it)
    annotation: bool = False
    # Whether it has a function literal (like `const f = function(x)`), whose blocks keep their place as written
    function_literal: bool = False


@dataclasses.dataclass
class _Block:
    """An open block: the indentation of its contents, of its `}`, and the statement it's inside (to resume after)."""

    contents: int
    close: int
    outer: _Statement


def _lex(source: str) -> list[_Token]:
    tokens = []
    line = 0
    position = 0
    for match in _TOKEN_PATTERN.finditer(source):
        kind = match.lastgroup or "invalid"
        start, end = match.start(), match.end()
        line += source.count("\n", position, start)
        position = start
        if kind != "space":
            tokens.append(_Token(kind, match.group(), start, end, line))
    return tokens


def _indentation(line: str) -> int:
    expanded = line.replace("\t", " " * INDENT)
    return len(expanded) - len(expanded.lstrip(" "))


def format_source(source: str) -> str:
    """The source, formatted (see the module's documentation). Only whitespace changes: if anything else would, the
    source is returned as it is."""
    source = source.replace("\r\n", "\n")
    lines = _checked_lines(source)
    return source if lines is None else _join(lines)


def _checked_lines(source: str) -> list[str | None] | None:
    """The source's lines, formatted (see `_format_lines`), if only whitespace changes (as it should)."""
    lines = _format_lines(source)
    return lines if _same_tokens(source, _join(lines)) else None


def _join(lines: list[str | None]) -> str:
    return "".join(line + "\n" for line in lines if line is not None)


def _same_tokens(source: str, formatted: str) -> bool:
    """Whether two sources have the same tokens (ignoring the whitespace starting and ending comments' lines)."""

    def values(text: str) -> list[str]:
        return [
            "\n".join(line.strip() for line in token.value.split("\n")) if "comment" in token.kind else token.value
            for token in _lex(text)
        ]

    return values(source) == values(formatted)


def is_generated(path: str) -> bool:
    """Whether a file is generated (and so formatted by what generates it, like `fs gen`), by its name."""
    return path.endswith(".gen.fs")


def _format_lines(source: str) -> list[str | None]:
    """Each of the source's lines (split on newlines), formatted, or None for ones which are removed."""
    lines = source.split("\n")
    tokens = _lex(source)

    # Lines which are inside a multi-line string or block comment (after its first line) aren't touched, but block
    # comments' lines move with their first line
    inside: dict[int, tuple[int, str]] = {}  # line -> (first line of the token, its kind)
    for token in tokens:
        if token.kind in ("string", "block_comment") and "\n" in token.value:
            last = token.line + token.value.count("\n")
            for line in range(token.line + 1, last + 1):
                inside[line] = (token.line, token.kind)

    first_token: dict[int, int] = {}  # line -> index of its first token
    line_tokens: dict[int, list[_Token]] = {}
    for index, token in enumerate(tokens):
        first_token.setdefault(token.line, index)
        line_tokens.setdefault(token.line, []).append(token)

    formatted_indent: dict[int, int] = {}
    statement = _Statement(0, 0)
    blocks: list[_Block] = []
    # For each open bracket: whether it's a block, the formatted indentation of its line, and what came before it
    brackets: list[tuple[str, bool, int, str | None]] = []
    previous: _Token | None = None  # The previous code token
    # What came before the last `(` closed (`switch`'s `(...)` is followed by a map), and where the last block's `}`
    # was
    closed_paren_head: str | None = None
    closed_block_indent = 0
    # Whether the previous code token was a block's `}`
    previous_closed_block = False

    def block_level() -> int:
        return blocks[-1].contents if blocks else 0

    def opens_block() -> bool:
        if previous is not None and previous.value == ")" and closed_paren_head == "switch":
            return False
        return _opens_block(previous, not brackets or brackets[-1][1])

    def header_indent() -> int | None:
        """The indentation of the header of a block whose `{` is on a line of its own: for a function's `precondition`
        block, the `precondition` line; for the block after it, that block's; for any other block in a block (not in
        parentheses, or a function literal's statement, whose blocks are where they're written), its statement's
        first line."""
        if previous is not None and previous.value == "precondition":
            return formatted_indent.get(previous.line, statement.formatted)
        if previous is not None and previous.value == "}" and previous_closed_block:
            return closed_block_indent
        if statement.depth == 0 and not statement.function_literal:
            return statement.formatted
        return None

    for line_number, text in enumerate(lines):
        if line_number in inside:
            first, kind = inside[line_number]
            if kind == "block_comment" and first in formatted_indent:
                delta = formatted_indent[first] - _indentation(lines[first])
                formatted_indent[line_number] = max(0, _indentation(text) + delta) if text.strip() else 0
            continue
        if not text.strip():
            continue
        token = tokens[first_token[line_number]] if line_number in first_token else None
        written = _indentation(text)
        if token is None:
            formatted_indent[line_number] = written
            continue

        # The line's first code token, if it starts with one
        is_code = token.kind not in ("line_comment", "block_comment")
        if is_code and token.value == "}" and brackets and brackets[-1][1]:
            indent = blocks[-1].close
        elif is_code and token.value == "{" and opens_block() and header_indent() is not None:
            # A control structure's or declaration's block: under its header
            indent = header_indent()
        elif is_code and token.value == "precondition" and statement.open:
            # Under its function's signature, but a function literal's is indented (as std's are)
            indent = statement.formatted + (INDENT if statement.depth > 0 or statement.function_literal else 0)
        elif statement.open:
            indent = statement.formatted + max(0, written - statement.written)
        else:
            indent = block_level()
        formatted_indent[line_number] = indent
        if not statement.open and not (is_code and token.value in ("}", "{")):
            statement = _Statement(written, indent)

        # Walk the line's code tokens, updating the brackets, blocks, and statement
        index = first_token[line_number]
        while index < len(tokens) and tokens[index].line == line_number:
            current = tokens[index]
            index += 1
            if current.kind in ("line_comment", "block_comment"):
                continue
            value = current.value
            closed_block = False
            if value == "{" and opens_block():
                # Its contents are indented from its header: the statement's first line, for a control structure's
                # or declaration's block (whose header may span several lines); for a block inside parentheses (a
                # function literal's body), the line it's on
                header = (
                    indent if token is current or statement.depth > 0 or statement.function_literal else header_indent()
                )
                blocks.append(_Block(header + INDENT, header, statement))
                brackets.append(("{", True, header, None))
                statement = _Statement(written, header + INDENT)
            elif value in "([{":
                brackets.append((value, False, indent, previous.value if previous is not None else None))
                statement.depth += 1
                statement.open = True
            elif value in ")]}":
                if brackets and brackets[-1][1] and value == "}":
                    brackets.pop()
                    block = blocks.pop()
                    closed_block_indent = block.close
                    closed_block = True
                    statement = block.outer
                    # A block ends its statement, unless it's inside parentheses or a function literal (whose statement
                    # ends with its `;`)
                    statement.open = statement.depth > 0 or statement.function_literal
                elif brackets:
                    closed_paren_head = brackets.pop()[3]
                    statement.depth = max(0, statement.depth - 1)
                    # An annotation's map ends with its `}`, and what it annotates starts after it
                    if statement.depth == 0 and statement.annotation and value == "}":
                        statement.open = False
            elif value == ";" and statement.depth == 0:
                statement.open = False
            else:
                if not statement.open and value == "annotation":
                    statement.annotation = True
                # A function literal (`function(x) { ... }`), not a declaration's name or the type `function`
                if value == "function" and statement.open and index < len(tokens) and tokens[index].value == "(":
                    statement.function_literal = True
                statement.open = True
            previous_closed_block = closed_block
            previous = current

    results: list[str | None] = []
    blank = 0
    for line_number, text in enumerate(lines):
        if line_number in inside and inside[line_number][1] == "string":
            results.append(text)
            blank = 0
            continue
        stripped = text.strip()
        if not stripped:
            blank += 1
            results.append("" if blank <= MAX_BLANK_LINES else None)
            continue
        blank = 0
        if line_number in inside:
            content = text.rstrip().lstrip(" \t")
        else:
            content = _space_line(source, line_tokens.get(line_number, []))
        results.append(" " * formatted_indent.get(line_number, _indentation(text)) + content)
    # No blank lines at the end (the file ends with a newline, after its last line)
    for line_number in reversed(range(len(results))):
        if results[line_number]:
            break
        results[line_number] = None
    return results


def _opens_block(previous: _Token | None, in_block: bool) -> bool:
    """Whether a `{` after previous opens a block (rather than a map). `in_block` is whether it's directly in a block
    (rather than parentheses or a map), where a `{` after another block's `{` opens a block too."""
    if previous is None:
        return True
    if previous.value == "{":
        return in_block
    if previous.value in _LITERAL_BEFORE:
        return False
    if previous.kind in _OPERATOR_KINDS:
        return False
    return True


def _space_line(source: str, tokens: list[_Token]) -> str:
    """A line's tokens, with the spacing between them fixed (see `_gap`). A token running onto later lines (a block
    comment or string) ends the line with its first line."""
    parts = []
    for index, token in enumerate(tokens):
        if index:
            previous = tokens[index - 1]
            parts.append(_gap(previous, token, source[previous.end : token.start], tokens, index))
        parts.append(token.value.split("\n", 1)[0])
    return "".join(parts).rstrip()


def _gap(before: _Token, after: _Token, written: str, tokens: list[_Token], index: int) -> str:
    """The space between two tokens on a line (`written` is what's there)."""
    collapsed = " " if written else ""
    if after.kind in ("line_comment", "block_comment"):
        return written or " "
    if before.kind in ("line_comment", "block_comment"):
        return collapsed
    a, b = before.value, after.value
    if b in (",", ";"):
        return ""
    if a in (",", ";"):
        return " "
    if a in ("(", "[") or b in (")", "]"):
        return ""
    if b == "(":
        if a in _SPACED_KEYWORDS:
            return " "
        if before.kind == "identifier" and a not in _NOT_CALLS:
            return ""
        return collapsed
    # Before an operator or a colon, a run of spaces is kept, for columns lined up (as std's value bounds are)
    if b in _SPACED_OPERATORS or b == ":":
        return written if written.strip(" ") == "" and written else " "
    if a in _SPACED_OPERATORS or a == ":":
        return " "
    if a == "{" and b == "}":
        return collapsed
    if a == "{" or b == "}" or b == "{":
        return " "
    return collapsed


def edits(source: str) -> list[tuple[int, int, str]]:
    """The changes formatting source makes, as (start, end, new text) replacements of its text, in order: each
    replaces a run of lines that changed, so an editor's undo and cursor see little change."""
    if "\r" in source:
        formatted = format_source(source)
        # Its line endings change everywhere
        return [] if formatted == source else [(0, len(source), formatted)]
    lines = _checked_lines(source)
    if lines is None:
        return []
    # Each line's text, with its newline (but the last, if the source doesn't end with one), and where it starts
    old = source.split("\n")
    if source.endswith("\n"):
        old.pop()
        lines.pop()  # The empty line after the last newline, which isn't one
    starts = [0]
    for line in old:
        starts.append(min(starts[-1] + len(line) + 1, len(source)))
    results: list[tuple[int, int, str]] = []
    for number, line in enumerate(lines):
        new = "" if line is None else line + "\n"
        if source[starts[number] : starts[number + 1]] == new:
            continue
        if results and results[-1][1] == starts[number]:
            start, _, text = results.pop()
            results.append((start, starts[number + 1], text + new))
        else:
            results.append((starts[number], starts[number + 1], new))
    return results
