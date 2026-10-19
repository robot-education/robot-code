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
 * length, a router bit's radius (less the offset, as the profile's offset grows into the gap), with a full round of
 * that radius where the gap's too narrow for both corners.
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
 * line. An involute offset along its normal is the same involute rotated, by the offset over the base radius.
 */
export function toothHalfAngle(form is map, radius is ValueWithUnits) returns ValueWithUnits
{
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
        throw regenError("The gear's teeth come to points: give it more teeth, or less clearance.", ["teeth"]);
    }
    const toothAngle = 360 * degree / form.teeth;
    const root = gearRoot(form);
    var curves = [];
    if (root.fullRound)
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
    if (root.lineTop > norm(root.lineTangent) + TOLERANCE.zeroLength * meter)
    {
        curves = append(curves, { "shape" : "line", "points" : [root.lineTangent, polar(root.lineTop, -root.lineAngle)] });
    }
    var flank = [];
    for (var i = 0; i <= FLANK_POINTS; i += 1)
    {
        const radius = root.lineTop + (form.tipRadius - root.lineTop) * i / FLANK_POINTS;
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
 * The root of a gear's gaps, on a tooth's clockwise side (for a tooth centered at angle 0): below its involute, a
 * radial line up to where the involute starts (its base circle, or above it, up to half the tooth's height, to make
 * room for the fillet), and a fillet between it and the root circle (`form.filletFraction` of the full round's radius,
 * or a router bit's `form.filletRadius`); or a full round, tangent to both sides' lines (and, by fraction, the root
 * circle; a router bit's, where two of its fillets don't fit in the gap); or without a fillet, a sharp corner.
 *
 * @returns {{
 *      @field rootRadius {ValueWithUnits} :
 *      @field fullRound {boolean} :
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
        throw regenError("The gear's too small for its teeth: give it more teeth.", ["teeth"]);
    }
    // Room for the fillet below the involute, up to half the tooth's height: above the base circle the line's a little
    // inside the involute, relieving it rather than interfering
    const lineTopFor = function(fillet is ValueWithUnits) returns ValueWithUnits
        {
            return max(form.baseRadius, rootRadius + min(2 * fillet, (form.tipRadius - rootRadius) / 2));
        };
    // Half the gap's angle, between the lines
    const halfGapAt = function(lineTop is ValueWithUnits) returns ValueWithUnits
        {
            const halfGap = 180 * degree / form.teeth - toothHalfAngle(form, lineTop);
            if (halfGap <= 0 * degree)
            {
                throw regenError("The gear's teeth meet at their roots: use more clearance.", ["profileOffsetDistance"]);
            }
            return halfGap;
        };

    var fillet;
    var lineTop;
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
        if ((rootRadius + full) * cos(halfGapAt(lineTop)) > lineTop + TOLERANCE.zeroLength * meter)
        {
            throw regenError("The root's full round doesn't fit below the teeth's flanks: use a smaller root fillet.", ["rootFillet"]);
        }
        fillet = full * form.filletFraction;
    }
    else
    {
        fillet = form.filletRadius;
        // The profile's offset out by at least the root's round
        if (fillet <= 0 * meter)
        {
            throw regenError("The profile offset is bigger than the roots' round: use less clearance, or a bigger router bit.", ["profileOffsetDistance"]);
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
    if (form.filletFraction != 1 && offAngle < halfGap && centerRadius * cos(offAngle) <= lineTop)
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
    const tangentRadius = roundCenterRadius * cos(halfGap);
    if (tangentRadius > lineTop + TOLERANCE.zeroLength * meter)
    {
        throw regenError("The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module.", ["bitDiameter"]);
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
        throw regenError("The rack's teeth come to points: use less clearance.", ["profileOffsetDistance"]);
    }
    if (rackHalfWidth(form, root) >= PI * form.module / 2)
    {
        throw regenError("The rack's teeth meet at their roots: use more clearance.", ["profileOffsetDistance"]);
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
    return mapArray(regions, region => relieveRackRoots(form, region, radius, root - faceHeight, -faceHeight - form.module));
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

const RACK_ROUND_DOESNT_FIT = "The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module.";

/**
 * `region` (see `rackCutRegions`) with its roots rounded to `radius`: each corner at the root (`rootY`) between the
 * floor and a tooth's flank becomes an arc tangent to both (a full round's two meet in the floor's middle). Where a
 * gap's floor is too narrow for both (a router bit's), it's a full round of that radius instead: an arc tangent to
 * both flanks, centered in the gap, which a bit can't cut below. Its
 * bottom must be below where a mating gear's tips reach (`tipReach`); a corner at an end's wall (not a flank) is left.
 */
export function relieveRackRoots(form is map, region is array, radius is ValueWithUnits, rootY is ValueWithUnits, tipReach is ValueWithUnits) returns array
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
    // the end of the edge before (or after) it. It mustn't run past the flank's.
    const fillet = function(i is number) returns map
        {
            const p = point(i);
            const u = normalize(point(i - 1) - p);
            const v = normalize(point(i + 1) - p);
            const half = acos(dot(u, v)) / 2;
            const along = radius / tan(half);
            const pastBefore = along > norm(point(i - 1) - p) + TOLERANCE.zeroLength * meter;
            const pastAfter = along > norm(point(i + 1) - p) + TOLERANCE.zeroLength * meter;
            if ((pastBefore && !atRoot(point(i - 1))) || (pastAfter && !atRoot(point(i + 1))))
            {
                throw regenError(RACK_ROUND_DOESNT_FIT, ["bitDiameter"]);
            }
            const bisector = normalize(u + v);
            const center = p + bisector * radius / sin(half);
            return {
                    "before" : p + u * along,
                    "mid" : center - bisector * radius,
                    "after" : p + v * along,
                    "center" : center,
                    "pastBefore" : pastBefore,
                    "pastAfter" : pastAfter
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
            // A gap's floor: fillets at both ends, or a full round if they'd overlap
            const left = fillet(i);
            const right = fillet(i + 1);
            if (tolerantEquals(left.after, right.before))
            {
                // Full rounds' fillets, meeting in the floor's middle
                result = concatenateArrays([result, [left.before, { "mid" : left.mid }, left.after, { "mid" : right.mid }, right.after]]);
            }
            else if (left.after[0] < right.before[0])
            {
                result = concatenateArrays([result, [left.before, { "mid" : left.mid }, left.after, right.before, { "mid" : right.mid }, right.after]]);
            }
            else
            {
                result = concatenateArrays([result, fullRound(point(i - 1), point(i), point(i + 1), point(i + 2), radius, tipReach)]);
            }
            i += 2;
        }
        else if (flankCorner(i))
        {
            const corner = fillet(i);
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
 * A full round across a gap's floor, from `p` to `q`: an arc of `radius` tangent to the flanks from `above` down to `p`,
 * and from `q` up to `after`, centered between them. Its bottom must be below `tipReach`.
 */
function fullRound(above is Vector, p is Vector, q is Vector, after is Vector, radius is ValueWithUnits, tipReach is ValueWithUnits) returns array
{
    const up = normalize(above - p);
    // Into the gap, square to the flank
    const inward = vector(up[1], -up[0]);
    const middle = (p[0] + q[0]) / 2;
    // Its center, on the gap's middle, is the radius from the flank
    const height = p[1] + (radius - (middle - p[0]) * inward[0]) / inward[1];
    const center = vector(middle, height);
    const bottom = height - radius;
    if (bottom > tipReach)
    {
        throw regenError(RACK_ROUND_DOESNT_FIT, ["bitDiameter"]);
    }
    const left = center - inward * radius;
    if (dot(left - p, up) > norm(above - p))
    {
        throw regenError(RACK_ROUND_DOESNT_FIT, ["bitDiameter"]);
    }
    const right = vector(2 * middle - left[0], left[1]);
    return [left, { "mid" : vector(middle, bottom) }, right];
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
 * A straight bevel gear's cones, by Tredgold's approximation, for `teeth` meshing with `mateTeeth` with their shafts
 * `shaftAngle` apart: its pitch cone's angle from its axis, its pitch circle's radius (at its back), the cone
 * distance (along its pitch cone, from its apex to that circle), and its virtual teeth. Its teeth are a spur gear's on
 * its back cone (the cone square to its pitch cone at its back), whose radius is the pitch radius over the pitch
 * angle's cosine: a spur gear with `teeth / cos(pitchAngle)` teeth, not a whole number.
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
 * Sketches the gap between two of a gear's teeth on `plane` (centered on the gear's center), and returns its face: from
 * its root (rounded as `halfGapCurves` rounds a gear's) to a module past the tip, between the tooth centered on the
 * plane's x axis and the next one around. For cutting a bevel gear's teeth (see `bevelCones`), whose `form` can have a
 * fractional number of teeth.
 */
export function sketchToothGap(context is Context, id is Id, plane is Plane, form is map) returns Query
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
    sketchCurves(sketch, "gap", concatenateArrays([mirroredCurves(halfGap), rotatedCurves(halfGap, pitch), top]));
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}
