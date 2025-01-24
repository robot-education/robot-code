import { Outlet } from "react-router-dom";
import { PushVersionCard } from "./push-version";
import { PushVersionRecursiveCard } from "./push-version-recursive";
import { UpdateAllReferencesCard } from "./update-all-references";
import { UpdateChildReferencesCard } from "./update-child-references";

export function Versions(): JSX.Element {
    return (
        <>
            <PushVersionCard />
            <PushVersionRecursiveCard/>
            <UpdateChildReferencesCard />
            <UpdateAllReferencesCard />
            <Outlet />
        </>
    );
}
