import { execFile } from "node:child_process";
import * as crypto from "node:crypto";
import * as path from "node:path";
import * as vscode from "vscode";

/** Features a FeatureScript defines: `export const name = defineFeature(...)`. */
const FEATURE = /\bexport\s+const\s+(\w+)\s*=\s*defineFeature\s*\(/g;

export function featureNames(text: string): string[] {
  return [...text.matchAll(FEATURE)].map((match) => match[1]!);
}

/** How to run the fs CLI: its command and arguments before the subcommand, from the repo root. */
export interface FsCommand {
  command: string;
  args: string[];
  cwd: string;
}

/**
 * A panel showing a feature's dialog as `fs ui` renders it, which updates when its file is saved. Clicking a tab,
 * checkbox, or dropdown in it changes that parameter (with `fs ui --set`), to preview the dialog's other states.
 */
export class UiPreview {
  private static readonly previews = new Map<string, UiPreview>();

  private feature: string | undefined;
  private readonly settings = new Map<string, string>();
  private rendering = 0;

  static show(uri: vscode.Uri, fs: FsCommand): void {
    const existing = UiPreview.previews.get(uri.fsPath);
    if (existing) {
      existing.panel.reveal(vscode.ViewColumn.Beside, true);
      return;
    }
    const panel = vscode.window.createWebviewPanel(
      "featurescriptUiPreview",
      `Preview ${path.basename(uri.fsPath)}`,
      { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true },
      { enableScripts: true, localResourceRoots: [] }
    );
    UiPreview.previews.set(uri.fsPath, new UiPreview(panel, uri, fs));
  }

  /** Re-renders the previews of a saved file. */
  static saved(document: vscode.TextDocument): void {
    UiPreview.previews.get(document.uri.fsPath)?.render();
  }

  private constructor(private readonly panel: vscode.WebviewPanel, private readonly uri: vscode.Uri, private readonly fs: FsCommand) {
    panel.onDidDispose(() => UiPreview.previews.delete(uri.fsPath));
    panel.webview.onDidReceiveMessage((message: { type: string; name?: string; value?: string }) => {
      if (message.type === "set" && message.name !== undefined && message.value !== undefined) {
        this.settings.set(message.name, message.value);
      } else if (message.type === "feature" && message.value !== undefined) {
        this.feature = message.value;
        this.settings.clear();
      } else if (message.type === "reset") {
        this.settings.clear();
      } else {
        return;
      }
      void this.render();
    });
    void this.render();
  }

  private async render(): Promise<void> {
    const rendering = ++this.rendering;
    const document = await vscode.workspace.openTextDocument(this.uri);
    const features = featureNames(document.getText());
    if (this.feature === undefined || !features.includes(this.feature)) {
      this.feature = features[0];
    }
    const args = [...this.fs.args, "ui", this.uri.fsPath, "-o", "-"];
    if (this.feature !== undefined && features.length > 1) {
      args.push("--feature", this.feature);
    }
    for (const [name, value] of this.settings) {
      args.push("--set", `${name}=${value}`);
    }
    const result = await new Promise<{ page: string; messages: string }>((resolve) => {
      execFile(this.fs.command, args, { cwd: this.fs.cwd, maxBuffer: 64 * 1024 * 1024 }, (error, stdout, stderr) => {
        resolve({ page: error ? "" : stdout, messages: (stderr || (error ? error.message : "")).trim() });
      });
    });
    // A newer render started while this one ran
    if (rendering !== this.rendering) {
      return;
    }
    this.panel.webview.html = this.page(result.page, result.messages, features);
  }

  /** The dialog's page, with a toolbar above it and a script which sends clicks back as settings. */
  private page(dialog: string, messages: string, features: string[]): string {
    const nonce = crypto.randomBytes(16).toString("base64");
    const csp = `default-src 'none'; style-src 'unsafe-inline'; img-src data:; script-src 'nonce-${nonce}';`;
    const options = features
      .map((name) => `<option value="${escape(name)}"${name === this.feature ? " selected" : ""}>${escape(name)}</option>`)
      .join("");
    const toolbar =
      `<div class="toolbar">` +
      (features.length > 1 ? `<select id="feature">${options}</select>` : "") +
      (this.settings.size > 0
        ? `<span class="settings">${escape([...this.settings].map(([name, value]) => `${name}=${value}`).join(", "))}</span>` +
          `<button id="reset">Reset</button>`
        : "") +
      `</div>` +
      (messages ? `<pre class="messages">${escape(messages)}</pre>` : "");
    const head =
      `<meta http-equiv="Content-Security-Policy" content="${csp}">` +
      `<style>${TOOLBAR_STYLE}</style>`;
    const script = `<script nonce="${nonce}">${SCRIPT}</script>`;
    if (!dialog) {
      return `<!doctype html><html><head><meta charset="utf-8">${head}</head><body>${toolbar}${script}</body></html>`;
    }
    return dialog
      .replace("<head>", `<head>${head}`)
      .replace(/<body>/, `<body>${toolbar}`)
      .replace("</body>", `${script}</body>`);
  }
}

function escape(text: string): string {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

const TOOLBAR_STYLE = `
.toolbar { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; font: 12px sans-serif; color: #ccc; }
.toolbar .settings { flex: 1; color: #999; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.toolbar button, .toolbar select { background: #333; color: #ddd; border: 1px solid #555; border-radius: 2px; }
.messages { color: #e0a040; white-space: pre-wrap; font: 11px monospace; margin: 0 0 8px; }
[data-name] { cursor: pointer; }
.options { position: absolute; z-index: 10; background: #2b2b2b; border: 1px solid #555; box-shadow: 0 2px 6px rgba(0,0,0,.5); }
.options div { padding: 3px 8px; color: #ddd; cursor: pointer; white-space: nowrap; }
.options div:hover { background: #3d5975; }
`;

/** Sends clicked tabs and checkboxes back as settings, and opens a list of a dropdown's options. */
const SCRIPT = `
const vscode = acquireVsCodeApi();
document.getElementById("feature")?.addEventListener("change", (event) => vscode.postMessage({ type: "feature", value: event.target.value }));
document.getElementById("reset")?.addEventListener("click", () => vscode.postMessage({ type: "reset" }));
document.addEventListener("click", (event) => {
  document.querySelectorAll(".options").forEach((list) => list.remove());
  const control = event.target.closest("[data-name]");
  if (!control) {
    return;
  }
  const name = control.dataset.name;
  if (control.dataset.value !== undefined) {
    vscode.postMessage({ type: "set", name, value: control.dataset.value });
    return;
  }
  if (control.dataset.options === undefined) {
    return;
  }
  const list = document.createElement("div");
  list.className = "options";
  for (const line of control.dataset.options.split("\\n")) {
    const separator = line.indexOf("=");
    const option = document.createElement("div");
    option.textContent = line.slice(separator + 1);
    option.addEventListener("click", (choice) => {
      choice.stopPropagation();
      vscode.postMessage({ type: "set", name, value: line.slice(0, separator) });
    });
    list.appendChild(option);
  }
  const box = control.getBoundingClientRect();
  list.style.left = box.left + window.scrollX + "px";
  list.style.top = box.bottom + window.scrollY + "px";
  list.style.minWidth = box.width + "px";
  document.body.appendChild(list);
  event.stopPropagation();
});
`;
