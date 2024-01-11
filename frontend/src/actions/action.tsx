import { ReactNode } from "react";
import { ActionDialog } from "./common/action-dialog";
import { ActionError } from "./common/action-error";
import { ActionProvider } from "./common/action-provider";
import { ActionSpinner } from "./common/action-spinner";
import { ActionSuccess } from "./common/action-success";
import { ActionInfo } from "./action-context";
import { UseMutationResult } from "@tanstack/react-query";

export interface ActionProps {
    actionInfo: ActionInfo;
    // actionState: ActionState;
    mutation: UseMutationResult<any, any, any, any>;
    actionForm: ReactNode;
    loadingMessage: string;
    successMessage: string;
    successDescription: string;
    successActions?: ReactNode;
}

export function Action(props: ActionProps) {
    return (
        <ActionProvider actionInfo={props.actionInfo} mutation={props.mutation}>
            <ActionDialog>
                {props.actionForm}
                <ActionSpinner message={props.loadingMessage} />
                <ActionSuccess
                    message={props.successMessage}
                    description={props.successDescription}
                    actions={props.successActions}
                />
                <ActionError />
            </ActionDialog>
        </ActionProvider>
    );
}
