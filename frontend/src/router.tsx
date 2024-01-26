import { createBrowserRouter } from "react-router-dom";
import { App } from "./app/app";
import { PartStudio } from "./part-studio/part-studio";
import { GenerateAssembly } from "./part-studio/generate-assembly";
import { GrantDenied } from "./pages/grant-denied";
import { Versions } from "./versions/versions";
import { PushVersion } from "./versions/push-version";
import { UpdateAllReferences } from "./versions/update-all-references";
import { LinkManager } from "./link-manager/link-manager";
import {
    DefaultNameType,
    makeDefaultNameLoader
} from "./common/default-name-loader";
import { License } from "./pages/license";
import { Assembly } from "./assembly/assembly";
import { MenuType } from "./common/menu-type";

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
            },
            {
                path: MenuType.VERSIONS,
                element: <Versions />,
                children: [
                    {
                        path: "link-manager",
                        element: <LinkManager />
                    },
                    {
                        path: "push-version",
                        element: <PushVersion />,
                        loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    },
                    {
                        path: "update-all-references",
                        element: <UpdateAllReferences />
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
