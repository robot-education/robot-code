FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/location.fs", version : "");
import(path : "core/mounting.fs", version : "");
import(path : "core/pointManipulator.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
// The extrude options; also exports the enums they use, which are parameter types
export import(path : "core/stdExtrude.fs", version : "");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");
import(path : "derive/edgeDerive.fs", version : "");

// TODO: import the nut strip Part Studio from FRCDesignLib, and opPointTransform.fs for partStudioData

/**
 * How nut strips are placed.
 */
export enum NutStripPlacement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT
}

/**
 * The thread of a nut strip's holes.
 */
export enum NutStripThread
{
    annotation { "Name" : "#10-32" }
    NUMBER_10_32,
    annotation { "Name" : "1/4-20" }
    QUARTER_20
}

export predicate isEdgePlacement(definition is map)
{
    definition.placement == NutStripPlacement.EDGE;
}

/**
 * Places nut strips along edges (built on edge derive), or extrudes one from a point.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add nut strips along edges, such as the inside edges of tube, or extrude one from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotNutStripManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic"
    }
export const robotNutStrip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Placement", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.placement is NutStripPlacement;

        annotation { "Name" : "Thread", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.thread is NutStripThread;

        if (isEdgePlacement(definition))
        {
            annotation { "Name" : "Edges", "Filter" : EntityType.EDGE && GeometryType.LINE, "UIHint" : ["UNCONFIGURABLE"] }
            definition.edges is Query;

            edgeDeriveParametersPredicate(definition);

            edgeDeriveTransformPredicate(definition);
        }
        else
        {
            locationPredicate(definition, "nut strip");

            ninePointManipulatorPredicate(definition);

            annotation { "Group Name" : "Extrude", "Collapsed By Default" : false }
            {
                // The opposite direction button flips the direction the nut strip is drawn in, so the rotate button
                // goes next to it, as in the std Transform feature
                newExtrudeEndTypePredicate(definition);
                secondaryAxisPredicate(definition);
                newExtrudeBoundsPredicate(definition);
            }
        }
    }
    {
        if (isEdgePlacement(definition))
        {
            verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to add nut strips to.");
            definition.partStudioData = nutStripPartStudioData(definition);
            // use the top level id to get edge derive's manipulators
            edgeDerive(context, id, definition);
        }
        else
        {
            extrudeNutStrip(context, id, definition);
        }
    }, {
            "placement" : NutStripPlacement.EDGE,
            "parameters" : [],
            "edgeQuery" : qNothing(),
            "transform" : false,
            "oppositeDirection" : false,
            "secondaryAxisType" : MateConnectorAxisType.PLUS_X,
            "index" : NINE_POINT_CENTER_INDEX
        });

/**
 * Extrudes a nut strip from the selected location. Its length comes from the extrude options, and it's drawn in the
 * extrude's direction (which matters since its holes alternate), rotated by `secondaryAxisType`. The nine point
 * manipulator chooses which point of its start lines up with the location.
 */
function extrudeNutStrip(context is Context, id is Id, definition is map)
{
    const plane = getLocationPlane(context, definition);

    // Extrude a small face with the extrude options and measure it, so they (and their manipulators) work as usual.
    // Use the top level id to get extrude's manipulators.
    const extrudeDefinition = transformDefintionForNewExtrude(definition, sketchLengthFace(context, id + "lengthFace", plane));
    callSubfeatureAndProcessStatus(id, extrude, context, id, extrudeDefinition, {
                "featureParameterMap" : { "entities" : "location" }
            });
    // Z along the extrude's direction (its opposite direction flip), and X reoriented by secondaryAxisType
    const drawPlane = applyAxisOrientation(definition, plane);
    const extent = evBox3d(context, {
                "topology" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "cSys" : coordSystem(drawPlane),
                "tight" : true
            });
    opDeleteBodies(context, id + "deleteLength", { "entities" : qCreatedBy(id, EntityType.BODY) });

    var partStudioData = nutStripPartStudioData(definition);
    partStudioData.configuration = mergeMaps(partStudioData.configuration ?? {}, {
                "length" : extent.maxCorner[2] - extent.minCorner[2]
            });
    partStudioData.partQuery = partStudioData.partQuery->qBodyType(BodyType.SOLID);
    const instantiator = newInstantiator(id + "nutStrip");
    const nutStrip = addInstance(instantiator, partStudioData);
    instantiate(context, instantiator);

    // The nut strip's length is along Z
    const points = ninePoints(evBox3d(context, { "topology" : nutStrip, "tight" : true }));
    var location = coordSystem(drawPlane);
    location.origin += drawPlane.normal * extent.minCorner[2];
    const placement = toWorld(location) * transform(-points[getPointIndex(definition, size(points))]);

    opTransform(context, id + "transform", { "bodies" : nutStrip, "transform" : placement });
    addPointManipulator(context, id, definition, mapArray(points, function(point)
            {
                return placement * point;
            }));
}

/**
 * Sketches a small circle on `plane` for extrudeNutStrip to extrude, and returns its face.
 */
function sketchLengthFace(context is Context, id is Id, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(sketch, "circle", { "center" : vector(0, 0) * meter, "radius" : 1 * millimeter });
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * The nut strip to derive. Its `length` configuration input is set to the length to make it, which is along its Z
 * axis.
 */
function nutStripPartStudioData(definition is map) // returns PartStudioData
{
    // TODO: return the FRCDesignLib nut strip once it's imported and standardized to take a `length` configuration
    // input, e.g. partStudioData(NutStrip::build, { "thread" : definition.thread });
    throw regenError("Nut strips aren't available yet.");
}

/**
 * @internal
 * The manipulator change function for robot nut strip.
 */
export function robotNutStripManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (isEdgePlacement(definition))
    {
        return edgeDeriveManipulatorChange(context, definition, newManipulators);
    }
    definition = pointManipulatorChange(definition, newManipulators);
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for robot nut strip.
 */
export function robotNutStripEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (isEdgePlacement(definition))
    {
        return edgeDeriveEditLogic(context, id, oldDefinition, definition, isCreating);
    }
    definition.entities = qNothing();
    if (!isQueryEmpty(context, definition.location))
    {
        try silent
        {
            definition.entities = sketchLengthFace(context, id + "lengthFace", getLocationPlane(context, definition));
        }
    }
    return stdNewExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
