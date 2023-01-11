# FeatureScript Code Generator

A set of python scripts for updating and maintaining certain aspects of my FeatureScript projects using Onshape's API.
My actual FeatureScript code can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

---

### update_versions.py
A script which pulls down every feature studio and runs a simple regex replace to automatically change all imports to point to the latest version of the Onshape Standard Library.



### Local Setup
This project uses `pipenv` to manage python dependencies.
1. Install python and pipenv.
2. Run code using: `pipenv run python tools/<script>.py`.
3. Alternatively, run `pipenv shell`, then run code using `python tools/<script>.py`.

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

To enable running scripts more easily, consider adding the following bash function to your `.bashrc`:
```
fssync() {
    pipenv run python ./tools/script.py $@
}
```
You may then execute commands as follows:
* `fssync pull` - Pulls code from Onshape into src
* `fssync push` - Pushes code in src to Onshape
* `fssync update` - Updates code in src to use latest version of the Onshape STD