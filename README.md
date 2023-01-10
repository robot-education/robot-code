# FeatureScript Code Generator

A set of python scripts for updating and maintaining certain aspects of my FeatureScript projects using Onshape's API.
My actual FeatureScript code can be found here:
[Alex's FeatureScript Backend](https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f)

---

### updateversion.py
A script which pulls down every feature studio and runs a simple regex replace to automatically change all imports to point to the latest version of the Onshape Standard Library.

### Local Setup
Run the following commands to get setup:
```
python3 -m venv venv
source venv/Scripts/activate
pip install -r requirements.txt
```

To exit the virtual environment at any time, simply type `deactivate`.

### Running the App

Run scripts using `python3`, e.g. `python3 src/update_fs.py`.
Debug scripts using the vs-code launch tasks.