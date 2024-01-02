import { Button, Intent } from "@blueprintjs/core";
import { NonIdealStateOverride } from "./common/non-ideal-state-override";

const URL = "https://cad.onshape.com/user/applications";

export function GrantDenied(): JSX.Element {
    const applicationAccessButton = (
        <Button
            text="Open access page"
            intent="primary"
            icon="share"
            onClick={() => {
                // location.href = URL;
                window.open(URL);
            }}
        />
    );

    return (
        <div style={{ height: "80vh" }}>
            <NonIdealStateOverride
                icon="cross"
                iconIntent={Intent.DANGER}
                title="Grant denied"
                description="Robot manager was denied access to your documents."
                action={applicationAccessButton}
            />
        </div>
    );
}
