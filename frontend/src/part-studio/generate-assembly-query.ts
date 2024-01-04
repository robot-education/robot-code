import { get, post } from "../api/api";
import {
    selectDocumentPath,
    selectElementPath
} from "../app/onshape-params-slice";
import { store } from "../app/store";

export async function generateAssemblyQuery(): Promise<string> {
    return get("/default-name", {
        ...selectDocumentPath(store.getState()),
        elementType: "ASSEMBLY"
    }).then((result) => result["name"]);
}

export interface GenerateAssemblyArgs {
    assemblyName: string;
}

export interface GenerateAssemblyResult {
    assemblyUrl: string;
}

export async function generateAssemblyMutation(
    args: GenerateAssemblyArgs,
    controller: AbortController
): Promise<GenerateAssemblyResult> {
    const elementQuery = selectElementPath(store.getState());
    const result = await post("/generate-assembly", {
        signal: controller.signal,
        query: elementQuery,
        body: { name: args.assemblyName }
    });
    if (!result) {
        throw new Error("Request failed.");
    }

    const assemblyPath = Object.assign({}, elementQuery);
    assemblyPath.elementId = result.elementId;
    const assemblyUrl = `https://cad.onshape.com/documents/${assemblyPath.documentId}/w/${assemblyPath.workspaceId}/e/${assemblyPath.elementId}`;
    // if (data.autoAssemble) {
    // const result = await post("/auto-assembly", assemblyPath.elementObject());
    // if (result == null) { return false; }
    // }
    return { assemblyUrl };
}
