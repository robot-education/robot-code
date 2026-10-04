# FeatureScript for VS Code

Onshape FeatureScript support for the [robot-code](https://github.com/robot-education/robot-code) repo:
highlighting, navigation, hovers, completions, diagnostics, and commands for pushing to Onshape.

- `src/extension.ts`: the extension itself, a thin client which starts the language server and runs the `fs` CLI.
- `server/fs_lsp/`: the Python language server, where all the language smarts live. It's installed into the
  repo's virtualenv by `uv sync` and started with `.venv/bin/fs-lsp` (or `uv run fs-lsp`).
- `server/tests/`: the language server's tests (run with `uv run pytest` from the repo root).
- `syntaxes/`, `snippets/`, `language-configuration.json`: static language support.

See the repo's README for installation and development notes. Much of this folder is based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT, see
`THIRD_PARTY_LICENSE`).
