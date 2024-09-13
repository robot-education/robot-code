import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../actions/action-card";
import { ActionInfo } from "../actions/action-context";
import { ActionForm } from "../actions/action-form";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ActionDialog } from "../actions/action-dialog";
import { post } from "../api/api";
import { currentInstanceApiPath } from "../app/onshape-params";
import { ActionError } from "../actions/action-error";
import { ActionSpinner } from "../actions/action-spinner";
import { ActionSuccess } from "../actions/action-success";
import { ExecuteButton } from "../components/execute-button";
import { Workspace } from "../api/path";
import { MutationProps } from "../query/mutation";
import { linkedParentDocumentsKey } from "../query/query-client";
import { VersionNameField } from "../components/version-fields";
import { isVersionNameValid } from "../common/version-utils";

const actionInfo: ActionInfo = {
    title: "Telescoping climber",
    description:
        "Add a modifiable copy of a COTs telescoping climber to your document.",
    route: "climber"
};

export function ClimberCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface ClimberArgs {
    versionName: string;
}

export function Climber() {
    const mutationFn = async (args: ClimberArgs) => {
        return post("/add-design" + currentInstanceApiPath(), {
            body: {
                versionName: args.versionName,
                // elementNames: [],
                documentId: "f7166c9cd8a6628556838a38",
                instanceId: "8113816c3e51cf1fa2a2b832"
            }
        });
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    let actionSuccess = null;
    if (mutation.isSuccess) {
        actionSuccess = (
            <ActionSuccess
                message="Successfully added climber"
                // description={description}
            />
        );
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && <AddDesignForm mutation={mutation} />}
            {mutation.isPending && (
                <ActionSpinner message="Adding climber..." />
            )}
            {mutation.isError && <ActionError />}
            {actionSuccess}
        </ActionDialog>
    );
}

function AddDesignForm(props: MutationProps) {
    const defaultName = useLoaderData() as string;
    const query = useQuery<Workspace[]>({ queryKey: linkedParentDocumentsKey });

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const disabled = !isVersionNameValid(versionName);

    const actions = (
        <>
            <ExecuteButton
                loading={!disabled && query.isFetching}
                disabled={disabled}
                onSubmit={() => props.mutation.mutate({ versionName })}
            />
        </>
    );
    return (
        <ActionForm description={actionInfo.description} actions={actions}>
            <VersionNameField
                versionName={versionName}
                onNameChange={setVersionName}
            />
        </ActionForm>
    );
}
