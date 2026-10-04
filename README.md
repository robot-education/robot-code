# Robot Code

A monorepo hosting python scripts for updating and maintaining my Robot FeatureScripts.

The Robot Manager Onshape app previously lived here; it was removed and its final state is preserved at the `robot-manager-final` tag.

Note: This repo is equipped to run with VSCode on Linux (specifically, WSL Ubuntu).

# Repo Setup

First, create a new file in the root of this project named `.env` and add the following contents:

```
# Server config
API_LOGGING=true # Enable or disable logging
API_BASE_PATH=https://cad.onshape.com # Use a different base path
API_VERSION=10 # Use a different version of the API

# API Keys
API_ACCESS_KEY=<Your API Access Key>
API_SECRET_KEY=<Your API Secret Key>
```


## Python Setup

This project uses [uv](https://github.com/astral-sh/uv) to manage Python.

Install `uv`:

```
curl -LsSf https://astral.sh/uv/install.sh | sh
```

Then use `uv` to install Python 3.12 and all of the project's dependencies:

```
uv python install 3.12
uv sync
```

Note that Python version 3.12 or greater is a hard requirement.

## Onshape API Key Setup

API keys allow you to use the Onshape API via locally developed Python scripts.

1. Get an API key from the [Onshape developer portal](https://dev-portal.onshape.com/keys).
1. Add your access key and secret key to `.env`.

You can then call `make_key_api()` inside a locally running Python script to get an `Api` instance you can pass to endpoints in `onshape_api/endpoints`.

# FeatureScript Scripts

Several scripts for pulling code from Onshape and pushing new versions of Robot FeatureScripts are included in `scripts`. Scripts can be invoked as follows:

```
./scripts/onshape.sh
```

The following scripts are available:

-   onshape - Can be used to push and pull code from Onshape via the API.
-   robot - Can be used to release new versions of Robot FeatureScripts.
