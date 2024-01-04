export enum DocumentLinkType {
    PARENT,
    CHILD
}

export interface DocumentLinkProps {
    linkType: DocumentLinkType;
}
