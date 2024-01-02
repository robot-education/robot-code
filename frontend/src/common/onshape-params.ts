import { ElementPath } from "../api/path";
import { createContext, useState, useEffect, useContext } from "react";
import { AppType } from "./app-type";
import { useSearchParams } from "react-router-dom";

export interface OnshapeParams extends ElementPath {
    appType: AppType;
}

export const onshapeParamsContext = createContext<OnshapeParams>(null!);

export function useSaveOnshapeParams(): OnshapeParams {
    // Initial state gets used for first render before getting immediately overwritten
    const [onshapeParams, setOnshapeParams] = useState<OnshapeParams>({
        appType: AppType.PART_STUDIO,
        documentId: "",
        workspaceId: "",
        elementId: ""
    });
    const params = useSearchParams()[0];
    useEffect(() => {
        if (params.has("appType")) {
            setOnshapeParams({
                appType: params.get("appType") as AppType,
                documentId: params.get("documentId")!,
                workspaceId: params.get("workspaceId")!,
                workspaceOrVersion: params.get("workspaceOrVersion")!,
                elementId: params.get("elementId")!
            });
        }
    }, [params]);
    return onshapeParams;
}

export function useAppType(): AppType {
    return useContext(onshapeParamsContext).appType;
}

export function useOnshapeParams(): OnshapeParams {
    return useContext(onshapeParamsContext);
}
