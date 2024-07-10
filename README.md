# Robot Code

A monorepo hosting python scripts for updating and maintaining my Robot FeatureScript projects as well as hosting and deploying the Robot Manager Onshape library app.

This repo is set up to work directly with my FeatureScript backend document which can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

# Onshape API

A generic library for connecting with and using the Onshape API.

API credentials are set by creating a file in the root named `.env`.

Two API variants are available, `KeyApi` and `OauthApi`. Each variant extends `Api`, which is the generic interface.
They are explained in more detail below.

Besides the environment variables required for each API variant, you can also set the variables:

```
API_LOGGING=true # Enable logging
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

This allows you to use the Onshape API with an OAuth flow via the `requests_oauthlib` library.

1. Create an OAuth application on the [Onshape developer portal](https://dev-portal.onshape.com/oauthApps).

You can then call `make_oauth_api()` to get an `Api` instance you can pass to endpoints or invoke directly.

## First Time Python Setup

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

## Scripts

Several scripts for pulling code from Onshape and pushing new versions of Robot FeatureScripts are included in `scripts`. Scripts can be invoked as follows:

```
./scripts/onshape.sh
```

The following scripts are available:

-   deploy - Deploys the Robot manager app to google cloud.
-   onshape - Can be used to push and pull code from Onshape via the API.
-   robot - Can be used to release new versions of Robot FeatureScripts.

# Robot Manager

The Robot manager app lives in the `frontend` and `backend` folders. The app uses Python and flask as the backend and API and Vite and React for the frontend. The app is deployed using Google Cloud App Engine, with Google Cloud Firestore as the database.

## Robot Manager Setup

You'll need to create a store entry in Onshape and generate secure certificates for the python dev server so Onshape will connect with it in your dev environment.

Use the `Launch servers` VSCode task to launch the dev servers necessary to view the app. You should set up Onshape to connect to:

```
https://localhost:3000/app
```

## Google Cloud Dev Setup

To emulate the google cloud database locally, you'll need to install the [Google Cloud CLI](https://cloud.google.com/sdk/docs/install#deb).

Then start up the google cloud emulator:

```
gcloud emulators firestore start
```

To enable python to connect to the emulator, add the variable `FIRESTORE_EMULATOR_HOST` to `.env` with the value `127.0.0.1:8080`.

<!-- Then restart the distro. This prevents google cloud from using the google cloud version located outside of WSL. -->

Note: this project uses Google Cloud Firestore as it's database. This is not to be confused with Google Firebase's Firestore, as Firebase is a separate project from Google Cloud.

## Deploying in Google Cloud

The app can be deployed using the google cloud CLI - `gcloud app deploy`.

Some notes:

-   To allow the App deployed in the App Engine to connect to Firestore, the App Engine service account must be given the Firestore user role in IAM.
-   You'll need to create an app.yaml file to deploy. Make sure to add the necessary ENV authentication variables. The entrypoint can be:

```
entrypoint: gunicorn -b :$PORT -w 2 "backend.server:create_app()"
```
