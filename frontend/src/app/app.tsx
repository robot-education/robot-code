import { QueryClientProvider } from "@tanstack/react-query";
import { Outlet, useLocation } from "react-router-dom";

import { queryClient } from "../query/query-client";
import { AppNavbar } from "../navbar/app-navbar";
import { Provider } from "react-redux";
import { store } from "./store";

export function App() {
    const location = useLocation();
    console.log(location.pathname + location.search);

    return (
        <QueryClientProvider client={queryClient}>
            <Provider store={store}>
                <AppNavbar />
                <Outlet />
            </Provider>
        </QueryClientProvider>
    );
}
