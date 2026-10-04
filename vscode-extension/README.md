# FeatureScript for VS Code

Onshape FeatureScript support for the [robot-code](https://github.com/robot-education/robot-code) repo:
highlighting, navigation, hovers, completions, and commands for pushing to Onshape.

The language features come from the repo's Python language server (`fs_lsp`), which this extension starts
with `.venv/bin/fs-lsp` (or `uv run fs-lsp`). See the repo's README for installation and development notes.

The grammar, snippets, and language configuration are based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT, see
`THIRD_PARTY_LICENSE`).
