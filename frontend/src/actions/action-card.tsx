import { Button, Card, H4, Intent } from "@blueprintjs/core";
import { ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { ActionInfo } from "./action-context";

interface ActionCardProps {
    actionInfo: ActionInfo;
}

/**
 * Represents a card with an operation which can be executed.
 */
export function ActionCard(props: ActionCardProps): ReactNode {
    const { actionInfo } = props;
    const navigate = useNavigate();
    const pathname = useLocation().pathname;

    return (
        <Card>
            <H4>{actionInfo.title}</H4>
            <p>{actionInfo.description}</p>
            <Button
                text="Configure"
                rightIcon="arrow-right"
                intent={Intent.PRIMARY}
                onClick={() => navigate(pathname + "/" + actionInfo.route)}
            />
        </Card>
    );
}
