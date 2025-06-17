import { queryClient } from "../query-client";
import { AppNavbar } from "./app-navbar";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import {
    Navigate,
    Outlet,
    useMatchRoute,
    useSearch
} from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { ColorTheme } from "./onshape-params";
import { Section } from "@blueprintjs/core";

export function App() {
    const matchRoute = useMatchRoute();
    const search = useSearch({ from: "/app" });

    if (matchRoute({ to: "/app" })) {
        return <Navigate to="/app/documents" />;
    }

    return (
        <Section
            className={search.theme === ColorTheme.DARK ? "bp5-dark" : ""}
            style={{ position: "absolute", height: "100%", width: "100%" }}
        >
            <QueryClientProvider client={queryClient}>
                <AppNavbar />
                <Outlet />
                <TanStackRouterDevtools />
            </QueryClientProvider>
        </Section>
    );
}
