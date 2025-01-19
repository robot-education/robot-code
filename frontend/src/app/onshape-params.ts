/**
 * Saves parameters sent Onshape to session storage so they're available to all routes.
 */
import { ElementType } from "../common/element-type";
import { ElementPath, toInstanceApiPath, toElementApiPath } from "../api/path";

export function saveOnshapeParams(params: URLSearchParams) {
    for (const key of [
        "elementType",
        "documentId",
        "instanceId",
        "instanceType",
        "elementId"
    ]) {
        sessionStorage.setItem(key, params.get(key)!);
    }
}

export function getCurrentElementPath(): ElementPath {
    const result: any = {};
    for (const key of [
        "documentId",
        "instanceId",
        "instanceType",
        "elementId"
    ]) {
        result[key] = sessionStorage.getItem(key)!;
    }
    return result as ElementPath;
}

export function getCurrentElementType() {
    return sessionStorage.getItem("elementType") as ElementType;
}

export function currentInstanceApiPath() {
    return toInstanceApiPath(getCurrentElementPath());
}

export function currentElementApiPath() {
    return toElementApiPath(getCurrentElementPath());
}
