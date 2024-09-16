import { Button, Card, H4 } from "@blueprintjs/core";
import { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { ActionInfo } from "./action-context";

interface ActionCardProps {
    actionInfo: ActionInfo;
    children?: ReactNode;
}

/**
 * Represents a card with an operation which can be executed.
 */
export function ActionCard(props: ActionCardProps): ReactNode {
    const { actionInfo } = props;
    const navigate = useNavigate();
    return (
        <Card>
            <H4>{actionInfo.title}</H4>
            <p>{actionInfo.description}</p>
            <Button
                text="Set up"
                rightIcon="arrow-right"
                intent="primary"
                onClick={() => navigate(actionInfo.route)}
            />
            {props.children}
        </Card>
    );
}
