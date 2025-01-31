import { createBrowserRouter } from "react-router-dom";
import { App } from "./app/app";
import { PartStudio } from "./pages/part-studio/part-studio";
import { GenerateAssembly } from "./pages/part-studio/generate-assembly";
import { GrantDenied } from "./pages/grant-denied";
import { LinkManager } from "./link-manager/link-manager";
import {
    DefaultNameType,
    makeDefaultNameLoader
} from "./common/default-name-loader";
import { License } from "./pages/license";
import { Assembly } from "./pages/assembly/assembly";
import { MenuType } from "./app/menu-type";

export const router = createBrowserRouter([
    {
        path: "app",
        element: <App />,
        children: [
            {
                path: MenuType.PART_STUDIO,
                element: <PartStudio />,
                children: [
                    {
                        path: "link-manager",
                        element: <LinkManager />
                    },
                    {
                        path: "generate-assembly",
                        element: <GenerateAssembly />,
                        loader: makeDefaultNameLoader(DefaultNameType.ASSEMBLY)
                    }
                ]
            },
            {
                path: MenuType.ASSEMBLY,
                element: <Assembly />,
                children: [
                    {
                        path: "link-manager",
                        element: <LinkManager />
                    }
                ]
            }
        ]
    },
    {
        path: "grant-denied",
        element: <GrantDenied />
    },
    {
        path: "license",
        element: <License />
    }
]);
