# FTC fasteners

What the Screw, Nut, and Washer features need in order to offer FTC's hardware: goBILDA's M4 hardware and REV's
M3 hardware, with each part's dimensions measured from the vendor's own STEP model. The features generate
imperial sizes only (`fastenerTables.py`'s `SIZES`), so this is the catalog the metric sizes are built from.

## What teams insert

From ConfigLib, FTC's library in FRCDesign, over the last year (`fs cots 'screw|nut|washer' -l config -d`):

| Part | Inserts | Options chosen |
| --- | --- | --- |
| Custom Screw | 3210 | M4 2940, M3 200, M5 38, M6 15, M2 11, M2.5 6 |
| — M4 head | 2799 | Socket 2109, button 561, countersunk 70, low profile 25 |
| — M3 head | 183 | Socket 110, button 48, 5 mm hex 17, low profile 3, countersunk 3 |
| — Material | 3210 | 18-8 stainless 3034 (its default), black oxide 176 |
| S - Nut (GB) | 337 | M4 nylock 299, M4 hex 35, M4 square 3, M4 hook 0 |
| Custom Nut | 333 | M4 241, M3 62, M6 13, M5 6; hex 329, square 4 |
| Custom Washer | 217 | M4 193, custom 15, M3 5, M5 3 |

So the size and head default to an M4 socket head, an M4 nylock, and an M4 washer; the vendor and finish default to
the cheapest (below). FTC's countersunk M4s are bought
elsewhere (ConfigLib's references are Amazon's), since neither goBILDA nor REV sells one. Screw lengths are typed,
so FRCDesign doesn't count them.

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

## AndyMark: M3 and M4

From AndyMark's store (`andymark.com/products.json`, the list `fastenerTables.py fetch` already reads for its
imperial screws). Zinc-plated steel unless a row says otherwise; lengths are under the head. AndyMark publishes no
models for these, so they take the standard's nominal dimensions (ISO 4762, ISO 7380, ISO 10642).

| Part | Lengths (mm), SKU |
| --- | --- |
| M4 socket head, 3 mm hex | 5 am-1343, 10 am-1264, 25 am-1532, 30 am-1533, 40 am-1422, 50 am-1401, 55 am-1251 (black oxide) |
| M4 button head, 2.5 mm hex | 35 am-1441 (black oxide) |
| M4 flat head | 8 am-1788, 12 am-1789: Phillips drive, so left out (ConfigLib's countersunk M4s are hex drive) |
| M3 socket head, 2.5 mm hex | 6 am-1254, 10 am-1021, 14 am-1367, 30 am-1578, 35 am-1671, 45 am-1659 (stainless), 55 am-1379 (stainless) |
| M3 socket head, thread lock patch | 5 am-1675, 8 am-1500, 12 am-1706 (14 am-1750 has a plain twin) |
| M3 flat head, 2 mm hex | 8 am-1723, 12 am-1765; 5 am-1724 with a thread lock patch |

| Part | SKU | Dimensions |
| --- | --- | --- |
| M4 nylock nut | am-1435 | 5.0 tall |
| M3 nylock nut | am-1023 | Not stated |
| M3 hex nut | am-1386 | 2.4 tall |
| M3 jam nut | am-1405 | 1.8 tall |
| M4 lock washer | am-1548 | 4.4 inner, 0.9 thick; outer not stated |
| M3 lock washer | am-1551 | 3.4 inner, 6.2 outer, 0.8 thick |

AndyMark sells no metric flat washer for M3 or M4.

## Defaults: the cheapest

Where usage doesn't decide, a part defaults to its cheapest: the vendor, and so the finish, with the lowest price per
piece in a pack of at most 250, the most a team buys at once. McMaster-Carr's 500 and 2,000 packs are left out: its
2,000 pack of 18-8 M3 nylocks undercuts its 100 pack of zinc-plated ones by $0.0002 a nut, for $97. Prices from
each vendor's store on 2026-11-08, per piece, the pack size in brackets.

| Part | goBILDA | AndyMark | REV | McMaster-Carr | Default |
| --- | --- | --- | --- | --- | --- |
| M4 x 10 socket head | $0.068 (250) | $0.20 (1) | — | Not in yet | goBILDA, for now |
| M4 x 10 button head | $0.136 (25) | — (35 mm only, $0.16) | — | Not in yet | goBILDA, for now |
| M4 nylock | $0.060 (250) | $0.13 (1) | — | $0.049 (100), zinc plated | McMaster-Carr 90576A103 |
| M4 hex nut | $0.100 (25) | — | — | Not in yet | goBILDA, for now |
| M4 flat washer, 8 mm | $0.040 (250) | — | — | Not in yet | goBILDA, for now |
| M3 x 6 socket head | Not priced (6) | $0.12 (1) | — | Not in yet | AndyMark, for now |
| M3 x 6 to 35 hex cap | — | — | $0.105 to $0.18 (100) | — | Once REV prices each length |
| M3 nylock | — | $0.06 (1) | $0.06 to $0.0875 (100) | $0.049 (100), zinc plated | McMaster-Carr 90576A102 |
| M3 hex nut | — | $0.04 (1) | $0.06 to $0.0875 (100) | Not in yet | AndyMark, for now |

goBILDA prices a screw by length, and its 250 pack halves its price per piece. REV quotes one price range per
product, not per length, so its per-length prices have to be read off the product page's options. ConfigLib's
default material, 18-8 stainless, isn't the cheapest for any part priced so far, so the default finish is zinc
plating.

## McMaster-Carr

McMaster-Carr's pages refuse automated reads, so its part numbers come from you (below), not a fetch. Each table
lists a size's lengths with up to four supplies, each its own part number and price: a small pack imported, a small
pack of United States origin, and a large pack of each. The catalog takes, for each length, the supply cheapest per
piece. The series to take them from, with the few numbers search results confirm:

| Part | Series | Confirmed |
| --- | --- | --- |
| Socket head, 18-8 stainless | 91292A | M3 x 8 91292A112, M4 x 10 91292A116, M4 x 12 91292A117 |
| Socket head, black-oxide alloy steel | 91290A | M4 x 10 91290A144, M4 x 12 91290A148 |
| Button head, 18-8 stainless | 92095A | M3 x 6 92095A179 |
| Low profile socket head, 18-8 stainless | 92855A | — |
| Flat head, 18-8 stainless | 92125A | — |
| Nylon-insert locknut, 18-8 stainless | 93625A | Below |
| Flat washer, 18-8 stainless | 93475A | — |

Nylon-insert locknuts, from its tables. An M4 is 7 wide and 5 tall, an M3 5.5 wide and 4 tall, every finish alike.
Prices are a pack's, its size in brackets.

| Size | Finish | Strength | Small pack | Large pack |
| --- | --- | --- | --- | --- |
| M4 x 0.7 | Zinc plated | Class 10 | 94645A101, $13.25 (100) | 94645A208, $185.50 (2,000) |
| M4 x 0.7 | Black corrosion-resistant coated | Class 10 | 90237A110, $10.97 (5) | — |
| M4 x 0.7 | Zinc-yellow chromate plated | Class 8 | 90576A812, $10.91 (100) | — |
| M4 x 0.7 | Zinc plated | Class 8 | 90576A103, $4.92 (100) | — |
| M4 x 0.7 | Black corrosion-resistant coated | Class 8 | 90819A111, $11.00 (5) | — |
| M4 x 0.7 | Zinc plated | Class 6 | 90631A112, $5.03 (100) | — |
| M3 x 0.5 | Zinc-yellow chromate plated | Class 8 | 90576A811, $10.70 (100) | 90576A111, $149.80 (2,000) |
| M3 x 0.5 | Zinc plated | Class 8 | 90576A102, $4.88 (100) | — |
| M3 x 0.5 | 18-8 stainless | — | 93625A100, $6.94 (100) | 93625A121, $97.16 (2,000) |
| M3 x 0.5 | Black-oxide 18-8 stainless | — | 93625A223, $5.40 (10) | 93625A129, $189.00 (500) |
| M3 x 0.5 | 316 stainless | — | 94205A220, $7.12 (50) | — |

McMaster-Carr's M4 nylock is 5 mm tall to goBILDA's 4.85, and its M3 4 mm, as REV's is.

## What the features need

- Metric sizes: `Size` holds threads per inch and inch diameters, and `std`'s ISO hole and thread tables
  replace the ANSI ones for M3 and M4.
- Heads: hex (REV's hex cap and goBILDA's hex head), low profile socket, and REV's T-slot.
- Nuts: a square nut 7 mm wide, and the nylock's dome as measured above, in place of `_nylock`'s proportions.
- Washers: lock washers, if they're offered at all.
- Vendors: goBILDA, beside REV, AndyMark, and McMaster-Carr. AndyMark's metric screws are titled
  `M4-0.7 x 10 mm Socket Head Cap Screw`, which `_STORE_SCREWS` has to match too.
- Heads: flat (countersunk) in metric, for AndyMark's and McMaster-Carr's.

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
| McMaster-Carr's M3 and M4 parts, nylocks aside | Its pages refuse automated reads | Each series' metric table (System of Measurement: Metric, then Thread Size M3 x 0.5 and M4 x 0.7), every length and supply with its price, and a STEP of one length per head, nut, and washer, from the part's CAD tab |
| REV's price per length | REV quotes one range per product | Each option's price on the hex cap screw and M3 nut pages |
| AndyMark M4 lock washer, am-1548 | Its outer diameter isn't stated | Calipers on one, or a STEP (optional) |
