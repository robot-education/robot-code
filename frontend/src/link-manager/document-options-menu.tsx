import {
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    Popover
} from "@blueprintjs/core";
import { makeUrl, openUrlInNewTab } from "../common/url";
import { InstancePath } from "../api/path";
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
import { linkedDocumentsKey, queryClient } from "../query/query-client";
import { useMutation } from "@tanstack/react-query";

interface DeleteDocumentArgs {
    documentPath: InstancePath;
    linkType: LinkType;
}

async function deleteDocumentMutationFn(
    args: DeleteDocumentArgs
): Promise<Workspace> {
    const currentApiPath = currentInstanceApiPath();
    return del(`/linked-documents/${args.linkType}` + currentApiPath, {
        documentId: args.documentPath.documentId,
        instanceId: args.documentPath.instanceId
    });
}

interface DocumentOptionsMenuProps extends LinkTypeProps {
    instancePath: InstancePath;
}

export function DocumentOptionsMenu(props: DocumentOptionsMenuProps) {
    const { instancePath } = props;
    const url = makeUrl(instancePath);
    const successToastId = useId();

    const deleteMutation = useMutation({
        mutationKey: ["linked-documents", "delete", instancePath],
        mutationFn: deleteDocumentMutationFn,
        onError: () => {
            showInternalErrorToast("Unexpectedly failed to delete document.");
        },
        onSuccess: (deletedDocument, args) => {
            // Update displayed documents
            queryClient.setQueryData<Workspace[]>(
                linkedDocumentsKey(args.linkType),
                (oldDocuments) =>
                    (oldDocuments ?? []).filter(
                        (document) =>
                            document.documentId !==
                                deletedDocument.documentId &&
                            document.instanceId !== deletedDocument.instanceId
                    )
            );

            // This can (and probably should) be it's own mutation
            const handleUndo = async () => {
                await post(
                    `/linked-documents/${args.linkType}` +
                        currentInstanceApiPath(),
                    {
                        query: {
                            documentId: deletedDocument.documentId,
                            instanceId: deletedDocument.instanceId
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
                        queryClient.invalidateQueries({
                            queryKey: linkedDocumentsKey(args.linkType)
                        });
                        showSuccessToast(
                            `Successfully restored ${deletedDocument.name}.`
                        );
                    });
            };

            toaster.show(
                {
                    ...infoToastArgs,
                    message: `Deleted link to ${deletedDocument.name}.`,
                    action: {
                        text: "Undo",
                        onClick: handleUndo
                    }
                },
                successToastId
            );
            return document;
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
                        documentPath: props.instancePath
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
