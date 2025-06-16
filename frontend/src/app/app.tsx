import { queryClient } from "../query-client";
import { AppNavbar } from "./app-navbar";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { Navigate, Outlet, useMatchRoute } from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";

export function App() {
    const matchRoute = useMatchRoute();

    if (matchRoute({ to: "/app" })) {
        return <Navigate to="/app/documents" />;
    }
    return (
        <>
            <QueryClientProvider client={queryClient}>
                <AppNavbar />
                <Outlet />
            </QueryClientProvider>
            <TanStackRouterDevtools />
        </>
    );
}
