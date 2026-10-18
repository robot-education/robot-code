# Robot Code

The source of truth for my Onshape FeatureScripts. Every FeatureScript lives in the Robot backend document in
Onshape, and is checked in here as a `.fs` file. They're edited locally in VS Code (with real language support),
pushed to the backend document with the `fs` command, and released to the public frontend document with
`fs release`.

The Robot Manager Onshape app previously lived here; its final state is preserved at the
[`robot-manager-final`](https://github.com/robot-education/robot-code/tree/robot-manager-final) tag.

## Layout

| Path                       | What it is                                                                           |
| -------------------------- | ------------------------------------------------------------------------------------ |
| `featurescripts/`          | The backend document's Feature Studios, plus the Python lookup table sources         |
| `featurescripts/released/` | Released FeatureScripts, each with its own files                                     |
| `featurescripts/core/`     | Modules shared between FeatureScripts                                                |
| `fs_cli/`                  | The `fs` command                                                                     |
| `fs_eval/`                 | A FeatureScript evaluator, for testing FeatureScript without Onshape                 |
| `onshape_api/`             | A small Onshape REST API client; see [its README](onshape_api/README.md)             |
| `std/`                     | A read-only copy of the Onshape std library, for reference                           |
| `onshape_icons/`           | Onshape's UI icons, for `fs ui`; browse them with its `index.html`                   |
| `docs/`                    | How the tools work, conventions for writing FeatureScripts, their writeups and icons |
| `tests/`                   | Tests of the tools, and FeatureScript tests                                          |
| `vscode-extension/`        | The VS Code extension and its Python language server; see [its README](vscode-extension/README.md) |
| `pyproject.toml`           | Python dependencies, plus the `[tool.fs]` table configuring the documents            |

## Setup

1. Install [uv](https://github.com/astral-sh/uv) (Python and its packages) and [Node.js](https://nodejs.org/) (which
   includes npm, for building the VS Code extension).

    On Windows, in PowerShell:

    ```
    winget install --id astral-sh.uv
    winget install --id OpenJS.NodeJS.LTS
    setx PYTHONUTF8 1
    Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
    ```

    `PYTHONUTF8` makes Python read and write files as UTF-8, as it does on macOS and Linux (by default it uses
    Windows' legacy encoding, which garbles FeatureScripts with symbols like `°`). The execution policy lets
    PowerShell run `npm`. Then close and reopen VS Code and its terminals, so they see the new `PATH` and variable.

    On macOS or Linux:

    ```
    curl -LsSf https://astral.sh/uv/install.sh | sh
    ```

    and Node.js from [nodejs.org](https://nodejs.org/) or your package manager.

2. Install Python and the dependencies, in the root of the repo:

    ```
    uv sync
    ```

3. Create an API key in the [Onshape developer portal](https://dev-portal.onshape.com/keys) and add it to a
   `.env` file in the root of the repo (it's gitignored):

    ```
    API_ACCESS_KEY=<Your API Access Key>
    API_SECRET_KEY=<Your API Secret Key>

    # Optional
    API_LOGGING=false            # Log every request
    API_BASE_URL=https://cad.onshape.com
    ```

    The same variables can be set directly in the environment instead (e.g. in CI).

4. Install the FeatureScript VS Code extension, then reload VS Code:

    ```
    cd vscode-extension
    npm ci
    npm run install-extension
    ```

5. Check it all works:

    ```
    uv run pytest
    uv run fs status
    ```

### VS Code

Open the repo's root folder. VS Code offers the extensions it recommends (`.vscode/extensions.json`):

- **FeatureScript** (step 4): highlighting, navigation, completions, diagnostics, formatting, a live preview of a
  feature's dialog, and push/pull commands. See [its README](vscode-extension/README.md).
- **Python** (`ms-python.python`, with Pylance): Python editing, and running the tests from the Testing view. Pick
  the repo's `.venv` as the interpreter if it doesn't pick it itself.
- **Prettier** (`esbenp.prettier-vscode`): formats JSON, Markdown, and TypeScript on save.
- **Code Spell Checker** (`streetsidesoftware.code-spell-checker`): optional.

Don't use a Python formatter like Black: the Python here isn't formatted with one, so formatting on save would rewrite
whole files. `fs ui` (screenshots of a feature's dialog) needs Chrome, Edge, or Chromium.

## Using it

```
uv run fs push               # push local changes to the backend document (and commit them)
uv run fs status             # what differs between the repo and Onshape
uv run fs pull               # bring changes made in Onshape into the repo
uv run fs check              # find problems in the FeatureScripts (--fix fixes the obvious ones)
uv run fs format             # format them like the std library
uv run fs gen                # regenerate lookup tables, profiles, and icons from their Python definitions
uv run fs release NAME --minor -d "What changed"   # release a FeatureScript to the frontend document
uv run pytest                # run the tests
```

Every command takes `--help`. More:

- [docs/fs-cli.md](docs/fs-cli.md): the `fs` command: syncing with Onshape, checking, generated files, releasing,
  and the std library
- [docs/testing.md](docs/testing.md): testing FeatureScript with the evaluator, and running the tests
- [docs/featurescript-style.md](docs/featurescript-style.md): conventions for writing FeatureScripts
- [docs/feature-writeups.md](docs/feature-writeups.md) and [docs/icons.md](docs/icons.md): features' writeups and
  icons
- [vscode-extension/README.md](vscode-extension/README.md): the extension's features, and developing it
