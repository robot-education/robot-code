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
    annotation { "Name" : "Gear" }
    GEAR,
    annotation { "Name" : "Sector" }
    SECTOR,
    annotation { "Name" : "Rack" }
    RACK
}

/**
 * What sizes a gear: its teeth, or a pitch circle (whose center places it, too).
 */
export enum GearSize
{
    annotation { "Name" : "Teeth" }
    TEETH,
    annotation { "Name" : "Pitch circle" }
    PITCH_CIRCLE
}

/**
 * Robot gears' standard pitches (with their pressure angles, see `GEAR_PITCHES`), or a custom one.
 */
export enum GearPitch
{
    annotation { "Name" : "10 DP" }
    DP_10,
    annotation { "Name" : "20 DP" }
    DP_20,
    annotation { "Name" : "32 DP" }
    DP_32,
    annotation { "Name" : "Module 0.8" }
    MODULE_0_8,
    annotation { "Name" : "Custom" }
    CUSTOM
}

/**
 * A helical gear's hand: which way its teeth twist. Meshing helical gears have opposite hands.
 */
export enum GearHand
{
    annotation { "Name" : "Right hand" }
    RIGHT,
    annotation { "Name" : "Left hand" }
    LEFT
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

const HELIX_ANGLE_BOUNDS = { (degree) : [5, 15, 45] } as AngleBoundSpec;

const RACK_END_MANIPULATOR = "rackEnd";

/**
 * The standard pitches' modules, pressure angles, and names (in part names). FRC's 20 DP gears (VEXpro, WCP, and
 * AndyMark) are 14.5°; its 32 DP gears (WCP), and FTC's module 0.8 gears (goBILDA), are 20°. 10 DP is for 3D printing:
 * bigger teeth, at 20°, which makes them stronger at the root than 14.5°.
 */
const GEAR_PITCHES = {
        (GearPitch.DP_10) : { "module" : inch / 10, "pressureAngle" : 20 * degree, "name" : "10DP" },
        (GearPitch.DP_20) : { "module" : inch / 20, "pressureAngle" : 14.5 * degree, "name" : "20DP" },
        (GearPitch.DP_32) : { "module" : inch / 32, "pressureAngle" : 20 * degree, "name" : "32DP" },
        (GearPitch.MODULE_0_8) : { "module" : 0.8 * millimeter, "pressureAngle" : 20 * degree, "name" : "Module 0.8" }
    };

/**
 * A gear or a sector of one: not a rack.
 */
predicate isGear(definition is map)
{
    definition.gearType != GearType.RACK;
}

/**
 * Makes involute spur, helical, and herringbone gears (or sectors of them), with optional root rounds a router bit can
 * cut, or cuts rack teeth into a part's face; at robot gears' standard pitches, or by diametral pitch (in inches) or
 * module (metric).
 */
annotation {
        "Feature Type Name" : "Robot gear",
        "Feature Type Description" : "Create spur, helical, and herringbone gears, or cut racks into parts, at FRC and FTC gears' standard pitches." ~ CREDIT,
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
            annotation { "Name" : "Size by", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
            definition.sizeBy is GearSize;

            if (definition.sizeBy == GearSize.TEETH)
            {
                locationPredicate(definition, "gear");
            }
            else
            {
                annotation { "Name" : "Pitch circle", "Filter" : EntityType.EDGE && (GeometryType.CIRCLE || GeometryType.ARC), "MaxNumberOfPicks" : 1,
                            "Description" : "A circle or arc: its center places the gear, and its size sets its teeth. A sector is centered on an arc." }
                definition.pitchCircle is Query;
            }
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
            annotation { "Name" : "Pitch", "Default" : GearPitch.DP_20, "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "10 DP at 20° (3D printing), 20 DP at 14.5° (FRC: VEXpro, WCP, AndyMark), 32 DP at 20° (FRC: WCP), or module 0.8 at 20° (FTC: goBILDA)." }
            definition.pitch is GearPitch;

            if (definition.pitch == GearPitch.CUSTOM)
            {
                if (isImperial(definition))
                {
                    annotation { "Name" : "Diametral pitch", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Teeth per inch of pitch diameter." }
                    isReal(definition.diametralPitch, DIAMETRAL_PITCH_BOUNDS);
                }
                else
                {
                    annotation { "Name" : "Module", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Pitch diameter per tooth." }
                    isLength(definition.module, MODULE_BOUNDS);
                }

                annotation { "Name" : "Pressure angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isAngle(definition.pressureAngle, PRESSURE_ANGLE_BOUNDS);
            }

            if (isGear(definition) && definition.sizeBy == GearSize.TEETH)
            {
                annotation { "Name" : "Teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The whole gear's." }
                isInteger(definition.teeth, GEAR_TEETH_BOUNDS);
            }

            if (definition.gearType == GearType.SECTOR)
            {
                annotation { "Name" : "Sector teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The sector's: a wedge of the gear's, from its center." }
                isInteger(definition.sectorTeeth, SECTOR_TEETH_BOUNDS);
            }

            if (isGear(definition))
            {
                annotation { "Name" : "Thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.thickness, THICKNESS_BOUNDS);
            }
        }

        if (isGear(definition))
        {
            annotation { "Name" : "Helical", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Teeth on a helix: quieter and smoother, but they push the gear along its shaft." }
            definition.helical is boolean;

            if (definition.helical)
            {
                annotation { "Group Name" : "Helical", "Collapsed By Default" : false, "Driving Parameter" : "helical" }
                {
                    annotation { "Name" : "Helix angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isAngle(definition.helixAngle, HELIX_ANGLE_BOUNDS);

                    annotation { "Name" : "Hand", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Description" : "Meshing helical gears have opposite hands." }
                    definition.hand is GearHand;

                    annotation { "Name" : "Double helical", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                                "Description" : "Herringbone: two halves of opposite hands, so they don't push along the shaft." }
                    definition.doubleHelical is boolean;
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
            cutRack(context, id, definition, getGearForm(module, undefined, pressureAngle(definition), standardFilletRadius(module), getProfileOffset(definition)));
            return;
        }

        const size = gearSize(context, definition, module);
        const gearPlane = applyStartOffset(context, definition, size.plane);
        addStartOffsetManipulator(context, id, definition, gearPlane);

        const fillet = definition.routerRelief ? definition.bitDiameter / 2 : standardFilletRadius(module);
        const form = getGearForm(module, size.teeth, pressureAngle(definition), fillet, getProfileOffset(definition));
        const sector = definition.gearType == GearType.SECTOR;
        if (sector && definition.sectorTeeth >= size.teeth)
        {
            throw regenError("A sector has fewer teeth than its gear.", ["sectorTeeth"]);
        }
        const profile = sketchGearProfile(context, id + "profile", gearPlane, form, sector ? definition.sectorTeeth : undefined);
        const gear = buildGearBody(context, id, definition, gearPlane, profile, form);
        cleanup(context, id + "deleteSketch", qCreatedBy(id + "profile", EntityType.BODY));
        setGearProperties(context, gear, definition, size.teeth);

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
        if (size.warning != undefined)
        {
            reportFeatureWarning(context, id, size.warning, ["pitchCircle"]);
        }
        else
        {
            // Its pitch diameter, for spacing gears: two mesh their pitch radii apart
            reportFeatureInfo(context, id, "Its pitch diameter is " ~ makeValueString(definition.unitSystem, 2 * form.pitchRadius, true) ~ ".");
        }
    });

/**
 * The gear's teeth and plane: Teeth's, at the location; or with Size by Pitch circle, the nearest whole number of
 * teeth's, at the circle's center, with a `warning` if the circle isn't exactly their size (within std's length
 * tolerance).
 *
 * @returns {{
 *      @field teeth {number} :
 *      @field plane {Plane} : Before the starting offset.
 *      @field warning {string} : If the pitch circle isn't a whole number of teeth's.
 * }}
 */
function gearSize(context is Context, definition is map, module is ValueWithUnits) returns map
{
    if (definition.sizeBy == GearSize.TEETH)
    {
        return { "teeth" : definition.teeth, "plane" : getLocationPlane(context, definition) };
    }
    const circle = verifyNonemptyQuery(context, definition, "pitchCircle", "Select a pitch circle.")[0];
    const curve = evCurveDefinition(context, { "edge" : circle });
    const radius = curve.radius;
    const teeth = nearestGearTeeth(radius, module);
    // Teeth's least
    if (teeth < 4)
    {
        throw regenError("The pitch circle is too small for a gear of this pitch.", ["pitchCircle"], circle);
    }
    var result = { "teeth" : teeth, "plane" : curve.coordSystem->plane() };
    // An arc's middle: a sector's centered on it
    if (!isQueryEmpty(context, circle->qGeometry(GeometryType.ARC)))
    {
        const middle = evEdgeTangentLine(context, { "edge" : circle, "parameter" : 0.5 }).origin;
        result.plane.x = normalize(middle - result.plane.origin);
    }
    if (!tolerantEquals(teeth * module / 2, radius))
    {
        result.warning = "The pitch circle isn't the size of a gear with a whole number of teeth: it's the nearest, with " ~ teeth ~ " teeth.";
    }
    return result;
}

/**
 * The teeth of the gear whose pitch circle's nearest `radius` in size: its pitch diameter is `module` per tooth.
 */
function nearestGearTeeth(radius is ValueWithUnits, module is ValueWithUnits) returns number
{
    return round(2 * radius / module);
}

/**
 * The gear's body: its profile extruded the thickness both ways from its plane; or with Helical, swept both ways along
 * helices on its pitch circle, at the helix angle, which turn it about its axis as it goes. The halves are the same
 * hand (a helical gear's teeth twist one way through it), or with Double helical, opposite hands (a herringbone). Its
 * teeth are where a spur gear's would be at its plane, its middle.
 */
function buildGearBody(context is Context, id is Id, definition is map, gearPlane is Plane, profile is Query, form is map) returns Query
{
    if (!definition.helical)
    {
        opExtrude(context, id + "extrude", {
                    "entities" : profile,
                    "direction" : gearPlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.thickness / 2,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : definition.thickness / 2
                });
        return qCreatedBy(id + "extrude", EntityType.BODY);
    }
    // How far along the axis the teeth go to turn once: the pitch circle's circumference over the helix angle's tangent
    const lead = 2 * PI * form.pitchRadius / tan(definition.helixAngle);
    // A right hand helix turns clockwise looking along it, either way
    const right = definition.hand == GearHand.RIGHT;
    var halves = [];
    for (var half in ["up", "down"])
    {
        const halfId = id + half;
        opHelix(context, halfId + "helix", {
                    "direction" : gearPlane.normal * (half == "up" ? 1 : -1),
                    "axisStart" : gearPlane.origin,
                    "startPoint" : gearPlane.origin + gearPlane.x * form.pitchRadius,
                    "interval" : [0, definition.thickness / 2 / lead],
                    "clockwise" : half == "down" && definition.doubleHelical ? !right : right,
                    "helicalPitch" : lead,
                    "spiralPitch" : 0 * meter
                });
        opSweep(context, halfId + "sweep", {
                    "profiles" : profile,
                    "path" : qCreatedBy(halfId + "helix", EntityType.EDGE)
                });
        halves = append(halves, qCreatedBy(halfId + "sweep", EntityType.BODY));
    }
    cleanup(context, id + "deleteHelices", qUnion([qCreatedBy(id + "up" + "helix", EntityType.BODY), qCreatedBy(id + "down" + "helix", EntityType.BODY)]));
    opBoolean(context, id + "join", {
                "tools" : qUnion(halves),
                "operationType" : BooleanOperationType.UNION
            });
    return qUnion(halves);
}

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
    for (var edge in evaluateQuery(context, qLoopEdges(faces)->qGeometry(GeometryType.LINE)))
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
    if (definition.pitch != GearPitch.CUSTOM)
    {
        return GEAR_PITCHES[definition.pitch].module;
    }
    return isImperial(definition) ? 1 * inch / definition.diametralPitch : definition.module;
}

/**
 * The pressure angle: a standard pitch's, or Pressure angle.
 */
function pressureAngle(definition is map) returns ValueWithUnits
{
    return definition.pitch == GearPitch.CUSTOM ? definition.pressureAngle : GEAR_PITCHES[definition.pitch].pressureAngle;
}

function setGearProperties(context is Context, gear is Query, definition is map, teeth is number)
{
    // 24T 20DP Gear, 30T Module 1 Helical Gear, 40T Module 0.8 Herringbone Gear, or 60T 10DP Sector Gear
    var size;
    if (definition.pitch != GearPitch.CUSTOM)
    {
        size = GEAR_PITCHES[definition.pitch].name;
    }
    else
    {
        size = isImperial(definition) ? definition.diametralPitch ~ "DP" : "Module " ~ roundToPrecision(definition.module / millimeter, 3);
    }
    const shape = definition.gearType == GearType.SECTOR ? "Sector Gear" : "Gear";
    const kind = !definition.helical ? shape : (definition.doubleHelical ? "Herringbone " : "Helical ") ~ shape;
    setProperty(context, {
                "entities" : gear,
                "propertyType" : PropertyType.NAME,
                "value" : teeth ~ "T " ~ size ~ " " ~ kind
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
