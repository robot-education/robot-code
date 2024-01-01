import { Button, Intent } from "@blueprintjs/core";
import { useCloseMenuRouter, useCurrentMutation } from "./action-utils";

export function CloseButton() {
    const mutation = useCurrentMutation();
    const closeMenuRouter = useCloseMenuRouter(mutation);
    return (
        <Button
            text="Close"
            intent={Intent.PRIMARY}
            icon="tick"
            onClick={closeMenuRouter}
        />
    );
}
