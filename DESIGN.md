### Design
# onshape.py
The script works as follows.
Invoking `onshape pull` pulls code from the document specified in `cfg`.
The project supports multiple documents. Document code is pulled into a `local_code` folder.

By default, `onshape pull` pulls the default document. Other documents may be specified as necessary.
Operations work on `local_code`.

All operations work on `.fs_code`.

Current implementation:
Support for updating all documents via direct ops (i.e. pull, operation, push) to ensure synchronization
Support for building code and then pushing to the cloud
Support for cleaning local code
Built files are marked specially and are always pushed
File name resolution happens automatically when a file is pushed - if a file id doesn't exist in local storage, pulls the target document and searches it

We can use the source microversion to check if we're overriding remote code
For all files (including auto-generated ones), when pushing check the sourceMicroversion


To avoid accidentally overwriting the cloud, the following procedures are in place:
When code is pulled, a local fixed copy is automatically generated.
The fixed copy is used to determine necessary updates when pushing. 

When `push` is invoked, the following behaviors occur:
Each local file which has been modified is pulled. Note since feature studio names are used, this may require pulling down the document, checking for feature studios, and then pulling the final id. Generated files are never checked in this manner.
For each pulled file with a fixed copy, a comparison is made to the local copy. If any fixed copy doesn't match, `push` fails.
This catches the case where a user pulls code, makes some changes, then makes some changes in Onshape and attempts to push.

## Example
`onshape pull` - `.fs_code` and `.storage` are pulled from Onshape.
`onshape update --version` - Each file in `.fs_code` is updated to the latest version of the `std`.
`onshape build` - Each file in `fs_gen` is *imported*, executed, and scanned for studio objects. Each studio object is invoked with the appropriate `Path` and `std` version.
    `pull` is not required to be run before `build`.
`onshape push` - Each file in `.fs_code` which was modified is sent to Onshape.



# TODOs:
Add `make_annotation` method to `Enum`?

Readd `__rstr__` for `expr`? Start taking `object`?

