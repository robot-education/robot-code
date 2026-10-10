# Quick rib

A prototype: sketches ribs (lines) between circles' centers, sketch points, and mate connectors' origins, for Robot
lighten's Ribs to use. Each group of Ribs has a pattern: a hub and its spokes, a chain (in the order chosen, closed or
not), or a line from one point to another.

| File | What it is |
| --- | --- |
| `quickRib.fs` | The feature |
| `../../tests/featurescript/quickRib_test.fs` | Tests of the patterns' lines (`ribSegments`, `uniqueSegments`) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Quick rib | feature name |
| Sketch ribs between circles and points: hub and spoke, chain, or point to point. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Sketch plane | Where the ribs are sketched. Without one, the first circle's plane. |

None are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add ribs to sketch. | error | Ribs is empty | `ribs` |
| Select a hub. / Select spokes. | error | a hub and spoke group without its hub or spokes | that parameter of the group |
| Select circles or points to chain. | error | a chain group with nothing chosen | its Chain |
| Select where the rib starts. / Select where the rib ends. | error | a point to point group without From or To | that parameter of the group |
| The ribs chosen have no length. | error | every line is from a point to itself | `ribs` |
| Select a sketch plane: none of the ribs' circles give one. | error | no Sketch plane, and only points and mate connectors chosen | `sketchPlane` |

## How it works

### Execution order

1. **Body**:
   1. Each group's points (`ribPoints`): each entity chosen, in order, as a point (`centerOf`: a mate connector's
      origin, a vertex's point, or a circle's or arc's center).
   2. Its lines (`ribSegments`): hub to each spoke; each point of the chain to the next (and the last to the first, if
      it's closed and has more than two); or From to To. Repeats (either way round) and lines with no length are
      dropped (`uniqueSegments`).
   3. The plane (`getSketchPlane`): Sketch plane, or the plane of the first circle or arc chosen.
   4. A sketch on it with a line for each, the points projected onto it (`worldToPlane`).

### Error handling

Every error is a check before the sketch is made; there are no `try`s.

## Issues found

- A prototype: untested in Onshape.
- Lines run center to center, through the circles: fine for Robot lighten, which leaves holes' walls around them, but
  not tangent to them.
- Chains follow the order things were chosen in, which Onshape's selections keep, but a chain can't be reordered
  without choosing again.
- The icon is the generic robot icon.
