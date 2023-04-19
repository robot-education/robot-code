FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

/**
 * A function called on parts which fetches the parts and returns the url links pointing to the specified parts so they can be
 * added to the assembly.
 * The mate connector transform is also returned; add this to the part's transform matrix to get the final result.
 * The part being assembled is assumed to be at the origin of the part studio. If an additional transform is needed,
 * it should be computed by parsing the specified part?
 *
 * We could execute a script in the target part studio to get the first mate connector
 * hmmm, yeah me likey that
 *
 * We can't know anything about the target part until after deriving it
 * Some things care about orientation - it'll be on MKCad to ensure the mate connectors point in the right direction I guess
 * Users can also rotate afterwards if desired
 * And custom wrappers should have tooling to enable performant previewing of components
 */
// function(context is Context, queries is map) returns map
//     {
//         const ASSEMBLY_ATTRIBUTE = "assemblyAttribute";
//         const mateConnectors = qEverything(EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR)->qHasAttribute(ASSEMBLY_ATTRIBUTE);
//         var result = '[';
//         for (var mateConnector in evaluateQuery(context, mateConnectors))
//         {
//             const location = evMateConnector(context, { "mateConnector" : mateConnector })->transform();
//             result ~= '{ "transform" : [], ';
//             const url = getAttribute(context, {
//                         "entity" : mateConnector,
//                         "name" : ASSEMBLY_ATTRIBUTE
//                     });
//             const parsed = match(url, ".*/documents/(\\w+)/w/(\\w+)/e/(\\w+)");
//             result ~= '"entity_id" : "' ~ parsed.captures[1];
//             result ~= '", "middle_id" : "' ~ parsed.captures[2];
//             result ~= '", "element_id" : "' ~ parsed.captures[3];
//             result ~= '" }';
//         }
//         print(result ~ ']');
//     }

/**
 * Returns the transform mapping the first mate connector to the origin.
 * Maybe we should just complicate the above script a bit - it's cached anyways, and we're going to build the part studio nevertheless
 */
// export const targetScript =

// function(context is Context, targetPart is Query) returns map
//     {
//         const mateConnector = qMateConnectorsOfParts(targetPart)->qNthElement(0);
//         if (!isQueryEmpty(context, mateConnector))
//         {
//             return evMateConnector(context, { "mateConnector" : mateConnector })->transform();
//         }
//         return identityTransform();
//     };
