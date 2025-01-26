import {
    QueryClient,
    QueryFunction,
    queryOptions
} from "@tanstack/react-query";
import { WorkspacePath } from "../api/path";
import { currentInstanceApiPath } from "../app/onshape-params";
import { LinkedDocument, LinkType } from "../link-manager/link-types";
import { get } from "../api/api";
import { ReportedError } from "../common/errors";

export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            retry: (count, error) => {
                if (count >= 4) {
                    return false;
                }
                if (error instanceof ReportedError) {
                    return false;
                }
                return true;
            }
        }
    }
});

const linkedDocumentsQueryFn: QueryFunction<LinkedDocument[]> = async (
    context
) => {
    const recursive = context.meta?.recursive as boolean;
    return await get(
        `/linked-documents/${context.meta?.linkType}` +
            currentInstanceApiPath(),
        { recursive: recursive.toString() }
    );
};

function linkedDocumentsKey(linkType: LinkType, recursive: boolean = false) {
    if (recursive) {
        return ["linked-documents", linkType, recursive];
    }
    return ["linked-documents", linkType];
}

export function getLinkedDocumentsOptions(
    linkType: LinkType,
    recursive: boolean = false
) {
    return queryOptions({
        queryKey: linkedDocumentsKey(linkType, recursive),
        queryFn: linkedDocumentsQueryFn,
        meta: { linkType, recursive }
    });
}

export function handleDocumentAdded(
    linkType: LinkType,
    document: LinkedDocument
) {
    queryClient.setQueryData<LinkedDocument[]>(
        linkedDocumentsKey(linkType),
        (documents) => (documents ?? []).concat(document)
    );
}

export function handleDocumentRemoved(
    linkType: LinkType,
    workspacePath: WorkspacePath
) {
    queryClient.setQueryData<LinkedDocument[]>(
        linkedDocumentsKey(linkType),
        (documents) =>
            (documents ?? []).filter(
                (document) =>
                    document.documentId !== workspacePath.documentId &&
                    document.instanceId !== workspacePath.instanceId
            )
    );
}
