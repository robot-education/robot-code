import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionInfo } from "../../actions/action-context";
import { useMutation } from "@tanstack/react-query";
import { ActionCard } from "../../actions/action-card";
import { ActionDialog } from "../../actions/action-dialog";
import { ActionError } from "../../actions/action-error";
import { ActionForm } from "../../actions/action-form";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { post } from "../../api/api";
import { currentInstanceApiPath } from "../../app/onshape-params";
import { isVersionNameValid } from "../../common/version-utils";
import { ExecuteButton } from "../../components/execute-button";
import { VersionNameField } from "../../components/version-fields";
import { OnSubmitProps } from "../../common/handlers";

const actionInfo: ActionInfo = {
    title: "Swerve drive",
    description: "Add a modifiable copy of a swerve drive to your document.",
    route: "swerve-drive"
};

export function SwerveDriveCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface SwerveDriveArgs {
    versionName: string;
}

export function SwerveDrive() {
    const mutationFn = async (args: SwerveDriveArgs) => {
        return post("/copy-design" + currentInstanceApiPath(), {
            body: {
                versionName: args.versionName,
                elementNames: [],
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
            <ActionSuccess message="Successfully added drivetrain" />
        );
    }

    return (
        <ActionDialog title={actionInfo.title} isPending={mutation.isPending}>
            {mutation.isIdle && <AddDesignForm onSubmit={mutation.mutate} />}
            {mutation.isPending && (
                <ActionSpinner message="Adding drivetrain..." />
            )}
            {mutation.isError && <ActionError />}
            {actionSuccess}
        </ActionDialog>
    );
}

function AddDesignForm(props: OnSubmitProps<SwerveDriveArgs>) {
    const defaultName = useLoaderData() as string;

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const disabled = !isVersionNameValid(versionName);

    const actions = (
        <>
            <ExecuteButton
                loading={!disabled}
                disabled={disabled}
                onSubmit={() => props.onSubmit({ versionName })}
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
