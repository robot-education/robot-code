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
 * Opens the given url in a new tab.
 */
export function openUrlInNewTab(url: string) {
    window.open(url);
}

/**
 * Changes the current window to the given url.
 */
export function openUrl(url: string) {
    window.parent.location.assign(url);
}
