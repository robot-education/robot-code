FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/coreUtils.fs", version : "");
import(path : "core/unitSystemDisplay.fs", version : "");
export import(path : "core/unitSystem.fs", version : "");
export import(path : "core/startOffset.fs", version : "");
import(path : "core/location.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "core/profileOffset.fs", version : "");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "core/bore.fs", version : "");
import(path : "gear/gearCommon.fs", version : "");

export enum GearType
{
    annotation { "Name" : "Gear" }
    GEAR,
    annotation { "Name" : "Rack" }
    RACK
}

const DIAMETRAL_PITCH_BOUNDS = { (unitless) : [1, 20, 200] } as RealBoundSpec;
const MODULE_BOUNDS = { (meter) : [1e-5, 0.001, 0.1], (millimeter) : 1, (inch) : 0.04 } as LengthBoundSpec;
const GEAR_TEETH_BOUNDS = { (unitless) : [4, 24, 1000] } as IntegerBoundSpec;
const RACK_TEETH_BOUNDS = { (unitless) : [1, 20, 10000] } as IntegerBoundSpec;
const SECTOR_TEETH_BOUNDS = { (unitless) : [1, 6, 1000] } as IntegerBoundSpec;
const PRESSURE_ANGLE_BOUNDS = { (degree) : [10, 20, 35] } as AngleBoundSpec;
const THICKNESS_BOUNDS = { (meter) : [1e-5, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;
const RACK_HEIGHT_BOUNDS = { (meter) : [1e-5, 0.00635, 500], (inch) : 0.25, (millimeter) : 6 } as LengthBoundSpec;
// A 1/8 in. router bit's
const BIT_DIAMETER_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;

predicate isGear(definition is map)
{
    definition.gearType == GearType.GEAR;
}

/**
 * Makes involute spur gears (or sectors of them) and racks, sized by diametral pitch (in inches) or module (metric),
 * with optional root rounds a router bit can cut.
 */
annotation {
        "Feature Type Name" : "Robot gear",
        "Feature Type Description" : "Create involute spur gears, sector gears, and racks, by diametral pitch or module, with roots a router bit can cut." ~ CREDIT,
        "Manipulator Change Function" : "robotGearManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotGear = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Gear type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.gearType is GearType;

        locationPredicate(definition, "gear");

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
            else
            {
                annotation { "Name" : "Rack teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.rackTeeth, RACK_TEETH_BOUNDS);
            }

            annotation { "Name" : "Pressure angle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "20 degrees for most gears; some older ones are 14.5." }
            isAngle(definition.pressureAngle, PRESSURE_ANGLE_BOUNDS);

            annotation { "Name" : "Thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.thickness, THICKNESS_BOUNDS);

            if (!isGear(definition))
            {
                annotation { "Name" : "Height", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "From its pitch line to its back." }
                isLength(definition.rackHeight, RACK_HEIGHT_BOUNDS);
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

        startOffsetPredicate(definition);

        if (isGear(definition))
        {
            borePredicate(definition);
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            profileOffsetPredicate(definition);

            annotation { "Name" : "Add mate connector", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addMateConnectors is boolean;
        }
    }
    {
        const gearPlane = applyStartOffset(context, definition, getLocationPlane(context, definition));
        addStartOffsetManipulator(context, id, definition, gearPlane);

        const module = gearModule(definition);
        const fillet = definition.routerRelief ? definition.bitDiameter / 2 : standardFilletRadius(module);
        const form = getGearForm(module, isGear(definition) ? definition.teeth : undefined, definition.pressureAngle, fillet, getProfileOffset(definition));
        var profile;
        if (isGear(definition))
        {
            if (definition.sector && definition.sectorTeeth >= definition.teeth)
            {
                throw regenError("A sector has fewer teeth than its gear.", ["sectorTeeth"]);
            }
            profile = sketchGearProfile(context, id + "profile", gearPlane, form, definition.sector ? definition.sectorTeeth : undefined);
        }
        else
        {
            profile = sketchRackProfile(context, id + "profile", gearPlane, form, definition.rackTeeth, definition.rackHeight);
        }
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
            const axis = isGear(definition) ? line(gearPlane.origin, gearPlane.x) : line(planeToWorld(gearPlane, vector(PI / 2 * module, 0 * meter)), gearPlane->yAxis());
            addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, axis, qSubtraction(faces, qParallelPlanes(faces, gearPlane)),
                definition.profileOffsetOppositeDirection);
        }

        if (definition.addMateConnectors)
        {
            opMateConnector(context, id + "mateConnector", {
                        "coordSystem" : coordSystem(gearPlane),
                        "owner" : gear
                    });
        }

        if (isGear(definition))
        {
            const bore = definitionBore(definition);
            if (bore != undefined)
            {
                cutBores(context, id, id + "bore", bore, [gearPlane], [undefined], [gear]);
            }
            // Its pitch diameter, for spacing gears: two mesh their pitch radii apart
            reportFeatureInfo(context, id, "Its pitch diameter is " ~ makeValueString(definition.unitSystem, 2 * form.pitchRadius, true) ~ ".");
        }
    });

/**
 * The module: one inch over the diametral pitch, in inches.
 */
function gearModule(definition is map) returns ValueWithUnits
{
    return isImperial(definition) ? 1 * inch / definition.diametralPitch : definition.module;
}

function setGearProperties(context is Context, gear is Query, definition is map)
{
    // 24T 20DP Gear, 30T Module 1 Gear, or 20DP Rack
    const size = isImperial(definition) ? definition.diametralPitch ~ "DP" : "Module " ~ roundToPrecision(definition.module / millimeter, 3);
    setProperty(context, {
                "entities" : gear,
                "propertyType" : PropertyType.NAME,
                "value" : isGear(definition) ? definition.teeth ~ "T " ~ size ~ " Gear" : size ~ " Rack"
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
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return startOffsetManipulatorChange(definition, newManipulators);
}
