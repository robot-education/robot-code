import { createBrowserRouter } from "react-router-dom";
import { App } from "./app/app";
import { PartStudio } from "./pages/part-studio/part-studio";
import { GenerateAssembly } from "./pages/part-studio/generate-assembly";
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
import { Assembly } from "./pages/assembly/assembly";
import { MenuType } from "./app/menu-type";
import { Design } from "./pages/design/design";
import { AddDesign } from "./pages/design/add-design";
import { UpdateChildReferences } from "./versions/update-child-references";

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
                    },
                    {
                        path: "update-child-references",
                        element: <UpdateChildReferences />
                    }
                ]
            },
            {
                path: MenuType.DESIGN,
                element: <Design />,
                children: [
                    {
                        path: "link-manager",
                        element: <LinkManager />
                    },
                    {
                        path: "add-design",
                        element: <AddDesign />,
                        loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    }
                    // {
                    //     path: "swerve-drive",
                    //     element: <SwerveDrive />,
                    //     loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    // },
                    // {
                    //     path: "climber",
                    //     element: <Climber />,
                    //     loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    // }
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
