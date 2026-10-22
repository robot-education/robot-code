FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "color/robotColor.fs", version : "");
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

// Each standard color's name gives its hex code
export function testStandardColorsAreTheirNamedHexCodes()
{
    const named = {
            StandardColor.BLACK : "#4D4D4D",
            StandardColor.WHITE : "#EAEAEA",
            StandardColor.LIGHT_GRAY : "#E6E6E6",
            StandardColor.MEDIUM_GRAY : "#A6A6A6",
            StandardColor.STEEL_GRAY : "#B3B3B3",
            StandardColor.UNCOATED_STEEL_GRAY : "#B8C0D4",
            StandardColor.BRASS_YELLOW : "#F5D578",
            StandardColor.COPPER : "#FCBC84",
            StandardColor.REV_ORANGE : "#FE8B00",
            StandardColor.PRINTED_GREEN : "#4DD188"
        };
    expectEqual(size(named), size(STANDARD_COLORS));
    for (var standard, hex in named)
    {
        expectEqual(STANDARD_COLORS[standard], hexColor(hex));
    }
}
