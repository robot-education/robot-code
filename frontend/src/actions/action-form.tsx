import { Button } from "@blueprintjs/core";
import { ReactNode } from "react";
import { ActionState } from "./action-state";
import { ActionDialogBody } from "./common/action-dialog-body";

interface ActionFormProps {
    disabled?: boolean;
    options?: ReactNode;
    onSubmit: () => void;
}

export function ActionForm(props: ActionFormProps) {
    const { disabled, options } = props;

    const executeButton = (
        <Button
            text="Execute"
            intent="primary"
            type="submit"
            rightIcon="arrow-right"
            disabled={disabled ?? false}
            onClick={props.onSubmit}
        />
    );

    return (
        <ActionDialogBody
            requiredState={ActionState.CONFIGURING}
            actions={executeButton}
        >
            {options}
        </ActionDialogBody>
    );
}
