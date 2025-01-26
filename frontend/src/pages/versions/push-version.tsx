import {
    Button,
    Callout,
    Checkbox,
    Classes,
    Collapse,
    FormGroup,
    Tooltip
} from "@blueprintjs/core";
import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../../actions/action-card";
import { ActionInfo } from "../../actions/action-context";
import { ActionForm } from "../../actions/action-form";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ActionDialog } from "../../actions/action-dialog";
import { post } from "../../api/api";
import { currentInstanceApiPath } from "../../app/onshape-params";
import { ActionError } from "../../actions/action-error";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { ExecuteButton } from "../../components/execute-button";
import { WorkspacePath, Workspace, toInstanceApiPath } from "../../api/path";
import { OpenLinkManagerButton } from "../../components/manage-links-button";
import {
    VersionDescriptionField,
    VersionNameField
} from "../../components/version-fields";
import { isVersionNameValid } from "../../common/version-utils";
import { LinkedCycleError, MissingPermissionError } from "../../common/errors";
import { handleBooleanChange, OnSubmitProps } from "../../common/handlers";
import { getLinkedDocumentsOptions } from "../../query/query-client";
import {
    allOpenableDocuments,
    isOpenableDocument,
    LinkType
} from "../../link-manager/link-types";

const actionInfo: ActionInfo = {
    title: "Push version",
    description:
        "Create a version and push it to one or more linked parent documents.",
    route: "push-version"
};

export function PushVersionCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface PushVersionArgs {
    name: string;
    description: string;
    instancePaths: WorkspacePath[];
}

export function PushVersion() {
    const pushVersionMutationFn = async (args: PushVersionArgs) => {
        return post("/push-version" + currentInstanceApiPath(), {
            body: {
                name: args.name,
                description: args.description,
                instancesToUpdate: args.instancePaths
            }
        });
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn: pushVersionMutationFn
    });

    let actionSuccess = null;
    if (mutation.isSuccess) {
        const args = mutation.variables;
        const length = args.instancePaths.length;
        const plural = length == 1 ? "" : "s";
        const description = `Successfully updated references in ${length} document${plural}.`;
        actionSuccess = (
            <ActionSuccess
                message="Successfully pushed version"
                description={description}
            />
        );
    }

    let actionError = null;
    if (mutation.isError) {
        const error = mutation.error;
        if (error instanceof MissingPermissionError) {
            actionError = (
                <ActionError
                    title="Cannot push document"
                    description={error.getDescription(
                        "You don't have access to a linked document."
                    )}
                />
            );
        } else {
            actionError = <ActionError />;
        }
    }

    return (
        <ActionDialog title={actionInfo.title} isPending={mutation.isPending}>
            {mutation.isIdle && <PushVersionForm onSubmit={mutation.mutate} />}
            {mutation.isPending && (
                <ActionSpinner message="Creating and pushing version..." />
            )}
            {actionSuccess}
            {actionError}
        </ActionDialog>
    );
}

function PushVersionForm(props: OnSubmitProps<PushVersionArgs>) {
    const defaultName = useLoaderData() as string;

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const [versionDescription, setVersionDescription] = useState("");
    const [pushRecursively, setPushRecursively] = useState(false);

    const query = useQuery({
        ...getLinkedDocumentsOptions(LinkType.PARENTS, pushRecursively)
    });
    const validDocuments = query.data?.filter(isOpenableDocument) ?? [];

    const enabled =
        isVersionNameValid(versionName) &&
        versionDescription.length <= 10000 &&
        query.isSuccess &&
        validDocuments.length > 0;

    let invalidLinkCallout = null;
    let noParentsCallout = null;
    let preview = null;
    if (query.isSuccess) {
        if (!allOpenableDocuments(query.data)) {
            invalidLinkCallout = (
                <>
                    <Callout title="Invalid link" intent="warning">
                        <p>There is an invalid link you can't push to.</p>
                        <OpenLinkManagerButton minimal={false} />
                    </Callout>
                    <br />
                </>
            );
        }

        if (query.data.length == 0) {
            noParentsCallout = <NoLinkedParentsCallout />;
        } else {
            preview = (
                <PushVersionPreview
                    enabled={enabled}
                    versionName={versionName}
                    documents={validDocuments}
                    recursive={pushRecursively}
                />
            );
        }
    } else if (query.isError) {
        if (pushRecursively && query.error instanceof LinkedCycleError) {
            invalidLinkCallout = (
                <>
                    <Callout title="Cycle detected" intent="danger">
                        <p>Cannot push documents in a circular loop.</p>
                        <OpenLinkManagerButton minimal={false} />
                    </Callout>
                    <br />
                </>
            );
        } else {
            return <ActionError title="Failed to get linked documents" />;
        }
    }

    const pushRecursivelyField = (
        <FormGroup label="Push recursively" labelFor="push-recursively" inline>
            <Tooltip content="If true, pushing will propagate recursively to all linked parents.">
                <Checkbox
                    id="push-recursively"
                    title="Push recursively"
                    checked={pushRecursively}
                    onClick={handleBooleanChange(setPushRecursively)}
                />
            </Tooltip>
        </FormGroup>
    );

    const versionNameField = (
        <VersionNameField
            versionName={versionName}
            onNameChange={setVersionName}
        />
    );

    const versionDescriptionField = (
        <VersionDescriptionField
            description={versionDescription}
            onDescriptionChange={setVersionDescription}
        />
    );

    const actions = (
        <>
            <ExecuteButton
                loading={!enabled || query.isFetching}
                disabled={!enabled || query.isFetching}
                onSubmit={() =>
                    props.onSubmit({
                        name: versionName,
                        description: versionDescription,
                        instancePaths: validDocuments
                    })
                }
            />
        </>
    );
    return (
        <ActionForm description={actionInfo.description} actions={actions}>
            {preview}
            {noParentsCallout}
            {invalidLinkCallout}
            {pushRecursivelyField}
            {versionNameField}
            {versionDescriptionField}
        </ActionForm>
    );
}

function NoLinkedParentsCallout() {
    return (
        <>
            <Callout title="No linked parent documents" intent="warning">
                <p>This document doesn't have any linked parents to push to.</p>
                <OpenLinkManagerButton minimal={false} />
            </Callout>
            <br />
        </>
    );
}

interface PushVersionPreviewProps {
    enabled: boolean;
    versionName: string;
    documents: Workspace[];
    recursive: boolean;
}

function PushVersionPreview(props: PushVersionPreviewProps) {
    const [showInfo, setShowInfo] = useState(false);
    const { enabled, versionName, documents, recursive } = props;

    const showInfoButton = (
        <Button
            disabled={!enabled}
            text="Explanation"
            icon="info-sign"
            rightIcon={showInfo ? "chevron-up" : "chevron-down"}
            intent="primary"
            onClick={() => setShowInfo(!showInfo)}
        />
    );

    const documentsList = documents.map((document) => (
        <li key={toInstanceApiPath(document)}>{document.name}</li>
    ));

    let preview;
    if (recursive) {
        preview = (
            <>
                Upon execution, {versionName} will be created and recursively
                pushed to the following documents:
                <ul className={Classes.LIST} style={{ listStyleType: "disc" }}>
                    {documentsList}
                </ul>
            </>
        );
    } else {
        preview = (
            <>
                Upon execution, the following things will happen:
                <ol className={Classes.LIST}>
                    <li>A new version named {versionName} will be created.</li>
                    <li>
                        All references to this document from the following
                        documents will be updated to use {versionName}:
                        <ul
                            className={Classes.LIST}
                            style={{ listStyleType: "disc" }}
                        >
                            {documentsList}
                        </ul>
                    </li>
                </ol>
            </>
        );
    }

    return (
        <>
            {showInfoButton}
            <Collapse isOpen={showInfo}>
                <Callout intent="primary" title="Push version steps">
                    {preview}
                </Callout>
            </Collapse>
            <br />
        </>
    );
}
