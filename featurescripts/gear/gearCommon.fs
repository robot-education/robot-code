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
    if (fillet <= 0 * meter || rootRadius <= 0 * meter)
    {
        throw regenError("The gear's too small for its teeth: give it more teeth.", ["teeth"]);
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
 * Sketches a rack's profile on `plane`, and returns its face: `teeth` teeth along the plane's x axis from its origin,
 * with its pitch line on the x axis, its teeth toward +y, and its back `height` below its pitch line.
 */
export function sketchRackProfile(context is Context, id is Id, plane is Plane, form is map, teeth is number, height is ValueWithUnits) returns Query
{
    const m = form.module;
    const pitch = PI * m;
    const tip = m + form.offset;
    const root = -1.25 * m + form.offset;
    if (height <= -root)
    {
        throw regenError("The rack's back must be below its teeth's roots.", ["rackHeight"]);
    }
    // Half a tooth's width at a height above the pitch line: its flank's an offset line
    const halfWidth = function(y is ValueWithUnits) returns ValueWithUnits
        {
            return pitch / 4 + form.offset / cos(form.pressureAngle) - y * tan(form.pressureAngle);
        };
    if (halfWidth(tip) <= 0 * meter)
    {
        throw regenError("The rack's teeth come to points: use less clearance.", ["profileOffsetDistance"]);
    }
    if (halfWidth(root) >= pitch / 2)
    {
        throw regenError("The rack's teeth meet at their roots: use more clearance.", ["profileOffsetDistance"]);
    }
    var points = [vector(0 * meter, -height), vector(0 * meter, root)];
    for (var tooth = 0; tooth < teeth; tooth += 1)
    {
        const center = (tooth + 0.5) * pitch;
        points = concatenateArrays([points, [
                        vector(center - halfWidth(root), root),
                        vector(center - halfWidth(tip), tip),
                        vector(center + halfWidth(tip), tip),
                        vector(center + halfWidth(root), root)
                    ]]);
    }
    points = concatenateArrays([points, [vector(teeth * pitch, root), vector(teeth * pitch, -height)]]);

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    for (var i, point in points)
    {
        skLineSegment(sketch, "line" ~ i, { "start" : point, "end" : points[(i + 1) % size(points)] });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}
