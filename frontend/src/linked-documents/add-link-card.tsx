import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Icon, Card, InputGroup, Button } from "@blueprintjs/core";
import { post } from "../api/api";
import {
    showErrorToast,
    showInternalErrorToast,
    showSuccessToast
} from "../app/toaster";
import { HandledError } from "../common/errors";
import { selectApiDocumentPath } from "../app/onshape-params-slice";
import { store } from "../app/store";
import { parseUrl } from "../common/url";
import { queryClient } from "../query/query-client";
import { toApiDocumentPath } from "../api/path";
import { LinkTypeProps, LinkType } from "./document-link-type";
import { Document } from "../api/path";

interface AddLinkArgs {
    url: string;
    linkType: LinkType;
}

async function addLinkMutationFn({
    url,
    linkType
}: AddLinkArgs): Promise<Document> {
    if (url.length === 0) {
        throw new HandledError("Enter a valid document link.");
    }

    const targetPath = parseUrl(url);
    if (!targetPath) {
        throw new HandledError(
            "Failed to parse the entered link. Is it a valid document link?"
        );
    }

    const currentApiPath = selectApiDocumentPath(store.getState());
    if (toApiDocumentPath(targetPath) === currentApiPath) {
        throw new HandledError("A document can't be linked to itself!");
    }

    return post(`/linked-documents/${linkType}` + currentApiPath, {
        query: {
            documentId: targetPath.documentId,
            workspaceId: targetPath.workspaceId
        }
    }).catch(() => {
        showInternalErrorToast("Unexpectedly failed to add link.");
    });
}

export function AddLinkCard({ linkType }: LinkTypeProps) {
    const [url, setUrl] = useState("");
    const addLinkMutation = useMutation({
        mutationKey: ["post", "linked-documents", linkType],
        mutationFn: addLinkMutationFn,
        onError: (error) => {
            if (error instanceof HandledError) {
                showErrorToast(error.message);
            }
        },
        onSuccess: (document, args) => {
            showSuccessToast(`Successfully linked ${document.name}.`);
            setUrl("");
            // queryClient.invalidateQueries({
            //     queryKey: ["linked-documents", linkType]
            // });
            queryClient.setQueryData<Document[]>(
                ["linked-documents", args.linkType],
                (queryData) => (queryData ?? []).concat(document)
            );
        }
    });

    return (
        <Card className="link-card" key="add">
            <InputGroup
                className="link-card-url-input"
                fill
                value={url}
                intent={addLinkMutation.error ? "danger" : undefined}
                onValueChange={(value) => setUrl(value)}
                leftElement={<Icon icon="link" />}
                placeholder="Document link"
            />
            <Button
                text="Add"
                icon="add"
                minimal
                intent="primary"
                loading={addLinkMutation.isPending}
                onClick={() => addLinkMutation.mutate({ url, linkType })}
            />
        </Card>
    );
}
