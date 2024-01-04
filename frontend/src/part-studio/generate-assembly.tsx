import { useRef, useState } from "react";
import { FormGroup, Tooltip, InputGroup } from "@blueprintjs/core";
import { useMutation } from "@tanstack/react-query";

import { handleStringChange } from "../common/handlers";
import { ActionForm } from "../actions/action-form";
import { ActionInfo } from "../actions/action-context";
import { ActionCard } from "../actions/action-card";
import { useCurrentMutation } from "../actions/common/action-utils";
import { Action } from "../actions/action";
import { useLoaderData } from "react-router-dom";
import {
    GenerateAssemblyArgs,
    generateAssemblyMutation
} from "./generate-assembly-query";
import { OpenUrlButtons } from "../components/open-url-buttons";

const actionInfo: ActionInfo = {
    title: "Generate assembly",
    description: "Generate a new assembly from the current part studio.",
    route: "generate-assembly"
};

export function GenerateAssemblyCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

/**
 * I think I can write a mutation wrapper which automatically injects the AbortController into the execute function when mutate is called.
 * That allows the mutation to handle the abort controller internally.
 */

export function GenerateAssembly() {
    const abortControllerRef = useRef<AbortController>(null!);

    const execute = async (args: GenerateAssemblyArgs) => {
        const controller = new AbortController();
        abortControllerRef.current = controller;
        return generateAssemblyMutation(args, controller);
    };

    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn: execute
    });

    const openButton = mutation.isSuccess && (
        <OpenUrlButtons
            openInNewTabText="Switch to assembly"
            openText="Open in new tab"
            url={mutation.data.assemblyUrl}
        />
    );

    return (
        <Action
            actionInfo={actionInfo}
            mutation={mutation}
            actionForm={<GenerateAssemblyForm />}
            loadingMessage="Generating assembly"
            controller={abortControllerRef.current}
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
    // const [autoAssemble, setAutoAssemble] = useState(true);
    const disabled = assemblyName === "";

    const options = (
        <>
            <FormGroup label="Assembly name" labelInfo="(required)">
                <Tooltip content={"The name of the generated assembly"}>
                    <InputGroup
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

    const handleSubmit = () => mutation.mutate({ assemblyName });

    return (
        <ActionForm
            disabled={disabled}
            options={options}
            onSubmit={handleSubmit}
        />
    );
}
