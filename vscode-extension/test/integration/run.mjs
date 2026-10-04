// Launches a real VS Code with the extension loaded and runs suite.cjs inside it.
// Requires a display; on headless Linux use `xvfb-run -a npm run test:integration`.
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { runTests } from "@vscode/test-electron";

const extensionRoot = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const repoRoot = resolve(extensionRoot, "..");

try {
  await runTests({
    version: process.env.VSCODE_VERSION ?? "stable",
    extensionDevelopmentPath: extensionRoot,
    extensionTestsPath: resolve(extensionRoot, "test/integration/suite.cjs"),
    launchArgs: [repoRoot, "--disable-extensions", "--disable-workspace-trust"]
  });
} catch (error) {
  console.error(error);
  process.exit(1);
}
