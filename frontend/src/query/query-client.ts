import { QueryClient, QueryFunction } from "@tanstack/react-query";
import { Workspace } from "../api/path";
import { currentInstanceApiPath } from "../app/onshape-params";
import { LinkType } from "../linked-documents/document-link-type";
import { get } from "../api/api";

export const queryClient = new QueryClient();

const linkedDocumentsQueryFn: QueryFunction = async (
    context
): Promise<Workspace[]> => {
    const result = await get(
        `/linked-documents/${context.meta?.linkType}` + currentInstanceApiPath()
    );
    return result.documents;
};

queryClient.setQueryDefaults(["linked-documents", LinkType.PARENTS], {
    queryFn: linkedDocumentsQueryFn,
    meta: { linkType: LinkType.PARENTS }
});

queryClient.setQueryDefaults(["linked-documents", LinkType.CHILDREN], {
    queryFn: linkedDocumentsQueryFn,
    meta: { linkType: LinkType.CHILDREN }
});
