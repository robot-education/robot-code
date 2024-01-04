import { Dialog } from "@blueprintjs/core";
import { useActionInfo } from "../action-context";
import { ReactNode } from "react";
import { ActionState } from "../action-state";
import { useCurrentMutation } from "./action-utils";
import { useNavigate } from "react-router-dom";

export interface ActionDialogProps {
    children: ReactNode;
    // actionState: ActionState;
}

export function ActionDialog(props: ActionDialogProps): ReactNode {
    const actionInfo = useActionInfo();
    const mutation = useCurrentMutation();
    const navigate = useNavigate();
    const actionState = mutation.status as ActionState;

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
            onClose={() => {
                mutation.reset();
                navigate("..");
            }}
        >
            {props.children}
        </Dialog>
    );
}
