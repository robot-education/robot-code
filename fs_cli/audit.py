"""`fs audit`: one page to audit a feature by: its dialog, which works (clicking dropdowns, checkboxes, tabs, and lookup
table levels, and typing values, shows the dialog as it would be), its writeup, and its file's `fs check` problems.

The dialog is drawn by the page's script (see `fs_cli.live_dialog`): its precondition's conditions are compiled to
JavaScript, and each parameter is rendered ahead of time for each of its values, so the script draws any combination of
choices as `fs ui --set` would, as they're made. Settings are listed above the dialog: a parameter set back to its
default leaves the list, and removing one from the list sets it back.
"""

from __future__ import annotations

import html

from markdown_it import MarkdownIt

from fs_cli.lookup_tables import TABLE_STYLE, Table, table_section

# Where a dialog's parameters are, in a page `fs ui` renders
LIST_START = "<os-parameter-list-view>"
LIST_END = "</os-parameter-list-view>"


def split_page(page: str) -> tuple[str, str]:
    """A rendered dialog page's shell (with `\\0` where its parameters go) and its parameters."""
    start = page.index(LIST_START) + len(LIST_START)
    end = page.index(LIST_END, start)
    return page[:start] + "\0" + page[end:], page[start:end]


def render_markdown(text: str) -> str:
    return MarkdownIt("commonmark", {"html": False}).enable("table").enable("strikethrough").render(text)


def audit_page(title: str, source: str, shell: str, script: str, writeup: str | None, problems: list[str], theme: str,
               tables: list[Table] | None = None) -> str:
    """The audit page: the dialog (in a frame of its own, as its styles are Onshape's), drawn by `script` (see
    `fs_cli.live_dialog.page_script`) in `shell` (a page `fs ui` renders, see `split_page`), the lookup tables its
    parameters use (every option at once), the writeup, and problems."""
    dialog = (
        shell.replace("\0", "")
        .replace("<body>", "<body>" + _DIALOG_TOOLBAR, 1)
        .replace("</body>", f"<script>{script}\n{_DIALOG_SCRIPT}</script></body>", 1)
        .replace("<style>", "<style>" + _DIALOG_STYLE, 1)
    )
    problems_html = (
        "<ul class='problems'>" + "".join(f"<li><code>{html.escape(problem)}</code></li>" for problem in problems) + "</ul>"
        if problems
        else "<p>No problems found.</p>"
    )
    if tables:
        tables_html = (
            "<p class='limit'>Every option, a row per path through its levels, in the dialog's order. ★ marks each level's "
            "default (where the dialog goes when you choose the level above), and the bold row is what it starts with.</p>"
            "<input id='table-filter' type='search' placeholder='Filter rows (all words must match)'>"
            "<div class='lookup'>" + "".join(table_section(table, f"table-{index}", "h3", values=False) for index, table in enumerate(tables)) + "</div>"
        )
    else:
        tables_html = "<p>Its parameters use no lookup tables.</p>"
    writeup_html = render_markdown(writeup) if writeup is not None else "<p>It has no writeup (a <code>.md</code> beside it).</p>"
    return _PAGE.format(
        title=html.escape(title),
        source=html.escape(source),
        theme=theme,
        dialog=html.escape(dialog, quote=True),
        problems=problems_html,
        writeup=writeup_html,
        tables=tables_html,
        table_style=TABLE_STYLE,
    )


_DIALOG_STYLE = """
.fs-toolbar { display: flex; align-items: flex-start; gap: 8px; margin: 0 0 8px; font: 12px system-ui, sans-serif; color: #b8bcc4; }
html[data-os-theme='light'] .fs-toolbar { color: #4b5563; }
.fs-toolbar #fs-settings { flex: 1; display: flex; flex-wrap: wrap; gap: 4px; min-height: 20px; align-items: center; }
.fs-toolbar button { font: inherit; padding: 2px 8px; cursor: pointer; }
.fs-chip { display: inline-flex; align-items: center; gap: 4px; padding: 1px 4px 1px 8px; border: 1px solid #4b5563; border-radius: 10px; overflow-wrap: anywhere; }
.fs-chip.fs-hidden { opacity: 0.55; font-style: italic; }
.fs-chip button { border: 0; background: none; color: inherit; padding: 0 4px; font-size: 14px; line-height: 1; }
#fs-note { margin: 0 0 8px; font: 12px system-ui, sans-serif; color: #f59e0b; }
/* A long press shows a tooltip, rather than selecting text or opening the browser's menu */
#feature-dialog { -webkit-touch-callout: none; -webkit-user-select: none; user-select: none; }
"""

_DIALOG_TOOLBAR = (
    "<div class='fs-toolbar'><span id='fs-settings'>Defaults</span><button id='fs-reset' hidden>Reset all</button></div>"
    "<div id='fs-note' hidden></div>"
)

# The page's glue: clicks set settings, and the dialog is drawn again (`renderList`, from fs_cli.live_dialog.RUNTIME)
_DIALOG_SCRIPT = r"""
const dialog = document.getElementById("feature-dialog");
const list = dialog.querySelector("os-parameter-list-view");
const settingsList = document.getElementById("fs-settings");
const note = document.getElementById("fs-note");
const reset = document.getElementById("fs-reset");
// What's been set (as fs ui --set takes it), and which groups and items have been opened or closed
const settings = {};
const toggled = {};

// A setting as the dialog shows it: an enum by its name, a lookup table's path, or what was typed
function describe(key) {
  const parameter = DIALOG.parameters[key];
  return parameter.name + " = " + settings[key];
}

function showSettings(shown) {
  settingsList.replaceChildren();
  const keys = Object.keys(settings).sort();
  for (const key of keys) {
    const chip = document.createElement("span");
    chip.className = "fs-chip" + (shown.has(key) ? "" : " fs-hidden");
    chip.title = shown.has(key) ? "Set; × sets it back to its default" : "Set, but not shown now; × sets it back to its default";
    chip.append(describe(key));
    const remove = document.createElement("button");
    remove.textContent = "×";
    remove.dataset.unset = key;
    chip.append(remove);
    settingsList.append(chip);
  }
  if (!keys.length) {
    settingsList.textContent = "Defaults";
  }
  reset.hidden = keys.length === 0;
}

// The frame fits the dialog, and any open menu (which is fixed in place, so not in the page's height)
function reportHeight() {
  let bottom = dialog.getBoundingClientRect().bottom + window.scrollY;
  for (const menu of dialog.querySelectorAll(".os-select-dropdown.open")) {
    bottom = Math.max(bottom, menu.getBoundingClientRect().bottom + window.scrollY);
  }
  parent.postMessage({ fsAuditHeight: Math.ceil(bottom) + 12 }, "*");
}

function draw() {
  hideTooltip();
  const result = renderList({ ...settings });
  list.innerHTML = result.html;
  for (const [key, open] of Object.entries(toggled)) {
    const expander = expanderFor(key);
    if (expander) {
      setOpen(expander, open);
    }
  }
  showSettings(new Set(result.shown));
  reportHeight();
}

// Setting a parameter to its default unsets it
function set(key, value) {
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
}

reset.addEventListener("click", () => {
  for (const key of Object.keys(settings)) {
    delete settings[key];
  }
  note.hidden = true;
  draw();
});

settingsList.addEventListener("click", (event) => {
  const remove = event.target.closest("[data-unset]");
  if (remove) {
    delete settings[remove.dataset.unset];
    draw();
  }
});

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

dialog.addEventListener("click", (event) => {
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
  if (target.closest("[data-remove], .os-param-array-add-button, .os-param-array-clear-button")) {
    note.textContent = "Array items aren't drawn here: use fs ui --set.";
    note.hidden = false;
    reportHeight();
    return;
  }
  if (target.closest("input[data-set]:not([type=checkbox])")) {
    return;
  }
  const control = target.closest("[data-set]");
  if (control && control.dataset.value !== undefined) {
    event.preventDefault();
    set(control.dataset.set, control.dataset.value);
  }
});

// Typed values
dialog.addEventListener("change", (event) => {
  if (event.target.matches("input[data-set]:not([type=checkbox])")) {
    set(event.target.dataset.set, event.target.value);
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
<p class="limit">Click dropdowns, checkboxes, tabs, and lookup table levels, or type values, to see what each choice shows: any combination works. What's set is listed above the dialog; × (or setting it back) returns it to its default. Hover a parameter for its description, default, and UI hints.</p>
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
