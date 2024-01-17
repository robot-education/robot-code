import {
    Button,
    Callout,
    Classes,
    Collapse,
    FormGroup,
    InputGroup,
    TextArea
} from "@blueprintjs/core";
import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../actions/action-card";
import { ActionInfo } from "../actions/action-context";
import { ActionForm } from "../actions/action-form";
import { handleStringChange } from "../common/handlers";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ActionDialog } from "../actions/action-dialog";
import { post } from "../api/api";
import { currentInstanceApiPath } from "../app/onshape-params";
import { ActionError } from "../actions/action-error";
import { ActionSpinner } from "../actions/action-spinner";
import { ActionSuccess } from "../actions/action-success";
import { ExecuteButton } from "../components/execute-button";
import { WorkspacePath, Workspace } from "../api/path";
import { MutationProps } from "../query/mutation";
import { linkedParentDocumentsKey } from "../query/query-client";
import { OpenLinkManagerButton } from "../components/manage-links-button";

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
    const mutationFn = async (args: PushVersionArgs) => {
        console.log("Description: " + args.description);
        console.log(args.instancePaths);
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
        mutationFn
    });

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && <PushVersionForm mutation={mutation} />}
            {mutation.isPending && (
                <ActionSpinner message="Creating and pushing version..." />
            )}
            {mutation.isError && <ActionError />}
            {mutation.isSuccess && (
                <ActionSuccess
                    message={`Successfully pushed version ${mutation.variables.name} to ${mutation.variables.instancePaths.length} documents`}
                />
            )}
        </ActionDialog>
    );
}

function PushVersionForm(props: MutationProps) {
    const defaultName = useLoaderData() as string;
    const query = useQuery<Workspace[]>({ queryKey: linkedParentDocumentsKey });

    const [showInfo, setShowInfo] = useState(false);

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const nameIsEmpty = versionName === "";
    const nameIsTooLong = versionName.length > 256;

    const [versionDescription, setVersionDescription] = useState("");
    const descriptionIsTooLong = versionDescription.length > 10000;

    const disabled = nameIsEmpty || nameIsTooLong || descriptionIsTooLong;

    const noParentsCallout =
        query.isSuccess && query.data.length == 0 ? (
            <>
                <Callout title="No linked parent documents" intent="warning">
                    <p>
                        This document doesn't have any linked parents to push
                        to.
                    </p>
                    <OpenLinkManagerButton minimal={false} />
                </Callout>
                <br />
            </>
        ) : null;

    const preview =
        query.isSuccess && query.data.length > 0 ? (
            <>
                <Button
                    disabled={disabled}
                    text="Explanation"
                    icon="info-sign"
                    rightIcon={showInfo ? "chevron-up" : "chevron-down"}
                    intent="primary"
                    onClick={() => setShowInfo(!showInfo)}
                />
                <Collapse isOpen={showInfo}>
                    <Callout intent="primary" title="Push version steps">
                        Upon execution, the following things will happen:
                        <ol className={Classes.LIST}>
                            <li>
                                A new version named {versionName} will be
                                created.
                            </li>
                            <li>
                                All references to this document from the
                                following documents will be updated to{" "}
                                {versionName}:
                                <ul
                                    className={Classes.LIST}
                                    style={{ listStyleType: "disc" }}
                                >
                                    {query.data.map((document) => (
                                        <li>{document.name}</li>
                                    ))}
                                </ul>
                            </li>
                        </ol>
                    </Callout>
                </Collapse>
                <br />
            </>
        ) : null;

    const versionNameField = (
        <FormGroup
            label="Version name"
            labelInfo="(required)"
            labelFor="version-name"
        >
            <InputGroup
                style={{ width: "auto" }}
                id="version-name"
                value={versionName}
                intent={nameIsEmpty || nameIsTooLong ? "danger" : undefined}
                onChange={handleStringChange(setVersionName)}
                placeholder="Name"
            />
        </FormGroup>
    );

    const versionDescriptionField = (
        <FormGroup
            label="Version description"
            labelInfo="(optional)"
            labelFor="version-description"
        >
            <TextArea
                fill
                id="version-description"
                value={versionDescription}
                intent={descriptionIsTooLong ? "danger" : undefined}
                onChange={handleStringChange(setVersionDescription)}
                placeholder="Description"
            />
        </FormGroup>
    );

    const actions = (
        <>
            <ExecuteButton
                loading={!disabled && query.isFetching}
                disabled={disabled}
                onSubmit={() =>
                    props.mutation.mutate({
                        name: versionName,
                        description: versionDescription,
                        instancePaths: query.data
                    })
                }
            />
        </>
    );
    return (
        <ActionForm description={actionInfo.description} actions={actions}>
            {preview}
            {noParentsCallout}
            {versionNameField}
            {versionDescriptionField}
        </ActionForm>
    );
}
