FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
Model1inExtrusionRevBlack::import(path : "9c9921210ddb6c3ed598f939", version : "eb6ba0c1761fe1968c5d9452"); // released/frame/models/1in-extrusion-rev-black.gen.step
Model1x1TubeRev18InWall::import(path : "dd42087feaa29787df7da749", version : "9001074e24138e55799ff9ad"); // released/frame/models/1x1-tube-rev-1-8-in-wall.gen.step
Model2x1TubeAndymark116InWall::import(path : "0f7091feddf254d24d9940cd", version : "376d453754263999137edb45"); // released/frame/models/2x1-tube-andymark-1-16-in-wall.gen.step
Model2x2TubeRevMaxPattern::import(path : "3fd4b2324d0c70d013d87048", version : "a6c6b5a4e2e5e95f228d8a13"); // released/frame/models/2x2-tube-rev-max-pattern.gen.step
ModelUChannelGobilda1120Series::import(path : "339869817a24bedd5e179520", version : "8950d6a4c6ebc7aa0f5cafb3"); // released/frame/models/u-channel-gobilda-1120-series.gen.step

/* Generated from frameModels.py by `fs gen` -- DO NOT EDIT */

// Models not imported yet (see frameModels.py's TRYING):
//     released/frame/models/0-74x0-74-angle-rev.gen.step
//     released/frame/models/1-2x1-2-tube-andymark-robits.gen.step
//     released/frame/models/1-2x1-2-tube-rev-1-16-in-wall.gen.step
//     released/frame/models/15mm-extrusion-rev-black.gen.step
//     released/frame/models/15x30mm-extrusion-rev-black.gen.step
//     released/frame/models/1x1-angle-wcp-2-5-mm-wall.gen.step
//     released/frame/models/1x1-tube-andymark-1-16-in-wall-no-holes.gen.step
//     released/frame/models/1x1-tube-andymark-1-16-in-wall.gen.step
//     released/frame/models/1x1-tube-andymark-1-8-in-wall-no-holes.gen.step
//     released/frame/models/1x1-tube-andymark-1-8-in-wall.gen.step
//     released/frame/models/1x1-tube-rev-1-16-in-wall.gen.step
//     released/frame/models/1x1-tube-rev-standard.gen.step
//     released/frame/models/1x1-tube-wcp-3-32-in-wall.gen.step
//     released/frame/models/1x1x1-c-channel-wcp-2-5-mm-wall.gen.step
//     released/frame/models/1x2x1-c-channel-wcp-2-5-mm-wall.gen.step
//     released/frame/models/2x1-tube-andymark-1-16-in-wall-no-holes.gen.step
//     released/frame/models/2x1-tube-andymark-1-8-in-wall-no-holes.gen.step
//     released/frame/models/2x1-tube-andymark-1-8-in-wall.gen.step
//     released/frame/models/2x1-tube-rev-1-16-in-wall.gen.step
//     released/frame/models/2x1-tube-rev-1-8-in-wall-max-pattern.gen.step
//     released/frame/models/2x1-tube-rev-1-8-in-wall.gen.step
//     released/frame/models/2x1-tube-rev-3-16-in-wall.gen.step
//     released/frame/models/2x1-tube-rev-light-1-in-sides-only.gen.step
//     released/frame/models/2x1-tube-rev-light.gen.step
//     released/frame/models/2x1-tube-rev-standard-1-in-sides-only.gen.step
//     released/frame/models/2x1-tube-rev-standard-max-pattern.gen.step
//     released/frame/models/2x1-tube-swyft-grid-0-035-in-wall.gen.step
//     released/frame/models/2x1-tube-swyft-grid-1-16-in-wall.gen.step
//     released/frame/models/2x1-tube-swyft-grid-1-8-in-wall.gen.step
//     released/frame/models/2x1-tube-swyft-plain-0-035-in-wall.gen.step
//     released/frame/models/2x1-tube-ttb-0-080-in-wall.gen.step
//     released/frame/models/2x1-tube-wcp-1-16-in-wall-1-in-sides-only.gen.step
//     released/frame/models/2x1-tube-wcp-1-8-in-wall-1-in-sides-only.gen.step
//     released/frame/models/2x1-tube-wcp-3-32-in-wall-1-in-sides-only.gen.step
//     released/frame/models/2x1-tube-wcp-3-32-in-wall.gen.step
//     released/frame/models/2x2-angle-wcp-2-5-mm-wall.gen.step
//     released/frame/models/2x2-tube-rev-grid.gen.step
//     released/frame/models/2x2-tube-rev-max-pattern-and-grid.gen.step
//     released/frame/models/2x2-tube-wcp-1-16-in-wall.gen.step
//     released/frame/models/2x2-tube-wcp-3-32-in-wall.gen.step
//     released/frame/models/low-side-u-channel-gobilda-1121-series.gen.step
//     released/frame/models/mini-low-side-u-channel-gobilda-1143-series.gen.step
//     released/frame/models/square-beam-gobilda-1106-series.gen.step

/**
 * The model Robot frame imports for each COTS frame, by its part name (see fs_cli/stock_models.py): its
 * tab's `data`, how far its holes go (`length`, from where they start as they're sold, as long as it's sold),
 * and how far it reaches back before its holes start (`lead`).
 */
export const FRAME_MODELS = {
        "1in Extrusion (REV, black)" : { "data" : Model1inExtrusionRevBlack::BLOB_DATA, "length" : 48 * inch, "lead" : 2 * inch },
        "1in Extrusion (REV, clear)" : { "data" : Model1inExtrusionRevBlack::BLOB_DATA, "length" : 48 * inch, "lead" : 2 * inch },
        "1x1 Tube (REV, 1/8 in. wall)" : { "data" : Model1x1TubeRev18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "1x1 Tube (Swyft, grid, 1/8 in. wall)" : { "data" : Model1x1TubeRev18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "2x1 Tube (AndyMark, 1/16 in. wall)" : { "data" : Model2x1TubeAndymark116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "2x1 Tube (Last Anvil, 1/16 in. wall)" : { "data" : Model2x1TubeAndymark116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "2x1 Tube (WCP, 1/16 in. wall)" : { "data" : Model2x1TubeAndymark116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "2x2 Tube (REV, MAX Pattern)" : { "data" : Model2x2TubeRevMaxPattern::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        "U-Channel (goBILDA 1120 Series)" : { "data" : ModelUChannelGobilda1120Series::BLOB_DATA, "length" : 1200 * millimeter, "lead" : 2 * inch }

        // Not imported yet (see frameModels.py), so these frames are built from their profiles
        // "0.74x0.74 Angle (REV)" : { "data" : Model074x074AngleRev::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1/2x1/2 Tube (AndyMark Robits)" : { "data" : Model12x12TubeAndymarkRobits::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1/2x1/2 Tube (REV, 1/16 in. wall)" : { "data" : Model12x12TubeRev116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "15mm Extrusion (REV, black)" : { "data" : Model15mmExtrusionRevBlack::BLOB_DATA, "length" : 1000 * millimeter, "lead" : 2 * inch },
        // "15mm Extrusion (REV, clear)" : { "data" : Model15mmExtrusionRevBlack::BLOB_DATA, "length" : 1000 * millimeter, "lead" : 2 * inch },
        // "15x30mm Extrusion (REV, black)" : { "data" : Model15x30mmExtrusionRevBlack::BLOB_DATA, "length" : 1000 * millimeter, "lead" : 2 * inch },
        // "15x30mm Extrusion (REV, clear)" : { "data" : Model15x30mmExtrusionRevBlack::BLOB_DATA, "length" : 1000 * millimeter, "lead" : 2 * inch },
        // "1x1 Angle (WCP, 2.5 mm wall)" : { "data" : Model1x1AngleWcp25MmWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (AndyMark, 1/16 in. wall, no holes)" : { "data" : Model1x1TubeAndymark116InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (AndyMark, 1/16 in. wall)" : { "data" : Model1x1TubeAndymark116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (WCP, 1/16 in. wall)" : { "data" : Model1x1TubeAndymark116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (AndyMark, 1/8 in. wall, no holes)" : { "data" : Model1x1TubeAndymark18InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (AndyMark, 1/8 in. wall)" : { "data" : Model1x1TubeAndymark18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (WCP, 1/8 in. wall)" : { "data" : Model1x1TubeAndymark18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (REV, 1/16 in. wall)" : { "data" : Model1x1TubeRev116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (Swyft, grid, 1/16 in. wall)" : { "data" : Model1x1TubeRev116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (REV, standard)" : { "data" : Model1x1TubeRevStandard::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1 Tube (WCP, 3/32 in. wall)" : { "data" : Model1x1TubeWcp332InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x1x1 C-Channel (WCP, 2.5 mm wall)" : { "data" : Model1x1x1CChannelWcp25MmWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "1x2x1 C-Channel (WCP, 2.5 mm wall)" : { "data" : Model1x2x1CChannelWcp25MmWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (AndyMark, 1/16 in. wall, no holes)" : { "data" : Model2x1TubeAndymark116InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, plain, 1/16 in. wall)" : { "data" : Model2x1TubeAndymark116InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (AndyMark, 1/8 in. wall, no holes)" : { "data" : Model2x1TubeAndymark18InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, plain, 1/8 in. wall)" : { "data" : Model2x1TubeAndymark18InWallNoHoles::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (AndyMark, 1/8 in. wall)" : { "data" : Model2x1TubeAndymark18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (WCP, 1/8 in. wall)" : { "data" : Model2x1TubeAndymark18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, 1/16 in. wall)" : { "data" : Model2x1TubeRev116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, 1/8 in. wall, MAX Pattern)" : { "data" : Model2x1TubeRev18InWallMaxPattern::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, 1/8 in. wall)" : { "data" : Model2x1TubeRev18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (TTB, 1/8 in. wall)" : { "data" : Model2x1TubeRev18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, 3/16 in. wall)" : { "data" : Model2x1TubeRev316InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, light, 1 in. sides only)" : { "data" : Model2x1TubeRevLight1InSidesOnly::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, light)" : { "data" : Model2x1TubeRevLight::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, standard, 1 in. sides only)" : { "data" : Model2x1TubeRevStandard1InSidesOnly::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (REV, standard, MAX Pattern)" : { "data" : Model2x1TubeRevStandardMaxPattern::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, grid, 0.035 in. wall)" : { "data" : Model2x1TubeSwyftGrid0035InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, grid, 1/16 in. wall)" : { "data" : Model2x1TubeSwyftGrid116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, grid, 1/8 in. wall)" : { "data" : Model2x1TubeSwyftGrid18InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (Swyft, plain, 0.035 in. wall)" : { "data" : Model2x1TubeSwyftPlain0035InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (TTB, 0.080 in. wall)" : { "data" : Model2x1TubeTtb0080InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (WCP, 1/16 in. wall, 1 in. sides only)" : { "data" : Model2x1TubeWcp116InWall1InSidesOnly::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (WCP, 1/8 in. wall, 1 in. sides only)" : { "data" : Model2x1TubeWcp18InWall1InSidesOnly::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (WCP, 3/32 in. wall, 1 in. sides only)" : { "data" : Model2x1TubeWcp332InWall1InSidesOnly::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x1 Tube (WCP, 3/32 in. wall)" : { "data" : Model2x1TubeWcp332InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x2 Angle (WCP, 2.5 mm wall)" : { "data" : Model2x2AngleWcp25MmWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x2 Tube (REV, grid)" : { "data" : Model2x2TubeRevGrid::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x2 Tube (REV, MAX Pattern and grid)" : { "data" : Model2x2TubeRevMaxPatternAndGrid::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x2 Tube (WCP, 1/16 in. wall)" : { "data" : Model2x2TubeWcp116InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "2x2 Tube (WCP, 3/32 in. wall)" : { "data" : Model2x2TubeWcp332InWall::BLOB_DATA, "length" : 47 * inch, "lead" : 2 * inch },
        // "Low-Side U-Channel (goBILDA 1121 Series)" : { "data" : ModelLowSideUChannelGobilda1121Series::BLOB_DATA, "length" : 1200 * millimeter, "lead" : 2 * inch },
        // "Mini Low-Side U-Channel (goBILDA 1143 Series)" : { "data" : ModelMiniLowSideUChannelGobilda1143Series::BLOB_DATA, "length" : 432 * millimeter, "lead" : 2 * inch },
        // "Square Beam (goBILDA 1106 Series)" : { "data" : ModelSquareBeamGobilda1106Series::BLOB_DATA, "length" : 328 * millimeter, "lead" : 2 * inch },
    };
