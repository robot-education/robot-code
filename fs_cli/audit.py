"""`fs audit`: one page to audit a feature by: its dialog, which works (clicking dropdowns, checkboxes, tabs, and lookup
table levels, and typing values, shows the dialog as it would be), the lookup tables its parameters use, its writeup,
and its file's `fs check` problems.

The dialog is drawn by the page's script (see `fs_cli.live_dialog`): its precondition's conditions are compiled to
JavaScript, and each parameter is rendered ahead of time for each of its values, so the script draws any combination of
choices as `fs ui --set` would, as they're made. `dialog_page.js` makes its controls work, as it does in VS Code's
feature panel, which shows the same sections (`audit_sections`, through the language server's `featurescript/audit`).
"""

from __future__ import annotations

import html
import pathlib

from markdown_it import MarkdownIt

from fs_cli.lookup_tables import TABLE_STYLE, Table, table_section

# Where a dialog's parameters are, in a page `fs ui` renders
LIST_START = "<os-parameter-list-view>"
LIST_END = "</os-parameter-list-view>"

# Where `fs audit --open` asks VS Code's FeatureScript extension to show a feature's preview: the extension watches for
# it (see vscode-extension/src/extension.ts)
OPEN_REQUEST = ".fs-audit/open.json"

# Makes a dialog's controls work (see its comment), here and in VS Code's feature panel
DIALOG_PAGE_SCRIPT = (pathlib.Path(__file__).parent / "dialog_page.js").read_text(encoding="utf-8")


def split_page(page: str) -> tuple[str, str]:
    """A rendered dialog page's shell (with `\\0` where its parameters go) and its parameters."""
    start = page.index(LIST_START) + len(LIST_START)
    end = page.index(LIST_END, start)
    return page[:start] + "\0" + page[end:], page[start:end]


def render_markdown(text: str) -> str:
    return MarkdownIt("commonmark", {"html": False}).enable("table").enable("strikethrough").render(text)


def check_problems(project, module) -> list[tuple[int, int, str]]:
    """A module's `fs check` problems: each one's line and character (from 0), and its text (as `fs check` prints it,
    without the file)."""
    problems = []
    for problem in project.check(module):
        line, character = module.position(problem.start)
        problems.append((line, character, f"{line + 1}:{character + 1}: {problem.severity}: {problem.message} [{problem.code}]"))
    return problems


def feature_tables(config, project, module) -> list[Table]:
    """The lookup tables a file's parameters use (theirs, or those of files it imports), in the order they're named."""
    from fs_cli.lookup_tables import find_tables, find_uses, lookup_parameters, source_evaluator
    from fs_eval import Evaluator

    modules = [module]
    seen = {module.path}
    for current in modules:
        for imported in current.imports:
            if imported.is_std or imported.namespace:
                continue
            target = project.resolve(imported)
            if target is not None and target.path not in seen:
                seen.add(target.path)
                modules.append(target)
    names: list[str] = []
    for current in modules:
        for table_name, _ in lookup_parameters(current.path.read_text()):
            if table_name not in names:
                names.append(table_name)
    if not names:
        return []
    evaluator = Evaluator(config.root)
    found = {}
    for current in modules:
        if '"entries"' in current.path.read_text():
            source = source_evaluator(project, config.std_dir, current.path)
            for found_table in find_tables(evaluator, current.path, config.code_dir, source):
                found.setdefault(found_table.name, found_table)
    tables = [found[name] for name in names if name in found]
    find_uses(tables, config.code_dir)
    return tables


def audit_sections(writeup: str | None, problems: list[tuple[int, int, str]], tables: list[Table]) -> dict[str, str]:
    """The HTML of the sections beside the dialog, styled by `SECTION_STYLE`: `tables` (every option of the lookup
    tables its parameters use, with a filter), `writeup`, and `problems` (each with its `data-line` and
    `data-character`)."""
    if tables:
        tables_html = (
            "<p class='limit'>Every option, a row per path through its levels, in the dialog's order. ★ marks each level's "
            "default (where the dialog goes when you choose the level above), and the bold row is what it starts with.</p>"
            "<input class='table-filter' type='search' placeholder='Filter rows (all words must match)'>"
            "<div class='lookup'>" + "".join(table_section(table, f"table-{index}", "h3", values=False) for index, table in enumerate(tables)) + "</div>"
        )
    else:
        tables_html = "<p>Its parameters use no lookup tables.</p>"
    if problems:
        problems_html = "<ul class='problems'>" + "".join(
            f"<li data-line='{line}' data-character='{character}'><code>{html.escape(text)}</code></li>"
            for line, character, text in problems
        ) + "</ul>"
    else:
        problems_html = "<p>No problems found.</p>"
    writeup_html = render_markdown(writeup) if writeup is not None else "<p>It has no writeup (a <code>.md</code> beside it).</p>"
    return {"tables": tables_html, "writeup": writeup_html, "problems": problems_html}


def audit_page(title: str, source: str, shell: str, script: str, writeup: str | None, problems: list[tuple[int, int, str]],
               theme: str, tables: list[Table] | None = None) -> str:
    """The audit page: the dialog (in a frame of its own, as its styles are Onshape's), drawn by `script` (see
    `fs_cli.live_dialog.page_script`) in `shell` (a page `fs ui` renders, see `split_page`), and `audit_sections`."""
    dialog = (
        shell.replace("\0", "")
        .replace("<body>", "<body>" + _DIALOG_TOOLBAR, 1)
        .replace("</body>", f"<script>{script}\n{_safe(DIALOG_PAGE_SCRIPT)}\n{_DIALOG_SCRIPT}</script></body>", 1)
        .replace("<style>", "<style>" + _DIALOG_STYLE, 1)
    )
    sections = audit_sections(writeup, problems, tables or [])
    return _PAGE.format(
        title=html.escape(title),
        source=html.escape(source),
        theme=theme,
        dialog=html.escape(dialog, quote=True),
        problems=sections["problems"],
        writeup=sections["writeup"],
        tables=sections["tables"],
        section_style=SECTION_STYLE,
    )


def _safe(script: str) -> str:
    """A script, safe in a page's script element."""
    return script.replace("</", "<\\/")


# The sections' styles, using the page's colors: --fg, --bg, --muted, --line, --head, --code, --accent, and --empty
SECTION_STYLE = """
.limit { color: var(--muted); font-size: 12px; margin: 4px 0; }
.writeup table { border-collapse: collapse; font-size: 13px; display: block; overflow-x: auto; }
.writeup th, .writeup td { border: 1px solid var(--line); padding: 4px 8px; text-align: left; vertical-align: top; }
.writeup th { background: var(--head); }
.writeup code { background: var(--code); padding: 1px 4px; border-radius: 3px; font-size: 12px; }
.writeup pre { background: var(--code); padding: 8px; border-radius: 6px; overflow-x: auto; }
.writeup pre code { padding: 0; }
.writeup h1 { font-size: 18px; }
.writeup h2 { font-size: 16px; margin-top: 24px; }
.writeup h3 { font-size: 14px; }
.problems code { background: var(--code); padding: 1px 4px; border-radius: 3px; }
.table-filter { width: 100%; max-width: 420px; box-sizing: border-box; padding: 6px 8px; margin-bottom: 8px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--fg); }
.lookup h3 { font-size: 14px; margin: 16px 0 2px; font-family: ui-monospace, monospace; }
""" + TABLE_STYLE

_DIALOG_STYLE = """
#fs-note { margin: 0 0 8px; font: 12px system-ui, sans-serif; color: #f59e0b; }
/* A long press shows a tooltip, rather than selecting text or opening the browser's menu */
#feature-dialog { -webkit-touch-callout: none; -webkit-user-select: none; user-select: none; }
/* By touch, typed values' fields are taller, to tap */
@media (pointer: coarse) { #feature-dialog .os-param-number { min-height: 34px; } }
"""

_DIALOG_TOOLBAR = "<div id='fs-note' hidden></div>"

# The page's glue: clicks set settings, and the dialog is drawn again (`renderList`, from fs_cli.live_dialog.RUNTIME)
_DIALOG_SCRIPT = r"""
const dialog = document.getElementById("feature-dialog");
const list = dialog.querySelector("os-parameter-list-view");
const note = document.getElementById("fs-note");
// What's been set (as fs ui --set takes it)
const settings = {};

// The frame fits the dialog, and any open menu (which is fixed in place, so not in the page's height)
function reportHeight() {
  let bottom = dialog.getBoundingClientRect().bottom + window.scrollY;
  for (const menu of dialog.querySelectorAll(".os-select-dropdown.open")) {
    bottom = Math.max(bottom, menu.getBoundingClientRect().bottom + window.scrollY);
  }
  parent.postMessage({ fsAuditHeight: Math.ceil(bottom) + 12 }, "*");
}

const page = dialogPage(dialog, {
  // Setting a parameter to its default unsets it
  set(key, value) {
    const parameter = DIALOG.parameters[key];
    if (!parameter) {
      return;
    }
    if (value === parameter.default) {
      delete settings[key];
    } else {
      settings[key] = value;
    }
    note.hidden = true;
    draw();
  },
  note(text) {
    note.textContent = text;
    note.hidden = false;
  },
  toggled: {},
  resized: reportHeight,
});

function draw() {
  list.innerHTML = renderList({ ...settings }).html;
  page.drawn();
}

draw();
window.addEventListener("load", reportHeight);
"""

_PAGE = """<!doctype html>
<html lang="en" data-theme="{theme}">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{title} audit</title>
<style>
:root {{ --bg: #ffffff; --fg: #1d2129; --muted: #6b7280; --line: #e5e7eb; --head: #f3f4f6; --code: #f3f4f6; --accent: #2563eb; --empty: #b45309; }}
@media (prefers-color-scheme: dark) {{
  :root:not([data-theme="light"]) {{ --bg: #16181d; --fg: #e5e7eb; --muted: #9ca3af; --line: #2d3139; --head: #1f2229; --code: #23262d; --accent: #60a5fa; --empty: #f59e0b; }}
}}
:root[data-theme="dark"] {{ --bg: #16181d; --fg: #e5e7eb; --muted: #9ca3af; --line: #2d3139; --head: #1f2229; --code: #23262d; --accent: #60a5fa; --empty: #f59e0b; }}
body {{ margin: 0; padding: 16px; background: var(--bg); color: var(--fg); font: 14px/1.5 system-ui, sans-serif; }}
main {{ max-width: 1100px; margin: 0 auto; }}
h1 {{ font-size: 20px; margin: 0; }}
.source {{ color: var(--muted); font: 12px ui-monospace, monospace; margin: 2px 0 16px; }}
nav {{ display: flex; gap: 16px; margin-bottom: 16px; }}
nav a {{ color: var(--accent); text-decoration: none; }}
section {{ margin-bottom: 32px; }}
section > h2 {{ font-size: 16px; border-bottom: 1px solid var(--line); padding-bottom: 4px; }}
iframe {{ width: 100%; max-width: 420px; height: 600px; border: 1px solid var(--line); border-radius: 6px; display: block; }}
{section_style}
</style>
</head>
<body>
<main>
<h1>{title}</h1>
<p class="source">{source}</p>
<nav><a href="#dialog">Dialog</a><a href="#tables">Lookup tables</a><a href="#writeup">Writeup</a><a href="#problems">Problems</a></nav>
<section id="dialog">
<h2>Dialog</h2>
<p class="limit">Click dropdowns, checkboxes, tabs, and lookup table levels, or type values, to see what each choice shows: any combination works. Setting a parameter back to its default (unchecking it, say) unsets it. Hover a parameter for its description, default, and UI hints.</p>
<iframe id="dialog-frame" title="Dialog" srcdoc="{dialog}"></iframe>
</section>
<section id="tables">
<h2>Lookup tables</h2>
{tables}
</section>
<section id="writeup" class="writeup">
<h2>Writeup</h2>
{writeup}
</section>
<section id="problems">
<h2>Problems (fs check)</h2>
{problems}
</section>
</main>
<script>
const tableFilter = document.querySelector(".table-filter");
if (tableFilter) {{
  tableFilter.addEventListener("input", () => {{
    const words = tableFilter.value.toLowerCase().split(/\\s+/).filter(Boolean);
    for (const row of document.querySelectorAll(".lookup tbody tr")) {{
      row.classList.toggle("hidden", !words.every((word) => row.dataset.text.includes(word)));
    }}
  }});
}}
const frame = document.getElementById("dialog-frame");
window.addEventListener("message", (event) => {{
  if (event.source === frame.contentWindow && event.data && event.data.fsAuditHeight) {{
    frame.style.height = Math.max(200, event.data.fsAuditHeight) + "px";
  }}
}});
</script>
</body>
</html>
"""
