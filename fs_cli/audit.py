"""`fs audit`: one page to audit a feature by: its dialog, which works (clicking dropdowns, checkboxes, tabs, and lookup
table levels shows the dialog as it would be), its writeup, and its file's `fs check` problems.

The page is static, so the dialog's states are rendered ahead of time (`explore_dialog`): from its defaults, each
choice a click could make is rendered in turn, breadth first, up to a limit, and the page's script swaps between them.
Values typed into fields aren't rendered (there are too many); `fs ui --set` renders any state.
"""

from __future__ import annotations

import heapq
import html
import json
import re
from typing import Callable

from markdown_it import MarkdownIt

from fs_cli.lookup_tables import TABLE_STYLE, Table, table_section

# Where a dialog's parameters are, in a page `fs ui` renders
LIST_START = "<os-parameter-list-view>"
LIST_END = "</os-parameter-list-view>"
# What clicking a control sets (see `fs_cli.ui._setting`)
_SETTING = re.compile(r"data-set='([^']*)' data-value='([^']*)'")
_PARAMETER = re.compile(r"data-parameter-id='([^']*)'")


def split_page(page: str) -> tuple[str, str]:
    """A rendered dialog page's shell (with `{list}` where its parameters go) and its parameters."""
    start = page.index(LIST_START) + len(LIST_START)
    end = page.index(LIST_END, start)
    return page[:start] + "\0" + page[end:], page[start:end]


def explore_dialog(render: Callable[[dict[str, str]], str], max_states: int) -> tuple[str, list[dict], bool]:
    """The dialog's states: each its settings (as `fs ui --set` takes them), its parameters' HTML, and the state each
    of its choices leads to (by `name=value`). Returns the page's shell (see `split_page`), the states (the first is
    the defaults), and whether there were more than `max_states`.

    States are explored fewest choices first, and among those, ones which show a set of parameters not seen yet first
    (choices which reveal or hide parameters), so every part of the dialog is reached before combinations of choices
    which only change what's shown in them."""
    shell, first = split_page(render({}))
    states: list[dict] = []
    by_html: dict[str, int] = {}
    shown: set[frozenset[str]] = set()
    queue: list[tuple[int, int, int]] = []
    truncated = False

    def add(settings: dict[str, str], parameters: str) -> int | None:
        nonlocal truncated
        if parameters in by_html:
            return by_html[parameters]
        if len(states) >= max_states:
            truncated = True
            return None
        index = len(states)
        states.append({"settings": settings, "html": parameters, "next": {}})
        by_html[parameters] = index
        structure = frozenset(_PARAMETER.findall(parameters))
        novel = structure not in shown
        shown.add(structure)
        heapq.heappush(queue, (len(settings), 0 if novel else 1, index))
        return index

    add({}, first)
    while queue and not truncated:
        state = states[heapq.heappop(queue)[2]]
        for match in _SETTING.finditer(state["html"]):
            name, value = html.unescape(match.group(1)), html.unescape(match.group(2))
            key = f"{name}={value}"
            if key in state["next"]:
                continue
            settings = {**state["settings"], name: value}
            index = add(settings, split_page(render(settings))[1])
            if index is None:
                break
            state["next"][key] = index
    return shell, states, truncated


def _pieces(states: list[dict]) -> tuple[list[str], list[list[int]]]:
    """The states' parameters, split into their parameter groups, each stored once: the groups, and each state's."""
    pieces: list[str] = []
    by_piece: dict[str, int] = {}
    layouts = []
    for state in states:
        layout = []
        for piece in re.split(r"(?=<os-parameter-group>)", state["html"]):
            if not piece:
                continue
            if piece not in by_piece:
                by_piece[piece] = len(pieces)
                pieces.append(piece)
            layout.append(by_piece[piece])
        layouts.append(layout)
    return pieces, layouts


def render_markdown(text: str) -> str:
    return MarkdownIt("commonmark", {"html": False}).enable("table").enable("strikethrough").render(text)


def audit_page(title: str, source: str, shell: str, states: list[dict], truncated: bool, writeup: str | None,
               problems: list[str], theme: str, tables: list[Table] | None = None) -> str:
    """The audit page: the dialog (in a frame of its own, as its styles are Onshape's), the lookup tables its
    parameters use (every option at once, with its values), the writeup, and problems."""
    pieces, layouts = _pieces(states)
    data = {
        "pieces": pieces,
        "states": [
            {"settings": sorted(f"{name}={value}" for name, value in state["settings"].items()), "layout": layout, "next": state["next"]}
            for state, layout in zip(states, layouts)
        ],
    }
    states_json = json.dumps(data, separators=(",", ":")).replace("</", "<\\/")
    dialog = (
        shell.replace("\0", states[0]["html"])
        .replace("<body>", "<body>" + _DIALOG_TOOLBAR, 1)
        .replace("</body>", f"<script type='application/json' id='fs-states'>{states_json}</script><script>{_DIALOG_SCRIPT}</script></body>", 1)
        .replace("<style>", "<style>" + _DIALOG_STYLE, 1)
    )
    limit = (
        f"{len(states)} states pre-rendered: the limit, so some choices aren't (raise it with --max-states)."
        if truncated
        else f"All {len(states)} states its choices reach are pre-rendered."
    )
    problems_html = (
        "<ul class='problems'>" + "".join(f"<li><code>{html.escape(problem)}</code></li>" for problem in problems) + "</ul>"
        if problems
        else "<p>No problems found.</p>"
    )
    if tables:
        tables_html = (
            "<p class='limit'>Every option, a row per path through them, with its values; each level's default is bold.</p>"
            "<input id='table-filter' type='search' placeholder='Filter rows (all words must match)'>"
            "<div class='lookup'>" + "".join(table_section(table, f"table-{index}", "h3") for index, table in enumerate(tables)) + "</div>"
        )
    else:
        tables_html = "<p>Its parameters use no lookup tables.</p>"
    writeup_html = render_markdown(writeup) if writeup is not None else "<p>It has no writeup (a <code>.md</code> beside it).</p>"
    return _PAGE.format(
        title=html.escape(title),
        source=html.escape(source),
        theme=theme,
        dialog=html.escape(dialog, quote=True),
        limit=html.escape(limit),
        problems=problems_html,
        writeup=writeup_html,
        tables=tables_html,
        table_style=TABLE_STYLE,
    )


_DIALOG_STYLE = """
.fs-toolbar { display: flex; align-items: center; gap: 8px; margin: 0 0 8px; font: 12px system-ui, sans-serif; color: #b8bcc4; }
html[data-os-theme='light'] .fs-toolbar { color: #4b5563; }
.fs-toolbar #fs-settings { flex: 1; overflow-wrap: anywhere; }
.fs-toolbar button { font: inherit; padding: 2px 8px; cursor: pointer; }
#fs-note { margin: 0 0 8px; font: 12px system-ui, sans-serif; color: #f59e0b; }
/* A long press shows a tooltip, rather than selecting text or opening the browser's menu */
#feature-dialog { -webkit-touch-callout: none; -webkit-user-select: none; user-select: none; }
"""

_DIALOG_TOOLBAR = (
    "<div class='fs-toolbar'><span id='fs-settings'>Defaults</span><button id='fs-reset' hidden>Reset</button></div>"
    "<div id='fs-note' hidden></div>"
)

# Adapted from the VS Code extension's preview (vscode-extension/src/uiPreview.ts), which renders each change instead
_DIALOG_SCRIPT = r"""
const data = JSON.parse(document.getElementById("fs-states").textContent);
const states = data.states;
const bySettings = new Map(states.map((state, index) => [state.settings.join("\n"), index]));
const dialog = document.getElementById("feature-dialog");
const list = dialog.querySelector("os-parameter-list-view");
const settingsText = document.getElementById("fs-settings");
const note = document.getElementById("fs-note");
const reset = document.getElementById("fs-reset");
const toggled = {};
let current = 0;

// The frame fits the dialog, and any open menu (which is fixed in place, so not in the page's height)
function reportHeight() {
  let bottom = dialog.getBoundingClientRect().bottom + window.scrollY;
  for (const menu of dialog.querySelectorAll(".os-select-dropdown.open")) {
    bottom = Math.max(bottom, menu.getBoundingClientRect().bottom + window.scrollY);
  }
  parent.postMessage({ fsAuditHeight: Math.ceil(bottom) + 12 }, "*");
}

function show(index) {
  current = index;
  hideTooltip();
  list.innerHTML = states[index].layout.map((piece) => data.pieces[piece]).join("");
  for (const [key, open] of Object.entries(toggled)) {
    const expander = expanderFor(key);
    if (expander) {
      setOpen(expander, open);
    }
  }
  const settings = states[index].settings;
  settingsText.textContent = settings.length ? settings.join(", ") : "Defaults";
  reset.hidden = index === 0;
  reportHeight();
}

function set(name, value) {
  let next = states[current].next[name + "=" + value];
  if (next === undefined) {
    // Reached another way
    const settings = states[current].settings.filter((setting) => !setting.startsWith(name + "=")).concat([name + "=" + value]).sort();
    next = bySettings.get(settings.join("\n"));
  }
  if (next === undefined) {
    note.textContent = "Not pre-rendered: " + name + "=" + value + ". Render it with fs ui --set, or raise fs audit's --max-states.";
    note.hidden = false;
  } else {
    note.hidden = true;
    show(next);
  }
  reportHeight();
}

reset.addEventListener("click", () => { note.hidden = true; show(0); });

function keyOf(expander) {
  const group = expander.closest("[data-group]");
  if (expander.classList.contains("os-param-group-expander") && group) {
    return "group:" + group.dataset.group;
  }
  const item = expander.closest(".os-param-array-item");
  if (item) {
    return "item:" + item.dataset.parentParameterId + ":" + [...item.parentElement.children].indexOf(item);
  }
  return undefined;
}

function expanderFor(key) {
  return [...dialog.querySelectorAll("[data-toggle]")].find((expander) => keyOf(expander) === key);
}

function contentsOf(expander) {
  const item = expander.closest(".os-param-array-item");
  if (item && !expander.classList.contains("os-param-group-expander")) {
    return item.querySelector(".os-param-array-item-contents");
  }
  return expander.closest(".os-param-group-collapsible-container").querySelector(".os-param-group-collapsible-contents");
}

function setOpen(expander, open) {
  contentsOf(expander).classList.toggle("ng-hide", !open);
  expander.querySelector("svg").classList.toggle("expanded", open);
}

function placeMenu(toggle, menu) {
  const box = toggle.getBoundingClientRect();
  const below = window.innerHeight - box.bottom;
  const up = menu.scrollHeight > below && box.top > below;
  menu.style.left = box.left + "px";
  menu.style.minWidth = box.width + "px";
  menu.style.maxHeight = Math.min(300, (up ? box.top : below) - 4) + "px";
  menu.style.top = up ? "auto" : box.bottom + "px";
  menu.style.bottom = up ? window.innerHeight - box.top + "px" : "auto";
}

function closeMenus() {
  for (const menu of dialog.querySelectorAll(".os-select-dropdown.open")) {
    menu.classList.remove("open");
    menu.classList.add("ng-hide");
  }
}

function expanderAt(target) {
  const expander = target.closest("[data-toggle]");
  if (expander) {
    return expander;
  }
  const header = target.closest(".os-param-group-header, .os-param-selection-list-entry");
  if (!header || target.closest("[data-set], [data-remove]")) {
    return null;
  }
  return header.querySelector("[data-toggle]");
}

document.addEventListener("click", (event) => {
  const target = event.target;
  const option = target.closest(".os-select-choices-row[data-set]");
  if (option) {
    closeMenus();
    set(option.dataset.set, option.dataset.value);
    return;
  }
  const toggle = target.closest(".os-select-toggle");
  if (toggle) {
    const menu = toggle.closest(".os-select-container").querySelector(".os-select-dropdown");
    const open = menu.classList.contains("open");
    closeMenus();
    if (!open) {
      menu.classList.remove("ng-hide");
      menu.classList.add("open");
      placeMenu(toggle, menu);
    }
    reportHeight();
    return;
  }
  closeMenus();
  const expander = expanderAt(target);
  if (expander) {
    const open = !expander.querySelector("svg").classList.contains("expanded");
    setOpen(expander, open);
    const key = keyOf(expander);
    if (key) {
      toggled[key] = open;
    }
    reportHeight();
    return;
  }
  if (target.closest("[data-remove]")) {
    note.textContent = "Adding and removing array items isn't pre-rendered: use fs ui --set.";
    note.hidden = false;
    return;
  }
  const control = target.closest("[data-set]");
  if (control && control.dataset.value !== undefined) {
    event.preventDefault();
    set(control.dataset.set, control.dataset.value);
  }
});

document.addEventListener("change", (event) => {
  if (event.target.matches("input[data-set]:not([type=checkbox])")) {
    note.textContent = "Typed values aren't rendered here: use fs ui --set " + event.target.dataset.set + "=...";
    note.hidden = false;
    reportHeight();
  }
});

// Tooltips: a parameter's name and description, then its default and UI hints, as the preview shows them
const tooltip = document.createElement("div");
tooltip.className = "fs-tooltip";
tooltip.hidden = true;
document.body.appendChild(tooltip);
let tooltipFor = null;
let tooltipTimer;

function hideTooltip() {
  clearTimeout(tooltipTimer);
  tooltip.hidden = true;
  tooltipFor = null;
}

function line(className, text) {
  const element = document.createElement("div");
  element.className = className;
  element.textContent = text;
  return element;
}

function showTooltip(parameter) {
  const tip = parameter.dataset;
  const parts = [line("fs-tooltip-name", tip.tipName)];
  if (tip.tipDescription) {
    parts.push(line("fs-tooltip-description", tip.tipDescription));
  }
  const details = document.createElement("dl");
  details.className = "fs-tooltip-details";
  for (const [term, value] of [["Default", tip.tipDefault], ["UI hints", tip.tipHints]]) {
    if (value) {
      const dt = document.createElement("dt");
      dt.textContent = term;
      const dd = document.createElement("dd");
      dd.textContent = value;
      details.append(dt, dd);
    }
  }
  if (details.childElementCount) {
    parts.push(details);
  }
  tooltip.replaceChildren(...parts);
  tooltip.hidden = false;
  const box = parameter.getBoundingClientRect();
  const size = tooltip.getBoundingClientRect();
  let left = box.right + 4;
  let top = box.top;
  if (left + size.width > window.innerWidth) {
    left = Math.max(0, Math.min(box.left, window.innerWidth - size.width));
    top = box.bottom + 4;
  }
  tooltip.style.left = left + "px";
  tooltip.style.top = Math.max(0, top) + "px";
}

// With a mouse, hovering shows a tooltip
document.addEventListener("pointerover", (event) => {
  if (event.pointerType !== "mouse") {
    return;
  }
  const parameter = event.target.closest(".os-select-dropdown.open") ? null : event.target.closest("[data-tip-name]");
  if (parameter === tooltipFor) {
    return;
  }
  hideTooltip();
  tooltipFor = parameter;
  if (parameter) {
    tooltipTimer = setTimeout(() => showTooltip(parameter), 500);
  }
});

// By touch, a tap chooses, as it does in Onshape, and a long press shows the tooltip instead (the tap it ends with is
// ignored); the next touch hides it
const LONG_PRESS_MS = 500;
let pressTimer;
let pressStart = null;
let longPressed = false;
document.addEventListener("pointerdown", (event) => {
  if (event.pointerType === "mouse") {
    return;
  }
  hideTooltip();
  const parameter = event.target.closest(".os-select-dropdown.open") ? null : event.target.closest("[data-tip-name]");
  if (!parameter) {
    return;
  }
  pressStart = [event.clientX, event.clientY];
  pressTimer = setTimeout(() => {
    longPressed = true;
    tooltipFor = parameter;
    showTooltip(parameter);
  }, LONG_PRESS_MS);
});
document.addEventListener("pointermove", (event) => {
  if (pressStart && Math.hypot(event.clientX - pressStart[0], event.clientY - pressStart[1]) > 10) {
    clearTimeout(pressTimer);
    pressStart = null;
  }
});
for (const type of ["pointerup", "pointercancel"]) {
  document.addEventListener(type, () => {
    clearTimeout(pressTimer);
    pressStart = null;
  });
}
document.addEventListener("click", (event) => {
  if (longPressed) {
    longPressed = false;
    event.preventDefault();
    event.stopImmediatePropagation();
  }
}, true);
document.addEventListener("contextmenu", (event) => {
  if (longPressed || pressStart) {
    event.preventDefault();
  }
});

window.addEventListener("load", reportHeight);
reportHeight();
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
.limit {{ color: var(--muted); font-size: 12px; margin: 4px 0; }}
iframe {{ width: 100%; max-width: 420px; height: 600px; border: 1px solid var(--line); border-radius: 6px; display: block; }}
.writeup table {{ border-collapse: collapse; font-size: 13px; display: block; overflow-x: auto; }}
.writeup th, .writeup td {{ border: 1px solid var(--line); padding: 4px 8px; text-align: left; vertical-align: top; }}
.writeup th {{ background: var(--head); }}
.writeup code {{ background: var(--code); padding: 1px 4px; border-radius: 3px; font-size: 12px; }}
.writeup pre {{ background: var(--code); padding: 8px; border-radius: 6px; overflow-x: auto; }}
.writeup pre code {{ padding: 0; }}
.writeup h1 {{ font-size: 18px; }}
.writeup h2 {{ font-size: 16px; margin-top: 24px; }}
.writeup h3 {{ font-size: 14px; }}
.problems code {{ background: var(--code); padding: 1px 4px; border-radius: 3px; }}
#table-filter {{ width: 100%; max-width: 420px; box-sizing: border-box; padding: 6px 8px; margin-bottom: 8px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--fg); }}
.lookup h3 {{ font-size: 14px; margin: 16px 0 2px; font-family: ui-monospace, monospace; }}
{table_style}
</style>
</head>
<body>
<main>
<h1>{title}</h1>
<p class="source">{source}</p>
<nav><a href="#dialog">Dialog</a><a href="#tables">Lookup tables</a><a href="#writeup">Writeup</a><a href="#problems">Problems</a></nav>
<section id="dialog">
<h2>Dialog</h2>
<p class="limit">Click dropdowns, checkboxes, tabs, and lookup table levels to see what each choice shows. Hover a parameter for its description, default, and UI hints. {limit}</p>
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
const tableFilter = document.getElementById("table-filter");
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
