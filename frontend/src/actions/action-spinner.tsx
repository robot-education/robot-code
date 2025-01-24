import { NonIdealState, Spinner } from "@blueprintjs/core";
import { ActionBody } from "./action-body";

interface ActionSpinnerProps {
    message: string;
    slow?: boolean;
}

export function ActionSpinner(props: ActionSpinnerProps) {
    const slow = props.slow ?? false;
    // const abortButton = (
    //     <Button
    //         text="Abort"
    //         intent="danger"
    //         icon={<Cross />}
    //         onClick={() => {
    //         }}
    //     />
    // );

    return (
        <ActionBody>
            <NonIdealState
                icon={<Spinner intent="primary" />}
                title={props.message}
                description={slow ? "This may take some time." : undefined}
                // action={abortButton}
            />
        </ActionBody>
    );
}
