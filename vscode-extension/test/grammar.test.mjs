// Checks the TextMate grammar against the shared fixtures in server/tests/fixtures.
// Ported from gatrall/featurescript-language-support (MIT).
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import { dirname, resolve } from "node:path";
import { before, describe, it } from "node:test";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const oniguruma = require("vscode-oniguruma");
const textmate = require("vscode-textmate");

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const fixtures = resolve(root, "server/tests/fixtures");

async function loadGrammar() {
  const wasm = await readFile(require.resolve("vscode-oniguruma/release/onig.wasm"));
  await oniguruma.loadWASM(wasm.buffer);
  const registry = new textmate.Registry({
    onigLib: Promise.resolve({
      createOnigScanner: oniguruma.createOnigScanner,
      createOnigString: oniguruma.createOnigString
    }),
    loadGrammar: async (scopeName) => {
      if (scopeName !== "source.featurescript") return null;
      const grammarPath = resolve(root, "syntaxes/featurescript.tmLanguage.json");
      return textmate.parseRawGrammar(await readFile(grammarPath, "utf8"), grammarPath);
    }
  });
  const grammar = await registry.loadGrammar("source.featurescript");
  assert.ok(grammar);
  return grammar;
}

const fixture = (name) => readFile(resolve(fixtures, name), "utf8");

function scopesFor(grammar, source, needle) {
  let ruleStack = textmate.INITIAL;
  for (const line of source.split(/\r?\n/)) {
    const result = grammar.tokenizeLine(line, ruleStack);
    const index = line.indexOf(needle);
    if (index >= 0) {
      const token = result.tokens.find((t) => t.startIndex <= index && t.endIndex >= index + needle.length);
      assert.ok(token, `No TextMate token found for ${needle}`);
      return token.scopes;
    }
    ruleStack = result.ruleStack;
  }
  assert.fail(`Needle not found: ${needle}`);
}

describe("FeatureScript TextMate grammar", () => {
  let grammar;
  before(async () => {
    grammar = await loadGrammar();
  });

  it("highlights the version directive", async () => {
    const source = await fixture("slot.fs");
    assert.ok(scopesFor(grammar, source, "FeatureScript").includes("storage.type.featurescript"));
    assert.ok(scopesFor(grammar, source, "2909").includes("constant.numeric.featurescript"));
  });

  it("highlights comments, strings, escapes, support calls, and punctuation", async () => {
    const source = await fixture("slot.fs");
    assert.ok(scopesFor(grammar, source, "//").includes("punctuation.definition.comment.featurescript"));
    assert.ok(scopesFor(grammar, source, "Slot").includes("string.quoted.double.featurescript"));
    assert.ok(scopesFor(grammar, await fixture("annotations.fs"), "\\n").includes("constant.character.escape.featurescript"));
    assert.ok(scopesFor(grammar, source, "opExtrude").includes("support.function.featurescript"));
    assert.ok(scopesFor(grammar, source, "{").includes("punctuation.section.block.begin.featurescript"));
    assert.ok(scopesFor(grammar, source, ":").includes("punctuation.separator.colon.featurescript"));
  });

  it("highlights declarations and namespace access", async () => {
    const imports = await fixture("imports.fs");
    const operators = await fixture("operators.fs");
    assert.ok(scopesFor(grammar, imports, "::").includes("punctuation.accessor.namespace.featurescript"));
    assert.ok(scopesFor(grammar, imports, "importedValue").includes("variable.other.constant.featurescript"));
    assert.ok(scopesFor(grammar, operators, "*").includes("entity.name.function.operator.featurescript"));
  });

  it("marks ++ and -- invalid", async () => {
    const source = await fixture("operators.fs");
    assert.ok(scopesFor(grammar, source, "++").includes("invalid.illegal.operator.increment.featurescript"));
    assert.ok(scopesFor(grammar, source, "--").includes("invalid.illegal.operator.increment.featurescript"));
  });
});
