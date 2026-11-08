FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "color/robotColor.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "testing.fs", version : "");

export function testHexCodesWithOrWithoutTheirHash()
{
    expectEqual(hexColor("#4D4D4D"), rgbColor(77, 77, 77));
    expectEqual(hexColor("fe8b00"), rgbColor(254, 139, 0));
    expectEqual(hexColor(" #Fe8B00 "), rgbColor(254, 139, 0));
}

export function testShortHexCodesDoubleEachDigit()
{
    expectEqual(hexColor("#FFF"), rgbColor(255, 255, 255));
    expectEqual(hexColor("4d8"), rgbColor(68, 221, 136));
}

export function testWhatIsNoHexCode()
{
    for (var code in ["", "#", "#4D4D4", "#4D4D4D4D", "#GGGGGG", "4D 4D 4D"])
    {
        expectEqual(hexColor(code), undefined);
    }
}

export function testRgbChannelsRunTo255()
{
    expectEqual(rgbColor(255, 0, 51), color(1, 0, 0.2));
}

export function testColorsByHexOrRgb()
{
    expectEqual(parseColor("#4D4D4D"), rgbColor(77, 77, 77));
    expectEqual(parseColor("#FFF"), rgbColor(255, 255, 255));
    for (var rgb in ["77, 77, 77", "77,77,77", "77 77 77", " rgb(77, 77, 77) ", "RGB(77 77 77)"])
    {
        expectEqual(parseColor(rgb), rgbColor(77, 77, 77));
    }
    // Three digits alone are a hex code, as CSS reads them
    expectEqual(parseColor("777"), rgbColor(119, 119, 119));
}

export function testWhatIsNoColor()
{
    for (var text in ["", "77, 77", "77, 77, 77, 77", "256, 0, 0", "rgb(77, 77, 77", "red", "-1, 0, 0"])
    {
        expectEqual(parseColor(text), undefined);
    }
}

// Each standard color's name gives its hex code, and robotProperties' constants are the standards they're named for
export function testStandardColorsAreTheirNamedHexCodes()
{
    expectEqual(size(STANDARD_COLORS), 34);
    expectEqual(STANDARD_COLORS[StandardColor.GENERAL_BLACK], hexColor("#373737"));
    expectEqual(STANDARD_COLORS[StandardColor.ZINC_COATED_STEEL], hexColor("#E1E7EC"));
    expectEqual(STANDARD_COLORS[StandardColor.CLEAR_POLYCARBONATE], color(230 / 255, 230 / 255, 230 / 255, 0.8));
    const constants = {
            StandardColor.GENERAL_WHITE : WHITE,
            StandardColor.LIGHT_GREY : LIGHT_GRAY,
            StandardColor.MEDIUM_GREY : MEDIUM_GRAY,
            StandardColor.STEEL_GREY : STEEL_GRAY,
            StandardColor.GENERAL_BLACK : BLACK,
            StandardColor.MACHINED_BRASS_YELLOW : BRASS_YELLOW,
            StandardColor.REV_ORANGE : REV_ORANGE,
            StandardColor.COPPER : COPPER,
            StandardColor.ZINC_COATED_STEEL : ZINC_COATED_STEEL
        };
    for (var standard, constant in constants)
    {
        expectEqual(STANDARD_COLORS[standard], constant);
    }
}
