FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "e269bd2b7266145c47eaf374", version : "6c8b8d8077dcf88085165ded");
export import(path : "00b10ef1fb1a7418097fc0af", version : "3ba879cf97235b1a292f0dbc");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");
export import(path : "b82468283e5ec09720bad185", version : "fd6fb14ea8cac07a38291b2f");
export import(path : "d82c5bf9082d0054f8f0b419", version : "5ddffe8574f5098d20aa559d");

const BELT_TEETH_BOUNDS = { (unitless) : [1, 100, 1e50] } as IntegerBoundSpec;

export predicate isSimpleBelt(definition is map)
{
    definition.beltMode == BeltMode.SIMPLE;
}

export predicate isComplexBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX;
}

export predicate isDoubleSidedBelt(definition is map)
{
    // Nested predicates not allowed
    definition.beltMode == BeltMode.COMPLEX && definition.isDoubleSidedBelt;
}

// export predicate canBeStandaloneBelt(definition is map)
// {
//     definition.beltMode == BeltMode.SIMPLE;
// }

export predicate isStandaloneBelt(definition is map)
{
    definition.beltMode == BeltMode.SIMPLE && definition.isStandaloneBelt;
}

export enum BeltSide
{
    annotation { "Name" : "Inside" }
    INSIDE,
    annotation { "Name" : "Outside" }
    OUTSIDE
}

export enum SelectionType
{
    annotation { "Name" : "Geometry" }
    GEOMETRY,
    annotation { "Name" : "Robot pulley" }
    ROBOT_PULLEY,
    annotation { "Name" : "Pitch circle" }
    PITCH_CIRCLE
}

export predicate selectionPredicate(definition is map)
{
    annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
    {
        if (isSimpleBelt(definition))
        {
            simpleBeltSelectionPredicate(definition);
        }
        else
        {
            complexBeltSelectionPredicate(definition);
        }
    }

    if (!isStandaloneBelt(definition))
    {
        startOffsetPredicate(definition);
    }
}

export predicate simpleBeltSelectionPredicate(definition is map)
{
    annotation { "Name" : "Standalone belt", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.isStandaloneBelt is boolean;

    if (!isStandaloneBelt(definition))
    {
        annotation { "Name" : "Selection type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.pulleyOneSelectionType is SelectionType;

        annotation {
                    "Name" : "Pulley one position",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES) || (GeometryType.CIRCLE && SketchObject.YES),
                    "MaxNumberOfPicks" : 1
                }
        definition.pulleyOneSelection is Query;
    }

    if (isStandaloneBelt(definition) || definition.pulleyOneSelectionType == SelectionType.GEOMETRY)
    {
        annotation { "Name" : "Pulley one teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.pulleyOneTeeth, PULLEY_TEETH_BOUNDS);
    }

    if (!isStandaloneBelt(definition))
    {
        annotation { "Name" : "Selection type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.pulleyTwoSelectionType is SelectionType;

        annotation {
                    "Name" : "Pulley two position",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES) || (GeometryType.CIRCLE && SketchObject.YES),
                    "MaxNumberOfPicks" : 1
                }
        definition.pulleyTwoSelection is Query;
    }

    if (isStandaloneBelt(definition) || definition.pulleyTwoSelectionType == SelectionType.GEOMETRY)
    {
        annotation { "Name" : "Pulley two teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.pulleyTwoTeeth, PULLEY_TEETH_BOUNDS);
    }
}


export predicate complexBeltSelectionPredicate(definition is map)
{
    annotation {
                "Name" : "Pulleys",
                "Item name" : "Pulley",
                "Item label template" : "#beltSide pulley",
                "UIHint" : [UIHint.FOCUS_INNER_QUERY],
                "Driving query" : "pulleyLocation"
            }
    definition.pulleys is array;
    for (var pulley in definition.pulleys)
    {
        annotation { "Name" : "Selection type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        pulley.selectionType is SelectionType;

        annotation {
                    "Name" : "Pulley location",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES),
                    "MaxNumberOfPicks" : 1
                }
        pulley.pulleySelection is Query;

        // We won't worry about trying to prevent selecting a robot pulley and then using it as an idler since it greatly complicates the UI
        // Instead we'll just flag it as an warning in the feature later
        annotation { "Name" : "Belt side", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
        pulley.beltSide is BeltSide;

        // Always show idler diamter when we have an idler
        if (!isDoubleSidedBelt(definition) && pulley.beltSide == BeltSide.OUTSIDE)
        {
            annotation { "Name" : "Idler diameter" }
            isLength(pulley.idlerDiameter, NONNEGATIVE_LENGTH_BOUNDS);
        }
        // Only show pulley teeth when it's not an idler and it's not a robot pulley
        else if (pulley.selectionType == SelectionType.GEOMETRY)
        {
            annotation { "Name" : "Pulley teeth" }
            isInteger(pulley.pulleyTeeth, PULLEY_TEETH_BOUNDS);
        }
    }
}


export predicate beltPredicate(definition is map)
{
    annotation { "Group Name" : "Belt", "Collapsed By Default" : false }
    {
        if (!isDoubleSidedBelt(definition))
        {
            beltTablePredicate(definition);
        }
        else
        {
            doubleBeltTablePredicate(definition);
        }

        annotation { "Name" : "Belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.beltTeeth, BELT_TEETH_BOUNDS);

        if (!isStandaloneBelt(definition))
        {
            annotation { "Name" : "Select closest belt" }
            isButton(definition.selectClosestBelt);
        }


        annotation { "Name" : "Model belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.modelBeltTeeth is boolean;
    }
}

export enum Pulley
{
    ONE,
    TWO
}

/**
 * @param pulley : @autocomplete `pulley`
 */
export function pulleyString(pulley is Pulley) returns string
{
    return "pulley" ~ (pulley == Pulley.ONE ? "One" : "Two");
}

export predicate optionsPredicate(definition is map)
{
    annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
    {
        annotation { "Name" : "Add mate connectors", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.addMateConnectors is boolean;

        if (isSimpleBelt(definition))
        {
            annotation { "Name" : "Center to center adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "An adjustment to apply to the center to center distance. Useful for making slight adjustments to the tension of belts."
                    }
            isLength(definition.centerToCenterAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Belt fit adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "An adjustment to apply to the fit of the belt. Useful for making slight adjustments to the tension of belts."
                    }
            isLength(definition.beltFitAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }

        annotation { "Name" : "Disable belt validation",
                    "Description" : "Disable errors related to incorrect center to center distance and/or belt size." }
        definition.disableBeltValidation is boolean;
    }
}

export predicate robotBeltPredicate(definition is map)
{
    unitSystemPredicate(definition);

    annotation { "Name" : "Belt mode", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
    definition.beltMode is BeltMode;

    if (isComplexBelt(definition))
    {
        annotation { "Name" : "Double sided belt" }
        definition.isDoubleSidedBelt is boolean;
    }

    selectionPredicate(definition);

    beltPredicate(definition);

    optionsPredicate(definition);
}

export function getBeltTableAndPath(definition is map) returns map
{
    var table;
    var path;
    if (!isDoubleSidedBelt(definition))
    {
        table = beltTable;
        path = definition.beltPath;
    }
    else
    {
        table = doubleBeltTable;
        path = definition.doubleBeltPath;
    }

    return {
            "table" : table,
            "path" : path
        };
}
