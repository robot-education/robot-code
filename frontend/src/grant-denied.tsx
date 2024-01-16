import {
    Classes,
    NonIdealState,
    NonIdealStateIconSize
} from "@blueprintjs/core";
import { OpenUrlButton } from "./components/open-url-button";
import { Cross } from "@blueprintjs/icons";

const URL = "https://cad.onshape.com/user/applications";

export function GrantDenied(): JSX.Element {
    const applicationAccessButton = (
        <OpenUrlButton text="Open Onshape application page" url={URL} />
    );

    return (
        <div style={{ height: "80vh" }}>
            <NonIdealState
                icon={
                    <Cross
                        className={Classes.INTENT_DANGER}
                        size={NonIdealStateIconSize.STANDARD}
                    />
                }
                title="Grant denied"
                description="Robot manager was denied access to your documents."
                action={applicationAccessButton}
            />
        </div>
    );
}
