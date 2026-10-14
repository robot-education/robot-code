FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Involute spur gears and racks, generated in code: each tooth's flanks are involutes (fit splines through points
 * computed on them), its tip an arc, and its root a fillet (or a full round) and an arc.
 */

/**
 * A gear's or rack's tooth form, for a module (`m`) and pressure angle (`α`): standard proportions (an addendum of `m`
 * and a dedendum of `1.25 m`), with a root fillet of `filletRadius`, and the whole profile offset outward by `offset`
 * (inward, if negative: for clearance).
 *
 * @returns {{
 *      @field module {ValueWithUnits} :
 *      @field pressureAngle {ValueWithUnits} :
 *      @field teeth {number} : `undefined` for a rack.
 *      @field pitchRadius {ValueWithUnits} : A gear's.
 *      @field baseRadius {ValueWithUnits} : A gear's: where its involutes start.
 *      @field tipRadius {ValueWithUnits} : A gear's.
 *      @field rootRadius {ValueWithUnits} : A gear's.
 *      @field filletRadius {ValueWithUnits} :
 *      @field offset {ValueWithUnits} :
 * }}
 */
export function getGearForm(module is ValueWithUnits, teeth, pressureAngle is ValueWithUnits, filletRadius is ValueWithUnits, offset is ValueWithUnits) returns map
{
    var form = {
        "module" : module,
        "teeth" : teeth,
        "pressureAngle" : pressureAngle,
        "filletRadius" : filletRadius - offset,
        "offset" : offset
    };
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

/**
 * The standard root fillet's radius: 0.38 of the module.
 */
export function standardFilletRadius(module is ValueWithUnits) returns ValueWithUnits
{
    return 0.38 * module;
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

/**
 * Sketches a gear's profile on `plane`, centered on it, and returns its faces: every tooth, or (with `sectorTeeth`) a
 * sector of that many teeth, centered on the plane's x axis, closed back to its center.
 */
export function sketchGearProfile(context is Context, id is Id, plane is Plane, form is map, sectorTeeth) returns Query
{
    const teeth = form.teeth;
    const toothAngle = 360 * degree / teeth;
    const root = gearRoot(form);
    if (toothHalfAngle(form, form.tipRadius) <= 0 * degree)
    {
        throw regenError("The gear's teeth come to points: give it more teeth, or less clearance.", ["teeth"]);
    }
    const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * radius;
        };

    // A tooth centered at angle 0, from the middle of the gap before it (on its clockwise side) to the middle of the
    // gap after it, as curves: each a "line", "arc" (with a middle point), or "spline" (points), as its shape
    var halfGap = [];
    if (root.fullRound)
    {
        // A full round: from the bottom of the gap up to the line's tangent point
        halfGap = append(halfGap, {
                        "shape" : "arc",
                        "points" : [at(root.bottomRadius, -toothAngle / 2), root.filletMiddle, root.lineTangent]
                    });
    }
    else
    {
        halfGap = append(halfGap, {
                        "shape" : "arc",
                        "points" : [at(root.rootRadius, -toothAngle / 2), at(root.rootRadius, (root.filletRootAngle - toothAngle / 2) / 2), at(root.rootRadius, root.filletRootAngle)]
                    });
        halfGap = append(halfGap, {
                        "shape" : "arc",
                        "points" : [at(root.rootRadius, root.filletRootAngle), root.filletMiddle, root.lineTangent]
                    });
    }
    if (root.lineTop > norm(root.lineTangent))
    {
        halfGap = append(halfGap, { "shape" : "line", "points" : [root.lineTangent, at(root.lineTop, -root.lineAngle)] });
    }
    var flank = [];
    for (var i = 0; i <= FLANK_POINTS; i += 1)
    {
        const radius = root.lineTop + (form.tipRadius - root.lineTop) * i / FLANK_POINTS;
        flank = append(flank, at(radius, -toothHalfAngle(form, radius)));
    }
    halfGap = append(halfGap, { "shape" : "spline", "points" : flank });

    const tipHalf = toothHalfAngle(form, form.tipRadius);
    var curves = halfGap;
    curves = append(curves, {
                    "shape" : "arc",
                    "points" : [at(form.tipRadius, -tipHalf), at(form.tipRadius, 0 * degree), at(form.tipRadius, tipHalf)]
                });
    // The other side, mirrored, in reverse
    for (var i = size(halfGap) - 1; i >= 0; i -= 1)
    {
        curves = append(curves, {
                        "shape" : halfGap[i].shape,
                        "points" : reverse(mapArray(halfGap[i].points, function(point)
                                {
                                    return vector(point[0], -point[1]);
                                }))
                    });
    }

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const toothCount = sectorTeeth ?? teeth;
    // A sector's centered on the x axis
    const firstAngle = sectorTeeth == undefined ? 0 * degree : -(sectorTeeth - 1) / 2 * toothAngle;
    for (var tooth = 0; tooth < toothCount; tooth += 1)
    {
        const angle = firstAngle + tooth * toothAngle;
        const rotate = function(point is Vector) returns Vector
            {
                return vector(point[0] * cos(angle) - point[1] * sin(angle), point[0] * sin(angle) + point[1] * cos(angle));
            };
        for (var i, curve in curves)
        {
            const points = mapArray(curve.points, rotate);
            const name = "tooth" ~ tooth ~ "_" ~ i;
            if (curve.shape == "line")
            {
                skLineSegment(sketch, name, { "start" : points[0], "end" : points[1] });
            }
            else if (curve.shape == "arc")
            {
                skArc(sketch, name, { "start" : points[0], "mid" : points[1], "end" : points[2] });
            }
            else
            {
                skFitSpline(sketch, name, { "points" : points });
            }
        }
    }
    if (sectorTeeth != undefined)
    {
        // Back to the center, from the middles of the gaps at its ends
        const bottom = root.fullRound ? root.bottomRadius : root.rootRadius;
        skLineSegment(sketch, "sectorStart", { "start" : vector(0, 0) * meter, "end" : at(bottom, firstAngle - toothAngle / 2) });
        skLineSegment(sketch, "sectorEnd", { "start" : at(bottom, firstAngle + (sectorTeeth - 0.5) * toothAngle), "end" : vector(0, 0) * meter });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * The root of a gear's gaps, on a tooth's clockwise side (for a tooth centered at angle 0): a fillet between its root
 * circle and a radial line up to where its involute starts (its base circle, or above it, up to half the tooth's
 * height, to make room for the fillet), or, if two fillets don't fit in the gap, a full round.
 *
 * @returns {{
 *      @field rootRadius {ValueWithUnits} :
 *      @field fullRound {boolean} :
 *      @field bottomRadius {ValueWithUnits} : A full round's bottom.
 *      @field filletRootAngle {ValueWithUnits} : Where a fillet meets the root circle.
 *      @field filletMiddle {Vector} :
 *      @field lineTangent {Vector} : Where it meets the radial line.
 *      @field lineAngle {ValueWithUnits} : The radial line's angle (clockwise of the tooth's center).
 *      @field lineTop {ValueWithUnits} : Where the radial line meets the involute.
 * }}
 */
function gearRoot(form is map) returns map
{
    const rootRadius = form.rootRadius;
    const fillet = form.filletRadius;
    if (rootRadius <= 0 * meter)
    {
        throw regenError("The gear's too small for its teeth: give it more teeth.", ["teeth"]);
    }
    // The profile's offset out by at least the root's round
    if (fillet <= 0 * meter)
    {
        throw regenError("The profile offset is bigger than the roots' round: use less clearance, or a bigger router bit.", ["profileOffsetDistance"]);
    }
    // Room for the fillet below the involute, up to half the tooth's height: above the base circle the line's a little
    // inside the involute, relieving it rather than interfering
    const lineTop = max(form.baseRadius, rootRadius + min(2 * fillet, (form.tipRadius - rootRadius) / 2));
    const lineAngle = toothHalfAngle(form, lineTop);
    const halfGap = 180 * degree / form.teeth - lineAngle;
    const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * radius;
        };

    // A fillet tangent to the root circle (outside it) and the line (on the gap's side)
    const centerRadius = rootRadius + fillet;
    const offAngle = asin(fillet / centerRadius);
    if (offAngle < halfGap && centerRadius * cos(offAngle) <= lineTop)
    {
        const centerAngle = -lineAngle - offAngle;
        const center = at(centerRadius, centerAngle);
        const lineTangent = at(centerRadius * cos(offAngle), -lineAngle);
        const rootTangent = at(rootRadius, centerAngle);
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
    if (halfGap <= 0 * degree || tangentRadius > lineTop)
    {
        throw regenError("The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module.", ["bitDiameter"]);
    }
    const gapAngle = -180 * degree / form.teeth;
    const center = at(roundCenterRadius, gapAngle);
    const lineTangent = at(tangentRadius, -lineAngle);
    const bottom = at(roundCenterRadius - fillet, gapAngle);
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
 * to the root. With a `relief` radius (a router bit's), the roots are rounded (see `relieveRackRoots`), which puts
 * arcs in the regions: a map `{ "mid" : Vector }` between two points is an arc through it, rather than a line.
 */
export function rackCutRegions(form is map, faceHeight is ValueWithUnits, length is ValueWithUnits, centers is array, relief is ValueWithUnits) returns array
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
    if (relief <= 0 * meter)
    {
        return regions;
    }
    return mapArray(regions, region => relieveRackRoots(form, region, relief, root - faceHeight, -faceHeight - form.module));
}

const RACK_ROUND_DOESNT_FIT = "The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module.";

/**
 * `region` (see `rackCutRegions`) with its roots rounded to `radius`, as a router bit cuts them: each corner at the root
 * (`rootY`) between the floor and a tooth's flank becomes an arc tangent to both. Where a gap's floor is too narrow for
 * both, it's a full round instead: an arc tangent to both flanks, centered in the gap, which a bit can't cut below. Its
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
            if (left.after[0] <= right.before[0] + TOLERANCE.zeroLength * meter)
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
 * the root to a module past the tip, between the tooth centered on the plane's x axis and the next one around. Its
 * flanks are involutes (fit splines) down to the base circle, and radial lines below it; its root is an arc, without
 * a fillet. For cutting a bevel gear's teeth (see `bevelCones`), whose `form` can have a fractional number of teeth.
 */
export function sketchToothGap(context is Context, id is Id, plane is Plane, form is map) returns Query
{
    const pitch = 360 * degree / form.teeth;
    if (form.rootRadius <= 0 * meter)
    {
        throw regenError("The gear's too small for its teeth: give it more teeth.", ["teeth"]);
    }
    if (toothHalfAngle(form, form.tipRadius) <= 0 * degree)
    {
        throw regenError("The gear's teeth come to points: give it more teeth, or less clearance.", ["teeth"]);
    }
    const bottom = max(form.baseRadius, form.rootRadius);
    if (toothHalfAngle(form, bottom) >= pitch / 2)
    {
        throw regenError("The gear's teeth meet at their roots: use more clearance.", ["profileOffsetDistance"]);
    }
    const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * radius;
        };
    // The lower flank is the first tooth's, and the upper the next's, mirrored across the gap's middle
    var lower = [];
    var upper = [];
    for (var i = 0; i <= FLANK_POINTS; i += 1)
    {
        const radius = bottom + (form.tipRadius - bottom) * i / FLANK_POINTS;
        lower = append(lower, at(radius, toothHalfAngle(form, radius)));
        upper = append(upper, at(radius, pitch - toothHalfAngle(form, radius)));
    }
    const outside = form.tipRadius + form.module;
    const tipAngle = toothHalfAngle(form, form.tipRadius);
    const bottomAngle = toothHalfAngle(form, bottom);

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skFitSpline(sketch, "lowerFlank", { "points" : lower });
    skLineSegment(sketch, "lowerTop", { "start" : lower[FLANK_POINTS], "end" : at(outside, tipAngle) });
    skArc(sketch, "top", { "start" : at(outside, tipAngle), "mid" : at(outside, pitch / 2), "end" : at(outside, pitch - tipAngle) });
    skLineSegment(sketch, "upperTop", { "start" : at(outside, pitch - tipAngle), "end" : upper[FLANK_POINTS] });
    skFitSpline(sketch, "upperFlank", { "points" : reverse(upper) });
    if (form.rootRadius < bottom)
    {
        // Radial lines from the base circle down to the root
        skLineSegment(sketch, "upperRadial", { "start" : upper[0], "end" : at(form.rootRadius, pitch - bottomAngle) });
        skArc(sketch, "root", { "start" : at(form.rootRadius, pitch - bottomAngle), "mid" : at(form.rootRadius, pitch / 2), "end" : at(form.rootRadius, bottomAngle) });
        skLineSegment(sketch, "lowerRadial", { "start" : at(form.rootRadius, bottomAngle), "end" : lower[0] });
    }
    else
    {
        skArc(sketch, "root", { "start" : upper[0], "mid" : at(form.rootRadius, pitch / 2), "end" : lower[0] });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}
