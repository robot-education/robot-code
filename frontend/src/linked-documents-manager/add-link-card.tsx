import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Icon, Card, InputGroup, Button } from "@blueprintjs/core";
import { post } from "../api/api";
import { toaster } from "../app/app-toaster";
import { HandledError } from "../common/error";

interface AddLinkArgs {
    url: string;
}

async function addLinkMutation({ url }: AddLinkArgs): Promise<string> {
    if (url.length === 0) {
        throw new HandledError("Enter a valid document url.");
    }
    // TODO: Parent or child link?
    const result = await post("/document-link", {
        query: { url }
    });
    if (!result) {
        throw new HandledError(
            "Unexpectedly failed to add link. If the problem persists, contact Alex."
        );
    } else if (result.error) {
        throw new HandledError(result.error);
    }
    return `Successfully linked ${result.documentName}.`;
}

export function AddLinkCard() {
    const [url, setUrl] = useState("");

    const mutation = useMutation({
        mutationFn: addLinkMutation,
        onError: (error) => {
            if (error instanceof HandledError) {
                toaster.show({
                    message: error.data,
                    icon: "warning-sign",
                    intent: "danger"
                });
            }
        },
        onSuccess: (data) => {
            setUrl("");
            toaster.show({
                message: data,
                icon: "tick",
                intent: "success"
            });
            // TODO: Add to appropriate query
        }
    });

    const linkIcon = <Icon icon="link" />;
    return (
        <Card className="link-card" key="add">
            <InputGroup
                className="link-card-url-input"
                value={url}
                intent={mutation.error ? "danger" : undefined}
                onValueChange={(value) => setUrl(value)}
                leftElement={linkIcon}
                placeholder="Document url"
                fill={true}
            />
            <Button
                text="Add"
                icon="add"
                minimal
                intent="primary"
                loading={mutation.isPending}
                onClick={() => mutation.mutate({ url })}
            />
        </Card>
    );
}
