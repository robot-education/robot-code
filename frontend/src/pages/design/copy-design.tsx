import { FormGroup, InputGroup, Icon } from "@blueprintjs/core";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../../actions/action-card";
import { ActionInfo } from "../../actions/action-context";
import { ActionDialog } from "../../actions/action-dialog";
import { ActionError } from "../../actions/action-error";
import { ActionForm } from "../../actions/action-form";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { currentInstanceApiPath } from "../../app/onshape-params";
import { showErrorToast } from "../../app/toaster";
import { HandledError } from "../../common/errors";
import { parseUrl } from "../../common/url";
import { post } from "../../api/api";
import { isVersionNameValid } from "../../common/version-utils";
import { ExecuteButton } from "../../components/execute-button";
import { VersionNameField } from "../../components/version-fields";
import { MutationProps } from "../../query/mutation";

const actionInfo: ActionInfo = {
    title: "Copy design",
    description: "Copy the contents of any other document into this one.",
    route: "copy-design"
};

export function CopyDesignCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface CopyDesignArgs {
    versionName: string;
    url: string;
}

export function CopyDesign() {
    const mutationFn = async (args: CopyDesignArgs) => {
        const parsed = parseUrl(args.url);
        if (parsed === undefined) {
            throw new HandledError(
                "Failed to parse url. Is it a valid document?"
            );
        }
        const body = {
            versionName: args.versionName,
            documentId: parsed.documentId,
            instanceId: parsed.instanceId
        };
        return post("/copy-design" + currentInstanceApiPath(), { body });
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn,
        onError: (error) => {
            if (error instanceof HandledError) {
                showErrorToast(error.message);
            } else {
                throw error;
            }
        }
    });

    let actionSuccess = null;
    if (mutation.isSuccess) {
        actionSuccess = <ActionSuccess message="Successfully added design" />;
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && <CopyDesignForm mutation={mutation} />}
            {mutation.isPending && <ActionSpinner message="Adding design..." />}
            {mutation.isError && <ActionError />}
            {actionSuccess}
        </ActionDialog>
    );
}

function CopyDesignForm(props: MutationProps) {
    const defaultName = useLoaderData() as string;

    const [url, setUrl] = useState("");
    const isUrlValid = parseUrl(url) !== undefined;

    const [versionName, setVersionName] = useState(defaultName);

    const urlInput = (
        <FormGroup
            label="Document link"
            labelInfo="(required)"
            labelFor="document-link"
        >
            <InputGroup
                fill
                value={url}
                id="document-link"
                type="url"
                intent={!isUrlValid ? "danger" : undefined}
                onValueChange={(value) => {
                    setUrl(value);
                }}
                leftElement={<Icon icon="link" />}
                placeholder="Document url"
            />
        </FormGroup>
    );

    const validParameters = isVersionNameValid(versionName) && isUrlValid;
    const disabled = !validParameters;
    const execute = (
        <ExecuteButton
            disabled={disabled}
            onSubmit={() => {
                props.mutation.mutate({
                    versionName,
                    url
                });
            }}
        />
    );

    return (
        <ActionForm description={actionInfo.description} actions={execute}>
            <VersionNameField
                versionName={versionName}
                onNameChange={setVersionName}
            />
            {urlInput}
        </ActionForm>
    );
}
