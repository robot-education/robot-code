import { ElementType } from "react";
import { apiGet } from "./api/api";
import { ElementPath } from "./api/path";
import { App } from "./app/app";
import { DocumentList } from "./app/document-list";
import { OnshapeParams } from "./app/onshape-params";
import { GrantDenied } from "./pages/grant-denied";
import { License } from "./pages/license";
import {
    createRootRoute,
    createRoute,
    createRouter,
    retainSearchParams,
    SearchSchemaInput
} from "@tanstack/react-router";
import { queryOptions } from "@tanstack/react-query";
import { queryClient } from "./query-client";

const rootRoute = createRootRoute();

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
        return search as unknown as OnshapeParams;
    },
    search: {
        middlewares: [retainSearchParams(true)]
    }
});

export interface DocumentResult {
    documents: DocumentObj[];
    elements: ElementObj[];
}

export interface DocumentObj {
    name: string;
    id: string;
    elementIds: string[];
}

export interface ElementObj extends ElementPath {
    name: string;
    id: string;
    elementType: ElementType;
}

const loadDocuments = queryOptions<DocumentResult>({
    queryKey: ["documents"],
    queryFn: () => apiGet("/documents"),
    staleTime: Infinity
});

const documentsRoute = createRoute({
    getParentRoute: () => appRoute,
    path: "/documents",
    component: DocumentList,
    loader: () => queryClient.ensureQueryData(loadDocuments)
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
