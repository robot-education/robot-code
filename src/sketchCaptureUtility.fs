FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export enum StorageType
{
    annotation { "Name" : "Single" }
    SINGLE,
    annotation { "Name" : "Map" }
    MAP
}

export enum MapKeyType
{
    annotation { "Name" : "Enum" }
    ENUM,
    annotation { "Name" : "String" }
    STRING
}

annotation { "Feature Type Name" : "Utility - sketch capture" }
export const sketchCapture = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Storage type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.storageType is StorageType;

        if (definition.storageType == StorageType.MAP)
        {
            annotation { "Name" : "Map key type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
            definition.mapKeyType is MapKeyType;
        }

        annotation { "Name" : "Variable name", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.variableName is string;

        if (definition.storageType == StorageType.SINGLE)
        {
            annotation { "Name" : "Sketch entities to save", "Filter" : EntityType.EDGE && SketchObject.YES }
            definition.singleEntities is Query;
        }
        else if (definition.storageType == StorageType.MAP)
        {
            if (definition.mapKeyType == MapKeyType.ENUM)
            {
                annotation { "Name" : "Enum name" }
                definition.enumName is string;
            }

            annotation { "Name" : "Sketches", "Item name" : "Sketches", "Item label template" : "#key" }
            definition.sketches is array;
            for (var sketch in definition.sketches)
            {
                annotation { "Name" : "Key" }
                sketch.key is string;

                annotation { "Name" : "Sketch entities to save", "Filter" : EntityType.EDGE && SketchObject.YES }
                sketch.entities is Query;
            }
        }
    }
    {
        if (definition.variableName == "")
        {
            throw regenError("Enter a valid variable name.", ["variableName"]);
        }

        if (definition.storageType == StorageType.SINGLE)
        {
            verifyNonemptyQuery(context, definition, "singleEntities", "Select sketch entities to save.");
            if (isQueryEmpty(context, definition.singleEntities->qCoincidesWithPlane(XY_PLANE)))
            {
                throw regenError("Selected entities must lie on the XY plane.", ["singleEntities"], definition.singleEntities);
            }

            printSingle(context, definition);
        }
        if (definition.storageType == StorageType.MAP)
        {
            verifyNonemptyArray(context, definition, "sketches", "Add one or more sketches to save.");
            for (var i, sketch in definition.sketches)
            {
                if (isQueryEmpty(context, sketch.entities))
                {
                    throw regenError("Select sketch entities to use.", ["sketches[" ~ i ~ "].entities"]);
                }

                if (isQueryEmpty(context, sketch.entities->qCoincidesWithPlane(XY_PLANE)))
                {
                    throw regenError("Selected entities must lie on the XY plane.", ["sketches[" ~ i ~ "].entities"], sketch.entities);
                }
            }

            printMap(context, definition);
        }

        println("");
        reportFeatureInfo(context, id, "Open the console ({✔}), then copy the result code and paste it into a feature studio.");
    });

function printSingle(context is Context, definition is map)
{
    println('const ' ~ definition.variableName ~ ' = [');

    for (var entity in evaluateQuery(context, definition.singleEntities))
    {
        printEntity(context, entity);
    }
    println('   ] as SketchDataArray;');
}

function printMap(context is Context, definition is map)
{
    println('const ' ~ definition.variableName ~ ' = {');

    for (var sketch in definition.sketches)
    {
        if (definition.mapKeyType == MapKeyType.ENUM)
        {
            println('           ' ~ definition.enumName ~ '.' ~ sketch.key ~ ' : [');
        }
        else if (definition.mapKeyType == MapKeyType.STRING)
        {
            println('           \"' ~ sketch.key ~ '\" : [');
        }

        for (var entity in evaluateQuery(context, sketch.entities))
        {
            printEntity(context, entity);
        }
        println('       ] as SketchDataArray,');
    }

    println('};');
}

function printEntity(context is Context, entity is Query)
{
    print('             { \"operation\" : SketchOperation.');
    if (!isQueryEmpty(context, entity->qGeometry(GeometryType.LINE)))
    {
        print('LINE, \"start\" : ');
        printVertex(context, qEdgeVertex(entity, true));

        print(', \"end\" : ');
        printVertex(context, qEdgeVertex(entity, false));
    }
    else if (!isQueryEmpty(context, entity->qGeometry(GeometryType.ARC)))
    {
        print('ARC, \"start\" : ');
        printVertex(context, qEdgeVertex(entity, true));

        print(', \"mid\" : ');
        const mid = evEdgeTangentLine(context, {
                        "edge" : entity,
                        "parameter" : 0.5
                    }).origin;
        printPoint(mid);

        print(', \"end\" : ');
        printVertex(context, qEdgeVertex(entity, false));
    }
    else if (!isQueryEmpty(context, entity->qGeometry(GeometryType.CIRCLE)))
    {
        const curve = evCurveDefinition(context, { "edge" : entity });
        print('CIRCLE, \"center\" : ');
        printPoint(curve.coordSystem.origin);
        print(', \"radius\" : ');
        print(roundToPrecision(curve.radius / millimeter, 4) ~ ' * millimeter');
    }
    println(' },');
}

function printVertex(context is Context, vertex is Query)
{
    printPoint(evVertexPoint(context, { "vertex" : vertex }));
}

function printPoint(point is Vector)
{
    point = worldToPlane(XY_PLANE, point);
    print("vector(" ~ roundToPrecision(point[0] / millimeter, 4) ~ ", " ~ roundToPrecision(point[1] / millimeter, 4) ~ ") * millimeter");
}
