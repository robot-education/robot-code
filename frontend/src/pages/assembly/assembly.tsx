import { Icon, NonIdealState, NonIdealStateIconSize } from "@blueprintjs/core";
import { Outlet } from "react-router-dom";

export function Assembly(): JSX.Element {
    return (
        <>
            <div style={{ height: "80vh" }}>
                <NonIdealState
                    icon={
                        <Icon
                            icon="build"
                            intent="primary"
                            size={NonIdealStateIconSize.STANDARD}
                        />
                    }
                    iconMuted={false}
                    title="Work in progress"
                    description={
                        "There aren't currently any assembly specific actions. Hopefully some will be added soon!"
                    }
                />
            </div>
            <Outlet />
        </>
    );
}
