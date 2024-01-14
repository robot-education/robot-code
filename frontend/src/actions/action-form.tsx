import { ReactNode } from "react";
import { ActionBody } from "./action-body";

interface ActionFormProps {
    description?: string;
    children?: ReactNode;
    executeButton: ReactNode;
}

export function ActionForm(props: ActionFormProps) {
    const { description, children, executeButton } = props;
    const renderDescription = description ? <p>{description}</p> : null;

    return (
        <ActionBody actions={executeButton}>
            {renderDescription}
            {children}
        </ActionBody>
    );
}
