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

export function testMetricSocketHeadsAreThreaded2DPlus12Millimeters()
{
    const size = FASTENER_SIZES[FastenerSize.M4];
    const socket = SCREW_HEADS[FastenerSize.M4][ScrewHead.SOCKET];
    expectNear(threadLength(size, socket, ScrewHead.SOCKET, 40 * millimeter, false), 20 * millimeter, 1e-9 * meter);
}

export function testVendorsFullyThreadedLengthsSaySo()
{
    // goBILDA's are threaded their whole length but its 75 mm
    const gobilda = SCREWS[FastenerVendor.GOBILDA][FastenerSize.M4][ScrewHead.SOCKET][ScrewFinish.ZINC_PLATED];
    expectEqual(gobilda[ScrewLength.MM_60].standard.fullyThreaded, true);
    expectEqual(gobilda[ScrewLength.MM_75].standard.fullyThreaded, undefined);
}

export function testHexHeadProfilesClose()
{
    for (var sizeId, heads in SCREW_HEADS)
    {
        for (var headType, head in heads)
        {
            if (!isHexHead(headType))
            {
                continue;
            }
            const points = hexHeadProfile(head);
            expectSamePoint(points[0], points[size(points) - 1]);
            // Chamfered inside its corners, out to its flats at its top
            expectSamePoint(points[3], vector(head.acrossFlats / 2, head.headHeight));
            expectTrue(points[1][0] > head.headDiameter / 2, "Its chamfer starts past its corners");
        }
    }
}

export function testFlatHeadsSitInTheirSizesCountersinks()
{
    expectEqual(SCREW_HEADS[FastenerSize.NO_10_32][ScrewHead.FLAT].countersinkAngle, 82 * degree);
    expectEqual(SCREW_HEADS[FastenerSize.M4][ScrewHead.FLAT].countersinkAngle, 90 * degree);
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
    expectEqual(WASHERS[FastenerVendor.GENERIC][FastenerSize.NO_10_32][WasherType.OD_1_2_X_0_049][WasherFinish.BLACK].name, "#10 Washer (1/2 in OD x 0.049 in)");
    expectEqual(WASHERS[FastenerVendor.GENERIC][FastenerSize.QUARTER_20][WasherType.OD_5_8_X_0_065][WasherFinish.BLACK].name, "1/4 in Washer (5/8 in OD x 0.065 in)");
}

export function testFinishesSetTheirMaterialAndAppearance()
{
    const finishes = SCREWS[FastenerVendor.WCP][FastenerSize.NO_10_32][ScrewHead.BUTTON];
    expectEqual(finishes[ScrewFinish.BLACK][ScrewLength.IN_0_25].standard.material, STEEL);
    expectEqual(finishes[ScrewFinish.BLACK][ScrewLength.IN_0_25].standard.appearance, BLACK);
    expectEqual(finishes[ScrewFinish.ALUMINUM][ScrewLength.IN_0_25].standard.material, ALUMINUM);
}

export function testLongSocketHeadsComeFullyThreadedToo()
{
    const threads = SCREWS[FastenerVendor.MCMASTER][FastenerSize.NO_8_32][ScrewHead.SOCKET][ScrewFinish.BLACK][ScrewLength.IN_2];
    expectEqual(threads.standard.partNumber, "91251A205");
    expectEqual(threads.fullThread.partNumber, "90044A113");
}

export function testNutProfilesClose()
{
    for (var vendor, sizes in NUTS)
    {
        for (var sizeId, types in sizes)
        {
            for (var nutType, finishes in types)
            {
                for (var finish, nut in finishes)
                {
                    const points = nutProfile(FASTENER_SIZES[sizeId], nut, nutType);
                    if (nutType == NutType.NYLOCK || nutType == NutType.THIN_NYLOCK)
                    {
                        // The dome closes it, from the top of its hex to its flat top
                        const dome = nylockDome(nut);
                        expectSamePoint(dome.start, points[size(points) - 1]);
                        expectSamePoint(dome.end, points[0]);
                    }
                    else
                    {
                        expectSamePoint(points[0], points[size(points) - 1]);
                    }
                }
            }
        }
    }
}

export function testNylonInsertsFitInsideTheirDomes()
{
    for (var vendor, sizes in NUTS)
    {
        for (var sizeId, types in sizes)
        {
            for (var nutType, finishes in types)
            {
                for (var finish, nut in finishes)
                {
                    if (nut.insertDiameter == undefined)
                    {
                        continue;
                    }
                    const dome = nylockDome(nut);
                    expectTrue(nut.hexHeight < nut.height, "a nylock's hex is shorter than it");
                    expectTrue(nut.insertDiameter / 2 < dome.end[0], "its insert is inside its flat top");
                    expectTrue(FASTENER_SIZES[sizeId].tapDrillDiameter < nut.insertDiameter, "its insert is wider than its bore");
                }
            }
        }
    }
}

export function testNutsMatchMcMasterCarrsModels()
{
    const nuts = NUTS[FastenerVendor.MCMASTER][FastenerSize.NO_10_32];
    const zinc = NutFinish.ZINC_PLATED;
    expectNear(nuts[NutType.THIN_NYLOCK][zinc].height, 11 / 64 * inch, 1e-6 * inch);
    expectNear(nuts[NutType.NYLOCK][zinc].height - nuts[NutType.NYLOCK][zinc].hexHeight, 5 / 128 * inch, 1e-6 * inch);
    expectNear(NUTS[FastenerVendor.MCMASTER][FastenerSize.NO_6_32][NutType.THIN_HEX][zinc].acrossFlats, 1 / 4 * inch, 1e-6 * inch);
    // And its metric ones (90591A255 and 94645A101)
    const m4 = NUTS[FastenerVendor.MCMASTER][FastenerSize.M4];
    expectNear(m4[NutType.HEX][zinc].height, 3.2 * millimeter, 1e-9 * meter);
    expectNear(m4[NutType.NYLOCK][zinc].hexHeight, 4.17 * millimeter, 1e-9 * meter);
}

function expectSamePoint(actual is Vector, expected is Vector)
{
    expectNear(norm(actual - expected), 0 * meter, 1e-9 * meter);
}

export function testAHiddenInputResolvesToTheOneMade()
{
    // FRC's nuts come in zinc plating alone, so any finish resolves to it
    const nylocks = NUTS[FastenerVendor.MCMASTER][FastenerSize.NO_10_32][NutType.NYLOCK];
    expectEqual(resolve(nylocks, NutFinish.BLACK), NutFinish.ZINC_PLATED);
    expectEqual(resolve({ "a" : 1, "b" : 2 }, "b"), "b");
    expectEqual(resolve({ "a" : 1, "b" : 2 }, "c"), undefined);
}

export function testAHiddenSizeResolvesWithinItsOwnPartSet()
{
    // REV makes #10-32 screws for FRC and M3 ones for FTC: each studio's hidden Size holds one of its own set's sizes
    const parameters = ["vendor", "size", "head", "finish", "length"];
    const ftc = resolveChoices(SCREWS, { "vendor" : FastenerVendor.REV, "size" : FastenerSize.M4, "head" : ScrewHead.SOCKET,
                "finish" : ScrewFinish.STAINLESS, "length" : ScrewLength.MM_20 }, parameters);
    expectEqual(ftc.definition.size, FastenerSize.M3);
    expectEqual(ftc.definition.head, ScrewHead.HEX);
    expectEqual(ftc.made, SCREWS[FastenerVendor.REV][FastenerSize.M3][ScrewHead.HEX][ftc.definition.finish][ScrewLength.MM_20]);
    const frc = resolveChoices(SCREWS, { "vendor" : FastenerVendor.REV, "size" : FastenerSize.NO_8_32, "head" : ScrewHead.SOCKET,
                "finish" : ScrewFinish.BLACK, "length" : ScrewLength.IN_0_5 }, parameters);
    expectEqual(frc.definition.size, FastenerSize.NO_10_32);
    // McMaster-Carr's e-clips come in four sizes for FRC, and one for FTC
    const eClip = resolveChoices(RETAINERS[RetainerType.E_CLIP], { "vendor" : RetainerVendor.MCMASTER, "size" : RetainerSize.MM_12 },
        ["vendor", "size"]);
    expectEqual(eClip.definition.size, RetainerSize.MM_8);
    // Nothing of goBILDA's comes in #10-32
    const unmade = resolveChoices(NUTS, { "vendor" : FastenerVendor.GOBILDA, "size" : FastenerSize.NO_10_32,
                "nutType" : NutType.NYLOCK, "finish" : NutFinish.ZINC_PLATED }, ["vendor", "size", "nutType", "finish"]);
    expectEqual(unmade.unmade, "size");
}

export function testRetainerOutlinesClose()
{
    for (var retainerType, vendors in RETAINERS)
    {
        for (var vendor, sizes in vendors)
        {
            for (var sizeId, retainer in sizes)
            {
                // Each curve's ends meet exactly one other's
                const curves = retainerProfile(retainer);
                for (var curve in curves)
                {
                    for (var point in [curve.start, curve.end])
                    {
                        var meeting = 0;
                        for (var other in curves)
                        {
                            for (var end in [other.start, other.end])
                            {
                                if (norm(end - point) < 1e-9 * meter)
                                {
                                    meeting += 1;
                                }
                            }
                        }
                        expectEqual(meeting, 2);
                    }
                }
            }
        }
    }
}

function retainerProfile(retainer is map) returns array
{
    return retainer.lugRadius == undefined ? eClipProfile(retainer) : retainingRingProfile(retainer);
}

export function testRetainingRingsMatchMcMasterCarrsModels()
{
    const rings = RETAINERS[RetainerType.RETAINING_RING][RetainerVendor.MCMASTER];
    const tolerance = 2e-6 * inch;
    // The ends of a lug's corner's and root's fillets, and of its side of the gap, in FRCDesign's
    for (var point in [vector(0.256961, 0.004485), vector(0.235484, 0.077631), vector(0.183600, 0.095896), vector(0.167459, 0.108945)])
    {
        expectRetainerPoint(rings[RetainerSize.IN_3_8], point * inch, tolerance);
    }
    for (var point in [vector(0.338448, 0.005908), vector(0.230465, 0.004023), vector(0.337437, 0.063868), vector(0.315843, 0.095913),
                vector(0.249062, 0.124332), vector(0.232099, 0.139456)])
    {
        expectRetainerPoint(rings[RetainerSize.IN_1_2], point * inch, tolerance);
    }
    // Its holes are 0.090918 in apart
    expectNear(2 * retainingRingHole(rings[RetainerSize.IN_1_2])[1], 0.090918 * inch, tolerance);
}

export function testEClipsMatchMcMasterCarrsModels()
{
    const clips = RETAINERS[RetainerType.E_CLIP][RetainerVendor.MCMASTER];
    // Where the opening's side meets its prong's fillet, in FRCDesign's 3/16 in and 1/2 in e-clips
    expectRetainerPoint(clips[RetainerSize.IN_3_16], vector(0.047624, 0.059288) * inch, 1e-5 * inch);
    expectRetainerPoint(clips[RetainerSize.IN_1_2], vector(0.124621, 0.158993) * inch, 1e-5 * inch);
}

export function testEClipsMatchGobildasModels()
{
    const clips = RETAINERS[RetainerType.E_CLIP][RetainerVendor.GOBILDA];
    const tolerance = 0.001 * millimeter;
    // Vertices of its models of 2106-4008-0560's and 2109-4012-1040's e-clips: the pockets' ends, the opening's sides',
    // and where its ends' fillets meet its rim
    for (var point in [vector(-0.4643, 4.378), vector(0, 3), vector(0, 3.9604), vector(-2.8062, 1.0607), vector(-3.7046, 1.4002),
                vector(-3.9311, 1.9822), vector(2.1639, 2.0778), vector(4.0787, 3.6009), vector(4.1077, 4.577)])
    {
        expectRetainerPoint(clips[RetainerSize.MM_8], point * millimeter, tolerance);
    }
    for (var point in [vector(-4.8056, 1.4692), vector(-4.4615, 0.8929), vector(-5.9759, 1.827), vector(-6.4712, 2.9212),
                vector(-2.9232, 6.4703), vector(-1.8289, 5.9754), vector(-1.4707, 4.8052), vector(-0.8943, 4.4612),
                vector(1.7918, 4.1823), vector(2.1286, 4.1619), vector(5.81, 5.2349), vector(6.1311, 6.5886)])
    {
        expectRetainerPoint(clips[RetainerSize.MM_12], point * millimeter, tolerance);
    }
}

/**
 * Expects one of a retainer's outline's curves to end at `point`.
 */
function expectRetainerPoint(retainer is map, point is Vector, tolerance is ValueWithUnits)
{
    var nearest = 1 * meter;
    for (var curve in retainerProfile(retainer))
    {
        nearest = min([nearest, norm(curve.start - point), norm(curve.end - point)]);
    }
    expectNear(nearest, 0 * meter, tolerance);
}
