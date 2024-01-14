import { createBrowserRouter } from "react-router-dom";
import { Root } from "./root";
import { App } from "./app/app";
import { PartStudio } from "./part-studio/part-studio";
import { GenerateAssembly } from "./part-studio/generate-assembly";
import { GrantDenied } from "./grant-denied";
import { Versions } from "./versions/versions";
import { PushVersion } from "./versions/push-version";
import { UpdateAllReferences } from "./versions/update-all-references";
import { Assembly } from "./assembly/assembly";
import { LinkedDocumentsDialog } from "./linked-documents/linked-documents-dialog";
import {
    DefaultNameType,
    makeDefaultNameLoader
} from "./common/default-name-loader";

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
                        path: "part-studio",
                        element: <PartStudio />,
                        children: [
                            {
                                path: "linked-documents",
                                element: <LinkedDocumentsDialog />
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
                        path: "assembly",
                        element: <Assembly />,
                        children: [
                            {
                                path: "linked-documents",
                                element: <LinkedDocumentsDialog />
                            }
                        ]
                    },
                    {
                        path: "versions",
                        element: <Versions />,
                        children: [
                            {
                                path: "linked-documents",
                                element: <LinkedDocumentsDialog />
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
            }
        ]
    }
]);
