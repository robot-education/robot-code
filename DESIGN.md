### Design
# onshape.py
The script works as follows.
Invoking `onshape pull` pulls code from the document specified in `cfg`.
The project supports multiple documents. Document code is pulled into a `local_code` folder.

By default, `onshape pull` pulls the default document. Other documents may be specified as necessary.
Operations work on `local_code`.

## Example
`onshape pull` - `.fs_code` and `.storage` are pulled from Onshape.
`onshape update --version` - Each file in `.fs_code` is updated to the latest version of the `std`.
`onshape build` - Each file in `fs_gen` is *imported*, executed, and scanned for studio objects. Each studio object is invoked with the appropriate `Path` and `std` version.
    `pull` is not required to be run before `build`.
`onshape push` - Each file in `.fs_code` which was modified is sent to Onshape.

# Rewrite:
I propose the following changes.
1. Writing a custom JavaScript/Python GH Actions workflow which syncs a repo (e.g. main) with one or more Onshape documents. The repo would specify the urls. Pushing to the repo would deploy to Onshape automatically.
2. Modifying these scripts to support "local" development tests. This would involve compiling FeatureScript and pushing them to a temporary Onshape document for testing. Caching would be done to help keep things synced, but because the files are considered local only, the doucment should be considered "temporary". In particular:
When code is first pushed, the document should be cleaned. Unused studios should be removed.
We can then create all of the neccessary feature studios, and construct a mapping of document ids to element ids.
We can then compile the FeatureScript code using the target document id mapping. Finally, we can push the FeatureScript code.
(Note - Github FeatureScript will never be true FeatureScript code, so no more editing in Onshape directly. Tragic.). 
Local history should be stored (the document name mapping) and used to determine if additional pushes are needed.
If a local studio is deleted, that should trigger cascading changes to the compilation of the studios which import it (we could store it as a map or recompile every time).
When a push occurs, the changed studios should be updated as needed. 

A VSCode extension to deploy code might be able to listen for rename changes directly, saving the above logic. It could also probably handle hmr automatically.
I may eventually implement HMR compiling, but that's a problem for future me.

The AST API should be broken into a custom build script which buils python AST files into OS files. When code is pushed, the GH actions script should compile (if neccessary) and deploy the compiled code. GitHub should always be the single source of truth for the Onshape document.



# TODOs:
Define custom `build` method which takes and passes on arbitrary **kwargs.
Add contexts to various statements.
Cleanup `Group` annotations.

Context implementations:
We would like to re-add the autocall behavior for predicates and enums.
Add an option for registering a predicate when making an enum? Maybe, maybe

Call stack:
Re-add Definition construct
Definition, Statement, and Expression should be mutually exclusive singleton
UI and type should be the same
Does singleton actually fix behavior? 
Definition and Expr, get Expr
Predicate is a Definition and an Expr
There aren't any Statement Expr constructs anymore
EnumValue is a Expr only
This also handles them going down the entire tree too far, yay!
Should Statement and Expr register themselves automatically?
It would need to be a prebuild thing (like I had before)
I think that works, yeah
Attributes should always roll back, hard to imagine a case where they shouldn't

The main use case for this is making it so that predicates, functions, annotations, and enum_values can change their behaviors automatically based on context.
For example, a predicate used within an expr environment should result in a predicate call, a predicate used within a top level statement environment should result in 
the predicate definition, and a ui test predicate used within a ui environment should result in the contents being inlined.

Contexts:
Problem: we wish to manage contexts in an effective (read: mandated) way. 
The issue is that knowing the *expected* type of a value can only be done by specifying the type passed to every caller.

In particular, suppose we pass a predicate (which is both an expression and a definition) to a function. The problem is that it looks like both, so *any* class based
solution for solving this issue in a systematic way won't work. So, the *type* of a class passed to `build` is no longer sufficient for determining the behavior; the
*call site* itself must specify its expectation - yikes!

If we were to eliminate this behavior, the main changes would be:
1. Code would get a bit uglier. Callables would be required to be called once again. Enums could remain as standard `expr`. Type checking would improve. 
Type checking would be more useful - for example, functions could be required to register themselves to an appropriate context.
2. On the other hand, this would clarify the call behavior of classes, which is a plus. On the other other hand, it's annoying, and this is my library.
3. `expr` and `stmt` could offer hooks exposing their `Level`. `Context` would automatically be set to the `Level` of the passed in `node`. OTOH, type checking
   makes this behavior moot; the type's behavior is already guaranteed to be defined at runtime. One exception is `ui` (and predicate expansion),
    but that already filters down automatically.
    Enum is already used to robustly handle the `enum` case. A specialized build function would also work. 
    The remaining case is `callable`, which is nifty but, again, hard to justify. Integrating calls also makes an external `Call` expr harder to justify?
So, we no longer need `level`. Like, at all - no features should be customizing their behavior in that way.
An excpetion is `top_level`. I'm happy just making a dedicated `ParentNode` class for that.

