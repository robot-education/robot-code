import { UseMutationResult } from "@tanstack/react-query";
import { ReactNode } from "react";
import { ActionInfo, ActionContextProvider } from "../action-context";
import { ActionDialog } from "./action-dialog";
import { MutationProvider } from "./action-utils";

interface ActionProviderProps {
    actionInfo: ActionInfo;
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
