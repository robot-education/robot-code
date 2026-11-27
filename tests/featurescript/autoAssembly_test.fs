FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/autoAssembly.fs", version : "");
import(path : "core/simplify.fs", version : "");
import(path : "testing.fs", version : "");

const PRODUCT = { "documentId" : "d", "elementId" : "e", "configuration" : { "Motor" : "Kraken_X60" }, "name" : "Kraken X60" } as Product;

export function testDeclarationCarriesItsProductAndVersion()
{
    const declaration = autoAssemblyDeclaration(AutoAssemblyKind.MOTOR, PRODUCT, { "pilot" : 0.75 * inch });
    expectEqual(declaration.version, AUTO_ASSEMBLY_VERSION);
    expectEqual(declaration.kind, AutoAssemblyKind.MOTOR);
    expectEqual(declaration.elementId, "e");
    expectEqual(declaration.configuration, { "Motor" : "Kraken_X60" });
    expectEqual(declaration.name, "Kraken X60");
    expectEqual(declaration.spec.pilot, 0.75 * inch);
}

export function testOnlyTrueSimplifies()
{
    expectEqual(isSimplifiedValue(true), true);
    expectEqual(isSimplifiedValue(false), false);
    expectEqual(isSimplifiedValue(1), false);
    expectEqual(isSimplifiedValue(undefined), false);
}
