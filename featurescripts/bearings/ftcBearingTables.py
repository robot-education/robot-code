"""FTC's library bearings, generated for FRCDesignApp's ConfigLib: goBILDA's, REV's, and AndyMark's FTC bearings. Run `uv
run fs gen` after editing bearingTables.py, which holds them.

They're FRC's feature and tables (bearings.fs, bearingTables.gen.fs), configured to offer FTC's vendors' bearings alone, as a
part set of their own (`[tool.fs.parts.ftcBearings]`) released into ConfigLib's document: this sets its studio and writes
its manifest for FRCDesign, ftcBearings.gen.json, as bearingTables.py does FRC's.
"""

from __future__ import annotations

import json
import pathlib
import runpy

from fs_cli.configured_studios import read_locations

_TABLES = runpy.run_path(str(pathlib.Path(__file__).with_name("bearingTables.py")))
FTC = _TABLES["FTC"]

STUDIOS = FTC.studios
RECORDS = FTC.records

# Where the studio was last released: `fs release ftcBearings` records it beside this file
LOCATIONS = read_locations(pathlib.Path(__file__))

EXPORTS = {
    "ftcBearings.gen.json": json.dumps(_TABLES["manifest"](FTC, LOCATIONS, pathlib.Path(__file__).name), indent=2) + "\n"
}
