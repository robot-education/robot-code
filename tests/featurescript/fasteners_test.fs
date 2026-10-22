FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "fasteners/fasteners.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
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
            // Down to its tip, chamfered, and back up the axis
            const tip = indexOf(points, vector(0 * meter, -length));
            expectSamePoint(points[tip - 1], vector(spec.majorDiameter / 2 - tipChamfer(spec), -length));
            expectEqual(points[tip + 1][0], 0 * meter);
            if (headType == ScrewHead.BUTTON)
            {
                // The dome closes it, from the head's side to its flat top
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
    const head = SCREW_HEADS[FastenerSize.QUARTER_20][ScrewHead.BUTTON];
    const dome = buttonDome(head);
    // A circle through all three points, centered on the axis
    const center = vector(0 * meter, (squaredNorm(dome.end) - squaredNorm(dome.start)) / (2 * (dome.end[1] - dome.start[1])));
    const radius = norm(dome.start - center);
    expectNear(norm(dome.mid - center), radius, 1e-9 * meter);
    expectNear(norm(dome.end - center), radius, 1e-9 * meter);
    // Its flat top is wider than its socket's corners
    expectTrue(dome.end[0] > head.socketSize / sqrt(3), "Socket wider than the flat top");
    expectSamePoint(dome.end, vector(1.4 * head.socketSize / 2, head.headHeight));
}

export function testStandardSocketHeadsAreThreaded2DPlusHalfAnInch()
{
    const size = FASTENER_SIZES[FastenerSize.QUARTER_20];
    const socket = SCREW_HEADS[FastenerSize.QUARTER_20][ScrewHead.SOCKET];
    expectNear(threadLength(size, socket, ScrewHead.SOCKET, 3 * inch, false), 1 * inch, 1e-9 * meter);
    expectNear(threadLength(size, socket, ScrewHead.SOCKET, 3 * inch, true), 3 * inch, 1e-9 * meter);
    // Short ones all the way up
    expectNear(threadLength(size, socket, ScrewHead.SOCKET, 0.5 * inch, false), 0.5 * inch, 1e-9 * meter);
    // A flat head's up to its head
    const flat = SCREW_HEADS[FastenerSize.QUARTER_20][ScrewHead.FLAT];
    expectNear(threadLength(size, flat, ScrewHead.FLAT, 1 * inch, false), 1 * inch - flatHeadDepth(size, flat), 1e-9 * meter);
}

export function testSocketsStayInsideTheirHeads()
{
    for (var sizeId, spec in FASTENER_SIZES)
    {
        for (var headType, head in SCREW_HEADS[sizeId])
        {
            const height = headType == ScrewHead.FLAT ? flatHeadDepth(spec, head) : head.headHeight;
            expectTrue(socketDepth(spec, head, headType) < height, "Socket deeper than its head");
        }
    }
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
            for (var _, lengths in flat)
            {
                for (var length, _ in lengths)
                {
                    const spec = FASTENER_SIZES[size];
                    expectTrue(SCREW_LENGTHS[length].length > flatHeadDepth(spec, SCREW_HEADS[size][ScrewHead.FLAT]) + tipChamfer(spec),
                        "Flat head and tip longer than its screw");
                }
            }
        }
    }
}

export function testWasherNames()
{
    expectEqual(WASHERS[FastenerVendor.GENERIC][FastenerSize.NO_10_32][WasherType.OD_1_2_X_0_049][FastenerFinish.BLACK].name, "#10 Washer (1/2 in. OD x 0.049 in.)");
    expectEqual(WASHERS[FastenerVendor.GENERIC][FastenerSize.QUARTER_20][WasherType.OD_5_8_X_0_065][FastenerFinish.BLACK].name, "1/4 in. Washer (5/8 in. OD x 0.065 in.)");
}

export function testFinishesSetTheirMaterialAndAppearance()
{
    const finishes = SCREWS[FastenerVendor.WCP][FastenerSize.NO_10_32][ScrewHead.BUTTON];
    expectEqual(finishes[FastenerFinish.BLACK][ScrewLength.IN_0_25].standard.material, STEEL);
    expectEqual(finishes[FastenerFinish.BLACK][ScrewLength.IN_0_25].standard.appearance, BLACK);
    expectEqual(finishes[FastenerFinish.ALUMINUM][ScrewLength.IN_0_25].standard.material, ALUMINUM);
}

export function testLongSocketHeadsComeFullyThreadedToo()
{
    const threads = SCREWS[FastenerVendor.MCMASTER][FastenerSize.NO_8_32][ScrewHead.SOCKET][FastenerFinish.BLACK][ScrewLength.IN_2];
    expectEqual(threads.standard.partNumber, "91251A205");
    expectEqual(threads.fullThread.partNumber, "90044A113");
}

function expectSamePoint(actual is Vector, expected is Vector)
{
    expectNear(norm(actual - expected), 0 * meter, 1e-9 * meter);
}
