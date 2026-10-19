<!-- Copied from https://cad.onshape.com/FsDoc/imports.html by `fs fsdoc`; don't edit it here. -->

# Imports

## Direct FeatureScript imports

The FeatureScript import system can be used to bring in code or data from other elements into your Feature Studios. In a Feature Studio, you can import another Onshape tab with the import dialog, found by pressing the import button:

![Import button](https://cad.onshape.com/FsDoc/tutorials/images/import-button.png)

This dialog allows you to find Feature Studios and other tabs within this workspace and in other documents. Clicking on a tab in the dialog will insert import text into your Feature Studio with a unique id that points to the selected Onshape tab:

```
// Workspace import
import(path : "990d0d558752560035c1bc8e", version : "e83d3c3d23dea63825b44d09");

// Linked document import
import(path : "62bfa9d164ac3029984f7b53/c28fe04f33cbfe2c69f4e894/aa388ed6ebc848668b717010", version : "56c4b769218439ffa1a4699e");
```

Feature Studios cover the path ids (above) with a label (below) containing the human-readable names of that imported document, version, and tab. To reveal the underlying path ids, simply move your cursor into or highlight the text of that label.

![Import labels](https://cad.onshape.com/FsDoc/tutorials/images/import-labels.png)

The "Workspace import" above is a reference to another tab in the current workspace. Like all workspace references in Onshape, this import will update automatically when the imported tab is changed.

The "Linked document import" above is a reference to a tab in another document. Like all linked document references in Onshape, it is fixed to a specific version of that document. When a newer version of that document is created, the link icon will turn blue. At any time, you can click the link icon to update to the latest version, or to point the import to any other version. For more on creating, linking, and updating versions in Onshape, see the [linked documents documentation](https://cad.onshape.com/help/Content/linked-documents.htm).

Importing any type of tab will make a number of symbols visible (see below what symbols each tab type exposes). These symbols may be used directly in the importing Feature Studio:

```
var result = importedFunction();
```

Any import can optionally be prefixed by a namespace. If it is, its symbols may be accessed through the same namespace:

```
MyFunctions::import(...);
...
x = MyFunctions::importedFunction();
```

### Importing another Feature Studio

When importing another Feature Studio, all the symbols which have been exported become available to the importing Feature Studio. An exported symbol is any top level node which has been declared with the `export` prefix:

```
export function myFunction(x) {...}
export enum myEnum {...}
export type myType typecheck p;
export predicate myPredicate(value) {...}
```

By default, symbols imported by the current module are not exported by it. However, you can also `export` an `import` like any other top level node. This makes sense if, for instance, a module also exports functions that use an imported type or a feature that uses imported enums.

```
export import(path : "onshape/std/tool.fs", version : "");
```

### Importing external data

To guarantee that Part Studios always regenerate the same way, external data must be uploaded into an Onshape tab before FeatureScript can reference it.

Importing an uploaded tab imports the constant `BLOB_DATA`, which is a map with at least the following keys:

- `mediaType`: A string for the mime / media type of the uploaded data, such as "text/csv" or "image/png"
- `blobType`: A less specific type string. Currently one of "IMAGE", "CSV", "JSON", "TEXT, ""BREP", "OTHER"
- `dataId`: A string that identifies the data. Suitable for passing for `opImportForeign` if `blobType` is "BREP".

Onshape currently understands uploaded image, CSV, JSON, and TEXT files.

Data in CSV files (parsed using Apache Commons CSV Excel format) can be referenced via the `csvData` key: the corresponding value is an array. If the CSV file consists of one record (one line), each entry in the array is a field, represented as a number if the field can be parsed as a number, otherwise represented as a string. If the CSV file consists of multiple records, each entry in the `csvData` array is an array corresponding to one record.

Data in JSON files can be referenced via the `jsonData` key. The value is of a type corresponding to the top-level entity of the file: it is a map if the entity was an object, an array if the entity was an array, and analogously for other standard types. Note that the JSON `null` value has no equivalent in FeatureScript and is imported as `undefined`. Because an `undefined` entry in a map is considered nonexistent, this can mean that the imported maps report a smaller size than the number of entries in the JSON.

Data in TEXT files can be referenced via the `textData` key: the corresponding value is a String. A UTF-8 encoding is assumed.

An image will have the keys `imageWidth` and `imageHeight` mapped to numbers of pixels. Color data for individual pixels is not currently available to FeatureScript.

For example, to access the first element in an uploaded CSV file with multiple records:

```
MyData::import(path : "...", version : "...");
...
const firstEntry = MyData::BLOB_DATA.csvData[0][0];
...
```

### Importing a Part Studio

A Part Studio exports a function named `build`, which takes a configuration argument (as a map) and returns a context containing everything created in that Part Studio. It is often useful to utilize a Part Studio to create pieces of your feature (which may be transformed or booleaned in FeatureScript), rather than manually writing the code to build complex geometry in FeatureScript.

The [instantiator module](https://cad.onshape.com/FsDoc/library.html#module-instantiator.fs) provides a convenient mechanism for doing this. A simple feature which uses it to builds a Part Studio, merge in its context, and transform its bodies to a mate connector is below. Naturally, the import should be changed to an import of a Part Studio.

```
MyThing::import(path : "", version : "");

annotation { "Feature Type Name" : "Add thing" }
export const addThing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Mounting mate connector", "Filter" : BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
        definition.mountingConnector is Query;
    }
    {
        var thingTransform is Transform = toWorld(evMateConnector(context, {
               "mateConnector" : definition.mountingConnector
        }));

        var instantiator = newInstantiator(id + "instantiate");
        addInstance(instantiator, MyThing::build, { "transform" : thingTransform });
        instantiate(context, instantiator);
    });
```

This feature can be used to add bodies from the imported part studio and place them correctly all in one feature. Just like with other tab imports, changing the Part Studio will instantly update the feature and any Part Studios referencing the feature, so long as all are in the same workspace.

![Using the Add Thing feature](https://cad.onshape.com/FsDoc/tutorials/images/add-thing.png)

### Referencing a sheet metal form or stock geometry in a custom feature

To reference a [sheet metal form](https://cad.onshape.com/FsDoc/library.html#module-sheetMetalFormed.fs) or stock geometry in a custom feature, [import the Part Studio](imports.md#importing-a-part-studio) that owns the form, reconstruct the Part Studio in the current context as [`PartStudioData`](https://cad.onshape.com/FsDoc/library.html#PartStudioData), and then pass the `PartStudioData` into the native form tool or connected document.

For example, the code below references the sheet metal form from the imported Part Studio:

```
MyForm::import(path : "", version : "");

var formStudioData = {
    "buildFunction" : MyForm::build,
    "configuration" : {
        "Diameter" : definition.diameter,
        "Height" : definition.height,
        "Thickness" : definition.thickness
    },
    "partQuery" : qEverything(EntityType.BODY)
} as PartStudioData;

callSubfeatureAndProcessStatus(id, sheetMetalFormed, context, id + ("formed" ~ cs), {
    "targetFaces" : frontFace,
    "locations" : qCreatedBy(id + (toString(c) ~ "mateConnector"), EntityType.BODY),
    "formPartStudio" : formStudioData,
    "flipDirection" : definition.flipDirection
});
```

### Importing a Variable Studio

A Variable Studio exports a function named `getVariableMap`, which takes one argument, a configuration map, and returns a map containing the variables in the Variable Studio. Each map entry is formatted as `name -> {value, description}`.

In this example, the feature extrudes a face by the `extrude_length` variable in an imported Variable Studio, which is configured by the `x` variable. The `preTransform` length parameter is passed into the configuration map:

```
TransformLengthOptions::import(path : "", version : "");

annotation { "Feature Type Name" : "Extrude By Variable" }
export const extrudeFace = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
        definition.faceQuery is Query;
        annotation { "Name" : "Pre-transformed Length" }
        isLength(definition.preTransform, LENGTH_BOUNDS);
    }
    {
        const vMap = TransformLengthOptions::getVariableMap({ "x" : definition.preTransform });
        opExtrude(context, id + "extrude1", {
                "entities" : definition.faceQuery,
                "direction" : evOwnerSketchPlane(context, {"entity" : definition.faceQuery}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : vMap.extrude_length.value
        });
    });
```

## Reference parameters

A reference parameter is a feature input which allows the user to select any tab in Onshape (provided they have link permission), and provides the geometry or data in that tab to the custom feature. Unlike direct FeatureScript imports, the import of a reference parameter is added to the Part Studio, and the data is passed to the feature. This allows users to change what data a feature is using without changing the code of the feature itself.

Reference parameters can allow selection of either Part Studios, CSV files, JSON files, images, or imported CAD data.

### Part Studio

A Part Studio reference parameter provides the feature with [PartStudioData](https://cad.onshape.com/FsDoc/library.html#PartStudioData). This data structure can be passed directly into an [`instantiaor`](https://cad.onshape.com/FsDoc/library.html#module-instantiator.fs) to create the user-selected bodies:

```
annotation { "Feature Type Name" : "Place parts" }
export const placeParts = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Parts" }
        definition.partStudio is PartStudioData;
    }
    {
        var instantiator = newInstantiator(id + "instantiator");
        addInstance(instantiator, definition.partStudio);
        instantiate(context, instantiator);
    });
```

![Part Studio reference in feature dialog](https://cad.onshape.com/FsDoc/tutorials/images/part-studio-reference-parameter.png)

The `PartStudioData` value contains three fields: a `buildFunction`, a `partQuery`, and a `configuration`, all of which are used directly by the [`addInstance`](https://cad.onshape.com/FsDoc/library.html#addInstance-Instantiator-function-map) function above.

To instantiate a different set of bodies than what the user selected, you can modify the `partQuery` before calling the `addInstance` function. For example, the code below will also instantiate the mate connectors belonging to any selected parts.

```
definition.partStudio.partQuery = qUnion([
        definition.partStudio.partQuery,
        qMateConnectorsOfParts(definition.partStudio.partQuery)
    ]);
addInstance(instantiator, definition.partStudio);
```

Configurations are specified with the configuration input's FeatureScript ID. FeatureScript IDs can be viewed and modified with the [Edit FeatureScript IDs dialog](https://cad.onshape.com/help/Content/PartStudio/managing_configurations.htm#editFsIds).

For List configuration inputs, refer to a configuration value in the following format:

`PART_STUDIO_NAMESPACE::configInput_conf.configValue`

A List configuration input is considered an enum in FeatureScript. The enum type name is the configuration input name plus the `_conf` suffix. For example, to set the `Size` configuration input to the `Small` configuration value, use:

```
addInstance(instantiator, PART_STUDIO::build, {
    "configuration" : {
        "Size" : PART_STUDIO::Size_conf.Small
    }
});
```

For Checkbox (boolean) configuration inputs, set the boolean directly:

```
addInstance(instantiator, PART_STUDIO::build, {
    "configuration" : {
        "Has_Fillet" : false
    }
});
```

For Configuration variable inputs, include units for lengths and angles:

```
addInstance(instantiator, PART_STUDIO::build, {
    "configuration" : {
        "Length" : 1 * inch
    }
});
```

You can also override any configuration inputs in the `addInstance` function. When doing this, it is recommended that you also annotate the reference parameter with a list of `"ComputedConfigurationInputs"`. These inputs are then marked as "Computed" and cannot be edited, preventing any user confusion about why their input is being overridden.

```
annotation { "Feature Type Name" : "Place with length" }
export const placePartWithLength = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Part Studio", "ComputedConfigurationInputs" : [ "Length" ] }
        definition.partStudio is PartStudioData;
    }
    {
        const computedLength = getComputedLength(context, id, definition);
        const instantiator = newInstantiator(id + "instantiator");
        addInstance(instantiator, definition.partStudio, {
            "configurationOverride" : {
                "Length" : computedLength
            }
        });
        instantiate(context, instantiator);
    });
```

![Place with length](https://cad.onshape.com/FsDoc/tutorials/images/place-part-with-length.png)

The Part Studio reference dialog by default allows selecting all available [`PartStudioItemType`](https://cad.onshape.com/FsDoc/library.html#PartStudioItemType)s. You can limit selection by specifying a `"Filter"` containing a union of any number of item types. When setting this filter, it is recommended that you also add a similar filter to the body of your feature, so the user can select the entire Part Studio without providing unexpected geometry to your feature.

```
annotation { "Feature Type Name" : "Place solids" }
export const myFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Parts", "Filter" : PartStudioItemType.SOLID || PartStudioItemType.ENTIRE_PART_STUDIO }
        definition.parts is PartStudioData;
    }
    {
        definition.parts.partQuery = qBodyType(definition.things.partQuery, BodyType.SOLID);

        const instantiator = newInstantiator(id + "instantiator");
        addInstance(instantiator, definition.parts);
        instantiate(context, instantiator);
    });
```

While a reference parameter always refers to a single tab, it will, by default, allow selecting any number of items within that tab. To allow only a single selection, you can add `"MaxNumberOfPicks" : 1` to the parameter annotation (other values of MaxNumberOfPicks are not yet supported).

```
annotation { "Name" : "Sketch profile", "Filter" : PartStudioItemType.SKETCH, "MaxNumberOfPicks" : 1 }
definition.sketchProfile is PartStudioData;
```

For more in-depth examples (or to try out the example features above), see the [reference parameter examples document](https://cad.onshape.com/documents/c407dcc5516019b9d00703fd).

### Variable Studio

A Variable Studio reference parameter provides the feature with [VariableStudioData](https://cad.onshape.com/FsDoc/library.html#VariableStudioData). The variables from the referenced Variable Studio are accessed with the `getVariableMap` function. Each map entry is formatted as `name -> {value, description}`.

This example uses a referenced Variable Studio to generate a helix in the Part Studio. `getVariableMap` takes a configuration set to that of the reference parameter, which enables configuring the Variable Studio in the reference parameter's dialog.

```
annotation { "Feature Type Name" : "Helix Generator" }
export const helixGenerator = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Helix Options" }
        definition.helixOptionsStudio is VariableStudioData;
    }
    {
        const vMap = definition.helixOptionsStudio.getVariableMap(definition.helixOptionsStudio.configuration);
        opHelix(context, id + "helix1", {
            "direction" : vMap.direction.value,
            "axisStart" : vMap.axisStart.value,
            "startPoint" : vMap.startPoint.value,
            "interval" : vMap.interval.value,
            "clockwise" : vMap.clockwise.value,
            "helicalPitch" : vMap.helicalPitch.value,
            "spiralPitch" : vMap.spiralPitch.value,
        });
    });
```

### Image

An image reference parameter provides the feature with [ImageData](https://cad.onshape.com/FsDoc/library.html#ImageData). The referenced image can be placed into a Part Studio by calling [skImage](https://cad.onshape.com/FsDoc/library.html#skImage).

```
annotation { "Feature Type Name" : "Place image" }
export const placeImage = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Image" }
        definition.image is ImageData;

        annotation { "Name" : "Image width" }
        isLength(definition.imageWidth, LENGTH_BOUNDS);
    }
    {
        var sketch1 = newSketch(context, id + "sketch1", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
        skImage(sketch1, "image1", {
                "blobInfo" : definition.image,
                "firstCorner" : vector(0 * inch, 0 * inch),
                "secondCorner" : vector(definition.imageWidth, 0 * inch) // calculate height automatically
        });
        skSolve(sketch1);
    });
```

For more in-depth examples (or to try out the example feature above), see the [reference parameter examples document](https://cad.onshape.com/documents/c407dcc5516019b9d00703fd).

### CSV

A CSV reference parameter provides the feature with [TableData](https://cad.onshape.com/FsDoc/library.html#TableData). The referenced data can be used by the feature to create new geometry like points or splines:

```
annotation { "Feature Type Name" : "Create 3D spline" }
export const create3dSpline = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Spline points" }
        definition.splinePointsTable is TableData;
    }
    {
        const csvUnits = inch;
        const points = mapArray(definition.splinePointsTable.csvData, function(row) {
            // map CSV columns 0, 1, and 2 to a vector of X, Y, Z
            return vector(row[0], row[1], row[2]) * csvUnits;
        });

        opFitSpline(context, id + "fitSpline1", {
                "points" : points
        });
    });
```

### JSON

A JSON reference parameter provides the feature with [JSONData](https://cad.onshape.com/FsDoc/library.html#JSONData). The data can be accessed like any other FeatureScript type.

```
annotation { "Feature Type Name" : "Sketch text" }
export const sketchText = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Person data" }
        definition.personData is JSONData;
    }
    {
        var sketch1 = newSketch(context, id + "sketch1", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
        skText(sketch1, "text1", {
                "text" : definition.personData.jsonData.firstName,
                "fontName" : "OpenSans-Regular.ttf",
                "firstCorner" : vector(0, 0) * inch,
                "secondCorner" : vector(1, 1) * inch
        });
        skSolve(sketch1);
    });
```

### Text

A text reference parameter provides the feature with [TextData](https://cad.onshape.com/FsDoc/library.html#TextData). The data can be accessed as a string, provided that the text file is under 100KB.

```
annotation { "Feature Type Name" : "Text content" }
export const textFile = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Text data" }
        definition.message is TextData;
    }
    {
        var textContent = definition.message.textData;
        println(textContent);
    });
```

### CAD Import

A CAD import reference parameter provides the feature with [CADImportData](https://cad.onshape.com/FsDoc/library.html#CADImportData). The referenced geometry can be placed into a Part Studio by calling [opImportForeign](https://cad.onshape.com/FsDoc/library.html#opImportForeign). A CAD import reference parameter allows selecting blob tabs with [imported CAD data uploaded to Onshape](https://www.onshape.com/cad-blog/import-export-onshape).

```
annotation { "Feature Type Name" : "Place import" }
export const placeBrep = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Brep" }
        definition.brep is CADImportData;
    }
    {
        if (size(definition.brep) > 0)
        {
            opImportForeign(context, id + "importForeign1", {
                "blobData" : definition.brep
            });
        }
    });
```

For more in-depth examples (or to try out the example feature above), see the [reference parameter examples document](https://cad.onshape.com/documents/c407dcc5516019b9d00703fd).

## Updating imports

FeatureScript imports, like all Onshape references, come in two flavors:

- **Workspace reference:** Inside the current document and workspace, imports of other tabs will always be up-to-date. Any changes to the other tab will propagate to the current tab without any additional user action.
- **Version reference:** Outside the current workspace, imports are always made at a single version (usually, a version of another document). Changes to the other tab will *not* automatically propagate. Instead, when a new version of the other tab is created, the referencing document always has the choice of whether or not to update.

In both the FeatureScript import dialog and the reference parameter dialog, the "Current document" tab will default to creating a workspace reference. The "Other documents" tab will always create a version reference.

In a direct Feature Studio import, the only way to update the import is by deleting the import statement, then using the import dialog to reinsert an import at another version.

With reference parameters, any feature with a version reference will display a link icon. The icon will turn blue when newer version is created, and a user can click this icon at any time to update to the latest version (or select any other version). ![Update linked document button](https://cad.onshape.com/FsDoc/tutorials/images/update-linked-document-icon.png)
