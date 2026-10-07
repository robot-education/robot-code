FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "core/stdExtrude.fs", version : "");
export import(path : "core/location.fs", version : "");
import(path : "core/pointManipulator.fs", version : "");
import(path : "core/robotFeature.fs", version : "");

/**
 * The shapes Robot primitive makes.
 */
export enum PrimitiveShape
{
    annotation { "Name" : "Cylinder" }
    CYLINDER,
    annotation { "Name" : "Rectangle" }
    RECTANGLE,
    annotation { "Name" : "Square" }
    SQUARE
}

const PRIMITIVE_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;
const PRIMITIVE_WIDTH_BOUNDS = { (meter) : [1e-5, 0.0508, 500], (inch) : 2, (millimeter) : 50 } as LengthBoundSpec;
const PRIMITIVE_HEIGHT_BOUNDS = { (meter) : [1e-5, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;

predicate isCylinder(definition is map)
{
    definition.shape == PrimitiveShape.CYLINDER;
}

predicate isRectangle(definition is map)
{
    definition.shape == PrimitiveShape.RECTANGLE;
}

/**
 * Extrudes a cylinder, rectangle, or square from a sketch point or mate connector, with the usual extrude options, as
 * a new part or added to, removed from, or intersected with others. A rectangle or square is centered on the point,
 * or has the corner or edge chosen with its point manipulator there, with its width along the point's X turned by its
 * angle (from the angle reference's direction, if there is one), which an angle manipulator also sets, as in std's
 * Frame.
 */
annotation { "Feature Type Name" : "Robot primitive",
        "Feature Type Description" : "Quickly extrude cylinders, rectangles, and squares from a point, and add them to or remove them from parts." ~ CREDIT,
        "Manipulator Change Function" : "robotPrimitiveManipulatorChange",
        "Editing Logic Function" : "robotPrimitiveEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotPrimitive = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        booleanStepTypePredicate(definition);

        annotation { "Name" : "Shape", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.shape is PrimitiveShape;

        if (isCylinder(definition))
        {
            annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.diameter, PRIMITIVE_DIAMETER_BOUNDS);
        }
        else if (isRectangle(definition))
        {
            annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.width, PRIMITIVE_WIDTH_BOUNDS);

            annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.height, PRIMITIVE_HEIGHT_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.size, PRIMITIVE_HEIGHT_BOUNDS);
        }

        locationPredicate(definition, "primitive");

        // Which point of a rectangle or square is on the location (see `ninePointOffsets`)
        ninePointManipulatorPredicate(definition);

        extrudeEndPredicate(definition);

        extrudeOffsetPredicate(definition);

        if (!isCylinder(definition))
        {
            annotation { "Group Name" : "Angle", "Collapsed By Default" : false }
            {
                // As std's Frame: the angle from the location's X axis, or from the angle reference's direction
                annotation { "Name" : "Angle" }
                isAngle(definition.angle, ANGLE_360_ZERO_DEFAULT_BOUNDS);

                annotation { "Name" : "Angle reference", "Filter" : QueryFilterCompound.ALLOWS_DIRECTION || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 1 }
                definition.angleReference is Query;
            }
        }

        extrudeOptionsPredicate(definition);

        booleanStepScopePredicate(definition);
    }
    {
        definition.entities = sketchPrimitive(context, id + "profile", definition);
        // The top level id, so the extrude's manipulators are the feature's; extrude does the boolean
        callSubfeatureAndProcessStatus(id, extrude, context, id, definition, { "featureParameterMap" : { "entities" : "location" } });
        if (!isCylinder(definition))
        {
            addPrimitiveManipulators(context, id, definition);
        }
        // After the boolean, which uses the profile
        opDeleteBodies(context, id + "deleteProfile", { "entities" : qCreatedBy(id + "profile", EntityType.BODY) });
    });

/**
 * The width and height of a rectangle or square primitive.
 */
function primitiveSize(definition is map) returns map
{
    if (isRectangle(definition))
    {
        return { "width" : definition.width, "height" : definition.height };
    }
    return { "width" : definition.size, "height" : definition.size };
}

/**
 * Where the center of a rectangle or square primitive is, relative to the location, so its chosen point is on it.
 */
function primitiveCenter(definition is map) returns Vector
{
    const size = primitiveSize(definition);
    return -ninePointOffsets(size.width, size.height)[getNinePointIndex(definition)];
}

/**
 * The plane a primitive's profile is sketched on: the location's, with its X along the angle reference's direction (if
 * there is one), turned by the angle (for a rectangle or square). Also returns the X it's turned from, `zeroX`.
 */
function primitivePlane(context is Context, definition is map) returns map
{
    const location = getLocationPlane(context, definition);
    if (isCylinder(definition))
    {
        return { "plane" : location, "zeroX" : location.x };
    }
    var zeroX = location.x;
    if (!isQueryEmpty(context, definition.angleReference))
    {
        const reference = extractDirection(context, definition.angleReference);
        const across = reference == undefined ? undefined : reference - location.normal * dot(reference, location.normal);
        if (across == undefined || tolerantEquals(norm(across), 0))
        {
            throw regenError("The angle reference must have a direction which isn't along the primitive's.", ["angleReference"],
                definition.angleReference);
        }
        zeroX = normalize(across);
    }
    const x = zeroX * cos(definition.angle) + cross(location.normal, zeroX) * sin(definition.angle);
    return { "plane" : plane(location.origin, location.normal, x), "zeroX" : zeroX };
}

/**
 * Sketches the primitive's profile, and returns its face.
 */
function sketchPrimitive(context is Context, id is Id, definition is map) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : primitivePlane(context, definition).plane });
    if (isCylinder(definition))
    {
        skCircle(sketch, "circle", { "center" : vector(0, 0) * meter, "radius" : definition.diameter / 2 });
    }
    else
    {
        const size = primitiveSize(definition);
        const center = primitiveCenter(definition);
        const corner = vector(size.width, size.height) / 2;
        skRectangle(sketch, "rectangle", {
                    "firstCorner" : vector(center[0], center[1]) - corner,
                    "secondCorner" : vector(center[0], center[1]) + corner
                });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

const ANGLE_MANIPULATOR = "angleManipulator";

/**
 * Adds a rectangle or square's manipulators: the nine point manipulator choosing which of its points is on the
 * location, and the angle manipulator, as std's Frame's: around the location's axis, from the zero angle's direction,
 * as far out as the profile is wide.
 */
function addPrimitiveManipulators(context is Context, id is Id, definition is map)
{
    const profile = primitivePlane(context, definition);
    const location = coordSystem(profile.plane);
    const size = primitiveSize(definition);
    const center = primitiveCenter(definition);
    addPointManipulator(context, id, definition, mapArray(ninePointOffsets(size.width, size.height), function(offset)
            {
                return toWorld(location, center + offset);
            }), getNinePointIndex(definition));
    addManipulators(context, id, {
                (ANGLE_MANIPULATOR) : angularManipulator({
                        "primaryParameterId" : "angle",
                        "axisOrigin" : profile.plane.origin,
                        "axisDirection" : profile.plane.normal,
                        "rotationOrigin" : profile.plane.origin + profile.zeroX * max(size.width, size.height),
                        "angle" : definition.angle,
                        "minValue" : 0 * degree,
                        "maxValue" : 360 * degree
                    })
            });
}

export function robotPrimitiveManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = pointManipulatorChange(definition, newManipulators);
    const angle = newManipulators[ANGLE_MANIPULATOR];
    if (angle != undefined && isAngle(angle.angle))
    {
        definition.angle = angle.angle;
    }
    return extrudeManipulatorChange(context, definition, newManipulators);
}

export function robotPrimitiveEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map,
    hiddenBodies is Query) returns map
{
    // The profile, so the merge scope is filled in from what it touches
    if (!isQueryEmpty(context, definition.location))
    {
        definition.entities = try silent(sketchPrimitive(context, id + "profile", definition));
    }
    return stdExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
