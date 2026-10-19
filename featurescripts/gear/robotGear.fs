FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "ea127c07807644fb48d3a1ae", version : "71504a904ad4d8a579c5b4da");
export import(path : "948c83c1b1ac83de4ccf921b", version : "5bd77bd9ef3230343bfbc217");
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
import(path : "53b4724e5ca49bd7fbc09e3e", version : "93bea9e54d4be1451a85af1e");
import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
import(path : "0103ad63394d7713fbf44448", version : "5f7d67b62160df309da69ec3");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "01f0c5634015659514b83da1", version : "2e33b7669ec63ae45dfedbfc");
import(path : "f9a7532fe0d68211bd3a8112", version : "88cdac249919d6e3b37ebf69");
import(path : "58d66340f7b70cfc86606676", version : "da525a155390a101429f7f9a");
// Where the gear is on its location: its bottom, middle, top, or a bevel gear's apex
import(path : "554542fc345271814c4463b0", version : "5b47f8330c585dfb754b8a98");

export enum GearType
{
    annotation { "Name" : "Gear" }
    GEAR,
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
 * How a sector's size is given: by its teeth, or the angle it spans (whole teeth, as many as fit).
 */
export enum SectorSize
{
    annotation { "Name" : "Teeth" }
    TEETH,
    annotation { "Name" : "Angle" }
    ANGLE
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

/**
 * How the roots are rounded, as Onshape's Spur gear's Root fillet: a full round (tangent to the root and both flanks),
 * a fillet whose radius is a third or a quarter of the gap's width there (two thirds or half the full round's), none,
 * or a router bit's radius, so it can cut them.
 */
export enum RootFillet
{
    annotation { "Name" : "Full" }
    FULL,
    annotation { "Name" : "1/3" }
    THIRD,
    annotation { "Name" : "1/4" }
    QUARTER,
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "Router bit" }
    ROUTER_BIT
}

/**
 * Each root fillet's radius, as a fraction of the full round's (see `getGearForm`).
 */
const ROOT_FILLET_FRACTIONS = {
        (RootFillet.FULL) : 1,
        (RootFillet.THIRD) : 2 / 3,
        (RootFillet.QUARTER) : 1 / 2,
        (RootFillet.NONE) : 0
    };

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
 * A sector of a spur gear: a wedge of it, from its center.
 */
predicate isSector(definition is map)
{
    definition.gearType == GearType.GEAR;
    definition.sector;
}

/**
 * A spur gear (or a sector of one).
 */
predicate isSpur(definition is map)
{
    definition.gearType == GearType.GEAR;
}

/**
 * Makes involute spur, helical, and herringbone gears (or sectors of them) and straight bevel gears, with fully rounded
 * roots (or Onshape's Spur gear's other root fillets, or a router bit's), or cuts rack teeth into a part's flat face (or
 * a gear's teeth around its round face); at robot gears' standard pitches, or by diametral pitch (in inches) or module
 * (metric).
 */
annotation {
        "Feature Type Name" : "Robot gear",
        "Feature Type Description" : "Create spur, helical, herringbone, and bevel gears, or cut racks and gear teeth into parts, at FRC and FTC gears' standard pitches." ~ CREDIT,
        "Manipulator Change Function" : "robotGearManipulatorChange",
        "Editing Logic Function" : "robotGearEditLogic",
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
                            annotation { "Name" : "Shaft angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                                        "Description" : "The angle the gears turn the drive through: between its shaft and its mate's (90 degrees for a right angle)." }
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
            annotation { "Name" : "Face to cut", "Filter" : EntityType.FACE && (GeometryType.PLANE || GeometryType.CYLINDER) && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES,
                        "MaxNumberOfPicks" : 1,
                        "Description" : "A flat face, for a rack: its teeth run along its longest edge, through its width. Or a round one, for a gear's teeth around it." }
            definition.rackFace is Query;

            annotation { "Name" : "Tangent propagation", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "Also cut the faces in line with it (or on the same cylinder), across tangent edges." }
            definition.tangentPropagation is boolean;

            annotation { "Name" : "Face at", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "The teeth's pitch line (the face is moved out to their tips first), or their tips." }
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

            if (isGear(definition))
            {
                annotation { "Name" : "Thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                            "Description" : "Along its axis, from its bottom to its top. A bevel gear's teeth run from its back as far toward its apex as it reaches." }
                isLength(definition.thickness, THICKNESS_BOUNDS);
            }

            annotation { "Name" : "Root fillet", "Default" : RootFillet.FULL, "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "A full round, a third or a quarter of the gap's width, none, or a router bit's radius, so it can cut them (a full round where the gap's too narrow for both corners)." }
            definition.rootFillet is RootFillet;

            if (definition.rootFillet == RootFillet.ROUTER_BIT)
            {
                annotation { "Name" : "Bit diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.bitDiameter, BIT_DIAMETER_BOUNDS);
            }
        }

        if (definition.gearType == GearType.GEAR)
        {
            annotation { "Name" : "Sector", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Make a wedge of the gear, from its center." }
            definition.sector is boolean;

            if (definition.sector)
            {
                annotation { "Group Name" : "Sector", "Collapsed By Default" : false, "Driving Parameter" : "sector" }
                {
                    annotation { "Name" : "Sector size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.sectorSize is SectorSize;

                    annotation { "Name" : "Angle reference", "Filter" : QueryFilterCompound.ALLOWS_VERTEX || GeometryType.LINE || GeometryType.CIRCLE || GeometryType.ARC,
                                "MaxNumberOfPicks" : 1, "Description" : "Where the sector's middle points (by teeth), or its angles are measured from." }
                    definition.sectorReference is Query;

                    if (definition.sectorSize == SectorSize.TEETH)
                    {
                        annotation { "Name" : "Sector teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The sector's: a wedge of the gear's, from its center." }
                        isInteger(definition.sectorTeeth, SECTOR_TEETH_BOUNDS);
                    }
                    else
                    {
                        // As std's revolve: from the angle reference, as many whole teeth as fit, centered
                        revolveAnglePredicate(definition);
                    }

                }
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

        // Where the gear is on its location (see `gearPointDistances`): its bottom, by default
        pointManipulatorPredicate(definition);
    }
    {
        const module = gearModule(definition);
        if (!isGear(definition))
        {
            cutTeeth(context, id, definition, module);
            return;
        }

        const size = gearSize(context, definition, module);
        const locationPlane = applyStartOffset(context, definition, size.plane);
        addStartOffsetManipulator(context, id, definition, locationPlane);
        if (definition.gearType == GearType.BEVEL)
        {
            // Its teeth reach as far toward its apex as its thickness does, and its mate's as far
            definition.faceWidth = bevelShape(definition, module, size.teeth).faceWidth;
        }
        // On the location at its bottom, middle, top, or apex (its plane is its middle, or a bevel gear's back)
        const distances = gearPointDistances(definition, module, size.teeth);
        var gearPlane = applyPointManipulator(definition, locationPlane, distances);
        addPointManipulator(context, id, definition, mapArray(distances, distance => gearPlane.origin + gearPlane.normal * distance));
        if (isSector(definition))
        {
            const sector = sectorPlacement(context, definition, gearPlane, size.teeth);
            gearPlane = sector.plane;
            definition.sectorTeeth = sector.teeth;
        }

        const gear = buildGear(context, id, id + "gear", definition, gearPlane, module, size.teeth);
        if (offsetProfile(definition) && isSpur(definition))
        {
            const faces = qOwnedByBody(gear.body, EntityType.FACE);
            addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(gearPlane.origin, gearPlane.x), qSubtraction(faces, qParallelPlanes(faces, gearPlane)),
                definition.profileOffsetOppositeDirection);
        }

        var gears = [gear];
        var warning = size.warning == undefined ? undefined : { "message" : size.warning, "parameters" : ["pitchCircle"] };
        // A spur gear's pitch diameter, for spacing gears: two mesh their pitch radii apart
        var info = isSpur(definition) ? "Its pitch diameter is " ~ makeValueString(definition.unitSystem, module * size.teeth, true) ~ "." : undefined;
        if (isPair(definition))
        {
            const place = matePlacement(context, id, definition, gearPlane, module, size.teeth);
            const mate = mateDefinition(definition, size.teeth);
            gears = append(gears, buildGear(context, id, id + "mate", mate, place.plane, module, place.teeth));
            if (info != undefined)
            {
                info = info ~ " The gears' centers are " ~ makeValueString(definition.unitSystem, norm(place.plane.origin - gearPlane.origin), true) ~ " apart.";
            }
            if (warning == undefined && place.warning != undefined)
            {
                warning = { "message" : place.warning, "parameters" : ["matePitchCircle"] };
            }
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
                if (warning == undefined && boreReach(bore) >= built.hubRadius)
                {
                    warning = {
                            "message" : "The bore cuts into the " ~ (i == 0 ? "gear's" : "mate's") ~ " teeth: use a smaller bore, or more teeth" ~
                                (definition.gearType == GearType.BEVEL ? ", or a thinner gear." : "."),
                            "parameters" : ["hexWidth", "boreDiameter", "splineType", "teeth", "thickness"]
                        };
                }
            }
        }
        if (warning != undefined)
        {
            reportFeatureWarning(context, id, warning.message, warning.parameters);
        }
        else if (info != undefined)
        {
            reportFeatureInfo(context, id, info);
        }
    });

/**
 * How far out from its axis a bore cuts: a hex's corners, or a round or spline bore's radius, and its entrance chamfer.
 */
function boreReach(bore is Bore) returns ValueWithUnits
{
    const radius = (bore.across + bore.clearance) / 2;
    return (bore.shape == BoreShape.HEX ? radius / cos(30 * degree) : radius) + (bore.chamfer ?? 0 * meter);
}

/**
 * Where the point manipulator's points are, along the gear's axis from its plane (a spur gear's middle, or a bevel
 * gear's back pitch circle): its bottom (first, so the default), middle, and top, and a bevel gear's apex. The gear's
 * plane is the location's, less the chosen point's (see `applyPointManipulator`, which takes them as a `Vector`).
 */
export function gearPointDistances(definition is map, module is ValueWithUnits, teeth is number) returns Vector
{
    if (definition.gearType != GearType.BEVEL)
    {
        return vector([-definition.thickness / 2, 0 * meter, definition.thickness / 2]);
    }
    const shape = bevelShape(definition, module, teeth);
    return vector([shape.bottom, (shape.bottom + shape.top) / 2, shape.top, shape.apex]);
}

/**
 * The roots' fillet, for `getGearForm`: a fraction of the full round's radius, or a router bit's radius.
 */
function rootFilletSize(definition is map)
{
    return definition.rootFillet == RootFillet.ROUTER_BIT ? definition.bitDiameter / 2 : ROOT_FILLET_FRACTIONS[definition.rootFillet];
}

/**
 * Where a sector's teeth go: its plane, turned so its x axis is the sector's middle (its teeth are centered on it), and
 * how many it has. By Sector teeth, its middle points toward the angle reference; by Angle, it spans the angles from the
 * angle reference (`revolveAngles`), with as many whole teeth as fit, centered in it. Without an angle reference, it's
 * measured from the gear's x axis.
 *
 * @returns {{
 *      @field plane {Plane} :
 *      @field teeth {number} :
 * }}
 */
function sectorPlacement(context is Context, definition is map, gearPlane is Plane, teeth is number) returns map
{
    const reference = applyAngleReference(context, definition, gearPlane, definition.sizeBy == GearSize.TEETH ? "location" : "pitchCircle", "sectorReference");
    if (definition.sectorSize == SectorSize.TEETH)
    {
        return { "plane" : reference, "teeth" : definition.sectorTeeth };
    }
    const angles = revolveAngles(definition);
    const span = sectorSpan(angles, teeth);
    if (span.teeth < 1)
    {
        throw regenError("The sector's angle is too small for a whole tooth.", ["angle", "angleBack"]);
    }
    return { "plane" : rotationAround(line(reference.origin, reference.normal), span.middle) * reference, "teeth" : span.teeth };
}

/**
 * A sector spanning `angles` (see `revolveAngles`) of a gear with `teeth`: as many whole teeth as fit (a tooth for every
 * `360° / teeth`, within std's angle tolerance), and its middle, where they're centered.
 */
export function sectorSpan(angles is map, teeth is number) returns map
{
    const pitch = 360 * degree / teeth;
    return {
            "teeth" : floor((angles.end - angles.start) / pitch + TOLERANCE.zeroAngle),
            "middle" : (angles.start + angles.end) / 2
        };
}

/**
 * A gear (a spur, helical, or herringbone gear, a sector, or a bevel gear) with `teeth`, centered on `gearPlane` (a
 * bevel gear's back), its first tooth on its x axis, and its properties set. `featureId` is the feature's, for statuses.
 *
 * @returns {{
 *      @field body {Query} :
 *      @field plane {Plane} : `gearPlane`, for its bore.
 *      @field connector {CoordSystem} : Where its mate connector goes: its center, or a bevel gear's apex.
 *      @field hubRadius {ValueWithUnits} : How far out it's solid all the way through, for its bore: its root, or a
 *              bevel gear's at its top.
 * }}
 */
function buildGear(context is Context, featureId is Id, id is Id, definition is map, gearPlane is Plane, module is ValueWithUnits, teeth is number) returns map
{
    var result = { "plane" : gearPlane, "connector" : coordSystem(gearPlane) };
    if (definition.gearType == GearType.BEVEL)
    {
        const bevel = buildBevelGear(context, featureId, id, definition, gearPlane, module, teeth);
        result.body = bevel.body;
        result.hubRadius = bevel.hubRadius;
        // Mated at their apexes, with their axes the shaft angle apart, a pair meshes
        result.connector = coordSystem(bevel.apex, gearPlane.x, gearPlane.normal);
    }
    else
    {
        const form = getGearForm(module, teeth, pressureAngle(definition), rootFilletSize(definition), getProfileOffset(definition));
        result.hubRadius = form.rootRadius;
        const sector = isSector(definition);
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
    mate.sector = false;
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
        "toothPhase" : isSector(definition) && definition.sectorTeeth % 2 == 0 ? 180 * degree / teeth : 0 * degree
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
 * A bevel gear's shape along its axis, from its back's pitch circle toward its apex: its cones (see `bevelPairCones`),
 * its `bottom` (its flat back, under its teeth's back ends), its `top` (flat, square to its axis, where its teeth stop
 * toward its apex), its `apex`, and its `faceWidth`, how far its teeth run along its pitch cone from its back to its
 * top. A gear's face width is from its thickness; its mate's is the gear's (`definition.faceWidth`, see `mateDefinition`),
 * so their teeth meet end to end. Errors if the gear (or its mate) is too thin for its teeth to reach its top, or so
 * thick its top reaches its apex.
 *
 * @returns {{
 *      @field cones {map} : Its and its mate's, as `bevelPairCones` gives them.
 *      @field bottom {ValueWithUnits} :
 *      @field top {ValueWithUnits} :
 *      @field apex {ValueWithUnits} :
 *      @field faceWidth {ValueWithUnits} :
 * }}
 */
export function bevelShape(definition is map, module is ValueWithUnits, teeth is number) returns map
{
    const cones = bevelPairCones(module, teeth, definition.mateTeeth, definition.shaftAngle);
    const pitchAngle = cones.gear.pitchAngle;
    const offset = getProfileOffset(definition);
    // Its teeth's back ends' tips and roots, along the back cone
    const bottom = -(1.25 * module - offset) * sin(pitchAngle);
    const tipHeight = function(angle is ValueWithUnits) returns ValueWithUnits
        {
            return (module + offset) * sin(angle);
        };
    const apex = cones.gear.coneDistance * cos(pitchAngle);
    const thicknessString = function(thickness is ValueWithUnits) returns string
        {
            return makeValueString(definition.unitSystem, thickness, true);
        };
    var faceWidth = definition.faceWidth;
    if (faceWidth == undefined)
    {
        const top = bottom + definition.thickness;
        if (top <= tipHeight(pitchAngle) + TOLERANCE.zeroLength * meter)
        {
            throw regenError("The gear's too thin for its teeth: make it thicker than " ~ thicknessString(tipHeight(pitchAngle) - bottom) ~ ".", ["thickness"]);
        }
        if (top >= apex - TOLERANCE.zeroLength * meter)
        {
            throw regenError("The gear's too thick: its top would reach its apex. Make it thinner than " ~ thicknessString(apex - bottom) ~ ".", ["thickness"]);
        }
        faceWidth = top / cos(pitchAngle);
        // Its mate's teeth run as far along its pitch cone
        const matePitchAngle = cones.mate.pitchAngle;
        if (faceWidth * cos(matePitchAngle) <= tipHeight(matePitchAngle) + TOLERANCE.zeroLength * meter)
        {
            const least = tipHeight(matePitchAngle) / cos(matePitchAngle) * cos(pitchAngle) - bottom;
            throw regenError("The gear's too thin for its mate's teeth: make it thicker than " ~ thicknessString(least) ~ ".", ["thickness"]);
        }
    }
    return {
            "cones" : cones,
            "bottom" : bottom,
            "top" : faceWidth * cos(pitchAngle),
            "apex" : apex,
            "faceWidth" : faceWidth
        };
}

/**
 * A straight bevel gear (Tredgold's approximation, see `bevelCones`), with its pitch circle on `gearPlane` (its back)
 * and its apex in front of it, along its normal, as `bevelShape` shapes it. In steps:
 *
 * 1. Its blank: revolved from its section through its axis: its flat bottom, out to its back cone, up that to its tip
 *    cone, along that toward the apex to its flat top, and back to its axis.
 * 2. A tooth gap: its back cone's virtual spur gear's gap (`sketchToothGap`), on the plane square to its pitch cone at
 *    its first tooth, lofted to the same gap scaled toward the apex. Scaled about the apex, its flanks run straight
 *    to it. It runs a tooth's height past the blank at both ends (past where its root cone reaches the top, or half
 *    way from there to the apex, if that's nearer), so it cuts through it.
 * 3. The gap, patterned around the axis once per tooth, and cut from the blank.
 *
 * @returns {{
 *      @field body {Query} :
 *      @field apex {Vector} :
 *      @field hubRadius {ValueWithUnits} : Where its root cone reaches its top: inside it, it's solid all the way through.
 * }}
 */
function buildBevelGear(context is Context, featureId is Id, id is Id, definition is map, gearPlane is Plane, module is ValueWithUnits, teeth is number) returns map
{
    if (definition.rootFillet == RootFillet.ROUTER_BIT)
    {
        throw regenError("A router can't cut a bevel gear's teeth: choose another root fillet.", ["rootFillet"]);
    }
    const shape = bevelShape(definition, module, teeth);
    const cones = shape.cones.gear;
    const offset = getProfileOffset(definition);
    const fillet = rootFilletSize(definition);
    const form = getGearForm(module, cones.virtualTeeth, pressureAngle(definition), fillet, offset);

    const axis = gearPlane.normal;
    const x = gearPlane.x;
    const apex = gearPlane.origin + axis * shape.apex;
    // Along the back cone, out from the virtual spur gear's center to the first tooth's pitch point
    const radial = x * cos(cones.pitchAngle) + axis * sin(cones.pitchAngle);
    const pitchPoint = gearPlane.origin + x * cones.pitchRadius;
    const virtualCenter = pitchPoint - radial * form.pitchRadius;
    const scaled = function(point is Vector, scale is number) returns Vector
        {
            return apex + (point - apex) * scale;
        };
    // How far a point is scaled toward the apex to reach the top
    const topScale = function(point is Vector) returns number
        {
            return (shape.apex - shape.top) / (shape.apex - dot(point - gearPlane.origin, axis));
        };

    // 1. The blank, revolved from its section: across its bottom, up its back and tip cones to its top, and to its axis
    const tipBack = pitchPoint + radial * (form.tipRadius - form.pitchRadius);
    const rootBack = pitchPoint + radial * (form.rootRadius - form.pitchRadius);
    const sectionPlane = plane(gearPlane.origin, cross(x, axis), x);
    const section = mapArray([rootBack, tipBack, scaled(tipBack, topScale(tipBack))], point => worldToPlane(sectionPlane, point));
    const outline = [section[0], section[1], section[2], vector(0 * meter, section[2][1]), vector(0 * meter, section[0][1])];
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
    const rootTop = topScale(rootBack);
    const scales = [1 + height / cones.coneDistance, max(rootTop - height / cones.coneDistance, rootTop / 2)];
    var gapFaces = [];
    for (var i, scale in scales)
    {
        const gapPlane = plane(scaled(virtualCenter, scale), cross(radial, gearPlane->yAxis()), radial);
        const scaledForm = getGearForm(module * scale, cones.virtualTeeth, pressureAngle(definition), fillet, offset * scale);
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
        throw regenError("Failed to create the bevel gear's teeth.", ["teeth", "mateTeeth", "thickness"], qUnion(gapFaces));
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
        throw regenError("Failed to cut the bevel gear's teeth.", ["teeth", "mateTeeth", "thickness"], gaps);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return { "body" : blank, "apex" : apex, "hubRadius" : dot(rootBack - gearPlane.origin, x) * rootTop };
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
 * Editing logic: choosing a circle for the location that's a pitch circle at this pitch (a whole number of teeth's)
 * switches Selection to Pitch circle, with it; likewise a circle for Gear pair's angle reference switches Mate selection
 * to Pitch circle.
 */
export function robotGearEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (!isGear(definition))
    {
        return definition;
    }
    const module = gearModule(definition);
    if (definition.sizeBy == GearSize.TEETH && oldDefinition.location != definition.location && isPitchCircle(context, definition.location, module))
    {
        definition.sizeBy = GearSize.PITCH_CIRCLE;
        definition.pitchCircle = definition.location;
        definition.location = qNothing();
    }
    if (definition.gearType != GearType.BEVEL && definition.gearPair && definition.mateSizeBy == GearSize.TEETH &&
        oldDefinition.angleReference != definition.angleReference && isPitchCircle(context, definition.angleReference, module))
    {
        definition.mateSizeBy = GearSize.PITCH_CIRCLE;
        definition.matePitchCircle = definition.angleReference;
        definition.angleReference = qNothing();
    }
    return definition;
}

/**
 * Whether `selection` is a circle whose radius is a gear's pitch radius at `module` (see `isWholeTeeth`).
 */
function isPitchCircle(context is Context, selection is Query, module is ValueWithUnits) returns boolean
{
    const circle = selection->qNthElement(0)->qGeometry(GeometryType.CIRCLE);
    return !isQueryEmpty(context, circle) && isWholeTeeth(evCurveDefinition(context, { "edge" : circle }).radius, module);
}

/**
 * Whether a pitch circle of `radius` is a gear's at `module`: a whole number of teeth's (within std's length tolerance),
 * at least a gear's least.
 */
export function isWholeTeeth(radius is ValueWithUnits, module is ValueWithUnits) returns boolean
{
    const teeth = nearestGearTeeth(radius, module);
    return teeth >= 4 && tolerantEquals(teeth * module / 2, radius);
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
 * Cuts teeth into the part around Face to cut: a rack along a flat face (`cutRack`), or a gear's teeth around a round
 * one (`cutRoundTeeth`).
 */
function cutTeeth(context is Context, id is Id, definition is map, module is ValueWithUnits)
{
    const face = verifyNonemptyQuery(context, definition, "rackFace", "Select a face to cut the teeth into.")[0];
    if (isQueryEmpty(context, face->qGeometry(GeometryType.CYLINDER)))
    {
        cutRack(context, id, definition, face, getGearForm(module, undefined, pressureAngle(definition), rootFilletSize(definition), getProfileOffset(definition)));
        return;
    }
    const warning = cutRoundTeeth(context, id, definition, face, module);
    if (warning != undefined)
    {
        reportFeatureWarning(context, id, warning, ["rackFace"]);
    }
}

/**
 * Moves `faces` (the part's) out `distance`, as Move face's Offset does: a face at the teeth's pitch line is moved out
 * to their tips before they're cut, so they're whole.
 */
function moveFacesOut(context is Context, featureId is Id, id is Id, faces is Query, distance is ValueWithUnits)
{
    try silent
    {
        opOffsetFace(context, id, { "moveFaces" : faces, "offsetDistance" : distance });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id, "propagateErrorDisplay" : true });
        throw regenError("Failed to move the face out to the teeth's tips.", ["rackFace", "rackFaceAt"], faces);
    }
}

/**
 * Cuts rack teeth into the part along Face to cut (and with Tangent propagation, the flat faces in line with it, across
 * tangent edges): along its longest straight edge, from end to end, through its width. At the teeth's pitch line
 * (`rackFaceAt`), the faces are first moved out a module, to their tips. Whole teeth fill it, a pitch apart, lined up
 * with an end (a gap centered on it) or the angle reference (likewise); past the first and last, it's cut to the root.
 */
function cutRack(context is Context, id is Id, definition is map, face is Query, form is map)
{
    var facePlane = evPlane(context, { "face" : face });
    var faces = definition.tangentPropagation ? qCoincidesWithPlane(qTangentConnectedFaces(face), facePlane) : face;
    // Before they're moved: the selection doesn't find moved faces
    const part = qUnion(evaluateQuery(context, qOwnerBody(face)));
    const direction = longestEdgeDirection(context, faces);
    if (direction == undefined)
    {
        throw regenError("The face needs a straight edge for the teeth to run along.", ["rackFace"], face);
    }
    if (definition.rackFaceAt == RackFaceAt.PITCH)
    {
        moveFacesOut(context, id, id + "moveFace", faces, form.module);
        // Moved, the faces beside them stretch to meet them, which can make them longer: measured where they are now
        facePlane.origin += facePlane.normal * form.module;
        faces = qCoincidesWithPlane(qOwnedByBody(part, EntityType.FACE), facePlane);
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
    // At the teeth's tips (moved there, if it was at their pitch line)
    const faceHeight = form.module;
    var tools = [];
    for (var i, piece in rackPieces(definition, width))
    {
        // Sketched where the piece starts across the width, over the face's length and as far past its ends as the
        // teeth slant, so the ends are cut to the root all the way across
        const extra = abs(piece.slant) * width;
        const shift = piece.shift + extra;
        const profileId = id + ("profile" ~ i);
        const piecePlane = plane(sketchPlane.origin + frame.zAxis * piece.start - direction * extra, sketchPlane.normal, direction);
        const regions = rackCutRegions(form, faceHeight, length + 2 * extra, mapArray(centers, center => center + shift));
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
 * Cuts a gear's teeth into the part around Face to cut, a round face (and with Tangent propagation, the faces on the
 * same cylinder, across tangent edges), across its width: a gear with the nearest whole number of teeth to its size (at
 * its pitch circle or tips, `rackFaceAt`), whose faces are first moved to its tips. Whole teeth fill it, a pitch apart,
 * lined up with an end (a gap centered on it; all the way around, with the cylinder's x axis) or the angle reference
 * (likewise); past the first and last, it's cut to the root. The teeth are a gear's (or a sector's, see
 * `buildGearBody`), and what's cut is what's around them: out past their tips and down to their root, across the face's
 * angles. Returns a warning if it isn't a whole number of teeth's size.
 */
function cutRoundTeeth(context is Context, id is Id, definition is map, face is Query, module is ValueWithUnits)
{
    const cylinder = evSurfaceDefinition(context, { "face" : face });
    const frame = cylinder.coordSystem;
    const axis = line(frame.origin, frame.zAxis);
    // Teeth inside a hole would be an internal gear's
    const sample = evFaceTangentPlane(context, { "face" : face, "parameter" : vector(0.5, 0.5) });
    if (dot(sample.normal, sample.origin - project(axis, sample.origin)) < 0)
    {
        throw regenError("Teeth can't be cut inside a hole yet: select a round face on the outside of a part.", ["rackFace"], face);
    }
    // Faces on the same axis, of a radius
    const onCylinder = function(faces is Query, radius is ValueWithUnits) returns array
        {
            return filter(evaluateQuery(context, faces->qGeometry(GeometryType.CYLINDER)), function(other)
                {
                    const surface = evSurfaceDefinition(context, { "face" : other });
                    return tolerantEquals(surface.radius, radius) && parallelVectors(surface.coordSystem.zAxis, frame.zAxis) &&
                        tolerantEquals(project(axis, surface.coordSystem.origin), surface.coordSystem.origin);
                });
        };
    const faces = definition.tangentPropagation ? onCylinder(qUnion([face, qTangentConnectedFaces(face)]), cylinder.radius) : [face];
    // Before they're moved: the selection doesn't find moved faces
    const part = qUnion(evaluateQuery(context, qOwnerBody(face)));

    // Its teeth
    const pitchRadius = definition.rackFaceAt == RackFaceAt.PITCH ? cylinder.radius : cylinder.radius - module;
    const teeth = nearestGearTeeth(pitchRadius, module);
    if (teeth < 4)
    {
        throw regenError("The face is too small for a gear of this pitch.", ["rackFace"], face);
    }
    const form = getGearForm(module, teeth, pressureAngle(definition), rootFilletSize(definition), getProfileOffset(definition));
    var warning;
    if (!tolerantEquals(teeth * module / 2, pitchRadius))
    {
        warning = "The face isn't the size of a gear with a whole number of teeth: it's cut as the nearest, with " ~ teeth ~ " teeth.";
    }

    // Out to the tips. Moved, the faces beside them stretch to meet them, which can widen their angles: they're measured
    // where they are now.
    const tipRadius = module * teeth / 2 + module;
    var tipFaces = faces;
    if (!tolerantEquals(tipRadius, cylinder.radius))
    {
        moveFacesOut(context, id, id + "moveFace", qUnion(faces), tipRadius - cylinder.radius);
        tipFaces = onCylinder(qOwnedByBody(part, EntityType.FACE), tipRadius);
    }
    const allFaces = qUnion(tipFaces);

    // Across its width, and around its angles
    const bounds = evBox3d(context, { "topology" : allFaces, "cSys" : frame, "tight" : true });
    const width = bounds.maxCorner[2] - bounds.minCorner[2];
    const middlePoint = toWorld(frame, vector(0 * meter, 0 * meter, (bounds.minCorner[2] + bounds.maxCorner[2]) / 2));
    const span = roundFacesSpan(context, tipFaces, frame);

    // Where a gap's centered
    const yAxis = cross(frame.zAxis, frame.xAxis);
    const around = function(angle is ValueWithUnits) returns Vector
        {
            return frame.xAxis * cos(angle) + yAxis * sin(angle);
        };
    var gap;
    if (definition.rackAlignment == RackAlignment.REFERENCE)
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
        const local = fromWorld(frame, point);
        if (tolerantEquals(vector(local[0], local[1]), vector(0, 0) * meter))
        {
            throw regenError("The angle reference is on the axis, so it doesn't give an angle.", ["rackReference"], reference);
        }
        gap = atan2(local[1], local[0]);
    }
    else if (span.full)
    {
        gap = 0 * degree;
    }
    else
    {
        gap = definition.oppositeEnd ? span.end : span.start;
        addManipulators(context, id, {
                    (RACK_END_MANIPULATOR) : flipManipulator({
                            "base" : middlePoint + around(gap) * tipRadius,
                            "direction" : around(gap + 90 * degree),
                            "flipped" : definition.oppositeEnd
                        })
                });
    }

    // Whole teeth: each from a gap's middle to the next's, a pitch apart
    const pitch = 360 * degree / teeth;
    var sectorTeeth;
    var middle = gap + pitch / 2;
    if (!span.full)
    {
        const first = ceil((span.start - gap) / pitch - TOLERANCE.zeroAngle);
        const last = floor((span.end - gap) / pitch + TOLERANCE.zeroAngle) - 1;
        if (last < first)
        {
            throw regenError("The face is too short for a whole tooth.", ["rackFace"], allFaces);
        }
        if (last - first + 1 < teeth)
        {
            sectorTeeth = last - first + 1;
            middle = gap + (first + last + 1) / 2 * pitch;
        }
    }

    // The teeth, centered on the face's width, their middle (a tooth, or a sector's middle) on the plane's x axis
    const gearPlane = plane(middlePoint, frame.zAxis, around(middle));
    const profile = sketchGearProfile(context, id + "profile", gearPlane, form, sectorTeeth);
    var gearDefinition = definition;
    gearDefinition.thickness = width;
    const teethBody = buildGearBody(context, id + "teeth", gearDefinition, gearPlane, profile, form);

    // Around them: out past their tips, down to their root, across the face's angles
    const outside = tipRadius + module;
    const ring = newSketchOnPlane(context, id + "ring", { "sketchPlane" : gearPlane });
    if (sectorTeeth == undefined && span.full)
    {
        skCircle(ring, "inside", { "center" : vector(0, 0) * meter, "radius" : form.rootRadius });
        skCircle(ring, "outside", { "center" : vector(0, 0) * meter, "radius" : outside });
    }
    else
    {
        const start = span.full ? -180 * degree : span.start - middle;
        const end = span.full ? 180 * degree : span.end - middle;
        const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
            {
                return vector(cos(angle), sin(angle)) * radius;
            };
        skArc(ring, "inside", { "start" : at(form.rootRadius, start), "mid" : at(form.rootRadius, (start + end) / 2), "end" : at(form.rootRadius, end) });
        skLineSegment(ring, "end", { "start" : at(form.rootRadius, end), "end" : at(outside, end) });
        skArc(ring, "outside", { "start" : at(outside, end), "mid" : at(outside, (start + end) / 2), "end" : at(outside, start) });
        skLineSegment(ring, "start", { "start" : at(outside, start), "end" : at(form.rootRadius, start) });
    }
    skSolve(ring);
    const ringFace = qContainsPoint(qCreatedBy(id + "ring", EntityType.FACE), gearPlane.origin + gearPlane.x * (form.rootRadius + outside) / 2);
    opExtrude(context, id + "ringExtrude", {
                "entities" : ringFace,
                "direction" : gearPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : width / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : width / 2
            });
    const tool = qCreatedBy(id + "ringExtrude", EntityType.BODY);
    opBoolean(context, id + "aroundTeeth", {
                "targets" : tool,
                "tools" : teethBody,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    cleanup(context, id + "deleteSketches", qUnion([qCreatedBy(id + "profile", EntityType.BODY), qCreatedBy(id + "ring", EntityType.BODY)]));
    opBoolean(context, id + "cut", {
                "targets" : part,
                "tools" : tool,
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    if (offsetProfile(definition))
    {
        addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(gearPlane.origin, gearPlane.x),
            qIntersection([qOwnedByBody(part, EntityType.FACE), qCreatedBy(id, EntityType.FACE)]), definition.profileOffsetOppositeDirection);
    }
    return warning;
}

/**
 * The angles round `faces` (on one cylinder, whose coordinate system is `frame`) span about its axis, from its x axis:
 * from `start` to `end`, counterclockwise, or `full`, all the way around. Each face's run from its middle as far as its
 * vertices go either way (a face all the way around has circles for edges), and they're joined where they meet.
 *
 * @returns {{
 *      @field full {boolean} :
 *      @field start {ValueWithUnits} :
 *      @field end {ValueWithUnits} : At least `start`, and less than a turn past it.
 * }}
 */
function roundFacesSpan(context is Context, faces is array, frame is CoordSystem) returns map
{
    const angleOf = function(point is Vector) returns ValueWithUnits
        {
            const local = fromWorld(frame, point);
            return atan2(local[1], local[0]);
        };
    if (!isQueryEmpty(context, qAdjacent(qUnion(faces), AdjacencyType.EDGE, EntityType.EDGE)->qGeometry(GeometryType.CIRCLE)))
    {
        return { "full" : true };
    }
    var spans = [];
    for (var face in faces)
    {
        const middle = angleOf(evFaceTangentPlane(context, { "face" : face, "parameter" : vector(0.5, 0.5) }).origin);
        var low = 0 * degree;
        var high = 0 * degree;
        for (var vertex in evaluateQuery(context, qAdjacent(face, AdjacencyType.VERTEX, EntityType.VERTEX)))
        {
            const delta = wrapAngle(angleOf(evVertexPoint(context, { "vertex" : vertex })) - middle);
            low = min(low, delta);
            high = max(high, delta);
        }
        spans = append(spans, { "start" : middle + low, "end" : middle + high });
    }
    return joinSpans(spans);
}

/**
 * `angle`, a whole number of turns away, from -180° (exclusive) to 180°.
 */
function wrapAngle(angle is ValueWithUnits) returns ValueWithUnits
{
    return angle - ceil((angle - 180 * degree) / (360 * degree)) * 360 * degree;
}

/**
 * Angle spans (maps of `start` and `end`) joined into one, starting from the first, where they meet or overlap (a
 * whole number of turns around): `full` if they go all the way around, or else its `start` and `end` (see
 * `roundFacesSpan`).
 */
export function joinSpans(spans is array) returns map
{
    const turn = 360 * degree;
    const tolerance = TOLERANCE.zeroAngle * radian;
    var start = spans[0].start;
    var end = spans[0].end;
    var remaining = subArray(spans, 1);
    var joined = true;
    while (joined && remaining != [])
    {
        joined = false;
        var left = [];
        for (var span in remaining)
        {
            // Its start, from the joined span's, less than a turn after it
            const from = start + (span.start - start) - floor((span.start - start) / turn) * turn;
            const to = from + span.end - span.start;
            if (from <= end + tolerance)
            {
                end = max(end, to);
                joined = true;
            }
            else if (to - turn >= start - tolerance)
            {
                // It starts before the joined span, and runs into it
                start = from - turn;
                end = max(end, to - turn);
                joined = true;
            }
            else
            {
                left = append(left, span);
            }
        }
        remaining = left;
    }
    if (end - start >= turn - tolerance)
    {
        return { "full" : true };
    }
    return { "full" : false, "start" : start, "end" : end };
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
    const shape = isSector(definition) ? "Sector Gear" : definition.gearType == GearType.BEVEL ? "Bevel Gear" : "Gear";
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
    definition = pointManipulatorChange(definition, newManipulators);
    return startOffsetManipulatorChange(definition, newManipulators);
}
