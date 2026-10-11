FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "ea127c07807644fb48d3a1ae", version : "3c1ddfaf5ff0b3d5897422d0");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
export import(path : "b82468283e5ec09720bad185", version : "f035c6827196f0268491d7a0");
import(path : "0195d390c3944cd4fab21ce0", version : "9cc37d84cbe31e0b3f0436b3");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
import(path : "0103ad63394d7713fbf44448", version : "5f7d67b62160df309da69ec3");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "01f0c5634015659514b83da1", version : "d70b443054b6c1445f39456d");
import(path : "f9a7532fe0d68211bd3a8112", version : "320a7476c75600c751d4c643");
import(path : "core/mounting.fs", version : "");

export enum GearType
{
    annotation { "Name" : "Create gear" }
    GEAR,
    annotation { "Name" : "Cut rack" }
    RACK
}

/**
 * Where a cut rack's face is on its teeth.
 */
export enum RackFaceAt
{
    annotation { "Name" : "Pitch diameter" }
    PITCH,
    annotation { "Name" : "Outside diameter" }
    OUTSIDE
}

/**
 * What a cut rack's teeth line up with.
 */
export enum RackAlignment
{
    annotation { "Name" : "End" }
    END,
    annotation { "Name" : "Angle reference" }
    REFERENCE
}

const DIAMETRAL_PITCH_BOUNDS = { (unitless) : [1, 20, 200] } as RealBoundSpec;
const MODULE_BOUNDS = { (meter) : [1e-5, 0.001, 0.1], (millimeter) : 1, (inch) : 0.04 } as LengthBoundSpec;
const GEAR_TEETH_BOUNDS = { (unitless) : [4, 24, 1000] } as IntegerBoundSpec;
const SECTOR_TEETH_BOUNDS = { (unitless) : [1, 6, 1000] } as IntegerBoundSpec;
const PRESSURE_ANGLE_BOUNDS = { (degree) : [10, 20, 35] } as AngleBoundSpec;
const THICKNESS_BOUNDS = { (meter) : [1e-5, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;
// A 1/8 in. router bit's
const BIT_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;

const RACK_END_MANIPULATOR = "rackEnd";

predicate isGear(definition is map)
{
    definition.gearType == GearType.GEAR;
}

/**
 * Makes involute spur gears (or sectors of them), with optional root rounds a router bit can cut, or cuts rack teeth
 * into a part's face; sized by diametral pitch (in inches) or module (metric).
 */
annotation {
        "Feature Type Name" : "Robot gear",
        "Feature Type Description" : "Create involute spur gears and sector gears, or cut racks into parts, by diametral pitch or module." ~ CREDIT,
        "Manipulator Change Function" : "robotGearManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotGear = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Gear type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.gearType is GearType;

        if (isGear(definition))
        {
            locationPredicate(definition, "gear");
        }
        else
        {
            annotation { "Name" : "Face to cut", "Filter" : EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES,
                        "MaxNumberOfPicks" : 1, "Description" : "The teeth run along its longest edge, cut through its width." }
            definition.rackFace is Query;

            annotation { "Name" : "Tangent propagation", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Also cut along the flat faces in line with it, across tangent edges." }
            definition.tangentPropagation is boolean;

            annotation { "Name" : "Face at", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "The teeth's pitch line (their tips are cut off at the face), or their tips." }
            definition.rackFaceAt is RackFaceAt;

            annotation { "Name" : "Align teeth to" }
            definition.rackAlignment is RackAlignment;

            if (definition.rackAlignment == RackAlignment.END)
            {
                annotation { "Name" : "Opposite end", "UIHint" : UIHint.OPPOSITE_DIRECTION }
                definition.oppositeEnd is boolean;
            }
            else
            {
                annotation { "Name" : "Angle reference", "Filter" : QueryFilterCompound.ALLOWS_VERTEX || GeometryType.CIRCLE || GeometryType.ARC,
                            "MaxNumberOfPicks" : 1, "Description" : "A point: a gap is centered on it, as on an end." }
                definition.angleReference is Query;
            }
        }

        annotation { "Group Name" : "Teeth", "Collapsed By Default" : false }
        {
            if (isImperial(definition))
            {
                annotation { "Name" : "Diametral pitch", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Teeth per inch of pitch diameter: 20 for most robot gears." }
                isReal(definition.diametralPitch, DIAMETRAL_PITCH_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Module", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Pitch diameter per tooth." }
                isLength(definition.module, MODULE_BOUNDS);
            }

            if (isGear(definition))
            {
                annotation { "Name" : "Teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.teeth, GEAR_TEETH_BOUNDS);
            }

            annotation { "Name" : "Pressure angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "20 degrees for most gears; some older ones are 14.5." }
            isAngle(definition.pressureAngle, PRESSURE_ANGLE_BOUNDS);

            if (isGear(definition))
            {
                annotation { "Name" : "Thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.thickness, THICKNESS_BOUNDS);
            }
        }

        if (isGear(definition))
        {
            annotation { "Name" : "Sector", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Only some of its teeth, in a wedge from its center." }
            definition.sector is boolean;

            if (definition.sector)
            {
                annotation { "Group Name" : "Sector", "Collapsed By Default" : false, "Driving Parameter" : "sector" }
                {
                    annotation { "Name" : "Sector teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(definition.sectorTeeth, SECTOR_TEETH_BOUNDS);
                }
            }
        }

        // Racks' roots are square: there's no round to relieve
        if (isGear(definition))
        {
            annotation { "Name" : "Router relief", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Round the roots to a router bit's radius, so it can cut them (a full round, deeper than the root, where the gap's too narrow)." }
            definition.routerRelief is boolean;

            if (definition.routerRelief)
            {
                annotation { "Group Name" : "Router relief", "Collapsed By Default" : false, "Driving Parameter" : "routerRelief" }
                {
                    annotation { "Name" : "Bit diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.bitDiameter, BIT_DIAMETER_BOUNDS);
                }
            }
        }

        if (isGear(definition))
        {
            startOffsetPredicate(definition);

            borePredicate(definition);
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            profileOffsetPredicate(definition);

            if (isGear(definition))
            {
                annotation { "Name" : "Add mate connector", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.addMateConnectors is boolean;
            }
        }
    }
    {
        const module = gearModule(definition);
        if (!isGear(definition))
        {
            cutRack(context, id, definition, getGearForm(module, undefined, definition.pressureAngle, standardFilletRadius(module), getProfileOffset(definition)));
            return;
        }

        const gearPlane = applyStartOffset(context, definition, getLocationPlane(context, definition));
        addStartOffsetManipulator(context, id, definition, gearPlane);

        const fillet = definition.routerRelief ? definition.bitDiameter / 2 : standardFilletRadius(module);
        const form = getGearForm(module, definition.teeth, definition.pressureAngle, fillet, getProfileOffset(definition));
        if (definition.sector && definition.sectorTeeth >= definition.teeth)
        {
            throw regenError("A sector has fewer teeth than its gear.", ["sectorTeeth"]);
        }
        const profile = sketchGearProfile(context, id + "profile", gearPlane, form, definition.sector ? definition.sectorTeeth : undefined);
        opExtrude(context, id + "extrude", {
                    "entities" : profile,
                    "direction" : gearPlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.thickness / 2,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : definition.thickness / 2
                });
        cleanup(context, id + "deleteSketch", qCreatedBy(id + "profile", EntityType.BODY));
        const gear = qCreatedBy(id + "extrude", EntityType.BODY);
        setGearProperties(context, gear, definition);

        if (offsetProfile(definition))
        {
            const faces = qOwnedByBody(gear, EntityType.FACE);
            addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(gearPlane.origin, gearPlane.x), qSubtraction(faces, qParallelPlanes(faces, gearPlane)),
                definition.profileOffsetOppositeDirection);
        }

        if (definition.addMateConnectors)
        {
            opMateConnector(context, id + "mateConnector", {
                        "coordSystem" : coordSystem(gearPlane),
                        "owner" : gear
                    });
        }

        const bore = definitionBore(definition);
        if (bore != undefined)
        {
            cutBores(context, id + "bore", bore, [gearPlane], [undefined], [gear]);
        }
        // Its pitch diameter, for spacing gears: two mesh their pitch radii apart
        reportFeatureInfo(context, id, "Its pitch diameter is " ~ makeValueString(definition.unitSystem, 2 * form.pitchRadius, true) ~ ".");
    });

/**
 * Cuts rack teeth into the part along Face to cut (and with Tangent propagation, the flat faces in line with it, across
 * tangent edges): along its longest straight edge, from end to end, through its width. Its face is at the teeth's
 * pitch line or tips (`rackFaceAt`). Whole teeth fill it, a pitch apart, lined up with an end (a gap centered on it) or
 * the angle reference (likewise); past the first and last, it's cut to the root.
 */
function cutRack(context is Context, id is Id, definition is map, form is map)
{
    const face = verifyNonemptyQuery(context, definition, "rackFace", "Select a face to cut the rack into.")[0];
    const facePlane = evPlane(context, { "face" : face });
    const faces = definition.tangentPropagation ? qCoincidesWithPlane(qTangentConnectedFaces(face), facePlane) : face;
    const direction = longestEdgeDirection(context, faces);
    if (direction == undefined)
    {
        throw regenError("The face needs a straight edge for the teeth to run along.", ["rackFace"], face);
    }
    // x along the teeth, y out of the part, and z across the face
    const frame = coordSystem(facePlane.origin, direction, cross(direction, facePlane.normal));
    const bounds = evBox3d(context, { "topology" : faces, "cSys" : frame, "tight" : true });
    const length = bounds.maxCorner[0] - bounds.minCorner[0];
    const width = bounds.maxCorner[2] - bounds.minCorner[2];
    // On the face, at the start of its length and one side of its width
    const sketchPlane = plane(toWorld(frame, vector(bounds.minCorner[0], 0 * meter, bounds.minCorner[2])), frame.zAxis, direction);

    var gap;
    if (definition.rackAlignment == RackAlignment.END)
    {
        gap = definition.oppositeEnd ? length : 0 * meter;
        addManipulators(context, id, {
                    (RACK_END_MANIPULATOR) : flipManipulator({
                            "base" : sketchPlane.origin + direction * gap + frame.zAxis * width / 2,
                            "direction" : direction,
                            "flipped" : definition.oppositeEnd
                        })
                });
    }
    else
    {
        const reference = qNthElement(definition.angleReference, 0);
        if (isQueryEmpty(context, reference))
        {
            throw regenError("Select an angle reference.", ["angleReference"]);
        }
        const point = angleReferencePoint(context, reference);
        if (point == undefined)
        {
            throw regenError("The angle reference must be a point.", ["angleReference"], reference);
        }
        gap = dot(point - sketchPlane.origin, direction);
    }

    const centers = rackToothCenters(form, length, gap);
    if (centers == [])
    {
        throw regenError("The face is too short for a whole tooth.", ["rackFace"], faces);
    }
    const faceHeight = definition.rackFaceAt == RackFaceAt.OUTSIDE ? form.module : 0 * meter;
    const profile = sketchRackCut(context, id + "profile", sketchPlane, rackCutRegions(form, faceHeight, length, centers));
    opExtrude(context, id + "extrude", {
                "entities" : profile,
                "direction" : frame.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : width
            });
    cleanup(context, id + "deleteSketch", qCreatedBy(id + "profile", EntityType.BODY));
    const part = qOwnerBody(face);
    opBoolean(context, id + "cut", {
                "targets" : part,
                "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    if (offsetProfile(definition))
    {
        // Out of the part, through the root of the gap after the first tooth
        const gapCenter = centers[0] + PI * form.module / 2;
        const below = planeToWorld3D(sketchPlane, vector(gapCenter, rackRoot(form) - faceHeight - form.module, 0 * meter));
        addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(below + frame.zAxis * width / 2, facePlane.normal),
            qIntersection([qOwnedByBody(part, EntityType.FACE), qCreatedBy(id, EntityType.FACE)]), definition.profileOffsetOppositeDirection);
    }
}

/**
 * The direction of `faces`' longest straight edge, or `undefined` if they have none.
 */
function longestEdgeDirection(context is Context, faces is Query)
{
    var longest;
    var longestLength = 0 * meter;
    for (var edge in evaluateQuery(context, qAdjacent(faces, AdjacencyType.EDGE, EntityType.EDGE)->qGeometry(GeometryType.LINE)))
    {
        const length = evLength(context, { "entities" : edge });
        if (length > longestLength)
        {
            longest = edge;
            longestLength = length;
        }
    }
    return longest == undefined ? undefined : evLine(context, { "edge" : longest }).direction;
}

/**
 * The module: one inch over the diametral pitch, in inches.
 */
function gearModule(definition is map) returns ValueWithUnits
{
    return isImperial(definition) ? 1 * inch / definition.diametralPitch : definition.module;
}

function setGearProperties(context is Context, gear is Query, definition is map)
{
    // 24T 20DP Gear, or 30T Module 1 Gear
    const size = isImperial(definition) ? definition.diametralPitch ~ "DP" : "Module " ~ roundToPrecision(definition.module / millimeter, 3);
    setProperty(context, {
                "entities" : gear,
                "propertyType" : PropertyType.NAME,
                "value" : definition.teeth ~ "T " ~ size ~ " Gear"
            });
    setProperty(context, {
                "entities" : gear,
                "propertyType" : PropertyType.MATERIAL,
                "value" : PLASTIC
            });
    setProperty(context, {
                "entities" : gear,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : PRINTED_GREEN
            });
}

export function robotGearManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators[RACK_END_MANIPULATOR] != undefined)
    {
        definition.oppositeEnd = newManipulators[RACK_END_MANIPULATOR].flipped;
        return definition;
    }
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return startOffsetManipulatorChange(definition, newManipulators);
}
