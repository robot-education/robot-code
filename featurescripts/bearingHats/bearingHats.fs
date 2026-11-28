FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "452d43a015d17145ad7775e4", version : "5632c018fec45fab4e533392");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");
// Also exports the enums used as parameter types
export import(path : "bearingHats/hatTables.gen.fs", version : "");
// Resolving the configuration's hidden inputs, as the bearings' are
import(path : "bearings/bearings.fs", version : "");

/**
 * FRCDesign's library bearing hats, generated for FRCDesignApp: WCP's bearing retention plates, in a Part Studio whose
 * configuration sets every parameter and offers only what's made (see hatTables.py), so this dialog is a flat list of
 * enums, with no conditions.
 *
 * Each plate lies on the XY plane, above it, its bore on the origin and its mounting holes along X. It has a mate
 * connector at the origin pointing up, out of the face it sits on, with X toward a mounting hole, and one on its top.
 */
annotation { "Feature Type Name" : "Bearing hat",
        "Feature Type Description" : "A bearing hat from FRCDesign's library, seated at the origin." ~ CREDIT }
export const libraryBearingHat = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Vendor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.vendor is BearingHatVendor;

        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is BearingHatSize;

        annotation { "Name" : "Centers", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.centers is BearingHatCenters;
    }
    {
        const choices = resolveChoices(BEARING_HATS, definition, ["vendor", "size", "centers"]);
        if (choices.unmade == "size")
        {
            throw regenError("Selected vendor does not make this hat in this size, change the size.", ["size"]);
        }
        if (choices.unmade != undefined)
        {
            throw regenError("Selected vendor does not make this hat with these centers, change the centers.", ["centers"]);
        }
        const hat = choices.made;

        // 1. Its outline: about the bore, and round each mounting hole, joined by tangents
        const holes = hatHoles(hat);
        const center = { "location" : vector(0, 0) * meter, "radius" : hat.outerDiameter / 2 + hat.rim, "flipped" : false } as BoundaryCircle;
        var circles = [];
        for (var hole in holes)
        {
            circles = concatenateArrays([circles, [{ "location" : hole, "radius" : hat.lobeRadius, "flipped" : false } as BoundaryCircle, center]]);
        }
        const counterClockwise = isCounterClockwise(holes);
        var connectingPoints = [];
        for (var i, circle in circles)
        {
            connectingPoints = append(connectingPoints, circleToCircle(circles[(i + size(circles) - 1) % size(circles)], circle, counterClockwise));
        }
        sketchConnectingLines(context, id + "lines", circles, XY_PLANE, connectingPoints);
        sketchConnectingArcs(context, id + "arcs", circles, XY_PLANE, connectingPoints, counterClockwise);
        const face = extractFaces(context, id + "outline", qCreatedBy(id, EntityType.EDGE), XY_PLANE);
        opExtrude(context, id + "extrude", {
                    "entities" : face,
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : hat.thickness
                });
        const body = qCreatedBy(id + "extrude", EntityType.BODY);

        // 2. Its bore, over the bearing's outer race, and its mounting holes, through
        const cutSketch = newSketchOnPlane(context, id + "cuts", { "sketchPlane" : XY_PLANE });
        skCircle(cutSketch, "bore", { "center" : vector(0, 0) * meter, "radius" : hat.innerDiameter / 2 });
        for (var i, hole in holes)
        {
            skCircle(cutSketch, "hole" ~ i, { "center" : hole, "radius" : hat.holeDiameter / 2 });
        }
        skSolve(cutSketch);
        opExtrude(context, id + "cutTool", {
                    "entities" : qSketchRegion(id + "cuts"),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : hat.thickness
                });
        opBoolean(context, id + "cut", {
                    "tools" : qCreatedBy(id + "cutTool", EntityType.BODY),
                    "targets" : body,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        // 3. Seat it, and mark its top, each pointing up
        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });
        opMateConnector(context, id + "topMateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, hat.thickness), X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
        setHatProperties(context, body, hat);
    });

/**
 * A hat's mounting holes, on its plane: along X, its centers apart about the bore.
 */
export function hatHoles(hat is map) returns array
{
    return [vector(-hat.centers / 2, 0 * meter), vector(hat.centers / 2, 0 * meter)];
}

function setHatProperties(context is Context, body is Query, hat is map)
{
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.NAME, "value" : hat.name });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.VENDOR, "value" : hat.vendor });
    if (hat.partNumber != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.PART_NUMBER, "value" : hat.partNumber });
    }
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.MATERIAL, "value" : ALUMINUM });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.APPEARANCE, "value" : LIGHT_GRAY });
}
