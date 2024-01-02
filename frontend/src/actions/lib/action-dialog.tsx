import { Dialog } from "@blueprintjs/core";
import { useActionInfo } from "../action-context";
import { ReactNode } from "react";
import { ActionState } from "../action-state";
import { useCloseMenuRouter, useCurrentMutation } from "./action-utils";

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
        </Dialog>
    );
}
