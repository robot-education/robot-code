import { useSearch } from "@tanstack/react-router";
import { ElementType } from "../api/element-type";
import { ElementPath } from "../api/path";

export enum ColorTheme {
    LIGHT = "light",
    DARK = "dark"
}

export interface OnshapeParams extends ElementPath {
    elementType: ElementType;
    theme: ColorTheme;
}

export function useOnshapeParams(): OnshapeParams {
    return useSearch({ from: "/app" });
}

export function useCurrentElementType(): ElementType {
    return useOnshapeParams().elementType;
}
