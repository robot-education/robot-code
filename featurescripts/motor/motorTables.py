"""Tables for Robot motor: the mounting faces of motors and gearboxes, and the envelopes of motors' block models.

Named as FRCDesign names them, in order of how often teams use them there (`fs cots motor`, `fs cots motor -l ftc`).
Sources are in `motor/vendor/` (REV's drawings) and noted beside each; angles go counterclockwise looking at the face,
from +x.
"""

from __future__ import annotations

import dataclasses

from fs_cli.tables import Node, Table, Value, inch, mm, string


@dataclasses.dataclass
class Face:
    """A mounting face: screws on a bolt circle, around a pilot (a boss the face's hole must clear)."""

    screw: str
    bolt_circle: str
    angles: list[float]
    pilot_diameter: str
    pilot_height: str | None = None

    def values(self) -> dict[str, str]:
        values = {
            "screw": string(self.screw),
            "boltCircleDiameter": self.bolt_circle,
            "holeAngles": "[" + ", ".join(f"{angle:g} * degree" for angle in self.angles) + "]",
            "pilotDiameter": self.pilot_diameter,
        }
        if self.pilot_height is not None:
            values["pilotHeight"] = self.pilot_height
        return values


@dataclasses.dataclass
class Body:
    """A motor's envelope behind its face: round, cut flat on its top and bottom (`flats`, the width across them), or
    with a bump at 270° (`bump`: how far its flat end is from the axis, and how wide it is); and its shaft (none for
    through-bore motors)."""

    diameter: str
    length: str
    flats: str | None = None
    bump: tuple[str, str] | None = None
    shaft_diameter: str | None = None
    shaft_length: str | None = None

    def values(self) -> dict[str, str]:
        values = {"bodyDiameter": self.diameter, "bodyLength": self.length}
        if self.flats is not None:
            values["bodyFlats"] = self.flats
        if self.bump is not None:
            values["bumpDistance"], values["bumpWidth"] = self.bump
        if self.shaft_diameter is not None and self.shaft_length is not None:
            values["shaftDiameter"] = self.shaft_diameter
            values["shaftLength"] = self.shaft_length
        return values


def leaf(name: str, part_name: str, face: Face, body: Body | None = None) -> Value:
    values = {"partName": string(part_name), **face.values()}
    if body is not None:
        values.update(body.values())
    return Value(name, values)


# Every 30°, but for the bump at 270° (WCP's drawing: docs.wcproducts.com, "Mounting Kraken X60")
KRAKEN_ANGLES = [angle for angle in range(0, 360, 30) if angle != 270]
# Every 45°, but for the flats at 90° and 270° (REV-21-1653-DR, REV-21-1652-REV-11-2159-DR)
FLATS_ANGLES = [0, 45, 135, 180, 225, 315]
# The pilot of motors which drop in for a CIM: 3/4 in., 3/16 in. tall (WCP, "Physical Specifications")
CIM_PILOT = {"pilot_diameter": inch(0.75), "pilot_height": inch(0.1875)}

# CTRE's Minion.STEP (github.com/CrossTheRoadElec/Device-CADs): an 18.8 mm pilot 2 mm tall, a 38 mm body 49.5 mm
# long, and a SplineXS shaft 23 mm past the face; its holes, tapped for three screws, have the 550's M3 at 0° and 180°
MINION_PILOT = {"pilot_diameter": mm(18.8), "pilot_height": mm(2)}
MINION_BODY = Body(mm(38), mm(49.5), shaft_diameter=mm(7.94), shaft_length=mm(23))
MINION = Node(
    "pattern",
    [
        # On a 1 in. bolt circle, every 60° but 90°
        leaf("#10-32", "Minion", Face("#10", inch(1), [30, 150, 210, 270, 330], **MINION_PILOT), MINION_BODY),
        leaf("550 (M3)", "Minion", Face("M3", mm(25), [0, 180], **MINION_PILOT), MINION_BODY),
        # Two more M4 holes are on a 32 mm bolt circle, at 124.2° and 304.2°: which motor's they're for isn't known
        leaf("775 (M4)", "Minion", Face("M4", mm(29), [53.5, 233.5], **MINION_PILOT), MINION_BODY),
    ],
    display_name="Hole pattern",
)

FRC_MOTORS = Node(
    "motor",
    [
        leaf(
            "Kraken X60",
            "Kraken X60",
            Face("#10", inch(2), KRAKEN_ANGLES, **CIM_PILOT),
            # 60 mm, 75 mm long with its Talon FX; its bump is 63.5 mm across the motor (WCP), and its end about
            # 15.5 mm wide (measured from WCP's drawing); SplineXS shaft
            Body(mm(60), mm(75), bump=(mm(33.5), mm(15.5)), shaft_diameter=mm(8), shaft_length=inch(1.25)),
        ),
        leaf(
            "Falcon 500",
            "Falcon 500",
            # 6x #10-32 on a 2 in. bolt circle; the Kraken X60's holes include them
            Face("#10", inch(2), [0, 60, 120, 180, 240, 300], **CIM_PILOT),
            # 60 mm by 81 mm, 4.569 in. long with its shaft (AndyMark)
            Body(mm(60), mm(81), shaft_diameter=mm(8), shaft_length=inch(1.38)),
        ),
        leaf(
            "Kraken X44",
            "Kraken X44",
            Face("#10", inch(1.375), KRAKEN_ANGLES, **CIM_PILOT),
            # 44 mm, its bump 47.4 mm across the motor (WCP), its end about 11 mm wide (measured)
            Body(mm(44), mm(75), bump=(mm(25.4), mm(11)), shaft_diameter=mm(8), shaft_length=inch(1.25)),
        ),
        leaf(
            "NEO",
            "NEO",
            # REV-21-1650-DR: 4x #10-32, a 19.1 mm pilot 3.5 mm tall, an 8 mm shaft 31.5 mm past it
            Face("#10", inch(2), [0, 90, 180, 270], mm(19.1), mm(3.5)),
            Body(mm(60), mm(58.3), shaft_diameter=mm(8), shaft_length=mm(35)),
        ),
        leaf(
            "NEO Vortex",
            "NEO Vortex",
            # REV-21-1652-REV-11-2159-DR; the pilot isn't dimensioned: 1.25 in. is measured from the drawing
            Face("#10", inch(2), FLATS_ANGLES, inch(1.25)),
            # 79.7 mm long docked to a SPARK Flex; its 1/2 in. hex shaft goes through it, so has no length of its own
            Body(mm(60), mm(79.7), flats=inch(2)),
        ),
        leaf(
            "NEO 550",
            "NEO 550",
            # REV-21-1651-DR: M3 on a 25 mm bolt circle, a 13 mm pilot; the pilot's height isn't dimensioned
            Face("M3", mm(25), [90, 270], mm(13), mm(1.5)),
            Body(mm(35), mm(44.5), shaft_diameter=mm(3.175), shaft_length=mm(8.5)),
        ),
        leaf(
            "NEO 2.0",
            "NEO 2.0",
            # REV-21-1653-DR: a 19 mm pilot, its 15T spline 31.5 mm past it and 35 mm past the face
            Face("#10", inch(2), FLATS_ANGLES, mm(19), mm(3.5)),
            Body(mm(60), mm(48), flats=inch(2), shaft_diameter=mm(8), shaft_length=mm(35)),
        ),
        # Without block models, for now
        leaf("CIM", "CIM", Face("#10", inch(2), [0, 180], inch(0.75))),
        # 2x M4 (REV's MAXPlanetary 775 guide); 29 mm and 17.5 mm are from the previous version of Robot motor
        leaf("RS-775", "RS-775", Face("M4", mm(29), [0, 180], mm(17.5))),
        Value("Minion", next=MINION),
        leaf("Mini CIM", "Mini CIM", Face("#10", inch(2), [0, 180], inch(0.75))),
    ],
)

FRC_GEARBOXES = Node(
    "gearbox",
    [
        # REV-21-2100-DR: 4x #10-32 on a 2 in. bolt circle, in two orientations; the output's boss isn't dimensioned:
        # 1.25 in. is measured from the drawing
        leaf("MAXPlanetary", "MAXPlanetary", Face("#10", inch(2), [0, 135, 180, 315], inch(1.25))),
        # WCP's "Basic Features": 6x #10-32 on a 2 in. bolt circle; the output's 1.5 in. is measured from its picture
        leaf("PlanetaryX", "PlanetaryX", Face("#10", inch(2), FLATS_ANGLES, inch(1.5))),
        # REV-41-1600-DR: 6x M3 at the hex's corners on a 32 mm bolt circle, around a 22 mm boss
        leaf("UltraPlanetary", "UltraPlanetary", Face("M3", mm(32), [30, 90, 150, 210, 270, 330], mm(22))),
        # #10-32 on a 2 in. bolt circle (VEX); the pilot is from the previous version of Robot motor
        leaf("VersaPlanetary", "VersaPlanetary", Face("#10", inch(2), [45, 135, 225, 315], inch(0.75))),
    ],
)

# goBILDA's 5203 and 5103 spec sheets and STEP: M4 on their 16 mm square pattern, and two more 24 mm apart, so on two
# circles: their positions are given per hole. Their plates' 14 mm hole clears the pilot.
GOBILDA_HOLES = [(12, 0), (8, 8), (-8, 8), (-12, 0), (-8, -8), (8, -8)]
# Yellow Jackets, by their speed at 12 V: their ratio, and their gearbox's stages
YELLOW_JACKETS = {
    "6000 RPM": ("1:1", 1),
    "1620 RPM": ("3.7:1", 1),
    "1150 RPM": ("5.2:1", 1),
    "435 RPM": ("13.7:1", 2),
    "312 RPM": ("19.2:1", 2),
    "223 RPM": ("26.9:1", 2),
    "117 RPM": ("50.9:1", 3),
    "84 RPM": ("71.2:1", 3),
    "60 RPM": ("99.5:1", 3),
    "43 RPM": ("139:1", 3),
    "30 RPM": ("188:1", 4),
}
# Their length behind the face, by stages: 107.5, 116.4, 125.1, and 134 mm long overall, less their 23.5 mm shafts
YELLOW_JACKET_LENGTHS = {1: 84.0, 2: 92.9, 3: 101.6, 4: 110.5}


def gobilda_values() -> dict[str, str]:
    positions = ", ".join(f"vector({x:g}, {y:g}) * millimeter" for x, y in GOBILDA_HOLES)
    return {"screw": string("M4"), "holePositions": f"[{positions}]", "pilotDiameter": mm(14)}


FTC_MOTORS = Node(
    "motor",
    [
        Value(
            "Yellow Jacket",
            next=Node(
                "speed",
                [
                    Value(
                        speed,
                        {
                            "partName": string(f"5203 Series Yellow Jacket ({ratio}, {speed})"),
                            **gobilda_values(),
                            # 37.5 mm at its face; 8 mm REX shaft, 23.5 mm long
                            "bodyDiameter": mm(37.5),
                            "bodyLength": mm(YELLOW_JACKET_LENGTHS[stages]),
                            "shaftDiameter": mm(8),
                            "shaftLength": mm(23.5),
                        },
                    )
                    for speed, (ratio, stages) in YELLOW_JACKETS.items()
                ],
                default="435 RPM",
            ),
        ),
    ],
)

FTC_GEARBOXES = Node(
    "gearbox",
    [
        Value("goBILDA planetary", {"partName": string("5103 Series Planetary Gearbox"), **gobilda_values()}),
        leaf("UltraPlanetary", "UltraPlanetary", Face("M3", mm(32), [30, 90, 150, 210, 270, 330], mm(22))),
    ],
)


CONTENTS = [
    Table("frcMotorTable", FRC_MOTORS),
    Table("frcGearboxTable", FRC_GEARBOXES),
    Table("ftcMotorTable", FTC_MOTORS),
    Table("ftcGearboxTable", FTC_GEARBOXES),
]
