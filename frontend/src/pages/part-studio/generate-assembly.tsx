import { useState } from "react";
import { FormGroup, Tooltip, InputGroup } from "@blueprintjs/core";
import { useMutation } from "@tanstack/react-query";

import { handleStringChange } from "../../common/handlers";
import { ActionForm } from "../../actions/action-form";
import { ActionInfo } from "../../actions/action-context";
import { ActionCard } from "../../actions/action-card";
import { ActionDialog } from "../../actions/action-dialog";
import { useLoaderData } from "react-router-dom";
import { OpenUrlButton } from "../../components/open-url-button";
import { getCurrentElementPath } from "../../app/onshape-params";
import { ElementPath, toElementApiPath } from "../../api/path";
import { makeUrl } from "../../common/url";
import { post } from "../../api/api";
import { ExecuteButton } from "../../components/execute-button";
import { ActionError } from "../../actions/action-error";
import { ActionSuccess } from "../../actions/action-success";
import { ActionSpinner } from "../../actions/action-spinner";
import { MutationProps } from "../../query/mutation";
import { MissingPermissionError } from "../../common/errors";

const actionInfo: ActionInfo = {
    title: "Generate assembly",
    description: "Generate a new assembly from the current part studio.",
    route: "generate-assembly"
};

export function GenerateAssemblyCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface GenerateAssemblyArgs {
    assemblyName: string;
}

export function GenerateAssembly() {
    const mutationFn = async (
        args: GenerateAssemblyArgs
    ): Promise<ElementPath> => {
        const path = getCurrentElementPath();
        const result = await post(
            "/generate-assembly" + toElementApiPath(path),
            {
                body: { name: args.assemblyName }
            }
        );
        // if (data.autoAssemble) {
        // await post("/auto-assembly", assemblyPath.elementObject());
        // }
        const resultPath = Object.assign({}, path);
        resultPath.elementId = result.elementId;
        return resultPath;
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    let actionError = null;
    if (mutation.isError) {
        const error = mutation.error;
        if (error instanceof MissingPermissionError) {
            actionError = (
                <ActionError
                    title="Missing permissions"
                    description={error.getDescription()}
                />
            );
        } else {
            actionError = <ActionError />;
        }
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && <GenerateAssemblyForm mutation={mutation} />}
            {mutation.isPending && (
                <ActionSpinner message="Generating assembly..." />
            )}
            {actionError}
            {mutation.isSuccess && (
                <ActionSuccess
                    message="Successfully generated assembly"
                    description="Remember to fix a part in the assembly to lock it in place."
                    actions={
                        <OpenUrlButton
                            text="Open assembly in new tab"
                            url={makeUrl(mutation.data)}
                        />
                    }
                />
            )}
        </ActionDialog>
    );
}

function GenerateAssemblyForm(props: MutationProps) {
    const mutation = props.mutation;

    const defaultName = useLoaderData() as string;
    const [assemblyName, setAssemblyName] = useState(defaultName);
    const disabled = assemblyName === "";
    // const [autoAssemble, setAutoAssemble] = useState(true);

    const assemblyNameForm = (
        <FormGroup
            label="Assembly name"
            labelInfo="(required)"
            labelFor="assembly-name"
            style={{ width: "auto" }}
        >
            <Tooltip content={"The name of the generated assembly"}>
                <InputGroup
                    id="assembly-name"
                    value={assemblyName}
                    intent={disabled ? "danger" : undefined}
                    onChange={handleStringChange(setAssemblyName)}
                    placeholder="Assembly name"
                />
            </Tooltip>
        </FormGroup>
    );
    // const executeAutoAssemblyForm = (
    //     <FormGroup
    //         label="Execute auto assembly"
    //         inline
    //         labelFor="execute-auto-assembly"
    //     >
    //         <Tooltip
    //             content={
    //                 "Whether to execute auto assembly on the generated assembly"
    //             }
    //         >
    //             <Checkbox
    //                 id="execute-auto-assembly"
    //                 checked={autoAssemble}
    //                 onClick={handleBooleanChange(setAutoAssemble)}
    //             />
    //         </Tooltip>
    //     </FormGroup>
    // );

    const executeButton = (
        <ExecuteButton
            disabled={disabled}
            onSubmit={() => mutation.mutate({ assemblyName })}
        />
    );

    return (
        <ActionForm
            description={actionInfo.description}
            actions={executeButton}
        >
            {assemblyNameForm}
        </ActionForm>
    );
}
