FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "chain/robotChain.fs", version : "");
import(path : "testing.fs", version : "");

export function testSprocketPitchDiameters()
{
    // Published #25 pitch diameters
    expectNear(2 * getSprocketRadius(0.25 * inch, 16), 1.2815 * inch, 0.0001 * inch);
    expectNear(2 * getSprocketRadius(0.25 * inch, 22), 1.7567 * inch, 0.0001 * inch);
    // and #35's
    expectNear(2 * getSprocketRadius(0.375 * inch, 15), 1.8036 * inch, 0.0001 * inch);
}

export function testChainInfo()
{
    expectEqual(getChainInfo(ChainType.ANSI_25).pitch, 0.25 * inch);
    expectEqual(getChainInfo(ChainType.ANSI_35).pitch, 0.375 * inch);
    expectEqual(getChainTypeName(ChainType.ANSI_35), "#35");
    expectEqual(getChainInfo(ChainType.ISO_05B).pitch, 8 * millimeter);
    expectEqual(getChainTypeName(ChainType.ANSI_25H), "#25H");
    // #25H is wider than #25, over its thicker plates
    expectTrue(getChainInfo(ChainType.ANSI_25H).width > getChainInfo(ChainType.ANSI_25).width, "#25H should be wider than #25");
}

export function testSameSprockets()
{
    // #25H runs on #25's sprockets
    expectTrue(sameSprockets(ChainType.ANSI_25, ChainType.ANSI_25H), "#25 sprockets should fit #25H chain");
    expectTrue(sameSprockets(ChainType.ANSI_35, ChainType.ANSI_35), "a chain's sprockets should fit it");
    expectTrue(!sameSprockets(ChainType.ANSI_25, ChainType.ANSI_35), "#25 sprockets shouldn't fit #35 chain");
}

export function testFlipManipulatorSwapsSide()
{
    const definition = {
            "sprockets" : [{ "chainSide" : ChainSide.INSIDE }, { "chainSide" : ChainSide.INSIDE }],
            "startOffset" : false
        };
    const flipped = robotChainManipulatorChange(newContext(), definition, { "chainSideFlipManipulator.1" : { "flipped" : true } });
    expectEqual(flipped.sprockets[0].chainSide, ChainSide.INSIDE);
    expectEqual(flipped.sprockets[1].chainSide, ChainSide.OUTSIDE);
}

export function testPitchCircleTeeth()
{
    expectNear(pitchCircleTeeth(0.25 * inch, getSprocketRadius(0.25 * inch, 22)), 22, 1e-9);
    expectEqual(pitchCircleTeeth(0.25 * inch, 0.1 * inch), undefined);
}
