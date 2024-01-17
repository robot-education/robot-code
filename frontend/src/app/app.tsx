import { QueryClientProvider } from "@tanstack/react-query";
import {
    Navigate,
    Outlet,
    useLocation,
    useMatch,
    useSearchParams
} from "react-router-dom";

import { queryClient } from "../query/query-client";
import { AppNavbar } from "../navbar/app-navbar";
import { saveOnshapeParams } from "./onshape-params";

export function App() {
    const params = useSearchParams()[0];

    const isApp = Boolean(useMatch("/app"));

    // location logging for debugging
    const location = useLocation();
    console.log(location.pathname + location.search);

    if (isApp) {
        saveOnshapeParams(params);
        return <Navigate to="/app/home" />;
    }
    return (
        <QueryClientProvider client={queryClient}>
            <AppNavbar />
            <Outlet />
        </QueryClientProvider>
    );
}
