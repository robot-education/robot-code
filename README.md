# Robot Code

A monorepo hosting python scripts for updating and maintaining my Robot FeatureScript projects as well as hosting and deploying the Robot Manager Onshape library app.

This repo is set up to work directly with my FeatureScript backend document which can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

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

# OAuth
OAUTH_CLIENT_ID=<Your OAuth client id>
OAUTH_CLIENT_SECRET=<Your OAuth client secret>
SESSION_SECRET=literallyAnythingWillDo

NODE_ENV=development
FIRESTORE_EMULATOR_HOST=127.0.0.1:8080
```

# Onshape API

A generic library for connecting with and using the Onshape API.

API credentials are set by creating a file in the root named `.env`.

Two API variants are available, `KeyApi` and `OauthApi`. Each variant extends `Api`, which is the generic interface.
They are explained in more detail below.

Besides the environment variables required for each API variant, you can also set the variables:

## Key API

This allows you to use the Onshape API with API keys.

1. Get an API key from the [Onshape developer portal](https://dev-portal.onshape.com/keys).
1. Add your access key and secret key to `.env`.

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
```

The use `pipx` to install poetry and install the project:

```
pipx install poetry
poetry install
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

## Onshape App Setup

To test Onshape app changes, you will need to create an OAuth application in the [Onshape Developer Portal](https://cad.onshape.com/appstore/dev-portal/oauthApps). Fill out the following information:

-   Name: (Arbitrary) Robot Manager Test
-   Primary format: (Arbitrary) com.robot-manager-test
-   Redirect URLs: `https://localhost:3000/redirect`
-   OAuth URL: `https://localhost:3000/sign-in`
-   Uncheck "Application can request purchases on your behalf".

Click save, then copy your OAuth app's OAuth client identifier and OAuth client secret and add them to your `.env` file.

Next, add the necessary Extensions to your OAuth application so you can see it in documents you open:

1. Open your OAuth application in the [Onshape Developer Portal](https://cad.onshape.com/appstore/dev-portal/oauthApps).
2. Go to the Extensions tab.
3. Create two extensions with the following properties:
    - Name: Robot manager test
    - Location: Element right panel
    - Context: Inside assembly/Inside part studio
    - Action URL:
      `https://localhost:3000/app?elementType=ASSEMBLY&documentId={$documentId}&instanceType={$workspaceOrVersion}&instanceId={$workspaceOrVersionId}&elementId={$elementId}`
      `https://localhost:3000/app?elementType=PART_STUDIO&documentId={$documentId}&instanceType={$workspaceOrVersion}&instanceId={$workspaceOrVersionId}&elementId={$elementId}`

### Flask Credentials Setup

Onshape requires all apps, even temporary test apps, to use https. This creates a headache for local development.
In order to solve this issue, you'll need to generate a certificate and add it to a folder named `credentials` in the root of this project:

```
/credentials/cert.pem
/credentials/key.pem
```

This can be done automatically by running the script `make_credentials.sh`:

```
./scripts/make_credentials.sh
```

You'll then need to add a security exception to your browser to avoid getting blocked by a security exception.
On Firefox, the procedure is:

1. Start the development servers using the `Launch servers` VSCode task.
2. Open Firefox and go to `Settings > Certificates > View Certificates... > Servers > Add Exception...`
3. Enter `https://localhost:3000` as the Location and click `Get Certificate`.
4. Check `Permanently store this exception` and then click `Confirm Security Exception`.

## Frontend Setup

Install Node.js, then use npm to install the dependencies in `frontend`:

```
cd frontend
npm install
```

## Google Cloud Dev Setup

To emulate the google cloud database locally, you'll need to install the [Google Cloud CLI](https://cloud.google.com/sdk/docs/install#deb).

You should also install the firestore emulator and a Java JRE:

```
sudo apt install google-cloud-cli-firestore-emulator default-jre
```

Then start up the google cloud emulator:

```
gcloud emulators firestore start
```

<!-- Then restart the distro. This prevents google cloud from using the google cloud version located outside of WSL. -->

Note: this project uses Google Cloud Firestore as it's database. This is not to be confused with Google Firebase's Firestore, as Firebase is a separate project from Google Cloud.

## Development Servers

Run the `Launch servers` VSCode task to launch the dev servers necessary to view and test the app.
If everything is setup properly, you should see all three servers start successfully.
You should also be able to launch Robot Manager from the right panel of any Onshape Part Studio or Assembly and see the Robot manager UI appear.

# Deploying in Google Cloud

The app can be deployed by running the script `./scripts/deploy.sh`.

Some notes:

-   To allow the App deployed in the App Engine to connect to Firestore, the App Engine service account must be given the Firestore user role in IAM.
-   You'll need to create an app.yaml file to deploy. Make sure to add the necessary ENV authentication variables. The entrypoint can be:

```
entrypoint: gunicorn -b :$PORT -w 2 "backend.server:create_app()"
```
