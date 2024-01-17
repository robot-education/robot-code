import {
    Icon,
    NonIdealState,
    NonIdealStateIconSize
} from "@blueprintjs/core";
import { ReactNode } from "react";
import { ActionBody } from "./action-body";
import { CloseButton } from "../components/close-button";

interface ActionSuccessProps {
    message: string;
    description?: string;
    actions?: ReactNode;
}
export function ActionSuccess(props: ActionSuccessProps) {
    const actions = (
        <>
            {props.actions}
            <CloseButton />
        </>
    );
    return (
        <ActionBody actions={actions}>
            <NonIdealState
                icon={
                    <Icon
                        icon="tick"
                        intent="success"
                        size={NonIdealStateIconSize.STANDARD}
                    />
                }
                iconMuted={true}
                title={props.message}
                description={props.description}
            />
        </ActionBody>
    );
}
