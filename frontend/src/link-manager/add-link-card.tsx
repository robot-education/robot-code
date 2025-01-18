import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Card, InputGroup, Button, Icon } from "@blueprintjs/core";
import { post } from "../api/api";
import {
    showErrorToast,
    showInternalErrorToast,
    showSuccessToast
} from "../app/toaster";
import { HandledError } from "../common/errors";
import { currentInstanceApiPath } from "../app/onshape-params";
import { parseUrl } from "../common/url";
import { handleDocumentAdded } from "../query/query-client";
import { toInstanceApiPath } from "../api/path";
import { LinkTypeProps, LinkType } from "./document-link-type";
import { Workspace } from "../api/path";
import { MissingPermissionError } from "../common/errors";

interface AddLinkArgs {
    url: string;
    linkType: LinkType;
}

async function addLinkMutationFn({
    url,
    linkType
}: AddLinkArgs): Promise<Workspace> {
    if (url.length === 0) {
        throw new HandledError("Enter a valid document link.");
    }

    const targetPath = parseUrl(url);
    if (!targetPath) {
        throw new HandledError(
            "Failed to parse the entered link. Is it a valid document link?"
        );
    }

    if (targetPath.instanceType !== "w") {
        throw new HandledError(
            "Links can only be created to workspaces, not versions."
        );
    }

    const currentApiPath = currentInstanceApiPath();
    if (toInstanceApiPath(targetPath) === currentApiPath) {
        throw new HandledError("A document can't be linked to itself!");
    }

    return post(`/linked-documents/${linkType}` + currentApiPath, {
        query: {
            documentId: targetPath.documentId,
            instanceId: targetPath.instanceId
        }
    }).catch((error) => {
        if (error instanceof MissingPermissionError) {
            throw new HandledError(
                "Failed to link document - you do not have necessary permissions."
            );
        }
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
            handleDocumentAdded(args.linkType, document);
        }
    });

    return (
        <Card className="link-card">
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
