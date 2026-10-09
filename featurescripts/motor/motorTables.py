"""Tables for Robot motor: the mounting faces of motors and gearboxes, and the envelopes of motors' block models.

Named as FRCDesign names them, in order of how often teams use them there (`fs cots motor`). Sources are in
`motor/vendor/` (REV's drawings) and noted beside each; angles go counterclockwise looking at the face, from +x.
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
    """A motor's envelope behind its face: round, or round with flats, and its shaft (none for through-bore motors)."""

    diameter: str
    length: str
    flats: str | None = None
    shaft_diameter: str | None = None
    shaft_length: str | None = None

    def values(self) -> dict[str, str]:
        values = {"bodyDiameter": self.diameter, "bodyLength": self.length}
        if self.flats is not None:
            values["bodyFlats"] = self.flats
        if self.shaft_diameter is not None and self.shaft_length is not None:
            values["shaftDiameter"] = self.shaft_diameter
            values["shaftLength"] = self.shaft_length
        return values


# Every 30°, but for the bump at 270° (WCP's drawing: docs.wcproducts.com, "Mounting Kraken X60")
KRAKEN_ANGLES = [angle for angle in range(0, 360, 30) if angle != 270]
# Every 45°, but for the flats at 90° and 270° (REV-21-1653-DR, REV-21-1652-REV-11-2159-DR)
FLATS_ANGLES = [0, 45, 135, 180, 225, 315]
# The pilot of motors which drop in for a CIM: 3/4 in., 3/16 in. tall (WCP, "Physical Specifications")
CIM_PILOT = {"pilot_diameter": inch(0.75), "pilot_height": inch(0.1875)}

MOTORS: dict[str, tuple[Face, Body | None]] = {
    "Kraken X60": (
        Face("#10", inch(2), KRAKEN_ANGLES, **CIM_PILOT),
        # 60 mm, without the bump (63.5 mm across it), 75 mm long with its Talon FX; SplineXS shaft
        Body(mm(60), mm(75), shaft_diameter=mm(8), shaft_length=inch(1.25)),
    ),
    "Falcon 500": (
        # 6x #10-32 on a 2 in. bolt circle; the Kraken X60's holes include them
        Face("#10", inch(2), [0, 60, 120, 180, 240, 300], **CIM_PILOT),
        # 60 mm by 81 mm, 4.569 in. long with its shaft (AndyMark)
        Body(mm(60), mm(81), shaft_diameter=mm(8), shaft_length=inch(1.38)),
    ),
    "NEO": (
        # REV-21-1650-DR: 4x #10-32, a 19.1 mm pilot 3.5 mm tall, an 8 mm shaft 31.5 mm past it
        Face("#10", inch(2), [0, 90, 180, 270], mm(19.1), mm(3.5)),
        Body(mm(60), mm(58.3), shaft_diameter=mm(8), shaft_length=mm(35)),
    ),
    "Kraken X44": (
        Face("#10", inch(1.375), KRAKEN_ANGLES, **CIM_PILOT),
        Body(mm(44), mm(75), shaft_diameter=mm(8), shaft_length=inch(1.25)),
    ),
    "NEO Vortex": (
        # REV-21-1652-REV-11-2159-DR; the pilot isn't dimensioned: 1.25 in. is measured from the drawing
        Face("#10", inch(2), FLATS_ANGLES, inch(1.25)),
        # 79.7 mm long docked to a SPARK Flex; its 1/2 in. hex shaft goes through it, so has no length of its own
        Body(mm(60), mm(79.7), flats=inch(2)),
    ),
    "NEO 550": (
        # REV-21-1651-DR: M3 on a 25 mm bolt circle, a 13 mm pilot; the pilot's height isn't dimensioned
        Face("M3", mm(25), [90, 270], mm(13), mm(1.5)),
        Body(mm(35), mm(44.5), shaft_diameter=mm(3.175), shaft_length=mm(8.5)),
    ),
    "NEO 2.0": (
        # REV-21-1653-DR: a 19 mm pilot, its 15T spline 31.5 mm past it and 35 mm past the face
        Face("#10", inch(2), FLATS_ANGLES, mm(19), mm(3.5)),
        Body(mm(60), mm(48), flats=inch(2), shaft_diameter=mm(8), shaft_length=mm(35)),
    ),
    # Without block models, for now
    "CIM": (Face("#10", inch(2), [0, 180], inch(0.75)), None),
    "Mini CIM": (Face("#10", inch(2), [0, 180], inch(0.75)), None),
    # 2x M4 (REV's MAXPlanetary 775 guide); 29 mm and 17.5 mm are from the previous version of Robot motor
    "RS-775": (Face("M4", mm(29), [0, 180], mm(17.5)), None),
}

GEARBOXES: dict[str, Face] = {
    # REV-21-2100-DR: 4x #10-32 on a 2 in. bolt circle, in two orientations; the output's boss isn't dimensioned:
    # 1.25 in. is measured from the drawing
    "MAXPlanetary": Face("#10", inch(2), [0, 135, 180, 315], inch(1.25)),
    # WCP's "Basic Features": 6x #10-32 on a 2 in. bolt circle; the output's 1.5 in. is measured from its picture
    "PlanetaryX": Face("#10", inch(2), FLATS_ANGLES, inch(1.5)),
    # REV-41-1600-DR: 6x M3 at the hex's corners on a 32 mm bolt circle, around a 22 mm boss
    "UltraPlanetary": Face("M3", mm(32), [30, 90, 150, 210, 270, 330], mm(22)),
    # #10-32 on a 2 in. bolt circle (VEX); the pilots are from the previous version of Robot motor
    "VersaPlanetary": Face("#10", inch(2), [45, 135, 225, 315], inch(0.75)),
    "Sport": Face("#10", inch(2), [45, 135, 225, 315], inch(1.5)),
}


def motor_node() -> Node:
    entries = []
    for name, (face, body) in MOTORS.items():
        values = {"partName": string(name), **face.values()}
        if body is not None:
            values.update(body.values())
        entries.append(Value(name, values))
    return Node("motor", entries)


def gearbox_node() -> Node:
    return Node("gearbox", [Value(name, {"partName": string(name), **face.values()}) for name, face in GEARBOXES.items()])


CONTENTS = [
    Table("motorTable", motor_node()),
    Table("gearboxTable", gearbox_node()),
]
