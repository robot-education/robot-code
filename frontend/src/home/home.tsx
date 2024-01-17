import { Outlet } from "react-router-dom";
import { GenerateAssemblyCard } from "./generate-assembly";
import { getCurrentElementType } from "../app/onshape-params";
import { ElementType } from "../common/element-type";

export function Home(): JSX.Element {
    const currentElementType = getCurrentElementType();
    return (
        <>
            {currentElementType == ElementType.PART_STUDIO ? (
                <>
                    <GenerateAssemblyCard />
                </>
            ) : null}
            <Outlet />
        </>
    );
}
