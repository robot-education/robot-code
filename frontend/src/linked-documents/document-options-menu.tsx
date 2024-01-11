import {
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    Popover
} from "@blueprintjs/core";
import { makeUrl, openUrlInNewTab } from "../common/url";
import { DocumentPath } from "../api/path";
import { del, post } from "../api/api";
import { useAppSelector } from "../app/hooks";
import { selectApiDocumentPath } from "../app/onshape-params-slice";
import { LinkType, LinkTypeProps } from "./document-link-type";
import {
    showInternalErrorToast,
    showSuccessToast,
    successToastArgs,
    toaster
} from "../app/toaster";
import { Document } from "../api/path";
import { useId } from "react";
import { queryClient } from "../query/query-client";
import { useMutation } from "@tanstack/react-query";
import { store } from "../app/store";

interface DeleteDocumentArgs {
    documentPath: DocumentPath;
    linkType: LinkType;
}

async function deleteDocumentMutationFn(
    args: DeleteDocumentArgs
): Promise<Document> {
    const currentApiPath = selectApiDocumentPath(store.getState());
    return del(`/linked-documents/${args.linkType}` + currentApiPath, {
        documentId: args.documentPath.documentId,
        workspaceId: args.documentPath.workspaceId
    });
}

interface DocumentOptionsMenuProps extends LinkTypeProps {
    documentPath: DocumentPath;
}

export function DocumentOptionsMenu(props: DocumentOptionsMenuProps) {
    const { documentPath } = props;
    const url = makeUrl(documentPath);
    const currentApiPath = useAppSelector(selectApiDocumentPath);
    const successToastId = useId();

    const deleteMutation = useMutation({
        mutationKey: ["delete", "linked-documents", documentPath],
        mutationFn: deleteDocumentMutationFn,
        onError: () => {
            showInternalErrorToast("Unexpectedly failed to delete document.");
        },
        onSuccess: (deletedDocument, args) => {
            // Update displayed documents
            queryClient.setQueryData<Document[]>(
                ["linked-documents", args.linkType],
                (oldDocuments) =>
                    (oldDocuments ?? []).filter(
                        (document) =>
                            document.documentId !==
                                deletedDocument.documentId &&
                            document.workspaceId !== deletedDocument.workspaceId
                    )
            );

            // This can (and probably should) be it's own mutation
            const handleUndo = async () => {
                await post(
                    `/linked-documents/${args.linkType}` + currentApiPath,
                    {
                        query: {
                            documentId: deletedDocument.documentId,
                            workspaceId: deletedDocument.workspaceId
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
                            queryKey: ["linked-documents", args.linkType]
                        });
                        showSuccessToast(
                            `Successfully restored ${deletedDocument.name}.`
                        );
                    });
            };

            toaster.show(
                {
                    ...successToastArgs,
                    message: `Successfully deleted ${deletedDocument.name}.`,
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
                        documentPath: props.documentPath
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
