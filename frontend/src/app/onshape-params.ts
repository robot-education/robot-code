import { AppType } from "../common/app-type";
import { ElementPath, toInstanceApiPath, toElementApiPath } from "../api/path";

// export interface OnshapeParams extends ElementPath {
//     appType: AppType;
// }

export function saveOnshapeParams(params: URLSearchParams) {
    for (const key of [
        "appType",
        "documentId",
        "instanceId",
        "instanceType",
        "elementId"
    ]) {
        sessionStorage.setItem(key, params.get(key)!);
    }
}

export function currentElementPath(): ElementPath {
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

export function currentAppType() {
    return sessionStorage.getItem("appType") as AppType;
}

export function currentInstanceApiPath() {
    return toInstanceApiPath(currentElementPath());
}

export function currentElementApiPath() {
    return toElementApiPath(currentElementPath());
}
