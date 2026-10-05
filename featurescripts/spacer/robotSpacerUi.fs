FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "21762d39019c8b2289e2fbb8", version : "06bafd6cfddc3fbe92c47892");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");

export import(path : "a6eeed056b8f09ac4e8ae12e", version : "bdabf6a4e955483d02698cb0");
export import(path : "6e24956e9977116c79280620", version : "4deaa9512490955da06665c4");
export import(path : "0103ad63394d7713fbf44448", version : "d9ead1a79bded860ba8f3ddf");
export import(path : "0195d390c3944cd4fab21ce0", version : "84c0fb95445553f4ea571ad5");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");


import(path : "8fc3df84a88e74d27ad43d26", version : "b6f17a04daefbce8624703b9");


export enum SpacerType
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Round" }
    ROUND,
    annotation { "Name" : "Spline" }
    SPLINE
}

export predicate hexSpacer(definition is map)
{
    definition.spacerType == SpacerType.HEX;
}

export predicate roundSpacer(definition is map)
{
    definition.spacerType == SpacerType.ROUND;
}

export predicate splineSpacer(definition is map)
{
    definition.spacerType == SpacerType.SPLINE;
}

// export predicate squareSpacer(definition is map)
// {
//     definition.spacerType == SpacerType.SQUARE;
// }

export enum Fit
{
    annotation { "Name" : "Close" }
    CLOSE,
    annotation { "Name" : "Free" }
    FREE
}

export enum HexSize
{
    annotation { "Name" : "1/2 in." }
    _1_2_IN,
    annotation { "Name" : "3/8 in." }
    _3_8_IN,
    annotation { "Name" : "Custom" }
    CUSTOM
}


export predicate robotSpacerPredicate(definition is map)
{
    unitSystemPredicate(definition);
    
    locationPredicate(definition, "spacer");

    generalPredicate(definition);

    annotation { "Group Name" : "Extrude", "Collapsed By Default" : false }
    {
        newExtrudePredicate(definition);
    }
}


export predicate generalPredicate(definition is map)
{
    annotation { "Group Name" : "Spacer", "Collapsed By Default" : false }
    {
        annotation { "Name" : "Spacer type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.spacerType is SpacerType;

        if (hexSpacer(definition))
        {
            hexSizePredicate(definition);
        }
        else if (roundSpacer(definition))
        {
            holeSizePredicate(definition);
        }
        else if (splineSpacer(definition))
        {
            annotation { "Name" : "Spline type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
            definition.splineType is SplineType;
        }

        if (canBeSnapOnSpacer(definition))
        {
            snapOnPredicate(definition);
        }

        // else if (squareSpacer(definition))
        // {
        //     annotation { "Name" : "Width" }
        //     isLength(definition.width, LENGTH_BOUNDS);
        // }

        if (canHaveProfileOffset(definition))
        {
            profileOffsetPredicate(definition);
        }

        // Option is required for custom spacers
        if (!isCustomSizeSpacer(definition))
        {
            annotation { "Name" : "Custom wall", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.customWall is boolean;
        }
        // isCustomSizeSpacer(definition) ? true : definition.customWall
        if (isCustomSizeSpacer(definition) || definition.customWall)
        {
            annotation { "Group Name" : "Custom wall", "Collapsed By Default" : false, "Driving Parameter" : "customWall" }
            {
                wallPredicate(definition);
            }
        }
    }
}

export predicate canHaveProfileOffset(definition is map)
{
    // !isCustomSizeSpacer, as all non-custom spacers can have a profile offset
    (definition.spacerType == SpacerType.HEX && definition.hexSize != HexSize.CUSTOM) || definition.spacerType == SpacerType.SPLINE;
}

export predicate isCustomSizeSpacer(definition is map)
{
    (definition.spacerType == SpacerType.HEX && definition.hexSize == HexSize.CUSTOM);
}

export predicate hexSizePredicate(definition is map)
{
    annotation { "Name" : "Hex size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.hexSize is HexSize;

    if (definition.hexSize == HexSize.CUSTOM)
    {
        annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.hexWidth, NONNEGATIVE_LENGTH_BOUNDS);
    }
}

function getHexWidth(definition is map) returns ValueWithUnits
{
    if (definition.hexSize == HexSize._1_2_IN)
    {
        return 0.5 * inch;
    }
    else if (definition.hexSize == HexSize._3_8_IN)
    {
        return 0.375 * inch;
    }
    else if (definition.hexSize == HexSize.CUSTOM)
    {
        return definition.hexWidth;
    }
}

export predicate holeSizePredicate(definition is map)
{
    annotation { "Name" : "Hole table", "Lookup Table" : clearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.clearanceHolePath is LookupTablePath;

    holeDiameterPredicate(definition);
}


export function getTableAndPath(definition is map) returns map
{
    return { "table" : clearanceHoleTable, "path" : definition.clearanceHolePath };
}

export predicate snapOnPredicate(definition is map)
{
    annotation { "Name" : "Snap on", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Use a snap on design." }
    definition.snapOn is boolean;
}

export predicate canBeSnapOnSpacer(definition is map)
{
    definition.spacerType == SpacerType.HEX && definition.hexSize != HexSize.CUSTOM;
}

export predicate isSnapOnSpacer(definition is map)
{
    canBeSnapOnSpacer(definition) && definition.snapOn;
}

export function getInnerDiameter(definition is map) returns ValueWithUnits
{
    if (hexSpacer(definition))
    {
        return getHexWidth(definition) + (definition.fit == Fit.CLOSE ? 0.008 * inch : 0.016 * inch);
    }
    else if (roundSpacer(definition))
    {
        return definition.holeDiameter;
    }
    else if (splineSpacer(definition))
    {
        return 1.375 * inch;
    }
}

export function getOuterDiameter(definition is map, innerDiameter is ValueWithUnits) returns ValueWithUnits
{
    if (isCustomSizeSpacer(definition) || definition.customWall)
    {
        return getWallDiameter(definition, innerDiameter);
    }
    if (hexSpacer(definition))
    {
        return switch (definition.hexSize)
            {
                    HexSize._3_8_IN : 0.625 * inch,
                    HexSize._1_2_IN : 0.75 * inch
                };
    }
    else if (roundSpacer(definition))
    {
        const size = getTableAndPath(definition).path.size;
        return switch (size) {
                    "#8" : 0.3125 * inch,
                    "#10" : 0.375 * inch,
                    "1/4" : 0.5 * inch
                };
    }
    else if (splineSpacer(definition))
    {
        return 1.625 * inch;
    }
}
