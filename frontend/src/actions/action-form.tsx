import { ReactNode } from "react";
import { ActionBody } from "./action-body";

interface ActionFormProps {
    description?: string;
    children?: ReactNode;
    actions: ReactNode;
}

export function ActionForm(props: ActionFormProps) {
    const { description, children, actions } = props;
    const renderDescription = description ? <p>{description}</p> : null;

    return (
        <ActionBody actions={actions}>
            {renderDescription}
            {children}
        </ActionBody>
    );
}
