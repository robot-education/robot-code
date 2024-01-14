import { NonIdealState, Spinner } from "@blueprintjs/core";
import { ActionBody } from "./action-body";

interface ActionSpinnerProps {
    message: string;
}

export function ActionSpinner(props: ActionSpinnerProps) {
    // const abortButton = (
    //     <Button
    //         text="Abort"
    //         intent="danger"
    //         icon="cross"
    //         onClick={() => {
    //         }}
    //     />
    // );

    return (
        <ActionBody>
            <NonIdealState
                icon={<Spinner intent="primary" />}
                title={props.message}
                // action={abortButton}
            />
        </ActionBody>
    );
}
