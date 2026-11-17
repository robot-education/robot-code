FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "e269bd2b7266145c47eaf374", version : "890cecdf29aaf3381935d816");
export import(path : "00b10ef1fb1a7418097fc0af", version : "2994750994597bce75afd4a7");
export import(path : "948c83c1b1ac83de4ccf921b", version : "5bd77bd9ef3230343bfbc217");
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
export import(path : "d82c5bf9082d0054f8f0b419", version : "abd856d63642589f61cbad83");
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");

const BELT_TEETH_BOUNDS = { (unitless) : [1, 100, 1e50] } as IntegerBoundSpec;
const MOVE_PULLEY_BOUNDS = { (unitless) : [1, 1, 1000] } as IntegerBoundSpec;

/**
 * A complex belt which is a loop, made to fit by moving one of its pulleys or idlers along a direction.
 */
export predicate isMovePulleyBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX && !definition.openBelt && definition.movePulley;
}

export predicate isSimpleBelt(definition is map)
{
    definition.beltMode == BeltMode.SIMPLE;
}

export predicate isComplexBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX;
}

/**
 * A simple belt between selections, which is whichever belt its supplier sells (of the type and width chosen) is
 * closest to their distance, rather than one chosen.
 */
export predicate isClosestBelt(definition is map)
{
    definition.beltMode == BeltMode.SIMPLE && !definition.isStandaloneBelt && definition.useClosestBelt;
}

/**
 * A complex belt with ends (clamped, like a linear slide's), rather than a loop.
 */
export predicate isOpenBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX && definition.openBelt;
}

/**
 * A complex belt which is a loop.
 */
export predicate isLoopComplexBelt(definition is map)
{
    definition.beltMode == BeltMode.COMPLEX && !definition.openBelt;
}

export predicate isDoubleSidedBelt(definition is map)
{
    // Nested predicates not allowed. An open belt's single sided.
    definition.beltMode == BeltMode.COMPLEX && !definition.openBelt && definition.isDoubleSidedBelt;
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
    annotation { "Name" : "Standalone belt", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
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

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
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

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
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
    annotation { "Name" : "Open belt", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.openBelt is boolean;

    if (definition.openBelt)
    {
        annotation { "Group Name" : "Open belt", "Collapsed By Default" : false, "Driving Parameter" : "openBelt" }
        {
            annotation { "Name" : "Start", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.startPoint is Query;

            annotation { "Name" : "End", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.endPoint is Query;
        }
    }
    else
    {
        annotation { "Name" : "Double sided belt" }
        definition.isDoubleSidedBelt is boolean;
    }

    annotation {
                "Name" : "Pulleys",
                "Item name" : "Pulley",
                "Item label template" : "#beltSide pulley",
                "Driven query" : "pulleySelection"
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

        annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
        pulley.selectionType is SelectionType;

        // A Robot pulley used as an idler (or a Robot pulley idler used as a pulley) is flagged by the feature
        annotation { "Name" : "Belt side", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
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

    if (!definition.openBelt)
    {
        annotation { "Name" : "Move a pulley to fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.movePulley is boolean;

        if (definition.movePulley)
        {
            annotation { "Group Name" : "Move a pulley to fit", "Collapsed By Default" : false, "Driving Parameter" : "movePulley" }
            {
                annotation { "Name" : "Pulley to move", "Description" : "Its number in Pulleys, or click its point." }
                isInteger(definition.movePulleyNumber, MOVE_PULLEY_BOUNDS);

                annotation { "Name" : "Direction", "Filter" : QueryFilterCompound.ALLOWS_DIRECTION, "MaxNumberOfPicks" : 1 }
                definition.moveDirection is Query;
            }
        }
    }
}


export predicate beltPredicate(definition is map)
{
    annotation { "Group Name" : "Belt", "Collapsed By Default" : false }
    {
        if (isSimpleBelt(definition) && !definition.isStandaloneBelt)
        {
            annotation { "Name" : "Use closest belt", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.useClosestBelt is boolean;
        }

        if (isOpenBelt(definition))
        {
            // An open belt's cut to length
            beltTypePredicate(definition);
        }
        else if (isClosestBelt(definition))
        {
            // The belt's teeth are the closest's
            if (isFrc(definition))
            {
                annotation { "Name" : "FRC belt size", "Lookup Table" : frcBeltSizeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcBeltSizePath is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "FTC belt size", "Lookup Table" : ftcBeltSizeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcBeltSizePath is LookupTablePath;
            }
        }
        else if (!isDoubleSidedBelt(definition))
        {
            if (isFrc(definition))
            {
                annotation { "Name" : "FRC belt", "Lookup Table" : frcBeltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.frcBeltPath is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "FTC belt", "Lookup Table" : ftcBeltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.ftcBeltPath is LookupTablePath;
            }
        }
        else if (isFrc(definition))
        {
            annotation { "Name" : "FRC double sided belt", "Lookup Table" : frcDoubleBeltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.frcDoubleBeltPath is LookupTablePath;
        }
        else
        {
            annotation { "Name" : "FTC double sided belt", "Lookup Table" : ftcDoubleBeltTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.ftcDoubleBeltPath is LookupTablePath;
        }

        // Set by editing logic, as Onshape can't show parameters by a lookup table's value
        annotation { "Name" : "Custom belt", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "isCustomBeltChosen" }
        definition.isCustomBelt is boolean;

        if (definition.isCustomBelt)
        {
            // An open belt's type is its table's, and it has no teeth to count
            if (!isOpenBelt(definition))
            {
                annotation { "Name" : "Belt type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.customBeltType is CustomBeltType;
            }

            customBeltWidthPredicate(definition);

            if (!isOpenBelt(definition) && !isClosestBelt(definition))
            {
                annotation { "Name" : "Belt teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.beltTeeth, BELT_TEETH_BOUNDS);
            }
        }

        if (!isOpenBelt(definition))
        {
            if (!isStandaloneBelt(definition) && !isClosestBelt(definition))
            {
                annotation { "Name" : "Select closest belt" }
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
            annotation { "Name" : "Center to center adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"]
                    }
            isLength(definition.centerToCenterAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }
        else if (isLoopComplexBelt(definition))
        {
            annotation { "Name" : "Belt fit adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "How much longer the path around the pulleys should be than the belt: positive for a looser belt."
                    }
            isLength(definition.beltFitAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }

        if (!isOpenBelt(definition))
        {
            annotation { "Name" : "Disable belt validation",
                        "Description" : "Hides the warnings that the pulleys are the wrong distance apart, or the belt the wrong length." }
            definition.disableBeltValidation is boolean;
        }
    }
}

export predicate robotBeltPredicate(definition is map)
{
    programPredicate(definition);

    annotation { "Name" : "Belt mode", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
    definition.beltMode is BeltMode;

    selectionPredicate(definition);

    beltPredicate(definition);

    optionsPredicate(definition);
}

/**
 * The name of the parameter the belt's chosen with: its lookup table's (by its program, and whether it's open or
 * double sided).
 */
export function getBeltPathName(definition is map) returns string
{
    if (isOpenBelt(definition))
    {
        return "beltTypePath";
    }
    const program = isFrc(definition) ? "frc" : "ftc";
    if (isClosestBelt(definition))
    {
        return program ~ "BeltSizePath";
    }
    return program ~ (isDoubleSidedBelt(definition) ? "DoubleBeltPath" : "BeltPath");
}

/**
 * The lookup table the belt's chosen from, and its path in it (an open belt's is only its type and width), and what
 * the table leaves to the feature (see `getBeltValue`): a Custom belt's type, width, and teeth, or an open belt's
 * Custom width.
 */
export function getBeltTableAndPath(definition is map) returns map
{
    const tables = {
            "beltTypePath" : beltTypeTable,
            "frcBeltPath" : frcBeltTable,
            "ftcBeltPath" : ftcBeltTable,
            "frcDoubleBeltPath" : frcDoubleBeltTable,
            "ftcDoubleBeltPath" : ftcDoubleBeltTable,
            "frcBeltSizePath" : frcBeltSizeTable,
            "ftcBeltSizePath" : ftcBeltSizeTable
        };
    const name = getBeltPathName(definition);
    return {
            "table" : tables[name],
            // A size's belts are the full table's at its path
            "optionsTable" : isClosestBelt(definition) ? (isFrc(definition) ? frcBeltTable : ftcBeltTable) : tables[name],
            "path" : definition[name],
            "customType" : getCustomBeltType(definition.customBeltType),
            "customWidth" : definition.customBeltWidth,
            "customTeeth" : definition.beltTeeth
        };
}

/**
 * Custom belt: whether the belt chosen is Custom (an open belt's, its width), so the Custom belt parameters show.
 */
export function isCustomBeltChosen(definition is map) returns boolean
{
    return showsCustomBelt(getBeltTableAndPath(definition).path);
}
