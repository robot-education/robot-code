import {
    DocumentBasePath,
    DocumentPath,
    ElementPath,
    isDocumentPath,
    isElementPath
} from "../api/path";

export function makeUrl(path: DocumentBasePath): string;
export function makeUrl(path: DocumentPath): string;
export function makeUrl(path: ElementPath): string;
export function makeUrl(
    path: DocumentBasePath | DocumentPath | ElementPath
): string {
    let url = `https://cad.onshape.com/documents/${path.documentId}`;
    if (isDocumentPath(path)) {
        url += `/${path.workspaceOrVersion ?? "w"}/${path.workspaceId}`;
    }
    if (isElementPath(path)) {
        url += `/e/${path.elementId}`;
    }
    return url;
}

/**
 * A utility which parses Onshape urls into an ElementPath.
 * Returns `null` if the url could not be parsed successfully.
 */
export function parseUrl(url: string): ElementPath | null {
    try {
        // Example pathname: /documents/769b556baf61d32b18813fd0/w/e6d6c2b3a472b97a7e352949/e/8a0c13d3b2b68a99502dc436
        const pathname = new URL(url).pathname;
        const parts = pathname.split("/");
        return {
            documentId: parts[2],
            workspaceId: parts[4],
            workspaceOrVersion: parts[3],
            elementId: parts[6]
        };
    } catch {
        return null;
    }
}

/**
 * Opens the given url in a new tab.
 */
export function openUrlInNewTab(url: string) {
    window.open(url);
}