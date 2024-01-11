import { get, post } from "../api/api";
import { toApiElementPath } from "../api/path";
import {
    selectApiDocumentPath,
    selectOnshapeParams
} from "../app/onshape-params-slice";
import { store } from "../app/store";
import { makeUrl } from "../common/url";

export async function generateAssemblyQuery(): Promise<string> {
    return get(
        "/default-name/assembly" + selectApiDocumentPath(store.getState())
    ).then((result) => result.name);
}

export interface GenerateAssemblyArgs {
    assemblyName: string;
}

export interface GenerateAssemblyResult {
    assemblyUrl: string;
}

export async function generateAssemblyMutationFn(
    args: GenerateAssemblyArgs
): Promise<GenerateAssemblyResult> {
    const elementPath = selectOnshapeParams(store.getState());
    const assemblyPath = await post(
        "/generate-assembly" + toApiElementPath(elementPath),
        {
            body: { name: args.assemblyName }
        }
    );
    const assemblyUrl = makeUrl(assemblyPath);
    // if (data.autoAssemble) {
    // await post("/auto-assembly", assemblyPath.elementObject());
    // }
    return { assemblyUrl };
}
