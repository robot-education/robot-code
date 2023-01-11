FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export import(path : "onshape/std/hole.fs", version : "1930.0");
export import(path : "onshape/std/holeUtils.fs", version : "1930.0");
export import(path : "onshape/std/holetables.gen.fs", version : "1930.0");

import(path : "69e8015ed20821ef00ec816e", version : "baafe66be21c75fdb0d1b221");

export predicate holeTopPredicate(definition is map)
{
    annotation { "Name" : "Style", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "INITIAL_FOCUS"] }
    definition.style is HoleStyle;

    annotation { "Name" : "Termination", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
    definition.endStyle is HoleEndStyle;

    annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION }
    definition.oppositeDirection is boolean;
}

export predicate holeSelectionPredicate(definition is map)
{
    if (definition.endStyle != HoleEndStyle.BLIND_IN_LAST && definition.standardTappedOrClearance != undefined)
    {
        annotation { "Name" : "Standard", "Lookup Table" : tappedOrClearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "UNCONFIGURABLE"] }
        definition.standardTappedOrClearance is LookupTablePath;
    }
    else if (definition.endStyle == HoleEndStyle.BLIND_IN_LAST && definition.standardBlindInLast != undefined)
    {
        annotation { "Name" : "Standard", "Lookup Table" : blindInLastHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "UNCONFIGURABLE"] }
        definition.standardBlindInLast is LookupTablePath;
    }
}

export predicate holeDiameterPredicate(definition is map)
{
    annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
    isLength(definition.holeDiameter, HOLE_DIAMETER_BOUNDS);
}

export predicate cBoreAndCSinkPredicate(definition is map)
{
    if (definition.style == HoleStyle.C_BORE)
    {
        annotation { "Name" : "Counterbore diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.cBoreDiameter, HOLE_BORE_DIAMETER_BOUNDS);

        annotation { "Name" : "Counterbore depth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.cBoreDepth, HOLE_BORE_DEPTH_BOUNDS);
    }
    else if (definition.style == HoleStyle.C_SINK)
    {
        annotation { "Name" : "Countersink diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.cSinkDiameter, HOLE_BORE_DIAMETER_BOUNDS);

        annotation { "Name" : "Countersink angle", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isAngle(definition.cSinkAngle, CSINK_ANGLE_BOUNDS);
    }
}

export predicate tapDrillDiameterPredicate(definition is map)
{
    if (definition.endStyle == HoleEndStyle.BLIND_IN_LAST && definition.tapDrillDiameter != undefined)
    {
        annotation { "Name" : "Tap drill diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.tapDrillDiameter, HOLE_DIAMETER_BOUNDS);
    }
}

export predicate holeDepthPredicate(definition is map)
{
    if (definition.majorDiameter != undefined)
    {
        annotation { "Name" : "Tap major diameter", "UIHint" : ["ALWAYS_HIDDEN"] }
        isLength(definition.majorDiameter, HOLE_MAJOR_DIAMETER_BOUNDS);
    }

    if (definition.endStyle != HoleEndStyle.THROUGH)
    {
        annotation { "Name" : "Depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isLength(definition.holeDepth, HOLE_DEPTH_BOUNDS);
    }
}

export predicate holeTapDepthPredicate(definition is map)
{
    /*
     * showTappedDepth, tappedDepth and tapClearance are for hole annotations;
     * they currently have no effect on geometry regeneration, but is stored in HoleAttribute. If we modeled the hole's
     * threads, then they would have an effect.
     */
    annotation { "Name" : "Tapped details", "UIHint" : UIHint.ALWAYS_HIDDEN }
    definition.showTappedDepth is boolean;

    if (definition.showTappedDepth)
    {
        if (definition.endStyle == HoleEndStyle.THROUGH)
        {
            annotation { "Name" : "Tap through all", "Default" : true, "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.isTappedThrough is boolean;
        }

        if (definition.endStyle != HoleEndStyle.THROUGH || !definition.isTappedThrough)
        {
            annotation { "Name" : "Tapped depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.tappedDepth, HOLE_DEPTH_BOUNDS);
        }

        if (definition.endStyle != HoleEndStyle.THROUGH)
        {
            annotation { "Name" : "Tap clearance (number of thread pitch lengths)", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isReal(definition.tapClearance, HOLE_CLEARANCE_BOUNDS);
        }
    }
}

export predicate holeLocationPredicate(definition is map)
{
    annotation { "Name" : "Sketch points to place holes",
                "Filter" : EntityType.VERTEX && SketchObject.YES && ModifiableEntityOnly.YES || BodyType.MATE_CONNECTOR }
    definition.locations is Query;
}

export predicate holeStartFromSketchPredicate(definition is map)
{
    if (definition.endStyle == HoleEndStyle.BLIND || (definition.endStyle == HoleEndStyle.THROUGH && definition.style != HoleStyle.SIMPLE))
    {
        annotation { "Name" : "Start holes on part", "Default" : false }
        definition.startFromSketch is boolean;
    }
}

export predicate holeBooleanScopePredicate(definition is map)
{
    annotation { "Name" : "Merge scope",
                "Filter" : (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES && AllowMeshGeometry.YES) }
    definition.scope is Query;
}

const HOLE_CLEARANCE_BOUNDS =
{
            (unitless) : [0, 3, 100]
        } as RealBoundSpec;

const HOLE_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.25,
            (foot) : 0.02,
            (yard) : 0.007
        } as LengthBoundSpec;

const HOLE_MAJOR_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.25,
            (foot) : 0.02,
            (yard) : 0.007
        } as LengthBoundSpec;

const HOLE_BORE_DIAMETER_BOUNDS =
{
            (meter) : [1e-5, 0.01, 500],
            (centimeter) : 1.0,
            (millimeter) : 10.0,
            (inch) : 0.5,
            (foot) : 0.04,
            (yard) : 0.014
        } as LengthBoundSpec;

const HOLE_DEPTH_BOUNDS =
{
            (meter) : [1e-5, 0.012, 500],
            (centimeter) : 1.2,
            (millimeter) : 12.0,
            (inch) : 0.5,
            (foot) : 0.04,
            (yard) : 0.014
        } as LengthBoundSpec;

const HOLE_BORE_DEPTH_BOUNDS =
{
            (meter) : [0.0, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.25,
            (foot) : 0.02,
            (yard) : 0.007
        } as LengthBoundSpec;

export function clusterVertexQueries(context is Context, selected is Query) returns array
{
    var perFeature = {};
    for (var tId in evaluateQuery(context, selected))
    {
        var operationId = lastModifyingOperationId(context, tId);
        if (perFeature[operationId] == undefined)
        {
            perFeature[operationId] = [];
        }
        perFeature[operationId] = append(perFeature[operationId], tId);
    }
    var clusterQueries = [];
    for (var entry in perFeature)
    {
        var nPoints = size(entry.value);
        if (nPoints == 1)
        {
            clusterQueries = append(clusterQueries, entry.value[0]);
        }
        else
        {
            var points = makeArray(nPoints);
            for (var i = 0; i < nPoints; i = i + 1)
            {
                points[i] = evVertexPoint(context, { 'vertex' : entry.value[i] });
            }
            var clusters = clusterPoints(points, TOLERANCE.zeroLength * meter);
            for (var cluster in clusters)
            {
                clusterQueries = append(clusterQueries, entry.value[cluster[0]]);
            }
        }
    }
    return evaluateQuery(context, qIntersection([selected, qUnion(clusterQueries)]));
}


