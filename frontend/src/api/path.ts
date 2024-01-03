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

export function toQuery(
    path: ElementPath,
    pathType: PathType = PathType.ELEMENT
): Record<string, string> {
    switch (pathType) {
        case PathType.DOCUMENT_BASE:
            return toDocumentBaseQuery(path);
        case PathType.DOCUMENT:
            return toDocumentQuery(path);
        case PathType.ELEMENT:
            return toElementQuery(path);
    }
}

export function toDocumentBaseQuery(
    path: DocumentBasePath
): Record<string, string> {
    return {
        documentId: path.documentId
    };
}

export function toDocumentQuery(path: DocumentPath): Record<string, string> {
    return {
        ...toDocumentBaseQuery(path),
        workspaceOrVersion: path.workspaceOrVersion ?? "w",
        workspaceId: path.workspaceId
    };
}

export function toElementQuery(path: ElementPath): Record<string, string> {
    return {
        ...toDocumentQuery(path),
        elementId: path.elementId
    };
}

/**
 * Returns the path to the current window.
 */
export function getCurrentPath(queryParams: URLSearchParams): ElementPath {
    return {
        documentId: queryParams.get("documentId") ?? "",
        workspaceOrVersion: queryParams.get("workspaceOrVersion") ?? undefined,
        workspaceId: queryParams.get("workspaceId") ?? "",
        elementId: queryParams.get("elementId") ?? ""
    };
}

export enum PathType {
    DOCUMENT_BASE,
    DOCUMENT,
    ELEMENT
}
