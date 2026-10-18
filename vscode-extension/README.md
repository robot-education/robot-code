# FeatureScript for VS Code

Onshape FeatureScript support for the [robot-code](https://github.com/robot-education/robot-code) repo:
highlighting, navigation, hovers, completions, diagnostics, formatting, and commands for pushing to Onshape. The
repo's README says how to install it.

- `src/extension.ts`: the extension itself, a thin client which starts the language server and runs the `fs` CLI.
- `server/fs_lsp/`: the Python language server, where all the language smarts live. It's installed into the
  repo's virtualenv by `uv sync`.
- `server/tests/`: the language server's tests (run with `uv run pytest` from the repo root).
- `syntaxes/`, `snippets/`, `language-configuration.json`: static language support.

## Features

The extension provides:

- TextMate and semantic highlighting (custom features, predicates, enums and members, annotation and map keys,
  stdlib symbols, ...)
- Outline, breadcrumbs, sticky scroll, and folding
- Go to Definition and Find References across files (imports are resolved through `fs-studios.json`), highlights,
  and workspace symbol search (Ctrl+T). Go to Definition also works on the function names in a feature's
  `"Manipulator Change Function"` and `"Editing Logic Function"`, and on `"UIHint"` strings
- Go to Definition into std (`std/`, the copy `fs pull-std` keeps): on its functions, constants, enums and their
  members, types, and the files imports of it refer to. Find References on a std symbol lists where the repo uses it
  (as `fs refs` does). Std's files navigate the same way, and get no diagnostics or formatting. Completions, hovers,
  and signature help for std come from an index of it built by `fs pull-std`, which is quick to load; its files are
  only parsed when they're navigated to
- A Preview Feature UI button (in the editor's title bar, for files defining a feature) which shows the feature's
  dialog as `fs ui` renders it, beside the file, following edits as they're made (saved or not), in VS Code's light
  or dark theme. It works like Onshape's dialog: clicking a tab, checkbox, button, or dropdown option, or entering a
  value, changes that parameter (showing and hiding the parameters which depend on it); groups and array items open
  and close; and array items can be added and removed. Clicking a number selects it all. Hovering over a parameter shows
  a tooltip like Onshape's (its name, a number's value, and its description), with its default and UI hints under it.
  The language server renders it, keeping the parsed std library between renders, so it updates in tens of milliseconds
- Hovers with doc comments laid out like Onshape's [FsDoc](https://cad.onshape.com/FsDoc/library.html) (for std
  symbols too), signatures, enum variants, feature definition fields, a feature's parameter on `definition.x` (how it's
  declared, its name, and whether it's hidden), and the file an import refers to
- Signature help in calls, with each parameter's documentation
- Completions for enum members (std's, and the project's: `BoundingType.`, `LightenEndType.`), feature
  definition-map keys (`extrude(context, id, { ... })`), the parameters a feature declares (`definition.`: the
  feature's whose definition reaches the code, through its body, editing logic, manipulator change function, and the
  functions and predicates they pass it to; with their types and names),
  annotation keys and UIHint names in annotations, and names: what's in scope, what imports declare, and std's
  (whose documentation shows as one's chosen)
- Rename Symbol (F2) across files, as `fs rename` does: not std's names, nor released features' constants
- Diagnostics: syntax errors, undefined names, and unused or unknown imports (the same as `fs check`), with quick
  fixes where there's one right answer (as `fs check --fix`): a misspelled annotation key's match, removing an unused
  import, or importing the one file which exports an undefined name
- Import paths link to the files they import (Ctrl+click)
- Formatting (Format Document, Format Selection, or `editor.formatOnSave`), the same as `fs format`. Files with syntax
  errors, generated files, and std's files are left as they are
- Snippets (`fs-header`, `defineFeature`, `annotation`, ...)
- Commands: **FeatureScript: Push File to Onshape** (also a button in the editor title bar), **Push All**,
  **Pull**, **Sync**, and **Show Onshape Status**, which save and then run `fs` in a terminal

It starts the language server with `.venv/bin/fs-lsp` (`.venv\Scripts\fs-lsp.exe` on Windows), falling back to
`uv run fs-lsp`; override this with the `featurescript.server.command` setting.

## Developing the extension and language server

- All the language smarts are Python, in `server/fs_lsp/`; `server.py` wires them up with
  [pygls](https://github.com/openlawlibrary/pygls). After changing them, run
  **FeatureScript: Restart Language Server**.
- `src/extension.ts` only launches the server and registers the `fs` commands. After changing
  it (or the grammar, snippets, or `package.json`), run `npm run install-extension` again, or press F5 (the
  **Run FeatureScript extension** launch configuration) to try it in a new window.
- The grammar is edited in `syntaxes/featurescript.tmLanguage.yaml`; `npm run build`
  regenerates the JSON.
- The server's knowledge of the Onshape standard library lives in `server/fs_lsp/data/`, generated
  from `std/`. See "The std library" in `docs/fs-cli.md` to update it.

Much of this folder is based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT, see
`THIRD_PARTY_LICENSE`).
