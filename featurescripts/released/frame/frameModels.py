"""The models Robot frame imports: each COTS frame's (its FRC and FTC tables), by its part name, from the STEP files
`fs models` builds in models/ (see fs_cli/stock_models.py). After the frame tables change, run `uv run fs gen` (which
regenerates frameTables.gen.fs, which this reads), `uv run fs models`, then `uv run fs gen` again.

Only the models in `TRYING` are imported for now, while cutting frames from models is tried in Onshape; the others are
commented out, so their frames are built from their profiles, and their tabs aren't needed yet. Push those with
`uv run fs push` and their paths. Remove `TRYING` to import every model.
"""

import pathlib

from fs_cli.stock_models import generated_contents

TRYING = {
    "2x1-tube-andymark-1-16-in-wall.gen.step",  # 2x1 1/16 in. wall tube with 0.5 in. holes, as WCP's and others'
    "1x1-tube-andymark-1-8-in-wall.gen.step",  # 1x1 1/8 in. wall tube, as REV's, Swyft's, and WCP's
    "1in-extrusion-rev-black.gen.step",
    "2x2-tube-rev-max-pattern.gen.step",  # The biggest model
    "u-channel-gobilda-1120-series.gen.step",  # FTC, in millimeters
}

CONTENTS = generated_contents(pathlib.Path(__file__), only=TRYING)
