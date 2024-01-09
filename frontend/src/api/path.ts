export interface DocumentBasePath {
    documentId: string;
}

export interface DocumentPath extends DocumentBasePath {
    workspaceId: string;
    /**
     * One of "w", "v", or "m". Defaults to "w".
     */
    workspaceOrVersion?: string;
}

export interface Document extends DocumentPath {
    name: string;
}

export interface ElementPath extends DocumentPath {
    elementId: string;
}

export function isDocumentPath(path: DocumentBasePath): path is DocumentPath {
    return (<DocumentPath>path).workspaceId !== undefined;
}

export function isElementPath(path: DocumentBasePath): path is ElementPath {
    return isDocumentPath(path) && (<ElementPath>path).elementId !== undefined;
}

export function toApiDocumentBase(path: DocumentBasePath): string {
    return `/d/${path.documentId}`;
}

/**
 * Trims an ElementPath to just the DocumentPath portion.
 */
export function toApiDocumentPath(path: DocumentPath): string {
    return (
        toApiDocumentBase(path) +
        `/${path.workspaceOrVersion ?? "w"}/${path.workspaceId}`
    );
}

/**
 * Trims an ElementPath to just the DocumentPath portion.
 */
export function toApiElementPath(path: ElementPath): string {
    return toApiDocumentPath(path) + `/e/${path.elementId}`;
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
