import { useRef, useState } from "react";
import {
    FormGroup,
    Tooltip,
    InputGroup,
    Intent,
    Button
} from "@blueprintjs/core";
import { useMutation } from "@tanstack/react-query";

import { handleStringChange } from "../common/handlers";
import { useOnshapeParams } from "../common/onshape-params";
import { ActionForm } from "../actions/action-form";
import { ActionInfo } from "../actions/action-context";
import { ActionCard } from "../actions/action-card";
import { useCurrentMutation } from "../actions/lib/action-utils";
import { Action } from "../actions/action";
import { post } from "../api/api";
import { ElementPath, toElementQuery } from "../api/path";

const actionInfo: ActionInfo = {
    title: "Generate assembly",
    description: "Generate a new assembly from the current part studio.",
    route: "generate-assembly"
};

interface ActionArgs {
    assemblyName: string;
}

export function GenerateAssemblyCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

export function GenerateAssembly() {
    const abortControllerRef = useRef<AbortController>(null!);
    const onshapeParams = useOnshapeParams();

    const execute = async (args: ActionArgs) => {
        abortControllerRef.current = new AbortController();
        const currentPath: ElementPath = onshapeParams;

        const result = await post(
            "/generate-assembly",
            abortControllerRef.current.signal,
            toElementQuery(currentPath),
            { name: args.assemblyName }
        );
        if (!result) {
            throw new Error("Request failed.");
        }

        const assemblyPath = Object.assign({}, currentPath);
        assemblyPath.elementId = result.elementId;
        const assemblyUrl = `https://cad.onshape.com/documents/${assemblyPath.documentId}/w/${assemblyPath.workspaceId}/e/${assemblyPath.elementId}`;
        // if (data.autoAssemble) {
        // const result = await post("/auto-assembly", assemblyPath.elementObject());
        // if (result == null) { return false; }
        // }
        return { assemblyUrl };
    };

    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn: execute
    });

    const openButton = mutation.isSuccess && (
        <Button
            text="Open assembly"
            intent="primary"
            icon="share"
            onClick={() => {
                window.open(mutation.data.assemblyUrl);
            }}
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
    const [assemblyName, setAssemblyName] = useState("Assembly");
    // const [autoAssemble, setAutoAssemble] = useState(true);
    const disabled = assemblyName === "";

    const options = (
        <>
            <FormGroup
                label="Assembly name"
                labelFor="assemblyName"
                labelInfo="(required)"
            >
                <Tooltip content={"The name of the generated assembly"}>
                    <InputGroup
                        name="assemblyName"
                        value={assemblyName}
                        intent={disabled ? Intent.DANGER : undefined}
                        onChange={handleStringChange(setAssemblyName)}
                    />
                </Tooltip>
            </FormGroup>
            {/* <FormGroup
            label="Execute auto assembly"
            labelFor="auto-assemble"
            inline={true}
        >
            <Tooltip
                content={
                    "Whether to execute auto assembly on the generated assembly"
                }
            >
                <Checkbox
                    id="auto-assemble"
                    checked={autoAssemble}
                    onClick={handleBooleanChange(setAutoAssemble)}
                />
            </Tooltip>
        </FormGroup> */}
        </>
    );

    const handleSubmit = () => {
        mutation.mutate({ assemblyName });
    };

    return (
        <ActionForm
            disabled={disabled}
            options={options}
            onSubmit={handleSubmit}
        />
    );
}
