FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "0794d10863d10d98a88c2ab4", version : "d292b4e6b25e9bc8369e8396");
// Also exports the enums used as parameter types
export import(path : "9211f87982e44e327e4bdfbc", version : "213c875bc7ff445550a6b7f9");

/**
 * FRCDesign's library bearings, generated for FRCDesignApp: flanged and radial ball bearings. They're made in a Part
 * Studio whose configuration sets every parameter and offers only what's made (see bearingTables.py), so this dialog is
 * rarely seen: a flat list of enums, with no conditions, which the configuration filters.
 *
 * Each bearing is built along the Z axis, with a mate connector at the origin pointing out of the hole it sits in: a
 * flanged bearing's flange is above the origin, and the rest of it below; a radial bearing is below, its
 * top face at the origin. Each points out of the part it's on, as do the ones on its top and bottom faces, where what's
 * stacked on its shaft sits against it: a flange's top points up, and any bearing's bottom down. A radial bearing's top
 * is where it seats, so it has no other.
 *
 * Every ball bearing's colors are the same, as WCP's models color them, whatever its vendor's: steel grey races, and
 * shields of `SHIELD_COLOR` or black seals between them.
 */

// Each race's thickness, of the distance from the bore's corners to the outer diameter, as REV's and AndyMark's models
// have them
const RACE_FRACTION = 0.3;
// How far the shields or seals between the races sit in from the faces, of the bearing's width
const SHIELD_RECESS = 1 / 16;
// WCP-0785's model's shields
const SHIELD_COLOR = color(171 / 255, 169 / 255, 156 / 255);

annotation { "Feature Type Name" : "Bearing",
        "Feature Type Description" : "A bearing from FRCDesign's library, seated at the origin." ~ CREDIT }
export const libraryBearing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Vendor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.vendor is BearingVendor;

        annotation { "Name" : "Type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bearingType is BearingType;

        annotation { "Name" : "Bore", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bore is BearingBore;

        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is BearingSize;

        annotation { "Name" : "Dimensions", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.dimensions is BearingDimensions;

        annotation { "Name" : "Seal", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.seal is BearingSeal;
    }
    {
        const choices = resolveChoices(BEARINGS, definition, ["vendor", "bearingType", "bore", "size", "dimensions", "seal"]);
        if (choices.unmade == "bearingType")
        {
            throw regenError("Selected vendor does not make this type of bearing, change the type.", ["bearingType"]);
        }
        if (choices.unmade == "bore")
        {
            throw regenError("Selected vendor does not make this bearing with this bore, change the bore.", ["bore"]);
        }
        if (choices.unmade == "size")
        {
            throw regenError("Selected vendor does not make this bearing in this size, change the size.", ["size"]);
        }
        if (choices.unmade == "dimensions")
        {
            throw regenError("Selected vendor does not make this bearing, change the dimensions.", ["dimensions"]);
        }
        if (choices.unmade != undefined)
        {
            throw regenError("Selected vendor does not make this bearing with this seal, change the seal.", ["seal"]);
        }
        definition = choices.definition;
        const bearing = choices.made;
        const boreShape = BEARING_BORES[definition.bore][definition.size];
        const isFlanged = definition.bearingType != BearingType.RADIAL;

        // 1. Revolve its races, from the axis out, with the shields or seals between them recessed
        const sketchId = id + "profile";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : PROFILE_PLANE });
        const points = bearingProfile(bearing, boreRadius(boreShape), definition.bearingType);
        skPolyline(sketch, "profile", { "points" : points });
        skSolve(sketch);
        opRevolve(context, id + "revolve", {
                    "entities" : qSketchRegion(sketchId),
                    "axis" : line(WORLD_ORIGIN, Z_DIRECTION),
                    "angleForward" : 2 * PI * radian
                });
        const body = qCreatedBy(id + "revolve", EntityType.BODY);

        // 2. Cut its bore through
        const bottom = points[0][1];
        cutBore(context, id + "bore", body, boreShape, bottom, bearing.width);

        // 3. Seat it, and mark its top and bottom, each pointing out of it
        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });
        if (isFlanged)
        {
            opMateConnector(context, id + "topMateConnector", {
                        "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, bottom + bearing.width), X_DIRECTION, Z_DIRECTION),
                        "owner" : body
                    });
        }
        opMateConnector(context, id + "bottomMateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, bottom), X_DIRECTION, -Z_DIRECTION),
                    "owner" : body
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
        setBearingProperties(context, body, bearing);
        if (definition.seal == BearingSeal.SHIELDED || definition.seal == BearingSeal.SEALED)
        {
            // The shields' or seals' faces, between the races
            const faces = qOwnedByBody(body, EntityType.FACE)->qGeometry(GeometryType.PLANE);
            const seals = qUnion([faces->qContainsPoint(sealPoint(points, 3)), faces->qContainsPoint(sealPoint(points, size(points) - 4))]);
            setProperty(context, {
                        "entities" : seals,
                        "propertyType" : PropertyType.APPEARANCE,
                        "value" : definition.seal == BearingSeal.SEALED ? BLACK : SHIELD_COLOR
                    });
        }
    });

/**
 * `definition` with each of `parameters`, a level of `entries` each, set to the option it stands for (see `resolve`),
 * and what they make (`made`); or the first parameter nothing is made in (`unmade`).
 */
export function resolveChoices(entries is map, definition is map, parameters is array) returns map
{
    var level = entries;
    for (var parameter in parameters)
    {
        const option = resolve(level, definition[parameter]);
        if (option == undefined)
        {
            return { "unmade" : parameter };
        }
        definition[parameter] = option;
        level = level[option];
    }
    return { "definition" : definition, "made" : level };
}

/**
 * The option of `entries` an input holding `value` stands for: `value` where it's made, or else the one option made.
 * The configuration hides an input where only one of its options is made, and a hidden input keeps whatever it was last
 * set to.
 */
export function resolve(entries is map, value)
{
    if (entries[value] != undefined)
    {
        return value;
    }
    return size(entries) == 1 ? keys(entries)[0] : undefined;
}

// The XZ plane, with the sketch's X along the radius and its Y along the axis
const PROFILE_PLANE = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);

/**
 * How far a bore reaches from the axis: a round bore's radius, or a hex's corners, where they're rounded the round's.
 */
export function boreRadius(boreShape is map) returns ValueWithUnits
{
    if (boreShape.acrossFlats == undefined)
    {
        return boreShape.diameter / 2;
    }
    const corner = boreShape.acrossFlats / sqrt(3);
    return boreShape.diameter == undefined ? corner : min(corner, boreShape.diameter / 2);
}

/**
 * The bearing's half section, revolved about the axis, from its bottom on the axis: up its inner race to its top, with
 * the shields between its races recessed from each face, and out to its outer diameter and a flanged one's flange.
 * A flanged bearing's flange is above the origin, the rest of it below; a radial bearing is below the origin. An
 * extended inner race (`outerWidth`) stands out of both faces of the outer race.
 */
export function bearingProfile(bearing is map, boreRadius is ValueWithUnits, bearingType is BearingType) returns array
{
    const zero = 0 * meter;
    const radius = bearing.outerDiameter / 2;
    const isFlanged = bearingType == BearingType.FLANGED;
    const recess = bearing.width * SHIELD_RECESS;
    const race = (radius - boreRadius) * RACE_FRACTION;
    const inner = boreRadius + race;
    const outer = radius - race;
    const top = isFlanged ? bearing.flangeThickness : zero;
    const bottom = top - bearing.width;
    const inset = bearing.outerWidth == undefined ? zero : (bearing.width - bearing.outerWidth) / 2;
    const outerBottom = bottom + inset;
    const outerTop = top - inset;
    var side = [vector(radius, outerTop)];
    if (isFlanged)
    {
        const flange = bearing.flangeDiameter / 2;
        side = [vector(radius, zero), vector(flange, zero), vector(flange, outerTop)];
    }
    return concatenateArrays([
                [vector(zero, bottom), vector(inner, bottom), vector(inner, outerBottom + recess),
                    vector(outer, outerBottom + recess), vector(outer, outerBottom), vector(radius, outerBottom)],
                side,
                [vector(outer, outerTop), vector(outer, outerTop - recess), vector(inner, outerTop - recess),
                    vector(inner, top), vector(zero, top), vector(zero, bottom)]
            ]);
}

/**
 * A point on a shield's face, halfway between the races, from the profile's point at its inner race (`index`).
 */
function sealPoint(points is array, index is number) returns Vector
{
    const middle = (points[index - 1] + points[index]) / 2;
    return vector(middle[0], 0 * meter, middle[1]);
}

/**
 * Cuts a bore through the bearing: a hex's corners rounded to its `diameter` where it has one.
 */
function cutBore(context is Context, id is Id, body is Query, boreShape is map, bottom is ValueWithUnits, width is ValueWithUnits)
{
    // From below the bearing to above it, so the cut doesn't share its faces
    const start = bottom - width;
    const sketchId = id + "sketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : plane(vector(0 * meter, 0 * meter, start), Z_DIRECTION) });
    if (boreShape.diameter != undefined)
    {
        skCircle(sketch, "round", { "center" : vector(0, 0) * meter, "radius" : boreShape.diameter / 2 });
    }
    if (boreShape.acrossFlats != undefined)
    {
        skRegularPolygon(sketch, "hex", {
                    "center" : vector(0, 0) * meter,
                    "firstVertex" : vector(boreShape.acrossFlats / sqrt(3), 0 * meter),
                    "sides" : 6
                });
    }
    skSolve(sketch);
    // Where a round rounds a hex's corners, the bore is the region both enclose, around the axis
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(sketchId)->qContainsPoint(vector(0 * meter, 0 * meter, start)),
                "direction" : Z_DIRECTION,
                "endBound" : BoundingType.BLIND,
                "endDepth" : 3 * width
            });
    opBoolean(context, id + "cut", {
                "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                "targets" : body,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
}

/**
 * Sets a part's properties from its table entry (see bearingTables.py's `Record`): its name, its vendor and part
 * number, a link to buy it where it has them, and its material and appearance.
 */
function setBearingProperties(context is Context, body is Query, entry is map)
{
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.NAME, "value" : entry.name });
    if (entry.vendor != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.VENDOR, "value" : entry.vendor });
    }
    if (entry.partNumber != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.PART_NUMBER, "value" : entry.partNumber });
    }
    if (entry.url != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.DESCRIPTION, "value" : entry.url });
    }
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.MATERIAL, "value" : entry.material });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.APPEARANCE, "value" : entry.appearance });
}
