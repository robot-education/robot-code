FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "e269bd2b7266145c47eaf374", version : "6c8b8d8077dcf88085165ded");
export import(path : "00b10ef1fb1a7418097fc0af", version : "3ba879cf97235b1a292f0dbc");

export import(path : "6e24956e9977116c79280620", version : "1cdcfd6334c53e51fef6f5f5");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");
export import(path : "0195d390c3944cd4fab21ce0", version : "eb719b1576c924e1ac6e1ffa");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");
export import(path : "554542fc345271814c4463b0", version : "8505eef33b9585d0bbfc9ee8");
export import(path : "b82468283e5ec09720bad185", version : "fd6fb14ea8cac07a38291b2f");
export import(path : "962bbb367fd7d91fae71cd4c", version : "4d1aeb63b3bc946e68206934");
export import(path : "0103ad63394d7713fbf44448", version : "d9ead1a79bded860ba8f3ddf");

export enum CreationMethod
{
    annotation { "Name" : "Manual" }
    MANUAL,
    annotation { "Name" : "Belt" }
    BELT
}

export predicate pulleyGeneralPredicate(definition is map)
{
    annotation { "Group Name" : "Pulley", "Collapsed By Default" : false }
    {
        beltTypePredicate(definition);

        annotation { "Name" : "Pulley teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.pulleyTeeth, PULLEY_TEETH_BOUNDS);
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

export const ENTRANCE_CHAMFER_BOUNDS =
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
                holeDiameterPredicate(definition);
            }
            else if (definition.boreType == BoreType.SPLINE)
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.splineType is SplineType;

                boreProfileOffsetPredicate(definition);
            }

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

    if (definition.addFlanges)
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

        annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.addMateConnectors is boolean;
    }

    pointManipulatorPredicate(definition);
}
