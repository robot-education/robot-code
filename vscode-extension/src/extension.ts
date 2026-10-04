import * as fs from "node:fs";
import * as path from "node:path";
import * as vscode from "vscode";
import { LanguageClient, type LanguageClientOptions, type ServerOptions } from "vscode-languageclient/node";

const CONFIG_FILE = "pyproject.toml";

let client: LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const output = vscode.window.createOutputChannel("FeatureScript Language Server");
  context.subscriptions.push(output);

  context.subscriptions.push(
    vscode.commands.registerCommand("featurescript.restartServer", async () => {
      await stopClient();
      await startClient(output);
    }),
    vscode.commands.registerCommand("featurescript.push", () => runFs(["push"])),
    vscode.commands.registerCommand("featurescript.pushFile", (uri?: vscode.Uri) => {
      const target = uri ?? vscode.window.activeTextEditor?.document.uri;
      if (!target || target.scheme !== "file") {
        void vscode.window.showWarningMessage("Open a FeatureScript file to push it.");
        return;
      }
      return runFs(["push", target.fsPath]);
    }),
    vscode.commands.registerCommand("featurescript.pull", () => runFs(["pull"])),
    vscode.commands.registerCommand("featurescript.sync", () => runFs(["sync"])),
    vscode.commands.registerCommand("featurescript.status", () => runFs(["status"]))
  );

  await startClient(output);
}

export async function deactivate(): Promise<void> {
  await stopClient();
}

/** Whether a folder is the repo root: its pyproject.toml configures the fs CLI. */
function isRepoRoot(folder: string): boolean {
  try {
    return /^\[tool\.fs\]/m.test(fs.readFileSync(path.join(folder, CONFIG_FILE), "utf8"));
  } catch {
    return false;
  }
}

/** The repo root, falling back to the first workspace folder. */
function repoRoot(): string | undefined {
  const folders = (vscode.workspace.workspaceFolders ?? []).map((folder) => folder.uri.fsPath);
  return folders.find(isRepoRoot) ?? folders[0];
}

/**
 * Resolves the command used to run one of the repo's Python entry points (fs or fs-lsp).
 * An explicit setting wins; otherwise prefer the uv-managed virtualenv, then `uv run`.
 */
function pythonCommand(setting: string, executable: string, root: string | undefined): { command: string; args: string[] } {
  const configured = vscode.workspace.getConfiguration("featurescript").get<string[]>(setting, []);
  if (configured.length > 0) {
    return { command: configured[0]!, args: configured.slice(1) };
  }
  if (root) {
    const venvExecutable = process.platform === "win32"
      ? path.join(root, ".venv", "Scripts", `${executable}.exe`)
      : path.join(root, ".venv", "bin", executable);
    if (fs.existsSync(venvExecutable)) {
      return { command: venvExecutable, args: [] };
    }
  }
  return { command: "uv", args: ["run", executable] };
}

async function startClient(output: vscode.OutputChannel): Promise<void> {
  const root = repoRoot();
  const { command, args } = pythonCommand("server.command", "fs-lsp", root);
  const serverOptions: ServerOptions = {
    command,
    args,
    options: root ? { cwd: root } : {}
  };
  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ language: "featurescript" }],
    outputChannel: output
  };
  client = new LanguageClient("featurescript", "FeatureScript Language Server", serverOptions, clientOptions);
  try {
    await client.start();
  } catch (error) {
    client = undefined;
    const message = error instanceof Error ? error.message : String(error);
    const choice = await vscode.window.showErrorMessage(
      `Failed to start the FeatureScript language server (${command}): ${message}. Run \`uv sync\` in the repo, or set featurescript.server.command.`,
      "Open Settings"
    );
    if (choice === "Open Settings") {
      await vscode.commands.executeCommand("workbench.action.openSettings", "featurescript.server.command");
    }
  }
}

async function stopClient(): Promise<void> {
  const running = client;
  client = undefined;
  if (running) {
    await running.stop();
  }
}

/** Runs the fs CLI in a terminal so its output (and any conflicts) are visible. */
async function runFs(fsArgs: string[]): Promise<void> {
  const root = repoRoot();
  if (!root || !isRepoRoot(root)) {
    void vscode.window.showWarningMessage(`Open the robot-code repo (whose ${CONFIG_FILE} has a [tool.fs] table) to use FeatureScript commands.`);
    return;
  }
  // The CLI reads files from disk
  await vscode.workspace.saveAll(false);
  const { command, args } = pythonCommand("fs.command", "fs", root);
  const task = new vscode.Task(
    { type: "shell", fs: fsArgs.join(" ") },
    vscode.TaskScope.Workspace,
    `fs ${fsArgs.map((arg) => path.isAbsolute(arg) ? path.relative(root, arg) : arg).join(" ")}`,
    "featurescript",
    new vscode.ShellExecution(command, [...args, ...fsArgs], { cwd: root })
  );
  task.presentationOptions = {
    reveal: vscode.TaskRevealKind.Always,
    panel: vscode.TaskPanelKind.Shared,
    clear: true
  };
  await vscode.tasks.executeTask(task);
}
