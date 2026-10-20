FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
// Also exports std's hole.fs
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "bb7c494edc43a307e631af8d");
import(path : "f58a965fe7005e2e3c9a67c7", version : "d96cdaac91eb8183e20e6de7");
// The hole tables Robot shaft and Robot spacer use
import(path : "8fc3df84a88e74d27ad43d26", version : "06fb8de71c6fd5a2c48eb7fd");

/**
 * What Robot edit does to the parts it's given.
 */
export enum RobotEditType
{
    annotation { "Name" : "Modify hole" }
    MODIFY_HOLE,
    annotation { "Name" : "Delete pocket" }
    DELETE_POCKET
}

/**
 * The kind of hole Modify hole makes a hole: a simple hole of any diameter, or a clearance or tapped hole from a hole
 * table.
 */
export enum ModifyHoleType
{
    annotation { "Name" : "Simple" }
    SIMPLE,
    annotation { "Name" : "Clearance" }
    CLEARANCE,
    annotation { "Name" : "Tapped" }
    TAPPED
}

predicate isModifyHole(definition is map)
{
    definition.editType == RobotEditType.MODIFY_HOLE;
}

predicate isClearanceHoleType(definition is map)
{
    definition.holeType == ModifyHoleType.CLEARANCE;
}

predicate isTappedHoleType(definition is map)
{
    definition.holeType == ModifyHoleType.TAPPED;
}

/**
 * Edits parts directly, whatever made them: Modify hole changes holes' diameters, and makes them simple, clearance, or
 * tapped holes; Delete pocket deletes pockets, filling them in.
 */
annotation { "Feature Type Name" : "Robot edit",
        "Feature Type Description" : "Edit parts directly: change holes' diameters or tap them, or delete pockets." ~ CREDIT,
        "Editing Logic Function" : "robotEditEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotEdit = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Edit type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.editType is RobotEditType;

        if (isModifyHole(definition))
        {
            annotation { "Name" : "Holes to modify",
                        "Filter" : EntityType.FACE && GeometryType.CYLINDER && SketchObject.NO && ConstructionObject.NO && ModifiableEntityOnly.YES && AllowMeshGeometry.NO,
                        "Description" : "A face of each hole." }
            definition.holes is Query;

            annotation { "Name" : "Hole type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.holeType is ModifyHoleType;

            if (isTappedHoleType(definition))
            {
                annotation { "Name" : "Hole table", "Lookup Table" : tappedHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.tappedHolePath is LookupTablePath;
            }
            else if (isClearanceHoleType(definition))
            {
                annotation { "Name" : "Hole table", "Lookup Table" : clearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.clearanceHolePath is LookupTablePath;
            }

            // Editing logic sets it from the hole table; it can be overridden
            holeDiameterPredicate(definition);
        }
        else
        {
            annotation { "Name" : "Pockets to delete",
                        "Filter" : EntityType.FACE && SketchObject.NO && ConstructionObject.NO && ModifiableEntityOnly.YES && AllowMeshGeometry.NO,
                        "Description" : "A face of each pocket. Every face tangent to it is deleted too." }
            definition.pockets is Query;
        }
    }
    {
        if (isModifyHole(definition))
        {
            modifyHoles(context, id, definition);
        }
        else
        {
            deletePockets(context, id, definition);
        }
    });

/**
 * Sets Hole diameter from the hole table when it changes (or Hole type does), as Robot shaft does; it can be
 * overridden.
 */
export function robotEditEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    const diameter = getTableHoleDiameter(definition);
    if (diameter != undefined && (oldDefinition.holeType != definition.holeType || getTableHoleDiameter(oldDefinition) != diameter))
    {
        definition.holeDiameter = diameter;
    }
    return definition;
}

/**
 * The hole table's diameter for a clearance or tapped hole (a tapped hole's tap drill), or undefined for a simple hole.
 */
function getTableHoleDiameter(definition is map)
{
    var diameter;
    if (isTappedHoleType(definition))
    {
        diameter = getLookupTable(tappedHoleTable, definition.tappedHolePath).tapDrillDiameter;
    }
    else if (isClearanceHoleType(definition))
    {
        diameter = getLookupTable(clearanceHoleTable, definition.clearanceHolePath).holeDiameter;
    }
    return diameter is string ? lookupTableEvaluate(diameter) : undefined;
}

/**
 * What Modify hole marks holes as (see `setHoleAttributes`): Hole diameter, and a clearance hole's size, or a tapped
 * hole's size and thread, from its hole table.
 */
export function getHoleSpec(definition is map) returns map
{
    if (isTappedHoleType(definition))
    {
        const path = definition.tappedHolePath;
        const thread = { "size" : path.size, "pitch" : path.pitch, "majorDiameter" : getLookupTable(tappedHoleTable, path).majorDiameter };
        return { "holeDiameter" : definition.holeDiameter, "size" : path.size, "thread" : thread };
    }
    if (isClearanceHoleType(definition))
    {
        return { "holeDiameter" : definition.holeDiameter, "size" : definition.clearanceHolePath.size };
    }
    return { "holeDiameter" : definition.holeDiameter };
}

/**
 * Modify hole: changes each selected hole's diameter, and marks it as the chosen kind of hole.
 *
 * 1. Find the holes: each selected face's hole (`getHoleFaces`), once, checking they're holes, and not ones std's Hole
 *    counterbored or countersunk.
 * 2. Offset each hole's faces to Hole diameter, if it isn't already.
 * 3. Mark the holes (`setHoleAttributes`), replacing what they were marked with, from where they start and how deep
 *    they go now (`measureHole`).
 */
function modifyHoles(context is Context, id is Id, definition is map)
{
    if (isQueryEmpty(context, definition.holes))
    {
        throw regenError("Select holes to modify.", ["holes"]);
    }

    // 1. Find the holes
    const notHoles = filter(evaluateQuery(context, definition.holes), function(face)
        {
            return !isHoleFace(context, face);
        });
    if (notHoles != [])
    {
        throw regenError("Some selected faces aren't holes.", ["holes"], qUnion(notHoles));
    }
    var holes = [];
    var found = [];
    for (var face in evaluateQuery(context, definition.holes))
    {
        if (!isIn(face, found))
        {
            const faces = getHoleFaces(context, face);
            found = concatenateArrays([found, faces]);
            holes = append(holes, qUnion(faces));
        }
    }
    const counterbored = filter(holes, function(faces)
        {
            return getHoleAttributes(context, faces)->any(function(attribute)
                {
                    return attribute.holeType != undefined && attribute.holeType != HoleStyle.SIMPLE;
                });
        });
    if (counterbored != [])
    {
        throw regenError("Counterbored and countersunk holes can't be modified.", ["holes"], qUnion(counterbored));
    }

    // 2. Offset each hole's faces to Hole diameter
    const radius = definition.holeDiameter / 2;
    var modified = [];
    for (var i, faces in holes)
    {
        const cylinder = evSurfaceDefinition(context, { "face" : qNthElement(faces, 0) });
        modified = append(modified, qUnion([faces, startTracking(context, faces)])->qGeometry(GeometryType.CYLINDER));
        if (tolerantEquals(cylinder.radius, radius))
        {
            continue;
        }
        try
        {
            // A hole's faces face its axis, so offsetting them out of the part shrinks it
            opOffsetFace(context, id + ("offset" ~ i), { "moveFaces" : faces, "offsetDistance" : cylinder.radius - radius });
        }
        catch
        {
            // A failed offset changes nothing, so the hole is still there to show
            processSubfeatureStatus(context, id, { "subfeatureId" : id + ("offset" ~ i), "propagateErrorDisplay" : true });
            throw regenError("Failed to change the hole's diameter.", ["holes", "holeDiameter"], faces);
        }
    }

    // 3. Mark the holes
    var measured = [];
    for (var faces in modified)
    {
        clearHoleAttributesAndThreads(context, faces);
        measured = append(measured, measureHole(context, faces));
    }
    setHoleAttributes(context, id, measured, getHoleSpec(definition));
}

/**
 * Whether a cylindrical face is a hole's, rather than a boss's or a shaft's: whether it faces its axis.
 */
function isHoleFace(context is Context, face is Query) returns boolean
{
    const axes = evSurfaceDefinition(context, { "face" : face }).coordSystem;
    const tangentPlane = evFaceTangentPlane(context, { "face" : face, "parameter" : vector(0.5, 0.5) });
    const outward = tangentPlane.origin - project(line(axes.origin, axes.zAxis), tangentPlane.origin);
    return dot(tangentPlane.normal, outward) < 0 * meter;
}

/**
 * A hole's faces, from one of them: the faces of its cylinder tangent to it (a hole which intersects another one may
 * be split).
 */
function getHoleFaces(context is Context, face is Query) returns array
{
    const cylinder = evSurfaceDefinition(context, { "face" : face });
    return filter(evaluateQuery(context, qTangentConnectedFaces(face)->qGeometry(GeometryType.CYLINDER)), function(other)
        {
            return isSameCylinder(cylinder, evSurfaceDefinition(context, { "face" : other }));
        });
}

/**
 * Whether two cylinders have the same axis and radius. Std's `tolerantEquals` compares their coordinate systems,
 * which needn't be the same.
 */
export function isSameCylinder(cylinder is Cylinder, other is Cylinder) returns boolean
{
    const axes = cylinder.coordSystem;
    return tolerantEquals(cylinder.radius, other.radius) && parallelVectors(axes.zAxis, other.coordSystem.zAxis) &&
        tolerantEqualsZero(norm(cross(other.coordSystem.origin - axes.origin, axes.zAxis)));
}

/**
 * Where a hole starts, and how deep it goes, from its faces (see `setHoleAttributes`). A blind hole's bottom is a
 * face beside them which is within its circle (flat, or a drill point's cone), and the hole starts at the other end;
 * the faces where a hole starts (and a through hole ends) go around it.
 */
function measureHole(context is Context, faces is Query) returns map
{
    const cylinder = evSurfaceDefinition(context, { "face" : qNthElement(faces, 0) });
    const axes = cylinder.coordSystem;
    const sides = evBox3d(context, { "topology" : faces, "cSys" : axes, "tight" : true });
    const start = toWorld(axes, vector(0 * meter, 0 * meter, sides.minCorner[2]));
    var hole = { "faces" : faces, "coordSystem" : coordSystem(start, axes.xAxis, axes.zAxis) };
    for (var face in evaluateQuery(context, qSubtraction(qAdjacent(faces, AdjacencyType.EDGE, EntityType.FACE), faces)))
    {
        const faceBox = evBox3d(context, { "topology" : face, "cSys" : axes, "tight" : true });
        if (isWithinRadius(faceBox, cylinder.radius))
        {
            if (faceBox.minCorner[2] + faceBox.maxCorner[2] < sides.minCorner[2] + sides.maxCorner[2])
            {
                // The bottom is at the start of the cylinder's axis, so the hole starts at its end
                const end = toWorld(axes, vector(0 * meter, 0 * meter, sides.maxCorner[2]));
                hole.coordSystem = coordSystem(end, axes.xAxis, -axes.zAxis);
            }
            hole.depth = sides.maxCorner[2] - sides.minCorner[2];
            const bottom = evSurfaceDefinition(context, { "face" : face });
            if (bottom is Cone)
            {
                hole.tipAngle = 2 * bottom.halfAngle;
            }
            return hole;
        }
    }
    return hole;
}

/**
 * Whether a box, in a cylinder's coordinates, is within its radius of its axis.
 */
export function isWithinRadius(bounds is Box3d, radius is ValueWithUnits) returns boolean
{
    const extent = max([abs(bounds.minCorner[0]), abs(bounds.maxCorner[0]), abs(bounds.minCorner[1]), abs(bounds.maxCorner[1])]);
    return tolerantLessThanOrEqual(extent, radius);
}

/**
 * Delete pocket: deletes each selected pocket (every face tangent to a selected face), extending the faces around it
 * to fill it in.
 */
function deletePockets(context is Context, id is Id, definition is map)
{
    if (isQueryEmpty(context, definition.pockets))
    {
        throw regenError("Select pockets to delete.", ["pockets"]);
    }
    const faces = qTangentConnectedFaces(definition.pockets);
    try
    {
        opDeleteFace(context, id + "delete", { "deleteFaces" : faces, "includeFillet" : false, "capVoid" : false });
    }
    catch
    {
        // A failed delete changes nothing, so the pockets are still there to show
        processSubfeatureStatus(context, id, { "subfeatureId" : id + "delete", "propagateErrorDisplay" : true });
        throw regenError("Failed to delete pocket.", ["pockets"], faces);
    }
}
