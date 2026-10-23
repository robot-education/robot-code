FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");

export enum ColorScope
{
    annotation { "Name" : "Selected" }
    SELECTED,
    annotation { "Name" : "All" }
    ALL
}

/**
 * FRCDesign's standard colors, as FRCDesignLib's color standards list them, in their order.
 */
export enum StandardColor
{
    annotation { "Name" : "General white (#FFFFFF)" }
    GENERAL_WHITE,
    annotation { "Name" : "Light grey (#E6E6E6)" }
    LIGHT_GREY,
    annotation { "Name" : "Medium grey (#C6C6C6)" }
    MEDIUM_GREY,
    annotation { "Name" : "Steel & neodymium grey (#B3B3B3)" }
    STEEL_GREY,
    annotation { "Name" : "Lighter black (#404040)" }
    LIGHTER_BLACK,
    annotation { "Name" : "General black (#373737)" }
    GENERAL_BLACK,
    annotation { "Name" : "Dark black (#191919)" }
    DARK_BLACK,
    annotation { "Name" : "Clear polycarbonate (#E6E6E6, 80% opaque)" }
    CLEAR_POLYCARBONATE,
    annotation { "Name" : "Smoked polycarbonate grey (#404040, 80% opaque)" }
    SMOKED_POLYCARBONATE,
    annotation { "Name" : "Machined brass yellow (#FDE086)" }
    MACHINED_BRASS_YELLOW,
    annotation { "Name" : "Brass yellow (#F6D681)" }
    BRASS_YELLOW,
    annotation { "Name" : "Brass/gold (#FFC10E)" }
    BRASS_GOLD,
    annotation { "Name" : "Dark yellow (#D7A01E)" }
    DARK_YELLOW,
    annotation { "Name" : "REV orange (#FE8B00)" }
    REV_ORANGE,
    annotation { "Name" : "Red (#C90202)" }
    RED,
    annotation { "Name" : "Dark red (#B11919)" }
    DARK_RED,
    annotation { "Name" : "PCB green (#008000)" }
    PCB_GREEN,
    annotation { "Name" : "Copper (#FCBC84)" }
    COPPER,
    annotation { "Name" : "Bearing recess (#CAC1A4)" }
    BEARING_RECESS,
    annotation { "Name" : "CAN high (#FFD80E)" }
    CAN_HIGH,
    annotation { "Name" : "CAN low (#59B031)" }
    CAN_LOW,
    annotation { "Name" : "Blue nitrile (#2D5DC5)" }
    BLUE_NITRILE,
    annotation { "Name" : "Limelight camera lens (#48473D)" }
    LIMELIGHT_LENS,
    annotation { "Name" : "Robot status light (#F88701)" }
    STATUS_LIGHT,
    annotation { "Name" : "Slightly yellowed plastic (#FDFBE0)" }
    YELLOWED_PLASTIC,
    annotation { "Name" : "WCP aluminum gears (#DFD4B6)" }
    WCP_ALUMINUM_GEARS,
    annotation { "Name" : "RedLine body (#CFDBE5)" }
    REDLINE_BODY,
    annotation { "Name" : "AndyMark aluminum gears (#A69E96)" }
    ANDYMARK_ALUMINUM_GEARS,
    annotation { "Name" : "Zinc-coated steel (nuts) (#E1E7EC)" }
    ZINC_COATED_STEEL,
    annotation { "Name" : "Dark grey (#787880)" }
    DARK_GREY,
    annotation { "Name" : "Aluminum gears (#DED3B5)" }
    ALUMINUM_GEARS,
    annotation { "Name" : "Wedgetop tread (#EDC57F)" }
    WEDGETOP_TREAD,
    annotation { "Name" : "Bronze bushings (#EBCDA6)" }
    BRONZE_BUSHINGS,
    annotation { "Name" : "Brass pneumatics (#CFA650)" }
    BRASS_PNEUMATICS
}

export const STANDARD_COLORS = {
        StandardColor.GENERAL_WHITE : hexColor("#FFFFFF"),
        StandardColor.LIGHT_GREY : hexColor("#E6E6E6"),
        StandardColor.MEDIUM_GREY : hexColor("#C6C6C6"),
        StandardColor.STEEL_GREY : hexColor("#B3B3B3"),
        StandardColor.LIGHTER_BLACK : hexColor("#404040"),
        StandardColor.GENERAL_BLACK : hexColor("#373737"),
        StandardColor.DARK_BLACK : hexColor("#191919"),
        StandardColor.CLEAR_POLYCARBONATE : translucent(hexColor("#E6E6E6"), 0.8),
        StandardColor.SMOKED_POLYCARBONATE : translucent(hexColor("#404040"), 0.8),
        StandardColor.MACHINED_BRASS_YELLOW : hexColor("#FDE086"),
        StandardColor.BRASS_YELLOW : hexColor("#F6D681"),
        StandardColor.BRASS_GOLD : hexColor("#FFC10E"),
        StandardColor.DARK_YELLOW : hexColor("#D7A01E"),
        StandardColor.REV_ORANGE : hexColor("#FE8B00"),
        StandardColor.RED : hexColor("#C90202"),
        StandardColor.DARK_RED : hexColor("#B11919"),
        StandardColor.PCB_GREEN : hexColor("#008000"),
        StandardColor.COPPER : hexColor("#FCBC84"),
        StandardColor.BEARING_RECESS : hexColor("#CAC1A4"),
        StandardColor.CAN_HIGH : hexColor("#FFD80E"),
        StandardColor.CAN_LOW : hexColor("#59B031"),
        StandardColor.BLUE_NITRILE : hexColor("#2D5DC5"),
        StandardColor.LIMELIGHT_LENS : hexColor("#48473D"),
        StandardColor.STATUS_LIGHT : hexColor("#F88701"),
        StandardColor.YELLOWED_PLASTIC : hexColor("#FDFBE0"),
        StandardColor.WCP_ALUMINUM_GEARS : hexColor("#DFD4B6"),
        StandardColor.REDLINE_BODY : hexColor("#CFDBE5"),
        StandardColor.ANDYMARK_ALUMINUM_GEARS : hexColor("#A69E96"),
        StandardColor.ZINC_COATED_STEEL : hexColor("#E1E7EC"),
        StandardColor.DARK_GREY : hexColor("#787880"),
        StandardColor.ALUMINUM_GEARS : hexColor("#DED3B5"),
        StandardColor.WEDGETOP_TREAD : hexColor("#EDC57F"),
        StandardColor.BRONZE_BUSHINGS : hexColor("#EBCDA6"),
        StandardColor.BRASS_PNEUMATICS : hexColor("#CFA650")
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
            annotation { "Name" : "Color", "Default" : "#373737", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "A hex code, like #373737 or #FFF, or RGB from 0 to 255, like 55, 55, 55." }
            definition.color is string;
        }
        else
        {
            annotation { "Name" : "Standard color", "Default" : StandardColor.GENERAL_BLACK, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
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
        throw regenError("Enter a hex code, like #373737, or RGB from 0 to 255, like 55, 55, 55.", ["color"]);
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
 * A color made see-through: `alpha` from 0, clear, to 1, opaque.
 */
export function translucent(opaque is Color, alpha is number) returns Color
{
    return color(opaque.red, opaque.green, opaque.blue, alpha);
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
