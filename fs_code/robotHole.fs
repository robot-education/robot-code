FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

export import(path : "69e8015ed20821ef00ec816e", version : "bac331a2c5d90f307e77578e");
export import(path : "6fecb15335bf8b26c712a092", version : "ee2c2beeb04d994eebfb9b49");

import(path : "2a1fbdd680ed055fe57e372f", version : "b09d630978b71b04194fc4a8");

PlateWallThickness::import(path : "bd6028cdede1b8e094b97d84", version : "dc2b419920bda08f6cfc197c");

export enum CreationType
{
    annotation { "Name" : "Preset" }
    PRESET,
    annotation { "Name" : "Custom" }
    CUSTOM
}

predicate holeCreationTypePredicate(definition is map)
{
    annotation { "Name" : "Creation type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
    definition.creationType is CreationType;
}

predicate presetSelectionPredicate(definition is map)
{
    if (definition.endStyle != HoleEndStyle.BLIND_IN_LAST && definition.presetTappedOrClearance != undefined)
    {
        annotation { "Name" : "Standard", "Lookup Table" : presetTappedOrClearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "UNCONFIGURABLE"] }
        definition.presetTappedOrClearance is LookupTablePath;
    }
    else
    {
        annotation { "Name" : "Standard", "Lookup Table" : presetBlindInLastHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "UNCONFIGURABLE"] }
        definition.presetBlindInLast is LookupTablePath;
    }
}

export enum PresetType
{
    annotation { "Name" : "Preset" }
    HOLE,
    annotation { "Name" : "Use case" }
    USE_CASE
}

predicate customStartLocationPredicate(definition is map)
{
    annotation { "Name" : "Custom start plane" }
    definition.customStartLocation is boolean;

    if (definition.customStartLocation)
    {
        annotation { "Name" : "Start plane", "Filter" : GeometryType.PLANE, "MaxNumberOfPicks" : 1 }
        definition.startPlane is Query;
    }
}

export enum OuterRadiusType
{
    annotation { "Name" : "Wall thickness" }
    WALL_THICKNESS,
    annotation { "Name" : "Outer diameter" }
    OUTER_DIAMETER
}

export const HOLE_OUTER_DIAMETER =
{
            (meter) : [1e-5, 0.0075, 500],
            (centimeter) : 0.75,
            (millimeter) : 7.5,
            (inch) : 0.375,
            (foot) : 0.03,
            (yard) : 0.01
        } as LengthBoundSpec;

/**
 * A predicate used by [annotateTube] and others.
 */
predicate holeOuterRadiusPredicate(definition is map)
{
    annotation { "Name" : "Wall type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.outerRadiusType is OuterRadiusType;

    if (definition.outerRadiusType == OuterRadiusType.WALL_THICKNESS)
    {
        annotation { "Name" : "Wall thickness", "Icon" : PlateWallThickness::BLOB_DATA, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.wallThickness, BLEND_BOUNDS);
    }
    else if (definition.outerRadiusType == OuterRadiusType.OUTER_DIAMETER)
    {
        annotation { "Name" : "Outer diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.outerDiameter, HOLE_OUTER_DIAMETER);
    }
}

annotation { "Feature Type Name" : "Robot hole",
        "Editing Logic Function" : "robotHoleEditLogic" }
export const robotHole = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        holeCreationTypePredicate(definition);

        annotation { "Group Name" : "Hole", "Collapsed By Default" : false }
        {
            holeTopPredicate(definition);

            if (definition.creationType == CreationType.PRESET)
            {
                presetSelectionPredicate(definition);
            }
            else if (definition.creationType == CreationType.CUSTOM)
            {
                holeSelectionPredicate(definition);

                holeDiameterPredicate(definition);
            }

            cBoreAndCSinkPredicate(definition);

            if (definition.creationType == CreationType.CUSTOM)
            {
                tapDrillDiameterPredicate(definition);
            }

            holeDepthPredicate(definition);
            holeTapDepthPredicate(definition);
        }

        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            customStartLocationPredicate(definition);
            holeStartFromSketchPredicate(definition);

            holeLocationPredicate(definition);

            holeBooleanScopePredicate(definition);
        }

        annotation { "Name" : "Expand plate corners", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.expandPlates is boolean;

        if (definition.expandPlates)
        {
            annotation { "Group Name" : "Expand plate corners", "Collapsed By Default" : false, "Driving Parameter" : "expandPlates", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            {
                holeOuterRadiusPredicate(definition);
            }
        }
    }
    {
        doRobotHole(context, id, definition);
    });

function doRobotHole(context is Context, id is Id, definition is map)
{
    if (definition.expandPlates)
    {
        verifyHoleOuterDiameter(definition);
        expandPlates(context, id, definition);
    }

    const locationId = id + "locations";
    definition.locations = createCustomStartLocations(context, locationId, definition);

    hole(context, id, definition);

    cleanupCustomStartLocations(context, id, definition.customStartLocation, definition.locations);
}


function verifyHoleOuterDiameter(definition is map)
{
    if (definition.outerRadiusType == OuterRadiusType.OUTER_DIAMETER)
    {
        var errorParameter;
        if (definition.style == HoleStyle.SIMPLE && definition.holeDiameter > definition.outerDiameter + TOLERANCE.zeroLength * meter)
        {
            errorParameter = "holeDiameter";
        }
        else if (definition.style == HoleStyle.C_BORE && definition.cBoreDiameter > definition.outerDiameter + TOLERANCE.zeroLength * meter)
        {
            errorParameter = "cBoreDiameter";
        }
        else if (definition.style == HoleStyle.C_SINK && definition.cSinkDiameter > definition.outerDiameter + TOLERANCE.zeroLength * meter)
        {
            errorParameter = "cSinkDiameter";
        }

        if (errorParameter != undefined)
        {
            throw regenError("The outer diameter of the plate must be larger than the diameter of the hole.", [errorParameter, "outerDiameter"]);
        }
    }
}

function getOuterRadius(definition is map) returns ValueWithUnits
{
    if (definition.outerRadiusType == OuterRadiusType.WALL_THICKNESS)
    {
        var radius;
        if (definition.style == HoleStyle.SIMPLE)
        {
            radius = definition.holeDiameter / 2;
        }
        else if (definition.style == HoleStyle.C_BORE)
        {
            radius = definition.cBoreDiameter / 2;
        }
        else if (definition.style == HoleStyle.C_SINK)
        {
            radius = definition.cSinkDiameter / 2;
        }
        return definition.wallThickness + radius;
    }
    else if (definition.outerRadiusType == OuterRadiusType.OUTER_DIAMETER)
    {
        return definition.outerDiameter / 2;
    }
}

/**
 * @param id : @autocomplete `id + "expand"`
 */
function expandPlates(context is Context, id is Id, definition is map)
{
    const locations = evaluateQuery(context, definition.locations);
    const points = mapArray(locations, function(location is Query)
        {
            return evVertexPoint(context, { "vertex" : location });
        });
    const outerRadius = getOuterRadius(definition);

    const plates = qPlateFilter(context, definition.scope);
    for (var i, plate in evaluateQuery(context, plates))
    {
        const plateId = id + "plate" + unstableIdComponent(i);
        setExternalDisambiguation(context, plateId, plate);

        opExpandPlate(context, plateId, {
                    "plate" : plate,
                    "points" : points,
                    "identities" : locations,
                    "radius" : outerRadius
                });
    }
}

function createCustomStartLocations(context is Context, id is Id, definition is map) returns Query
{
    if (!definition.customStartLocation)
    {
        return definition.locations;
    }
    verifyNonemptyQuery(context, definition, "startPlane", "Select a plane to start holes from.");
    const startPlane = evPlane(context, { "face" : definition.startPlane });

    var newLocations = [];
    for (var i, location in evaluateQuery(context, definition.locations))
    {
        const vertex = evVertexPoint(context, { "vertex" : location });

        setExternalDisambiguation(context, id + unstableIdComponent(i), location);
        const sketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : startPlane });
        skPoint(sketch, "point", { "position" : worldToPlane(startPlane, vertex) });
        skSolve(sketch);

        newLocations = append(newLocations, qCreatedBy(id, EntityType.VERTEX));
    }
    return qUnion(newLocations);
}

function cleanupCustomStartLocations(context is Context, id is Id, customStartLocation is boolean, startLocations is Query)
{
    if (customStartLocation)
    {
        opDeleteBodies(context, id + "deleteNewLocations", { "entities" : startLocations });
    }
}

export function robotHoleEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (oldDefinition.locations != definition.locations)
    {
        definition.locations = qUnion(clusterVertexQueries(context, definition.locations));
    }

    if ((definition.creationType == CreationType.PRESET &&
                oldDefinition.presetTappedOrClearance != definition.presetTappedOrClearance ||
                oldDefinition.presetBlindInLast != definition.presetBlindInLast) ||
        (definition.creationType == CreationType.CUSTOM &&
                oldDefinition.standardTappedOrClearance != definition.standardTappedOrClearance ||
                oldDefinition.standardBlindInLast != definition.standardBlindInLast) ||
        oldDefinition.endStyle != definition.endStyle)
    {
        definition = updateHoleDefinition(oldDefinition, definition);
    }

    if (definition.creationType == CreationType.CUSTOM)
    {
        definition = setToCustomIfStandardViolated(definition);
    }

    definition = adjustThreadDepth(oldDefinition, definition);

    if (isCreating)
    {
        definition = holeScopeFlipHeuristicsCall(context, oldDefinition, definition, specifiedParameters, hiddenBodies);
    }

    return definition;
}

// function copyPresetDefinition(oldDefinition is map, definition is map, specifiedParameters is map) returns map
// {
//     if (definition.endStyle != HoleEndStyle.BLIND_IN_LAST)
//     {
//         if (!specifiedParameters.standardTappedOrClearance)
//         {
//             definition.standardTappedOrClearance = lookupTablePath(mergeMaps({ "standard" : "ANSI" }, definition.presetTappedOrClearance));
//         }

//     }
//     else
//     {
//         if (!specifiedParameters.standardBlindInLast)
//         {
//             definition.standardBlindInLast = lookupTablePath(mergeMaps({ "standard" : "ANSI", "type" : "Clearance & tapped" }, definition.presetBlindInLast));
//         }
//     }
//     return definition;
// }

function adjustThreadDepth(oldDefinition is map, definition is map) returns map
{
    if (threadPitchChanged(oldDefinition, definition))
    {
        definition.showTappedDepth = false;
        var pitch = computePitch(definition);
        if (pitch != undefined)
        {
            definition.showTappedDepth = true;

            // if blind hole type and have valid tap clearance value, then calculate and set either tapped or hole depth
            if ((definition.endStyle == HoleEndStyle.BLIND || definition.endStyle == HoleEndStyle.BLIND_IN_LAST) && definition.tapClearance != undefined)
            {
                if (definition.holeDepth != oldDefinition.holeDepth)
                {
                    if (definition.holeDepth != undefined)
                    {
                        definition.tappedDepth = definition.holeDepth - definition.tapClearance * pitch;
                    }
                }
                else
                {
                    if (definition.tappedDepth != undefined)
                    {
                        definition.holeDepth = definition.tappedDepth + definition.tapClearance * pitch;
                    }
                }
            }
        }
    }
    return definition;
}

function setToCustomIfStandardViolated(definition is map) returns map
{
    var table = getStandardTable(definition);
    if (isLookupTableViolated(definition, table, ignoreInvalidateStandardProperties(table)))
    {
        definition.standardTappedOrClearance = lookupTablePath({ "standard" : "Custom" });
        definition.standardBlindInLast = lookupTablePath({ "standard" : "Custom" });
    }
    return definition;
}

function getStandardAndTable(definition is map) returns map
{
    var standard;
    var table;
    if (definition.endStyle == HoleEndStyle.BLIND_IN_LAST)
    {
        if (definition.creationType == CreationType.PRESET)
        {
            standard = definition.presetBlindInLast;
            table = presetBlindInLastHoleTable;
        }
        else
        {
            standard = definition.standardBlindInLast;
            table = blindInLastHoleTable;
        }

    }
    else
    {
        if (definition.creationType == CreationType.PRESET)
        {
            standard = definition.presetTappedOrClearance;
            table = presetTappedOrClearanceHoleTable;
        }
        else
        {
            standard = definition.standardTappedOrClearance;
            table = tappedOrClearanceHoleTable;
        }
    }
    return { "standard" : standard, "table" : table };
}

function getStandardTable(definition is map) returns map
{
    var result = getStandardAndTable(definition);

    if (result.standard == undefined)
    {
        return {};
    }
    result.table = getLookupTable(result.table, result.standard);
    if (result.table == undefined)
    {
        return {};
    }
    return result.table;
}

/**
 * Some standard based sizes do not have standard based cbore and/or csink specifications for that size. If that is
 * the case (indicated by a value of -1 in the cbore or csink diameter of the standard data definition), then don't
 * invalidate the standard, but allow the user to put in his own desired cbore/csink values for that standard size
 *
 * @param table : the current table map for the active size
 * @returns : map of property names, if value is true, then that property's value should not be used to invalidate the standard setting
 */
function ignoreInvalidateStandardProperties(table is map) returns map
{
    var ignoreProperties = {};

    // if there's no standard cbore or csink diameter defined for the current size (indicated by a negative value in the data definition),
    // don't invalidate the standard, keep the standard active and allow user to put in what they want
    if (!shouldPropertyValueInvalidateStandard(table, "cBoreDiameter"))
    {
        ignoreProperties["cBoreDiameter"] = true;
        // if the cbore diameter doesn't invalidate the standard, the depth shouldn't either
        ignoreProperties["cBoreDepth"] = true;
    }
    if (!shouldPropertyValueInvalidateStandard(table, "cSinkDiameter"))
    {
        ignoreProperties["cSinkDiameter"] = true;
        // if the csink diameter doesn't invalidate the standard, the angle shouldn't either
        ignoreProperties["cSinkAngle"] = true;
    }

    return ignoreProperties;
}

/**
 * Determines if the specified property name should be used to invalid the standard based upon the specified data table map
 *
 * @param table : the current table map for the active size
 * @param propertyName : the property name to check
 * @returns : true if the property name should be used to check for standard invalidity, else false
 */
function shouldPropertyValueInvalidateStandard(table is map, propertyName is string) returns boolean
{
    var fieldValue = table[propertyName];
    // default data values less than 0 mean this property value should not be used to check for standard invalidity
    return fieldValue == undefined || lookupTableGetValue(fieldValue) >= 0;
}


function threadPitchChanged(oldDefinition is map, definition is map) returns boolean
{
    return (definition.creationType == CreationType.CUSTOM && oldDefinition.standardBlindInLast != definition.standardBlindInLast ||
                oldDefinition.standardTappedOrClearance != definition.standardTappedOrClearance) ||
        (definition.creationType == CreationType.PRESET && oldDefinition.presetBlindInLast != definition.presetBlindInLast ||
                oldDefinition.presetTappedOrClearance != definition.presetTappedOrClearance) ||
        oldDefinition.endStyle != definition.endStyle ||
        oldDefinition.holeDepth != definition.holeDepth ||
        oldDefinition.tappedDepth != definition.tappedDepth ||
        oldDefinition.tapClearance != definition.tapClearance;
}

/**
 * Updated to work with presets
 */
function computePitch(definition is map)
{
    const standard = getStandardAndTable(definition).standard;

    if (standard != undefined && standard.pitch != undefined)
    {
        // Check for NN.N tpi or NN.N mm
        var result = match(standard.pitch, "([0123456789.]*)\\s*(tpi|mm)");
        if (result.hasMatch)
        {
            if (result.captures[2] == "tpi")
            {
                return 1.0 / stringToNumber(result.captures[1]) * inch;
            }
            else if (result.captures[2] == "mm")
            {
                return stringToNumber(result.captures[1]) * millimeter;
            }
        }
    }

    return undefined;
}

/**
 * Implements standard hole sizes. Set the appropriate standard string for the hole type
 * in the same format as the UI specification and it will set the appropriate values,
 * and then return an updated definition.
 */
function updateHoleDefinition(oldDefinition is map, definition is map) returns map
{
    definition = syncStandards(oldDefinition, definition);
    var evaluatedDefinition = definition;
    var table = getStandardTable(definition);
    for (var entry in table)
    {
        definition[entry.key] = lookupTableFixExpression(entry.value);
        evaluatedDefinition[entry.key] = lookupTableGetValue(definition[entry.key]);
    }

    if (evaluatedDefinition.tapDrillDiameter > evaluatedDefinition.holeDiameter)
    {
        definition.tapDrillDiameter = definition.holeDiameter;
    }
    if (evaluatedDefinition.cBoreDiameter < evaluatedDefinition.holeDiameter)
    {
        definition.cBoreDiameter = definition.holeDiameter;
    }
    if (evaluatedDefinition.cSinkDiameter < evaluatedDefinition.holeDiameter)
    {
        definition.cSinkDiameter = definition.holeDiameter;
    }

    return definition;
}

function syncStandards(oldDefinition is map, definition is map) returns map
{
    if (definition.creationType == CreationType.PRESET)
    {
        if (oldDefinition.presetTappedOrClearance != undefined && oldDefinition.presetTappedOrClearance != definition.presetTappedOrClearance)
        {
            definition.presetBlindInLast = definition.presetTappedOrClearance;
        }
        else if (oldDefinition.presetBlindInLast != undefined && oldDefinition.presetBlindInLast != definition.presetBlindInLast)
        {
            definition.presetTappedOrClearance = definition.presetBlindInLast;
        }
    }
    else
    {
        if (oldDefinition.standardTappedOrClearance != undefined && oldDefinition.standardTappedOrClearance != definition.standardTappedOrClearance)
        {
            definition.standardBlindInLast = definition.standardTappedOrClearance;
        }
        else if (oldDefinition.standardBlindInLast != undefined && oldDefinition.standardBlindInLast != definition.standardBlindInLast)
        {
            definition.standardTappedOrClearance = definition.standardBlindInLast;
        }
    }

    return definition;
}
