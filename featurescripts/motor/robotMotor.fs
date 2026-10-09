FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "01402b7c9eebd8bf0b5d3e52", version : "bb7c494edc43a307e631af8d");
import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
// Exports MateConnectorAxisType, a parameter type
export import(path : "58d66340f7b70cfc86606676", version : "c9963ef4d574eccc05ff889f");
// Exports Fit, a parameter type
export import(path : "926d933eb33b11a3452660fd", version : "734a856ee6a464616f05e7e4");
// Exports Program, a parameter type
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");
export import(path : "motor/motorTables.gen.fs", version : "");

export enum ComponentType
{
    annotation { "Name" : "Motor" }
    MOTOR,
    annotation { "Name" : "Gearbox" }
    GEARBOX
}

predicate isMotor(definition is map)
{
    definition.componentType == ComponentType.MOTOR;
}

predicate showBlockMotor(definition is map)
{
    definition.componentType == ComponentType.MOTOR;
    definition.hasBlockModel;
}

const HOLE_INDEX_BOUNDS = { (unitless) : [1, 1, 1e5] } as IntegerBoundSpec;

/**
 * Cuts a motor's or gearbox's mounting face into parts (its screws' holes, and a hole for its pilot), and builds a block
 * model of a motor: its envelope, pilot, and shaft.
 */
annotation { "Feature Type Name" : "Robot motor",
        "Feature Type Description" : "Cut the mounting holes of a motor or gearbox, and model a block motor." ~ CREDIT,
        "Editing Logic Function" : "robotMotorEditLogic",
        "Manipulator Change Function" : "robotMotorManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotMotor = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Name" : "Component type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.componentType is ComponentType;

        // Whether the chosen motor has a block model, which editing logic sets from its table (conditions can't read
        // lookup tables)
        annotation { "Name" : "Has block model", "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.hasBlockModel is boolean;

        if (isMotor(definition))
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "Motor", "Lookup Table" : frcMotorTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcMotor is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Motor", "Lookup Table" : ftcMotorTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcMotor is LookupTablePath;
            }

            if (showBlockMotor(definition))
            {
                annotation { "Name" : "Block motor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                            "Description" : "Model the motor as a block: its body's envelope, its pilot, and its shaft." }
                definition.blockMotor is boolean;
            }
        }
        else
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "Gearbox", "Lookup Table" : frcGearboxTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcGearbox is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Gearbox", "Lookup Table" : ftcGearboxTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcGearbox is LookupTablePath;
            }
        }

        locationPredicate(definition, "motor");

        axisOrientationPredicate(definition);

        angleReferencePredicate(definition);

        angleOffsetPredicate(definition);

        holeMergeScopePredicate(definition);

        // Of the mounting holes' screws
        fitPredicate(definition);

        // Of the hole for the pilot
        boreFitPredicate(definition);

        annotation { "Name" : "Skip holes" }
        definition.skipHoles is boolean;

        annotation { "Group Name" : "Skip holes", "Driving Parameter" : "skipHoles", "Collapsed By Default" : false }
        {
            if (definition.skipHoles)
            {
                annotation { "Name" : "Holes to skip", "Item name" : "hole", "Item label template" : "#index", "Show labels only" : true,
                            "UIHint" : [UIHint.INITIAL_FOCUS, UIHint.PREVENT_ARRAY_REORDER, UIHint.ALLOW_ARRAY_FOCUS] }
                definition.skippedHoles is array;

                for (var hole in definition.skippedHoles)
                {
                    annotation { "Name" : "Index" }
                    isInteger(hole.index, HOLE_INDEX_BOUNDS);
                }
            }
        }
    }
    {
        const face = getMotorFace(definition);

        var plane = getLocationPlane(context, definition);
        plane = applyAngleReference(context, definition, plane);
        plane = applyAxisOrientation(definition, plane);
        const positions = holePositions(face);
        // Outside the holes, so its handle isn't on one
        addAngleOffsetManipulator(context, id, definition, plane, 1.5 * max(mapArray(positions, norm)));
        plane = applyAngleOffset(definition, plane);

        if (definition.skipHoles)
        {
            addSkippedHolesManipulator(context, id, definition, plane, positions);
            if (any(definition.skippedHoles, hole => hole.index > size(positions)))
            {
                reportFeatureInfo(context, id, "The " ~ face.partName ~ " has " ~ size(positions) ~ " holes, so holes to skip past " ~ size(positions) ~ " are ignored.");
            }
        }

        const buildBlock = showBlockMotor(definition) && definition.blockMotor;
        if (buildBlock && face.bodyDiameter == undefined)
        {
            // Has block model is stale: the motor was changed some way other than in the dialog
            throw regenError("There's no block model of the " ~ face.partName ~ ".", ["blockMotor"]);
        }

        if (isQueryEmpty(context, definition.scope))
        {
            // A block motor alone needs nothing to cut
            if (!buildBlock)
            {
                throw regenError(ErrorStringEnum.HOLE_EMPTY_SCOPE, ["scope"]);
            }
        }
        else
        {
            cutMountingFace(context, id + "cut", definition, face, plane, keptHolePositions(definition, positions));
        }

        if (buildBlock)
        {
            buildBlockMotor(context, id + "block", face, plane);
        }

        cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    });

/**
 * A motor's or gearbox's mounting face, from `motorTables.gen.fs`'s tables, and for motors with block models, the
 * envelope behind it.
 */
export type MotorFace typecheck canBeMotorFace;

/**
 * @param value {{
 *      @field partName {string} : The motor's or gearbox's name, as FRCDesign names it.
 *      @field screw {string} : The mounting screws' size, as std's hole tables name it (`"#10"`, `"M3"`).
 *      @field boltCircleDiameter {ValueWithUnits} : @requiredif {`holePositions` isn't given.}
 *      @field holeAngles {array} : @requiredif {`holePositions` isn't given.} Each hole's angle on the bolt circle,
 *              counterclockwise from the face's x axis.
 *      @field holePositions {array} : @optional Each hole's position on the face, for holes on several circles.
 *      @field pilotDiameter {ValueWithUnits} : The boss the face's hole must clear.
 *      @field pilotHeight {ValueWithUnits} : @optional How far the pilot stands off the face, for block models.
 *      @field bodyDiameter {ValueWithUnits} : @optional The diameter of a motor's body. Motors without one have no
 *              block model.
 *      @field bodyFlats {ValueWithUnits} : @optional The width across flats of a body cut flat on its top and bottom.
 *      @field bumpDistance {ValueWithUnits} : @optional How far the end of a bump in the body (at 270°) is from its
 *              axis. Its sides are tangent to the body.
 *      @field bumpWidth {ValueWithUnits} : @requiredif {`bumpDistance` is given.} How wide the bump's end is.
 *      @field bodyLength {ValueWithUnits} : @requiredif {`bodyDiameter` is given.} How far the body goes behind the face.
 *      @field shaftDiameter {ValueWithUnits} : @optional
 *      @field shaftLength {ValueWithUnits} : @requiredif {`shaftDiameter` is given.} How far the shaft goes in front
 *              of the face.
 * }}
 */
export predicate canBeMotorFace(value)
{
    value is map;
    value.partName is string;
    value.screw is string;
    if (value.holePositions == undefined)
    {
        isLength(value.boltCircleDiameter);
        value.holeAngles is array;
        for (var angle in value.holeAngles)
        {
            isAngle(angle);
        }
    }
    else
    {
        value.holePositions is array;
        for (var position in value.holePositions)
        {
            is2dPoint(position);
        }
    }
    isLength(value.pilotDiameter);
}

function getMotorFace(definition is map) returns MotorFace
{
    if (isMotor(definition))
    {
        if (isFrc(definition))
        {
            return getLookupTable(frcMotorTable, definition.frcMotor) as MotorFace;
        }
        return getLookupTable(ftcMotorTable, definition.ftcMotor) as MotorFace;
    }
    if (isFrc(definition))
    {
        return getLookupTable(frcGearboxTable, definition.frcGearbox) as MotorFace;
    }
    return getLookupTable(ftcGearboxTable, definition.ftcGearbox) as MotorFace;
}

/**
 * Where each of a face's holes is, on its plane.
 */
export function holePositions(face is MotorFace) returns array
{
    if (face.holePositions != undefined)
    {
        return face.holePositions;
    }
    return mapArray(face.holeAngles, function(angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * face.boltCircleDiameter / 2;
        });
}

/**
 * The holes to skip, as indices into the face's holes (Holes to skip counts from 1), leaving out those past the last
 * hole.
 */
export function skippedHoleIndices(definition is map, holeCount is number) returns array
{
    if (!definition.skipHoles)
    {
        return [];
    }
    var indices = [];
    for (var hole in definition.skippedHoles)
    {
        if (hole.index <= holeCount && !isIn(hole.index - 1, indices))
        {
            indices = append(indices, hole.index - 1);
        }
    }
    return indices;
}

/**
 * The positions of the holes which aren't skipped.
 */
export function keptHolePositions(definition is map, positions is array) returns array
{
    const skipped = skippedHoleIndices(definition, size(positions));
    var kept = [];
    for (var i, position in positions)
    {
        if (!isIn(i, skipped))
        {
            kept = append(kept, position);
        }
    }
    return kept;
}

const SKIPPED_HOLES_MANIPULATOR = "skippedHoles";

/**
 * A point on each hole, which toggles whether it's skipped, as std's patterns' Skip instances do.
 */
function addSkippedHolesManipulator(context is Context, id is Id, definition is map, plane is Plane, positions is array)
{
    addManipulators(context, id, {
                (SKIPPED_HOLES_MANIPULATOR) : togglePointsManipulator({
                        "points" : mapArray(positions, position => planeToWorld(plane, position)),
                        "selectedIndices" : skippedHoleIndices(definition, size(positions)),
                        "suppressedIndices" : []
                    })
            });
}

export function robotMotorManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const points = newManipulators[SKIPPED_HOLES_MANIPULATOR];
    if (points != undefined)
    {
        definition.skippedHoles = mapArray(points.selectedIndices, index => { "index" : index + 1 });
    }
    return angleOffsetManipulatorChange(definition, newManipulators);
}

export function robotMotorEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.hasBlockModel = isMotor(definition) && getMotorFace(definition).bodyDiameter != undefined;
    return mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}

/**
 * Cuts the face's holes into the merge scope: its pilot's at the center, and its screws' at `positions`, through the
 * parts behind `plane` (against its normal).
 */
function cutMountingFace(context is Context, id is Id, definition is map, face is MotorFace, plane is Plane, positions is array)
{
    const bounds = evBox3d(context, { "topology" : definition.scope, "cSys" : coordSystem(plane), "tight" : false });
    const depth = -bounds.minCorner[2];
    if (!tolerantGreaterThan(depth, 0 * meter))
    {
        throw regenError("The parts to cut aren't behind the sketch point. Flip the primary axis.", ["oppositeDirection", "scope"], definition.scope);
    }

    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    const pilotDiameter = face.pilotDiameter + boreFitClearance(definition, face.pilotDiameter);
    skCircle(sketch, "pilot", { "center" : vector(0, 0) * meter, "radius" : pilotDiameter / 2 });
    const screwDiameter = fastenerHoleDiameter(definition, face.screw);
    for (var i, position in positions)
    {
        skCircle(sketch, "hole" ~ i, { "center" : position, "radius" : screwDiameter / 2 });
    }
    skSolve(sketch);

    opExtrude(context, id + "tools", {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : -plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });

    const tools = qCreatedBy(id + "tools", EntityType.BODY);
    try
    {
        opBoolean(context, id + "cut", {
                    "tools" : tools,
                    "targets" : definition.scope,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        // A failed boolean changes nothing, so the holes are still there to show
        throw regenError("Failed to cut the mounting holes.", ["scope"], tools);
    }
}

/**
 * Builds a block model of a motor on `plane`: its body behind the face (along the plane's normal), and its pilot and
 * shaft in front, as one part named for the motor, with a mate connector on its face.
 */
function buildBlockMotor(context is Context, id is Id, face is MotorFace, plane is Plane)
{
    sketchBodyProfile(context, id + "sketch", plane, face);

    opExtrude(context, id + "body", {
                "entities" : qSketchRegion(id + "sketch"),
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : face.bodyLength
            });

    if (face.pilotHeight != undefined)
    {
        fCylinder(context, id + "pilot", {
                    "bottomCenter" : plane.origin,
                    "topCenter" : plane.origin - plane.normal * face.pilotHeight,
                    "radius" : face.pilotDiameter / 2
                });
    }

    if (face.shaftDiameter != undefined)
    {
        fCylinder(context, id + "shaft", {
                    "bottomCenter" : plane.origin,
                    "topCenter" : plane.origin - plane.normal * face.shaftLength,
                    "radius" : face.shaftDiameter / 2
                });
    }

    const motor = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
    if (size(evaluateQuery(context, motor)) > 1)
    {
        opBoolean(context, id + "union", {
                    "tools" : motor,
                    "operationType" : BooleanOperationType.UNION
                });
    }

    setProperty(context, { "entities" : motor, "propertyType" : PropertyType.NAME, "value" : face.partName });
    setProperty(context, { "entities" : motor, "propertyType" : PropertyType.APPEARANCE, "value" : BLACK });

    opMateConnector(context, id + "mateConnector", {
                "coordSystem" : coordSystem(plane),
                "owner" : motor
            });
}

/**
 * Sketches a motor body's profile on `plane`, centered on its origin: a circle `bodyDiameter` across, cut flat
 * `bodyFlats` across on its top and bottom (along the plane's y axis), or with a bump at 270° whose sides are tangent
 * to it, ending `bumpDistance` from its center, `bumpWidth` wide.
 */
export function sketchBodyProfile(context is Context, id is Id, plane is Plane, face is MotorFace)
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const radius = face.bodyDiameter / 2;
    if (face.bodyFlats != undefined && tolerantLessThan(face.bodyFlats, face.bodyDiameter))
    {
        const halfFlats = face.bodyFlats / 2;
        const x = sqrt(radius ^ 2 - halfFlats ^ 2);
        skLineSegment(sketch, "top", { "start" : vector(x, halfFlats), "end" : vector(-x, halfFlats) });
        skArc(sketch, "left", { "start" : vector(-x, halfFlats), "mid" : vector(-radius, 0 * meter), "end" : vector(-x, -halfFlats) });
        skLineSegment(sketch, "bottom", { "start" : vector(-x, -halfFlats), "end" : vector(x, -halfFlats) });
        skArc(sketch, "right", { "start" : vector(x, -halfFlats), "mid" : vector(radius, 0 * meter), "end" : vector(x, halfFlats) });
    }
    else if (face.bumpDistance != undefined && tolerantGreaterThan(face.bumpDistance, radius))
    {
        // The bump's end's right corner, and where the line from it is tangent to the body
        const corner = vector(face.bumpWidth / 2, -face.bumpDistance);
        const tangentAngle = atan2(corner[1], corner[0]) + acos(radius / norm(corner));
        const tangent = vector(cos(tangentAngle), sin(tangentAngle)) * radius;
        skArc(sketch, "body", { "start" : tangent, "mid" : vector(0 * meter, radius), "end" : vector(-tangent[0], tangent[1]) });
        skLineSegment(sketch, "bumpLeft", { "start" : vector(-tangent[0], tangent[1]), "end" : vector(-corner[0], corner[1]) });
        skLineSegment(sketch, "bumpEnd", { "start" : vector(-corner[0], corner[1]), "end" : corner });
        skLineSegment(sketch, "bumpRight", { "start" : corner, "end" : tangent });
    }
    else
    {
        skCircle(sketch, "body", { "center" : vector(0, 0) * meter, "radius" : radius });
    }
    skSolve(sketch);
}
