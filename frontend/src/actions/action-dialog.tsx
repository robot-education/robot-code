import { Dialog } from "@blueprintjs/core";
import {
    ActionContextProvider,
    ActionInfo,
    useActionInfo
} from "./action-context";
import { ReactNode } from "react";
import { ActionState } from "./action-state";
import {
    MutationProvider,
    useCloseMenuRouter,
    useCurrentMutation
} from "./action-utils";
import { ActionError } from "./action-error";
import { UseMutationResult } from "@tanstack/react-query";

interface ActionProviderProps {
    actionInfo: ActionInfo;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    mutation: UseMutationResult<any, any, any, any>;
    children: ReactNode;
}

export function ActionProvider(props: ActionProviderProps) {
    return (
        <ActionContextProvider value={props.actionInfo}>
            <MutationProvider value={props.mutation}>
                <ActionDialog>{props.children}</ActionDialog>
            </MutationProvider>
        </ActionContextProvider>
    );
}

export function ActionDialog(props: { children: ReactNode }): ReactNode {
    const actionInfo = useActionInfo();
    const mutation = useCurrentMutation();
    const actionState = mutation.status as ActionState;
    const closeMenuRouter = useCloseMenuRouter(mutation);

    return (
        <Dialog
            isOpen
            title={actionInfo.title}
            canOutsideClickClose={
                actionState === ActionState.SUCCESS ||
                actionState === ActionState.ERROR
            }
            canEscapeKeyClose={actionState !== ActionState.EXECUTING}
            isCloseButtonShown={actionState !== ActionState.EXECUTING}
            onClose={closeMenuRouter}
        >
            {props.children}
            <ActionError />
        </Dialog>
    );
}
