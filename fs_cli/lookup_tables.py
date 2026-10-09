"""Lookup tables (FeatureScript's `LookupTable`s: nested maps of options, each level with a `name`, a `displayName`, a
`default`, and `entries`) as flat rows, one per path to a leaf, with the leaf's values: for auditing them (`fs table`).

They're found and evaluated with the evaluator (`fs_eval`), so a table is shown as FeatureScript builds it, whether it's
generated (`fs gen`) or written by hand, and values computed from expressions are shown worked out.
"""

from __future__ import annotations

import dataclasses
import html
import math
import pathlib
import re

from fs_cli.remote import is_local
from fs_eval.interpreter import ConstSymbol, to_display
from fs_eval.values import EnumValue, FSArray, FSMap, Tagged, format_number, untag

# Leaf values longer than this are cut short in text and Markdown (HTML shows them whole)
MAX_TEXT_CELL = 48


@dataclasses.dataclass
class Row:
    # The key at each level, and whether it's that level's default
    path: list[str]
    defaults: list[bool]
    # The leaf's values, formatted, by field
    values: dict[str, str]


@dataclasses.dataclass
class Table:
    name: str
    # Where it's defined, relative to the code folder
    module: str
    # Each level's display name (several, joined with " / ", where branches name a level differently)
    levels: list[str]
    # The leaves' fields, in the order they're first seen
    fields: list[str]
    rows: list[Row]
    # The parameters which use it, like `released/shaft/robotShaft.fs: Tapped hole` (see `find_uses`)
    used_by: list[str] = dataclasses.field(default_factory=list)


def is_lookup_table(value) -> bool:
    value = untag(value)
    return type(value) is FSMap and type(untag(value.get_str("entries"))) is FSMap and isinstance(value.get_str("name"), str)


def flatten(name: str, module: str, table) -> Table:
    """A lookup table's rows: every path from its top level to a leaf."""
    levels: list[list[str]] = []
    fields: list[str] = []
    rows: list[Row] = []

    def visit(node, path: list[str], defaults: list[bool]) -> None:
        node = untag(node)
        if is_lookup_table(node):
            depth = len(path)
            label = node.get_str("displayName") or node.get_str("name")
            while len(levels) <= depth:
                levels.append([])
            if label not in levels[depth]:
                levels[depth].append(label)
            default = node.get_str("default")
            for key, child in untag(node.get_str("entries")).sorted_items():
                key_text = format_value(key)
                visit(child, path + [key_text], defaults + [key == default])
            return
        values = {}
        if type(node) is FSMap:
            for key, value in node.sorted_items():
                field = format_value(key)
                if field not in fields:
                    fields.append(field)
                values[field] = format_value(value)
        else:
            values["value"] = format_value(node)
            if "value" not in fields:
                fields.append("value")
        rows.append(Row(path, defaults, values))

    visit(table, [], [])
    return Table(name, module, [" / ".join(names) for names in levels], fields, rows)


def find_tables(evaluator, path: pathlib.Path, code_dir: pathlib.Path) -> list[Table]:
    """The lookup tables a file defines (its constants which are lookup tables), evaluated."""
    from fs_eval import FSError

    module = evaluator.module(path)
    relative = path.resolve().relative_to(code_dir.resolve()).as_posix()
    tables = []
    for name, symbol in module.symbols.items():
        if not isinstance(symbol, ConstSymbol):
            continue
        try:
            value = evaluator.eval(name, module)
        except FSError:
            continue
        if is_lookup_table(value):
            tables.append(flatten(name, relative, value))
    return tables


_LOOKUP_ANNOTATION = re.compile(r'annotation\s*\{[^}]*"Lookup Table"\s*:\s*(\w+)[^}]*\}')
_NAME = re.compile(r'"Name"\s*:\s*"([^"]*)"')


def find_uses(tables: list[Table], code_dir: pathlib.Path) -> None:
    """Fills in each table's `used_by`: the parameters whose annotations name it as their `"Lookup Table"`, by name
    (so a table another file defines with the same name would be matched too)."""
    by_name: dict[str, list[Table]] = {}
    for table in tables:
        by_name.setdefault(table.name, []).append(table)
    for path in sorted(code_dir.rglob("*.fs")):
        if is_local(path.name):
            continue
        for match in _LOOKUP_ANNOTATION.finditer(path.read_text()):
            for table in by_name.get(match.group(1), []):
                name = _NAME.search(match.group(0))
                table.used_by.append(f"{path.relative_to(code_dir).as_posix()}: {name.group(1) if name else '?'}")


# Values


def format_value(value) -> str:
    kind = type(value)
    if kind is Tagged:
        return format_value(value.value)
    if kind is str:
        return value
    if kind is float:
        return format_number(value)
    if kind is bool:
        return "true" if value else "false"
    if value is None:
        return "undefined"
    if kind is EnumValue:
        return f"{value.enum.name}.{value.name}"
    if kind is FSArray:
        if len(value.items) > 4:
            return f"[{len(value.items)} items]"
        return "[" + ", ".join(format_value(item) for item in value.items) + "]"
    if kind is FSMap:
        tag_name = value.tag.name if value.tag is not None else None
        if tag_name == "ValueWithUnits":
            return format_quantity(value)
        if tag_name == "Color":
            channels = [value.get_str(channel) for channel in ("red", "green", "blue")]
            if all(isinstance(channel, float) for channel in channels):
                return "#" + "".join(f"{round(channel * 255):02x}" for channel in channels)
        if tag_name == "Material":
            return f"{format_value(value.get_str('name'))} ({format_value(value.get_str('density'))})"
        tag = f"{value.tag.name} " if value.tag is not None else ""
        return tag + "{" + ", ".join(f"{format_value(k)}: {format_value(v)}" for k, v in value.sorted_items()) + "}"
    return to_display(value)


def format_quantity(value: FSMap) -> str:
    """A `ValueWithUnits` in the unit it was most likely written in: a length in inches if it's a round number of them
    (to 4 places), else in millimeters; an angle in degrees."""
    number = value.get_str("value")
    unit = {key: exponent for key, exponent in untag(value.get_str("unit")).sorted_items()}
    if unit == {"meter": 1.0}:
        inches = number / 0.0254
        if _is_round(inches, 4):
            return f"{_round(inches, 4)} in"
        return f"{_round(number * 1000, 4)} mm"
    if unit == {"radian": 1.0}:
        return f"{_round(math.degrees(number), 4)} deg"
    if unit == {"kilogram": 1.0, "meter": -3.0}:
        return f"{_round(number / 1000, 4)} g/cm³"
    text = format_number(float(f"{number:.6g}"))
    for name, exponent in unit.items():
        text += " " + name + ("" if exponent == 1 else "^" + format_number(exponent))
    return text


def _is_round(number: float, places: int) -> bool:
    return abs(round(number, places) - number) < 1e-9 * max(1.0, abs(number))


def _round(number: float, places: int) -> str:
    return format_number(round(number, places) + 0.0)


# Output


def _header(table: Table) -> list[str]:
    return [*table.levels, *table.fields]


def _cells(table: Table, row: Row, short: bool) -> list[str]:
    keys = [key + (" *" if default else "") for key, default in zip(row.path, row.defaults)]
    keys += [""] * (len(table.levels) - len(keys))
    values = [row.values.get(field, "") for field in table.fields]
    if short:
        values = [value if len(value) <= MAX_TEXT_CELL else value[: MAX_TEXT_CELL - 1] + "…" for value in values]
    return keys + values


def to_text(tables: list[Table]) -> str:
    """Aligned columns, a table at a time, with each level's default marked *."""
    out = []
    for table in tables:
        header = _header(table)
        rows = [_cells(table, row, True) for row in table.rows]
        widths = [max(len(cell) for cell in column) for column in zip(header, *rows)] if rows else [len(h) for h in header]
        out.append(f"{table.name}  ({table.module}, {_count(table)})")
        out.append(f"Used by: {_uses(table)}")
        out.append("  ".join(cell.ljust(width) for cell, width in zip(header, widths)).rstrip())
        out.append("  ".join("-" * width for width in widths))
        for cells in rows:
            out.append("  ".join(cell.ljust(width) for cell, width in zip(cells, widths)).rstrip())
        out.append("")
    return "\n".join(out)


def to_markdown(tables: list[Table]) -> str:
    out = []
    for table in tables:
        out.append(f"### {table.name}\n\n`{table.module}`, {_count(table)}. Used by: {_uses(table)}. Each level's default is marked *.\n")
        header = _header(table)
        out.append("| " + " | ".join(header) + " |")
        out.append("| " + " | ".join("---" for _ in header) + " |")
        for row in table.rows:
            cells = [cell.replace("|", "\\|") for cell in _cells(table, row, True)]
            out.append("| " + " | ".join(cells) + " |")
        out.append("")
    return "\n".join(out)


def _uses(table: Table) -> str:
    return "; ".join(table.used_by) or "nothing (no parameter's Lookup Table)"


def _count(table: Table) -> str:
    return f"{len(table.rows)} row" + ("" if len(table.rows) == 1 else "s")


def to_html(tables: list[Table], title: str = "Lookup tables") -> str:
    """One page with every table: a filter, and each table's rows with repeated path prefixes dimmed, defaults bold,
    and empty leaves called out."""
    sections = []
    nav = []
    for index, table in enumerate(tables):
        anchor = f"t{index}"
        nav.append(f'<a href="#{anchor}">{html.escape(table.name)}</a>')
        head = "".join(f'<th class="level">{html.escape(level)}</th>' for level in table.levels)
        head += "".join(f"<th>{html.escape(field)}</th>" for field in table.fields)
        body = []
        previous: list[str] = []
        for row in table.rows:
            cells = []
            for depth in range(len(table.levels)):
                if depth >= len(row.path):
                    cells.append('<td class="level"></td>')
                    continue
                key = row.path[depth]
                repeated = previous[: depth + 1] == row.path[: depth + 1]
                classes = ["level"]
                if repeated:
                    classes.append("repeat")
                if row.defaults[depth]:
                    classes.append("default")
                cells.append(f'<td class="{" ".join(classes)}">{html.escape(key)}</td>')
            if not row.values:
                cells.append(f'<td class="empty" colspan="{max(1, len(table.fields))}">no values</td>')
            else:
                cells += [f'<td class="value">{_value_html(row.values.get(field, ""))}</td>' for field in table.fields]
            previous = row.path
            text = " ".join([*row.path, *row.values.values()]).lower()
            body.append(f'<tr data-text="{html.escape(text)}">{"".join(cells)}</tr>')
        sections.append(
            f'<section id="{anchor}"><h2>{html.escape(table.name)}</h2>'
            f'<p class="meta">{html.escape(table.module)} · {_count(table)} · defaults in bold<br>Used by: {html.escape(_uses(table))}</p>'
            f'<div class="scroll"><table><thead><tr>{head}</tr></thead><tbody>{"".join(body)}</tbody></table></div>'
            "</section>"
        )
    return _PAGE.format(title=html.escape(title), nav="".join(nav), sections="".join(sections))


def _value_html(value: str) -> str:
    """A value's cell, with a swatch for a color."""
    if len(value) == 7 and value.startswith("#") and all(c in "0123456789abcdef" for c in value[1:]):
        return f'<span class="swatch" style="background:{value}"></span>{value}'
    return html.escape(value)


_PAGE = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title}</title>
<style>
:root {{ --bg: #ffffff; --fg: #1d2129; --muted: #6b7280; --line: #e5e7eb; --head: #f3f4f6; --accent: #2563eb; --empty: #b45309; }}
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{ --bg: #16181d; --fg: #e5e7eb; --muted: #9ca3af; --line: #2d3139; --head: #1f2229; --accent: #60a5fa; --empty: #f59e0b; }}
}}
:root[data-theme="dark"] {{ --bg: #16181d; --fg: #e5e7eb; --muted: #9ca3af; --line: #2d3139; --head: #1f2229; --accent: #60a5fa; --empty: #f59e0b; }}
body {{ margin: 0; padding: 16px; background: var(--bg); color: var(--fg); font: 14px/1.4 system-ui, sans-serif; }}
header {{ position: sticky; top: 0; background: var(--bg); padding-bottom: 8px; z-index: 2; }}
h1 {{ font-size: 18px; margin: 0 0 8px; }}
nav {{ display: flex; flex-wrap: wrap; gap: 4px 12px; margin-bottom: 8px; }}
nav a {{ color: var(--accent); text-decoration: none; font-family: ui-monospace, monospace; font-size: 13px; }}
input {{ width: 100%; max-width: 420px; box-sizing: border-box; padding: 6px 8px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--fg); }}
h2 {{ font-size: 15px; margin: 24px 0 2px; font-family: ui-monospace, monospace; }}
.meta {{ margin: 0 0 8px; color: var(--muted); font-size: 12px; }}
.scroll {{ overflow-x: auto; }}
table {{ border-collapse: collapse; font-size: 13px; }}
th, td {{ border-bottom: 1px solid var(--line); padding: 4px 10px; text-align: left; white-space: nowrap; vertical-align: top; }}
td.value {{ white-space: normal; min-width: 80px; max-width: 360px; overflow-wrap: anywhere; }}
.swatch {{ display: inline-block; width: 10px; height: 10px; border-radius: 2px; margin-right: 4px; vertical-align: -1px; border: 1px solid var(--line); }}
th {{ background: var(--head); position: sticky; top: 0; }}
td.level {{ font-weight: 500; }}
td.repeat {{ color: var(--muted); font-weight: 400; opacity: 0.45; }}
td.default {{ font-weight: 700; }}
td.default::after {{ content: " ★"; color: var(--accent); font-size: 11px; }}
td.empty {{ color: var(--empty); font-style: italic; }}
tr.hidden {{ display: none; }}
</style>
</head>
<body>
<header>
<h1>{title}</h1>
<nav>{nav}</nav>
<input id="filter" type="search" placeholder="Filter rows (all words must match)">
</header>
{sections}
<script>
const filter = document.getElementById("filter");
filter.addEventListener("input", () => {{
  const words = filter.value.toLowerCase().split(/\\s+/).filter(Boolean);
  for (const row of document.querySelectorAll("tbody tr")) {{
    const text = row.dataset.text;
    row.classList.toggle("hidden", !words.every(word => text.includes(word)));
  }}
}});
</script>
</body>
</html>
"""
