FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "e269bd2b7266145c47eaf374", version : "ae998ef171199ea1dc18395a");
export import(path : "00b10ef1fb1a7418097fc0af", version : "2994750994597bce75afd4a7");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
export import(path : "b82468283e5ec09720bad185", version : "f035c6827196f0268491d7a0");
export import(path : "d82c5bf9082d0054f8f0b419", version : "85f89bc09b57a2add3d22385");

const BELT_TEETH_BOUNDS = { (unitless) : [1, 100, 1e50] } as IntegerBoundSpec;

export predicate isSimpleBelt(definition is map)
{
    definition.beltMode == BeltMode.SIMPLE;
}

export predicate isComplexBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX;
}

export predicate isOpenBelt(definition is map)
{
    definition.beltMode == BeltMode.OPEN;
}

export predicate isDoubleSidedBelt(definition is map)
{
    // Nested predicates not allowed
    definition.beltMode == BeltMode.COMPLEX && definition.isDoubleSidedBelt;
}

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

/**
 * What a pulley's location is: its center (with its teeth given), its pitch circle (which gives its teeth), or a
 * Robot pulley (which has its own). Editing logic sets it from what's selected.
 */
export enum SelectionType
{
    annotation { "Name" : "Center" }
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
    annotation { "Name" : "Standalone belt", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "A belt on its own, with its pulleys as far apart as it puts them, rather than between selections." }
    definition.isStandaloneBelt is boolean;

    if (!isStandaloneBelt(definition))
    {
        annotation {
                    "Name" : "Pulley one location",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES) || (GeometryType.CIRCLE && EntityType.EDGE),
                    "MaxNumberOfPicks" : 1,
                    "Description" : "Its center (a sketch point, mate connector, or a circle around it), its pitch circle, or a Robot pulley."
                }
        definition.pulleyOneSelection is Query;

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"], "Description" : "What the location is. Set from what's selected: a circle the size of a pitch circle is one." }
        definition.pulleyOneSelectionType is SelectionType;
    }

    if (isStandaloneBelt(definition) || definition.pulleyOneSelectionType == SelectionType.GEOMETRY)
    {
        annotation { "Name" : "Pulley one teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.pulleyOneTeeth, PULLEY_TEETH_BOUNDS);
    }

    if (!isStandaloneBelt(definition))
    {
        annotation {
                    "Name" : "Pulley two location",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES) || (GeometryType.CIRCLE && EntityType.EDGE),
                    "MaxNumberOfPicks" : 1,
                    "Description" : "Its center (a sketch point, mate connector, or a circle around it), its pitch circle, or a Robot pulley."
                }
        definition.pulleyTwoSelection is Query;

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"], "Description" : "What the location is. Set from what's selected: a circle the size of a pitch circle is one." }
        definition.pulleyTwoSelectionType is SelectionType;
    }

    if (isStandaloneBelt(definition) || definition.pulleyTwoSelectionType == SelectionType.GEOMETRY)
    {
        annotation { "Name" : "Pulley two teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isInteger(definition.pulleyTwoTeeth, PULLEY_TEETH_BOUNDS);
    }
}

export predicate complexBeltSelectionPredicate(definition is map)
{
    if (isOpenBelt(definition))
    {
        annotation { "Name" : "Start", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1,
                    "Description" : "Where the belt starts: where its end is clamped." }
        definition.startPoint is Query;
    }

    annotation {
                "Name" : "Pulleys",
                "Item name" : "Pulley",
                "Item label template" : "#beltSide pulley",
                "UIHint" : [UIHint.FOCUS_INNER_QUERY],
                "Driving query" : "pulleySelection"
            }
    definition.pulleys is array;
    for (var pulley in definition.pulleys)
    {
        annotation {
                    "Name" : "Pulley location",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES) || (EntityType.EDGE && GeometryType.CIRCLE),
                    "MaxNumberOfPicks" : 1,
                    "Description" : "Its center (a sketch point, mate connector, or a circle around it), its pitch circle, or a Robot pulley."
                }
        pulley.pulleySelection is Query;

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"], "Description" : "What the location is. Set from what's selected: a circle the size of a pitch circle is one." }
        pulley.selectionType is SelectionType;

        // A Robot pulley used as an idler (or a Robot pulley idler used as a pulley) is flagged by the feature
        annotation { "Name" : "Belt side", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Which side of the belt it's on: its teeth's (inside), or its back (outside, an idler)." }
        pulley.beltSide is BeltSide;

        if (!isDoubleSidedBelt(definition) && pulley.beltSide == BeltSide.OUTSIDE)
        {
            // A Robot pulley's size is its own
            if (pulley.selectionType != SelectionType.ROBOT_PULLEY)
            {
                annotation { "Name" : "Idler diameter" }
                isLength(pulley.idlerDiameter, NONNEGATIVE_LENGTH_BOUNDS);
            }
        }
        // Only show pulley teeth when it's not an idler and it's not a robot pulley
        else if (pulley.selectionType == SelectionType.GEOMETRY)
        {
            annotation { "Name" : "Pulley teeth" }
            isInteger(pulley.pulleyTeeth, PULLEY_TEETH_BOUNDS);
        }
    }

    if (isOpenBelt(definition))
    {
        annotation { "Name" : "End", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1,
                    "Description" : "Where the belt ends: where its other end is clamped." }
        definition.endPoint is Query;
    }
}


export predicate beltPredicate(definition is map)
{
    annotation { "Group Name" : "Belt", "Collapsed By Default" : false }
    {
        if (isOpenBelt(definition))
        {
            // An open belt's cut to length
            beltTypePredicate(definition);
        }
        else
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
                annotation { "Name" : "Select closest belt", "Description" : "Sets the belt to the one which fits the selections best: of those its supplier sells, or any." }
                isButton(definition.selectClosestBelt);
            }

            annotation { "Name" : "Model belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.modelBeltTeeth is boolean;
        }
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
        else if (isComplexBelt(definition))
        {
            annotation { "Name" : "Belt fit adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "An adjustment to apply to the fit of the belt. Useful for making slight adjustments to the tension of belts."
                    }
            isLength(definition.beltFitAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }

        if (!isOpenBelt(definition))
        {
            annotation { "Name" : "Disable belt validation",
                        "Description" : "Disable errors related to incorrect center to center distance and/or belt size." }
            definition.disableBeltValidation is boolean;
        }
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

/**
 * The lookup table the belt's chosen from, and its path in it: an open belt's is only its type and width.
 */
export function getBeltTableAndPath(definition is map) returns map
{
    if (isOpenBelt(definition))
    {
        return { "table" : beltTypeTable, "path" : definition.beltTypePath };
    }
    if (isDoubleSidedBelt(definition))
    {
        return { "table" : doubleBeltTable, "path" : definition.doubleBeltPath };
    }
    return { "table" : beltTable, "path" : definition.beltPath };
}
