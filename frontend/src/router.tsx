import { createBrowserRouter } from "react-router-dom";
import { App } from "./app/app";
import { PartStudio } from "./pages/part-studio/part-studio";
import { GenerateAssembly } from "./pages/part-studio/generate-assembly";
import { GrantDenied } from "./pages/grant-denied";
import { Versions } from "./pages/versions/versions";
import { PushVersion } from "./pages/versions/push-version";
import { PushVersionRecursive } from "./pages/versions/push-version-recursive";
import { UpdateAllReferences } from "./pages/versions/update-all-references";
import { LinkManager } from "./link-manager/link-manager";
import {
    DefaultNameType,
    makeDefaultNameLoader
} from "./common/default-name-loader";
import { License } from "./pages/license";
import { Assembly } from "./pages/assembly/assembly";
import { MenuType } from "./app/menu-type";
import { Design } from "./pages/design/design";
import { CopyDesign } from "./pages/design/copy-design";
import { UpdateChildReferences } from "./pages/versions/update-child-references";
import { Climber } from "./pages/design/climber";
import { Code } from "./pages/code/code";
import { UpdateFeatureScriptVersion } from "./pages/code/update-featurescript-version";
import { get } from "./api/api";

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
                        path: "push-version-recursive",
                        element: <PushVersionRecursive />,
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
                        path: "copy-design",
                        element: <CopyDesign />,
                        loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    },
                    {
                        path: "climber",
                        element: <Climber />,
                        loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    }
                    // {
                    //     path: "swerve-drive",
                    //     element: <SwerveDrive />,
                    //     loader: makeDefaultNameLoader(DefaultNameType.VERSION)
                    // },
                ]
            },
            {
                path: MenuType.CODE,
                element: <Code />,
                children: [
                    { path: "link-manager", element: <LinkManager /> },
                    {
                        path: "update-featurescript-version",
                        element: <UpdateFeatureScriptVersion />,
                        loader: () =>
                            get("/latest-std-version").then(
                                (result) => result.stdVersion
                            )
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
