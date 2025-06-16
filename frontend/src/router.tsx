import { usePrefetchQuery } from "@tanstack/react-query";
import { apiGet } from "./api/api";
import { ElementPath, InstanceType } from "./api/path";
import { App } from "./app/app";
import { DocumentList } from "./app/document-list";
import { OnshapeParams } from "./app/onshape-params";
import { ElementType } from "./common/element-type";
import { GrantDenied } from "./pages/grant-denied";
import { License } from "./pages/license";
import {
    createRootRoute,
    createRoute,
    createRouter,
    retainSearchParams,
    SearchSchemaInput
} from "@tanstack/react-router";

const rootRoute = createRootRoute({});

/**
 * When the app is first loaded by Onshape, Onshape provides search params indiciating the context the app is being accessed from.
 * This route saves those parameters off to a store for later.
 */
const appRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/app",
    component: App,
    // Add SearchSchemaInput so search parameters become optional
    validateSearch: (
        search: Record<string, unknown> & SearchSchemaInput
    ): OnshapeParams => {
        return {
            elementType: search.elementType as ElementType,
            documentId: search.documentId as string,
            instanceType: search.instanceType as InstanceType,
            instanceId: search.instanceId as string,
            elementId: search.elementId as string
        };
    },
    search: {
        middlewares: [retainSearchParams(true)]
    }
});

export interface DocumentResult {
    documents: Document[];
    elements: Element[];
}

export interface Document {
    name: string;
    id: string;
    elementIds: string[];
}

export interface Element extends ElementPath {
    name: string;
    id: string;
}

const documentsRoute = createRoute({
    getParentRoute: () => appRoute,
    path: "/documents",
    component: DocumentList,
    loader: async (): Promise<DocumentResult> => apiGet("/documents")
});

const grantDeniedRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/grant-denied",
    component: GrantDenied
});

const licenseRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/license",
    component: License
});

const routeTree = rootRoute.addChildren([
    appRoute.addChildren([documentsRoute]),
    grantDeniedRoute,
    licenseRoute
]);

export const router = createRouter({ routeTree });

// export const router = createBrowserRouter([
//     {
//         path: "app",
//         element: <App />,
//         children: [
//             {
//                 path: MenuType.PART_STUDIO,
//                 element: <PartStudio />,
//                 children: [
//                     {
//                         path: "link-manager",
//                         element: <LinkManager />
//                     },
//                     {
//                         path: "generate-assembly",
//                         element: <GenerateAssembly />,
//                         loader: makeDefaultNameLoader(DefaultNameType.ASSEMBLY)
//                     }
//                 ]
//             },
//             {
//                 path: MenuType.ASSEMBLY,
//                 element: <Assembly />,
//                 children: [
//                     {
//                         path: "link-manager",
//                         element: <LinkManager />
//                     }
//                 ]
//             }
//         ]
//     },
//     {
//         path: "grant-denied",
//         element: <GrantDenied />
//     },
//     {
//         path: "license",
//         element: <License />
//     }
// ]);
