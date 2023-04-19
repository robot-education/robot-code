# FeatureScript Code Editor

A set of python scripts for updating and maintaining certain aspects of my FeatureScript projects using Onshape's API.
FeatureScripts are traditionally stored and edited in the cloud using Onshape's proprietary editor. This repo enables development to be done on local machines by interfacing with Onshape's REST API in a manner similar to tools like git.

This repo is set up to work directly with my FeatureScript backend document which can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

To use the Onshape API, you'll need to provide credentials.
* Get an API key from the [Onshape developer protal](https://dev-portal.onshape.com/keys).
* Create a file in the root named `creds.json`.
* Add your access key and secret key to the file:
```
{
    "https://cad.onshape.com": {
        "access_key": "<your_access_key>",
        "secret_key": "<your_secret_key>"
    }
}
```