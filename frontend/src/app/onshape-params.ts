import { ElementType } from "../common/element-type";
import { ElementPath } from "../api/path";
import { Store } from "@tanstack/react-store";

export interface OnshapeParams extends ElementPath {
    elementType: ElementType;
}

const onshapeParamsStore = new Store({});

export function saveOnshapeParams(params: OnshapeParams) {
    onshapeParamsStore.setState(params);
}

export function useOnshapeParams(): OnshapeParams {
    return onshapeParamsStore.state as OnshapeParams;
}

export function useCurrentElementType(): ElementType {
    return useOnshapeParams().elementType;
}
