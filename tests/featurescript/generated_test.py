"""Checks of what `fs gen` generates from the Python definitions in featurescripts/ (lookup tables, sketch profiles,
and icons): it's checked in up to date, and its profiles are closed."""

import collections
import pathlib
import runpy

from fs_cli.gen import generate
from fs_cli.sketches import Arc, FitSpline, Line, Sketch, SketchMap

CODE = pathlib.Path(__file__).parents[2] / "featurescripts"


def test_checked_in_files_are_up_to_date():
    """Also checks generated imports resolve without the sync state, from their annotations."""
    for result in generate(CODE, None):
        assert not result.changed, f"{result.output} is stale; run `fs gen`"


def test_checked_in_profiles_are_closed():
    """Every endpoint of a profile meets exactly one other (apart from MAXSpline's legacy stub line)."""
    for source in sorted(CODE.rglob("*.py")):
        for item in runpy.run_path(str(source)).get("CONTENTS", []):
            profiles = (
                item.profiles.values() if isinstance(item, SketchMap)
                else [item.profile] if isinstance(item, Sketch) else []
            )
            for profile in profiles:
                ends = collections.Counter()
                for entity in profile.entities:
                    if isinstance(entity, (Line, Arc, FitSpline)):
                        for point in (entity.start, entity.end):
                            ends[(round(point.x, 9), round(point.y, 9))] += 1
                def key(point):
                    return (round(point.x, 9), round(point.y, 9))

                dangling = [
                    e for e in profile.entities
                    if isinstance(e, Line) and ends[key(e.start)] == 1 and ends[key(e.end)] == 1
                ]
                loose = {key(e.start) for e in dangling} | {key(e.end) for e in dangling}
                unmatched = [p for p, count in ends.items() if count != 2 and p not in loose]
                assert not unmatched, (source.name, item.name, unmatched)
                assert not dangling or item.name == "MAX_SPLINE", (source.name, item.name)
