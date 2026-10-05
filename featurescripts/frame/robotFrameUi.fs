FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "fd1a648a505300077513a2f3", version : "3299f8f15b3d6f758115e5a3");

export enum SourceType
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Custom" }
    CUSTOM
}

export predicate sourceTypePredicate(definition is map)
{
    annotation { "Name" : "Source type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
    definition.sourceType is SourceType;
}



const TWO_INCH_SPACING_BOUNDS = {
            (meter) : [1e-05, 0.0254, 100000],
            (inch) : 0.5
        } as LengthBoundSpec;

const ONE_INCH_SPACING_BOUNDS = {
            (meter) : [1e-05, 0.0127, 100000],
            (inch) : 0.5
        } as LengthBoundSpec;

const SPACING_BOUNDS = {
            (meter) : [1e-05, 0.0127, 100000],
            (millimeter) : 5,
            (inch) : 0.5
        } as LengthBoundSpec;

export predicate tubeFacePredicate(definition is map)
{
    if (canHaveTwoInchFace(definition))
    {
        annotation { "Name" : "2 in. face hole count", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.twoInchFaceHoleCount is TwoInchFaceHoleCount;

        if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.TWO || definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.THREE || definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.FOUR)
        {
            annotation { "Name" : "2 in. face spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
            isLength(definition.twoInchFaceSpacing, TWO_INCH_SPACING_BOUNDS);
        }
    }

    if (canHaveOneInchFace(definition))
    {
        annotation { "Name" : "1 in. face hole count", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.oneInchFaceHoleCount is OneInchFaceHoleCount;

        if (definition.oneInchFaceHoleCount == OneInchFaceHoleCount.TWO)
        {
            annotation { "Name" : "1 in. face spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
            isLength(definition.oneInchFaceSpacing, ONE_INCH_SPACING_BOUNDS);
        }
    }

    if (isTubeSizeCustom(definition))
    {
        annotation { "Name" : "First face hole count", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isInteger(definition.firstFaceCount, POSITIVE_COUNT_BOUNDS);

        annotation { "Name" : "First face spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.firstFaceSpacing, SPACING_BOUNDS);

        annotation { "Name" : "Second face hole count", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isInteger(definition.secondFaceCount, POSITIVE_COUNT_BOUNDS);

        annotation { "Name" : "Second face spacing", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.secondFaceSpacing, SPACING_BOUNDS);
    }

    if (hasCustomHoles(definition))
    {
        annotation { "Name" : "Distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.distance, NONNEGATIVE_LENGTH_BOUNDS);
    }

    annotation { "Name" : "Flip pattern start", "Default" : false }
    definition.flipStart is boolean;

    if (hasCustomHoles(definition))
    {
        annotation { "Name" : "Custom start offset", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.customStartOffset is boolean;

        if (definition.customStartOffset)
        {
            annotation { "Name" : "Start offset", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
            isLength(definition.startOffset, NONNEGATIVE_LENGTH_BOUNDS);
        }

        annotation { "Name" : "Custom end offset", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.customEndOffset is boolean;

        if (definition.customEndOffset)
        {
            annotation { "Name" : "End offset", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
            isLength(definition.endOffset, NONNEGATIVE_LENGTH_BOUNDS);
        }
    }
}

export predicate tubeHoleDiameterPredicate(definition is map)
{
    if (!hasPredrilledHoles(definition))
    {
        annotation { "Name" : "Hole size", "Default" : "NO_10", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.holeSize is HoleSize;

        if (isHoleSizeSet(definition))
        {
            annotation { "Name" : "Hole fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
            definition.holeFit is HoleFit;
        }
    }
    else
    {
        annotation { "Name" : "Override hole diameter", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.overrideHoleDiameter is boolean;
    }

    if ((!hasPredrilledHoles(definition) && !isHoleSizeSet(definition)) || (hasPredrilledHoles(definition) && definition.overrideHoleDiameter))
    {
        annotation { "Name" : "Hole diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.holeDiameter, BLEND_BOUNDS);
    }

    annotation { "Name" : "Finish", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.finish is boolean;
}

export predicate wallThicknessPredicate(definition is map)
{
    annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.wallThickness is WallThickness;

    if (definition.wallThickness == WallThickness.CUSTOM)
    {
        annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.customWallThickness, SHELL_OFFSET_BOUNDS);
    }
}

export predicate tubeSizePredicate(definition is map)
{
    annotation { "Name" : "Size", "Default" : "TWO_BY_ONE", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
    definition.tubeSize is TubeSize;

    if (isTubeSizeCustom(definition))
    {
        annotation { "Name" : "First face width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.firstFaceWidth, NONNEGATIVE_LENGTH_BOUNDS);

        annotation { "Name" : "Second face width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
        isLength(definition.secondFaceWidth, NONNEGATIVE_LENGTH_BOUNDS);
    }
    else
    {
        annotation { "Name" : "Type", "Default" : "CUSTOM", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.tubeType is TubeType;

        if (isMaxTube(definition))
        {
            if (isTubeSizeTwoByOne(definition))
            {
                annotation { "Name" : "Pattern type", "Default" : "GRID", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
                definition.maxTubePatternType is MaxTubePatternType;

                if (canBeLight(definition))
                {
                    annotation { "Name" : "Light", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.isLight is boolean;
                }
            }

            annotation { "Name" : "Draw scribe lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.hasScribeLines is boolean;
        }
    }

    if (isTubeSizeCustom(definition) || isTubeTypeCustom(definition))
    {
        wallThicknessPredicate(definition);
    }
}

export predicate tubePredicate(definition is map)
{
    annotation { "Group Name" : "Tube", "Collapsed By Default" : false }
    {
        tubeSizePredicate(definition);
    }

    if (!hasPredrilledHoles(definition))
    {
        annotation { "Name" : "Holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.hasHoles is boolean;
    }
    if (!(!hasPredrilledHoles(definition)) || definition.hasHoles)
    {
        annotation { "Group Name" : "Holes", "Collapsed By Default" : false, "Driving Parameter" : "hasHoles" }
        {
            tubeFacePredicate(definition);

            tubeHoleDiameterPredicate(definition);
        }
    }
}

export predicate robotFramePredicate(definition is map)
{
    tubePredicate(definition);

    frameSelectionPredicate(definition);
}

function getHoleDiameter(definition is map) returns ValueWithUnits
{
    if (hasPredrilledHoles(definition))
    {
        if ((definition.holeSize == HoleSize.NO_8 || definition.holeSize == HoleSize.NO_10))
        {
            return HOLE_SIZES[definition.holeSize][definition.holeFit];
        }
        else if (!definition.overrideHoleDiameter)
        {
            if (isMaxTube(definition))
            {
                return 5 * millimeter;
            }
        }
    }
    return definition.holeDiameter;
}

function getFirstFaceWidth(definition is map) returns ValueWithUnits
{
    if (isTubeSizeTwoByOne(definition))
    {
        return 2 * inch;
    }
    else if (isTubeSizeOneByOne(definition))
    {
        return 1 * inch;
    }
    return definition.firstFaceWidth;
}

function getTwoInchFaceHoleCount(definition is map) returns number
{
    if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.FOUR)
    {
        return 4;
    }
    else if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.THREE)
    {
        return 3;
    }
    else if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.TWO)
    {
        return 2;
    }
    else if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.ONE)
    {
        return 1;
    }
    else if (definition.twoInchFaceHoleCount == TwoInchFaceHoleCount.NONE)
    {
        return;
    }
}

function getSecondFaceWidth(definition is map) returns map
{
    if (isTubeSizeTwoByOne(definition) || isTubeSizeOneByOne(definition))
    {
        return 1 * inch;
    }
    return definition.secondFaceWidth;
}

const ONE_INCH_PATTERN = {
        "distance" : 0.5 * inch,
        "startOffset" : 0.5 * inch,
        "endOffset" : 0.5 * inch,
        "patternCount" : 1
    };

const TWO_INCH_MAX_GRID_PATTERN = {
        "distance" : 0.5 * inch,
        "startOffset" : 0.5 * inch,
        "endOffset" : 0.5 * inch,
        "patternCount" : 3,
        "patternSpacing" : 0.5 * inch
    };

const TWO_INCH_MAX_PATTERN = {
        "distance" : 2 * inch,
        "startOffset" : 0.5 * inch,
        "endOffset" : 0.5 * inch,
        "patternCount" : 3,
        "patternSpacing" : 0.5 * inch
    };

export const MAX_PATTERN = {
        "distance" : 2 * inch,
        "startOffset" : 1.5 * inch,
        "endOffset" : 1 * inch,
        "patternCount" : 1,
        "patternSpacing" : 0 * meter,
        "holeDepth" : 1 * inch,
        "width" : 2 * inch
    };

function getFirstFacePatternDefinition(definition is map) returns map
{
    var tubeFaceDefinition = {};
    if (canHaveTwoInchFace(definition))
    {
        tubeFaceDefinition = { "patternCount" : getTwoInchFaceHoleCount(definition), "patternSpacing" : definition.twoInchFaceSpacing };
    }
    else if (canHaveOneInchFace(definition))
    {
        tubeFaceDefinition = { "patternCount" : getOneInchFaceHoleCount(definition), "patternSpacing" : definition.oneInchFaceSpacing };
    }
    else if (isTubeSizeCustom(definition))
    {
        tubeFaceDefinition = { "patternCount" : definition.firstFaceCount, "patternSpacing" : definition.firstFaceSpacing };
    }
    else if (isMaxTube(definition))
    {
        if (isTubeSizeOneByOne(definition))
        {
            tubeFaceDefinition = ONE_INCH_PATTERN;
        }
        else if (definition.maxTubePatternType == MaxTubePatternType.GRID)
        {
            tubeFaceDefinition = TWO_INCH_MAX_GRID_PATTERN;
        }
        else if (definition.maxTubePatternType == MaxTubePatternType.MAX)
        {
            tubeFaceDefinition = TWO_INCH_MAX_PATTERN;
        }
    }
    return mergeMaps({
                "width" : getFirstFaceWidth(definition),
                "holeDepth" : getSecondFaceWidth(definition),
                "holeDiameter" : getHoleDiameter(definition),
                "distance" : definition.distance,
                "startOffset" : (definition.customStartOffset ? definition.startOffset : 0.5 * inch),
                "endOffset" : (definition.customEndOffset ? definition.endOffset : 0.5 * inch),
                "patternSpacing" : 0 * inch
            }, tubeFaceDefinition);
}

function getSecondFacePatternDefinition(definition is map) returns map
{
    var tubeFaceDefinition = {};
    if (canHaveOneInchFace(definition))
    {
        tubeFaceDefinition = { "patternCount" : getOneInchFaceHoleCount(definition), "patternSpacing" : definition.oneInchFaceSpacing };
    }
    else if (isTubeSizeCustom(definition))
    {
        tubeFaceDefinition = { "patternCount" : definition.secondFaceCount, "patternSpacing" : definition.secondFaceSpacing };
    }
    else if (isMaxTube(definition))
    {
        tubeFaceDefinition = ONE_INCH_PATTERN;
    }
    return mergeMaps({
                "width" : getSecondFaceWidth(definition),
                "holeDepth" : getFirstFaceWidth(definition),
                "holeDiameter" : getHoleDiameter(definition),
                "patternSpacing" : 0 * inch,
                "distance" : definition.distance,
                "startOffset" : (definition.customStartOffset ? definition.startOffset : 0.5 * inch),
                "endOffset" : (definition.customEndOffset ? definition.endOffset : 0.5 * inch)
            }, tubeFaceDefinition);
}

function getWallThickness(definition is map) returns ValueWithUnits
{
    if (definition.wallThickness == WallThickness.ONE_SIXTEENTH)
    {
        return 0.0625 * inch;
    }
    else if (definition.wallThickness == WallThickness.ONE_EIGHTH)
    {
        return 0.125 * inch;
    }
    else if (definition.wallThickness == WallThickness.CUSTOM)
    {
        return definition.customWallThickness;
    }
}

function getMaxTubeProfileType(definition is map) returns MaxTubeProfileType
{
    if (isTubeSizeTwoByOne(definition))
    {
        if (canBeLight(definition) && definition.isLight)
        {
            return MaxTubeProfileType.TWO_BY_ONE_LIGHT;
        }
        return MaxTubeProfileType.TWO_BY_ONE;
    }
    return MaxTubeProfileType.ONE_BY_ONE;
}

function getMaxTubeDefinition(definition is map) returns map
{
    return {
            "maxTubePatternType" : definition.maxTubePatternType,
            "hasScribeLines" : definition.hasScribeLines,
            "maxTubeProfileType" : getMaxTubeProfileType(definition)
        };
}

export function getTubeDefinition(definition is map) returns map
{
    var tubeDefinition = {
        "wallThickness" : getWallThickness(definition),
        "hasHoles" : hasHoles(definition),
        "isMaxTube" : isMaxTube(definition)
    };
    if (tubeDefinition.hasHoles)
    {
        tubeDefinition = mergeMaps(tubeDefinition, {
                    flipStart : definition.flipStart,
                    (TubeFace.FIRST) : getFirstFacePatternDefinition(definition),
                    (TubeFace.SECOND) : getSecondFacePatternDefinition(definition)
                });
    }
    if (tubeDefinition.isMaxTube)
    {
        tubeDefinition = mergeMaps(tubeDefinition, getMaxTubeDefinition(definition));
    }
    return tubeDefinition;
}
