FeatureScript 2960;
/**
 * A benchmark of ways to build a tube with holes, to profile in Onshape: from whole walls, as wall stock does
 * (`core/wallStock.fs`), or from a section of tube a few holes long, copied along it with `opPattern` and joined. It
 * builds a FRC 2x1 tube (1/16 in. walls, #10 holes every 1/2 in. starting 1/2 in. from the end, three rows on its 2 in.
 * faces and one on its 1 in. faces) each way at once, along X, each 3 in. further along Y, and stands alone (std only),
 * so nothing else is timed. Each way is built on a line of its own, so the profiler times each.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

const WIDTH = 2 * inch;
const HEIGHT = 1 * inch;
const WALL = 0.0625 * inch;
const HOLE_RADIUS = 0.098 * inch;
const PITCH = 0.5 * inch;
// Where the copied sections start: halfway between the end and the first hole, so sections end between holes
const SECTION_START = 0.25 * inch;
const TOP_OFFSETS = [-0.5 * inch, 0 * inch, 0.5 * inch];
const SIDE_OFFSETS = [0 * inch];
const BENCH_TOLERANCE = 1e-6 * meter;
// How far apart the tubes are built
const ROW = 3 * inch;

const TUBE_LENGTH_BOUNDS = { (meter) : [0.0254, 0.7112, 10], (inch) : 28, (millimeter) : 700 } as LengthBoundSpec;

/** How copied sections are joined. */
enum BenchJoin
{
    /** One union of every section (and the pieces at the ends). */
    UNION_ALL,
    /** The block of sections is copied and joined to itself, a pattern and a two-body union each time it doubles. */
    DOUBLING,
    /** Not at all, to time copying them alone. */
    NONE
}

annotation { "Feature Type Name" : "Tube pattern bench", "Feature Type Description" : "Builds a 2x1 tube from whole walls, and from copied sections, to compare their regen times." }
export const tubePatternBench = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.length, TUBE_LENGTH_BOUNDS);
    }
    {
        const length = definition.length;
        buildWalls(context, id + "walls", length, 0 * ROW);
        // 1 hole (1/2 in.) per section
        buildSections(context, id + "sections1Union", length, 1, BenchJoin.UNION_ALL, 1 * ROW);
        buildSections(context, id + "sections1Doubling", length, 1, BenchJoin.DOUBLING, 2 * ROW);
        buildSections(context, id + "sections1Unjoined", length, 1, BenchJoin.NONE, 3 * ROW);
        // 4 holes (2 in.)
        buildSections(context, id + "sections4Union", length, 4, BenchJoin.UNION_ALL, 4 * ROW);
        buildSections(context, id + "sections4Doubling", length, 4, BenchJoin.DOUBLING, 5 * ROW);
        // 8 holes (4 in.)
        buildSections(context, id + "sections8Union", length, 8, BenchJoin.UNION_ALL, 6 * ROW);
        // 16 holes (8 in.)
        buildSections(context, id + "sections16Union", length, 16, BenchJoin.UNION_ALL, 7 * ROW);
    });

/**
 * The tube `length` long at `y`, from whole walls: its top and one side sketched with all their holes, the others
 * copies, joined with one union.
 */
function buildWalls(context is Context, id is Id, length is ValueWithUnits, y is ValueWithUnits)
{
    const walls = buildPiece(context, id + "tube", 0 * meter, length, y);
    deleteSketches(context, id);
    opBoolean(context, id + "join", { "tools" : walls, "operationType" : BooleanOperationType.UNION });
}

/**
 * The tube `length` long at `y`, from sections `holes` holes long: one built as `buildWalls` builds a tube, copied along
 * it, and joined as `join` says, with the pieces before and after the sections sketched.
 */
function buildSections(context is Context, id is Id, length is ValueWithUnits, holes is number, join is BenchJoin, y is ValueWithUnits)
{
    const span = holes * PITCH;
    const count = floor((length - SECTION_START) / span + 1e-9);
    if (count < 1)
    {
        throw regenError("The tube is shorter than a section.", ["length"]);
    }
    const middleEnd = SECTION_START + count * span;
    // One section, whole, then the pieces before and after the sections
    const cellId = id + "cell";
    const cellWalls = buildPiece(context, cellId, SECTION_START, SECTION_START + span, y);
    buildPiece(context, id + "start", 0 * meter, SECTION_START, y);
    if (length - middleEnd > BENCH_TOLERANCE)
    {
        buildPiece(context, id + "end", middleEnd, length, y);
    }
    deleteSketches(context, id);
    opBoolean(context, cellId + "join", { "tools" : cellWalls, "operationType" : BooleanOperationType.UNION });
    const cell = qCreatedBy(cellId, EntityType.BODY)->qBodyType(BodyType.SOLID);

    if (join == BenchJoin.DOUBLING)
    {
        // Up to the largest power of two sections; the rest are copied from the section at the start
        var doubled = 1;
        while (doubled * 2 <= count)
        {
            doubled *= 2;
        }
        copyAlong(context, id + "rest", cell, span, doubled, count - 1);
        const block = qUnion([cell, qCreatedBy(id + "double", EntityType.BODY)])->qBodyType(BodyType.SOLID);
        for (var blockSize = 1; blockSize < doubled; blockSize *= 2)
        {
            copyAlong(context, id + "double" + ("copy" ~ blockSize), block, span, blockSize, blockSize);
            opBoolean(context, id + "double" + ("join" ~ blockSize), { "tools" : block, "operationType" : BooleanOperationType.UNION });
        }
    }
    else
    {
        copyAlong(context, id + "copies", cell, span, 1, count - 1);
    }
    if (join != BenchJoin.NONE)
    {
        opBoolean(context, id + "join", {
                    "tools" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                    "operationType" : BooleanOperationType.UNION
                });
    }
}

/**
 * The tube's four walls from `from` to `to` along it, at `y`, not joined: its top and one side sketched with their holes
 * and extruded, and the bottom and other side copies of them.
 */
function buildPiece(context is Context, id is Id, from is ValueWithUnits, to is ValueWithUnits, y is ValueWithUnits) returns Query
{
    const top = sketchWall(context, id + "top", plane(vector(0 * meter, y, HEIGHT / 2), Z_DIRECTION, X_DIRECTION), from, to,
        WIDTH / 2, TOP_OFFSETS);
    const bottom = copyBy(context, id + "bottom", top, vector(0 * meter, 0 * meter, WALL - HEIGHT));
    // Between the top and bottom
    const side = sketchWall(context, id + "side", plane(vector(0 * meter, y + WIDTH / 2, 0 * meter), Y_DIRECTION, X_DIRECTION), from, to,
        HEIGHT / 2 - WALL, SIDE_OFFSETS);
    const otherSide = copyBy(context, id + "otherSide", side, vector(0 * meter, WALL - WIDTH, 0 * meter));
    return qUnion([top, bottom, side, otherSide]);
}

/**
 * A wall sketched on its outside, from `from` to `to` along it and `halfWidth` either side of its middle, with a hole
 * at each of `offsets` across it every `PITCH` along it (those wholly inside it), extruded inward as thick as `WALL`.
 */
function sketchWall(context is Context, id is Id, wallPlane is Plane, from is ValueWithUnits, to is ValueWithUnits,
    halfWidth is ValueWithUnits, offsets is array) returns Query
{
    const sketchId = id + "sketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : wallPlane });
    const corners = [vector(from, -halfWidth), vector(to, -halfWidth), vector(to, halfWidth), vector(from, halfWidth)];
    for (var i = 0; i < 4; i += 1)
    {
        skLineSegment(sketch, "side" ~ i, { "start" : corners[i], "end" : corners[(i + 1) % 4] });
    }
    for (var k = max(1, ceil(from / PITCH - 1e-9)); k * PITCH + HOLE_RADIUS < to - BENCH_TOLERANCE; k += 1)
    {
        const position = k * PITCH;
        if (position - HOLE_RADIUS > from + BENCH_TOLERANCE)
        {
            for (var j, offset in offsets)
            {
                skCircle(sketch, "hole" ~ k ~ "." ~ j, { "center" : vector(position, offset), "radius" : HOLE_RADIUS });
            }
        }
    }
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(sketchId, true),
                "direction" : -wallPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : WALL
            });
    return qCreatedBy(id + "extrude", EntityType.BODY);
}

/** A copy of `bodies` moved by `offset`. */
function copyBy(context is Context, id is Id, bodies is Query, offset is Vector) returns Query
{
    opPattern(context, id, { "entities" : bodies, "transforms" : [transform(offset)], "instanceNames" : ["copy"] });
    return qCreatedBy(id, EntityType.BODY);
}

/** Copies of `bodies` moved `span` along the tube (X) `first` to `last` times, with one pattern (none if `last < first`). */
function copyAlong(context is Context, id is Id, bodies is Query, span is ValueWithUnits, first is number, last is number)
{
    var transforms = [];
    var names = [];
    for (var k = first; k <= last; k += 1)
    {
        transforms = append(transforms, transform(vector(k * span, 0 * meter, 0 * meter)));
        names = append(names, "s" ~ k);
    }
    if (transforms != [])
    {
        opPattern(context, id, { "entities" : bodies, "transforms" : transforms, "instanceNames" : names });
    }
}

function deleteSketches(context is Context, id is Id)
{
    opDeleteBodies(context, id + "deleteSketches", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
}
