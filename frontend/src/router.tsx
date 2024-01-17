import { createBrowserRouter } from "react-router-dom";
import { Root } from "./pages/root";
import { App } from "./app/app";
import { Home } from "./home/home";
import { GenerateAssembly } from "./home/generate-assembly";
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

export const router = createBrowserRouter([
    {
        path: "/",
        element: <Root />,
        children: [
            {
                path: "app",
                element: <App />,
                children: [
                    {
                        path: "home",
                        element: <Home />,
                        children: [
                            {
                                path: "link-manager",
                                element: <LinkManager />
                            },
                            {
                                path: "generate-assembly",
                                element: <GenerateAssembly />,
                                loader: makeDefaultNameLoader(
                                    DefaultNameType.ASSEMBLY
                                )
                            }
                        ]
                    },
                    {
                        path: "versions",
                        element: <Versions />,
                        children: [
                            {
                                path: "link-manager",
                                element: <LinkManager />
                            },
                            {
                                path: "push-version",
                                element: <PushVersion />,
                                loader: makeDefaultNameLoader(
                                    DefaultNameType.VERSION
                                )
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
        ]
    }
]);
