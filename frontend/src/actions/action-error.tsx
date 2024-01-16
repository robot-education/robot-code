import { Cross } from "@blueprintjs/icons";
import { CloseButton } from "../components/close-button";
import { ActionBody } from "./action-body";
import {
    Classes,
    NonIdealState,
    NonIdealStateIconSize
} from "@blueprintjs/core";

export function ActionError() {
    return (
        <ActionBody actions={<CloseButton />}>
            <NonIdealState
                icon={
                    <Cross
                        className={Classes.INTENT_DANGER}
                        size={NonIdealStateIconSize.STANDARD}
                    />
                }
                iconMuted={false}
                title="Request failed unexpectedly"
                description="If the problem persists, contact Alex."
            />
        </ActionBody>
    );
}
