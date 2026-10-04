// Converts the hand-edited YAML grammar into the JSON grammar VS Code loads.
import { readFile, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import YAML from "yaml";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = await readFile(resolve(root, "syntaxes/featurescript.tmLanguage.yaml"), "utf8");
await writeFile(
  resolve(root, "syntaxes/featurescript.tmLanguage.json"),
  JSON.stringify(YAML.parse(source), null, 2) + "\n",
  "utf8"
);
