FeatureScript 2960;
/**
 * Declared parts: a feature names the FRCDesignLib part that goes where it built something, and FRCDesign's Auto
 * Assembly puts that part in an assembly (see docs/auto-assembly.md).
 *
 * A feature declaring parts says so in its annotation (`"Auto Assembly" : true`, which `fs check` keeps honest and
 * `fs release` writes into the manifest), and builds a stand-in for each (`addStandIn`): a simple solid of the part's
 * envelope, named as it, left out of the bill of materials, with mate connectors named and placed as the library part's
 * are, carrying the declaration (`declarePart`). In an assembly, a stand-in is mated like any part, and Auto Assembly
 * swaps it for the library part on those same mates.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * What a declaration declares. Stored in documents' attributes, which FRCDesign reads: never rename or remove one.
 */
export enum AutoAssemblyKind
{
    PULLEY,
    SPROCKET
}

/** The declarations' attribute: on a stand-in, and each of its mate connectors. */
export const AUTO_ASSEMBLY_ATTRIBUTE = "frcAutoAssembly";

/** The format of the declaration (`declarePart`): a new optional field leaves it, a field changing meaning bumps it. */
export const AUTO_ASSEMBLY_VERSION = 1;

/** Stand-ins' look: translucent, so none is mistaken for a part to make. */
export const STAND_IN_APPEARANCE = color(140 / 255, 160 / 255, 200 / 255, 0.5);

/**
 * A library part, as a product table (like `PULLEY_PRODUCTS`) gives it.
 * @type {{
 *      @field name {string} : As FRCDesign shows it, like `15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2" Hex Bore)`.
 *      @field partNumber {string} :
 *      @field documentId {string} : Its Part Studio's document and tab, with no version: FRCDesign inserts the version
 *              its library loaded last.
 *      @field elementId {string} :
 *      @field configuration {map} : Its configuration, as FRCDesign stores a selection (parameter id to option id).
 * }}
 */
export type Product typecheck canBeProduct;

export predicate canBeProduct(value)
{
    value is map;
    value.name is string;
    value.partNumber is string;
    value.documentId is string;
    value.elementId is string;
    value.configuration is map;
}

/**
 * Declares `product` on `entities` (a stand-in and its mate connectors), as a `kind`, with `spec`, what the feature
 * built it for (a pulley's belt type and teeth, say), so FRCDesign can say a product doesn't fit rather than insert it.
 */
export function declarePart(context is Context, entities is Query, kind is AutoAssemblyKind, product is Product, spec is map)
{
    setAttribute(context, {
                "entities" : entities,
                "name" : AUTO_ASSEMBLY_ATTRIBUTE,
                "attribute" : {
                    "version" : AUTO_ASSEMBLY_VERSION,
                    "kind" : kind,
                    "documentId" : product.documentId,
                    "elementId" : product.elementId,
                    "configuration" : product.configuration,
                    "spec" : spec,
                    "name" : product.name,
                    "partNumber" : product.partNumber
                }
            });
}

/**
 * Makes a round stand-in, `id + "extrude"`: a disc of `radius`, `width` thick, centered on `plane`, with a hole of
 * `boreDiameter` if there is one; named, numbered, and colored as `product`, out of the bill of materials, with mate
 * connectors `bottom` and `top` on its axis, on each face, Z out of it and X along the plane's (`id + "bottom"` and
 * `id + "top"`, as a gear's, pulley's or sprocket's library part has them), and declares it. Returns the stand-in, and
 * its mate connectors.
 *
 * @returns {{
 *      @field body {Query} :
 *      @field connectors {Query} :
 * }}
 */
export function addStandIn(context is Context, id is Id, plane is Plane, radius is ValueWithUnits, width is ValueWithUnits, boreDiameter,
    kind is AutoAssemblyKind, product is Product, spec is map) returns map
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    skCircle(sketch, "outside", { "center" : vector(0, 0) * meter, "radius" : radius });
    if (boreDiameter != undefined)
    {
        skCircle(sketch, "bore", { "center" : vector(0, 0) * meter, "radius" : boreDiameter / 2 });
    }
    skSolve(sketch);
    // Its face, not the bore's
    const face = qSketchRegion(id + "sketch", true);
    opExtrude(context, id + "extrude", {
                "entities" : face,
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : width / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : width / 2
            });
    opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(id + "sketch", EntityType.BODY) });
    const body = qCreatedBy(id + "extrude", EntityType.BODY);

    for (var side in ["bottom", "top"])
    {
        const sign = side == "top" ? 1 : -1;
        opMateConnector(context, id + side, {
                    "coordSystem" : coordSystem(plane.origin + plane.normal * sign * width / 2, plane.x, plane.normal * sign),
                    "owner" : body
                });
    }
    const connectors = qUnion(qCreatedBy(id + "bottom", EntityType.BODY), qCreatedBy(id + "top", EntityType.BODY));

    for (var property in [
                [PropertyType.NAME, product.name],
                [PropertyType.PART_NUMBER, product.partNumber],
                [PropertyType.EXCLUDE_FROM_BOM, true],
                [PropertyType.APPEARANCE, STAND_IN_APPEARANCE]
            ])
    {
        setProperty(context, { "entities" : body, "propertyType" : property[0], "value" : property[1] });
    }
    declarePart(context, qUnion(body, connectors), kind, product, spec);
    return { "body" : body, "connectors" : connectors };
}

/**
 * The product of `products` (a product table's, for one vendor and bore) `matches` takes, or the first of those
 * `matches` takes, ordered by `order` (a function of a product, smallest first), or undefined.
 */
export function findProduct(products is array, matches is function, order)
{
    var found = undefined;
    for (var product in products)
    {
        if (matches(product) && (found == undefined || (order != undefined && order(product) < order(found))))
        {
            found = product;
        }
    }
    return found;
}
