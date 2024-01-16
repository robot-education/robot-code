export interface DocumentPath {
    documentId: string;
}

export interface WorkspacePath extends DocumentPath {
    instanceId: string;
}

export interface InstancePath extends WorkspacePath {
    /**
     * One of "w", "v", or "m". Defaults to "w".
     */
    instanceType?: string;
}

export interface Workspace extends WorkspacePath {
    name: string;
}

export interface ElementPath extends InstancePath {
    elementId: string;
}

export function isWorkspacePath(path: DocumentPath): path is WorkspacePath {
    return (<WorkspacePath>path).instanceId !== undefined;
}

export function isInstancePath(path: DocumentPath): path is InstancePath {
    return (<InstancePath>path).instanceId !== undefined;
}

export function isElementPath(path: DocumentPath): path is ElementPath {
    return isWorkspacePath(path) && (<ElementPath>path).elementId !== undefined;
}

export function toDocumentApiPath(path: DocumentPath): string {
    return `/d/${path.documentId}`;
}

/**
 * Trims an ElementPath to just the DocumentPath portion.
 */
export function toInstanceApiPath(path: InstancePath): string {
    return (
        toDocumentApiPath(path) +
        `/${path.instanceType ?? "w"}/${path.instanceId}`
    );
}

/**
 * Trims an ElementPath to just the DocumentPath portion.
 */
export function toElementApiPath(path: ElementPath): string {
    return toInstanceApiPath(path) + `/e/${path.elementId}`;
}
