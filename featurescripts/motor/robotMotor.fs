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
// FRCDesign's Block Motor (in its FRC library), a configurable Part Studio
BlockMotor::import(path : "5e3874e07384706ec3840340/5657eb187a0b8ed8fb95125a/c0895459c41bc1da9850fd7e", version : "4173ef57af8115dabb532b5d");

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

        locationPredicate(definition, "motor");

        if (isMotor(definition))
        {
            annotation { "Group Name" : "Motor", "Collapsed By Default" : false }
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
            }
        }
        else
        {
            annotation { "Group Name" : "Gearbox", "Collapsed By Default" : false }
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
        }

        annotation { "Group Name" : "Position", "Collapsed By Default" : false }
        {
            axisOrientationPredicate(definition);

            angleReferencePredicate(definition);

            angleOffsetPredicate(definition);
        }

        annotation { "Group Name" : "Holes", "Collapsed By Default" : false }
        {
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

        if (showBlockMotor(definition))
        {
            annotation { "Name" : "Block motor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Bring in a block model of the motor: its body's envelope, its pilot, and its shaft." }
            definition.blockMotor is boolean;
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
        if (buildBlock && !hasBlockModel(face))
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
 * A motor's or gearbox's mounting face, from `motorTables.gen.fs`'s tables, and for motors with block models, how to
 * build one.
 */
export type MotorFace typecheck canBeMotorFace;

/**
 * Holes are as the face's drawing shows them, looking at the face (its shaft toward you).
 *
 * @param value {{
 *      @field partName {string} : The motor's or gearbox's name.
 *      @field screw {string} : The mounting screws' size, as std's hole tables name it (`"#10"`, `"M3"`).
 *      @field boltCircleDiameter {ValueWithUnits} : @requiredif {`holePositions` isn't given.}
 *      @field holeAngles {array} : @requiredif {`holePositions` isn't given.} Each hole's angle on the bolt circle,
 *              counterclockwise from the right.
 *      @field holePositions {array} : @optional Each hole's position (right, up), for holes on several circles.
 *      @field pilotDiameter {ValueWithUnits} : The boss the face's hole must clear.
 *      @field blockMotor {string} : @optional The option of FRCDesign's Block Motor's Motor list which models the motor.
 *      @field blockAngle {ValueWithUnits} : @requiredif {`blockMotor` is given.} How far to turn FRCDesign's Block
 *              Motor (counterclockwise, looking at the face) to line it up with the holes.
 *      @field bodyDiameter {ValueWithUnits} : @optional For motors FRCDesign's Block Motor doesn't have, a block
 *              model of our own: a cylinder this wide,
 *      @field bodyLength {ValueWithUnits} : @requiredif {`bodyDiameter` is given.} this long behind the face,
 *      @field pilotHeight {ValueWithUnits} : @requiredif {`bodyDiameter` is given.} the pilot, this tall,
 *      @field shaftDiameter {ValueWithUnits} : @requiredif {`bodyDiameter` is given.} and a shaft this wide,
 *      @field shaftLength {ValueWithUnits} : @requiredif {`bodyDiameter` is given.} going this far past the face.
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
    if (value.blockMotor != undefined)
    {
        value.blockMotor is string;
        isAngle(value.blockAngle);
    }
}

/**
 * Whether a face's motor has a block model.
 */
export function hasBlockModel(face is MotorFace) returns boolean
{
    return face.blockMotor != undefined || face.bodyDiameter != undefined;
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
 * Where each of a face's holes is, on its plane. The plane's normal points behind the face (toward the motor's body),
 * so looking at the face, its x axis points left.
 */
export function holePositions(face is MotorFace) returns array
{
    const drawn = face.holePositions ?? mapArray(face.holeAngles, function(angle is ValueWithUnits) returns Vector
            {
                return vector(cos(angle), sin(angle)) * face.boltCircleDiameter / 2;
            });
    return mapArray(drawn, position => vector(-position[0], position[1]));
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
    definition.hasBlockModel = isMotor(definition) && hasBlockModel(getMotorFace(definition));
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
 * Builds a block model of a motor on `plane`, its body behind the face (along the plane's normal), and its pilot and
 * shaft in front, as one part named for the motor, with a mate connector on its face: FRCDesign's Block Motor, or for
 * motors it doesn't have, a model of our own.
 */
function buildBlockMotor(context is Context, id is Id, face is MotorFace, plane is Plane)
{
    if (face.blockMotor != undefined)
    {
        deriveBlockMotor(context, id + "derive", face, plane);
    }
    else
    {
        fCylinder(context, id + "body", {
                    "bottomCenter" : plane.origin,
                    "topCenter" : plane.origin + plane.normal * face.bodyLength,
                    "radius" : face.bodyDiameter / 2
                });
        fCylinder(context, id + "pilot", {
                    "bottomCenter" : plane.origin,
                    "topCenter" : plane.origin - plane.normal * face.pilotHeight,
                    "radius" : face.pilotDiameter / 2
                });
        fCylinder(context, id + "shaft", {
                    "bottomCenter" : plane.origin,
                    "topCenter" : plane.origin - plane.normal * face.shaftLength,
                    "radius" : face.shaftDiameter / 2
                });
        opBoolean(context, id + "union", {
                    "tools" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                    "operationType" : BooleanOperationType.UNION
                });
    }

    const motor = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
    setProperty(context, { "entities" : motor, "propertyType" : PropertyType.NAME, "value" : face.partName });
    if (face.blockMotor == undefined)
    {
        setProperty(context, { "entities" : motor, "propertyType" : PropertyType.APPEARANCE, "value" : BLACK });
    }

    opMateConnector(context, id + "mateConnector", {
                "coordSystem" : coordSystem(plane),
                "owner" : motor->qNthElement(0)
            });
}

/** FRCDesign's Block Motor's Motor list, by its FeatureScript id. */
const BLOCK_MOTOR_LIST = "List_SctRc1by7v7Fbg";

/**
 * Brings in FRCDesign's Block Motor, configured as `face.blockMotor`, without its pinion, spacer, or Powerpole board.
 * Its face is on its Top plane, its shaft pointing up (+z), so it's placed with its z axis against `plane`'s normal,
 * and turned `face.blockAngle`.
 */
function deriveBlockMotor(context is Context, id is Id, face is MotorFace, plane is Plane)
{
    // Looking at the face, the plane's x axis points left, and the Block Motor's (from its shaft's side) right
    const placement = coordSystem(plane.origin, -plane.x, -plane.normal);
    const instantiator = newInstantiator(id);
    addInstance(instantiator, BlockMotor::build, {
                "configuration" : {
                    (BLOCK_MOTOR_LIST) : blockMotorOption(face.blockMotor),
                    "Has_Pinion" : false,
                    "Has_spacer" : false,
                    "Has_Powerpole_Board" : false
                },
                "transform" : toWorld(placement) * rotationAround(Z_AXIS, face.blockAngle),
                "name" : "motor"
            });
    try
    {
        instantiate(context, instantiator);
    }
    catch
    {
        throw regenError("Failed to bring in FRCDesign's Block Motor.", ["blockMotor"]);
    }
}

/**
 * An option of FRCDesign's Block Motor's Motor list, by its id.
 */
function blockMotorOption(option is string)
{
    return {
                "Kraken_X60" : BlockMotor::List_SctRc1by7v7Fbg_conf.Kraken_X60,
                "Kraken_X44" : BlockMotor::List_SctRc1by7v7Fbg_conf.Kraken_X44,
                "NEO_Vortex" : BlockMotor::List_SctRc1by7v7Fbg_conf.NEO_Vortex,
                "Copy_of_NEO_Vortex" : BlockMotor::List_SctRc1by7v7Fbg_conf.Copy_of_NEO_Vortex,
                "Copy_of_NEO_V1_1" : BlockMotor::List_SctRc1by7v7Fbg_conf.Copy_of_NEO_V1_1,
                "KrakenX60" : BlockMotor::List_SctRc1by7v7Fbg_conf.KrakenX60,
                "Falcon_500_V3" : BlockMotor::List_SctRc1by7v7Fbg_conf.Falcon_500_V3,
                "NEO_V1_1" : BlockMotor::List_SctRc1by7v7Fbg_conf.NEO_V1_1,
                "NEO_V1_0" : BlockMotor::List_SctRc1by7v7Fbg_conf.NEO_V1_0,
                "NEO_550" : BlockMotor::List_SctRc1by7v7Fbg_conf.NEO_550
            }[option];
}
