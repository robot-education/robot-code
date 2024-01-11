import { useState } from "react";
import { FormGroup, Tooltip, InputGroup } from "@blueprintjs/core";
import { useMutation } from "@tanstack/react-query";

import { handleStringChange } from "../common/handlers";
import { ActionForm } from "../actions/action-form";
import { ActionInfo } from "../actions/action-context";
import { ActionCard } from "../actions/action-card";
import { useCurrentMutation } from "../actions/common/action-utils";
import { Action } from "../actions/action";
import { useLoaderData } from "react-router-dom";
import { generateAssemblyMutationFn } from "./generate-assembly-query";
import { OpenUrlButton } from "../components/open-url-button";

const actionInfo: ActionInfo = {
    title: "Generate assembly",
    description: "Generate a new assembly from the current part studio.",
    route: "generate-assembly"
};

export function GenerateAssemblyCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

export function GenerateAssembly() {
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn: generateAssemblyMutationFn
    });

    const openButton = mutation.isSuccess && (
        <OpenUrlButton
            text="Open assembly in new tab"
            url={mutation.data.assemblyUrl}
        />
    );

    return (
        <Action
            actionInfo={actionInfo}
            mutation={mutation}
            actionForm={<GenerateAssemblyForm />}
            loadingMessage="Generating assembly"
            successMessage="Successfully generated assembly."
            successDescription="Remember to fix a part in the assembly to lock it in place."
            successActions={openButton}
        />
    );
}

function GenerateAssemblyForm() {
    const mutation = useCurrentMutation();
    const defaultName = useLoaderData() as string;
    const [assemblyName, setAssemblyName] = useState(defaultName);
    const disabled = assemblyName === "";
    // const [autoAssemble, setAutoAssemble] = useState(true);

    const options = (
        <>
            <FormGroup
                label="Assembly name"
                labelInfo="(required)"
                labelFor="assembly-name"
            >
                <Tooltip content={"The name of the generated assembly"}>
                    <InputGroup
                        id="assembly-name"
                        value={assemblyName}
                        intent={disabled ? "danger" : undefined}
                        onChange={handleStringChange(setAssemblyName)}
                    />
                </Tooltip>
            </FormGroup>
            {/* <FormGroup
            label="Execute auto assembly"
            inline
        >
            <Tooltip
                content={
                    "Whether to execute auto assembly on the generated assembly"
                }
            >
                <Checkbox
                    checked={autoAssemble}
                    onClick={handleBooleanChange(setAutoAssemble)}
                />
            </Tooltip>
        </FormGroup> */}
        </>
    );

    return (
        <ActionForm
            disabled={disabled}
            options={options}
            onSubmit={() => mutation.mutate({ assemblyName })}
        />
    );
}
