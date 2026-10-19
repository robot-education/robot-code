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
  settings?: [string, string][];
}

/** What its featurescript/previewPage request returns: the script making the dialog work, and the sections' style. */
interface PageResult {
  script: string;
  style: string;
}

/** What its featurescript/audit request returns: `fs audit`'s other sections. */
interface AuditResult {
  html?: { tables: string; writeup: string; problems: string };
  tables?: number;
  problems?: number;
  error?: string;
}

// How long to wait after an edit before rendering it
const EDIT_DELAY_MS = 250;

/**
 * A panel showing what `fs audit`'s page does: a feature's dialog as `fs ui` renders it (with Onshape's own markup and
 * styles), and tabs for the lookup tables its parameters use, its writeup, and its file's problems. The language
 * server renders them, keeping the parsed std library between renders. The dialog follows edits to the file, saved
 * or not, and the other tabs follow saved files.
 *
 * The dialog works like Onshape's (see fs_cli/dialog_page.js, which the server gives it): clicking a tab, checkbox,
 * button, or dropdown option, or entering a value, changes that parameter (as `fs ui --set` does), which shows and
 * hides the parameters which depend on it; groups and array items open and close; and array items can be added and
 * removed.
 */
export class UiPreview {
  private static readonly previews = new Map<string, UiPreview>();

  private readonly settings = new Map<string, string>();
  private rendering = 0;
  private auditing = 0;
  private pendingEdit: NodeJS.Timeout | undefined;

  /** Shows a file's preview, of `feature` if it's given (or the one it showed, or the file's first). */
  static show(uri: vscode.Uri, client: () => LanguageClient | undefined, feature?: string): void {
    const existing = UiPreview.previews.get(uri.fsPath);
    if (existing) {
      existing.panel.reveal(vscode.ViewColumn.Beside, true);
      if (feature && feature !== existing.feature) {
        existing.feature = feature;
        existing.settings.clear();
        void existing.render();
      }
      void existing.audit();
      return;
    }
    const panel = vscode.window.createWebviewPanel(
      "featurescriptUiPreview",
      `Preview ${path.basename(uri.fsPath)}`,
      { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true },
      { enableScripts: true, localResourceRoots: [], retainContextWhenHidden: true }
    );
    UiPreview.previews.set(uri.fsPath, new UiPreview(panel, uri, client, feature));
  }

  /** Re-renders a document's dialog (debounced while it's being edited). */
  static changed(document: vscode.TextDocument): void {
    UiPreview.previews.get(document.uri.fsPath)?.renderSoon();
  }

  /** Updates the other tabs when a file they read is saved: any FeatureScript (which can hold tables), or a writeup. */
  static saved(document: vscode.TextDocument): void {
    UiPreview.changed(document);
    if (document.languageId === "featurescript" || document.uri.fsPath.endsWith(".md")) {
      for (const preview of UiPreview.previews.values()) {
        void preview.audit();
      }
    }
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
    private readonly client: () => LanguageClient | undefined,
    private feature: string | undefined
  ) {
    panel.onDidDispose(() => {
      UiPreview.previews.delete(uri.fsPath);
      clearTimeout(this.pendingEdit);
    });
    panel.webview.onDidReceiveMessage((message: Message) => this.receive(message));
    void this.load();
  }

  /** Shows the page, with the script from the server. */
  private async load(): Promise<void> {
    try {
      const page = await this.request<PageResult>("featurescript/previewPage", {});
      this.panel.webview.html = shell(page);
    } catch (error) {
      this.panel.webview.html = errorPage(error instanceof Error ? error.message : String(error));
    }
  }

  private async request<T>(method: string, params: object): Promise<T> {
    const client = this.client();
    if (!client) {
      throw new Error("The FeatureScript language server isn't running.");
    }
    return client.sendRequest<T>(method, params);
  }

  private receive(message: Message): void {
    switch (message.type) {
      case "ready":
        void this.audit();
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
      case "reveal":
        void this.reveal(message.line, message.character);
        return;
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

  /** Shows a problem in the file's editor (or opens it beside the preview). */
  private async reveal(line: number, character: number): Promise<void> {
    const position = new vscode.Position(line, character);
    const editor = vscode.window.visibleTextEditors.find((visible) => visible.document.uri.fsPath === this.uri.fsPath);
    await vscode.window.showTextDocument(this.uri, {
      viewColumn: editor?.viewColumn ?? vscode.ViewColumn.Beside,
      selection: new vscode.Range(position, position)
    });
  }

  private renderSoon(): void {
    clearTimeout(this.pendingEdit);
    this.pendingEdit = setTimeout(() => void this.render(), EDIT_DELAY_MS);
  }

  private async render(): Promise<void> {
    const rendering = ++this.rendering;
    let result: RenderResult;
    try {
      result = await this.request<RenderResult>("featurescript/renderUi", {
        uri: this.uri.toString(),
        feature: this.feature,
        settings: [...this.settings],
        theme: isLightTheme() ? "light" : "dark"
      });
    } catch (error) {
      result = { error: error instanceof Error ? error.message : String(error), features: [] };
    }
    // A newer render started while this one ran
    if (rendering !== this.rendering) {
      return;
    }
    this.feature = result.feature ?? this.feature;
    // Without the settings which do nothing, like one set back to its default
    if (result.settings) {
      this.settings.clear();
      for (const [name, value] of result.settings) {
        this.settings.set(name, value);
      }
    }
    void this.panel.webview.postMessage({
      type: "render",
      ...result,
      settings: [...this.settings].map(([name, value]) => `${name}=${value}`)
    });
  }

  private async audit(): Promise<void> {
    const auditing = ++this.auditing;
    let result: AuditResult;
    try {
      result = await this.request<AuditResult>("featurescript/audit", { uri: this.uri.toString() });
    } catch (error) {
      result = { error: error instanceof Error ? error.message : String(error) };
    }
    if (auditing === this.auditing) {
      void this.panel.webview.postMessage({ type: "audit", ...result });
    }
  }
}

type Message =
  | { type: "ready" }
  | { type: "set"; name: string; value: string }
  | { type: "remove"; name: string; index: number }
  | { type: "feature"; value: string }
  | { type: "reset" }
  | { type: "reveal"; line: number; character: number };

/** The index of the array item a setting is for (like 1 for `items.1.length`), if it's for one of `array`'s. */
function itemIndex(key: string, array: string): number | undefined {
  const match = key.startsWith(`${array}.`) ? /^(\d+)\./.exec(key.slice(array.length + 1)) : null;
  return match ? Number(match[1]) : undefined;
}

function isLightTheme(): boolean {
  const kind = vscode.window.activeColorTheme.kind;
  return kind === vscode.ColorThemeKind.Light || kind === vscode.ColorThemeKind.HighContrastLight;
}

/** A script's text, safe in a page's script element. */
function scriptText(text: string): string {
  return text.replace(/<\//g, "<\\/");
}

function errorPage(message: string): string {
  const escaped = message.replace(/&/g, "&amp;").replace(/</g, "&lt;");
  return `<!doctype html><html><body><pre style="color: var(--vscode-errorForeground); white-space: pre-wrap">${escaped}</pre></body></html>`;
}

/**
 * The page the preview is shown in: tabs, the dialog (with a toolbar under it), the other sections (in a shadow root,
 * away from Onshape's styles), and a script which shows renders and sends changes back.
 */
function shell(page: PageResult): string {
  const nonce = crypto.randomBytes(16).toString("base64");
  const csp = `default-src 'none'; style-src 'unsafe-inline'; img-src data:; script-src 'nonce-${nonce}';`;
  return (
    `<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="${csp}">` +
    `<style>${PANEL_STYLE}</style><style id="dialog-style"></style></head><body>` +
    `<nav class="fs-tabs">${TABS.map(([tab, label]) => `<button data-tab="${tab}">${label}</button>`).join("")}</nav>` +
    `<div class="fs-page" data-page="dialog"><div id="dialog"></div>` +
    `<div class="toolbar"><select id="feature" hidden></select><span id="settings"></span>` +
    `<button id="reset" hidden>Reset</button></div><pre id="messages" hidden></pre></div>` +
    `<div class="fs-page" data-page="sections" id="sections" hidden></div>` +
    `<script nonce="${nonce}">const SECTION_STYLE = ${scriptText(JSON.stringify(SECTIONS_HOST_STYLE + page.style))};\n` +
    `${scriptText(page.script)}\n${SCRIPT}</script></body></html>`
  );
}

const TABS = [
  ["dialog", "Dialog"],
  ["tables", "Lookup tables"],
  ["writeup", "Writeup"],
  ["problems", "Problems"]
] as const;

const PANEL_STYLE = `
body { margin: 0; }
.fs-tabs { display: flex; flex-wrap: wrap; gap: 2px; margin: 0 0 10px; text-align: left; font: 12px var(--vscode-font-family, sans-serif); }
.fs-tabs button { background: none; color: var(--vscode-foreground); border: 0; border-bottom: 2px solid transparent; border-radius: 0; padding: 3px 8px; opacity: 0.7; font: inherit; cursor: pointer; }
.fs-tabs button[aria-selected="true"] { opacity: 1; border-bottom-color: var(--vscode-focusBorder); }
.fs-tabs button[hidden], .fs-page[hidden] { display: none; }
.toolbar { display: flex; align-items: center; gap: 8px; padding: 6px 10px; font: 12px var(--vscode-font-family, sans-serif); color: var(--vscode-foreground); text-align: left; }
.toolbar #settings { flex: 1; color: var(--vscode-descriptionForeground); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.toolbar button, .toolbar select { background: var(--vscode-button-secondaryBackground); color: var(--vscode-button-secondaryForeground); border: 1px solid var(--vscode-contrastBorder, transparent); border-radius: 2px; }
#messages { color: var(--vscode-editorWarning-foreground); white-space: pre-wrap; font: 11px var(--vscode-editor-font-family, monospace); margin: 0 10px 6px; text-align: left; }
`;

// The sections' page, in its shadow root: VS Code's colors, for the names SECTION_STYLE uses
const SECTIONS_HOST_STYLE = `
:host {
  all: initial; display: block; box-sizing: border-box; padding: 4px 14px 16px; border-radius: 6px;
  background: var(--bg); color: var(--fg); font: 13px/1.5 var(--vscode-font-family, system-ui, sans-serif); text-align: left;
  --bg: var(--vscode-editor-background); --fg: var(--vscode-editor-foreground); --muted: var(--vscode-descriptionForeground);
  --line: var(--vscode-panel-border, rgba(128, 128, 128, 0.35)); --head: var(--vscode-editorWidget-background);
  --code: var(--vscode-textCodeBlock-background); --accent: var(--vscode-textLink-foreground); --empty: var(--vscode-editorWarning-foreground);
}
[hidden] { display: none !important; }
a { color: var(--accent); }
.error { color: var(--vscode-errorForeground); white-space: pre-wrap; }
.problems { padding-left: 18px; }
.problems li[data-line] { cursor: pointer; margin: 2px 0; }
.problems li[data-line]:hover code { text-decoration: underline; }
`;

/**
 * Shows renders and audits, and sends changes back. The dialog's controls are made to work by `dialogPage` (from
 * fs_cli/dialog_page.js); which groups and array items were toggled, and the tab shown, are kept across renders (and
 * reloads).
 */
const SCRIPT = `
const vscode = acquireVsCodeApi();
const state = Object.assign({ toggled: {}, tab: "dialog" }, vscode.getState());
const dialog = document.getElementById("dialog");
const style = document.getElementById("dialog-style");
const messages = document.getElementById("messages");
const featureSelect = document.getElementById("feature");
const reset = document.getElementById("reset");

featureSelect.addEventListener("change", () => vscode.postMessage({ type: "feature", value: featureSelect.value }));
reset.addEventListener("click", () => vscode.postMessage({ type: "reset" }));

const page = dialogPage(dialog, {
  set: (name, value) => vscode.postMessage({ type: "set", name, value }),
  remove: (name, index) => vscode.postMessage({ type: "remove", name, index }),
  toggled: state.toggled,
  saveToggled: () => vscode.setState(state),
});

// The other sections, away from Onshape's styles
const sections = document.getElementById("sections").attachShadow({ mode: "open" });
sections.innerHTML = "<style>" + SECTION_STYLE + "</style>" +
  "<div data-section='tables'></div><div data-section='writeup' class='writeup'></div><div data-section='problems'></div>";
let filter = "";

function filterTables() {
  const words = filter.toLowerCase().split(/\\s+/).filter(Boolean);
  for (const row of sections.querySelectorAll(".lookup tbody tr")) {
    row.classList.toggle("hidden", !words.every((word) => row.dataset.text.includes(word)));
  }
}

sections.addEventListener("input", (event) => {
  if (event.target.matches(".table-filter")) {
    filter = event.target.value;
    filterTables();
  }
});
sections.addEventListener("click", (event) => {
  const problem = event.target.closest("li[data-line]");
  if (problem) {
    vscode.postMessage({ type: "reveal", line: Number(problem.dataset.line), character: Number(problem.dataset.character) });
  }
});

function tabButton(tab) {
  return document.querySelector(".fs-tabs [data-tab='" + tab + "']");
}

function showTab(tab) {
  if (tabButton(tab).hidden) {
    tab = "dialog";
  }
  state.tab = tab;
  vscode.setState(state);
  for (const button of document.querySelectorAll(".fs-tabs button")) {
    button.setAttribute("aria-selected", String(button.dataset.tab === tab));
  }
  document.querySelector("[data-page='dialog']").hidden = tab !== "dialog";
  document.getElementById("sections").hidden = tab === "dialog";
  for (const section of sections.querySelectorAll("[data-section]")) {
    section.hidden = section.dataset.section !== tab;
  }
}

document.querySelector(".fs-tabs").addEventListener("click", (event) => {
  const button = event.target.closest("button[data-tab]");
  if (button) {
    showTab(button.dataset.tab);
  }
});
showTab(state.tab);

function showRender(message) {
  featureSelect.replaceChildren(...message.features.map((name) => new Option(name, name, false, name === message.feature)));
  featureSelect.hidden = message.features.length < 2;
  document.getElementById("settings").textContent = message.settings.join(", ");
  reset.hidden = message.settings.length === 0;
  const notes = [message.error, ...(message.warnings || [])].filter(Boolean);
  messages.textContent = notes.join("\\n");
  messages.hidden = notes.length === 0;
  if (message.html) {
    const rendered = new DOMParser().parseFromString(message.html, "text/html");
    document.documentElement.setAttribute("data-os-theme", rendered.documentElement.getAttribute("data-os-theme") || "dark");
    const css = rendered.querySelector("style").textContent;
    if (style.textContent !== css) {
      style.textContent = css;
    }
    dialog.replaceChildren(...rendered.body.childNodes);
    page.drawn();
  }
}

function showAudit(message) {
  for (const section of sections.querySelectorAll("[data-section]")) {
    if (message.html) {
      section.innerHTML = message.html[section.dataset.section];
    } else {
      section.replaceChildren(Object.assign(document.createElement("p"), { className: "error", textContent: message.error }));
    }
  }
  const input = sections.querySelector(".table-filter");
  if (input) {
    input.value = filter;
    filterTables();
  }
  tabButton("tables").hidden = message.tables === 0;
  tabButton("problems").textContent = message.problems ? "Problems (" + message.problems + ")" : "Problems";
  showTab(state.tab);
}

window.addEventListener("message", (event) => {
  if (event.data.type === "render") {
    showRender(event.data);
  } else if (event.data.type === "audit") {
    showAudit(event.data);
  }
});

vscode.postMessage({ type: "ready" });
`;
