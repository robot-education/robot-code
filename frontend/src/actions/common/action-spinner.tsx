import { NonIdealState, Spinner, Button } from "@blueprintjs/core";
import { ActionDialogBody } from "./action-dialog-body";
import { ActionState } from "../action-state";
import { useMutationResetHandler, useCurrentMutation } from "./action-utils";

interface ActionSpinnerProps {
    message: string;
    controller: AbortController;
}

export function ActionSpinner(props: ActionSpinnerProps) {
    const mutation = useCurrentMutation();
    const closeMenuRouter = useMutationResetHandler(mutation);
    const abortButton = (
        <Button
            text="Abort"
            intent="danger"
            icon="cross"
            onClick={() => {
                props.controller.abort();
                closeMenuRouter();
            }}
        />
    );

    return (
        <ActionDialogBody requiredState={ActionState.EXECUTING}>
            <NonIdealState
                icon={<Spinner intent="primary" />}
                title={props.message}
                action={abortButton}
            />
        </ActionDialogBody>
    );
}
