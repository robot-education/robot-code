FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "quickFastener/quickFastener.fs", version : "");
import(path : "testing.fs", version : "");

export function testWasherThickness()
{
    expectEqual(washerThickness(FastenerSize.NO_10_32, SeatWasher.NONE), 0 * meter);
    expectNear(washerThickness(FastenerSize.NO_10_32, SeatWasher.FLAT), 0.049 * inch, 1e-9 * meter);
    expectNear(washerThickness(FastenerSize.M3, SeatWasher.WIDE), 0.8 * millimeter, 1e-9 * meter);
}

export function testFlatHeadMeetsShankAtCountersinkAngle()
{
    const size = FASTENER_SIZES[FastenerSize.NO_10_32];
    const head = SCREW_HEADS[FastenerSize.NO_10_32][ScrewHead.FLAT];
    // 82 degrees: (0.411 - 0.19) / 2 / tan(41 degrees)
    expectNear(flatHeadHeight(size, head), 0.12711 * inch, 0.00001 * inch);
}

export function testProfilesCloseOnTheAxis()
{
    for (var sizeId, spec in FASTENER_SIZES)
    {
        for (var headType, head in SCREW_HEADS[sizeId])
        {
            const length = spec.lengths[size(spec.lengths) - 1];
            const points = screwProfile(spec, head, headType, length);
            // Down to the end of the shank, and back along the axis
            expectSamePoint(points[size(points) - 2], vector(0 * meter, -length));
            if (headType == ScrewHead.BUTTON)
            {
                // The dome closes it, from the axis to the first point
                const dome = buttonDome(head);
                expectSamePoint(dome.start, points[0]);
                expectSamePoint(dome.end, points[size(points) - 1]);
            }
            else
            {
                expectSamePoint(points[0], points[size(points) - 1]);
            }
        }
    }
}

export function testButtonDomeIsAnArcCenteredOnTheAxis()
{
    const dome = buttonDome(SCREW_HEADS[FastenerSize.NO_10_32][ScrewHead.BUTTON]);
    // The middle point is as far from the center as both ends
    const rise = dome.end[1] - dome.start[1];
    const radius = (dome.start[0] ^ 2 + rise ^ 2) / (2 * rise);
    const center = vector(0 * meter, dome.end[1] - radius);
    expectNear(norm(dome.mid - center), radius, 1e-9 * meter);
    expectNear(norm(dome.start - center), radius, 1e-9 * meter);
}

export function testLengthNames()
{
    expectEqual(lengthName(FASTENER_SIZES[FastenerSize.NO_10_32], 0.5 * inch), "0.5 in.");
    expectEqual(lengthName(FASTENER_SIZES[FastenerSize.M3], 10 * millimeter), "10 mm");
}

export function testEveryScrewLengthIsInBounds()
{
    for (var _, spec in FASTENER_SIZES)
    {
        for (var length in spec.lengths)
        {
            expectTrue(length > 0 * meter && length <= 12 * inch, "Length out of bounds");
        }
    }
}

function expectSamePoint(actual is Vector, expected is Vector)
{
    expectNear(norm(actual - expected), 0 * meter, 1e-9 * meter);
}
