<!-- Copied from https://cad.onshape.com/FsDoc/index.html by `fs fsdoc`; don't edit it here. -->

# Welcome to FeatureScript

FeatureScript is a new programming language designed by Onshape for building and working with 3D parametric models. The language is built into Onshape from the ground up, providing the foundation of Part Studio modeling, including robust geometric references, powerful parametric tools, and a type system with types built for math in three dimensions.

The standard feature types in Onshape (like Extrude, Fillet, and Helix) are already written as FeatureScript functions by our developers. Custom feature types extend this same mechanism to all users of Onshape. FeatureScript can also be used to write [custom table types](tables.md) that gather and process data from a Part Studio.

FeatureScript can be added to any new or existing Onshape document by creating a Feature Studio. This short video shows how to create and use a new custom feature type:

[Video](https://cad.onshape.com/FsDoc/intro.mp4) Your browser does not support the video tag.

## Tutorials

The FeatureScript tutorials introduce the essential concepts of writing FeatureScript, while instructing you how to construct and improve your own custom feature types.

[Start FeatureScript tutorials](tutorials/create-a-slot-feature.md)

## The Onshape Standard Library

The Onshape Standard library provides all of Onshape's features (like Extrude and Fillet), as well as a large number of functions and types designed to work with geometry and help build custom features. All functions in the Standard Library are imported by default into all new Feature Studios.

### Documentation

The documentation for the standard library is a useful reference for the functions and types you will use when writing FeatureScript.

[View Standard Library documentation](https://cad.onshape.com/FsDoc/library.html)

### Source code

The Onshape Standard Library is open-source and freely viewable in a public Onshape document named "std". Viewing the library's source is often useful for finding examples of functions and types in action. You can search through the document by pressing the search button on the top navbar.

[View Standard Library source code](https://cad.onshape.com/documents/12312312345abcabcabcdeff)

## Example Features

Onshape's Custom Feature Library contains 50+ high-quality, general-use custom features from the Onshape community. Each is documented with a description of how to use it, and each has public source code that can be viewed (or copied and modified).

[View custom feature examples](https://www.onshape.com/en/features/custom-features)
