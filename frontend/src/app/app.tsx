import { QueryClientProvider } from "@tanstack/react-query";
import { Navigate, Outlet, useMatch, useSearchParams } from "react-router-dom";

import { queryClient } from "../query/query-client";
import { AppNavbar } from "../navbar/app-navbar";
import { getCurrentElementType, saveOnshapeParams } from "./onshape-params";
import { ElementType } from "../common/element-type";
import { MenuType } from "./menu-type";
import { SaveMenuType } from "./save-menu-type";

export function App() {
    const params = useSearchParams()[0];
    const isApp = Boolean(useMatch("/app"));

    if (isApp) {
        saveOnshapeParams(params);
        const elementType = getCurrentElementType();
        const defaultMenuType =
            elementType === ElementType.PART_STUDIO
                ? MenuType.PART_STUDIO
                : MenuType.ASSEMBLY;
        return <Navigate to={"/app/" + defaultMenuType} />;
    }
    return (
        <QueryClientProvider client={queryClient}>
            <SaveMenuType>
                <AppNavbar />
                <Outlet />
            </SaveMenuType>
        </QueryClientProvider>
    );
}
