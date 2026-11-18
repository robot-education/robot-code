FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");

/**
 * Declared parts, for FRCDesignApp's Auto Assembly (see `docs/auto-assembly.md`): a feature names the library part
 * that goes where it cut something, on a mate connector carrying an attribute (`AUTO_ASSEMBLY_ATTRIBUTE`). A host
 * connector is on the part the hardware attaches to; an Auto Assembly body is a simple body for a big part, with connectors of its
 * own, named and placed as the library part's are. A feature declaring parts says `"Auto Assembly" : true` in its
 * annotation, which `fs check` requires.
 */
export const AUTO_ASSEMBLY_ATTRIBUTE = "frcAutoAssembly";

/** The attribute's format: a field changing meaning bumps it, and FRCDesignApp reads every version it has read. */
export const AUTO_ASSEMBLY_VERSION = 1;

/**
 * What kind of part a declaration is, which decides its connectors' convention and the `spec` FRCDesignApp checks it
 * against.
 */
export enum AutoAssemblyKind
{
    SCREW,
    NUT,
    WASHER,
    SHAFT_END_SCREW,
    RETAINING_RING,
    E_CLIP,
    GEAR,
    MOTOR,
    GEARBOX,
    BEARING,
    BUSHING,
    BEARING_HAT
}

/**
 * A library part: its Part Studio by document and tab, with no version (FRCDesignApp inserts from the version its
 * library loaded last), and its configuration, as FRCDesignApp stores it.
 */
export type Product typecheck canBeProduct;

/**
 * @field documentId {string} : FRCDesignLib's document.
 * @field elementId {string} : The Part Studio or assembly's tab.
 * @field configuration {map} : Each configuration parameter's id to its option's id (or a quantity's value, as a
 *          string); empty for the defaults.
 * @field name {string} : The part's name, for its Auto Assembly body and for messages when it can't be placed.
 */
export predicate canBeProduct(value)
{
    value is map;
    value.documentId is string;
    value.elementId is string;
    value.configuration is map;
    value.name is string;
}

/**
 * The attribute a declaration's connectors and Auto Assembly body carry.
 * @param spec : What the feature cut for the part, by kind, for FRCDesignApp to check the product fits: lengths as
 *          lengths.
 */
export function autoAssemblyDeclaration(kind is AutoAssemblyKind, product is Product, spec is map) returns map
{
    return {
            "version" : AUTO_ASSEMBLY_VERSION,
            "kind" : kind,
            "documentId" : product.documentId,
            "elementId" : product.elementId,
            "configuration" : product.configuration,
            "name" : product.name,
            "spec" : spec
        };
}

/**
 * Makes a mate connector carrying a declaration: Auto Assembly fastens the part's connector of the same name to it.
 * @param id : Stable and named for what it is (`id + "face"`), so a mate to the connector survives the feature being
 *          edited.
 * @param definition {{
 *      @field owner {Query} : The part it's on: the host, or an Auto Assembly body.
 *      @field coordSystem {CoordSystem} : Where the part's connector goes, by its kind's convention.
 *      @field connector {string} : The library part's connector it takes, by name (`face`, `seat`, `bottom`).
 *      @field declaration {map} : From `autoAssemblyDeclaration`.
 * }}
 */
export function declarePart(context is Context, id is Id, definition is map)
{
    opMateConnector(context, id, { "coordSystem" : definition.coordSystem, "owner" : definition.owner });
    setAttribute(context, {
                "entities" : qCreatedBy(id, EntityType.BODY),
                "name" : AUTO_ASSEMBLY_ATTRIBUTE,
                "attribute" : mergeMaps(definition.declaration, { "connector" : definition.connector })
            });
}

/**
 * Auto Assembly body: a simple body where a big part goes, which Auto Assembly swaps for it.
 */
export predicate autoAssemblyBodyPredicate(definition is map)
{
    annotation { "Name" : "Auto Assembly body", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part." }
    definition.autoAssemblyBody is boolean;
}

/**
 * Makes a body an Auto Assembly body: named as its product, left out of the bill of materials, translucent, carrying its
 * declaration, and with a connector of its own for each of the library part's. Without a declaration (a part not yet
 * in the library), it's only named and colored.
 * @param definition {{
 *      @field body {Query} : The body.
 *      @field connectors {map} : Each connector's name to where it goes.
 *      @field declaration {map} : @optional From `autoAssemblyDeclaration`.
 *      @field name {string} : @requiredif {`declaration` isn't given.} Its name.
 * }}
 */
export function makeAutoAssemblyBody(context is Context, id is Id, definition is map)
{
    const declaration = definition.declaration;
    setProperty(context, { "entities" : definition.body, "propertyType" : PropertyType.NAME, "value" : declaration == undefined ? definition.name : declaration.name });
    setProperty(context, { "entities" : definition.body, "propertyType" : PropertyType.EXCLUDE_FROM_BOM, "value" : true });
    setProperty(context, { "entities" : definition.body, "propertyType" : PropertyType.APPEARANCE, "value" : AUTO_ASSEMBLY_BLUE });
    if (declaration == undefined)
    {
        return;
    }
    setAttribute(context, { "entities" : definition.body, "name" : AUTO_ASSEMBLY_ATTRIBUTE, "attribute" : declaration });
    for (var name, coordSystem in definition.connectors)
    {
        declarePart(context, id + name, {
                    "owner" : definition.body,
                    "coordSystem" : coordSystem,
                    "connector" : name,
                    "declaration" : declaration
                });
    }
}

/**
 * The part a face sits on, for its host connector: of `scope`'s parts, the one reaching nearest `plane` from behind it
 * (against its normal). Found by where they are, as nothing in their history says which one the face is on.
 */
export function partBehind(context is Context, scope is Query, plane is Plane) returns Query
{
    var nearest;
    var reach;
    for (var part in evaluateQuery(context, scope))
    {
        const top = evBox3d(context, { "topology" : part, "cSys" : coordSystem(plane), "tight" : true }).maxCorner[2];
        if (reach == undefined || top > reach)
        {
            nearest = part;
            reach = top;
        }
    }
    return nearest;
}
