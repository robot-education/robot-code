import { ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { Outlet, useLocation } from "react-router-dom";

import { queryClient } from "../query-client";
import {
    onshapeParamsContext,
    useSaveOnshapeParams
} from "../common/onshape-params";
import { AppNavbar } from "../navbar/app-navbar";

export function App() {
    const location = useLocation();
    console.log(location.pathname + location.search);

    return (
        <QueryClientProvider client={queryClient}>
            <OnshapeParamsProvider>
                <AppNavbar />
                <Outlet />
            </OnshapeParamsProvider>
        </QueryClientProvider>
    );
}

function OnshapeParamsProvider(props: { children: ReactNode }) {
    const appType = useSaveOnshapeParams();
    return (
        <onshapeParamsContext.Provider value={appType}>
            {props.children}
        </onshapeParamsContext.Provider>
    );
}
