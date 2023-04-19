FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

annotation { "Feature Type Name" : "Assembly mate" }
export const assemblyMate = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Location", "Filter" : BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
        definition.location is Query;

        annotation { "Name" : "Part", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
        definition.part is Query;
    }
    {
        const coordSystem = evMateConnector(context, { "mateConnector" : definition.location });
        // const partStudioData = {
        //     "build" : Tool::build,
        //     "configuration" : {}
        // } as PartStudioData;

        opAssemblyMate(context, id + "assemblyMate", {
                    "coordSystem" : coordSystem,
                    "part" : definition.part,
                    "url" : "https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f/e/331b3d88c2eb650982e9dfda"
                });
    });
    
export const ASSEMBLY_ATTRIBUTE = "assemblyAttribute";

/**
 * Add a mate connector to `part` which defines an entity to be assembled to it later.
 * The assembled entity is specified using API ids.
 *
 * @param id : @autocomplete `id + "assemblyMate"`
 * @param definition {{
 *      @field coordSystem {CoordSystem} :
 *      @field part {Query} :   
 *              The part to attatch to.
 *      @field configuration {map} : @optional
 *      @field url {map} : 
 *              The `url` of the part studio containing the target part.
 *              The target part should have a mate connector representing its location in space.
 *      @field replicate {boolean} : 
 *              Whether to enable using a replicate to place duplicate entities instead of using individual mates.
 *      @field replicateReference {Query} : 
 *              A `Query` for a reference entity to be used when replicating.
 * }}
 */
export const opAssemblyMate = function(context is Context, id is Id, definition is map)
    {
        const mateId = id + "mate";
        opMateConnector(context, mateId, {
                    "coordSystem" : definition.coordSystem,
                    "owner" : definition.part
                });
        setAttribute(context, {
                    "entities" : qCreatedBy(mateId, EntityType.BODY),
                    "name" : ASSEMBLY_ATTRIBUTE,
                    "attribute" : definition.url
                });
    };
