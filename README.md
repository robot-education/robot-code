# FeatureScript Code Editor

A set of python scripts for updating and maintaining certain aspects of my FeatureScript projects using Onshape's API.
FeatureScripts are traditionally stored and edited in the cloud using Onshape's proprietary editor. This repo enables development to be done on local machines by interfacing with Onshape's REST API in a manner similar to tools like git.

This repo is set up to work directly with my FeatureScript backend document which can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

# Onshape API

API credentials are set by creating a file in the root named `.env`.

Two API variants are available, `KeyApi` and `OauthApi`. Each variant extends `Api`, which is the generic interface.
They are explained in more detail below.

Besides the environment variables required for each API variant, you can also set the variables:

```
API_LOGGING=true # enable logging
API_BASE_PATH=https://cad.onshape.com # Use a different base path
API_VERSION=6 # Use a different version of the API
```

## Key API

This allows you to use the Onshape API with API keys.

1. Get an API key from the [Onshape developer portal](https://dev-portal.onshape.com/keys).
1. Add your access key and secret key to `.env`:

```
API_ACCESS_KEY=<Your access key>
API_SECRET_KEY=<Your secret key>
```

You can then call `make_key_api()` to get an `Api` instance you can pass to endpoints or invoke directly.

## OAuth API

This allows you to use the Onshape API with an OAuth flow.

```

```

You can then call `make_oauth_api()` to get an `Api` instance you can pass to endpoints or invoke directly.

# First Time Python Setup

Install `python`:

```
sudo apt install software-properties-common
sudo add-apt-repository ppa:deadsnakes/ppa
sudo apt update
sudo apt install python3.12
```

Note you may need to restart your terminal after installing `software-properties-common`.

Install `pipx`:

```
sudo apt install pipx
pipx ensurepath
pipx install poetry
```

# Robot Manager Setup

You'll need to create a store entry in Onshape and generate secure certificates for the python dev server so Onshape will connect with it in your dev environment.

Use the `Launch servers` task to launch the dev servers necessary to view the app. By default, the flask app runs on port 3000, and the vite app runs on port 5173.

# Google Cloud Setup

This app is setup to be deployed using Google Cloud.

To emulate the google cloud database locally and deploy, you'll need to install the [Google Cloud CLI](https://cloud.google.com/sdk/docs/install#deb).

<!-- To emulate the google cloud database locally, first install a version of the Java JRE:

```
sudo apt install default-jre
``` -->

Then start up the google cloud emulator:

```
gcloud emulators firestore start
```

To enable python to connect to the emulator, add the variable `FIRESTORE_EMULATOR_HOST` to `.env` with the value `127.0.0.1:8080`.

Then restart the distro. This prevents google cloud from using the google cloud version located outside of WSL.

Note: this project uses Google Cloud Firestore as it's database. This is not to be confused with Google Firebase's Firestore, as Firebase is a separate project from Google Cloud. Yikes.

<!-- To connect to the cloud db from your dev environment, run:
```gcloud auth application-default login``` -->
