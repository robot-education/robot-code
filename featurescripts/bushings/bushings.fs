FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
// Also exports the enums used as parameter types
export import(path : "bushings/bushingTables.gen.fs", version : "");
// The bearings' choices, bore, and properties, which bushings share
import(path : "bearings/bearings.fs", version : "");

/**
 * FRCDesign's library bushings, generated for FRCDesignApp: flanged bushings, made as FRCDesign's bearings are (see
 * `bearings/bearings.fs`), in a Part Studio whose configuration sets every parameter and offers only what's made (see
 * bushingTables.py), so this dialog is a flat list of enums, with no conditions.
 *
 * Each bushing is built along the Z axis, its flange above the origin and its body below, with a mate connector at the origin pointing out of the hole it sits in, one on its flange's top pointing up,
 * and one on its bottom pointing down, as the bearings' are.
 */

// The XZ plane, with the sketch's X along the radius and its Y along the axis
const PROFILE_PLANE = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);

annotation { "Feature Type Name" : "Bushing",
        "Feature Type Description" : "A bushing from FRCDesign's library, seated at the origin." ~ CREDIT }
export const libraryBushing = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Vendor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.vendor is BushingVendor;

        annotation { "Name" : "Bore", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.bore is BushingBore;

        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is BushingSize;

        annotation { "Name" : "Material", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.material is BushingMaterial;

        annotation { "Name" : "Dimensions", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.dimensions is BushingDimensions;
    }
    {
        const choices = resolveChoices(BUSHINGS, definition, ["vendor", "bore", "size", "material", "dimensions"]);
        if (choices.unmade == "bore")
        {
            throw regenError("Selected vendor does not make this bushing with this bore, change the bore.", ["bore"]);
        }
        if (choices.unmade == "size")
        {
            throw regenError("Selected vendor does not make this bushing in this size, change the size.", ["size"]);
        }
        if (choices.unmade == "material")
        {
            throw regenError("Selected vendor does not make this bushing in this material, change the material.", ["material"]);
        }
        if (choices.unmade != undefined)
        {
            throw regenError("Selected vendor does not make this bushing, change the dimensions.", ["dimensions"]);
        }
        definition = choices.definition;
        const bushing = choices.made;

        // 1. Revolve its section, from the axis out
        const sketchId = id + "profile";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : PROFILE_PLANE });
        const points = bushingProfile(bushing);
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
        cutBore(context, id + "bore", body, BUSHING_BORES[definition.bore][definition.size], bottom, bushing.width);

        // 3. Seat it, and mark its top and bottom, each pointing out of it
        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });
        opMateConnector(context, id + "topMateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, bushing.flangeThickness), X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });
        opMateConnector(context, id + "bottomMateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, bottom), X_DIRECTION, -Z_DIRECTION),
                    "owner" : body
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
        setBearingProperties(context, body, bushing);
    });

/**
 * The bushing's half section, revolved about the axis, from its bottom on the axis: out to its outer diameter, up to
 * the origin, out to its flange, up its flange, and back to the axis.
 */
export function bushingProfile(bushing is map) returns array
{
    const zero = 0 * meter;
    const top = bushing.flangeThickness;
    const bottom = top - bushing.width;
    const radius = bushing.outerDiameter / 2;
    const flange = bushing.flangeDiameter / 2;
    return [vector(zero, bottom), vector(radius, bottom), vector(radius, zero), vector(flange, zero), vector(flange, top),
            vector(zero, top), vector(zero, bottom)];
}
