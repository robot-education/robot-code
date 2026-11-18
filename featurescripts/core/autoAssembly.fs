FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");

/**
 * Declared parts, for FRCDesignApp's Auto Assembly (see `docs/auto-assembly.md`): a feature names the library part
 * that goes where it cut something, on a mate connector carrying an attribute (`AUTO_ASSEMBLY_ATTRIBUTE`). A host
 * connector is on the part the hardware attaches to; a stand-in is a simple body for a big part, with connectors of its
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
 * @field name {string} : The part's name, for its stand-in and for messages when it can't be placed.
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
 * The attribute a declaration's connectors and stand-in carry.
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
 *      @field owner {Query} : The part it's on: the host, or a stand-in.
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
 * Stand-in: a simple body where a big part goes, which Auto Assembly swaps for it.
 */
export predicate standInPredicate(definition is map)
{
    annotation { "Name" : "Stand-in", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part." }
    definition.standIn is boolean;
}

/**
 * Makes a body a stand-in: named as its product, left out of the bill of materials, translucent, carrying its
 * declaration, and with a connector of its own for each of the library part's.
 * @param definition {{
 *      @field standIn {Query} : The body.
 *      @field connectors {map} : Each connector's name to where it goes.
 *      @field declaration {map} : From `autoAssemblyDeclaration`.
 * }}
 */
export function makeStandIn(context is Context, id is Id, definition is map)
{
    setProperty(context, { "entities" : definition.standIn, "propertyType" : PropertyType.NAME, "value" : definition.declaration.name });
    setProperty(context, { "entities" : definition.standIn, "propertyType" : PropertyType.EXCLUDE_FROM_BOM, "value" : true });
    setProperty(context, { "entities" : definition.standIn, "propertyType" : PropertyType.APPEARANCE, "value" : STAND_IN_BLUE });
    setAttribute(context, { "entities" : definition.standIn, "name" : AUTO_ASSEMBLY_ATTRIBUTE, "attribute" : definition.declaration });
    for (var name, coordSystem in definition.connectors)
    {
        declarePart(context, id + name, {
                    "owner" : definition.standIn,
                    "coordSystem" : coordSystem,
                    "connector" : name,
                    "declaration" : definition.declaration
                });
    }
}
