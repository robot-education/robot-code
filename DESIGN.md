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
Define custom `build` method which takes and passes on arbitrary **kwargs.
Add contexts to various statements.
Add `if_block` which wraps context.
Cleanup `Group` annotations.

Context implementations:
The goal is to add **kwargs to build to enable build tooling to be more context aware. 

The main use case for this is making it so that predicates, functions, annotations, and enum_values can change their behaviors automatically based on context.
For example, a predicate used within an expr environment should result in a predicate call, a predicate used within a top level statement environment should result in 
the predicate definition, and a ui test predicate used within a ui environment should result in the contents being inlined.

Contexts:
expr, statement, top, ui, enum.

Node should be changed to expose a build function.
Contexts should be special objects inheriting from `Context`. Their presence (i.e. not None) indicates that context is active.
A tab context should be made available?
Automatic inlining expressions is tricky. It's very hard without knowing beforehand. It's possible users could disable inline manually. 
If we know an expression should be run in inline, we can automatically inline as we go, dropping newlines before children whenever
the current `Width` context becomes to large?
As we run, we keep track of the current width. If the current expr puts us over the limit (and inline_expr is true), drop a newline (and possibly a tab) before the current token?
somethingBig & 
    somethingMedium & small & 
    somethingBig & small