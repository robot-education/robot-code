// import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

// Used to make static assets work in dev
import "vite/modulepreload-polyfill";

// Blueprint css
import "normalize.css/normalize.css";
import "@blueprintjs/core/lib/css/blueprint.css";
import "@blueprintjs/icons/lib/css/blueprint-icons.css";
import "@blueprintjs/select/lib/css/blueprint-select.css";

// Custom css
import "./main.scss";

// Router
import { RouterProvider } from "react-router-dom";
import { router } from "./router";

// Blueprint Icon loading
import { Icons, IconPaths } from "@blueprintjs/icons";
// see https://vitejs.dev/guide/features.html#glob-import
const iconModules: Record<string, { default: IconPaths }> = import.meta.glob(
    [
        "../node_modules/@blueprintjs/icons/lib/esm/generated/16px/paths/*.js",
        "../node_modules/@blueprintjs/icons/lib/esm/generated/20px/paths/*.js"
    ],
    { eager: true }
);

Icons.setLoaderOptions({
    loader: async (name, size) =>
        iconModules[
            `../node_modules/@blueprintjs/icons/lib/esm/generated/${size}px/paths/${name}.js`
        ].default
});

createRoot(document.getElementById("root")!).render(
    // <StrictMode>
    <RouterProvider router={router} />
    // </StrictMode>
);
