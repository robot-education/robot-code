FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/autoAssembly.fs", version : "");
import(path : "testing.fs", version : "");

const PRODUCT = { "documentId" : "d", "elementId" : "e", "configuration" : { "Size" : "IN_1_2" }, "name" : "1/2\" Hex Bushing" } as Product;

export function testDeclarationCarriesItsProductAndVersion()
{
    const declaration = autoAssemblyDeclaration(AutoAssemblyKind.BUSHING, PRODUCT, { "outerDiameter" : 0.75 * inch });
    expectEqual(declaration.version, AUTO_ASSEMBLY_VERSION);
    expectEqual(declaration.kind, AutoAssemblyKind.BUSHING);
    expectEqual(declaration.elementId, "e");
    expectEqual(declaration.configuration, { "Size" : "IN_1_2" });
    expectEqual(declaration.name, "1/2\" Hex Bushing");
    expectEqual(declaration.spec.outerDiameter, 0.75 * inch);
}
