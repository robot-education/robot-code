# Robot edit

Edits parts directly, whatever made them (Robot frame's tubes, Robot lighten's plates, imported parts). Modify hole
changes holes' diameters, and makes them simple, clearance, or tapped holes, marked as std's Hole feature marks them;
Delete pocket deletes pockets, extending the faces around them to fill them in.

| File | What it is |
| --- | --- |
| `robotEdit.fs` | The feature: its dialog, editing logic, Modify hole (`modifyHoles`, finding and measuring holes), and Delete pocket (`deletePockets`) |
| `../core/tappedHole.fs` | Marking holes with hole attributes and cosmetic threads (`setHoleAttributes`, `clearHoleAttributesAndThreads`) |
| `../released/shaft/robotShaftTables.gen.fs` | The hole tables, Robot shaft's and Robot spacer's: tapped (`tappedHoleTable`: #8 to 5/16, by thread) and clearance (`clearanceHoleTable`: #8 to 1/4, by fit) |
| `../../tests/featurescript/robotEdit_test.fs` | Tests of the editing logic, what holes are marked as (`getHoleSpec`), and the geometry checks (`isSameCylinder`, `isWithinRadius`) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot edit | feature name |
| Edit parts directly: change holes' diameters or tap them, or delete pockets. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Holes to modify | A face of each hole. |
| Pockets to delete | A face of each pocket. Every face tangent to it is deleted too. |

None are hidden. Hole diameter is set by editing logic from the hole table, and can be overridden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select holes to modify. | error | no Holes to modify | `holes` |
| Some selected faces aren't holes. | error | a selected cylinder faces away from its axis (a boss or shaft) | `holes`; the faces |
| Counterbored and countersunk holes can't be modified. | error | a hole std's Hole made counterbored or countersunk | `holes`; the hole |
| Failed to change the hole's diameter. | error | `opOffsetFace` fails (with its error display) | `holes`, `holeDiameter`; the hole |
| Select pockets to delete. | error | no Pockets to delete | `pockets` |
| Failed to delete pocket. | error | `opDeleteFace` fails (with its error display) | `pockets`; the faces it was to delete |

## How it works

### Execution order

1. **Editing logic** (`robotEditEditLogic`, on every change): for a clearance or tapped hole, when Hole type or the
   table's diameter changes (and when the feature is created), sets Hole diameter from the hole table: a clearance
   hole's diameter for its fit, or a tapped hole's tap drill (`getTableHoleDiameter`). A typed diameter stays until
   then. It only reads the definition.
2. **Body**: Modify hole or Delete pocket, by Edit type.

### Modify hole (`modifyHoles`)

1. **Find the holes**: each selected face must face its axis (`isHoleFace`: its normal, halfway across it, points
   toward the axis). Each one's hole is the faces of the same cylinder tangent to it (`getHoleFaces`, with
   `isSameCylinder`: same radius and axis), since a hole which crosses another may be split; faces of a hole already
   found are skipped, so picking two faces of one hole modifies it once. Holes with std hole attributes of a
   counterbore or countersink are an error, since only their through face would be changed.
2. **Offset**: each hole whose radius isn't Hole diameter's has its faces offset (`opOffsetFace`) by its radius less
   the new one: its faces face its axis, so offsetting them out of the part shrinks the hole. Each hole's faces are
   tracked through it (`startTracking`, with the faces themselves), keeping cylinders.
3. **Mark**: each hole's hole attributes and cosmetic threads are removed (`clearHoleAttributesAndThreads`), then it's
   measured (`measureHole`) and marked (`setHoleAttributes`) with what `getHoleSpec` gives: Hole diameter, and a
   clearance hole's size, or a tapped hole's size, pitch, and major diameter. Every modified hole gets attributes,
   including simple ones which had none, so hole tables and callouts list them. A tapped hole is tapped as deep as it
   goes, with a cosmetic thread.

`measureHole` finds where a hole starts, and whether it's blind: a blind hole's bottom is a face beside it within its
circle (`isWithinRadius`, in the cylinder's coordinates: a flat bottom, or a drill point's cone, whose angle becomes
the tip angle), and the hole starts at the other end, its depth the length of its sides. Faces where a hole starts and
a through hole ends go around it, so they're outside its circle.

### Delete pocket (`deletePockets`)

Deletes every face tangent to a selected face (`qTangentConnectedFaces`) with one `opDeleteFace`, which heals the parts
by extending the faces around them (no capping, and the fillets beside them stay).

### `try`s

- Around each hole's `opOffsetFace`: on failure, copies its status and error display, and throws "Failed to change the
  hole's diameter.", showing the hole (a failed offset changes nothing).
- Around `opDeleteFace`: on failure, copies its status and error display, and throws "Failed to delete pocket.",
  showing the faces it was given.

Neither is a fallback.

## Issues found

- Not yet run in Onshape: whether `opOffsetFace`'s sign shrinks holes as expected, whether `startTracking` finds an
  offset hole's faces, and how drawings show the attributes it writes are untested.
- Delete pocket deletes only the faces tangent to what's selected: a blind pocket whose floor meets its walls at a
  sharp edge needs its floor selected too, or healing may fail.
- The hole tables are inch only (#8 to 5/16), as Robot shaft's are.
