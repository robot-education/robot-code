import { ReactNode } from "react";
import { ActionDialog } from "./lib/action-dialog";
import { ActionError } from "./lib/action-error";
import { ActionProvider } from "./lib/action-provider";
import { ActionSpinner } from "./lib/action-spinner";
import { ActionSuccess } from "./lib/action-success";
import { ActionInfo } from "./action-context";
import { UseMutationResult } from "@tanstack/react-query";

export interface ActionProps {
    actionInfo: ActionInfo;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    mutation: UseMutationResult<any, any, any, any>;
    actionForm: ReactNode;
    loadingMessage: string;
    controller: AbortController;
    successMessage: string;
    successDescription: string;
    successActions?: ReactNode;
}

export function Action(props: ActionProps) {
    return (
        <ActionProvider actionInfo={props.actionInfo} mutation={props.mutation}>
            <ActionDialog>
                {props.actionForm}
                <ActionSpinner
                    message={props.loadingMessage}
                    controller={props.controller}
                />
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
