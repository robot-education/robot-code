import { Outlet } from "react-router-dom";
import { PushVersionCard } from "./push-version";
import { UpdateAllReferencesCard } from "./update-all-references";

export function Versions(): JSX.Element {
    return (
        <>
            <PushVersionCard />
            <UpdateAllReferencesCard />
            <Outlet />
        </>
    );
}
