FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Involute spur gears and racks, generated in code: each tooth's flanks are involutes (fit splines through points
 * computed on them), its tip an arc, and its root a fillet (or a full round) and an arc.
 */

/**
 * A gear's or rack's tooth form, for a module (`m`) and pressure angle (`α`): standard proportions (an addendum of `m`
 * and a dedendum of `1.25 m`), with its roots rounded by `rootFillet`, and the whole profile offset outward by `offset`
 * (inward, if negative: for clearance).
 *
 * `rootFillet` is a number, the roots' fillet's radius as a fraction of the full round's (the round tangent to the
 * root and both flanks: 1 is the full round, and 0 leaves them sharp), as Onshape's Spur gear's Root fillet; or a
 * length, a router bit's radius (less the offset, as the profile's offset grows into the gap), with a slot the bit's
 * width down the middle of a gap too narrow for it to reach the root (see `gearRoot`).
 *
 * @returns {{
 *      @field module {ValueWithUnits} :
 *      @field pressureAngle {ValueWithUnits} :
 *      @field teeth {number} : `undefined` for a rack.
 *      @field pitchRadius {ValueWithUnits} : A gear's.
 *      @field baseRadius {ValueWithUnits} : A gear's: where its involutes start.
 *      @field tipRadius {ValueWithUnits} : A gear's.
 *      @field rootRadius {ValueWithUnits} : A gear's.
 *      @field filletFraction {number} : By a fraction of the full round, or:
 *      @field filletRadius {ValueWithUnits} : A router bit's.
 *      @field offset {ValueWithUnits} :
 * }}
 */
export function getGearForm(module is ValueWithUnits, teeth, pressureAngle is ValueWithUnits, rootFillet, offset is ValueWithUnits) returns map
{
    var form = {
        "module" : module,
        "teeth" : teeth,
        "pressureAngle" : pressureAngle,
        "offset" : offset
    };
    if (rootFillet is number)
    {
        form.filletFraction = rootFillet;
    }
    else
    {
        form.filletRadius = rootFillet - offset;
    }
    if (teeth != undefined)
    {
        const pitchRadius = module * teeth / 2;
        form.pitchRadius = pitchRadius;
        form.baseRadius = pitchRadius * cos(pressureAngle);
        form.tipRadius = pitchRadius + module + offset;
        form.rootRadius = pitchRadius - 1.25 * module + offset;
    }
    return form;
}

function involute(angle is ValueWithUnits) returns ValueWithUnits
{
    return (tan(angle) - angle / radian) * radian;
}

/**
 * Half of a gear's tooth's angular width at `radius` (at least its base radius): its flank's angle from its center
 * line. An involute offset along its normal is the same involute rotated, by the offset over the base radius. A form
 * with a `flankAngle` function (a spherical involute bevel gear's, see `sphericalBevelForm`) gives it instead.
 */
export function toothHalfAngle(form is map, radius is ValueWithUnits) returns ValueWithUnits
{
    if (form.flankAngle != undefined)
    {
        return form.flankAngle(radius);
    }
    const atRadius = acos(min(form.baseRadius / radius, 1));
    return 90 * degree / form.teeth + involute(form.pressureAngle) - involute(atRadius) + form.offset / form.baseRadius * radian;
}

const FLANK_POINTS = 8;

function polar(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
{
    return vector(cos(angle), sin(angle)) * radius;
}

/**
 * Half a gap and the flank above it, for a tooth centered at angle 0, on its clockwise side: from the middle of the gap
 * up to the corner of its tip, as curves, each a "line", "arc" (with a middle point), or "spline" (points). The root
 * is rounded as `gearRoot` says, below the involute flank.
 */
export function halfGapCurves(form is map) returns array
{
    if (toothHalfAngle(form, form.tipRadius) <= 0 * degree)
    {
        throw regenError("Teeth come to points, increase teeth or change profile offset.", ["teeth", "profileOffsetDistance"]);
    }
    const toothAngle = 360 * degree / form.teeth;
    const root = gearRoot(form);
    var curves = [];
    // Where the involute starts: the top of the radial line, or above it, where a slot meets it
    var flankStart = root.lineTop;
    if (root.slot != undefined)
    {
        // A slot: round at the bottom, then up its side to where it meets the tooth's flank
        const slot = root.slot;
        curves = [
                { "shape" : "arc", "points" : [polar(root.rootRadius, -toothAngle / 2), slot.middle, slot.side] },
                { "shape" : "line", "points" : [slot.side, slot.exit] }
            ];
        if (slot.exitRadius < root.lineTop - TOLERANCE.zeroLength * meter)
        {
            curves = append(curves, { "shape" : "line", "points" : [slot.exit, polar(root.lineTop, -root.lineAngle)] });
        }
        flankStart = max(slot.exitRadius, root.lineTop);
    }
    else if (root.fullRound)
    {
        // A full round: from the bottom of the gap up to the line's tangent point
        curves = append(curves, {
                        "shape" : "arc",
                        "points" : [polar(root.bottomRadius, -toothAngle / 2), root.filletMiddle, root.lineTangent]
                    });
    }
    else
    {
        curves = append(curves, {
                        "shape" : "arc",
                        "points" : [polar(root.rootRadius, -toothAngle / 2), polar(root.rootRadius, (root.filletRootAngle - toothAngle / 2) / 2),
                                polar(root.rootRadius, root.filletRootAngle)]
                    });
        if (root.filletMiddle != undefined)
        {
            curves = append(curves, {
                            "shape" : "arc",
                            "points" : [polar(root.rootRadius, root.filletRootAngle), root.filletMiddle, root.lineTangent]
                        });
        }
    }
    if (root.slot == undefined && root.lineTop > norm(root.lineTangent) + TOLERANCE.zeroLength * meter)
    {
        curves = append(curves, { "shape" : "line", "points" : [root.lineTangent, polar(root.lineTop, -root.lineAngle)] });
    }
    // A slot wider than the gap at the tip runs up to the tip's corner, leaving no flank
    if (flankStart >= form.tipRadius - TOLERANCE.zeroLength * meter)
    {
        return curves;
    }
    var flank = [];
    for (var i = 0; i <= FLANK_POINTS; i += 1)
    {
        const radius = flankStart + (form.tipRadius - flankStart) * i / FLANK_POINTS;
        flank = append(flank, polar(radius, -toothHalfAngle(form, radius)));
    }
    return append(curves, { "shape" : "spline", "points" : flank });
}

/**
 * `curves` (see `halfGapCurves`) mirrored across the x axis, and in reverse.
 */
function mirroredCurves(curves is array) returns array
{
    var result = [];
    for (var i = size(curves) - 1; i >= 0; i -= 1)
    {
        result = append(result, {
                        "shape" : curves[i].shape,
                        "points" : reverse(mapArray(curves[i].points, point => vector(point[0], -point[1])))
                    });
    }
    return result;
}

/**
 * `curves` (see `halfGapCurves`) turned `angle` counterclockwise.
 */
function rotatedCurves(curves is array, angle is ValueWithUnits) returns array
{
    const rotate = function(point is Vector) returns Vector
        {
            return vector(point[0] * cos(angle) - point[1] * sin(angle), point[0] * sin(angle) + point[1] * cos(angle));
        };
    return mapArray(curves, curve => mergeMaps(curve, { "points" : mapArray(curve.points, rotate) }));
}

/**
 * Sketches `curves` (see `halfGapCurves`), each named `prefix` and its index.
 */
function sketchCurves(sketch is Sketch, prefix is string, curves is array)
{
    for (var i, curve in curves)
    {
        const name = prefix ~ i;
        if (curve.shape == "line")
        {
            skLineSegment(sketch, name, { "start" : curve.points[0], "end" : curve.points[1] });
        }
        else if (curve.shape == "arc")
        {
            skArc(sketch, name, { "start" : curve.points[0], "mid" : curve.points[1], "end" : curve.points[2] });
        }
        else
        {
            skFitSpline(sketch, name, { "points" : curve.points });
        }
    }
}

/**
 * Sketches a gear's profile on `plane`, centered on it, and returns its faces: every tooth, or (with `sectorTeeth`) a
 * sector of that many teeth, centered on the plane's x axis, closed back to its center.
 */
export function sketchGearProfile(context is Context, id is Id, plane is Plane, form is map, sectorTeeth) returns Query
{
    const teeth = form.teeth;
    const toothAngle = 360 * degree / teeth;
    // A tooth centered at angle 0, from the middle of the gap before it (on its clockwise side) to the middle of the
    // gap after it
    const halfGap = halfGapCurves(form);
    const tipHalf = toothHalfAngle(form, form.tipRadius);
    const tip = {
            "shape" : "arc",
            "points" : [polar(form.tipRadius, -tipHalf), polar(form.tipRadius, 0 * degree), polar(form.tipRadius, tipHalf)]
        };
    const curves = concatenateArrays([halfGap, [tip], mirroredCurves(halfGap)]);

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const toothCount = sectorTeeth ?? teeth;
    // A sector's centered on the x axis
    const firstAngle = sectorTeeth == undefined ? 0 * degree : -(sectorTeeth - 1) / 2 * toothAngle;
    for (var tooth = 0; tooth < toothCount; tooth += 1)
    {
        sketchCurves(sketch, "tooth" ~ tooth ~ "_", rotatedCurves(curves, firstAngle + tooth * toothAngle));
    }
    if (sectorTeeth != undefined)
    {
        // Back to the center, from the middles of the gaps at its ends
        const bottom = norm(halfGap[0].points[0]);
        skLineSegment(sketch, "sectorStart", { "start" : vector(0, 0) * meter, "end" : polar(bottom, firstAngle - toothAngle / 2) });
        skLineSegment(sketch, "sectorEnd", { "start" : polar(bottom, firstAngle + (sectorTeeth - 0.5) * toothAngle), "end" : vector(0, 0) * meter });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * How many times `gearRoot` refines a full round: the radial line it's tangent to rises with its radius, which turns
 * the line, which changes the radius a little less each time.
 */
const FULL_ROUND_STEPS = 12;

/**
 * How many times `gearSlot` halves the range where a slot meets the tooth's flank: from the root circle to the tip, to
 * well within std's length tolerance.
 */
const SLOT_STEPS = 50;

/**
 * The root of a gear's gaps, on a tooth's clockwise side (for a tooth centered at angle 0): below its involute, a
 * radial line up to where the involute starts (its base circle, or above it, up to half the tooth's height, to make
 * room for the fillet; with `form.contactRadius`, no higher than that, where its mate's tips touch it, see
 * `lowestContactRadius`), and a fillet between it and the root circle (`form.filletFraction` of the full round's radius,
 * or a router bit's `form.filletRadius`); or a full round, tangent to both sides' lines (and, by fraction, the root
 * circle; a router bit's, where two of its fillets don't fit in the gap, but it reaches the root circle between the
 * lines); or without a fillet, a sharp corner. With `form.contactRadius`, a full round which doesn't fit below the
 * line's top is the largest fillet which does: tangent to the root circle, and the line at its top.
 *
 * A router bit too wide to reach the root circle between the lines cuts a slot instead (`gearSlot`), as robot gears
 * cut with a router are: the bit's path straight down the middle of the gap, square to the pitch circle, to the root
 * circle. Its sides cut into the teeth's flanks, up to where the gap's as wide as the bit.
 *
 * @returns {{
 *      @field rootRadius {ValueWithUnits} :
 *      @field fullRound {boolean} :
 *      @field slot {map} : A router bit's slot (see `gearSlot`), or `undefined`.
 *      @field bottomRadius {ValueWithUnits} : A full round's bottom.
 *      @field filletRootAngle {ValueWithUnits} : Where a fillet (or a sharp corner) meets the root circle.
 *      @field filletMiddle {Vector} : A fillet's or full round's: `undefined` for a sharp corner.
 *      @field lineTangent {Vector} : Where it meets the radial line.
 *      @field lineAngle {ValueWithUnits} : The radial line's angle (clockwise of the tooth's center).
 *      @field lineTop {ValueWithUnits} : Where the radial line meets the involute.
 * }}
 */
export function gearRoot(form is map) returns map
{
    const rootRadius = form.rootRadius;
    if (rootRadius <= 0 * meter)
    {
        throw regenError("Too few teeth for a gear, increase teeth.", ["teeth"]);
    }
    // Room for the fillet below the involute, up to half the tooth's height: above the base circle the line's a little
    // inside the involute, relieving it rather than interfering
    const lineTopFor = function(fillet is ValueWithUnits) returns ValueWithUnits
        {
            const top = rootRadius + min(2 * fillet, (form.tipRadius - rootRadius) / 2);
            return max(form.baseRadius, form.contactRadius == undefined ? top : min(top, form.contactRadius));
        };
    // Half the gap's angle, between the lines
    const halfGapAt = function(lineTop is ValueWithUnits) returns ValueWithUnits
        {
            const halfGap = 180 * degree / form.teeth - toothHalfAngle(form, lineTop);
            if (halfGap <= 0 * degree)
            {
                throw regenError("Teeth meet at their roots, change profile offset.", ["profileOffsetDistance"]);
            }
            return halfGap;
        };

    var fillet;
    var lineTop;
    // A fillet, not a full round, though by fraction it'd be one
    var partial = form.filletFraction != 1;
    if (form.filletFraction != undefined)
    {
        // The full round: tangent to the root circle and both sides' lines
        var full = 0 * meter;
        for (var i = 0; i < FULL_ROUND_STEPS; i += 1)
        {
            const halfGap = halfGapAt(lineTopFor(full));
            full = rootRadius * sin(halfGap) / (1 - sin(halfGap));
        }
        lineTop = lineTopFor(full);
        if (form.filletFraction > 0 && (rootRadius + full) * cos(halfGapAt(lineTop)) > lineTop + TOLERANCE.zeroLength * meter)
        {
            if (form.contactRadius == undefined)
            {
                throw regenError("Full root fillet does not fit below the flanks, select a smaller root fillet.", ["rootFillet"]);
            }
            // The largest which does: its center the root radius and it out, its tangent on the line at the line's top
            full = (lineTop ^ 2 - rootRadius ^ 2) / (2 * rootRadius) / form.filletFraction;
            partial = true;
        }
        fillet = full * form.filletFraction;
    }
    else
    {
        fillet = form.filletRadius;
        // The profile's offset out by at least the root's round
        if (fillet <= 0 * meter)
        {
            throw regenError("Profile offset is larger than the bit's radius, decrease profile offset or increase bit diameter.", ["profileOffsetDistance", "bitDiameter"]);
        }
        lineTop = lineTopFor(fillet);
    }
    const lineAngle = toothHalfAngle(form, lineTop);
    const halfGap = halfGapAt(lineTop);

    if (fillet <= 0 * meter)
    {
        // A sharp corner, where the line meets the root circle
        return {
                "rootRadius" : rootRadius,
                "fullRound" : false,
                "filletRootAngle" : -lineAngle,
                "lineTangent" : polar(rootRadius, -lineAngle),
                "lineAngle" : lineAngle,
                "lineTop" : lineTop
            };
    }

    // A fillet tangent to the root circle (outside it) and the line (on the gap's side)
    const centerRadius = rootRadius + fillet;
    const offAngle = asin(fillet / centerRadius);
    if (partial && offAngle < halfGap && centerRadius * cos(offAngle) <= lineTop + TOLERANCE.zeroLength * meter)
    {
        const centerAngle = -lineAngle - offAngle;
        const center = polar(centerRadius, centerAngle);
        const lineTangent = polar(centerRadius * cos(offAngle), -lineAngle);
        const rootTangent = polar(rootRadius, centerAngle);
        return {
                "rootRadius" : rootRadius,
                "fullRound" : false,
                "filletRootAngle" : centerAngle,
                "filletMiddle" : center + normalize(normalize(rootTangent - center) + normalize(lineTangent - center)) * fillet,
                "lineTangent" : lineTangent,
                "lineAngle" : lineAngle,
                "lineTop" : lineTop
            };
    }
    // A full round: a circle of the fillet's radius tangent to both sides' lines, centered in the gap
    const roundCenterRadius = fillet / sin(halfGap);
    if (form.filletRadius != undefined && roundCenterRadius - fillet > rootRadius + TOLERANCE.zeroLength * meter)
    {
        // A router bit which doesn't reach the root circle between the lines: a slot down to it
        return {
                "rootRadius" : rootRadius,
                "fullRound" : false,
                "slot" : gearSlot(form, fillet, lineAngle, lineTop),
                "lineAngle" : lineAngle,
                "lineTop" : lineTop
            };
    }
    const tangentRadius = roundCenterRadius * cos(halfGap);
    if (tangentRadius > lineTop + TOLERANCE.zeroLength * meter)
    {
        throw regenError("Bit diameter is too large, decrease the value.", ["bitDiameter"]);
    }
    const gapAngle = -180 * degree / form.teeth;
    const center = polar(roundCenterRadius, gapAngle);
    const lineTangent = polar(tangentRadius, -lineAngle);
    const bottom = polar(roundCenterRadius - fillet, gapAngle);
    return {
            "rootRadius" : rootRadius,
            "fullRound" : true,
            "bottomRadius" : roundCenterRadius - fillet,
            "filletMiddle" : center + normalize(normalize(bottom - center) + normalize(lineTangent - center)) * fillet,
            "lineTangent" : lineTangent,
            "lineAngle" : lineAngle,
            "lineTop" : lineTop
        };
}

/**
 * A router bit's slot down the middle of a gear's gap (see `gearRoot`), on the clockwise side of a tooth centered at
 * angle 0, whose flank below `lineTop` is a radial line at `lineAngle`: its round bottom, of the bit's `radius`, on the
 * root circle; where its side leaves that; and where its side meets the flank (its `exit`), found by halving the range
 * it's in, as the gap only widens outward. A slot wider than the gap at the tip meets it at the tip's corner.
 *
 * It's `tooLarge` if it meets the flank above the pitch circle: the bit's wider than the gap there (as wide as a tooth,
 * half the circular pitch), so it cuts into the middle of the flank, where meshing teeth always touch.
 *
 * @returns {{
 *      @field middle {Vector} : The middle of its bottom's round, from the root circle up to its side.
 *      @field side {Vector} :
 *      @field exit {Vector} :
 *      @field exitRadius {ValueWithUnits} :
 *      @field tooLarge {boolean} :
 * }}
 */
export function gearSlot(form is map, radius is ValueWithUnits, lineAngle is ValueWithUnits, lineTop is ValueWithUnits) returns map
{
    const gapAngle = -180 * degree / form.teeth;
    // Out along the gap's middle, and across it toward the tooth
    const out = vector(cos(gapAngle), sin(gapAngle));
    const across = vector(-sin(gapAngle), cos(gapAngle));
    const flankAngle = function(at is ValueWithUnits) returns ValueWithUnits
        {
            return at <= lineTop ? lineAngle : toothHalfAngle(form, at);
        };
    // How far the flank is from the gap's middle
    const halfWidth = function(at is ValueWithUnits) returns ValueWithUnits
        {
            return at * sin(180 * degree / form.teeth - flankAngle(at));
        };
    var low = form.rootRadius;
    var high = form.tipRadius;
    if (halfWidth(high) > radius)
    {
        for (var i = 0; i < SLOT_STEPS; i += 1)
        {
            const middle = (low + high) / 2;
            if (halfWidth(middle) < radius)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }
    }
    const center = out * (form.rootRadius + radius);
    return {
            "middle" : center + normalize(across - out) * radius,
            "side" : center + across * radius,
            "exit" : polar(high, -flankAngle(high)),
            "exitRadius" : high,
            "tooLarge" : high > form.pitchRadius + TOLERANCE.zeroLength * meter
        };
}

/**
 * Half a rack tooth's width `y` above its pitch line: its flanks are straight, at the pressure angle, and offset by the
 * profile offset.
 */
export function rackHalfWidth(form is map, y is ValueWithUnits) returns ValueWithUnits
{
    return PI * form.module / 4 + form.offset / cos(form.pressureAngle) - y * tan(form.pressureAngle);
}

/**
 * Where a rack's teeth are along a face `length` long: one every circular pitch (`π m`), centered half a pitch from
 * `gap` (where a gap's centered), as many as fit whole, root and all, from 0 to `length` (within std's length
 * tolerance).
 */
export function rackToothCenters(form is map, length is ValueWithUnits, gap is ValueWithUnits) returns array
{
    const pitch = PI * form.module;
    const halfRoot = rackHalfWidth(form, rackRoot(form));
    const tolerance = TOLERANCE.zeroLength * meter;
    const first = gap + pitch / 2;
    var centers = [];
    for (var k = ceil((halfRoot - first - tolerance) / pitch); k <= floor((length - halfRoot - first + tolerance) / pitch); k += 1)
    {
        centers = append(centers, first + k * pitch);
    }
    return centers;
}

/**
 * A rack's root: `1.25 m` below its pitch line, less the profile offset.
 */
export function rackRoot(form is map) returns ValueWithUnits
{
    return -1.25 * form.module + form.offset;
}

/**
 * What's cut from a part to leave rack teeth along its face: the region between the face and the teeth, as polygons
 * (arrays of 2D points), from 0 to `length` along the face. The face is on the x axis, and its part toward -y; the
 * pitch line is `faceHeight` below it (0 for a face at the pitch line, `m` for one at the teeth's tips). Teeth are at
 * `centers` (see `rackToothCenters`), cut off at the face if they'd reach past it; beyond the first and last, it's cut
 * to the root. The roots are rounded as the form says (see `rackRootFilletRadius` and `relieveRackRoots`), which puts
 * arcs in the regions: a map `{ "mid" : Vector }` between two points is an arc through it, rather than a line.
 */
export function rackCutRegions(form is map, faceHeight is ValueWithUnits, length is ValueWithUnits, centers is array) returns array
{
    const root = rackRoot(form);
    const tip = min(form.module + form.offset, faceHeight);
    if (rackHalfWidth(form, tip) <= 0 * meter)
    {
        throw regenError("Teeth come to points, change profile offset.", ["profileOffsetDistance"]);
    }
    if (rackHalfWidth(form, root) >= PI * form.module / 2)
    {
        throw regenError("Teeth meet at their roots, change profile offset.", ["profileOffsetDistance"]);
    }
    // In the face's coordinates: the face at y = 0
    const at = function(x is ValueWithUnits, y is ValueWithUnits) returns Vector
        {
            return vector(x, y - faceHeight);
        };
    var regions = [];
    if (tip < faceHeight)
    {
        // The teeth stop short of the face: one region, over them
        var points = [at(0 * meter, faceHeight), at(0 * meter, root)];
        for (var center in centers)
        {
            points = concatenateArrays([points, [
                            at(center - rackHalfWidth(form, root), root),
                            at(center - rackHalfWidth(form, tip), tip),
                            at(center + rackHalfWidth(form, tip), tip),
                            at(center + rackHalfWidth(form, root), root)
                        ]]);
        }
        regions = [concatenateArrays([points, [at(length, root), at(length, faceHeight)]])];
    }
    else
    {
        // The teeth reach the face: a region for each gap, between the ends and the teeth's flanks
        var top = at(0 * meter, faceHeight);
        var bottom = at(0 * meter, root);
        for (var center in centers)
        {
            regions = append(regions, [top, bottom, at(center - rackHalfWidth(form, root), root), at(center - rackHalfWidth(form, tip), tip)]);
            top = at(center + rackHalfWidth(form, tip), tip);
            bottom = at(center + rackHalfWidth(form, root), root);
        }
        regions = append(regions, [top, bottom, at(length, root), at(length, faceHeight)]);
    }
    // A tooth whole to an end (within tolerance) leaves points on top of each other there
    regions = filter(mapArray(regions, region => distinctPoints(region)), region => size(region) >= 3);
    const radius = rackRootFilletRadius(form);
    if (radius <= 0 * meter)
    {
        return regions;
    }
    return mapArray(regions, region => relieveRackRoots(form, region, radius, root - faceHeight));
}

/**
 * The radius of a rack's root fillets: a router bit's (`form.filletRadius`), or `form.filletFraction` of the full
 * round's, which is tangent to the floor between two teeth and both their flanks (their corners, `90° + α`, meet in
 * the floor's middle).
 */
export function rackRootFilletRadius(form is map) returns ValueWithUnits
{
    if (form.filletFraction == undefined)
    {
        return form.filletRadius;
    }
    const floor = PI * form.module - 2 * rackHalfWidth(form, rackRoot(form));
    return form.filletFraction * floor / 2 * tan(45 * degree + form.pressureAngle / 2);
}

/**
 * Whether a router bit's slots (see `relieveRackRoots`) cut a rack's teeth's flanks above its pitch line, as a gear's
 * mustn't above its pitch circle (see `gearSlot`). With no slots (its fillets fit across the floor), they don't. Worked
 * out from the form, as every gap between teeth is the same: its floor's half width `f`, and the corners between it
 * and the flanks, `90° + α`.
 */
export function rackBitTooLarge(form is map) returns boolean
{
    const radius = form.filletRadius;
    if (radius == undefined)
    {
        return false;
    }
    const tolerance = TOLERANCE.zeroLength * meter;
    const alpha = form.pressureAngle;
    const root = rackRoot(form);
    const f = PI * form.module / 2 - rackHalfWidth(form, root);
    // Each fillet runs along the floor `r / tan(45° + α / 2)`
    if (radius / tan(45 * degree + alpha / 2) <= f + tolerance)
    {
        return false;
    }
    // The flanks are `r` from the gap's middle `(r - f) / tan(α)` above the root
    return root + (radius - f) / tan(alpha) > tolerance;
}

const RACK_ROUND_DOESNT_FIT = "Bit diameter is too large, decrease the value.";

/**
 * `region` (see `rackCutRegions`) with its roots rounded to `radius`: each corner at the root (`rootY`) between the
 * floor and a tooth's flank becomes an arc tangent to both (a full round's two meet in the floor's middle). Where a
 * gap's floor is too narrow for both (a router bit's), the bit can't reach the root between the flanks (a round
 * tangent to both is above it once its fillets overlap): it's a slot the bit's width down the gap's middle instead,
 * square to the pitch line, to the root, cutting into the flanks (`rackSlot`; see `rackBitTooLarge` for where it
 * mustn't). A corner at an end's wall (not a flank) is left.
 */
export function relieveRackRoots(form is map, region is array, radius is ValueWithUnits, rootY is ValueWithUnits) returns array
{
    const count = size(region);
    const point = function(i is number) returns Vector
        {
            return region[(i + count) % count];
        };
    const atRoot = function(p is Vector) returns boolean
        {
            return tolerantEquals(p[1], rootY);
        };
    // A corner at the root between the floor and a flank (neither vertical nor flat)
    const flankCorner = function(i is number) returns boolean
        {
            const p = point(i);
            if (!atRoot(p))
            {
                return false;
            }
            for (var other in [point(i - 1), point(i + 1)])
            {
                if (!atRoot(other) && !tolerantEquals(other[0], p[0]))
                {
                    return true;
                }
            }
            return false;
        };
    // A corner's fillet: where it leaves the edges before and after, its middle and center, and whether it runs past
    // the end of the edge before (or after) it, or past the flank's
    const fillet = function(i is number) returns map
        {
            const p = point(i);
            const u = normalize(point(i - 1) - p);
            const v = normalize(point(i + 1) - p);
            const half = acos(dot(u, v)) / 2;
            const along = radius / tan(half);
            const pastBefore = along > norm(point(i - 1) - p) + TOLERANCE.zeroLength * meter;
            const pastAfter = along > norm(point(i + 1) - p) + TOLERANCE.zeroLength * meter;
            const bisector = normalize(u + v);
            const center = p + bisector * radius / sin(half);
            return {
                    "before" : p + u * along,
                    "mid" : center - bisector * radius,
                    "after" : p + v * along,
                    "center" : center,
                    "pastBefore" : pastBefore,
                    "pastAfter" : pastAfter,
                    "pastFlank" : (pastBefore && !atRoot(point(i - 1))) || (pastAfter && !atRoot(point(i + 1)))
                };
        };
    // Where a fillet running past an end's wall (at `wall`) crosses it, below its center, and the arc's middle from
    // there to where it leaves the flank (`flank`)
    const clipped = function(corner is map, wall is Vector, flank is Vector) returns map
        {
            const dx = wall[0] - corner.center[0];
            const crossing = vector(wall[0], corner.center[1] - sqrt(radius * radius - dx * dx));
            const middle = normalize(normalize(crossing - corner.center) + normalize(flank - corner.center));
            return { "crossing" : crossing, "mid" : corner.center + middle * radius };
        };
    var result = [];
    var i = 0;
    while (i < count)
    {
        if (flankCorner(i) && flankCorner(i + 1) && atRoot(point(i + 1)))
        {
            // A gap's floor: fillets at both ends, or a router bit's slot if they'd overlap
            const left = fillet(i);
            const right = fillet(i + 1);
            const overlap = !tolerantEquals(left.after, right.before) && left.after[0] > right.before[0];
            if (!overlap && (left.pastFlank || right.pastFlank))
            {
                throw regenError(RACK_ROUND_DOESNT_FIT, ["bitDiameter"]);
            }
            if (tolerantEquals(left.after, right.before))
            {
                // Full rounds' fillets, meeting in the floor's middle
                result = concatenateArrays([result, [left.before, { "mid" : left.mid }, left.after, { "mid" : right.mid }, right.after]]);
            }
            else if (!overlap)
            {
                result = concatenateArrays([result, [left.before, { "mid" : left.mid }, left.after, right.before, { "mid" : right.mid }, right.after]]);
            }
            else
            {
                result = concatenateArrays([result, rackSlot(point(i - 1), point(i), point(i + 1), radius)]);
            }
            i += 2;
        }
        else if (flankCorner(i))
        {
            const corner = fillet(i);
            if (corner.pastFlank)
            {
                throw regenError(RACK_ROUND_DOESNT_FIT, ["bitDiameter"]);
            }
            if (corner.pastBefore)
            {
                // The floor before it is an end's, too short: the round's cut off at the end's wall, in place of its
                // corner there
                const clip = clipped(corner, point(i - 1), corner.after);
                result = concatenateArrays([resize(result, size(result) - 1), [clip.crossing, { "mid" : clip.mid }, corner.after]]);
                i += 1;
            }
            else if (corner.pastAfter)
            {
                const clip = clipped(corner, point(i + 1), corner.before);
                result = concatenateArrays([result, [corner.before, { "mid" : clip.mid }, clip.crossing]]);
                i += 2;
            }
            else
            {
                result = concatenateArrays([result, [corner.before, { "mid" : corner.mid }, corner.after]]);
                i += 1;
            }
        }
        else
        {
            result = append(result, point(i));
            i += 1;
        }
    }
    return result;
}

/**
 * A router bit's slot across a gap's floor, from `p` to `q`, between the flanks from `above` down to `p` and from `q`
 * up (the same, mirrored across the gap's middle): the bit's width (`radius` each side) straight down the gap's middle,
 * square to the floor, to it. Its sides run up from its round bottom to where they meet the flanks, or (a slot wider
 * than the gap) up to the flanks' tops.
 */
function rackSlot(above is Vector, p is Vector, q is Vector, radius is ValueWithUnits) returns array
{
    const up = normalize(above - p);
    const middle = (p[0] + q[0]) / 2;
    // Up the flank to where it's the radius from the gap's middle
    const exit = p + up * min((middle - radius - p[0]) / up[0], norm(above - p));
    const side = vector(middle - radius, p[1] + radius);
    const points = [side, { "mid" : vector(middle, p[1]) }, vector(middle + radius, side[1])];
    if (tolerantEquals(exit, above))
    {
        return points;
    }
    return concatenateArrays([[exit], points, [vector(2 * middle - exit[0], exit[1])]]);
}

/**
 * `points`, without any within std's length tolerance of the one before it (or, for the last, the first).
 */
function distinctPoints(points is array) returns array
{
    var result = [];
    for (var point in points)
    {
        if (result == [] || !tolerantEquals(point, result[size(result) - 1]))
        {
            result = append(result, point);
        }
    }
    if (size(result) > 1 && tolerantEquals(result[0], result[size(result) - 1]))
    {
        result = resize(result, size(result) - 1);
    }
    return result;
}

/**
 * Sketches `regions` (see `rackCutRegions`) on `plane`, and returns their faces.
 */
export function sketchRackCut(context is Context, id is Id, plane is Plane, regions is array) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    for (var r, region in regions)
    {
        const count = size(region);
        for (var i, point in region)
        {
            // An arc's middle is drawn with the points either side of it
            if (point is map)
            {
                continue;
            }
            const next = region[(i + 1) % count];
            if (next is map)
            {
                skArc(sketch, "arc" ~ r ~ "_" ~ i, { "start" : point, "mid" : next.mid, "end" : region[(i + 2) % count] });
            }
            else
            {
                skLineSegment(sketch, "line" ~ r ~ "_" ~ i, { "start" : point, "end" : next });
            }
        }
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * A straight bevel gear's cones, for `teeth` meshing with `mateTeeth` with their shafts `shaftAngle` apart: its pitch
 * cone's angle from its axis, its pitch circle's radius (at its back), the cone distance (along its pitch cone, from
 * its apex to that circle), and its virtual teeth, for Tredgold's approximation: its teeth as a spur gear's on its back
 * cone (the cone square to its pitch cone at its back), whose radius is the pitch radius over the pitch angle's cosine:
 * a spur gear with `teeth / cos(pitchAngle)` teeth, not a whole number.
 *
 * @returns {{
 *      @field pitchAngle {ValueWithUnits} :
 *      @field pitchRadius {ValueWithUnits} :
 *      @field coneDistance {ValueWithUnits} :
 *      @field virtualTeeth {number} :
 * }}
 */
export function bevelCones(module is ValueWithUnits, teeth is number, mateTeeth is number, shaftAngle is ValueWithUnits) returns map
{
    const pitchAngle = atan2(sin(shaftAngle), mateTeeth / teeth + cos(shaftAngle));
    const pitchRadius = module * teeth / 2;
    return {
            "pitchAngle" : pitchAngle,
            "pitchRadius" : pitchRadius,
            "coneDistance" : pitchRadius / sin(pitchAngle),
            "virtualTeeth" : teeth / cos(pitchAngle)
        };
}

/**
 * Gleason's allowance on a straight bevel gear's whole depth (`2.188 m` and this), as the cutter's: its clearance is a
 * little more than `0.188 m`.
 */
const GLEASON_DEPTH_ALLOWANCE = 0.002 * inch;

/**
 * A straight bevel gear's proportions at its back, by Gleason's system for straight bevel gears (as KHK's and Boston
 * Gear's are), for `teeth` meshing with `mateTeeth`, with pitch cones `pitchAngle` and `matePitchAngle` from their axes:
 *
 * - A working depth of `2 m`, shared between them by their ratio, so a small pinion's teeth are long and thick, and
 *   don't undercut: the gear with more teeth's addendum is `0.54 m + 0.46 m / m90²`, where `m90²` is
 *   `z_gear cos(pitch angle_pinion) / (z_pinion cos(pitch angle_gear))` (their ratio, squared, at a right angle), and
 *   the pinion's is the rest. A pair with the same teeth has `m` each.
 * - A whole depth of `2.188 m` (and Gleason's allowance, `GLEASON_DEPTH_ALLOWANCE`): its dedendum is the rest.
 * - Its circular thickness at its back's pitch circle: half the circular pitch, and the difference in their addenda
 *   times the pressure angle's tangent (as a profile shift would), so the pair's add up to the circular pitch. Gleason
 *   also takes a little more from the gear for the pinion (`K m`, from a chart, for strength): that's left out.
 *
 * @returns {{
 *      @field addendum {ValueWithUnits} :
 *      @field dedendum {ValueWithUnits} :
 *      @field mateAddendum {ValueWithUnits} :
 *      @field mateDedendum {ValueWithUnits} :
 *      @field thickness {ValueWithUnits} :
 * }}
 */
export function gleasonProportions(module is ValueWithUnits, teeth is number, mateTeeth is number, pitchAngle is ValueWithUnits,
    matePitchAngle is ValueWithUnits, pressureAngle is ValueWithUnits) returns map
{
    const isGear = teeth >= mateTeeth;
    const ratio = isGear ? teeth * cos(matePitchAngle) / (mateTeeth * cos(pitchAngle)) : mateTeeth * cos(pitchAngle) / (teeth * cos(matePitchAngle));
    const gearAddendum = module * (0.54 + 0.46 / ratio);
    const addendum = isGear ? gearAddendum : 2 * module - gearAddendum;
    const mateAddendum = 2 * module - addendum;
    const wholeDepth = 2.188 * module + GLEASON_DEPTH_ALLOWANCE;
    return {
            "addendum" : addendum,
            "dedendum" : wholeDepth - addendum,
            "mateAddendum" : mateAddendum,
            "mateDedendum" : wholeDepth - mateAddendum,
            "thickness" : PI * module / 2 + (addendum - mateAddendum) * tan(pressureAngle)
        };
}

/**
 * The least radius a gear's flanks (pitch radius `pitchRadius`: a bevel gear's virtual spur gear's, on its back cone)
 * are touched at by its mate's tips (its pitch radius `matePitchRadius`, its tips `mateAddendum` past it), meshing at
 * `pressureAngle`: where the mate's tip circle crosses their line of action. Below it, its flanks can be relieved. If the
 * mate's tips reach past where the line of action touches its base circle, its base radius (they'd interfere there).
 */
export function lowestContactRadius(pitchRadius is ValueWithUnits, matePitchRadius is ValueWithUnits, mateAddendum is ValueWithUnits,
    pressureAngle is ValueWithUnits) returns ValueWithUnits
{
    const base = pitchRadius * cos(pressureAngle);
    const mateBase = matePitchRadius * cos(pressureAngle);
    // Along the line of action, from where it touches the base circle to where the mate's tips cross it
    const along = (pitchRadius + matePitchRadius) * sin(pressureAngle) - sqrt((matePitchRadius + mateAddendum) ^ 2 - mateBase ^ 2);
    return along <= 0 * meter ? base : sqrt(base ^ 2 + along ^ 2);
}

/**
 * The spherical involute function: how far around its base cone's axis a spherical involute is at `angle` from the
 * axis, from where it leaves its base cone (`baseAngle` from the axis). A spherical involute is traced by a point on a
 * plane through the cone's apex rolling around it; `roll` is how far the plane has turned about the line it touches the
 * cone along.
 */
export function sphericalInvolute(baseAngle is ValueWithUnits, angle is ValueWithUnits) returns ValueWithUnits
{
    const roll = acos(min(cos(angle) / cos(baseAngle), 1));
    return roll / sin(baseAngle) - atan(tan(roll) / sin(baseAngle));
}

/**
 * How many times `sphericalBevelForm` halves the range of angles a point on its flank is in, to find it at a radius:
 * from the base cone to the tip, to well within std's length tolerance.
 */
const BEVEL_FLANK_STEPS = 50;

/**
 * A straight bevel gear's tooth form with spherical involute flanks (exactly conjugate, unlike Tredgold's virtual spur
 * gear), as `sketchToothGap` sketches a gap: its teeth's flanks are cones through its apex, through spherical
 * involutes on the sphere of its cone distance, so they're sketched as seen from its apex, projected onto the plane
 * square to its pitch cone at its back, through a gap's middle. There, lines through the axis's point on the plane (the
 * back cone's apex, the sketch's center) are planes through the axis, and scaled about its apex, the gap's the same
 * sketch scaled. So the form is a gear's (see `getGearForm`) in that plane, centered on that point, whose
 * `flankAngle` (see `toothHalfAngle`) is the projected flank's, whose teeth's middles are as far around as the
 * projected teeth's (`teeth`, not a whole number), and whose roots are full rounds:
 *
 * - Its base cone's angle from its axis is `asin(sin(pitch angle) cos(pressure angle))`: a spherical involute from it
 *   crosses the pitch cone at the pressure angle.
 * - A tooth is `thickness` thick on its back's pitch circle, and offset by `offset`, as an involute is (rotated about
 *   the axis by the offset over the base cone's radius, at the back).
 * - Its tips (`addendum` out along the back cone from its back's pitch circle) and roots (`dedendum` in) are as far
 *   from that circle in the plane, through the gap's middle; its tips there are the highest its teeth reach (by Gleason's
 *   proportions, the tips come down toward the apex, see `bevelDepths`).
 * - Its root's radial lines stay below where its mate's tips touch it (`contactRadius`, see `gearRoot`): on its back
 *   cone's virtual spur gear, meshing with its mate's (`matePitchAngle` from its axis), whose tips are `mateAddendum`
 *   past its pitch circle.
 *
 * @param cones : Its, as `bevelCones` gives them.
 */
export function sphericalBevelForm(module is ValueWithUnits, teeth is number, cones is map, matePitchAngle is ValueWithUnits, pressureAngle is ValueWithUnits,
    thickness is ValueWithUnits, addendum is ValueWithUnits, dedendum is ValueWithUnits, mateAddendum is ValueWithUnits, offset is ValueWithUnits) returns map
{
    const pitchAngle = cones.pitchAngle;
    const coneDistance = cones.coneDistance;
    // The back cone's, from its apex to the pitch circle
    const backRadius = coneDistance * tan(pitchAngle);
    const baseAngle = asin(sin(pitchAngle) * cos(pressureAngle));
    // Half a tooth, around the axis, at the back's pitch circle
    const pitchHalf = (thickness / 2 / cones.pitchRadius + offset / (coneDistance * sin(baseAngle))) * radian;
    const halfPitch = 180 * degree / teeth;
    // How far around from the gap's middle the next tooth's flank is, at `angle` from the axis (along a plane through the
    // axis below the base cone)
    const flankAround = function(angle is ValueWithUnits) returns ValueWithUnits
        {
            return halfPitch - pitchHalf - sphericalInvolute(baseAngle, pitchAngle) + sphericalInvolute(baseAngle, max(angle, baseAngle));
        };
    // A direction `angle` from the axis and `around` from the gap's middle, projected onto the plane: out along the back
    // cone from its apex, and around
    const projected = function(angle is ValueWithUnits, around is ValueWithUnits) returns Vector
        {
            const along = sin(angle) * cos(around) * sin(pitchAngle) + cos(angle) * cos(pitchAngle);
            return vector(backRadius + coneDistance * (sin(angle) * cos(around) * cos(pitchAngle) - cos(angle) * sin(pitchAngle)) / along,
                coneDistance * sin(angle) * sin(around) / along);
        };
    const flankPoint = function(angle is ValueWithUnits) returns Vector
        {
            return projected(angle, flankAround(angle));
        };
    const tipAngle = pitchAngle + atan(addendum / coneDistance);
    const baseRadius = norm(flankPoint(baseAngle));
    // The teeth's middles, around from the gap's middle (as Tredgold's teeth are about `180° cos(pitch angle) / teeth`)
    const toothLine = atan(tan(halfPitch) * cos(pitchAngle));
    return {
            "module" : module,
            "teeth" : 180 * degree / toothLine,
            "pressureAngle" : pressureAngle,
            "offset" : offset,
            "filletFraction" : 1,
            "pitchRadius" : backRadius,
            "baseRadius" : baseRadius,
            "tipRadius" : norm(flankPoint(tipAngle)),
            "rootRadius" : backRadius - dedendum,
            "contactRadius" : lowestContactRadius(backRadius, coneDistance * tan(matePitchAngle), mateAddendum, pressureAngle),
            "flankAngle" : function(radius is ValueWithUnits) returns ValueWithUnits
                {
                    // Out along the flank from the base cone to its tip, as far as `radius`: below the base cone, it's
                    // the plane through the axis there
                    var low = baseAngle;
                    var high = tipAngle;
                    if (radius <= baseRadius)
                    {
                        high = baseAngle;
                    }
                    for (var i = 0; i < BEVEL_FLANK_STEPS && radius > baseRadius; i += 1)
                    {
                        const middle = (low + high) / 2;
                        if (norm(flankPoint(middle)) < radius)
                        {
                            low = middle;
                        }
                        else
                        {
                            high = middle;
                        }
                    }
                    const point = flankPoint(high);
                    return toothLine - atan2(point[1], point[0]);
                }
        };
}

/**
 * The root a router bit of `bitRadius` cuts in a bevel gear's gap (`form`, at its back, as `sketchToothGap` sketches
 * it) where the gap is scaled by `scale` toward its apex: as `gearRoot` gives it, at the form's size (the bit's radius
 * over the scale), less the profile offset. Its `slot` is there where the bit's too wide to reach the gap's full round:
 * then it cuts a slot its width straight down the gap's middle to the root (see `gearSlot`), `tooLarge` if it meets the
 * flanks above the pitch cone.
 */
export function bevelBitRoot(form is map, bitRadius is ValueWithUnits, scale is number) returns map
{
    var bitForm = form;
    bitForm.filletFraction = undefined;
    bitForm.filletRadius = bitRadius / scale - form.offset;
    return gearRoot(bitForm);
}

/**
 * `curves` (see `halfGapCurves`) scaled about the center by `scale`.
 */
function scaledCurves(curves is array, scale is number) returns array
{
    return mapArray(curves, curve => mergeMaps(curve, { "points" : mapArray(curve.points, point => point * scale) }));
}

/**
 * Sketches the gap between two of a gear's teeth on `plane` (centered on the gear's center), scaled about its center by
 * `scale`, and returns its face: from its root (rounded as `halfGapCurves` rounds a gear's) to a module past the tip,
 * between the tooth centered on the plane's x axis and the next one around. For cutting a bevel gear's teeth, whose
 * `form` (a Tredgold virtual spur gear's, see `bevelCones`, or `sphericalBevelForm`) can have a fractional number of
 * teeth, at both ends of the gap.
 */
export function sketchToothGap(context is Context, id is Id, plane is Plane, form is map, scale is number) returns Query
{
    const pitch = 360 * degree / form.teeth;
    // Down the first tooth's counterclockwise side (its clockwise side's, mirrored) and up the next's clockwise side
    const halfGap = halfGapCurves(form);
    const outside = form.tipRadius + form.module;
    const tipAngle = toothHalfAngle(form, form.tipRadius);
    const top = [
            { "shape" : "line", "points" : [polar(form.tipRadius, pitch - tipAngle), polar(outside, pitch - tipAngle)] },
            { "shape" : "arc", "points" : [polar(outside, pitch - tipAngle), polar(outside, pitch / 2), polar(outside, tipAngle)] },
            { "shape" : "line", "points" : [polar(outside, tipAngle), polar(form.tipRadius, tipAngle)] }
        ];
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    sketchCurves(sketch, "gap", scaledCurves(concatenateArrays([mirroredCurves(halfGap), rotatedCurves(halfGap, pitch), top]), scale));
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}
