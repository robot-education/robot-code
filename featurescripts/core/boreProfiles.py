"""SplineXS bores at each standard fit, sketched with their fit, so a bore needn't offset its faces (see core/bore.fs).

Run `uv run fs gen` after editing.

A fit's clearance across is how much bigger std's clearance hole is than the inch fastener nearest the spline's size
(8 mm, nearest 5/16 in.: see `standardFitClearance` in core/fit.fs, and `ANSI_V2ClearanceHoleTable` in std's
holetables.gen.fs). tests/featurescript/fit_test.fs checks these are what `standardFitClearance` gives.
"""

import pathlib
import runpy

from fs_cli.gen import Constant, Import
from fs_cli.tables import number
from fs_cli.sketches import Profile, SketchMap

_SPLINES = runpy.run_path(str(pathlib.Path(__file__).parent.parent / "released/splineProfile/splineProfiles.py"))
spline_xs_shaft = _SPLINES["spline_xs_shaft"]

INCH = 25.4

# The clearance across, in inches: 5/16 in.'s close (0.323 in.) and free (0.332 in.) clearance holes, less 5/16 in.
SPLINE_XS_CLEARANCES = {
    "Fit.FREE": 0.332 - 5 / 16,
    "Fit.CLOSE": 0.323 - 5 / 16,
    "Fit.NONE": 0,
}

CONTENTS = [
    Import("core/sketchData.fs"),
    Import("core/fit.fs"),
    # A SplineXS shaft's profile, its sides moved out half its fit's clearance
    SketchMap(
        "SPLINE_XS_BORES",
        {fit: Profile(spline_xs_shaft(clearance * INCH / 2)) for fit, clearance in SPLINE_XS_CLEARANCES.items()},
    ),
    # Their clearances, for checking against `standardFitClearance`
    Constant(
        "SPLINE_XS_BORE_CLEARANCES",
        "{ "
        + ", ".join(f"{fit} : {number(round(clearance, 12))} * inch" for fit, clearance in SPLINE_XS_CLEARANCES.items())
        + " }",
    ),
]
