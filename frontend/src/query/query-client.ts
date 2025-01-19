import { QueryClient, QueryFunction } from "@tanstack/react-query";
import { Workspace, WorkspacePath } from "../api/path";
import { currentInstanceApiPath } from "../app/onshape-params";
import { LinkType } from "../link-manager/link-types";
import { get } from "../api/api";

export const queryClient = new QueryClient();

const linkedDocumentsQueryFn: QueryFunction = async (
    context
): Promise<Workspace[]> => {
    return await get(
        `/linked-documents/${context.meta?.linkType}` + currentInstanceApiPath()
    );
};

export function linkedDocumentsKey(linkType: LinkType) {
    return ["linked-documents", linkType];
}

export const linkedParentDocumentsKey = linkedDocumentsKey(LinkType.PARENTS);
export const linkedChildDocumentsKey = linkedDocumentsKey(LinkType.CHILDREN);

queryClient.setQueryDefaults(linkedParentDocumentsKey, {
    queryFn: linkedDocumentsQueryFn,
    meta: { linkType: LinkType.PARENTS }
});

queryClient.setQueryDefaults(linkedChildDocumentsKey, {
    queryFn: linkedDocumentsQueryFn,
    meta: { linkType: LinkType.CHILDREN }
});

export function handleDocumentAdded(linkType: LinkType, document: Workspace) {
    queryClient.setQueryData<Workspace[]>(
        linkedDocumentsKey(linkType),
        (documents) => (documents ?? []).concat(document)
    );
}

export function handleDocumentRemoved(
    linkType: LinkType,
    workspacePath: WorkspacePath
) {
    queryClient.setQueryData<Workspace[]>(
        linkedDocumentsKey(linkType),
        (documents) =>
            (documents ?? []).filter(
                (document) =>
                    document.documentId !== workspacePath.documentId &&
                    document.instanceId !== workspacePath.instanceId
            )
    );
}
