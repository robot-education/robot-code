FeatureScript 1589;
import(path : "onshape/std/geometry.fs", version : "1589.0");

// Tube From Boxes
// Draw each tube's footprint in a sketch and pick the regions. The two longest parallel sides are the
// tube's sides; the rest of the outline forms the two ends, which can be square, angled, or made of
// several segments (notched). End cuts go straight through the tube's height.
//
// Wide faces can also take REV's MAX Pattern (MAXSpline cutouts every 2", shape from REV's own STEP file)
// or SWYFT's bearing pattern (1.130" bearing holes every 2" with a 3-row grid around them).
//
// Speed:
//  - Walls are extruded from sketches that already contain the holes (no shell, no hole subtraction).
//  - Outlines that are identical or mirror images of each other are built once and copied/mirrored.
//  - All temporary sketches are deleted in a single step at the end.
//
// Half holes: the hole pattern runs right through the tube's ends, like stock cut on a saw. Holes that
// would only nick an end, or leave a sliver thinner than MIN_WEB, are skipped.

export enum HeightSide
{
    annotation { "Name" : "On top of sketch" }
    UP,
    annotation { "Name" : "Below sketch" }
    DOWN,
    annotation { "Name" : "Centered on sketch" }
    CENTERED
}

export enum WideRows
{
    annotation { "Name" : "3 rows, 0.5\" apart" }
    THREE_HALF,
    annotation { "Name" : "2 rows, 1\" apart" }
    TWO_ONE,
    annotation { "Name" : "2 rows, 1.5\" apart" }
    TWO_ONEHALF,
    annotation { "Name" : "4 rows, 0.5\" apart" }
    FOUR_HALF,
    annotation { "Name" : "1 center row" }
    ONE,
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "REV MAX Pattern" }
    MAX_PATTERN,
    annotation { "Name" : "SWYFT bearing pattern" }
    SWYFT_BEARING
}

export enum NarrowRows
{
    annotation { "Name" : "1 center row" }
    ONE,
    annotation { "Name" : "2 rows, 0.5\" apart" }
    TWO,
    annotation { "Name" : "None" }
    NONE
}

export enum BoxPatternBase
{
    annotation { "Name" : "Centered on tube" }
    CENTERED,
    annotation { "Name" : "From one end" }
    FROM_END
}

export enum OffsetType
{
    annotation { "Name" : "Distance" }
    DISTANCE,
    annotation { "Name" : "Up to point / line / plane" }
    UP_TO
}

const WALL_BOUNDS = { (inch) : [0.01, 0.0625, 0.25] } as LengthBoundSpec;
const HEIGHT_BOUNDS = { (inch) : [0.25, 2, 12] } as LengthBoundSpec;
const SPACING_BOUNDS = { (inch) : [0.25, 0.5, 4] } as LengthBoundSpec;
const HOLE_BOUNDS = { (inch) : [0.01, 0.196, 1] } as LengthBoundSpec;
const PATTERN_OFS_BOUNDS = { (inch) : [-5, 0, 5] } as LengthBoundSpec;
const END_MARGIN_BOUNDS = { (inch) : [-1, 0, 1] } as LengthBoundSpec;
const OFFSET_BOUNDS = { (inch) : [0, 1, 100] } as LengthBoundSpec;

const WIDE_FACE = 1.5 * inch;      // faces wider than this use the wide-face pattern
const MIN_WEB = 0.02 * inch;       // half holes: skip a hole if an end would leave a sliver thinner than this
const BEARING_R = 0.565 * inch;    // SWYFT's bearing holes are 1.130" for 1.125" hex bearings
const TOL = 1e-6 * meter;

annotation { "Feature Type Name" : "Tube From Boxes", "Feature Type Description" : "Build hole-patterned box tubes from outlines drawn in a sketch." }
export const tubeFromBoxes = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Sketch outlines", "Filter" : EntityType.FACE && SketchObject.YES }
        definition.regions is Query;

        annotation { "Name" : "Height", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
        isLength(definition.height, HEIGHT_BOUNDS);

        annotation { "Name" : "Height side", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE, UIHint.SHOW_LABEL] }
        definition.heightSide is HeightSide;

        annotation { "Name" : "Offset", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
        definition.doOffset is boolean;
        if (definition.doOffset)
        {
            annotation { "Name" : "Offset type", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE, UIHint.SHOW_LABEL] }
            definition.offsetType is OffsetType;
            if (definition.offsetType == OffsetType.DISTANCE)
            {
                annotation { "Name" : "Offset distance", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
                isLength(definition.offsetDistance, OFFSET_BOUNDS);

                annotation { "Name" : "Opposite direction", "UIHint" : [UIHint.OPPOSITE_DIRECTION] }
                definition.offsetFlip is boolean;
            }
            else
            {
                annotation { "Name" : "Offset to", "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.LINE) || GeometryType.PLANE || BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
                definition.offsetRef is Query;
            }
        }

        annotation { "Name" : "Wall thickness", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
        isLength(definition.wall, WALL_BOUNDS);

        annotation { "Name" : "Hole pattern", "Default" : true, "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
        definition.doHoles is boolean;

        if (definition.doHoles)
        {
            annotation { "Name" : "Wide (2\") face pattern", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE, UIHint.SHOW_LABEL] }
            definition.wideRows is WideRows;

            annotation { "Name" : "Narrow (1\") face rows", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE, UIHint.SHOW_LABEL] }
            definition.narrowRows is NarrowRows;

            annotation { "Name" : "Hole spacing", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
            isLength(definition.spacing, SPACING_BOUNDS);

            annotation { "Name" : "Hole diameter", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
            isLength(definition.holeDia, HOLE_BOUNDS);

            annotation { "Name" : "Pattern base", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE, UIHint.SHOW_LABEL] }
            definition.patternBase is BoxPatternBase;
            if (definition.patternBase == BoxPatternBase.FROM_END)
            {
                annotation { "Name" : "Use other end", "UIHint" : [UIHint.OPPOSITE_DIRECTION] }
                definition.flipEnd is boolean;
            }

            annotation { "Name" : "Pattern offset", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
            isLength(definition.patternOfs, PATTERN_OFS_BOUNDS);

            annotation { "Name" : "Half holes at ends", "Default" : true, "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
            definition.halfHoles is boolean;
            if (!definition.halfHoles)
            {
                annotation { "Name" : "Extra end margin", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
                isLength(definition.endMargin, END_MARGIN_BOUNDS);
            }
        }

        annotation { "Name" : "Name in metric", "UIHint" : [UIHint.REMEMBER_PREVIOUS_VALUE] }
        definition.metricName is boolean;
    }
    {
        const regions = evaluateQuery(context, definition.regions);
        if (size(regions) == 0)
        {
            throw regenError("Select one or more sketch outlines.", ["regions"]);
        }

        // 1. Read every outline first (just math, no geometry yet)
        var tubes = [];
        for (var region in regions)
        {
            const sketchPlane = evOwnerSketchPlane(context, { "entity" : region });
            const n = sketchPlane.normal;
            const offset = offsetFor(context, definition, sketchPlane);
            var fp = normalizeFootprint(readFootprint(context, region, n), n, region, offset);
            fp.spec = placeSpec(definition, fp);
            tubes = append(tubes, fp);
        }

        // 2. Group outlines that are identical or mirror images of each other
        var sources = [];
        var copies = [];
        for (var j = 0; j < size(tubes); j += 1)
        {
            var placed = false;
            for (var g = 0; g < size(sources); g += 1)
            {
                const sym = matchSymmetry(tubes[sources[g]], tubes[j]);
                if (sym >= 0)
                {
                    copies[g] = append(copies[g], { "tube" : j, "sym" : sym });
                    placed = true;
                    break;
                }
            }
            if (!placed)
            {
                sources = append(sources, j);
                copies = append(copies, []);
            }
        }

        // 3. Build each unique shape once
        for (var g = 0; g < size(sources); g += 1)
        {
            buildPlateTube(context, id + ("tube" ~ g), definition, tubes[sources[g]]);
        }

        // 4. One cleanup for every temporary sketch
        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qSketchFilter(qCreatedBy(id, EntityType.BODY), SketchObject.YES)
                });

        // 5. Copy or mirror each shape onto its matching outlines, then name each group
        for (var g = 0; g < size(sources); g += 1)
        {
            const src = tubes[sources[g]];
            const srcBody = qCreatedBy(id + ("tube" ~ g), EntityType.BODY);
            var group = [srcBody];
            if (size(copies[g]) > 0)
            {
                const fromSrc = fromWorld(src.frame);
                var transforms = [];
                var names = [];
                for (var c in copies[g])
                {
                    transforms = append(transforms, toWorld(tubes[c.tube].frame) * symTransform(c.sym, src.L) * fromSrc);
                    names = append(names, "copy" ~ c.tube);
                }
                opPattern(context, id + ("copies" ~ g), {
                            "entities" : srcBody,
                            "transforms" : transforms,
                            "instanceNames" : names
                        });
                group = append(group, qCreatedBy(id + ("copies" ~ g), EntityType.BODY));
            }
            setProperty(context, {
                        "entities" : qUnion(group),
                        "propertyType" : PropertyType.NAME,
                        "value" : tubeName(definition, src)
                    });
        }

        // taken from http://asm.matweb.com/search/SpecificMaterial.asp?bassnum=MA6061T6
        setProperty(context, {
                    "entities" : qCreatedBy(id, EntityType.BODY),
                    "propertyType" : PropertyType.MATERIAL,
                    "value" : material("6061-T6", 0.0975 * pound / inch ^ 3)
                });
    });

// ===================== Face patterns =====================

const NO_FACE = { "kind" : "none", "rows" : [], "reach" : 0 * inch };

/**
 * The tube's height and the hole pattern for its sides (normal +/-s) and its top and bottom (normal +/-n).
 * Each face gets the wide-face pattern if it's wider than WIDE_FACE, else the narrow-face rows.
 */
function placeSpec(def is map, fp is map) returns map
{
    const t = def.wall;
    const H = def.height;
    const w = fp.w;
    var sideFace = NO_FACE;
    var topFace = NO_FACE;
    var phase = 0.5 * inch;
    if (def.doHoles)
    {
        sideFace = facePattern(def, H, t);
        topFace = facePattern(def, w, t);
        if (def.wideRows == WideRows.MAX_PATTERN || def.wideRows == WideRows.SWYFT_BEARING)
        {
            phase = 1.5 * inch; // stock starts with a hole column 0.5" in, so the first cutout sits 1.5" in
        }
    }
    return { "wall" : t, "H" : H, "phase" : phase, "sideFace" : sideFace, "topFace" : topFace };
}

function facePattern(def is map, faceWidth is ValueWithUnits, t is ValueWithUnits) returns map
{
    const r = def.holeDia / 2;
    const rows3 = [-0.5 * inch, 0 * inch, 0.5 * inch];
    var face;
    if (faceWidth > WIDE_FACE && def.wideRows == WideRows.MAX_PATTERN)
    {
        face = { "kind" : "max", "rows" : rows3, "r" : r, "shape" : MAXSPLINE, "bigR" : MAXSPLINE_R,
                "reach" : max(MAXSPLINE_HALF_WIDTH, 0.5 * inch + r) };
    }
    else if (faceWidth > WIDE_FACE && def.wideRows == WideRows.SWYFT_BEARING)
    {
        face = { "kind" : "bearing", "rows" : rows3, "pitch" : def.spacing, "r" : r, "bigR" : BEARING_R,
                "reach" : max(BEARING_R, 0.5 * inch + r) };
    }
    else
    {
        const rows = rowOffsets(def, faceWidth);
        var reach = 0 * inch;
        for (var row in rows)
        {
            reach = max(reach, abs(row) + r);
        }
        face = { "kind" : size(rows) > 0 ? "grid" : "none", "rows" : rows, "pitch" : def.spacing, "r" : r, "reach" : reach };
    }
    if (face.reach > faceWidth / 2 - t - 0.005 * inch)
    {
        throw regenError("The hole pattern doesn't fit on the " ~ roundToPrecision(faceWidth / inch, 3) ~ "\" face with this wall and hole size.",
            [faceWidth > WIDE_FACE ? "wideRows" : "narrowRows"]);
    }
    return face;
}

function rowOffsets(def is map, faceWidth is ValueWithUnits) returns array
{
    if (faceWidth > WIDE_FACE)
    {
        if (def.wideRows == WideRows.THREE_HALF)
        {
            return [-0.5 * inch, 0 * inch, 0.5 * inch];
        }
        if (def.wideRows == WideRows.TWO_ONE)
        {
            return [-0.5 * inch, 0.5 * inch];
        }
        if (def.wideRows == WideRows.TWO_ONEHALF)
        {
            return [-0.75 * inch, 0.75 * inch];
        }
        if (def.wideRows == WideRows.FOUR_HALF)
        {
            return [-0.75 * inch, -0.25 * inch, 0.25 * inch, 0.75 * inch];
        }
        if (def.wideRows == WideRows.ONE)
        {
            return [0 * inch];
        }
        return []; // None, or a special pattern handled in facePattern
    }
    if (def.narrowRows == NarrowRows.ONE)
    {
        return [0 * inch];
    }
    if (def.narrowRows == NarrowRows.TWO)
    {
        return [-0.25 * inch, 0.25 * inch];
    }
    return [];
}

function tubeName(def is map, fp is map) returns string
{
    const H = fp.spec.H;
    const w = fp.w;
    var scale = inch;
    var precision = 3;
    var unit = "\"";
    if (def.metricName)
    {
        scale = millimeter;
        precision = 0;
        unit = "";
    }
    var name = "Tube " ~ roundToPrecision(max(w, H) / scale, precision) ~ unit ~ "x" ~ roundToPrecision(min(w, H) / scale, precision) ~ unit ~ "x" ~ roundToPrecision(fp.L / scale, precision) ~ unit;
    if (def.metricName)
    {
        name ~= " mm";
    }
    return name;
}

// ===================== Building =====================

/** Distance from the sketch to the tube's mid-height. */
function midHeight(def is map, H is ValueWithUnits) returns ValueWithUnits
{
    if (def.heightSide == HeightSide.DOWN)
    {
        return -H / 2;
    }
    if (def.heightSide == HeightSide.CENTERED)
    {
        return 0 * inch;
    }
    return H / 2;
}

/** Where the hole pattern is anchored along the tube (a hole, MAXSpline or bearing sits here). */
function patternAnchor(def is map, fp is map) returns ValueWithUnits
{
    if (!def.doHoles)
    {
        return 0 * inch;
    }
    const uS0 = chainU(fp.start, 0 * inch, true);
    const uE0 = chainU(fp.end, 0 * inch, false);
    if (def.patternBase == BoxPatternBase.FROM_END)
    {
        const phase = fp.spec.phase;
        return def.flipEnd ? uE0 - phase - def.patternOfs : uS0 + phase + def.patternOfs;
    }
    return (uS0 + uE0) / 2 + def.patternOfs;
}

function isHalf(def is map) returns boolean
{
    if (def.doHoles)
    {
        return def.halfHoles;
    }
    return false;
}

/** Stop-short mode: how far a feature's center must stay from an end. */
function endClearance(def is map, R is ValueWithUnits) returns ValueWithUnits
{
    if (R < 0.15 * inch)
    {
        return max(2 * R + def.endMargin, R + 0.01 * inch);
    }
    return R + 0.1 * inch + def.endMargin;
}

/**
 * Every hole/cutout a face pattern would put between uLo and uHi (plus overhang), in the wall's sketch
 * coordinates: u along the tube, v across the face. Circles carry R; MAXSplines also carry their shape.
 */
function faceFeatures(face is map, anchor is ValueWithUnits, uLo is ValueWithUnits, uHi is ValueWithUnits) returns array
{
    var out = [];
    if (face.kind == "none")
    {
        return out;
    }
    const r = face.r;
    if (face.kind == "max")
    {
        for (var u in gridPositions(anchor, uLo - face.bigR, uHi + face.bigR, 2 * inch))
        {
            out = append(out, { "u" : u, "v" : 0 * inch, "R" : face.bigR, "shape" : face.shape });
        }
        for (var row in face.rows)
        {
            for (var u in gridPositions(anchor + 1 * inch, uLo - r, uHi + r, 2 * inch))
            {
                out = append(out, { "u" : u, "v" : row, "R" : r });
            }
        }
        return out;
    }
    if (face.kind == "bearing")
    {
        for (var u in gridPositions(anchor, uLo - face.bigR, uHi + face.bigR, 2 * inch))
        {
            out = append(out, { "u" : u, "v" : 0 * inch, "R" : face.bigR });
        }
    }
    for (var row in face.rows)
    {
        for (var u in gridPositions(anchor, uLo - r, uHi + r, face.pitch))
        {
            if (face.kind == "bearing")
            {
                // skip grid holes that would run into a bearing hole
                const k = round((u - anchor) / (2 * inch));
                const du = u - anchor - k * 2 * inch;
                if (sqrt(du * du + row * row) < face.bigR + r + 0.02 * inch)
                {
                    continue;
                }
            }
            out = append(out, { "u" : u, "v" : row, "R" : r });
        }
    }
    return out;
}

/** Features on a top/bottom face, kept or dropped against the outline's ends. */
function topFeatures(def is map, face is map, anchor is ValueWithUnits, fp is map) returns array
{
    const half = isHalf(def);
    const segs = chainSegments(fp.start, fp.end);
    const lo = chainRangeU(fp.start)[0];
    const hi = chainRangeU(fp.end)[1];
    var out = [];
    for (var f in faceFeatures(face, anchor, lo, hi))
    {
        const p = vector(f.u, f.v);
        if (half ? topHoleCuts(p, f.R, segs, fp.outline) : (pointInPolygon(p, fp.outline) && clearOfSegments(p, segs, endClearance(def, f.R))))
        {
            out = append(out, f);
        }
    }
    return out;
}

/** Features on a side face whose wall strip runs from sOut/sIn (start cut) to eIn/eOut (end cut). */
function sideFeatures(def is map, face is map, anchor is ValueWithUnits, cuts is array) returns array
{
    const half = isHalf(def);
    var out = [];
    for (var f in faceFeatures(face, anchor, cuts[0], cuts[3]))
    {
        if (half)
        {
            if (sideHoleCuts(f.u, f.R, cuts[0], cuts[1], cuts[2], cuts[3]))
            {
                out = append(out, f);
            }
        }
        else
        {
            const clr = endClearance(def, f.R);
            if (f.u - clr >= cuts[1] - TOL && f.u + clr <= cuts[2] + TOL)
            {
                out = append(out, f);
            }
        }
    }
    return out;
}

/** [sOut, sIn, eIn, eOut]: how far the start and end cuts reach across the strip v1..v2. */
function stripCuts(fp is map, v1 is ValueWithUnits, v2 is ValueWithUnits) returns array
{
    const sIn = chainExtreme(fp.start, v1, v2, true);
    const sOut = chainExtreme(fp.start, v1, v2, false);
    const eIn = chainExtreme(fp.end, v1, v2, false);
    const eOut = chainExtreme(fp.end, v1, v2, true);
    if (eIn <= sIn)
    {
        throw regenError("This tube is too short for its end cuts.", ["regions"], fp.region);
    }
    return [sOut, sIn, eIn, eOut];
}

/**
 * Four flat plates drawn with their holes already in them, joined into one part.
 * Temporary sketches are left for the single cleanup step.
 */
function buildPlateTube(context is Context, id is Id, def is map, fp is map)
{
    const pl = fp.spec;
    const t = pl.wall;
    const H = pl.H;
    const n = fp.n;
    const d = fp.d;
    const s = fp.s;
    const w = fp.w;
    if (2 * t >= min(w, H))
    {
        throw regenError("Wall is too thick for this tube size.", ["wall"], fp.region);
    }
    const origin = fp.origin + n * midHeight(def, H); // (u = 0, v = 0) at the tube's mid-height
    const half = isHalf(def);
    const anchor = patternAnchor(def, fp);
    const startRange = chainRangeU(fp.start);
    const endRange = chainRangeU(fp.end);

    // ---------- Top and bottom walls: exact outline, drilled ----------
    const topV = w / 2 - t / 2; // a point in the wall no hole reaches
    const topU = (chainU(fp.start, topV, true) + chainU(fp.end, topV, false)) / 2;
    const top = makeWall(context, id + "top", origin + n * (H / 2), n, d, fp.outline,
        topFeatures(def, pl.topFace, anchor, fp), t, vector(topU, topV));
    const bottom = copyBody(context, id + "bottom", top, -n * (H - t));
    var parts = [top, bottom];

    // ---------- Side walls ----------
    // Stop short: drilled rectangle between the ends, plus fill pieces at angled ends.
    // Half holes: drilled rectangle out to the ends, then the angled parts trimmed off (holes and all).
    var sides = [];
    var fills = [];
    var trimStart = false;
    var trimEnd = false;
    for (var sign in [1, -1])
    {
        const v1 = sign > 0 ? w / 2 - t : -w / 2;
        const v2 = sign > 0 ? w / 2 : -w / 2 + t;
        const cuts = stripCuts(fp, v1, v2);
        var a = cuts[1];
        var b = cuts[2];
        if (half)
        {
            a = cuts[0];
            b = cuts[3];
            trimStart = trimStart || cuts[1] - cuts[0] > TOL;
            trimEnd = trimEnd || cuts[3] - cuts[2] > TOL;
        }
        sides = append(sides, { "key" : cuts, "a" : a, "b" : b, "features" : sideFeatures(def, pl.sideFace, anchor, cuts) });
        if (!half)
        {
            fills = append(fills, fillPolygon(fp.start, v1, v2, a, true));
            fills = append(fills, fillPolygon(fp.end, v1, v2, b, false));
        }
    }

    const sideV = H / 2 - t - 0.002 * inch; // a point in the side wall no hole reaches
    const sideA = makeWall(context, id + "sideA", origin + s * (w / 2), s, d,
        rectangle(sides[0].a, sides[0].b, H / 2 - t), sides[0].features, t, vector((sides[0].a + sides[0].b) / 2, sideV));
    var sideB;
    var sameSides = true;
    for (var k = 0; k < 4; k += 1)
    {
        if (abs(sides[0].key[k] - sides[1].key[k]) > TOL)
        {
            sameSides = false;
        }
    }
    if (sameSides)
    {
        // Square or symmetric ends: the other side is just a copy
        sideB = copyBody(context, id + "sideB", sideA, -s * (w - t));
    }
    else
    {
        // its sketch runs the other way up (y is +n), so mirror the positions to line up with side A
        sideB = makeWall(context, id + "sideB", origin - s * (w / 2), -s, d,
            rectangle(sides[1].a, sides[1].b, H / 2 - t), flipV(sides[1].features), t, vector((sides[1].a + sides[1].b) / 2, sideV));
    }
    parts = concatenateArrays([parts, [sideA, sideB]]);

    if (trimStart || trimEnd)
    {
        // Cut the side walls back to the angled ends: one tool for both ends, one subtraction
        const sketch = newSketchOnPlane(context, id + "trimSketch", { "sketchPlane" : plane(origin - n * H, n, d) });
        drawEndTrims(sketch, fp, startRange, endRange, 2 * t + 0.1 * inch, trimStart, trimEnd);
        skSolve(sketch);
        opExtrude(context, id + "trimTool", {
                    "entities" : qSketchRegion(id + "trimSketch"),
                    "direction" : n,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 2 * H
                });
        opBoolean(context, id + "trim", {
                    "tools" : qCreatedBy(id + "trimTool", EntityType.BODY),
                    "targets" : qUnion([sideA, sideB]),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }

    var fillPolys = [];
    for (var f in fills)
    {
        if (size(f) > 0)
        {
            fillPolys = append(fillPolys, f);
        }
    }
    if (size(fillPolys) > 0)
    {
        const sketch = newSketchOnPlane(context, id + "fillSketch", { "sketchPlane" : plane(origin - n * (H / 2 - t), n, d) });
        for (var k = 0; k < size(fillPolys); k += 1)
        {
            drawPolygon(sketch, "f" ~ k ~ "s", fillPolys[k]);
        }
        skSolve(sketch);
        opExtrude(context, id + "fill", {
                    "entities" : qSketchRegion(id + "fillSketch"),
                    "direction" : n,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : H - 2 * t
                });
        parts = append(parts, qCreatedBy(id + "fill", EntityType.BODY));
    }

    opBoolean(context, id + "join", {
                "tools" : qUnion(parts),
                "operationType" : BooleanOperationType.UNION
            });
}

function flipV(features is array) returns array
{
    var out = [];
    for (var f in features)
    {
        var g = f;
        g.v = -f.v;
        g.flip = f.flip != true; // mirror the cutout's shape as well, so it lines up with the opposite face
        out = append(out, g);
    }
    return out;
}

// ===================== Offset =====================

/** How far to move tubes from this sketch along its normal. */
function offsetFor(context is Context, def is map, sketchPlane is Plane) returns ValueWithUnits
{
    if (!def.doOffset)
    {
        return 0 * inch;
    }
    if (def.offsetType == OffsetType.DISTANCE)
    {
        return def.offsetFlip ? -def.offsetDistance : def.offsetDistance;
    }

    const ref = def.offsetRef;
    const n = sketchPlane.normal;
    if (isQueryEmpty(context, ref))
    {
        throw regenError("Pick a point, line, plane, or mate connector to offset to.", ["offsetRef"]);
    }
    if (!isQueryEmpty(context, qBodyType(ref, BodyType.MATE_CONNECTOR)))
    {
        const cs = evMateConnector(context, { "mateConnector" : ref });
        return dot(cs.origin - sketchPlane.origin, n);
    }
    if (!isQueryEmpty(context, qEntityFilter(ref, EntityType.VERTEX)))
    {
        return dot(evVertexPoint(context, { "vertex" : ref }) - sketchPlane.origin, n);
    }
    if (!isQueryEmpty(context, qEntityFilter(ref, EntityType.EDGE)))
    {
        const ln = evLine(context, { "edge" : ref });
        if (abs(dot(ln.direction, n)) > 1e-6)
        {
            throw regenError("The offset line has to run parallel to the sketch.", ["offsetRef"], ref);
        }
        return dot(ln.origin - sketchPlane.origin, n);
    }
    const pl = evPlane(context, { "face" : ref });
    if (norm(cross(pl.normal, n)) > 1e-6)
    {
        throw regenError("The offset plane has to be parallel to the sketch.", ["offsetRef"], ref);
    }
    return dot(pl.origin - sketchPlane.origin, n);
}

// ===================== Matching identical / mirrored outlines =====================

/**
 * Returns which symmetry maps outline a onto outline b, or -1 if they don't match.
 * 0 = same, 1 = turned end for end, 2 = mirrored side to side, 3 = mirrored end to end.
 */
function matchSymmetry(a is map, b is map) returns number
{
    if (size(a.outline) != size(b.outline) || abs(a.L - b.L) > TOL || abs(a.w - b.w) > TOL)
    {
        return -1;
    }
    for (var sym = 0; sym < 4; sym += 1)
    {
        var moved = [];
        for (var p in a.outline)
        {
            moved = append(moved, symPoint(p, sym, a.L));
        }
        if (sameVertices(moved, b.outline))
        {
            return sym;
        }
    }
    return -1;
}

function symPoint(p is Vector, sym is number, L is ValueWithUnits) returns Vector
{
    if (sym == 1)
    {
        return vector(L - p[0], -p[1]);
    }
    if (sym == 2)
    {
        return vector(p[0], -p[1]);
    }
    if (sym == 3)
    {
        return vector(L - p[0], p[1]);
    }
    return p;
}

/** The same symmetry as a 3D transform in a tube's local frame (x along the tube, y across, z up). */
function symTransform(sym is number, L is ValueWithUnits) returns Transform
{
    const mid = vector(L / 2, 0 * meter, 0 * meter);
    if (sym == 1)
    {
        return rotationAround(line(mid, Z_DIRECTION), 180 * degree);
    }
    if (sym == 2)
    {
        return mirrorAcross(plane(mid, Y_DIRECTION));
    }
    if (sym == 3)
    {
        return mirrorAcross(plane(mid, X_DIRECTION));
    }
    return identityTransform();
}

function sameVertices(a is array, b is array) returns boolean
{
    if (size(a) != size(b))
    {
        return false;
    }
    for (var p in a)
    {
        var found = false;
        for (var q in b)
        {
            if (norm(p - q) < TOL)
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            return false;
        }
    }
    return true;
}

// ===================== Reading outlines =====================

/**
 * Reads a sketch region's outline. Returns the tube direction, width, the outline in (u, v),
 * and the two end chains, each ordered by v from one side to the other.
 */
function readFootprint(context is Context, region is Query, n is Vector) returns map
{
    // Collect the outline segments
    var segs = [];
    for (var e in evaluateQuery(context, qAdjacent(region, AdjacencyType.EDGE, EntityType.EDGE)))
    {
        if (isQueryEmpty(context, qGeometry(e, GeometryType.LINE)))
        {
            throw regenError("Tube outlines can only use straight lines.", ["regions"], region);
        }
        const ends = evEdgeTangentLines(context, { "edge" : e, "parameters" : [0, 1] });
        segs = append(segs, [ends[0].origin, ends[1].origin]);
    }
    if (size(segs) < 4)
    {
        throw regenError("A tube outline needs at least 4 sides.", ["regions"], region);
    }

    // Chain the segments into one closed loop
    var pts = [segs[0][0], segs[0][1]];
    var used = makeArray(size(segs), false);
    used[0] = true;
    for (var count = 1; count < size(segs); count += 1)
    {
        const last = pts[size(pts) - 1];
        var next = -1;
        var nextPt = last;
        for (var k = 0; k < size(segs); k += 1)
        {
            if (!used[k])
            {
                if (norm(segs[k][0] - last) < TOL)
                {
                    next = k;
                    nextPt = segs[k][1];
                    break;
                }
                if (norm(segs[k][1] - last) < TOL)
                {
                    next = k;
                    nextPt = segs[k][0];
                    break;
                }
            }
        }
        if (next < 0)
        {
            throw regenError("Each tube must be one closed outline with nothing inside it.", ["regions"], region);
        }
        used[next] = true;
        pts = append(pts, nextPt);
    }
    if (norm(pts[size(pts) - 1] - pts[0]) < TOL)
    {
        pts = without(pts, size(pts) - 1);
    }

    // Merge collinear segments (edges get split where other outlines touch)
    var merged = true;
    while (merged && size(pts) > 3)
    {
        merged = false;
        const m = size(pts);
        for (var i = 0; i < m; i += 1)
        {
            const a = pts[(i + m - 1) % m];
            const b = pts[i];
            const c = pts[(i + 1) % m];
            if (norm(cross(b - a, c - b)) <= TOL * (norm(b - a) + norm(c - b)))
            {
                pts = without(pts, i);
                merged = true;
                break;
            }
        }
    }

    // The tube's sides are the longest pair of parallel, non-adjacent outline edges
    const m = size(pts);
    if (m < 4)
    {
        throw regenError("A tube outline needs at least 4 sides.", ["regions"], region);
    }
    var bestI = -1;
    var bestJ = -1;
    var bestLen = 0 * meter;
    for (var i = 0; i < m; i += 1)
    {
        const ei = pts[(i + 1) % m] - pts[i];
        for (var j = i + 2; j < m; j += 1)
        {
            if (i == 0 && j == m - 1)
            {
                continue;
            }
            const ej = pts[(j + 1) % m] - pts[j];
            const total = norm(ei) + norm(ej);
            if (total > bestLen && norm(cross(ei, ej)) < 1e-6 * norm(ei) * norm(ej) &&
                norm(cross(normalize(ei), pts[j] - pts[i])) > TOL)
            {
                bestI = i;
                bestJ = j;
                bestLen = total;
            }
        }
    }
    if (bestI < 0)
    {
        throw regenError("Couldn't find two parallel sides for the tube in this outline.", ["regions"], region);
    }

    const d = normalize(pts[(bestI + 1) % m] - pts[bestI]);
    const s = cross(n, d);
    const ref = pts[bestI];
    const vOther = dot(pts[bestJ] - ref, s);
    const vc = vOther / 2;
    const w = abs(vOther);

    var uv = [];
    for (var p in pts)
    {
        uv = append(uv, vector(dot(p - ref, d), dot(p - ref, s) - vc));
    }
    for (var p in uv)
    {
        if (abs(p[1]) > w / 2 + TOL)
        {
            throw regenError("Part of this outline sticks out past the tube's sides.", ["regions"], region);
        }
    }

    // The two end chains: everything between the two side edges
    var chainA = [];
    var k = (bestI + 1) % m;
    while (true)
    {
        chainA = append(chainA, uv[k]);
        if (k == bestJ)
        {
            break;
        }
        k = (k + 1) % m;
    }
    var chainB = [];
    k = (bestJ + 1) % m;
    while (true)
    {
        chainB = append(chainB, uv[k]);
        if (k == bestI)
        {
            break;
        }
        k = (k + 1) % m;
    }
    chainA = sortChain(chainA, region);
    chainB = sortChain(chainB, region);
    const startIsA = meanU(chainA) < meanU(chainB);

    return {
            "d" : d,
            "s" : s,
            "w" : w,
            "origin" : ref + s * vc,
            "outline" : uv,
            "start" : startIsA ? chainA : chainB,
            "end" : startIsA ? chainB : chainA
        };
}

/** Shifts the outline so u starts at 0, applies the offset, and attaches the tube's local frame. */
function normalizeFootprint(fp is map, n is Vector, region is Query, offset is ValueWithUnits) returns map
{
    var minU = fp.outline[0][0];
    var maxU = minU;
    for (var p in fp.outline)
    {
        minU = min(minU, p[0]);
        maxU = max(maxU, p[0]);
    }
    var out = fp;
    out.outline = shiftU(fp.outline, minU);
    out.start = shiftU(fp.start, minU);
    out.end = shiftU(fp.end, minU);
    out.origin = fp.origin + fp.d * minU + n * offset;
    out.L = maxU - minU;
    out.n = n;
    out.region = region;
    out.frame = coordSystem(out.origin, fp.d, n);
    return out;
}

function shiftU(pts is array, du is ValueWithUnits) returns array
{
    var out = [];
    for (var p in pts)
    {
        out = append(out, vector(p[0] - du, p[1]));
    }
    return out;
}

function sortChain(chain is array, region is Query) returns array
{
    var c = chain;
    if (c[0][1] > c[size(c) - 1][1])
    {
        c = reverse(c);
    }
    for (var k = 0; k + 1 < size(c); k += 1)
    {
        if (c[k + 1][1] < c[k][1] - TOL)
        {
            throw regenError("Each end has to run across the tube without folding back on itself.", ["regions"], region);
        }
    }
    return c;
}

function meanU(chain is array) returns ValueWithUnits
{
    var total = 0 * meter;
    for (var p in chain)
    {
        total += p[0];
    }
    return total / size(chain);
}

// ===================== End-chain geometry =====================

/** u of an end chain at a given v (the inner-most value if the chain has a step there). */
function chainU(chain is array, v is ValueWithUnits, takeMax is boolean)
{
    var result = undefined;
    for (var k = 0; k + 1 < size(chain); k += 1)
    {
        const a = chain[k];
        const b = chain[k + 1];
        if (v < a[1] - TOL || v > b[1] + TOL)
        {
            continue;
        }
        var u;
        if (b[1] - a[1] < TOL)
        {
            u = takeMax ? max(a[0], b[0]) : min(a[0], b[0]);
        }
        else
        {
            const f = max(0, min(1, (v - a[1]) / (b[1] - a[1])));
            u = a[0] + (b[0] - a[0]) * f;
        }
        if (result == undefined || (takeMax ? u > result : u < result))
        {
            result = u;
        }
    }
    return result;
}

/** Most extreme u of an end chain across the strip v1..v2 (max if takeMax, else min). */
function chainExtreme(chain is array, v1 is ValueWithUnits, v2 is ValueWithUnits, takeMax is boolean) returns ValueWithUnits
{
    var result = chainU(chain, v1, takeMax);
    const at2 = chainU(chain, v2, takeMax);
    if (takeMax ? at2 > result : at2 < result)
    {
        result = at2;
    }
    for (var p in chain)
    {
        if (p[1] > v1 && p[1] < v2 && (takeMax ? p[0] > result : p[0] < result))
        {
            result = p[0];
        }
    }
    return result;
}

/** [min u, max u] over an end chain's points. */
function chainRangeU(chain is array) returns array
{
    var lo = chain[0][0];
    var hi = lo;
    for (var p in chain)
    {
        lo = min(lo, p[0]);
        hi = max(hi, p[0]);
    }
    return [lo, hi];
}

/**
 * The region just outside one end (u below the start chain, or above the end chain), reaching a margin
 * past both sides of the tube. Used to trim walls back to an angled end.
 */
function outsidePolygon(chain is array, range is array, m is ValueWithUnits, isStart is boolean) returns array
{
    const far = isStart ? range[0] - m : range[1] + m;
    const first = chain[0];
    const last = chain[size(chain) - 1];
    return concatenateArrays([chain, [
                    vector(last[0], last[1] + m),
                    vector(far, last[1] + m),
                    vector(far, first[1] - m),
                    vector(first[0], first[1] - m)
                ]]);
}

function drawEndTrims(sketch is Sketch, fp is map, startRange is array, endRange is array, m is ValueWithUnits, doStart is boolean, doEnd is boolean)
{
    if (doStart)
    {
        drawPolygon(sketch, "s", outsidePolygon(fp.start, startRange, m, true));
    }
    if (doEnd)
    {
        drawPolygon(sketch, "e", outsidePolygon(fp.end, endRange, m, false));
    }
}

/** The piece of a side wall between an end chain and the straight part's end, or [] if there isn't one. */
function fillPolygon(chain is array, v1 is ValueWithUnits, v2 is ValueWithUnits, bound is ValueWithUnits, takeMax is boolean) returns array
{
    var raw = [vector(chainU(chain, v1, takeMax), v1)];
    for (var p in chain)
    {
        if (p[1] > v1 + TOL && p[1] < v2 - TOL)
        {
            raw = append(raw, p);
        }
    }
    raw = append(raw, vector(chainU(chain, v2, takeMax), v2));
    raw = append(raw, vector(bound, v2));
    raw = append(raw, vector(bound, v1));

    var poly = [];
    for (var p in raw)
    {
        if (size(poly) == 0 || norm(p - poly[size(poly) - 1]) > TOL)
        {
            poly = append(poly, p);
        }
    }
    if (size(poly) > 1 && norm(poly[0] - poly[size(poly) - 1]) < TOL)
    {
        poly = without(poly, size(poly) - 1);
    }
    if (size(poly) < 3 || abs(polygonArea(poly)) < (1e-4 * inch) ^ 2)
    {
        return [];
    }
    return poly;
}

function polygonArea(poly is array) returns ValueWithUnits
{
    var total = 0 * meter ^ 2;
    const m = size(poly);
    for (var k = 0; k < m; k += 1)
    {
        const a = poly[k];
        const b = poly[(k + 1) % m];
        total += a[0] * b[1] - b[0] * a[1];
    }
    return total / 2;
}

function pointInPolygon(p is Vector, poly is array) returns boolean
{
    var inside = false;
    const m = size(poly);
    for (var i = 0; i < m; i += 1)
    {
        const a = poly[i];
        const b = poly[(i + m - 1) % m];
        if ((a[1] > p[1]) != (b[1] > p[1]))
        {
            const x = a[0] + (p[1] - a[1]) * (b[0] - a[0]) / (b[1] - a[1]);
            if (p[0] < x)
            {
                inside = !inside;
            }
        }
    }
    return inside;
}

function chainSegments(startChain is array, endChain is array) returns array
{
    var segs = [];
    for (var c in [startChain, endChain])
    {
        for (var k = 0; k + 1 < size(c); k += 1)
        {
            segs = append(segs, [c[k], c[k + 1]]);
        }
    }
    return segs;
}

function segDist(p is Vector, a is Vector, b is Vector) returns ValueWithUnits
{
    const ab = b - a;
    const len2 = dot(ab, ab);
    if (len2 < TOL * TOL)
    {
        return norm(p - a);
    }
    const f = max(0, min(1, dot(p - a, ab) / len2));
    return norm(p - (a + ab * f));
}

// ===================== Hole checks =====================

function clearOfSegments(p is Vector, segs is array, clearance is ValueWithUnits) returns boolean
{
    for (var sg in segs)
    {
        if (segDist(p, sg[0], sg[1]) < clearance)
        {
            return false;
        }
    }
    return true;
}

/**
 * Half holes on a top/bottom face: keep a feature if it removes material, unless one of the end cuts
 * passes within MIN_WEB of its edge (a sliver or a barely-there nick).
 */
function topHoleCuts(p is Vector, r is ValueWithUnits, segs is array, outline is array) returns boolean
{
    var dmin = undefined;
    for (var sg in segs)
    {
        const di = segDist(p, sg[0], sg[1]);
        if (abs(di - r) < MIN_WEB)
        {
            return false;
        }
        if (dmin == undefined || di < dmin)
        {
            dmin = di;
        }
    }
    return dmin < r || pointInPolygon(p, outline);
}

/**
 * Half holes on a side face. The start cut runs from sOut to sIn across the wall's thickness (equal for a
 * square end), the end cut from eIn to eOut. Skip features that miss the wall, and features whose edge
 * lands within MIN_WEB of either cut.
 */
function sideHoleCuts(u is ValueWithUnits, r is ValueWithUnits, sOut is ValueWithUnits, sIn is ValueWithUnits,
    eIn is ValueWithUnits, eOut is ValueWithUnits) returns boolean
{
    if (u + r <= sOut + MIN_WEB || u - r >= eOut - MIN_WEB)
    {
        return false;
    }
    for (var x in [u - r, u + r])
    {
        if ((x > sOut - MIN_WEB && x < sIn + MIN_WEB) || (x > eIn - MIN_WEB && x < eOut + MIN_WEB))
        {
            return false;
        }
    }
    return true;
}

function gridPositions(anchor is ValueWithUnits, lo, hi, spacing) returns array
{
    var out = [];
    if (lo == undefined || hi == undefined || spacing == undefined)
    {
        return out;
    }
    const kMin = ceil((lo - anchor) / spacing - 1e-9);
    const kMax = floor((hi - anchor) / spacing + 1e-9);
    for (var k = kMin; k <= kMax; k += 1)
    {
        out = append(out, anchor + k * spacing);
    }
    return out;
}

// ===================== Sketch helpers =====================

/**
 * One wall: outline + holes/cutouts in a single sketch on the wall's outer face, extruded inward.
 * The wall is the sketch region containing `inside` (u, v), a point no hole reaches.
 */
function makeWall(context is Context, id is Id, planeOrigin is Vector, normal is Vector, xDir is Vector,
    outline is array, features is array, t is ValueWithUnits, inside is Vector) returns Query
{
    const pl = plane(planeOrigin, normal, xDir);
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : pl });
    drawPolygon(sketch, "o", outline);
    drawFeatures(sketch, features);
    skSolve(sketch);
    opExtrude(context, id + "wall", {
                "entities" : qContainsPoint(qSketchRegion(id + "sketch"), planeToWorld(pl, inside)),
                "direction" : -normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : t
            });
    return qCreatedBy(id + "wall", EntityType.BODY);
}

function drawFeatures(sketch is Sketch, features is array)
{
    for (var k = 0; k < size(features); k += 1)
    {
        const f = features[k];
        if (f.shape == undefined)
        {
            skCircle(sketch, "h" ~ k, { "center" : vector(f.u, f.v), "radius" : f.R });
        }
        else
        {
            drawLoop(sketch, "x" ~ k ~ "_", f.shape, f.u, f.v, f.flip == true);
        }
    }
}

/** Draws a loop of [x1, y1, x2, y2] lines and [x1, y1, xMid, yMid, x2, y2] arcs (inches), shifted by (du, dv), optionally mirrored in y. */
function drawLoop(sketch is Sketch, prefix is string, loop is array, du is ValueWithUnits, dv is ValueWithUnits, flipY is boolean)
{
    for (var k = 0; k < size(loop); k += 1)
    {
        const sg = loop[k];
        if (size(sg) == 4)
        {
            skLineSegment(sketch, prefix ~ k, {
                        "start" : loopPoint(sg[0], sg[1], du, dv, flipY),
                        "end" : loopPoint(sg[2], sg[3], du, dv, flipY)
                    });
        }
        else
        {
            skArc(sketch, prefix ~ k, {
                        "start" : loopPoint(sg[0], sg[1], du, dv, flipY),
                        "mid" : loopPoint(sg[2], sg[3], du, dv, flipY),
                        "end" : loopPoint(sg[4], sg[5], du, dv, flipY)
                    });
        }
    }
}

function loopPoint(x is number, y is number, du is ValueWithUnits, dv is ValueWithUnits, flipY is boolean) returns Vector
{
    return vector(du + x * inch, dv + (flipY ? -y : y) * inch);
}

function copyBody(context is Context, id is Id, bodies is Query, offset is Vector) returns Query
{
    opPattern(context, id, {
                "entities" : bodies,
                "transforms" : [transform(offset)],
                "instanceNames" : ["copy"]
            });
    return qCreatedBy(id, EntityType.BODY);
}

function drawPolygon(sketch is Sketch, prefix is string, pts is array)
{
    const m = size(pts);
    for (var k = 0; k < m; k += 1)
    {
        skLineSegment(sketch, prefix ~ k, { "start" : pts[k], "end" : pts[(k + 1) % m] });
    }
}

function rectangle(a is ValueWithUnits, b is ValueWithUnits, halfHeight is ValueWithUnits) returns array
{
    return [vector(a, -halfHeight), vector(b, -halfHeight), vector(b, halfHeight), vector(a, halfHeight)];
}

function without(arr is array, index is number) returns array
{
    var out = [];
    for (var k = 0; k < size(arr); k += 1)
    {
        if (k != index)
        {
            out = append(out, arr[k]);
        }
    }
    return out;
}

// ===================== MAXSpline =====================
// REV's MAXSpline cutout, traced from REV-21-3558 (MAXTube 2x1, 1/8in wall, MAX Pattern) STEP file,
// revrobotics.com/content/cad/REV-21-3558.STEP. Inches, centered on (0, 0): x along the tube, y across the face.
// [x1, y1, x2, y2] lines and [x1, y1, xMid, yMid, x2, y2] arcs.

const MAXSPLINE_R = 0.6954 * inch;          // farthest point from the center
const MAXSPLINE_HALF_WIDTH = 0.6412 * inch; // how far it reaches across the face

const MAXSPLINE = [[0.438688, -0.439137, 0.426844, -0.391802, 0.444019, -0.346132], [0.444019, -0.346132, 0.487565, -0.281496, 0.521768, -0.211466], [0.521768, -0.211466, 0.552733, -0.173757, 0.599648, -0.160346], [0.599648, -0.160346, 0.660393, -0.138493, 0.690589, -0.081433], [0.690589, -0.081433, 0.694177, -0.040787, 0.695374, 0], [0.695374, 0, 0.694177, 0.040787, 0.690589, 0.081433], [0.690589, 0.081433, 0.660393, 0.138493, 0.599648, 0.160346], [0.599648, 0.160346, 0.552733, 0.173757, 0.521768, 0.211466], [0.521768, 0.211466, 0.487565, 0.281496, 0.444019, 0.346132], [0.444019, 0.346132, 0.426844, 0.391802, 0.438688, 0.439137], [0.438688, 0.439137, 0.448432, 0.461512, 0.451763, 0.485688], [0.451763, 0.485688, 0.442273, 0.525775, 0.415818, 0.557351], [0.415818, 0.557351, 0.347687, 0.602212, 0.274771, 0.638785], [0.274771, 0.638785, 0.210258, 0.641164, 0.16096, 0.599483], [0.16096, 0.599483, 0.125889, 0.565559, 0.077749, 0.557598], [0.077749, 0.557598, 0, 0.562992, -0.077749, 0.557598], [-0.077749, 0.557598, -0.125889, 0.565559, -0.16096, 0.599483], [-0.16096, 0.599483, -0.210258, 0.641164, -0.274771, 0.638785], [-0.274771, 0.638785, -0.347687, 0.602212, -0.415818, 0.557351], [-0.415818, 0.557351, -0.450135, 0.502671, -0.438688, 0.439137], [-0.438688, 0.439137, -0.429761, 0.418639, -0.42671, 0.39649], [-0.42671, 0.39649, -0.431158, 0.369865, -0.444019, 0.346132], [-0.444019, 0.346132, -0.487565, 0.281496, -0.521768, 0.211466], [-0.521768, 0.211466, -0.552733, 0.173757, -0.599648, 0.160346], [-0.599648, 0.160346, -0.660393, 0.138493, -0.690589, 0.081433], [-0.690589, 0.081433, -0.695374, 0, -0.690589, -0.081433], [-0.690589, -0.081433, -0.660393, -0.138493, -0.599648, -0.160346], [-0.599648, -0.160346, -0.552733, -0.173757, -0.521768, -0.211466], [-0.521768, -0.211466, -0.487565, -0.281496, -0.444019, -0.346132], [-0.444019, -0.346132, -0.431158, -0.369865, -0.42671, -0.39649], [-0.42671, -0.39649, -0.429761, -0.418639, -0.438688, -0.439137], [-0.438688, -0.439137, -0.450135, -0.502671, -0.415818, -0.557351], [-0.415818, -0.557351, -0.347687, -0.602212, -0.274771, -0.638785], [-0.274771, -0.638785, -0.210258, -0.641164, -0.16096, -0.599483], [-0.16096, -0.599483, -0.125889, -0.565559, -0.077749, -0.557598], [-0.077749, -0.557598, 0, -0.562992, 0.077749, -0.557598], [0.077749, -0.557598, 0.125889, -0.565559, 0.16096, -0.599483], [0.16096, -0.599483, 0.210258, -0.641164, 0.274771, -0.638785], [0.274771, -0.638785, 0.347687, -0.602212, 0.415818, -0.557351], [0.415818, -0.557351, 0.442273, -0.525775, 0.451763, -0.485688], [0.451763, -0.485688, 0.448432, -0.461512, 0.438688, -0.439137]];