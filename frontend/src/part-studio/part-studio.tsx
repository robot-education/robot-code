import { Outlet } from "react-router-dom";
import { GenerateAssemblyCard } from "./generate-assembly";

export function PartStudio(): JSX.Element {
    return (
        <>
            <GenerateAssemblyCard />
            <Outlet />
        </>
    );
}
