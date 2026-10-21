FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "quickFastener/quickFastener.fs", version : "");
import(path : "testing.fs", version : "");

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
            const length = 2 * inch;
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

export function testEveryFlatHeadIsShorterThanItsScrews()
{
    for (var _, sizes in SCREWS)
    {
        for (var size, heads in sizes)
        {
            const flat = heads[ScrewHead.FLAT];
            if (flat == undefined)
            {
                continue;
            }
            for (var length, _ in flat)
            {
                expectTrue(SCREW_LENGTHS[length].length > flatHeadHeight(FASTENER_SIZES[size], SCREW_HEADS[size][ScrewHead.FLAT]),
                    "Flat head longer than its screw");
            }
        }
    }
}

export function testWasherNames()
{
    expectEqual(washerName({ "size" : FastenerSize.NO_10_32, "washerType" : WasherType.OD_1_2_X_0_049 }), "#10 Washer (1/2 in. OD x 0.049 in.)");
    expectEqual(washerName({ "size" : FastenerSize.QUARTER_20, "washerType" : WasherType.OD_5_8_X_0_065 }), "1/4 in. Washer (5/8 in. OD x 0.065 in.)");
}

function expectSamePoint(actual is Vector, expected is Vector)
{
    expectNear(norm(actual - expected), 0 * meter, 1e-9 * meter);
}
