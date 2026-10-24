"""The models Robot imported frame imports: each COTS frame's (Robot frame's FRC and FTC tables), by its part name, from
the STEP files `fs models` builds in models/ (see fs_cli/stock_models.py). After the frame tables change, run `uv run
fs gen` (which regenerates frameTables.gen.fs, which this reads), `uv run fs models`, then `uv run fs gen` again.
"""

import pathlib

from fs_cli.stock_models import generated_contents

CONTENTS = generated_contents(pathlib.Path(__file__))
