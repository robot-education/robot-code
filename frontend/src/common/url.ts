import {
    DocumentPath,
    InstancePath,
    ElementPath,
    isInstancePath,
    isElementPath,
    isWorkspacePath,
    WorkspacePath
} from "../api/path";

export function makeUrl(path: DocumentPath): string;
export function makeUrl(path: WorkspacePath): string;
export function makeUrl(path: InstancePath): string;
export function makeUrl(path: ElementPath): string;
// Impelmentation handler
export function makeUrl(path: DocumentPath): string {
    let url = `https://cad.onshape.com/documents/${path.documentId}`;
    // Match most specific match first
    if (isInstancePath(path)) {
        url += `/${path.instanceType}/${path.instanceId}`;
    } else if (isWorkspacePath(path)) {
        url += `/w/${path.instanceId}`;
    }
    if (isElementPath(path)) {
        url += `/e/${path.elementId}`;
    }
    return url;
}

export interface Configuration {
    configuration?: string;
}

/**
 * A utility which parses Onshape urls into an ElementPath.
 * Returns `undefined` if the url could not be parsed successfully.
 */
export function parseUrl(
    urlString: string
): (ElementPath & Configuration) | undefined {
    try {
        // Example pathname: /documents/769b556baf61d32b18813fd0/w/e6d6c2b3a472b97a7e352949/e/8a0c13d3b2b68a99502dc436
        const url = new URL(urlString);
        const parts = url.pathname.split("/");
        const configuration =
            url.searchParams.get("configuration") ?? undefined;
        return {
            documentId: parts[2],
            instanceId: parts[4],
            instanceType: parts[3],
            elementId: parts[6],
            configuration
        };
    } catch {
        return undefined;
    }
}

/**
 * Opens the given url in a new tab.
 */
export function openUrlInNewTab(url: string) {
    window.open(url);
}
