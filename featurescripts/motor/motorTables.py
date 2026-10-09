"""Tables for Robot motor: the mounting faces of motors and gearboxes, and their block models.

Named as FRCDesign names them, in order of how often teams use them there (`fs cots motor`, `fs cots motor -l ftc`).
Sources are in `motor/vendor/` (vendors' drawings) and noted beside each. Holes are as a face's drawing shows them,
looking at the face (its shaft toward you): angles go counterclockwise from the right, and positions are (right, up).

A motor's block model is FRCDesign's Block Motor (its `blockMotor` option, turned `blockAngle` to line up with its
holes), or for motors that has none, an envelope of the motor's own (`bodyDiameter` and the rest).
"""

from __future__ import annotations

import dataclasses

from fs_cli.tables import Node, Table, Value, inch, mm, string


@dataclasses.dataclass
class Holes:
    """Screws on a bolt circle at `angles`, or at `positions` (in millimeters) for holes on several circles."""

    screw: str
    bolt_circle: str | None = None
    angles: list[float] | None = None
    positions: list[tuple[float, float]] | None = None

    def values(self) -> dict[str, str]:
        values = {"screw": string(self.screw)}
        if self.positions is not None:
            values["holePositions"] = "[" + ", ".join(f"vector({x:g}, {y:g}) * millimeter" for x, y in self.positions) + "]"
        else:
            assert self.bolt_circle is not None and self.angles is not None
            values["boltCircleDiameter"] = self.bolt_circle
            values["holeAngles"] = "[" + ", ".join(f"{angle:g} * degree" for angle in self.angles) + "]"
        return values


@dataclasses.dataclass
class Envelope:
    """A block model of our own, for motors FRCDesign's Block Motor doesn't have: a cylinder behind the face, and the
    pilot and shaft in front of it."""

    diameter: str
    length: str
    pilot_height: str
    shaft_diameter: str
    shaft_length: str

    def values(self) -> dict[str, str]:
        return {
            "bodyDiameter": self.diameter,
            "bodyLength": self.length,
            "pilotHeight": self.pilot_height,
            "shaftDiameter": self.shaft_diameter,
            "shaftLength": self.shaft_length,
        }


def block_motor(option: str, angle: float = 0) -> dict[str, str]:
    """FRCDesign's Block Motor, configured as `option` (its Motor list's option id), turned `angle` (counterclockwise,
    looking at the face) to line up with the face's holes."""
    return {"blockMotor": string(option), "blockAngle": f"{angle:g} * degree"}


def leaf(name: str, part_name: str, holes: Holes, pilot: str, block: dict[str, str] | None = None) -> Value:
    return Value(name, {"partName": string(part_name), **holes.values(), "pilotDiameter": pilot, **(block or {})})


def patterns(part_name: str, pilot: str, options: dict[str, Holes], block: dict[str, str] | None = None) -> Node:
    """A choice between a face's hole patterns, for faces with several."""
    return Node(
        "pattern",
        [leaf(name, part_name, holes, pilot, block) for name, holes in options.items()],
        display_name="Hole pattern",
    )


# Every 30°, but for the bump at 270° (WCP's drawing: docs.wcproducts.com, "Mounting Kraken X60"); WCP says they fit
# the Falcon's 6 holes and the CIM's 2
KRAKEN_ANGLES = [angle for angle in range(0, 360, 30) if angle != 270]
FALCON_ANGLES = [0, 60, 120, 180, 240, 300]
CIM_ANGLES = [0, 180]
# Every 45°, but for the flats at 90° and 270° (REV-21-1653-DR, REV-21-1652-REV-11-2159-DR)
FLATS_ANGLES = [0, 45, 135, 180, 225, 315]
# Motors which drop in for a CIM have a 3/4 in. pilot (WCP, "Physical Specifications")
CIM_PILOT = inch(0.75)
# FRCDesign's Block Motor has its Krakens' bumps at 0°, where ours are at 270°
KRAKEN_BLOCK_ANGLE = -90


def kraken(name: str, bolt_circle: str, option: str) -> Value:
    options = {
        "All": Holes("#10", bolt_circle, KRAKEN_ANGLES),
        "Falcon 500": Holes("#10", bolt_circle, FALCON_ANGLES),
        "CIM": Holes("#10", bolt_circle, CIM_ANGLES),
    }
    if bolt_circle != inch(2):
        # The X44's bolt circle is its own
        return leaf(name, name, options["All"], CIM_PILOT, block_motor(option, KRAKEN_BLOCK_ANGLE))
    return Value(name, next=patterns(name, CIM_PILOT, options, block_motor(option, KRAKEN_BLOCK_ANGLE)))


FALCON = Holes("#10", inch(2), FALCON_ANGLES)
FALCON_VERSIONS = Node(
    "version",
    [
        leaf("V1/2", "Falcon 500", FALCON, CIM_PILOT, block_motor("KrakenX60")),
        leaf("V3", "Falcon 500", FALCON, CIM_PILOT, block_motor("Falcon_500_V3")),
    ],
)

# REV-21-1650-DR: 4x #10-32, a 19.1 mm pilot (V1.0 and V1.1 alike)
NEO = Holes("#10", inch(2), [0, 90, 180, 270])
NEO_VERSIONS = Node(
    "version",
    [
        leaf("V1.1", "NEO", NEO, mm(19.1), block_motor("NEO_V1_1")),
        # REV-21-1653-DR: a 19 mm pilot, with holes but at its flats
        leaf("V2.0", "NEO 2.0", Holes("#10", inch(2), FLATS_ANGLES), mm(19), block_motor("Copy_of_NEO_Vortex")),
        leaf("V1.0", "NEO", NEO, mm(19.1), block_motor("NEO_V1_0")),
    ],
)

# CTRE's Minion.STEP (github.com/CrossTheRoadElec/Device-CADs): its holes are tapped for three screws; an 18.8 mm pilot
# 2 mm tall, a 38 mm body 49.5 mm long, and a SplineXS shaft 23 mm past the face
MINION = patterns(
    "Minion",
    mm(18.8),
    {
        # On a 1 in. bolt circle, every 60° but 90°
        "#10-32": Holes("#10", inch(1), [30, 150, 210, 270, 330]),
        "550": Holes("M3", mm(25), [0, 180]),
        # Two more M4 holes are on a 32 mm bolt circle, at 124.2° and 304.2°: which motor's they're for isn't known
        "775": Holes("M4", mm(29), [53.5, 233.5]),
    },
    Envelope(mm(38), mm(49.5), mm(2), mm(7.94), mm(23)).values(),
)

# The Thrifty Bot's TTB-0350 drawing: #10-32 on a 1.375 in. bolt circle at 45°, M4 on a 29 mm one at 0°, a 19 mm pilot
PULSAR = patterns(
    "Thrifty Pulsar",
    mm(19),
    {
        "#10-32": Holes("#10", inch(1.375), [45, 135, 225, 315]),
        "775": Holes("M4", mm(29), [0, 90, 180, 270]),
    },
    block_motor("Copy_of_NEO_V1_1"),
)

FRC_MOTORS = Node(
    "motor",
    [
        kraken("Kraken X60", inch(2), "Kraken_X60"),
        Value("Falcon 500", next=FALCON_VERSIONS),
        kraken("Kraken X44", inch(1.375), "Kraken_X44"),
        Value("NEO", next=NEO_VERSIONS),
        # REV-21-1652-REV-11-2159-DR; its pilot isn't dimensioned: 1.25 in. is measured from the drawing
        leaf("NEO Vortex", "NEO Vortex", Holes("#10", inch(2), FLATS_ANGLES), inch(1.25), block_motor("NEO_Vortex")),
        # REV-21-1651-DR: M3 on a 25 mm bolt circle, a 13 mm pilot
        leaf("NEO 550", "NEO 550", Holes("M3", mm(25), [90, 270]), mm(13), block_motor("NEO_550")),
        leaf("CIM", "CIM", Holes("#10", inch(2), CIM_ANGLES), CIM_PILOT),
        # 2x M4 (REV's MAXPlanetary 775 guide); 29 mm and 17.5 mm are from the previous version of Robot motor
        leaf("RS-775", "RS-775", Holes("M4", mm(29), [0, 180]), mm(17.5)),
        Value("Minion", next=MINION),
        leaf("Mini CIM", "Mini CIM", Holes("#10", inch(2), CIM_ANGLES), CIM_PILOT),
        Value("Thrifty Pulsar", next=PULSAR),
    ],
)

FRC_GEARBOXES = Node(
    "gearbox",
    [
        # REV-21-2100-DR: 4x #10-32 on a 2 in. bolt circle, in two orientations; the output's boss isn't dimensioned:
        # 1.25 in. is measured from the drawing
        leaf("MAXPlanetary", "MAXPlanetary", Holes("#10", inch(2), [0, 135, 180, 315]), inch(1.25)),
        # WCP's "Basic Features": 6x #10-32 on a 2 in. bolt circle; the output's 1.5 in. is measured from its picture
        leaf("PlanetaryX", "PlanetaryX", Holes("#10", inch(2), FLATS_ANGLES), inch(1.5)),
        # REV-41-1600-DR: 6x M3 at the hex's corners on a 32 mm bolt circle, around a 22 mm boss
        leaf("UltraPlanetary", "UltraPlanetary", Holes("M3", mm(32), [30, 90, 150, 210, 270, 330]), mm(22)),
        # #10-32 on a 2 in. bolt circle (VEX); the pilot is from the previous version of Robot motor
        leaf("VersaPlanetary", "VersaPlanetary", Holes("#10", inch(2), [45, 135, 225, 315]), inch(0.75)),
    ],
)

# goBILDA's 5203 and 5103 spec sheets and STEP: M4 on their 16 mm square pattern, and two more 24 mm apart; their
# plates' 14 mm hole clears the pilot
GOBILDA_PILOT = mm(14)
GOBILDA_SQUARE = [(8, 8), (-8, 8), (-8, -8), (8, -8)]
GOBILDA_PATTERNS = {
    "16 mm square": Holes("M4", positions=GOBILDA_SQUARE),
    "All": Holes("M4", positions=[(12, 0), (8, 8), (-8, 8), (-12, 0), (-8, -8), (8, -8)]),
}
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
# Their length behind the face, by stages: 107.5, 116.4, 125.1, and 134 mm long overall, less their 23.5 mm shafts;
# 37.5 mm across at the gearbox; an 8 mm REX shaft. The pilot's height isn't dimensioned.
YELLOW_JACKET_LENGTHS = {1: 84.0, 2: 92.9, 3: 101.6, 4: 110.5}


def yellow_jacket(speed: str, ratio: str, stages: int) -> Node:
    envelope = Envelope(mm(37.5), mm(YELLOW_JACKET_LENGTHS[stages]), mm(2), mm(8), mm(23.5))
    return patterns(f"5203 Series Yellow Jacket ({ratio}, {speed})", GOBILDA_PILOT, GOBILDA_PATTERNS, envelope.values())


FTC_MOTORS = Node(
    "motor",
    [
        Value(
            "Yellow Jacket",
            next=Node(
                "speed",
                [Value(speed, next=yellow_jacket(speed, ratio, stages)) for speed, (ratio, stages) in YELLOW_JACKETS.items()],
                default="435 RPM",
            ),
        ),
    ],
)

FTC_GEARBOXES = Node(
    "gearbox",
    [
        Value("goBILDA planetary", next=patterns("5103 Series Planetary Gearbox", GOBILDA_PILOT, GOBILDA_PATTERNS)),
        leaf("UltraPlanetary", "UltraPlanetary", Holes("M3", mm(32), [30, 90, 150, 210, 270, 330]), mm(22)),
    ],
)


CONTENTS = [
    Table("frcMotorTable", FRC_MOTORS),
    Table("frcGearboxTable", FRC_GEARBOXES),
    Table("ftcMotorTable", FTC_MOTORS),
    Table("ftcGearboxTable", FTC_GEARBOXES),
]
