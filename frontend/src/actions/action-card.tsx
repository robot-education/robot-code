import { Button, Card, H4 } from "@blueprintjs/core";
import { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { ActionInfo } from "./action-context";
import { ArrowRight } from "@blueprintjs/icons";

interface ActionCardProps {
    actionInfo: ActionInfo;
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
                text="Configure"
                rightIcon={<ArrowRight />}
                intent="primary"
                onClick={() => navigate(actionInfo.route)}
            />
        </Card>
    );
}
