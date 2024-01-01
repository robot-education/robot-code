import { ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { Navigate, Outlet, useLocation, useMatch } from "react-router-dom";

import { queryClient } from "../query-client";
import {
    onshapeParamsContext,
    useAppType,
    useSaveOnshapeParams
} from "../common/onshape-params";
import { AppNavbar } from "./app-navbar";

export function App() {
    const location = useLocation();
    console.log(location.pathname + location.search);

    const match = Boolean(useMatch("/app"));
    const children = match ? (
        <AppRedirect />
    ) : (
        <>
            <AppNavbar />
            <Outlet />
        </>
    );
    return <AppProviders>{children}</AppProviders>;
}

function AppProviders(props: { children: ReactNode }) {
    return (
        <QueryClientProvider client={queryClient}>
            <OnshapeParamsProvider>{props.children}</OnshapeParamsProvider>
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

function AppRedirect() {
    const appType = useAppType();
    return <Navigate to={appType} />;
}
