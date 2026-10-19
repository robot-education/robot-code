FeatureScript 2960;
/**
 * Makes holes cut by a feature into tapped holes, the way the std Hole feature marks them: with hole attributes (used
 * by hole callouts, hole tables, and features which look for holes) and cosmetic threads.
 *
 * This is much cheaper than calling the Hole feature or `opHole`: cut the holes however is convenient (e.g. as circles
 * in the sketch a part is extruded from), then call `setTappedThroughHoles` on their faces. `setHoleAttributes` marks
 * holes of any kind (simple, clearance, or tapped; through or blind).
 */
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/cosmeticThreadUtils.fs", version : "2960.0");
import(path : "onshape/std/hole.fs", version : "2960.0");

/**
 * Marks holes as tapped through holes, with the same hole attributes and cosmetic threads as the std Hole feature
 * gives a simple, tapped, through hole.
 *
 * @param id : The id of the feature making the holes; each hole's attribute id is made from it.
 * @param holes {array} : A map for each hole: {{
 *      @field faces {Query} : The hole's faces. Usually one cylindrical face, but a hole which intersects another
 *              one may be split.
 *      @field coordSystem {CoordSystem} : Where the hole starts, with its Z axis along the hole.
 * }}
 * @param thread {map} : {{
 *      @field size {string} : The thread's size, as in the std hole tables. @eg `"#10"` or `"M3"`
 *      @field pitch {string} : The thread's pitch, as in the std hole tables. @eg `"32 tpi"` or `"0.5 mm"`
 *      @field majorDiameter {ValueWithUnits} : The thread's major diameter.
 *      @field tapDrillDiameter {ValueWithUnits} : The diameter of the holes.
 * }}
 */
export function setTappedThroughHoles(context is Context, id is Id, holes is array, thread is map)
{
    setHoleAttributes(context, id, holes, { "holeDiameter" : thread.tapDrillDiameter, "size" : thread.size, "thread" : thread });
}

/**
 * Marks holes with the same hole attributes as the std Hole feature gives a simple hole (from a hole table, for a
 * clearance or tapped hole), and tapped holes with cosmetic threads. It doesn't remove what the holes' faces are
 * already marked with: see `clearHoleAttributesAndThreads`.
 *
 * @param id : The id of the feature making (or changing) the holes; each hole's attribute id is made from it.
 * @param holes {array} : A map for each hole: {{
 *      @field faces {Query} : The hole's faces. Usually one cylindrical face, but a hole which intersects another
 *              one may be split.
 *      @field coordSystem {CoordSystem} : Where the hole starts, with its Z axis along the hole (into a blind hole).
 *      @field depth {ValueWithUnits} : @optional A blind hole's depth: how far its sides go. A through hole has none.
 *      @field tipAngle {ValueWithUnits} : @optional A blind hole's drill point angle, if it has one.
 * }}
 * @param spec {map} : {{
 *      @field holeDiameter {ValueWithUnits} : The diameter of the holes.
 *      @field size {string} : @optional The fastener size a clearance or tapped hole is for, as in the std hole
 *              tables. @eg `"#10"`
 *      @field thread {map} : @optional The thread a tapped hole is tapped with: its `size`, `pitch`, and
 *              `majorDiameter` (see `setTappedThroughHoles`). It's tapped as deep as it goes.
 * }}
 */
export function setHoleAttributes(context is Context, id is Id, holes is array, spec is map)
{
    var attribute = makeHoleAttribute(true, "", HoleStyle.SIMPLE);
    attribute.holeFeatureCount = size(holes);
    attribute.showTappedDepth = false;
    attribute.holeDiameter = spec.holeDiameter;
    attribute.isTappedHole = false;
    attribute.isTaperedPipeTapHole = false;
    attribute.isStraightPipeTapHole = false;
    attribute.tappedAngle = 0.0;
    attribute.tapSize = "";
    if (spec.size != undefined)
    {
        attribute.standardComponentSizeDesignation = spec.size;
    }
    var threadPitch;
    if (spec.thread != undefined)
    {
        threadPitch = computePitchValue(context, spec.thread.pitch);
        attribute.majorDiameter = spec.thread.majorDiameter;
        attribute.isTappedHole = true;
        attribute.tapSize = spec.thread.size ~ buildPitchAnnotation(context, spec.thread.pitch);
        attribute.threadPitch = threadPitch;
    }
    attribute.tolerances = {};
    // fs check: ignore keyword-key (std's hole attributes name it "type")
    attribute.sectionFace = { "type" : HoleSectionFaceType.THROUGH_FACE };

    for (var i, hole in holes)
    {
        var holeAttribute = attribute;
        holeAttribute.attributeId = toAttributeId(id + ("hole-" ~ i));
        holeAttribute.holeNumber = i;
        if (hole.depth == undefined)
        {
            holeAttribute.endType = HoleEndStyle.THROUGH;
            holeAttribute.partialThrough = false;
        }
        else
        {
            holeAttribute.endType = HoleEndStyle.BLIND;
            holeAttribute.holeDepth = hole.depth;
            if (hole.tipAngle != undefined)
            {
                holeAttribute.tipAngle = hole.tipAngle;
            }
        }
        if (spec.thread != undefined)
        {
            // A tapped depth of zero means tapped through
            holeAttribute.isTappedThrough = hole.depth == undefined;
            holeAttribute.tappedDepth = hole.depth == undefined ? 0 * meter : hole.depth;
            holeAttribute.showTappedDepth = hole.depth != undefined;
            addCosmeticThreadAttribute(context, hole.faces,
                createCosmeticThreadDataFromEntity(hole.coordSystem, holeAttribute.tappedDepth.value, threadPitch.value));
        }
        setAttribute(context, { "entities" : hole.faces, "attribute" : holeAttribute });
    }
}

/**
 * Removes the hole attributes and cosmetic threads (see `setHoleAttributes`) from faces, e.g. to mark a hole again.
 */
export function clearHoleAttributesAndThreads(context is Context, faces is Query)
{
    clearHoleAttributes(context, faces);
    // std's cosmeticThreadUtils.fs names its attribute this, and keeps the name to itself
    setAttribute(context, { "entities" : faces, "name" : "cosmeticThreadAttribute", "attribute" : undefined });
}
