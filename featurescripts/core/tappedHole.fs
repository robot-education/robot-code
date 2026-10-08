FeatureScript 2960;
/**
 * Makes holes cut by a feature into tapped holes, the way the std Hole feature marks them: with hole attributes (used
 * by hole callouts, hole tables, and features which look for holes) and cosmetic threads.
 *
 * This is much cheaper than calling the Hole feature or `opHole`: cut the holes however is convenient (e.g. as circles
 * in the sketch a part is extruded from), then call `setTappedThroughHoles` on their faces.
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
    const threadPitch = computePitchValue(context, thread.pitch);
    var attribute = makeHoleAttribute(true, "", HoleStyle.SIMPLE);
    attribute.holeFeatureCount = size(holes);
    attribute.endType = HoleEndStyle.THROUGH;
    attribute.partialThrough = false;
    attribute.showTappedDepth = false;
    attribute.holeDiameter = thread.tapDrillDiameter;
    attribute.majorDiameter = thread.majorDiameter;
    attribute.isTappedHole = true;
    attribute.isTaperedPipeTapHole = false;
    attribute.isStraightPipeTapHole = false;
    attribute.tappedAngle = 0.0;
    attribute.standardComponentSizeDesignation = thread.size;
    attribute.tapSize = thread.size ~ buildPitchAnnotation(context, thread.pitch);
    attribute.threadPitch = threadPitch;
    // A tapped depth of zero means tapped through
    attribute.isTappedThrough = true;
    attribute.tappedDepth = 0 * meter;
    attribute.tolerances = {};
    // fs check: ignore keyword-key (std's hole attributes name it "type")
    attribute.sectionFace = { "type" : HoleSectionFaceType.THROUGH_FACE };

    for (var i, hole in holes)
    {
        attribute.attributeId = toAttributeId(id + ("hole-" ~ i));
        attribute.holeNumber = i;
        addCosmeticThreadAttribute(context, hole.faces, createCosmeticThreadDataFromEntity(hole.coordSystem, 0, threadPitch.value));
        setAttribute(context, { "entities" : hole.faces, "attribute" : attribute });
    }
}
