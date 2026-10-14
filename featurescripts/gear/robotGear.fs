FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "ea127c07807644fb48d3a1ae", version : "3c1ddfaf5ff0b3d5897422d0");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
import(path : "0103ad63394d7713fbf44448", version : "5f7d67b62160df309da69ec3");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "01f0c5634015659514b83da1", version : "1a4ff017f8d67f81b129502b");
import(path : "f9a7532fe0d68211bd3a8112", version : "22f75fd6cadf552cd38dd645");
import(path : "58d66340f7b70cfc86606676", version : "88137a312d950df85d20e7f9");

export enum GearType
{
    annotation { "Name" : "Gear" }
    GEAR,
    annotation { "Name" : "Sector" }
    SECTOR,
    annotation { "Name" : "Rack" }
    RACK,
    annotation { "Name" : "Bevel" }
    BEVEL
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
const SHAFT_ANGLE_BOUNDS = { (degree) : [10, 90, 170] } as AngleBoundSpec;
const FACE_WIDTH_BOUNDS = { (meter) : [1e-5, 0.00635, 500], (inch) : 0.25, (millimeter) : 6 } as LengthBoundSpec;

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
 * A gear, a sector of one, or a bevel gear: not a rack.
 */
predicate isGear(definition is map)
{
    definition.gearType != GearType.RACK;
}

/**
 * Made with its mate: with Gear pair, or a bevel gear, whose cones depend on its mate.
 */
predicate isPair(definition is map)
{
    definition.gearType != GearType.RACK;
    definition.gearType == GearType.BEVEL || definition.gearPair;
}

/**
 * A pair whose mate is placed by its teeth and the angle (a bevel gear's always is), not its pitch circle.
 */
predicate isMateByTeeth(definition is map)
{
    definition.gearType == GearType.BEVEL || definition.mateSizeBy == GearSize.TEETH;
}

/**
 * A spur gear or a sector of one.
 */
predicate isSpur(definition is map)
{
    definition.gearType == GearType.GEAR || definition.gearType == GearType.SECTOR;
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
            annotation { "Name" : "Selection", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
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

            startOffsetPredicate(definition);

            // A bevel gear is made with its mate, which its cones depend on
            if (definition.gearType != GearType.BEVEL)
            {
                annotation { "Name" : "Gear pair", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Also make the gear it meshes with." }
                definition.gearPair is boolean;
            }

            if (isPair(definition))
            {
                annotation { "Group Name" : "Gear pair", "Collapsed By Default" : false, "Driving Parameter" : "gearPair" }
                {
                    // A bevel gear's mate is placed by its teeth: it shares the apex
                    if (definition.gearType != GearType.BEVEL)
                    {
                        annotation { "Name" : "Mate selection", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.mateSizeBy is GearSize;
                    }

                    if (isMateByTeeth(definition))
                    {
                        annotation { "Name" : "Mating gear teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The gear it meshes with." }
                        isInteger(definition.mateTeeth, GEAR_TEETH_BOUNDS);

                        if (definition.gearType == GearType.BEVEL)
                        {
                            annotation { "Name" : "Shaft angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Between its shaft and its mate's." }
                            isAngle(definition.shaftAngle, SHAFT_ANGLE_BOUNDS);
                        }

                        // Where its mate is, around it: toward the angle reference, turned by the angle
                        angleReferencePredicate(definition);

                        angleOffsetPredicate(definition);
                    }
                    else
                    {
                        annotation { "Name" : "Mate pitch circle", "Filter" : EntityType.EDGE && GeometryType.CIRCLE, "MaxNumberOfPicks" : 1,
                                    "Description" : "Its center places the mate, and its size sets its teeth. It should touch the gear's." }
                        definition.matePitchCircle is Query;
                    }
                }
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
                definition.rackReference is Query;
            }
        }

        annotation { "Group Name" : "Teeth", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Pitch", "Default" : GearPitch.DP_20, "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
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

            if (isSpur(definition))
            {
                annotation { "Name" : "Thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.thickness, THICKNESS_BOUNDS);
            }

            if (definition.gearType == GearType.BEVEL)
            {
                annotation { "Name" : "Face width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Its teeth's length, from its back toward its apex." }
                isLength(definition.faceWidth, FACE_WIDTH_BOUNDS);
            }
        }

        // Bevel gears are straight
        if (definition.gearType != GearType.BEVEL)
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

                    annotation { "Name" : "Double helical", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                                "Description" : "Herringbone: two halves of opposite hands, so they don't push along the shaft." }
                    definition.doubleHelical is boolean;

                    // A herringbone turned over is the other hand
                    if (!definition.doubleHelical)
                    {
                        annotation { "Name" : "Hand", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Description" : "Meshing helical gears have opposite hands." }
                        definition.hand is GearHand;
                    }
                }
            }
        }

        // Bevel gears' roots are cut sharp
        if (definition.gearType != GearType.BEVEL)
        {
            annotation { "Name" : "Router relief", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Round the roots to a router bit's radius, so it can cut them (a full round where the gap's too narrow for both corners)." }
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

        // Its pitch diameter, for spacing gears: two mesh their pitch radii apart
        var info = "Its pitch diameter is " ~ makeValueString(definition.unitSystem, module * size.teeth, true) ~ ".";
        const gear = buildGear(context, id, id + "gear", definition, gearPlane, module, size.teeth);
        if (definition.gearType == GearType.BEVEL)
        {
            info = "Its pitch diameter is " ~ makeValueString(definition.unitSystem, module * size.teeth, true) ~ ", and its apex (where its mate's goes) is " ~
                makeValueString(definition.unitSystem, norm(gear.apex - gearPlane.origin), true) ~ " in front of its back.";
        }

        if (offsetProfile(definition) && isSpur(definition))
        {
            const faces = qOwnedByBody(gear.body, EntityType.FACE);
            addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(gearPlane.origin, gearPlane.x), qSubtraction(faces, qParallelPlanes(faces, gearPlane)),
                definition.profileOffsetOppositeDirection);
        }

        var gears = [gear];
        var warning = size.warning;
        if (isPair(definition))
        {
            const place = matePlacement(context, id, definition, gearPlane, module, size.teeth);
            const mate = mateDefinition(definition, size.teeth);
            gears = append(gears, buildGear(context, id, id + "mate", mate, place.plane, module, place.teeth));
            if (definition.gearType != GearType.BEVEL)
            {
                info = info ~ " The gears' centers are " ~ makeValueString(definition.unitSystem, norm(place.plane.origin - gearPlane.origin), true) ~ " apart.";
            }
            warning = warning ?? place.warning;
        }

        const bore = definitionBore(definition);
        for (var i, built in gears)
        {
            if (definition.addMateConnectors)
            {
                opMateConnector(context, id + ("mateConnector" ~ (i == 0 ? "" : i)), {
                            "coordSystem" : built.connector,
                            "owner" : built.body
                        });
            }
            if (bore != undefined)
            {
                cutBores(context, id + ("bore" ~ (i == 0 ? "" : i)), bore, [built.plane], [undefined], [built.body]);
            }
        }
        if (warning != undefined)
        {
            reportFeatureWarning(context, id, warning, [warning == size.warning ? "pitchCircle" : "matePitchCircle"]);
        }
        else
        {
            reportFeatureInfo(context, id, info);
        }
    });

/**
 * A gear (a spur, helical, or herringbone gear, a sector, or a bevel gear) with `teeth`, centered on `gearPlane` (a
 * bevel gear's back), its first tooth on its x axis, and its properties set. `featureId` is the feature's, for statuses.
 *
 * @returns {{
 *      @field body {Query} :
 *      @field plane {Plane} : `gearPlane`, for its bore.
 *      @field connector {CoordSystem} : Where its mate connector goes: its center, or a bevel gear's apex.
 *      @field apex {Vector} : A bevel gear's.
 * }}
 */
function buildGear(context is Context, featureId is Id, id is Id, definition is map, gearPlane is Plane, module is ValueWithUnits, teeth is number) returns map
{
    var result = { "plane" : gearPlane, "connector" : coordSystem(gearPlane) };
    if (definition.gearType == GearType.BEVEL)
    {
        const bevel = buildBevelGear(context, featureId, id, definition, gearPlane, module, teeth);
        result.body = bevel.body;
        result.apex = bevel.apex;
        // Mated at their apexes, with their axes the shaft angle apart, a pair meshes
        result.connector = coordSystem(bevel.apex, gearPlane.x, gearPlane.normal);
    }
    else
    {
        const fillet = definition.routerRelief ? definition.bitDiameter / 2 : standardFilletRadius(module);
        const form = getGearForm(module, teeth, pressureAngle(definition), fillet, getProfileOffset(definition));
        const sector = definition.gearType == GearType.SECTOR;
        if (sector && definition.sectorTeeth >= teeth)
        {
            throw regenError("A sector has fewer teeth than its gear.", ["sectorTeeth"]);
        }
        const profile = sketchGearProfile(context, id + "profile", gearPlane, form, sector ? definition.sectorTeeth : undefined);
        result.body = buildGearBody(context, id, definition, gearPlane, profile, form);
        cleanup(context, id + "deleteSketch", qCreatedBy(id + "profile", EntityType.BODY));
    }
    setGearProperties(context, result.body, definition, teeth);
    return result;
}

/**
 * Where a gear's mate goes, and its teeth: by Mating gear teeth, toward the angle reference turned by the angle (with
 * its manipulator); or by Mate pitch circle, at its center (on the gear's plane), with the nearest whole number of
 * teeth to its size, and a `warning` if it isn't exactly their size, or doesn't touch the gear's pitch circle.
 *
 * @returns {{
 *      @field plane {Plane} : See `gearMatePlane`.
 *      @field teeth {number} :
 *      @field warning {string} : @optional
 * }}
 */
function matePlacement(context is Context, id is Id, definition is map, gearPlane is Plane, module is ValueWithUnits, teeth is number) returns map
{
    if (isMateByTeeth(definition))
    {
        // Toward its mate: the angle reference's direction, turned by the angle
        const referencePlane = applyAngleReference(context, definition, gearPlane, definition.sizeBy == GearSize.TEETH ? "location" : "pitchCircle");
        addAngleOffsetManipulator(context, id, definition, referencePlane, module * teeth / 2);
        const toward = applyAngleOffset(definition, referencePlane).x;
        return { "plane" : gearMatePlane(gearPlane, toward, gearMesh(definition, module, teeth, definition.mateTeeth)), "teeth" : definition.mateTeeth };
    }
    const circle = verifyNonemptyQuery(context, definition, "matePitchCircle", "Select the mate's pitch circle.")[0];
    const curve = evCurveDefinition(context, { "edge" : circle });
    if (!parallelVectors(curve.coordSystem.zAxis, gearPlane.normal))
    {
        throw regenError("The mate's pitch circle must be parallel to the gear's.", ["matePitchCircle"], circle);
    }
    const mateTeeth = nearestGearTeeth(curve.radius, module);
    if (mateTeeth < 4)
    {
        throw regenError("The mate's pitch circle is too small for a gear of this pitch.", ["matePitchCircle"], circle);
    }
    const center = project(gearPlane, curve.coordSystem.origin);
    const distance = norm(center - gearPlane.origin);
    if (tolerantEquals(distance, 0 * meter))
    {
        throw regenError("The mate's pitch circle is centered on the gear.", ["matePitchCircle"], circle);
    }
    var plane = gearMatePlane(gearPlane, (center - gearPlane.origin) / distance, gearMesh(definition, module, teeth, mateTeeth));
    plane.origin = center;
    var result = { "plane" : plane, "teeth" : mateTeeth };
    if (!tolerantEquals(mateTeeth * module / 2, curve.radius))
    {
        result.warning = "The mate's pitch circle isn't the size of a gear with a whole number of teeth: it's the nearest, with " ~ mateTeeth ~ " teeth.";
    }
    else if (!tolerantEquals(distance, module * (teeth + mateTeeth) / 2))
    {
        result.warning = "The pitch circles don't touch, so the gears don't mesh.";
    }
    return result;
}

/**
 * The definition of a gear's mate (with `teeth`, its mate's): a whole gear, even for a sector, of the opposite hand,
 * with Mating gear teeth's mate the gear.
 */
function mateDefinition(definition is map, teeth is number) returns map
{
    var mate = definition;
    if (mate.gearType == GearType.SECTOR)
    {
        mate.gearType = GearType.GEAR;
    }
    if (mate.hand != undefined)
    {
        mate.hand = mate.hand == GearHand.RIGHT ? GearHand.LEFT : GearHand.RIGHT;
    }
    mate.mateTeeth = teeth;
    return mate;
}

/**
 * How a gear (with `teeth`) and its mate (with `mateTeeth`) mesh, for `gearMatePlane`: their teeth, the angle
 * between their teeth's centers (`pitch`, for the gear), where the gear's first tooth is, from its x axis (a sector with
 * an even number of teeth has a gap there), and for a spur pair, how far apart their centers are, or for a bevel pair,
 * their cones (see `bevelPairCones`) and shaft angle.
 */
export function gearMesh(definition is map, module is ValueWithUnits, teeth is number, mateTeeth is number) returns map
{
    var result = {
        "teeth" : teeth,
        "mateTeeth" : mateTeeth,
        "toothPhase" : definition.gearType == GearType.SECTOR && definition.sectorTeeth % 2 == 0 ? 180 * degree / teeth : 0 * degree
    };
    if (definition.gearType == GearType.BEVEL)
    {
        result.cones = bevelPairCones(module, teeth, mateTeeth, definition.shaftAngle);
        result.shaftAngle = definition.shaftAngle;
    }
    else
    {
        result.centerDistance = module * (teeth + mateTeeth) / 2;
    }
    return result;
}

/**
 * Where a gear's mate goes, meshing with it, with the gear on `gearPlane` (its center, or a bevel gear's back) and the
 * mate toward `toward` (a direction square to its axis) from it: its plane (a bevel gear's back), with its first tooth
 * on its x axis, as `buildGear` builds it. A spur mate is the gears' pitch radii away, on the same plane. A bevel mate
 * shares the gear's apex, its axis the shaft angle from the gear's, and its pitch cone touching the gear's toward
 * `toward`.
 *
 * Their teeth mesh where their pitch circles touch: with a tooth of the gear there, the mate has a gap. The gear's
 * tooth nearest it is `angle` (from its first tooth to `toward`) less a whole number of teeth around; turning the gear
 * back by that angle turns the mate forward by it times the gears' ratio (each about its axis toward its apex, for
 * bevel gears), so the mate turns from a gap there by that much.
 */
export function gearMatePlane(gearPlane is Plane, toward is Vector, mesh is map) returns Plane
{
    const axis = gearPlane.normal;
    const angle = atan2(dot(cross(gearPlane.x, toward), axis), dot(gearPlane.x, toward)) - mesh.toothPhase;
    const turn = 180 * degree / mesh.mateTeeth + angle * mesh.teeth / mesh.mateTeeth;
    var origin;
    var mateAxis;
    var pitchPoint;
    if (mesh.cones == undefined)
    {
        origin = gearPlane.origin + toward * mesh.centerDistance;
        mateAxis = axis;
        pitchPoint = gearPlane.origin + toward * (mesh.centerDistance * mesh.teeth / (mesh.teeth + mesh.mateTeeth));
    }
    else
    {
        const cones = mesh.cones;
        const apex = gearPlane.origin + axis * cones.gear.coneDistance * cos(cones.gear.pitchAngle);
        // From the apex: out along the cones' touching line to their pitch circles, and to the mate's back
        pitchPoint = apex + (-axis * cos(cones.gear.pitchAngle) + toward * sin(cones.gear.pitchAngle)) * cones.gear.coneDistance;
        const back = -axis * cos(mesh.shaftAngle) + toward * sin(mesh.shaftAngle);
        origin = apex + back * cones.gear.coneDistance * cos(cones.mate.pitchAngle);
        mateAxis = -back;
    }
    const facing = plane(origin, mateAxis, normalize(pitchPoint - origin));
    return rotationAround(line(origin, mateAxis), turn) * facing;
}

/**
 * A bevel gear's cones (see `bevelCones`), and its mate's, as `gear` and `mate`: an error if either pitch cone is 90°
 * or more, as then it would be a crown or internal gear, not a bevel gear.
 */
export function bevelPairCones(module is ValueWithUnits, teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits) returns map
{
    const cones = bevelCones(module, teeth, mateTeeth, shaftAngle);
    if (cones.pitchAngle >= 90 * degree - TOLERANCE.zeroAngle * radian)
    {
        throw regenError("At this shaft angle, it would be a crown or internal gear: use a smaller shaft angle, or more mating teeth.", ["shaftAngle", "mateTeeth"]);
    }
    const mate = bevelCones(module, mateTeeth, teeth, shaftAngle);
    if (mate.pitchAngle >= 90 * degree - TOLERANCE.zeroAngle * radian)
    {
        throw regenError("At this shaft angle, its mate would be a crown or internal gear: use a smaller shaft angle, or fewer mating teeth.", ["shaftAngle", "mateTeeth"]);
    }
    return { "gear" : cones, "mate" : mate };
}

/**
 * A straight bevel gear (Tredgold's approximation, see `bevelCones`), with its pitch circle on `gearPlane` (its back)
 * and its apex in front of it, along its normal. In steps:
 *
 * 1. Its blank: revolved from its section through its axis, between its root and tip cones, from its back cone to the
 *    same cone scaled Face width toward the apex (its front).
 * 2. A tooth gap: its back cone's virtual spur gear's gap (`sketchToothGap`), on the plane square to its pitch cone at
 *    its first tooth, lofted to the same gap scaled toward the apex. Scaled about the apex, its flanks run straight
 *    to it. It runs a tooth's height past the blank at both ends (or at the front, half way to the apex, if that's
 *    nearer), so it cuts through it.
 * 3. The gap, patterned around the axis once per tooth, and cut from the blank.
 *
 * @returns {{
 *      @field body {Query} :
 *      @field apex {Vector} :
 * }}
 */
function buildBevelGear(context is Context, featureId is Id, id is Id, definition is map, gearPlane is Plane, module is ValueWithUnits, teeth is number) returns map
{
    const cones = bevelPairCones(module, teeth, definition.mateTeeth, definition.shaftAngle).gear;
    if (definition.faceWidth >= cones.coneDistance)
    {
        throw regenError("The face width reaches the gear's apex: make it narrower.", ["faceWidth"]);
    }
    const offset = getProfileOffset(definition);
    const form = getGearForm(module, cones.virtualTeeth, pressureAngle(definition), standardFilletRadius(module), offset);

    const axis = gearPlane.normal;
    const x = gearPlane.x;
    const apex = gearPlane.origin + axis * cones.coneDistance * cos(cones.pitchAngle);
    // Along the back cone, out from the virtual spur gear's center to the first tooth's pitch point
    const radial = x * cos(cones.pitchAngle) + axis * sin(cones.pitchAngle);
    const pitchPoint = gearPlane.origin + x * cones.pitchRadius;
    const virtualCenter = pitchPoint - radial * form.pitchRadius;
    const scaled = function(point is Vector, scale is number) returns Vector
        {
            return apex + (point - apex) * scale;
        };
    // Its front, scaled about the apex: the back's scale is 1
    const front = (cones.coneDistance - definition.faceWidth) / cones.coneDistance;

    // 1. The blank, revolved from its section: across its tip and root cones, from its back to its front, and to its axis
    const tipBack = pitchPoint + radial * (form.tipRadius - form.pitchRadius);
    const rootBack = pitchPoint + radial * (form.rootRadius - form.pitchRadius);
    const sectionPlane = plane(gearPlane.origin, cross(x, axis), x);
    const section = mapArray([tipBack, scaled(tipBack, front), scaled(rootBack, front), rootBack], point => worldToPlane(sectionPlane, point));
    const outline = [section[0], section[1], section[2], vector(0 * meter, section[2][1]), vector(0 * meter, section[3][1]), section[3]];
    const blankSketch = newSketchOnPlane(context, id + "section", { "sketchPlane" : sectionPlane });
    for (var i, point in outline)
    {
        skLineSegment(blankSketch, "line" ~ i, { "start" : point, "end" : outline[(i + 1) % size(outline)] });
    }
    skSolve(blankSketch);
    opRevolve(context, id + "blank", {
                "entities" : qCreatedBy(id + "section", EntityType.FACE),
                "axis" : line(gearPlane.origin, axis),
                "angleForward" : 360 * degree
            });
    const blank = qCreatedBy(id + "blank", EntityType.BODY);

    // 2. A gap, lofted between its virtual spur gear's gaps at both ends, scaled about the apex
    const height = form.tipRadius - form.rootRadius;
    const scales = [1 + height / cones.coneDistance, max(front - height / cones.coneDistance, front / 2)];
    var gapFaces = [];
    for (var i, scale in scales)
    {
        const gapPlane = plane(scaled(virtualCenter, scale), cross(radial, gearPlane->yAxis()), radial);
        const scaledForm = getGearForm(module * scale, cones.virtualTeeth, pressureAngle(definition), standardFilletRadius(module) * scale, offset * scale);
        gapFaces = append(gapFaces, sketchToothGap(context, id + ("gap" ~ i), gapPlane, scaledForm));
    }
    try silent
    {
        opLoft(context, id + "loft", {
                    "profileSubqueries" : gapFaces,
                    "bodyType" : ToolBodyType.SOLID
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "loft", "propagateErrorDisplay" : true });
        throw regenError("Failed to create the bevel gear's teeth.", ["teeth", "mateTeeth", "faceWidth"], qUnion(gapFaces));
    }

    // 3. Patterned once per tooth, and cut from the blank
    var transforms = [];
    var names = [];
    for (var i = 1; i < teeth; i += 1)
    {
        transforms = append(transforms, rotationAround(line(gearPlane.origin, axis), i * 360 * degree / teeth));
        names = append(names, "" ~ i);
    }
    opPattern(context, id + "gaps", {
                "entities" : qCreatedBy(id + "loft", EntityType.BODY),
                "transforms" : transforms,
                "instanceNames" : names
            });
    const gaps = qUnion([qCreatedBy(id + "loft", EntityType.BODY), qCreatedBy(id + "gaps", EntityType.BODY)]);
    try silent
    {
        opBoolean(context, id + "cut", {
                    "targets" : blank,
                    "tools" : gaps,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "cut", "propagateErrorDisplay" : true });
        throw regenError("Failed to cut the bevel gear's teeth.", ["teeth", "mateTeeth", "faceWidth"], gaps);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return { "body" : blank, "apex" : apex };
}

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
        const reference = qNthElement(definition.rackReference, 0);
        if (isQueryEmpty(context, reference))
        {
            throw regenError("Select an angle reference.", ["rackReference"]);
        }
        const point = angleReferencePoint(context, reference);
        if (point == undefined)
        {
            throw regenError("The angle reference must be a point.", ["rackReference"], reference);
        }
        gap = dot(point - sketchPlane.origin, direction);
    }

    // Where the teeth are across the middle of its width (a helical rack's slant across it)
    const centers = rackToothCenters(form, length, gap);
    if (centers == [])
    {
        throw regenError("The face is too short for a whole tooth.", ["rackFace"], faces);
    }
    const faceHeight = definition.rackFaceAt == RackFaceAt.OUTSIDE ? form.module : 0 * meter;
    const relief = definition.routerRelief ? definition.bitDiameter / 2 : 0 * meter;
    var tools = [];
    for (var i, piece in rackPieces(definition, width))
    {
        // Sketched where the piece starts across the width, over the face's length and as far past its ends as the
        // teeth slant, so the ends are cut to the root all the way across
        const extra = abs(piece.slant) * width;
        const shift = piece.shift + extra;
        const profileId = id + ("profile" ~ i);
        const piecePlane = plane(sketchPlane.origin + frame.zAxis * piece.start - direction * extra, sketchPlane.normal, direction);
        const regions = rackCutRegions(form, faceHeight, length + 2 * extra, mapArray(centers, center => center + shift), relief);
        const profile = sketchRackCut(context, profileId, piecePlane, regions);
        // Slanted, up to a plane across its far side: exactly its width across, whichever way it slants
        const endId = id + ("end" ~ i);
        opPlane(context, endId, { "plane" : plane(piecePlane.origin + frame.zAxis * piece.width, frame.zAxis) });
        opExtrude(context, id + ("extrude" ~ i), {
                    "entities" : profile,
                    "direction" : normalize(frame.zAxis + direction * piece.slant),
                    "endBound" : BoundingType.UP_TO_SURFACE,
                    "endBoundEntity" : qCreatedBy(endId, EntityType.FACE)
                });
        cleanup(context, id + ("deleteSketch" ~ i), qUnion([qCreatedBy(profileId, EntityType.BODY), qCreatedBy(endId, EntityType.BODY)]));
        tools = append(tools, qCreatedBy(id + ("extrude" ~ i), EntityType.BODY));
    }
    const part = qOwnerBody(face);
    opBoolean(context, id + "cut", {
                "targets" : part,
                "tools" : qUnion(tools),
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    if (offsetProfile(definition))
    {
        // Out of the part, through the root of the gap after the first tooth
        const gapCenter = centers[0] + PI * form.module / 2;
        const below = planeToWorld(sketchPlane, vector(gapCenter, rackRoot(form) - faceHeight - form.module));
        addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(below + frame.zAxis * width / 2, facePlane.normal),
            qIntersection([qOwnedByBody(part, EntityType.FACE), qCreatedBy(id, EntityType.FACE)]), definition.profileOffsetOppositeDirection);
    }
}

/**
 * A rack's cut, in pieces across its width (`width`): straight, one piece; helical, one, its teeth slanting across it at
 * the helix angle; or double helical, two halves slanting opposite ways. Each piece starts `start` across the width,
 * and is `width` wide; its teeth move `slant` along the rack per unit across, and are `shift` along from where they are
 * at the middle of the width. A rack's hand is the hand of a gear it stands in for: like gears, it meshes with a gear of
 * the opposite hand: a left hand rack's teeth move along it (x, see `cutRack`) as they go across its width (z), and a
 * right hand rack's back.
 *
 * @returns {array} : Of maps of `start`, `width`, `slant`, and `shift`.
 */
export function rackPieces(definition is map, width is ValueWithUnits) returns array
{
    if (!definition.helical)
    {
        return [{ "start" : 0 * meter, "width" : width, "slant" : 0, "shift" : 0 * meter }];
    }
    const slant = tan(definition.helixAngle) * (definition.hand == GearHand.LEFT ? 1 : -1);
    if (!definition.doubleHelical)
    {
        return [{ "start" : 0 * meter, "width" : width, "slant" : slant, "shift" : -slant * width / 2 }];
    }
    // Back to where they are at the middle, then away again
    return [
            { "start" : 0 * meter, "width" : width / 2, "slant" : slant, "shift" : -slant * width / 2 },
            { "start" : width / 2, "width" : width / 2, "slant" : -slant, "shift" : 0 * meter }
        ];
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
    const shape = definition.gearType == GearType.SECTOR ? "Sector Gear" : definition.gearType == GearType.BEVEL ? "Bevel Gear" : "Gear";
    const kind = !(isSpur(definition) && definition.helical) ? shape : (definition.doubleHelical ? "Herringbone " : "Helical ") ~ shape;
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
    definition = angleOffsetManipulatorChange(definition, newManipulators);
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return startOffsetManipulatorChange(definition, newManipulators);
}
