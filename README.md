# Robot Code

The source of truth for my Onshape FeatureScripts. Every FeatureScript lives in the Robot backend document in
Onshape, and is checked in here as a `.fs` file. They're edited locally in VS Code (with real language support),
pushed to the backend document with the `fs` command, and released to the public frontend document with
`fs release`.

The Robot Manager Onshape app previously lived here; its final state is preserved at the
[`robot-manager-final`](https://github.com/robot-education/robot-code/tree/robot-manager-final) tag.

## Layout

| Path                       | What it is                                                                   |
| -------------------------- | ---------------------------------------------------------------------------- |
| `featurescripts/`          | The backend document's Feature Studios, organized however you like           |
| `fs_cli/`                  | The `fs` command                                                             |
| `onshape_api/`             | A small Onshape REST API client; see [its README](onshape_api/README.md)     |
| `std/`                     | A read-only copy of the Onshape std library, for reference                   |
| `vscode-extension/`        | The VS Code extension (TypeScript client, grammar, snippets)                 |
| `vscode-extension/server/` | The Python FeatureScript language server the extension runs (`fs_lsp`)       |
| `pyproject.toml`           | Python dependencies, plus the `[tool.fs]` table configuring the documents    |

# Setup

1. Install [uv](https://github.com/astral-sh/uv), then install Python and the dependencies:

    ```
    curl -LsSf https://astral.sh/uv/install.sh | sh
    uv python install 3.12
    uv sync
    ```

2. Create an API key in the [Onshape developer portal](https://dev-portal.onshape.com/keys) and add it to a
   `.env` file in the root of the repo (it's gitignored):

    ```
    API_ACCESS_KEY=<Your API Access Key>
    API_SECRET_KEY=<Your API Secret Key>

    # Optional
    API_LOGGING=false            # Log every request
    API_BASE_URL=https://cad.onshape.com
    API_VERSION=10
    ```

    The same variables can be set directly in the environment instead (e.g. in CI).

3. Install the VS Code extension (requires Node.js), then reload VS Code:

    ```
    cd vscode-extension
    npm ci
    npm run install-extension
    ```

# The `fs` command

## Pushing and pulling

`[tool.fs]` in `pyproject.toml` points `fs` at the backend document, and every Feature Studio in it corresponds
to a file in `featurescripts/`. The local folder structure is yours: once a file is tracked you can move or rename
it freely, and the folders in Onshape are ignored. The only time Onshape's folders matter is when a studio is
pulled into the repo for the first time, e.g. a studio named `robotFrame.fs` in the document's `Robot` folder
lands at `featurescripts/Robot/robotFrame.fs`.

The repo is the source of truth, so pushing is the default:

```
uv run fs                    # same as `fs push`: push every local change
uv run fs push robotFrame    # push one FeatureScript, by name...
uv run fs push featurescripts/Robot   # ...or by file or folder
uv run fs push --dry-run     # show what would be pushed
```

New files become new Feature Studios at the top level of the document (the API can't create folders); move the
tabs in Onshape if you like, `fs` won't care. The API doesn't report compile errors, so check new code in Onshape.

For the occasional edit made directly in Onshape:

```
uv run fs status             # what differs, and what to do about it
uv run fs diff               # unified diff of Onshape vs. the repo
uv run fs pull               # bring Onshape changes into the repo
uv run fs sync               # pull Onshape-only changes and push local-only changes
uv run fs update-std --push  # bump `FeatureScript NNNN;` and std imports to the latest std
```

Every command takes the same kinds of targets as `fs push`, and `--help` lists each one's options.

### How conflicts are detected

`fs` never silently overwrites work. For each Feature Studio it compares the local file, the Feature Studio in
Onshape, and what the studio looked like the last time it was pushed or pulled. That last-synced state is kept
per machine in `.fs-state.json` (gitignored). On a fresh clone, Onshape's contents are instead compared
against the file's recent git history: if Onshape matches a committed version, local edits are safe to push.

- Only the local file changed: `fs push` updates Onshape.
- Only Onshape changed: `fs push` skips it and suggests `fs pull`.
- Both changed: both commands skip it; inspect with `fs diff`, then pick a side with `fs pull --force` or
  `fs push --force`.

Changes to the `version` of imports of other tabs in the document (`import(path : "<element id>", version :
"...")`) are never treated as edits, since Onshape manages them itself and may update them when the imported tab
changes. When Onshape's are newer, `fs push`, `fs pull`, and `fs sync` copy them into the local file, and
`fs push` never sends older ones.

Files are matched to Feature Studios by element id once synced, so renaming or moving either the file or the tab
doesn't break the link. (On a fresh clone, studios are matched to the local file with the same name.) Deleting a
file never deletes the tab in Onshape (delete the tab yourself), and a tab deleted in Onshape is recreated by
`fs push` unless you delete the file.

### API usage

Onshape limits API calls per year (2,500 per user on Standard/Free plans; see
[API limits](https://onshape-public.github.io/docs/auth/limits/)), so `fs` keeps them to a minimum:

- `fs status` / `fs push` with nothing to do: 1 call, which lists every tab's microversion (and the folders).
  A studio is only downloaded when its microversion changed since the last sync, so on a new machine the
  first run downloads each studio once. A changed microversion doesn't always mean changed code (e.g. it may
  change when a tab it imports changes); then the download just confirms nothing needs doing.
- Pushing: 1 call per studio pushed, plus 1 to record the new microversions.
- Pulling: 1 call per studio pulled.
- `fs release`: about 8 calls.

Failed calls (like a 400) don't count.

## Releasing

The frontend document is what users import. It isn't mirrored into the repo: each of its Feature Studios is a
small generated file re-exporting a version of the backend studio, and `fs release` keeps it up to date.

```
uv run fs release robotFrame --minor -d "Added a thing"   # 1.2.3 -> 1.3.0
uv run fs release robotFrame --patch --publish            # also create the version in the frontend
uv run fs release robotFrameBeta --major --beta           # start a beta: 1.3.0 -> 2.0.0-beta.1
uv run fs release robotFrameBeta --beta                   # continue it: 2.0.0-beta.2
uv run fs release robotFrame --minor --dry-run            # show the plan without changing anything
```

A release creates a version named e.g. `Robot frame - v1.3.0` in the backend document, then points the
frontend studio of the same name at it (creating the studio if needed). Beta releases go to the separate
`frontend_beta` document instead, and the feature's name must contain "beta". The studio must be pushed and in
sync before it can be released, and `fs release` asks for confirmation since versions can't be deleted (`-y`
skips the prompt).

`uv run fs sync-versions` creates any release versions that exist in the backend document but not in the
frontend document.

# VS Code

Open the repo root in VS Code; `.vscode/settings.json` associates `*.fs` with FeatureScript.

The extension provides:

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
  **Pull**, **Sync**, and **Show Onshape Status**, which save and then run `fs` in a terminal

It starts the language server with `.venv/bin/fs-lsp`, falling back to `uv run fs-lsp`; override this with the
`featurescript.server.command` setting.

## Developing the extension and language server

- All the language smarts are Python, in `vscode-extension/server/fs_lsp/`; `server.py` wires them up with
  [pygls](https://github.com/openlawlibrary/pygls). After changing them, run
  **FeatureScript: Restart Language Server**.
- `vscode-extension/src/extension.ts` only launches the server and registers the `fs` commands. After changing
  it (or the grammar, snippets, or `package.json`), run `npm run install-extension` again, or press F5 (the
  **Run FeatureScript extension** launch configuration) to try it in a new window.
- The grammar is edited in `vscode-extension/syntaxes/featurescript.tmLanguage.yaml`; `npm run build`
  regenerates the JSON.
- The server's knowledge of the Onshape standard library lives in `vscode-extension/server/fs_lsp/data/`, generated
  from `std/`. See below to update it.

# The std library

`std/` holds a copy of the Onshape std library ([MIT](std/LICENSE.txt)), with version numbers replaced by `✨` so
updates only touch the files that changed. Its README notes the version. To update it to the latest std, and
regenerate the language server's stdlib indexes:

```
uv run python -m fs_lsp.tools.update_stdlib
```

This uses the [std library mirror on GitHub](https://github.com/javawizard/onshape-std-library-mirror), so it makes
no Onshape API calls. If the mirror lags behind Onshape, `--from-onshape` downloads the std from Onshape instead,
at one API call per std Feature Studio (~270). `--offline` just regenerates the indexes from `std/`.

# The Onshape API

`onshape_api/reference/openapi.json` is a trimmed, committed copy of Onshape's API definition (the one behind the
[Glassworks API explorer](https://cad.onshape.com/glassworks/explorer)), covering just the endpoints we call.
`onshape_api/types.py` hand-types the parts of those responses we read. See the
[onshape_api README](onshape_api/README.md) for how to add an endpoint.

The extension and language server are based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT).

# Tests

```
uv run pytest                                      # fs CLI, Onshape client, and language server
cd vscode-extension && npm test                    # TextMate grammar
cd vscode-extension && npm run test:integration    # the extension inside a real VS Code
```

On a headless Linux machine, run the integration test under `xvfb-run -a`.
