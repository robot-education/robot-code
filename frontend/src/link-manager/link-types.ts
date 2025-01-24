import { WorkspacePath } from "../api/path";

export enum LinkType {
    PARENTS = "parents",
    CHILDREN = "children"
}

export interface LinkTypeProps {
    linkType: LinkType;
}
export interface LinkedDocument extends WorkspacePath {
    isOpenable: boolean;
}

export interface OpenableLinkedDocument extends LinkedDocument {
    name: string;
    workspaceName: string;
}

export function isOpenableDocument(
    linkedDocument: LinkedDocument
): linkedDocument is OpenableLinkedDocument {
    return linkedDocument.isOpenable;
}
