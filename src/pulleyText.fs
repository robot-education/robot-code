FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

annotation { "Feature Type Name" : "Pulley text" }
export const pulleyText = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Pulley diameter" }
        isLength(definition.pulleyDiameter, LENGTH_BOUNDS);
    }
    {
        const teeth = getVariable(context, "teeth");
        const hasFlanges = getVariable(context, "hasFlanges");
        const flangeDiameterOffset = getVariable(context, "flangeDiameterOffset");
        const pulleyWidth = getVariable(context, "width") + (hasFlanges ? getVariable(context, "flangeWidth") * 2 : 0 * meter);

        var plane = XY_PLANE;
        plane.origin += plane.normal * pulleyWidth / 2;

        var radius = definition.pulleyDiameter / 2;
        if (hasFlanges)
        {
            radius += flangeDiameterOffset - 1.5 * millimeter;
        }
        else if (getVariable(context, "beltType") as string == "GT2")
        {
            radius -= 2.25 * millimeter;
        }
        else
        {
            radius -= 3.5 * millimeter;
        }

        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });

        skText(sketch, "text", {
                    "text" : "" ~ teeth,
                    "fontName" : "OpenSans-Regular.ttf",
                    "firstCorner" : vector(-4 / 2 * millimeter, radius),
                    "secondCorner" : vector(4 / 2 * millimeter, radius - 2.5 * millimeter)
                });

        skSolve(sketch);

        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(id + "sketch", true),
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : 0.25 * millimeter
                });

        opBoolean(context, id + "engrave", {
                    "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                    "targets" : qEverything(EntityType.BODY)->qBodyType(BodyType.SOLID)->qLargest(),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    });

