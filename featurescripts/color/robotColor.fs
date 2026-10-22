FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");

export enum ColorScope
{
    annotation { "Name" : "Selected" }
    SELECTED,
    annotation { "Name" : "All" }
    ALL
}

/**
 * FRCDesign's standard appearances, as `core/robotProperties.fs` defines them (see `STANDARD_COLORS`).
 */
export enum StandardColor
{
    annotation { "Name" : "General black (#4D4D4D)" }
    BLACK,
    annotation { "Name" : "General white (#EAEAEA)" }
    WHITE,
    annotation { "Name" : "Light gray (#E6E6E6)" }
    LIGHT_GRAY,
    annotation { "Name" : "Medium gray (#A6A6A6)" }
    MEDIUM_GRAY,
    annotation { "Name" : "Steel (#B3B3B3)" }
    STEEL_GRAY,
    annotation { "Name" : "Uncoated steel (#B8C0D4)" }
    UNCOATED_STEEL_GRAY,
    annotation { "Name" : "Machined brass yellow (#F5D578)" }
    BRASS_YELLOW,
    annotation { "Name" : "Copper (#FCBC84)" }
    COPPER,
    annotation { "Name" : "REV orange (#FE8B00)" }
    REV_ORANGE,
    annotation { "Name" : "3D printed green (#4DD188)" }
    PRINTED_GREEN
}

export const STANDARD_COLORS = {
        StandardColor.BLACK : BLACK,
        StandardColor.WHITE : WHITE,
        StandardColor.LIGHT_GRAY : LIGHT_GRAY,
        StandardColor.MEDIUM_GRAY : MEDIUM_GRAY,
        StandardColor.STEEL_GRAY : STEEL_GRAY,
        StandardColor.UNCOATED_STEEL_GRAY : UNCOATED_STEEL_GRAY,
        StandardColor.BRASS_YELLOW : BRASS_YELLOW,
        StandardColor.COPPER : COPPER,
        StandardColor.REV_ORANGE : REV_ORANGE,
        StandardColor.PRINTED_GREEN : PRINTED_GREEN
    };

annotation { "Feature Type Name" : "Robot color",
        "Feature Type Description" : "Give parts one of FRCDesign's standard appearances, or any color by hex code or RGB." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA }
export const robotColor = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Parts", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.scope is ColorScope;

        if (colorsSelectedParts(definition))
        {
            annotation { "Name" : "Parts to color", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES }
            definition.parts is Query;
        }

        annotation { "Name" : "Custom color", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.customColor is boolean;

        if (definition.customColor)
        {
            annotation { "Name" : "Color", "Default" : "#4D4D4D", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "A hex code, like #4D4D4D or #FFF, or RGB from 0 to 255, like 77, 77, 77." }
            definition.color is string;
        }
        else
        {
            annotation { "Name" : "Standard color", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.standardColor is StandardColor;
        }
    }
    {
        const parts = colorsSelectedParts(definition) ? definition.parts : qAllModifiableSolidBodies();
        if (isQueryEmpty(context, parts))
        {
            if (colorsSelectedParts(definition))
            {
                throw regenError("Select parts to color.", ["parts"]);
            }
            reportFeatureInfo(context, id, "There are no parts to color.");
            return;
        }
        setProperty(context, { "entities" : parts, "propertyType" : PropertyType.APPEARANCE, "value" : chosenColor(definition) });
    });

predicate colorsSelectedParts(definition is map)
{
    definition.scope == ColorScope.SELECTED;
}

function chosenColor(definition is map) returns Color
{
    if (!definition.customColor)
    {
        return STANDARD_COLORS[definition.standardColor];
    }
    const parsed = parseColor(definition.color);
    if (parsed == undefined)
    {
        throw regenError("Enter a hex code, like #4D4D4D, or RGB from 0 to 255, like 77, 77, 77.", ["color"]);
    }
    return parsed;
}

/**
 * The color a string names: a hex code (see `hexColor`), or red, green, and blue from 0 to 255, apart by commas or
 * spaces, and in `rgb(...)` or not (`77, 77, 77`, `77 77 77`, `rgb(77, 77, 77)`); undefined if it names none.
 */
export function parseColor(text is string)
{
    const hex = hexColor(text);
    if (hex != undefined)
    {
        return hex;
    }
    for (var pattern in ["\\s*[rR][gG][bB]\\s*\\((.*)\\)\\s*", "(.*)"])
    {
        const found = match(text, pattern);
        if (!found.hasMatch)
        {
            continue;
        }
        const channels = match(found.captures[1], "\\s*(\\d{1,3})\\s*[,\\s]\\s*(\\d{1,3})\\s*[,\\s]\\s*(\\d{1,3})\\s*");
        if (!channels.hasMatch)
        {
            return undefined;
        }
        var values = [];
        for (var channel in subArray(channels.captures, 1, 4))
        {
            const value = stringToNumber(channel);
            if (value > 255)
            {
                return undefined;
            }
            values = append(values, value);
        }
        return rgbColor(values[0], values[1], values[2]);
    }
    return undefined;
}

/**
 * A color from red, green, and blue channels, each from 0 to 255.
 */
export function rgbColor(red is number, green is number, blue is number) returns Color
{
    return color(red / 255, green / 255, blue / 255);
}

const HEX_DIGITS = {
        "0" : 0, "1" : 1, "2" : 2, "3" : 3, "4" : 4, "5" : 5, "6" : 6, "7" : 7, "8" : 8, "9" : 9,
        "a" : 10, "b" : 11, "c" : 12, "d" : 13, "e" : 14, "f" : 15,
        "A" : 10, "B" : 11, "C" : 12, "D" : 13, "E" : 14, "F" : 15
    };

/**
 * The color a hex code names: 6 digits (`#4D4D4D`) or 3 (`#FFF`, each digit doubled), with or without the `#`, and
 * spaces around it ignored; undefined if it names none.
 */
export function hexColor(code is string)
{
    const found = match(code, "\\s*#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})\\s*");
    if (!found.hasMatch)
    {
        return undefined;
    }
    var digits = splitIntoCharacters(found.captures[1]);
    if (size(digits) == 3)
    {
        digits = [digits[0], digits[0], digits[1], digits[1], digits[2], digits[2]];
    }
    var channels = [];
    for (var i = 0; i < 6; i += 2)
    {
        channels = append(channels, HEX_DIGITS[digits[i]] * 16 + HEX_DIGITS[digits[i + 1]]);
    }
    return rgbColor(channels[0], channels[1], channels[2]);
}
