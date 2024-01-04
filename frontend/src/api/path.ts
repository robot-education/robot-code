export interface DocumentBasePath extends Record<string, string> {
    documentId: string;
}

export interface DocumentPath extends DocumentBasePath {
    workspaceId: string;
    /**
     * One of "w", "v", or "m". Defaults to "w".
     */
    workspaceOrVersion: string;
}

export interface Document extends DocumentPath {
    name: string;
}

export interface ElementPath extends DocumentPath {
    elementId: string;
}

export function toDocumentBase(path: DocumentPath): Record<string, string> {
    return {
        documentId: path.documentId
    };
}

/**
 * Trims an ElementPath to just the DocumentPath portion.
 */
export function toDocumentPath(path: ElementPath): Record<string, string> {
    return {
        ...toDocumentBase(path),
        workspaceOrVersion: path.workspaceOrVersion ?? "w",
        workspaceId: path.workspaceId
    };
}

/**
 * Returns the path to the current window.
 */
export function getCurrentPath(queryParams: URLSearchParams): ElementPath {
    return {
        documentId: queryParams.get("documentId") ?? "",
        workspaceId: queryParams.get("workspaceId") ?? "",
        workspaceOrVersion: "w",
        elementId: queryParams.get("elementId") ?? ""
    };
}
