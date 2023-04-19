FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");
import(path : "eb11a2948f8123134339137f", version : "592d1a1db76738a8ed4fc2e7");

/**
 * Use qHasAttribute to collect all points - use attribute creation id as hole attribute
 *
 * Instantiation methods:
 * Frame - qHasAttribute(holeAttributeName), create all tools at tool length (wall thickness)
 * If points to exclude:
 * qSubtract(frameHole)
 *      If exclude entire hole:
 *      qSubtract(qOppositeHole(frameHole))
 *
 * If faces to exclude (a.k.a. half depth):
 * qSubtract(qHasAttribute(holeAttributeName)->qIntersectsPlane(evPlane())
 *
 * Point - each hole has its info
 * Collect identical holes using frameHoleAttribute
 * If full depth:
 * qOppositeHole(frameHole)
 * If half depth:
 * Group by radius and depth (or just group by frameAttributeName)
 *
 * We need a way to get opposites
 * Could do qIntersectsLine with the hole axis - easiest probably
 */

/**
 * An enum defining the possible robot competition types.
 */
export enum Competition
{
    annotation { "Name" : "FRC" }
    FRC,
    // annotation { "Name" : "FTC" }
    // FTC,
    annotation { "Name" : "VEX" }
    VEX
}

export enum FrameCreationStyle
{
    annotation { "Name" : "Create" }
    CREATE,
    annotation { "Name" : "Convert" }
    CONVERT
}

// /**
//  * @return {{
//  *      @field frameOperation {FrameOperation} :
//  *              A `FrameOperation` to use.
//  * }}
//  */
// predicate frameOperationPredicate(definition is map)
// {
//     annotation { "Name" : "Frame operation", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
//     definition.frameOperation is FrameOperation;
// }

/**
 * Converts frames selected using the `frameConvertPredicate` into solid parts using the `convertFunction`.
 * Throws if no frames are selected.
 */
export function convertFrame(context is Context, id is Id, definition is map, convertFunction is function)
{
    verifyNonemptyQuery(context, definition, "framesToConvert", "Select frame surfaces created by a frame FeatureScript to convert.");
    forEachEntity(context, id + "convert", definition.framesToConvert, function(frame is Query, id is Id)
        {
            convertFunction(context, id, frame);
        });
}

export enum FrameFace
{
    FRONT,
    RIGHT
}

/**
 * A type defining information about frames.
 * Used to assist with defining holes in the frame and adding point manipulators.
 *
 * @type {{
 *      @field frameFaceMap {map} :
 *              A map relating `FrameFace`s to `FrameFaceInfo`.
 *      @field framePatterns {array} :
 *              An array of `FramePattern`s to add.
 * }}
 */
export type FrameInfo typecheck canBeFrameInfo;

export predicate canBeFrameInfo(value)
{
    value.frameFaceMap is map;
    value.frameFaceMap[FrameFace.FRONT] is FrameFaceInfo;
    value.frameFaceMap[FrameFace.RIGHT] is FrameFaceInfo;
}

/**
 * A constructor for `FrameInfo`.
 * @param definition {{
 *      @field frontWidth {ValueWithUnits} :
 *      @field frontHasOpposite {boolean} : @optional
 *              Defaults to `true`.
 *      @field rightWidth {ValueWithUnits} :
 *      @field rightHasOpposite {ValueWithUnits} : @optional
 *              Defaults to `true`.
 *      @field length {ValueWithUnits} :
 *              The length of the frame.
 * }}
 */
export function frameInfo(definition is map) returns FrameInfo
precondition
{
    definition.frontWidth is ValueWithUnits;
    definition.frontHasOpposite is undefined || definition.frontHasOpposite is boolean;
    definition.rightWidth is ValueWithUnits;
    definition.rightHasOpposite is undefined || definition.rightHasOpposite is boolean;
    definition.length is ValueWithUnits;
}

{
    definition = mergeMaps({ "frontHasOpposite" : true, "rightHasOpposite" : true }, definition);

    const frontPlane = plane(vector(definition.frontWidth / 2, -definition.rightWidth / 2, -definition.length / 2), -Y_DIRECTION, Z_DIRECTION);
    const rightPlane = plane(vector(definition.frontWidth / 2, definition.rightWidth / 2, -definition.length / 2), X_DIRECTION, Z_DIRECTION);

    return {
                "frameFaceMap" : {
                    FrameFace.FRONT : { "plane" : frontPlane, "width" : definition.frontWidth, "hasOpposite" : definition.frontHasOpposite } as FrameFaceInfo,
                    FrameFace.RIGHT : { "plane" : rightPlane, "width" : definition.rightWidth, "hasOpposite" : definition.rightHasOpposite } as FrameFaceInfo
                },
            } as FrameInfo;
}

/**
 * @type {{
 *      @field width {ValueWithUnits} :
 *              The width of the face.
 *      @field plane {Plane} :
 *              A plane specifying the position of the frame face.
 *              The plane normal is away from the face.
 *              Its origin is in the bottom left corner of the face.
 *              Its x is along the length of the frame. Its y is along the face.
 *      @field hasOpposite {boolean} :
 *              Whether the opposite face of the frame exists.
 *              False for c-channel and angle.
 * }}
 */
export type FrameFaceInfo typecheck canBeFrameFaceInfo;

export predicate canBeFrameFaceInfo(value)
{
    value.width is ValueWithUnits;
    value.plane is Plane;
    value.hasOpposite is boolean;
}

/**
 * Defines the position of the hole pattern on the frame.
 */
export enum PatternPosition
{
    annotation { "Name" : "Centered" }
    CENTERED,
    annotation { "Name" : "End" }
    END
}

/**
 * A type defining a single pattern along a frame.
 * @type {{
 *      @field profileDefinition :
 *              Either a `SketchDataArray` representing the frame profile, or a `ValueWithUnits`
 *              representing the diameter of the hole to create.
 *      @field frameFace {FrameFace} :
 *              The frame face the pattern lies on.
 *      @field locations {array} :
 *              An array of 2D `Vector`s to use.
 * }}
 */
export type FramePattern typecheck canBeFramePattern;

export predicate canBeFramePattern(value)
{
    value.frameFace is FrameFace;
    value.frameHoleAttribute is FrameHoleAttribute;

    value.locations is array;
    for (var location in value.locations)
    {
        is2dPoint(location);
    }
}

/**
 * A constructor for `FramePattern`.
 * @param definition {{
 *      @field frameFace {FrameFace} :
 *              The frame face to use.
 *      @field frameHoleAttribute {FrameHoleAttribute} :
 *              The frame hole attribute defining the hole.
 *      @field locations {array} :
 *              An array of 2D `Vector`s defining points to add.
 * }}
 */
export function framePattern(definition is map) returns FramePattern
precondition
{
    definition.frameFace is FrameFace;
    definition.frameHoleAttribute is FrameHoleAttribute;
    is2dPointVector(definition.locations);
}
{
    return definition as FramePattern;
}

/**
 * Adds frame patterns to a frame.
 * All FramePatterns are added and marked with sketch points.
 * FramePatterns which are marked `allowPartial` are also instantiated immediately.
 *
 * @param id {Id} : @autocomplete `id + "framePattern"`
 * @param definition {{
 *      @field frameInfo {FrameInfo} :
 *              `FrameInfo` to use when adding points.
 *      @field framePatterns {array} :
 *              An array of `FramePattern`s to add.
 *      @field cutFrame {boolean} :
 *              Whether to cut the frame with holes that require instantiation.
 * }}
 */
export function addFramePatterns(context is Context, id is Id, definition is map)
precondition
{
    definition.frameInfo is FrameInfo;
    definition.framePatterns is array;
    for (var framePattern in definition.framePatterns)
    {
        framePattern is FramePattern;
    }
}
{
    const cutterId = id + "cutter";
    fCylinder(context, cutterId, {
                "topCenter" : zeroVector(3) * meter,
                "bottomCenter" : vector(0, 0, -1 / 16) * inch,
                "radius" : 0.177 / 2 * inch
            });

    var transforms = [];
    var names = [];
    for (var i, pattern in definition.framePatterns)
    {
        const pointId = id + ("point" ~ i);
        const frameFaceInfo = definition.frameInfo.frameFaceMap[pattern.frameFace];
        const plane = frameFaceInfo.plane;
        var oppositePlane;
        if (frameFaceInfo.hasOpposite)
        {
            oppositePlane = frameOppositePlane(definition.frameInfo, pattern.frameFace);
        }

        for (var j, location in pattern.locations)
        {
            transforms = append(transforms, coordSystem(planeToWorld(plane, location), plane.x, plane.normal)->toWorld());
            names = append(names, "hole" ~ i ~ "_" ~ j);

            if (oppositePlane != undefined)
            {
                location[0] *= -1;
                transforms = append(transforms, coordSystem(planeToWorld(oppositePlane, location), oppositePlane.x, oppositePlane.normal)->toWorld());
                names = append(names, "oppositeHole" ~ i ~ "_" ~ j);
            }
        }

        // setFrameHoleAttribute(context, qCreatedBy(sketchId, EntityType.VERTEX), pattern.frameHoleAttribute);
    }

    opPattern(context, id + "pattern", {
                "entities" : qCreatedBy(cutterId, EntityType.BODY),
                "transforms" : transforms,
                "instanceNames" : names
            });

    opDeleteBodies(context, id + "deleteBodies", {
                "entities" : qCreatedBy(cutterId, EntityType.BODY)
            });
}

/**
 * Retrives the opposite plane of a frame.
 * The z-axis still faces outwards, but the frame lies along the -x direction (rather than the x direction).
 */
function frameOppositePlane(frameInfo is FrameInfo, frameFace is FrameFace) returns Plane
{
    const frameFaceInfo = frameInfo.frameFaceMap[frameFace];

    var newPlane = frameFaceInfo.plane;
    newPlane.origin += frameFaceInfo.plane.normal * -otherFrameFaceWidth(frameInfo, frameFace);
    newPlane.normal *= -1;
    newPlane.x *= -1;
    return newPlane;
}

/**
 * The width of the other frame face.
 */
function otherFrameFaceWidth(frameInfo is FrameInfo, frameFace is FrameFace) returns ValueWithUnits
{
    return frameInfo.frameFaceMap[(frameFace == FrameFace.FRONT ? FrameFace.RIGHT : FrameFace.FRONT)].width;
}

// export function frameLength(frameInfo is FrameInfo) returns ValueWithUnits
// {
//     return -2 * frameInfo.frameFaceMap[FrameFace.FRONT].plane.origin[2];
// }

// /**
//  * Throws an error if `frame` or `frames` are empty or resolve to a `Query` which is not a valid frame.
//  * @seealso [singleFramePredicate]
//  * @seealso [multipleFramePredicate]
//  */
// export function verifyFrameSelections(context is Context, definition is map) returns array
// precondition
// {
//     definition.frame is Query || definition.frames is Query;
// }
// {
//     if (definition.frame != undefined)
//     {
//         const result = verifyNonemptyQuery(context, definition, "frame", "Select a frame to use.");
//         if (isQueryEmpty(context, qHasAttribute(definition.frame, TUBE_ATTRIBUTE)))
//         {
//             throw regenError("The selection is not a valid frame created by a frame feature.", ["frame"], definition.frame);
//         }
//         return result;
//     }
//     else if (definition.frames != undefined)
//     {
//         // since if statement won't trip if definition.frames is empty, it works
//         if (!isQueryEmpty(context, definition.frames->qSubtraction(qHasAttribute(definition.frames, TUBE_ATTRIBUTE))))
//         {
//             throw regenError("One or more selections are not valid frames created by frame features.", ["frames"],
//                 definition.frames->qSubtraction(qHasAttribute(definition.frames, TUBE_ATTRIBUTE)));
//         }
//         return verifyNonemptyQuery(context, definition, "frames", "Select one or more frames to use.");
//     }
// }

export const TUBE_ATTRIBUTE = "frameAttribute";

/**
 * The attribute for a `frame`.
 * @type {{
 *      @field name {String} :
 *              The name associated with this frame. Not to be confused with the
 *              name of the corresponding part.
 *      @field holeName {String} :
 *              The name of the attribute attatched to the frame's holes.
 *      @field bodyType {BodyType} :
 *              The `BodyType` of the frame.
 *      @field length {ValueWithUnits} :
 *              The length of the frame.
 *      @field hasHole {boolean} :
 *              Whether the frame has holes.
 *      @field holeRadius {ValueWithUnits} : @requiredif { `hasHole` == `true` }
 *              The radius of holes on the frame.
 *      @field holeDepth {ValueWithUnits} : @requiredif { `hasHole` == `true` }
 *              The depth of holes on the frame.
 *              Typically equal to the frame's wall thickness.
 * }}
 */
export type FrameAttribute typecheck canBeFrameAttribute;

export predicate canBeFrameAttribute(value)
{
    value is map;
    value.name is string;
    value.bodyType is ToolBodyType;
    value.length is ValueWithUnits;

    value.hasHoles is boolean;
    if (value.hasHoles)
    {
        value.holeName is string;
        isLength(value.holeRadius);
        isLength(value.holeDepth);
    }
}

/**
 * A constructor for a `FrameAttribute` with holes.
 */
export function frameAttribute(id is Id, bodyType is ToolBodyType, length is ValueWithUnits, holeRadius is ValueWithUnits, holeDepth is ValueWithUnits)
{
    return {
                "name" : TUBE_ATTRIBUTE ~ toString(id),
                "bodyType" : bodyType,
                "length" : length,
                "hasHoles" : true,
                "holeName" : TUBE_HOLE_ATTRIBUTE ~ toString(id),
                "holeRadius" : holeRadius,
                "holeDepth" : holeDepth
            } as FrameAttribute;
}

/**
 * A constructor for a `FrameAttribute` with no holes.
 */
export function frameAttribute(id is Id, bodyType is ToolBodyType, length is ValueWithUnits)
{
    return {
                "name" : TUBE_ATTRIBUTE ~ toString(id),
                "bodyType" : bodyType,
                "length" : length,
                "hasHoles" : false
            } as FrameAttribute;
}

export function setFrameAttribute(context is Context, frame is Query, frameAttribute is FrameAttribute)
{
    setAttribute(context, {
                "entities" : frame,
                "name" : TUBE_ATTRIBUTE,
                "attribute" : frameAttribute
            });
}

export function getFrameAttribute(context is Context, frame is Query)
{
    return getAttribute(context, {
                "entity" : frame,
                "name" : TUBE_ATTRIBUTE
            });
}

export const TUBE_HOLE_ATTRIBUTE = "frameHoleAttribute";

/**
 * The attribute attatched to a frame hole construction plane. Used to enable down stream hole instantiation and frame plate functionality.
 * @type {{
 *      @field name {String} :
 *              The name of the hole.
 *      @field frameName {String} :
 *              The name of the frame attribute.
 *      @field holeRadius {ValueWithUnits} :
 *              The radius of the hole.
 *      @field requireInstantiation {boolean} :
 *              Whether the hole should always be instantiated.
 *              Used to mark COTs profiles.
 *      @field instantiated {boolean} :
 *              Whether the frame hole has been instantiated.
 *              Should always be false for holes associated with surfaces.
 *      @field depth {ValueWithUnits} : @requiredif { `instantiated` == `false` }
 *              The depth of the hole.
 *      @field sketchDataArray {SketchDatatArray} : @optional
 *              An optional `SketchDataArray` representing a more complex profile.
 * }}
 */
export type FrameHoleAttribute typecheck canBeFrameHoleAttribute;

export predicate canBeFrameHoleAttribute(value)
{
    value is map;
    value.name is string;
    value.frameName is string;

    isLength(value.holeRadius);
    value.instantiated is boolean;
    if (!value.instantiated)
    {
        isLength(value.holeDepth);
    }

    value.sketchDataArray is undefined || value.sketchDataArray is SketchDataArray;
}

/**
 * Constructs a `FrameHoleAttribute` from a `FrameAttribute`.
 */
export function frameHoleAttribute(frameAttribute is FrameAttribute) returns FrameHoleAttribute
{
    return { "name" : frameAttribute.holeName, "frameName" : frameAttribute.name, "holeRadius" : frameAttribute.holeRadius, "holeDepth" : frameAttribute.holeDepth } as FrameHoleAttribute;
}

function setFrameHoleAttribute(context is Context, entities is Query, frameHoleAttribute is FrameHoleAttribute)
{
    setAttribute(context, {
                "entities" : entities,
                "name" : TUBE_HOLE_ATTRIBUTE,
                "attribute" : frameHoleAttribute
            });
}

export function qFrame(frameAttribute is FrameAttribute) returns Query
{
    return qHasAttributeWithValueMatching(TUBE_ATTRIBUTE, { "name" : frameAttribute.name });
}

export function qFrame(frameHoleAttribute is FrameHoleAttribute) returns Query
{
    return qHasAttributeWithValueMatching(qAllModifiableSolidBodiesNoMesh(), TUBE_ATTRIBUTE, { "name" : frameHoleAttribute.frameName });
}

export function qFrameHole(frameAttribute is FrameAttribute) returns Query
{
    return qHasAttributeWithValueMatching(
        qEverything(EntityType.VERTEX)->qSketchFilter(SketchObject.YES), TUBE_HOLE_ATTRIBUTE, { "name" : frameAttribute.holeName }
        );
}

export function qOppositeHole(context is Context, frameHole is Query) returns Query
{
    const direction = evOwnerSketchPlane(context, { "entity" : frameHole }).normal;
    const origin = evVertexPoint(context, { "vertex" : frameHole });
    return qIntersectsLine(qHasAttribute(qEverything(EntityType.VERTEX)->qSubtraction(frameHole), TUBE_HOLE_ATTRIBUTE), line(origin, direction));
}

/**
 * The attribute for a frame point which has not been marked as a hole.
 */
export const TUBE_POINT_ATTRIBUTE = "framePoint";


