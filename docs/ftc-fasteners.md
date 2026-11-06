# FTC fasteners

What the Screw, Nut, and Washer features need in order to offer FTC's hardware: goBILDA's M4 hardware and REV's
M3 hardware, with each part's dimensions measured from the vendor's own STEP model. The features generate
imperial sizes only (`fastenerTables.py`'s `SIZES`), so this is the catalog the metric sizes are built from.

FRCDesign has no FTC library, so there is no usage data to rank sizes or pick defaults (`fs cots -l ftc` names a
library id FRCDesign doesn't have).

## goBILDA: M4 x 0.7

Every screw is zinc-plated steel, and its length is measured under the head. Through 60 mm a screw is threaded
its whole length. The 75 mm socket head is threaded about 21 mm from the tip, which is ISO 4762's `b = 2d + 12`.

| Part | Series | Lengths (mm) |
| --- | --- | --- |
| Socket head | 2800-0004-LLLL | 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 25, 30, 35, 40, 45, 50, 55, 60, 75 |
| Button head | 2802-0004-LLLL | 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 30, 35, 40, 45, 50, 55, 60 |
| Low profile socket head | 2804-0004-LLLL | 5, 6, 22, 25, 30, 35, 45, 50, 55, 60 |
| Hex head | 2813-0004-LLLL | 8, 10, 12, 16 |
| M3 x 0.5 socket head | 2800-0003-LLLL | 6, 8, 10, 12 |

goBILDA also sells thread-locking and thread-forming M4 socket heads, which are left out.

| Head | Diameter | Height | Drive |
| --- | --- | --- | --- |
| Socket (M4) | 7.0 | 4.0 | 3.0 hex, 2.25 deep |
| Button | 7.6 | 2.2 | 2.5 hex |
| Low profile socket | 7.0 | 2.8 | 2.5 hex, 1.4 deep |
| Hex | 6.9 across flats | 2.8, of which a 6.15 mm washer face is 0.25 | — |
| Socket (M3) | 5.5 | 3.0 | 2.5 hex |

| Nut | SKU | Across flats | Height |
| --- | --- | --- | --- |
| Nylock | 2812-0004-0007 | 7.0 | 4.85: a 4.04 hex under the dome; insert bore 3.36 |
| Hex | 2811-0004-0007 | 7.0 | 2.75 |
| Square | 2803-0007-0007 | 7.0 | 3.0 |

| Washer | SKU | Inner | Outer | Thickness |
| --- | --- | --- | --- | --- |
| Flat, 8 mm | 2801-0004-0008 | 4.0 | 8.0 | 1.0 |
| Flat, 11 mm | 2801-0004-0011 | 4.15 | 10.9 | 1.5 |
| Split lock | 2809-0004-0007 | 4 (nominal) | 6.8 | 1.69 overall, uncompressed |
| External tooth lock | 2810-0004-0008 | 4.3 | 8.0 | 0.5, teeth twisted to 1.21 |

## REV: M3 x 0.5

Zinc-plated steel. A hex cap screw's length is measured under the head.

| Part | SKU | Length (mm) |
| --- | --- | --- |
| Hex cap screw | REV-41-1713 | 6 |
| Hex cap screw | REV-41-1359 | 8 |
| Hex cap screw | REV-41-1123 | 10 |
| Hex cap screw | REV-41-1360 | 16 |
| Hex cap screw | REV-41-1124 | 20 |
| Hex cap screw | REV-41-1715 | 25 |
| Hex cap screw | REV-41-1125 | 35 |
| T-slot screw | REV-41-1907 | 8 |

| Part | Dimensions |
| --- | --- |
| Hex cap head | 5.5 across flats, 2.0 tall, a 120° chamfer on top |
| T-slot head | The hex cap's, cut to 3.2 wide (REV's drawing; the model is 3.15) so it drops into the 15 mm extrusion's slot |
| Nyloc nut, REV-41-1361 | 5.5 across flats, 4.0 tall: a 3.33 hex, then a 3.35 collar; insert bore 2.46. REV's product page says 4.4, but its drawing and model say 4.0 |
| Hex nut, REV-41-1126 | 5.5 across flats, 2.4 tall, from REV's drawing; it has no model |
| Washer, REV-41-2886 | Stainless steel, for M3. REV publishes no dimensions, model, or drawing |

REV's UltraPlanetary hardware pack (REV-41-1609) has M3 button heads, which REV doesn't sell on their own; they're
left out.

## What the features need

- Metric sizes: `Size` holds threads per inch and inch diameters, and `std`'s ISO hole and thread tables
  replace the ANSI ones for M3 and M4.
- Heads: hex (REV's hex cap and goBILDA's hex head), low profile socket, and REV's T-slot.
- Nuts: a square nut 7 mm wide, and the nylock's dome as measured above, in place of `_nylock`'s proportions.
- Washers: lock washers, if they're offered at all.
- Vendors: goBILDA, beside REV.

## STEP files

Downloaded from the vendors and measured above (goBILDA at `https://www.gobilda.com/content/step_files/<SKU>.zip`,
REV at `https://www.revrobotics.com/content/cad/<SKU>.STEP`): goBILDA 2800-0003-0008, 2800-0004-0008/0040/0045/
0050/0055/0060/0075, 2802-0004-0008/0040/0060, 2804-0004-0006/0030/0060, 2813-0004-0008, 2811-0004-0007,
2812-0004-0007, 2803-0007-0007, 2801-0004-0008, 2801-0004-0011, 2809-0004-0007, 2810-0004-0008; REV
REV-41-1713, 1359, 1123, 1360, 1124, 1715, 1125, 1907, and 1361.

Still needed:

| Part | Why | What would do |
| --- | --- | --- |
| REV M3 washer, REV-41-2886 | Nothing published | A STEP, or calipers: inner diameter, outer diameter, thickness |
| goBILDA split lock washer, 2809-0004-0007 | Its model is freeform, so it gives only the outside | Its section's width and thickness, if lock washers are offered |
| REV M3 hex nut, REV-41-1126 | Drawing only, so its chamfers are guessed | A STEP (optional) |
