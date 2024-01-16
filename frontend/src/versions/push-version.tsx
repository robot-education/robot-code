import { FormGroup, InputGroup, TextArea } from "@blueprintjs/core";
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
import { LinkType } from "../linked-documents/document-link-type";

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
                <ActionSpinner message="Creating and pushing version" />
            )}
            {mutation.isError && <ActionError />}
            {mutation.isSuccess && (
                <ActionSuccess
                    message={`Successfully created and pushed version ${mutation.variables.name}.`}
                />
            )}
        </ActionDialog>
    );
}

function PushVersionForm(props: MutationProps) {
    const mutation = props.mutation;
    const defaultName = useLoaderData() as string;

    const query = useQuery<Workspace[]>({
        queryKey: ["linked-documents", LinkType.PARENTS]
    });

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const nameIsEmpty = versionName === "";
    const nameIsTooLong = versionName.length > 256;

    const [versionDescription, setVersionDescription] = useState("");
    const descriptionIsTooLong = versionDescription.length > 10000;

    const disabled = nameIsEmpty || nameIsTooLong || descriptionIsTooLong;

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

    const executeButton = (
        <ExecuteButton
            loading={!disabled && query.isFetching}
            disabled={disabled}
            onSubmit={() =>
                mutation.mutate({
                    name: versionName,
                    description: versionDescription,
                    instancePaths: query.data
                })
            }
        />
    );
    return (
        <ActionForm
            description={actionInfo.description}
            executeButton={executeButton}
        >
            {versionNameField}
            {versionDescriptionField}
        </ActionForm>
    );
}
