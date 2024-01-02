import { DialogBody, DialogFooter } from "@blueprintjs/core";
import { ReactNode } from "react";
import { useActionInfo } from "./action-context";
import { ActionState } from "./action-state";
import { useCurrentMutation } from "./action-utils";

interface ActionDialogBodyProps {
    requiredState: ActionState;
    children: ReactNode;
    actions?: ReactNode;
}

export function ActionDialogBody(props: ActionDialogBodyProps) {
    const { requiredState } = props;
    const actionInfo = useActionInfo();
    const mutation = useCurrentMutation();
    const actionState = mutation.status as ActionState;

    return (
        actionState === requiredState && (
            <>
                <DialogBody useOverflowScrollContainer={false}>
                    {actionState === ActionState.CONFIGURING && (
                        <p>{actionInfo.description}</p>
                    )}
                    {props.children}
                </DialogBody>
                <DialogFooter minimal actions={props.actions} />
            </>
        )
    );
}
