FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

export enum TextSize
{
    annotation { "Name" : "Tiny" }
    TINY,
    annotation { "Name" : "Small" }
    SMALL,
    annotation { "Name" : "Medium" }
    MEDIUM,
    annotation { "Name" : "Large" }
    LARGE
}

export predicate textPredicate(definition is map)
{
    annotation { "Name" : "Text size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"], "Default" : TextSize.MEDIUM }
    definition.textSize is TextSize;
    
    annotation { "Name" : "Bold text", "UIHint" : "REMEMBER_PREVIOUS_VALUE", "Default" : true }
    definition.boldText is boolean;
}

export function getTextHeight(definition is map) returns ValueWithUnits
{
    return switch (definition.textSize) {
                TextSize.TINY : 2.5 * millimeter,
                TextSize.SMALL : 3 * millimeter,
                TextSize.MEDIUM : 4 * millimeter,
                TextSize.LARGE : 5 * millimeter
            };
}

export function getTextDepth(definition is map) returns ValueWithUnits
{
    return switch (definition.textSize) {
                TextSize.TINY : 0.5 * millimeter,
                TextSize.SMALL : 0.75 * millimeter,
                TextSize.MEDIUM : 1 * millimeter,
                TextSize.LARGE : 1 * millimeter
            };
}

/**
 * @param id : @autocomplete `id + "text"`
 * @param definition {{
 *          @field text {string} :
 *          @field bold {boolean} : @optional
 *                  Defaults to `false`. 
 *          @field height {ValueWithUnits} :
 *          @field plane {Plane} :
 *          @field mirrorHorizontal {boolean} : @optional
 *          @field mirrorVertical {boolean} : @optional
 * }}
 *
 * @returns {Query} : A query for the sketch regions.
 */
export const opText = function(context is Context, id is Id, definition is map) returns Query
    {
        definition = mergeMaps({ "mirrorHorizontal" : false, "mirrorVertical" : false, "bold" : false }, definition);
        
        const fontName = "OpenSans-" ~ (definition.bold ? "Bold" : "Regular") ~ ".ttf";

        const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : XY_PLANE });
        skText(sketch, "text", {
                    "text" : definition.text,
                    "fontName" : fontName,
                    "firstCorner" : vector(0 * meter, definition.height / 2),
                    "secondCorner" : vector(1 * meter, -definition.height / 2),
                    "mirrorHorizontal" : definition.mirrorHorizontal,
                    "mirrorVertical" : definition.mirrorVertical
                });
        skSolve(sketch);

        const topEdge = qCreatedBy(id + "sketch", EntityType.EDGE)->qConstructionFilter(ConstructionObject.YES)->qNthElement(1);
        const width = evLength(context, { "entities" : topEdge });
        const centerVector = vector(-width / 2, 0 * meter, 0 * meter);

        cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY)->qConstructionFilter(ConstructionObject.YES));

        // For some reason, inner faces get lost without a tracking query
        const faces = startTracking(context, qSketchRegion(id + "sketch", true));

        opTransform(context, id + "transform", {
                    "bodies" : qCreatedBy(id + "sketch", EntityType.BODY),
                    "transform" : toWorld(definition.plane->coordSystem()) * transform(centerVector)
                });

        return faces;
    };
