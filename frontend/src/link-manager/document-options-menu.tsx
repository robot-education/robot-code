import {
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    Popover
} from "@blueprintjs/core";
import { makeUrl, openUrlInNewTab } from "../common/url";
import { WorkspacePath } from "../api/path";
import { del, post } from "../api/api";
import { currentInstanceApiPath } from "../app/onshape-params";
import { LinkType, LinkTypeProps } from "./document-link-type";
import {
    infoToastArgs,
    showInternalErrorToast,
    showSuccessToast,
    toaster
} from "../app/toaster";
import { Workspace } from "../api/path";
import { useId } from "react";
import {
    handleDocumentAdded,
    handleDocumentRemoved
} from "../query/query-client";
import { useMutation } from "@tanstack/react-query";

interface RemoveDocumentArgs {
    workspacePath: WorkspacePath;
    linkType: LinkType;
}

async function removeDocumentMutationFn(
    args: RemoveDocumentArgs
): Promise<Workspace> {
    const currentApiPath = currentInstanceApiPath();
    return del(`/linked-documents/${args.linkType}` + currentApiPath, {
        documentId: args.workspacePath.documentId,
        instanceId: args.workspacePath.instanceId
    });
}

interface DocumentOptionsMenuProps extends LinkTypeProps {
    workspacePath: WorkspacePath;
}

export function DocumentOptionsMenu(props: DocumentOptionsMenuProps) {
    const { workspacePath } = props;
    const url = makeUrl(workspacePath);
    const successToastId = useId();

    const deleteMutation = useMutation({
        mutationKey: ["linked-documents", "delete", workspacePath],
        mutationFn: removeDocumentMutationFn,
        onError: () => {
            showInternalErrorToast("Unexpectedly failed to delete document.");
        },
        onSuccess: (removedDocument, args) => {
            handleDocumentRemoved(args.linkType, removedDocument);

            // This can (and probably should) be it's own mutation
            const handleUndo = async () => {
                await post(
                    `/linked-documents/${args.linkType}` +
                        currentInstanceApiPath(),
                    {
                        query: {
                            documentId: removedDocument.documentId,
                            instanceId: removedDocument.instanceId
                        }
                    }
                )
                    .catch(() => {
                        toaster.dismiss(successToastId);
                        showInternalErrorToast(
                            "Unexpectedly failed to restore document."
                        );
                    })
                    .then(() => {
                        toaster.dismiss(successToastId);
                        handleDocumentAdded(args.linkType, removedDocument);
                        showSuccessToast(
                            `Successfully restored ${removedDocument.name}.`
                        );
                    });
            };

            toaster.show(
                {
                    ...infoToastArgs,
                    message: `Removed link to ${removedDocument.name}.`,
                    action: {
                        text: "Undo",
                        onClick: handleUndo
                    }
                },
                successToastId
            );
        }
    });

    const menu = (
        <Menu>
            <MenuItem
                text="Open in new tab"
                icon="share"
                intent="primary"
                onClick={() => openUrlInNewTab(url)}
            />
            <MenuDivider />
            <MenuItem
                text="Delete link"
                icon="cross"
                intent="danger"
                onClick={() => {
                    deleteMutation.mutate({
                        linkType: props.linkType,
                        workspacePath: props.workspacePath
                    });
                }}
            />
        </Menu>
    );
    return (
        <Popover content={menu} placement="bottom-end" minimal>
            <Button
                alignText="left"
                intent="primary"
                text="Options"
                rightIcon="caret-down"
                minimal
            />
        </Popover>
    );
}
