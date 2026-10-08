import * as crypto from "node:crypto";
import * as path from "node:path";
import * as vscode from "vscode";
import type { LanguageClient } from "vscode-languageclient/node";

/** Features a FeatureScript defines: `export const name = defineFeature(...)`. */
const FEATURE = /\bexport\s+const\s+(\w+)\s*=\s*defineFeature\s*\(/g;

export function featureNames(text: string): string[] {
  return [...text.matchAll(FEATURE)].map((match) => match[1]!);
}

/** What the language server's featurescript/renderUi request returns (see fs_lsp/server.py). */
interface RenderResult {
  html?: string;
  error?: string;
  warnings?: string[];
  features: string[];
  feature?: string;
}

// How long to wait after an edit before rendering it
const EDIT_DELAY_MS = 250;

/**
 * A panel showing a feature's dialog as `fs ui` renders it (with Onshape's own markup and styles), rendered by the
 * language server, which keeps the parsed std library between renders. It follows edits to the file, saved or not.
 *
 * The dialog works like Onshape's: clicking a tab, checkbox, button, or dropdown option, or entering a value, changes
 * that parameter (as `fs ui --set` does), which shows and hides the parameters which depend on it; groups and array
 * items open and close; and array items can be added and removed.
 */
export class UiPreview {
  private static readonly previews = new Map<string, UiPreview>();

  private feature: string | undefined;
  private readonly settings = new Map<string, string>();
  private rendering = 0;
  private pendingEdit: NodeJS.Timeout | undefined;

  static show(uri: vscode.Uri, client: () => LanguageClient | undefined): void {
    const existing = UiPreview.previews.get(uri.fsPath);
    if (existing) {
      existing.panel.reveal(vscode.ViewColumn.Beside, true);
      return;
    }
    const panel = vscode.window.createWebviewPanel(
      "featurescriptUiPreview",
      `Preview ${path.basename(uri.fsPath)}`,
      { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true },
      { enableScripts: true, localResourceRoots: [], retainContextWhenHidden: true }
    );
    UiPreview.previews.set(uri.fsPath, new UiPreview(panel, uri, client));
  }

  /** Re-renders a document's preview (debounced while it's being edited). */
  static changed(document: vscode.TextDocument): void {
    UiPreview.previews.get(document.uri.fsPath)?.renderSoon();
  }

  /** Re-renders every preview, e.g. when the color theme changes. */
  static renderAll(): void {
    for (const preview of UiPreview.previews.values()) {
      void preview.render();
    }
  }

  private constructor(
    private readonly panel: vscode.WebviewPanel,
    private readonly uri: vscode.Uri,
    private readonly client: () => LanguageClient | undefined
  ) {
    panel.onDidDispose(() => {
      UiPreview.previews.delete(uri.fsPath);
      clearTimeout(this.pendingEdit);
    });
    panel.webview.onDidReceiveMessage((message: Message) => this.receive(message));
    panel.webview.html = this.shell();
  }

  private receive(message: Message): void {
    switch (message.type) {
      case "ready":
        break;
      case "set":
        this.set(message.name, message.value);
        break;
      case "remove":
        this.removeItem(message.name, message.index);
        break;
      case "feature":
        this.feature = message.value;
        this.settings.clear();
        break;
      case "reset":
        this.settings.clear();
        break;
      default:
        return;
    }
    void this.render();
  }

  private set(name: string, value: string): void {
    this.settings.set(name, value);
    // Shrinking an array forgets its removed items' settings
    if (/^\d+$/.test(value)) {
      for (const key of [...this.settings.keys()]) {
        const index = itemIndex(key, name);
        if (index !== undefined && index >= Number(value)) {
          this.settings.delete(key);
        }
      }
    }
  }

  /** Removes an array's item, moving the settings of the items after it up. */
  private removeItem(name: string, index: number): void {
    const count = Number(this.settings.get(name) ?? "0");
    const moved = new Map<string, string>();
    for (const [key, value] of this.settings) {
      const item = itemIndex(key, name);
      if (item === undefined || item < index) {
        moved.set(key, value);
      } else if (item > index) {
        moved.set(`${name}.${item - 1}${key.slice(`${name}.${item}`.length)}`, value);
      }
    }
    moved.set(name, String(Math.max(0, count - 1)));
    this.settings.clear();
    for (const [key, value] of moved) {
      this.settings.set(key, value);
    }
  }

  private renderSoon(): void {
    clearTimeout(this.pendingEdit);
    this.pendingEdit = setTimeout(() => void this.render(), EDIT_DELAY_MS);
  }

  private async render(): Promise<void> {
    const rendering = ++this.rendering;
    const client = this.client();
    let result: RenderResult;
    if (!client) {
      result = { error: "The FeatureScript language server isn't running.", features: [] };
    } else {
      try {
        result = await client.sendRequest<RenderResult>("featurescript/renderUi", {
          uri: this.uri.toString(),
          feature: this.feature,
          settings: [...this.settings],
          theme: isLightTheme() ? "light" : "dark"
        });
      } catch (error) {
        result = { error: error instanceof Error ? error.message : String(error), features: [] };
      }
    }
    // A newer render started while this one ran
    if (rendering !== this.rendering) {
      return;
    }
    this.feature = result.feature ?? this.feature;
    void this.panel.webview.postMessage({
      type: "render",
      ...result,
      settings: [...this.settings].map(([name, value]) => `${name}=${value}`)
    });
  }

  /** The page the dialog is shown in: the dialog, a toolbar under it, and a script which shows renders and sends changes back. */
  private shell(): string {
    const nonce = crypto.randomBytes(16).toString("base64");
    const csp = `default-src 'none'; style-src 'unsafe-inline'; img-src data:; script-src 'nonce-${nonce}';`;
    return (
      `<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="${csp}">` +
      `<style>${TOOLBAR_STYLE}</style><style id="dialog-style"></style></head><body><div id="dialog"></div>` +
      `<div class="toolbar"><select id="feature" hidden></select><span id="settings"></span>` +
      `<button id="reset" hidden>Reset</button></div><pre id="messages" hidden></pre>` +
      `<script nonce="${nonce}">${SCRIPT}</script></body></html>`
    );
  }
}

type Message =
  | { type: "ready" }
  | { type: "set"; name: string; value: string }
  | { type: "remove"; name: string; index: number }
  | { type: "feature"; value: string }
  | { type: "reset" };

/** The index of the array item a setting is for (like 1 for `items.1.length`), if it's for one of `array`'s. */
function itemIndex(key: string, array: string): number | undefined {
  const match = key.startsWith(`${array}.`) ? /^(\d+)\./.exec(key.slice(array.length + 1)) : null;
  return match ? Number(match[1]) : undefined;
}

function isLightTheme(): boolean {
  const kind = vscode.window.activeColorTheme.kind;
  return kind === vscode.ColorThemeKind.Light || kind === vscode.ColorThemeKind.HighContrastLight;
}

const TOOLBAR_STYLE = `
body { margin: 0; }
.toolbar { display: flex; align-items: center; gap: 8px; padding: 6px 10px; font: 12px var(--vscode-font-family, sans-serif); color: var(--vscode-foreground); }
.toolbar #settings { flex: 1; color: var(--vscode-descriptionForeground); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.toolbar button, .toolbar select { background: var(--vscode-button-secondaryBackground); color: var(--vscode-button-secondaryForeground); border: 1px solid var(--vscode-contrastBorder, transparent); border-radius: 2px; }
#messages { color: var(--vscode-editorWarning-foreground); white-space: pre-wrap; font: 11px var(--vscode-editor-font-family, monospace); margin: 0 10px 6px; }
`;

/**
 * Shows renders, and sends changes back. Groups and array items are opened and closed here, and which were toggled
 * is kept across renders (and reloads).
 */
const SCRIPT = `
const vscode = acquireVsCodeApi();
const state = vscode.getState() || { toggled: {} };
const dialog = document.getElementById("dialog");
const style = document.getElementById("dialog-style");
const messages = document.getElementById("messages");
const featureSelect = document.getElementById("feature");
const reset = document.getElementById("reset");

featureSelect.addEventListener("change", () => vscode.postMessage({ type: "feature", value: featureSelect.value }));
reset.addEventListener("click", () => vscode.postMessage({ type: "reset" }));

window.addEventListener("message", (event) => {
  const message = event.data;
  if (message.type !== "render") {
    return;
  }
  featureSelect.replaceChildren(...message.features.map((name) => new Option(name, name, false, name === message.feature)));
  featureSelect.hidden = message.features.length < 2;
  document.getElementById("settings").textContent = message.settings.join(", ");
  reset.hidden = message.settings.length === 0;
  const notes = [message.error, ...(message.warnings || [])].filter(Boolean);
  messages.textContent = notes.join("\\n");
  messages.hidden = notes.length === 0;
  if (message.html) {
    const page = new DOMParser().parseFromString(message.html, "text/html");
    document.documentElement.setAttribute("data-os-theme", page.documentElement.getAttribute("data-os-theme") || "dark");
    const css = page.querySelector("style").textContent;
    if (style.textContent !== css) {
      style.textContent = css;
    }
    hideTooltip();
    dialog.replaceChildren(...page.body.childNodes);
    for (const [key, open] of Object.entries(state.toggled)) {
      const expander = expanderFor(key);
      if (expander) {
        setOpen(expander, open);
      }
    }
  }
});

/** A key for an expander, to remember whether it's open: its group's name, or its array item's position. */
function keyOf(expander) {
  const group = expander.closest("[data-group]");
  if (expander.classList.contains("os-param-group-expander") && group) {
    return "group:" + group.dataset.group;
  }
  const item = expander.closest(".os-param-array-item");
  if (item) {
    const index = [...item.parentElement.children].indexOf(item);
    return "item:" + item.dataset.parentParameterId + ":" + index;
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

function openMenu(toggle, menu) {
  menu.classList.remove("ng-hide");
  menu.classList.add("open");
  placeMenu(toggle, menu);
}

/**
 * Places a dropdown's open menu under it, or over it if there's more room there. It's fixed in place, as Onshape's
 * float above the dialog, since the dialog's parameter list would cut it off; so it's placed again as the page
 * scrolls.
 */
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

function placeMenus() {
  for (const menu of dialog.querySelectorAll(".os-select-dropdown.open")) {
    placeMenu(menu.closest(".os-select-container").querySelector(".os-select-toggle"), menu);
  }
}

window.addEventListener("resize", placeMenus);
window.addEventListener("scroll", (event) => {
  // Not when the menu itself scrolls
  if (!(event.target instanceof Element && event.target.closest(".os-select-dropdown"))) {
    placeMenus();
  }
}, true);

/**
 * The expander a click opens or closes: the one clicked, or the one in the group or array item header clicked (as
 * clicking a group's name does in Onshape), but not through the header's checkbox or delete button.
 */
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

function send(control) {
  vscode.postMessage({ type: "set", name: control.dataset.set, value: control.dataset.value ?? control.value });
}

document.addEventListener("click", (event) => {
  const target = event.target;
  const option = target.closest(".os-select-choices-row[data-set]");
  if (option) {
    closeMenus();
    send(option);
    return;
  }
  const toggle = target.closest(".os-select-toggle");
  if (toggle) {
    const menu = toggle.closest(".os-select-container").querySelector(".os-select-dropdown");
    const open = menu.classList.contains("open");
    closeMenus();
    if (!open) {
      openMenu(toggle, menu);
    }
    return;
  }
  closeMenus();
  const expander = expanderAt(target);
  if (expander) {
    const key = keyOf(expander);
    const open = !expander.querySelector("svg").classList.contains("expanded");
    setOpen(expander, open);
    if (key) {
      state.toggled[key] = open;
      vscode.setState(state);
    }
    return;
  }
  const remove = target.closest("[data-remove]");
  if (remove) {
    vscode.postMessage({ type: "remove", name: remove.dataset.remove, index: Number(remove.dataset.index) });
    return;
  }
  const control = target.closest("[data-set]");
  if (control && control.tagName !== "INPUT") {
    // Checkboxes change when the dialog is rendered with the new value
    event.preventDefault();
    send(control);
  }
});

// Values are set when they're entered: Enter commits a value (which changes it, if it's new)
document.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && event.target.matches("input[data-set]")) {
    event.target.blur();
  }
});
document.addEventListener("change", (event) => {
  if (event.target.matches("input[data-set]:not([type=checkbox])")) {
    send(event.target);
  }
});

// Clicking into a number selects all of it, as in Onshape (but not clicking in one that's already being edited)
let editing = null;
document.addEventListener("mousedown", (event) => {
  editing = document.activeElement;
  hideTooltip();
});
document.addEventListener("click", (event) => {
  const input = event.target.closest("input.os-param-number[data-set]");
  if (input && input !== editing) {
    input.select();
  }
});

/**
 * Tooltips: hovering over a parameter shows its name (and a number's value) and description, as Onshape's tooltips
 * do, beside it, then its default and UI hints.
 */
const TOOLTIP_DELAY_MS = 500;
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

document.addEventListener("mouseover", (event) => {
  // Not over open dropdowns, which cover other parameters
  const parameter = event.target.closest(".os-select-dropdown.open") ? null : event.target.closest("[data-tip-name]");
  if (parameter === tooltipFor) {
    return;
  }
  hideTooltip();
  tooltipFor = parameter;
  if (parameter) {
    tooltipTimer = setTimeout(() => showTooltip(parameter), TOOLTIP_DELAY_MS);
  }
});
document.documentElement.addEventListener("mouseleave", hideTooltip);
window.addEventListener("scroll", hideTooltip, true);

function line(className, text) {
  const element = document.createElement("div");
  element.className = className;
  element.textContent = text;
  return element;
}

function showTooltip(parameter) {
  const tip = parameter.dataset;
  const number = parameter.querySelector("input.os-param-number");
  const parts = [line("fs-tooltip-name", tip.tipName + (number && number.value ? ": " + number.value : ""))];
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
  // Beside the parameter's control, centered on it, or under it if there's no room beside it
  const control = parameter.querySelector(
    "input, .os-select-container, .os-param-tabs, label.os-param-checkbox, .os-param-query-list, table, osc-svg-icon, .os-param-container"
  ) || parameter;
  const box = control.getBoundingClientRect();
  const size = tooltip.getBoundingClientRect();
  let left = box.right + 4;
  let top = box.top + (box.height - size.height) / 2;
  if (left + size.width > window.innerWidth) {
    left = Math.max(0, Math.min(box.left, window.innerWidth - size.width));
    top = box.bottom + 4;
    if (top + size.height > window.innerHeight) {
      top = box.top - 4 - size.height;
    }
  }
  tooltip.style.left = left + "px";
  tooltip.style.top = Math.max(0, top) + "px";
}

vscode.postMessage({ type: "ready" });
`;
