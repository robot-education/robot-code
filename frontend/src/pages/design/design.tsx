import { Outlet } from "react-router-dom";
import { ClimberCard } from "./climber";
import { AddDesignCard } from "./add-design";

export function Design(): JSX.Element {
    return (
        <>
            <AddDesignCard />
            {/* <SwerveDriveCard /> */}
            <ClimberCard />
            <Outlet />
        </>
    );
}
