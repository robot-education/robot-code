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

So the size and head default to an M4 socket head, an M4 nylock, and an M4 washer, from the first vendor, in its
cheapest finish (below). FTC's countersunk M4s are bought
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

## Vendors and finishes

Vendors are listed Generic, McMaster-Carr, goBILDA, REV, AndyMark, and a part defaults to the first that sells it.
goBILDA would go ahead of McMaster-Carr only if it were cheaper across the board, and it isn't: McMaster-Carr's
screws and nuts cost less a piece, smallest pack to smallest pack.

| Part | McMaster-Carr | goBILDA |
| --- | --- | --- |
| M4 x 10 socket head | $0.103 (100) | $0.136 (25), $0.068 (250) |
| M4 nylock | $0.049 (100) | $0.120 (25), $0.060 (250) |
| M4 hex nut | $0.034 (100) | $0.100 (25) |

Washers are compared once McMaster-Carr's are in.

Generic is a screw in every size, head and length any vendor sells, and a washer in each size, each in one finish,
as the FRC fasteners have them; there's no generic nut, as there isn't for FRC.

Within a vendor, a part's finishes are listed cheapest first, so the default is never a dearer finish for its own
sake: by price per piece in its smallest pack, since that's what a team orders first, over the lengths both finishes
sell. Where two are within a cent, the one sold in more lengths goes first, so the default finish has a part at
most lengths. The finishes are the
catalog's own: zinc plated, black (black oxide), and stainless (18-8). Where a vendor sells a finish at several
strengths, a steel nut is McMaster-Carr's Class 8, medium strength. McMaster-Carr's unfinished steel, zinc-yellow
chromate, black coated and 316 stainless are left out, unfinished steel although it's the cheapest nut, since it
rusts.

| Part | McMaster-Carr, cheapest first |
| --- | --- |
| M3 nylock | Zinc plated $0.049, stainless $0.069 |
| M4 nylock | Zinc plated $0.049, stainless $0.070, black $0.60 (black-oxide 18-8, 10 pack) |
| M3 hex nut | Zinc plated $0.031, stainless $0.057, black $0.081 (black-oxide 18-8) |
| M4 hex nut | Zinc plated $0.034, stainless $0.073, black $0.121 (black-oxide 18-8) |
| M3 socket head | Stainless (23 lengths), black (22), zinc plated (7) |
| M4 socket head | Stainless (27 lengths), zinc plated (11), black (32) |
| M3 button head | Stainless (20 lengths), black (10), zinc plated (9) |
| M4 button head | Stainless (23 lengths), black (12), zinc plated (11) |

McMaster-Carr's 18-8 socket heads cost no more than its zinc-plated ones at the lengths both sell: an M3 x 10 is
$0.071 stainless to $0.171 zinc plated, and an M4 is within two cents from 5 to 20 mm (an M4 x 10 is $0.103
either way). Zinc
plating comes in 7 M3 lengths and 11 M4, so stainless is the screws' default, as it is ConfigLib's. Black oxide is
last at M4, one to ten cents dearer from 5 to 20 mm; beyond 25 mm it's often cheaper, but those are seldom used.
Its 18-8 button heads are the cheapest at M3 and at the M4 lengths used most (an M4 x 10 is $0.094, to $0.141
black and $0.178 zinc plated), and come in twice the lengths, so stainless goes first for them too.
A black nut is black-oxide 18-8 stainless, which McMaster-Carr sells for less than black-oxide steel.

## McMaster-Carr

McMaster-Carr's pages refuse automated reads, so its part numbers come from you (below), not a fetch. Each table
lists a size's lengths with up to four supplies, each its own part number and price: a small pack imported, a small
pack of United States origin, and a large pack of each. The catalog takes, for each length, the cheaper small pack,
imported or United States origin; a large pack only where there's no small one. The series to take them from, with the few numbers search results confirm:

| Part | Series | Confirmed |
| --- | --- | --- |
| Socket head, 18-8 stainless | 91292A, 90751A | In the snapshot |
| Socket head, black-oxide alloy steel | 91290A | In the snapshot |
| Button head | 92095A 18-8, 91239A black oxide, 91306A zinc plated | In the snapshot |
| Flat head, 18-8 stainless | 92125A | — |
| Socket head, zinc-plated alloy steel | 90128A, 95263A | In the snapshot |
| Nylon-insert locknut | 90576A zinc plated, 93625A 18-8 | In the snapshot |
| Hex nut | 90591A zinc plated, 91828A 18-8, 98676A black-oxide 18-8 | In the snapshot |
| Flat washer, 18-8 stainless | 93475A | — |

The rows the catalog uses are in `featurescripts/fasteners/vendor/mcmaster-metric.json`: every length of the socket
heads (zinc-plated and black-oxide alloy steel, class 12.9) and button heads (the same, 120,000 psi), each also in
18-8 stainless, with each supply, and the nylocks
and hex nuts in the three finishes, Class 8 where steel. Its nylocks are DIN 985, an M3 5.5 wide and 4 tall, an M4
7 wide and 5 tall (goBILDA's is 4.85); its hex nuts DIN 934, an M3 5.5 wide and 2.4 tall, an M4 7 wide and 3.2
tall (goBILDA's is 2.75). Socket heads are fully threaded to 20 mm (M3) or 25 mm (M4), then partly; where a length
comes both ways, partly threaded is the standard and fully threaded a variant, as McMaster-Carr's FRC screws are.
Its button heads are ISO 7380, an M3 5.7 across and 1.65 tall on a 2 mm hex, an M4 7.6 and 2.2 on a 2.5 mm hex, as
goBILDA's; its socket heads ISO 4762, with a knurled head. Measured from its models of 91290A119, 92095A196 and
92095A475, which agree with its tables.

## What the features need

- Metric sizes: `Size` holds threads per inch and inch diameters, and `std`'s ISO hole and thread tables
  replace the ANSI ones for M3 and M4.
- Heads: hex (REV's hex cap and goBILDA's hex head), and REV's T-slot. Low profile socket heads are left out: 25 of
  ConfigLib's 2,799 M4 inserts, a button head is lower anyway (2.2 mm to 2.8 at M4, on the same 2.5 mm hex), and
  goBILDA sells them in 10 lengths.
- Nuts: a square nut 7 mm wide, and the nylock's dome as measured above, in place of `_nylock`'s proportions.
- Washers: lock washers, if they're offered at all.
- Vendors: Generic, McMaster-Carr, goBILDA, REV, and AndyMark, in that order; FRC's `FastenerVendor` has no goBILDA.
  AndyMark's metric screws are titled `M4-0.7 x 10 mm Socket Head Cap Screw`, which `_STORE_SCREWS` has to match
  too.
- Finishes: each part's in order of price per piece, where FRC's put the standard finish first.
- Heads: flat (countersunk) in metric, for AndyMark's and McMaster-Carr's.

## STEP files

Downloaded from the vendors and measured above (goBILDA at `https://www.gobilda.com/content/step_files/<SKU>.zip`,
REV at `https://www.revrobotics.com/content/cad/<SKU>.STEP`): goBILDA 2800-0003-0008, 2800-0004-0008/0040/0045/
0050/0055/0060/0075, 2802-0004-0008/0040/0060, 2804-0004-0006/0030/0060, 2813-0004-0008, 2811-0004-0007,
2812-0004-0007, 2803-0007-0007, 2801-0004-0008, 2801-0004-0011, 2809-0004-0007, 2810-0004-0008; REV
REV-41-1713, 1359, 1123, 1360, 1124, 1715, 1125, 1907, and 1361.

From you, McMaster-Carr's 91290A119 (M3 x 14 socket head), 92095A196 (M4 x 20 button head) and 92095A475 (M3 x 50
button head).

Still needed:

| Part | Why | What would do |
| --- | --- | --- |
| REV M3 washer, REV-41-2886 | Nothing published | A STEP, or calipers: inner diameter, outer diameter, thickness |
| goBILDA split lock washer, 2809-0004-0007 | Its model is freeform, so it gives only the outside | Its section's width and thickness, if lock washers are offered |
| REV M3 hex nut, REV-41-1126 | Drawing only, so its chamfers are guessed | A STEP (optional) |
| McMaster-Carr's M3 and M4 flat heads and washers, and its nuts' models | Its pages refuse automated reads | The flat heads' (hex drive) and flat washers' metric tables, every length and supply with its price, and a STEP of a flat head, a nylock, a hex nut and a washer, from each part's CAD tab |
| REV's price per length | REV quotes one range per product | Each option's price on the hex cap screw and M3 nut pages |
| AndyMark M4 lock washer, am-1548 | Its outer diameter isn't stated | Calipers on one, or a STEP (optional) |
