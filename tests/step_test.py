"""Tests for reading profiles out of STEP files."""

import math

import pytest

from fs_cli.sketches import Arc, FitSpline, Line
from fs_cli.step import StepError, StepFile

# A planar face at z = 1mm bounded by a quarter circle, a cubic Bezier, and a line, in millimeters
STEP = """ISO-10303-21;
HEADER;
FILE_NAME('test.step', '', (''), (''), '', '', '');
ENDSEC;
DATA;
#1 = CARTESIAN_POINT('', (0., 0., 1.));
#2 = DIRECTION('', (0., 0., 1.));
#3 = DIRECTION('', (1., 0., 0.));
#4 = AXIS2_PLACEMENT_3D('', #1, #2, #3);
#5 = PLANE('', #4);
#10 = CARTESIAN_POINT('', (1., 0., 1.));
#11 = VERTEX_POINT('', #10);
#12 = CARTESIAN_POINT('', (0., 1., 1.));
#13 = VERTEX_POINT('', #12);
#14 = CARTESIAN_POINT('', (-1., 0., 1.));
#15 = VERTEX_POINT('', #14);
#20 = CIRCLE('', #4, 1.);
#21 = EDGE_CURVE('', #11, #13, #20, .T.);
#31 = CARTESIAN_POINT('', (-0.5, 1.2, 1.));
#32 = CARTESIAN_POINT('', (-1., 0.5, 1.));
#34 = B_SPLINE_CURVE_WITH_KNOTS('', 3, (#12, #31, #32, #14), .UNSPECIFIED., .F., .F., (4, 4), (0., 1.), .UNSPECIFIED.);
#35 = EDGE_CURVE('', #13, #15, #34, .T.);
#41 = VECTOR('', #3, 2.);
#40 = LINE('', #14, #41);
#42 = EDGE_CURVE('', #15, #11, #40, .T.);
#50 = ORIENTED_EDGE('', *, *, #21, .T.);
#51 = ORIENTED_EDGE('', *, *, #35, .T.);
#52 = ORIENTED_EDGE('', *, *, #42, .T.);
#53 = EDGE_LOOP('', (#50, #51, #52));
#54 = FACE_OUTER_BOUND('', #53, .T.);
#55 = ADVANCED_FACE('', (#54), #5, .T.);
#60 = ( LENGTH_UNIT() NAMED_UNIT(*) SI_UNIT(.MILLI., .METRE.) );
#61 = ( GEOMETRIC_REPRESENTATION_CONTEXT(3) GLOBAL_UNIT_ASSIGNED_CONTEXT((#60)) REPRESENTATION_CONTEXT('', '') );
#62 = ADVANCED_BREP_SHAPE_REPRESENTATION('', (#55), #61);
ENDSEC;
END-ISO-10303-21;
"""

MM = 1 / 25.4


@pytest.fixture
def step(tmp_path) -> StepFile:
    path = tmp_path / "test.step"
    path.write_text(STEP)
    return StepFile(path)


def test_units(step):
    assert step.inches_per_unit == pytest.approx(MM)


def test_profile(step):
    arc, spline, line = step.profile(tolerance=1e-6)
    assert isinstance(arc, Arc) and isinstance(spline, FitSpline) and isinstance(line, Line)
    # The arc goes counterclockwise from (1, 0) to (0, 1)
    assert (arc.mid.x, arc.mid.y) == pytest.approx((MM * math.sqrt(0.5), MM * math.sqrt(0.5)))
    # Each entity ends exactly where the next starts, so the profile closes
    assert spline.start == arc.end and spline.end == line.start and line.end == arc.start
    # The spline's points lie on the Bezier
    control = [(0, 1), (-0.5, 1.2), (-1, 0.5), (-1, 0)]
    def bezier(t):
        weights = [(1 - t) ** 3, 3 * t * (1 - t) ** 2, 3 * t * t * (1 - t), t**3]
        return tuple(MM * sum(w * p[c] for w, p in zip(weights, control)) for c in range(2))
    curve = [bezier(i / 2000) for i in range(2001)]
    for point in spline.points:
        assert min(math.dist((point.x, point.y), q) for q in curve) < 1e-5
    assert len(spline.points) >= 3


def test_profile_needs_a_face(step):
    with pytest.raises(StepError, match="z = 5"):
        step.profile(z=5)
    assert len(step.profile(z=1 * MM)) == 3
