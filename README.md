# Robot Code

The source of truth for my Onshape FeatureScripts. FeatureScripts are checked in as `.fs` files, edited
locally in VS Code (with real language support), and pushed to Onshape with the `fs` command.

The Robot Manager Onshape app previously lived here; it was removed and its final state is preserved at the
[`robot-manager-final`](https://github.com/robot-education/robot-code/tree/robot-manager-final) tag.

## Layout

| Path                   | What it is                                                                              |
| ---------------------- | --------------------------------------------------------------------------------------- |
| `featurescripts/`      | FeatureScript source, one folder per Onshape document                                   |
| `featurescripts.toml`  | Maps each folder to an Onshape document workspace                                       |
| `fs_cli/`              | The `fs` command for pushing to (and pulling from) Onshape                              |
| `fs_lsp/`              | A Python FeatureScript language server (highlighting, navigation, hovers, completions)  |
| `vscode-extension/`    | The VS Code extension: grammar, snippets, and a thin client that runs `fs_lsp`          |
| `onshape_api/`         | A small Onshape REST API client                                                         |
| `featurescript/`, `robot_code/` | Older tooling: a Python DSL for generating FeatureScript and Robot release scripts |

# Setup

This project uses [uv](https://github.com/astral-sh/uv) to manage Python (3.12+):

```
curl -LsSf https://astral.sh/uv/install.sh | sh
uv python install 3.12
uv sync
```

## Onshape API keys

`fs` talks to Onshape with API keys:

1. Create an API key in the [Onshape developer portal](https://dev-portal.onshape.com/keys).
2. Create a `.env` file in the root of this repo (it's gitignored):

```
API_ACCESS_KEY=<Your API Access Key>
API_SECRET_KEY=<Your API Secret Key>

# Optional
API_LOGGING=false            # Log every request
API_BASE_URL=https://cad.onshape.com
API_VERSION=10
```

The same variables can be set directly in the environment instead (e.g. in CI).

# Pushing FeatureScripts to Onshape

Each entry in `featurescripts.toml` maps an Onshape workspace to a folder:

```toml
[documents.robot]
url = "https://cad.onshape.com/documents/<document id>/w/<workspace id>"
# path = "featurescripts/robot"   # optional, defaults to featurescripts/<name>
```

Every Feature Studio in that workspace corresponds to `<folder>/<studio name>.fs`.

The repo is the source of truth, so pushing is the default:

```
uv run fs                    # same as `fs push`: push every local change
uv run fs push robot         # just one document
uv run fs push featurescripts/robot/robotFrame.fs
uv run fs push --dry-run     # show what would be pushed
```

New `.fs` files are created as new Feature Studios. Any errors or warnings Onshape reports for pushed code are
printed.

For the occasional edit made directly in Onshape:

```
uv run fs status             # what differs, and what to do about it
uv run fs diff               # unified diff of Onshape vs. the repo
uv run fs pull               # bring Onshape changes into the repo
uv run fs sync               # pull Onshape-only changes and push local-only changes
uv run fs update-std --push  # bump `FeatureScript NNNN;` and std imports to the latest std
```

To start tracking an existing document, add it to `featurescripts.toml` and run `uv run fs pull` once.

### How conflicts are detected

`fs` never silently overwrites work. For each Feature Studio it compares the local file, the Feature Studio in
Onshape, and what the studio looked like the last time it was pushed or pulled. That last-synced state is kept
per machine in `.fs-state.json` (gitignored). On a fresh clone, Onshape's contents are instead compared
against the file's recent git history: if Onshape matches a committed version, local edits are safe to push.

- Only the local file changed: `fs push` updates Onshape.
- Only Onshape changed: `fs push` skips it and suggests `fs pull`.
- Both changed: both commands skip it; inspect with `fs diff`, then pick a side with `fs pull --force` or
  `fs push --force`.

Deleting a file locally never deletes the Feature Studio in Onshape; delete the tab in Onshape yourself.

# VS Code

Open this repo in VS Code. `.vscode/settings.json` associates `*.fs` with FeatureScript.

## Installing the extension

Requires Node.js. From the repo root:

```
cd vscode-extension
npm ci
npm run install-extension
```

(or run the **Install FeatureScript extension** task). Re-run it after changing the extension. Changes to the
Python language server only need `uv sync` and **FeatureScript: Restart Language Server**.

The extension starts the language server with `.venv/bin/fs-lsp`, falling back to `uv run fs-lsp`; override
this with the `featurescript.server.command` setting.

## Features

- TextMate and semantic highlighting (custom features, predicates, enums and members, annotation and map keys,
  stdlib symbols, ...)
- Outline, breadcrumbs, sticky scroll, and folding
- Go to Definition, Find References, and highlights for symbols within a file
- Hovers with doc comments, stdlib signatures, enum variants, and feature definition fields
- Completions for enum members (`BoundingType.`) and feature definition-map keys
  (`extrude(context, id, { ... })`)
- Syntax diagnostics: unbalanced brackets, unterminated strings and comments, `++`/`--`
- Snippets (`fs-header`, `defineFeature`, `annotation`, ...)
- Commands: **FeatureScript: Push File to Onshape** (also a button in the editor title bar), **Push All**,
  **Pull**, **Sync**, and **Show Onshape Status**, which run `fs` in a terminal after saving

## Developing the extension and language server

- `fs_lsp/` holds all language smarts, in Python. `fs_lsp/server.py` wires them up with
  [pygls](https://github.com/openlawlibrary/pygls).
- `vscode-extension/src/extension.ts` only launches the server and registers the `fs` commands.
- Run the **Run FeatureScript extension** launch configuration (F5) to open a window with the in-development
  extension.
- The grammar is edited in `vscode-extension/syntaxes/featurescript.tmLanguage.yaml`; `npm run build`
  regenerates the JSON.

The language server's knowledge of the Onshape standard library lives in `fs_lsp/data/`. Regenerate it from the
latest std version in Onshape (requires API keys) with:

```
uv run python -m fs_lsp.tools.update_stdlib
```

The language server and grammar are based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT).

# Tests

```
uv run pytest                                  # fs CLI and language server
cd vscode-extension && npm test                # TextMate grammar
cd vscode-extension && npm run test:integration  # the extension inside a real VS Code
```

On a headless Linux machine, run the integration test under `xvfb-run -a`.

# Robot FeatureScript releases

`./scripts/robot.sh` releases new versions of Robot FeatureScripts from the backend document to the public
frontend document. Run `./scripts/robot.sh --help` for details.
