import { Intent } from "@blueprintjs/core";
import { NonIdealStateOverride } from "../../components/non-ideal-state-override";
import { ActionDialogBody } from "./action-dialog-body";
import { ActionState } from "../action-state";
import { ReactNode } from "react";
import { CloseButton } from "../../components/close-button";

interface ActionSuccessProps {
    message: string;
    description: string;
    actions?: ReactNode;
}
export function ActionSuccess(props: ActionSuccessProps) {
    const actions = (
        <>
            {props.actions}
            <CloseButton />
        </>
    );

    return (
        <ActionDialogBody requiredState={ActionState.SUCCESS} actions={actions}>
            <NonIdealStateOverride
                icon="tick"
                iconIntent={Intent.SUCCESS}
                title={props.message}
                description={props.description}
            />
        </ActionDialogBody>
    );
}
