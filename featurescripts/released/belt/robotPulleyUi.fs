FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "e269bd2b7266145c47eaf374", version : "b4f6ac77f1482a8f418914d7");
export import(path : "00b10ef1fb1a7418097fc0af", version : "e74e529a6b7dd93216441e1c");

export import(path : "6e24956e9977116c79280620", version : "0ec5da0acf56336b68065e37");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
export import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");
export import(path : "554542fc345271814c4463b0", version : "9c477217d62dbff99c9b2ad2");
export import(path : "b82468283e5ec09720bad185", version : "c1982c22aa4741f71a6e97cf");
export import(path : "962bbb367fd7d91fae71cd4c", version : "0230c5f617a7df45c9349b96");
export import(path : "0103ad63394d7713fbf44448", version : "93809a6b0922842a07809b6f");
// Exports Fit, a parameter type
export import(path : "core/fit.fs", version : "");

export enum CreationMethod
{
    annotation { "Name" : "Manual" }
    MANUAL,
    annotation { "Name" : "Belt" }
    BELT
}

export enum PulleyKind
{
    annotation { "Name" : "Pulley" }
    PULLEY,
    annotation { "Name" : "Idler" }
    IDLER
}

/**
 * Whether the feature makes an idler (in Manual; in Belt, each is whatever the belt has at the face).
 */
export predicate isManualIdler(definition is map)
{
    definition.creationMethod == CreationMethod.MANUAL && definition.pulleyKind == PulleyKind.IDLER;
}

export predicate pulleyGeneralPredicate(definition is map)
{
    annotation { "Group Name" : "Pulley", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Description" : "A pulley, with teeth, or an idler, smooth, for a belt's back." }
        definition.pulleyKind is PulleyKind;

        beltTypePredicate(definition);

        if (definition.pulleyKind == PulleyKind.PULLEY)
        {
            annotation { "Name" : "Pulley teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isInteger(definition.pulleyTeeth, PULLEY_TEETH_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Idler diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.idlerDiameter, IDLER_DIAMETER_BOUNDS);
        }
    }
}

export predicate pulleyFlangePredicate(definition is map)
{
    annotation { "Name" : "Add flanges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.addFlanges is boolean;

    if (definition.addFlanges)
    {
        annotation { "Group Name" : "Add flanges", "Collapsed By Default" : false, "Driving Parameter" : "addFlanges" }
        {
            annotation { "Name" : "Flange size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.flangeSize is FlangeSize;
        }
    }
}

const TEXT_POSITION_BOUNDS =
{
            (unitless) : [0.0, 0.75, 1.0]
        } as RealBoundSpec;

export predicate pulleyTextPredicate(definition is map)
{
    annotation { "Name" : "Engrave tooth count", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.addText is boolean;

    if (definition.addText)
    {
        annotation { "Group Name" : "Engrave tooth count", "Collapsed By Default" : false, "Driving Parameter" : "addText" }
        {
            textPredicate(definition);

            annotation { "Name" : "Engrave both sides", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.engraveBothSides is boolean;

            annotation { "Name" : "Text position", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isReal(definition.textPosition, TEXT_POSITION_BOUNDS);

            // annotation { "Name" : "Extra text", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            // definition.text is string;
        }
    }
}

export enum BoreType
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Hole" }
    HOLE,
    annotation { "Name" : "Spline" }
    SPLINE
}

const ENTRANCE_CHAMFER_BOUNDS =
{
            (meter) : [1e-5, 0.001, 500],
            (centimeter) : 0.1,
            (millimeter) : 1,
            (inch) : 1 / 32,
        } as LengthBoundSpec;

export predicate pulleyBorePredicate(definition is map)
{
    annotation { "Name" : "Add bore", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.addBore is boolean;

    if (definition.addBore)
    {
        annotation { "Group Name" : "Add bore", "Collapsed By Default" : false, "Driving Parameter" : "addBore" }
        {
            annotation { "Name" : "Bore type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.boreType is BoreType;

            if (definition.boreType == BoreType.HEX)
            {
                annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
                isLength(definition.hexWidth, NONNEGATIVE_LENGTH_BOUNDS);
            }
            else if (definition.boreType == BoreType.HOLE)
            {
                annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"], "Description" : "The diameter of the shaft it goes on." }
                isLength(definition.boreDiameter, HOLE_DIAMETER_BOUNDS);
            }
            else if (definition.boreType == BoreType.SPLINE)
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.splineType is SplineType;
            }

            // Of the bore on its shaft
            fitPredicate(definition);

            annotation { "Name" : "Entrance chamfer", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.entranceChamfer is boolean;

            if (definition.entranceChamfer)
            {
                annotation { "Group Name" : "Entrance chamfer", "Collapsed By Default" : false, "Driving Parameter" : "entranceChamfer" }
                {
                    annotation { "Name" : "Chamfer distance" }
                    isLength(definition.chamferDistance, ENTRANCE_CHAMFER_BOUNDS);
                }
            }
        }
    }
}

export predicate pulleyPredicate(definition is map)
{
    unitSystemPredicate(definition);

    annotation { "Name" : "Creation method", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"], "Default" : "BELT" }
    definition.creationMethod is CreationMethod;

    if (definition.creationMethod == CreationMethod.MANUAL)
    {
        locationPredicate(definition, "pulley");
    }
    else
    {
        annotation { "Name" : "Curved belt faces or mate connectors", "Filter" : (EntityType.FACE && GeometryType.CYLINDER) || BodyType.MATE_CONNECTOR, "UIHint" : UIHint.PREVENT_CREATING_NEW_MATE_CONNECTORS }
        definition.beltSelections is Query;
    }

    if (definition.creationMethod == CreationMethod.MANUAL)
    {
        pulleyGeneralPredicate(definition);

        startOffsetPredicate(definition);
    }

    pulleyFlangePredicate(definition);

    pulleyBorePredicate(definition);

    // An idler has no teeth to engrave the count of
    if (definition.addFlanges && !isManualIdler(definition))
    {
        pulleyTextPredicate(definition);
    }

    annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
    {
        annotation { "Name" : "Two belts" }
        definition.twoBelts is boolean;

        if (definition.twoBelts && definition.creationMethod == CreationMethod.BELT)
        {
            annotation { "Name" : "Flip side", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "OPPOSITE_DIRECTION"] }
            definition.flipTwoBeltSide is boolean;
        }

        profileOffsetPredicate(definition);

        // A two belt pulley always has them, for its belts
        if (!definition.twoBelts)
        {
            annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addMateConnectors is boolean;
        }
    }

    pointManipulatorPredicate(definition);
}
