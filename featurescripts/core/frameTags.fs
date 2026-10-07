FeatureScript 2960;
/**
 * Tags bodies as frames, the way the std Frame feature does, so the std frame features (Frame trim, Cutlist, End
 * cap, ...) work with them: a profile attribute on the body (holding its default cutlist entries), and topology
 * attributes on its swept faces and edges and its start and end cap faces.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/frameAttributes.fs", version : "2960.0");
export import(path : "onshape/std/frameUtils.fs", version : "2960.0");

/**
 * Tags the body extruded by `extrudeId` (e.g. from a profile sketch, with `opExtrude`) as a frame running from its
 * start cap to its end cap. Tag it before cutting holes in it, so its faces are still the extrude's.
 *
 * @param description : The frame's description in cutlists, e.g. `"2x1 Tube (WCP, 1/16 in. wall)"`.
 * @param standard : The frame's standard in cutlists, e.g. a vendor.
 */
export function tagExtrudeAsFrame(context is Context, extrudeId is Id, description is string, standard is string)
{
    setFrameProfileAttribute(context, qCreatedBy(extrudeId, EntityType.BODY), frameProfileAttribute({
                    (CUTLIST_STANDARD) : standard,
                    (CUTLIST_DESCRIPTION) : description
                }));
    setFrameTopologyAttribute(context, qNonCapEntity(extrudeId, EntityType.FACE), frameTopologyAttributeForSwept(FrameTopologyType.SWEPT_FACE));
    const sweptEdges = qNonCapEntity(extrudeId, EntityType.EDGE);
    if (!isQueryEmpty(context, sweptEdges))
    {
        setFrameTopologyAttribute(context, sweptEdges, frameTopologyAttributeForSwept(FrameTopologyType.SWEPT_EDGE));
    }
    // A single frame is its own terminus
    setFrameTopologyAttribute(context, qCapEntity(extrudeId, CapType.START, EntityType.FACE), frameTopologyAttributeForCapFace(true, true, false));
    setFrameTopologyAttribute(context, qCapEntity(extrudeId, CapType.END, EntityType.FACE), frameTopologyAttributeForCapFace(false, true, false));
}

/**
 * Tags faces which end a frame, like those left by trimming it, as its start (or end) cap faces.
 */
export function tagFrameCaps(context is Context, faces is Query, isStart is boolean)
{
    if (!isQueryEmpty(context, faces))
    {
        setFrameTopologyAttribute(context, faces, frameTopologyAttributeForCapFace(isStart, true, false));
    }
}
