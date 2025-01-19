import { Outlet } from "react-router-dom";
import { ClimberCard } from "./climber";
import { CopyDesignCard } from "./copy-design";

export function Design(): JSX.Element {
    return (
        <>
            <CopyDesignCard />
            {/* <SwerveDriveCard /> */}
            <ClimberCard />
            <Outlet />
        </>
    );
}
