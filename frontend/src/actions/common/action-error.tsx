import { ActionDialogBody } from "./action-dialog-body";
import { ActionState } from "../action-state";
import { CloseButton } from "../../components/close-button";
import { NonIdealStateOverride } from "../../components/non-ideal-state-override";
import { useCurrentMutation } from "./action-utils";

export function ActionError() {
    const mutation = useCurrentMutation();
    return (
        <ActionDialogBody
            requiredState={ActionState.ERROR}
            actions={<CloseButton onClose={mutation.reset} />}
        >
            <NonIdealStateOverride
                icon="cross"
                iconIntent="danger"
                title="Request failed unexpectedly"
                description="If the problem persists, contact Alex."
            />
        </ActionDialogBody>
    );
}
