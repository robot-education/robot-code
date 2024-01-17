import { CloseButton } from "../components/close-button";
import { ActionBody } from "./action-body";
import { Icon, NonIdealState, NonIdealStateIconSize } from "@blueprintjs/core";

export function ActionError() {
    return (
        <ActionBody actions={<CloseButton />}>
            <NonIdealState
                icon={
                    <Icon
                        icon="cross"
                        intent="danger"
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
