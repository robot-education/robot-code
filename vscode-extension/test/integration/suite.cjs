// Runs inside VS Code: opens a FeatureScript fixture and exercises the language server through the
// same APIs the editor uses.
const assert = require("node:assert/strict");
const path = require("node:path");
const vscode = require("vscode");

const repoRoot = path.resolve(__dirname, "../../..");

async function retry(description, attempt) {
  // The language server takes a moment to start
  const deadline = Date.now() + 30000;
  for (;;) {
    try {
      return await attempt();
    } catch (error) {
      if (Date.now() > deadline) {
        throw new Error(`${description}: ${error.message}`);
      }
      await new Promise((resolve) => setTimeout(resolve, 250));
    }
  }
}

function positionOf(document, needle, after = 0) {
  const offset = document.getText().indexOf(needle);
  assert.notEqual(offset, -1, `Missing ${needle}`);
  return document.positionAt(offset + after);
}

const tests = {
  async "detects the FeatureScript language"(document) {
    assert.equal(document.languageId, "featurescript");
  },

  async "provides semantic tokens"(document) {
    const legend = await vscode.commands.executeCommand("vscode.provideDocumentSemanticTokensLegend", document.uri);
    assert.ok(legend.tokenTypes.includes("mapKey"));
    const tokens = await vscode.commands.executeCommand("vscode.provideDocumentSemanticTokens", document.uri);
    assert.ok(tokens.data.length > 0);
  },

  async "provides hovers"(document) {
    const hovers = await vscode.commands.executeCommand(
      "vscode.executeHoverProvider",
      document.uri,
      positionOf(document, "opExtrude")
    );
    const text = hovers.flatMap((hover) => hover.contents.map((content) => content.value ?? String(content))).join("\n");
    assert.match(text, /opExtrude/);
  },

  async "goes to definitions"(document) {
    const locations = await vscode.commands.executeCommand(
      "vscode.executeDefinitionProvider",
      document.uri,
      positionOf(document, "\"entities\" : definition.slotPath", "\"entities\" : definition.".length)
    );
    assert.ok(locations.length > 0);
  },

  async "completes enum members"(document) {
    const editor = await vscode.window.showTextDocument(document);
    const end = document.lineAt(document.lineCount - 1).range.end;
    await editor.edit((edit) => edit.insert(end, "\nconst completionProbe = BoundingType."));
    const position = document.lineAt(document.lineCount - 1).range.end;
    const list = await vscode.commands.executeCommand("vscode.executeCompletionItemProvider", document.uri, position);
    assert.ok(list.items.some((item) => (item.label.label ?? item.label) === "THROUGH_ALL"));
    await vscode.commands.executeCommand("workbench.action.files.revert");
  },

  async "lists document symbols"(document) {
    const symbols = await vscode.commands.executeCommand("vscode.executeDocumentSymbolProvider", document.uri);
    assert.ok(symbols.some((symbol) => symbol.name === "slot"));
  }
};

exports.run = async function run() {
  const uri = vscode.Uri.file(path.join(repoRoot, "tests/fixtures/slot.fs"));
  const document = await vscode.workspace.openTextDocument(uri);
  await vscode.window.showTextDocument(document);
  let failures = 0;
  for (const [name, test] of Object.entries(tests)) {
    try {
      await retry(name, () => test(document));
      console.log(`ok - ${name}`);
    } catch (error) {
      failures += 1;
      console.error(`not ok - ${name}\n${error.stack ?? error}`);
    }
  }
  if (failures > 0) {
    throw new Error(`${failures} integration test(s) failed`);
  }
};
