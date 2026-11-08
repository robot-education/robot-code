"""FTC's library fasteners, generated for FRCDesignApp's ConfigLib: metric screws, washers, and nuts. Run `uv run fs gen`
after editing fastenerTables.py, which holds them.

They're FRC's features and tables (fasteners.fs, fastenerTables.gen.fs), configured to offer FTC's metric sizes alone, as
a part set of their own (`[tool.fs.parts.ftcFasteners]`) released into ConfigLib's document: this sets its studios and
writes its manifest for FRCDesign, ftcFasteners.gen.json, as fastenerTables.py does FRC's. See docs/ftc-fasteners.md
for where its parts come from.
"""

from __future__ import annotations

import json
import pathlib
import runpy

from fs_cli.configured_studios import read_locations

_TABLES = runpy.run_path(str(pathlib.Path(__file__).with_name("fastenerTables.py")))
FTC = _TABLES["FTC"]

STUDIOS = FTC.studios
RECORDS = FTC.records

# Where each studio was last released: `fs parts ftcFasteners --release` records it beside this file
LOCATIONS = read_locations(pathlib.Path(__file__))

EXPORTS = {
    "ftcFasteners.gen.json": json.dumps(
        _TABLES["manifest"](FTC, LOCATIONS, pathlib.Path(__file__).name), indent=2
    )
    + "\n"
}
