import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../../actions/action-card";
import { ActionInfo } from "../../actions/action-context";
import { ActionForm } from "../../actions/action-form";
import { useMutation } from "@tanstack/react-query";
import { ActionDialog } from "../../actions/action-dialog";
import { post } from "../../api/api";
import { currentInstanceApiPath } from "../../app/onshape-params";
import { ActionError } from "../../actions/action-error";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { ExecuteButton } from "../../components/execute-button";
import { MissingPermissionError } from "../../common/errors";
import { OnSubmitProps } from "../../common/handlers";
import { useState } from "react";
import { Button, Callout, Code, Collapse } from "@blueprintjs/core";

const actionInfo: ActionInfo = {
    title: "Update FeatureScript version",
    description:
        "Update the FeatureScript version of all Feature Studios in the current document to the latest version.",
    route: "update-featurescript-version"
};

export function UpdateFeatureScriptVersionCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface UpdateFeatureScriptVersionArgs {
    stdVersion: string;
}

export function UpdateFeatureScriptVersion() {
    const updateStdVersionsMutationFn = async (
        args: UpdateFeatureScriptVersionArgs
    ) => {
        return post(
            "/update-featurescript-version" + currentInstanceApiPath(),
            {
                body: { stdVersion: args.stdVersion }
            }
        );
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn: updateStdVersionsMutationFn
    });
    let actionSuccess = null;
    if (mutation.isSuccess) {
        const updatedStudios = mutation.data.updatedStudios;
        const stdVersion = mutation.variables.stdVersion;
        const plural = updatedStudios == 1 ? "" : "s";
        const description = `Successfully updated ${updatedStudios} Feature Studio${plural} to FeatureScript version ${stdVersion}.`;
        actionSuccess = (
            <ActionSuccess
                message="Successfully updated Feature Studios"
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
                    title="Cannot update FeatureScripts"
                    description={error.getDescription()}
                />
            );
        } else {
            actionError = <ActionError />;
        }
    }
    return (
        <ActionDialog title={actionInfo.title} isPending={mutation.isPending}>
            {mutation.isIdle && (
                <UpdateFeatureScriptVersionForm onSubmit={mutation.mutate} />
            )}
            {mutation.isPending && (
                <ActionSpinner message="Updating Feature Studios..." slow />
            )}
            {actionSuccess}
            {actionError}
        </ActionDialog>
    );
}

function UpdateFeatureScriptVersionForm(
    props: OnSubmitProps<UpdateFeatureScriptVersionArgs>
) {
    const stdVersion = useLoaderData() as string;

    const [showInfo, setShowInfo] = useState(true);

    const callout = (
        <Callout intent="primary" title="Update FeatureScript versions">
            Upon execution, all FeatureScript versions and standard library
            imports will be changed to std version <Code>{stdVersion}</Code>.
            <br />
        </Callout>
    );

    const preview = (
        <>
            <Button
                text="Explanation"
                icon="info-sign"
                rightIcon={showInfo ? "chevron-up" : "chevron-down"}
                intent="primary"
                onClick={() => setShowInfo(!showInfo)}
            />
            <Collapse isOpen={showInfo}>{callout}</Collapse>
            <br />
        </>
    );

    const executeButton = (
        <ExecuteButton onSubmit={() => props.onSubmit({ stdVersion })} />
    );
    return (
        <ActionForm
            description={actionInfo.description}
            actions={executeButton}
        >
            {preview}
        </ActionForm>
    );
}
