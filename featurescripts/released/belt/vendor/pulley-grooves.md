# Vendors' pulley grooves

Robot pulley's grooves (`getPulleyGroove` in `robotPulleyCommon.fs`) are these vendors' pulleys' grooves, made
`GROOVE_CLEARANCE` (0.1 mm) bigger for printing. Measured from each pulley's STEP file (fetched 2026-11-15) on one end
of its teeth: each arc's radius, and its center's depth inside the lands' circle and its distance across the groove's
axis (on the counter clockwise side; the other side's is its mirror image). All in mm.

`tests/featurescript/fs_eval_sketch_test.py`'s `test_grooves_without_clearance_are_vendors` checks that the grooves
without the clearance are these, to 0.01 mm.

## 2mm GT2: goBILDA 3421-0014-0060 (60T)

From https://www.gobilda.com/content/step_files/3421-0014-0060.zip. Lands 0.254 inside the pitch circle (the belt's
standard).

| Arc | Radius | Center depth | Center across |
| --- | --- | --- | --- |
| Bottom | 0.63 | 0.195 | 0 |
| Flank | 1.075 | 0 (on the lands' circle) | -0.4001 |
| Fillet | 0.15 | 0.15 | 0.8134 |

This is Gates' GT2 tooth (R3 0.555, 0.75 tall, R2 1.00 flanks 0.40 across, R1 0.15) with its arcs 0.075 bigger.

## 3mm GT2: WCP-0663 (36T)

From https://wcproducts.info/files/frc/cad/WCP-0663.step. Lands 0.387 inside the pitch circle.

| Arc | Radius | Center depth | Center across |
| --- | --- | --- | --- |
| Bottom | 0.9703 | 0.1981 | 0 |
| Fillet | 0.2997 | 0.2318 | 1.2954 |

Its sides are straight lines from the bottom's arc to the fillets. Robot pulley's are Gates' 3mm GT2 tooth (R3 0.85,
1.14 tall, R2 1.52 flanks, R1 0.25) with its arcs 0.075 bigger, as goBILDA's 2mm grooves are: its bottom is 0.047
deeper than WCP's, and its sides within a few hundredths.

## 3mm HTD: goBILDA 3402-0014-0036 (36T)

From https://www.gobilda.com/content/step_files/3402-0014-0036.zip. Lands 0.389 inside the pitch circle.

| Arc | Radius | Center depth | Center across |
| --- | --- | --- | --- |
| Bottom | 0.8636 | 0.4572 | 0.0305 |
| Fillet | 0.2694 | 0.2694 | 1.1533 |

The bottom's two arcs go up past their centers' depth (the groove's widest below its top), to the fillets.

## 5mm HTD: goBILDA 3411-0014-0024 (24T)

From https://www.gobilda.com/content/step_files/3411-0014-0024.zip. Lands 0.569 inside the pitch circle.

| Arc | Radius | Center depth | Center across |
| --- | --- | --- | --- |
| Bottom | 1.1938 | 0.8097 | 0.3302 |
| Fillet | 0.4699 | 0.4699 | 2.0574 |

The bottom's flat between its arcs is 0.6604 long, and its walls are lines 0.5158 long tangent to its arcs, at 9.794
degrees to the groove's axis.

## RT25: REV-21-2206 (16T, aluminum) and REV-25-2236 (32T, plastic)

From https://www.revrobotics.com/content/cad/REV-21-2206.STEP and
https://www.revrobotics.com/content/cad/REV-25-2236.STEP. Lands 0.307 (16T) and 0.561 (32T) inside the pitch circle;
REV's drawing of the 16T (https://www.revrobotics.com/content/docs/REV-21-2206-DR.pdf) gives a 31.4 mm outside
diameter, 0.47 inside it, so Robot pulley keeps the belt's 0.5588.

| Arc | Radius | Center depth | Center across |
| --- | --- | --- | --- |
| Bottom | 1.8017 | 0.6113 | 0 |
| Flank | 3.2199 | 0 (on the lands' circle) | -1.3028 (16T), -1.29 (32T) |
| Fillet | 0.5266 (16T), 0.8865 (32T) | as its radius | 2.3847 (16T), 2.6983 (32T) |

Robot pulley's fillets are the aluminum pulley's, which leave room for smaller pulleys.
