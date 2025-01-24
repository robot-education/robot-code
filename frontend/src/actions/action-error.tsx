import { CloseButton } from "../components/close-button";
import { ActionBody } from "./action-body";
import { Icon, NonIdealState, NonIdealStateIconSize } from "@blueprintjs/core";

interface ActionErrorProps {
    title?: string;
    description?: string;
}

export function ActionError(props: ActionErrorProps) {
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
                title={props.title ?? "Request failed unexpectedly"}
                description={
                    props.description ??
                    "If the problem persists, contact Alex."
                }
            />
        </ActionBody>
    );
}
