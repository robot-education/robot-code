import { PropsWithChildren } from "react";
import { saveMenuType, useCurrentMenuType } from "./menu-type";

/**
 * A simple wrapper component which saves the current menu type.
 * Separate from menu-type.ts to avoid breaking Vite rules.
 */
export function SaveMenuType(props: PropsWithChildren) {
    const menuType = useCurrentMenuType();
    saveMenuType(menuType);
    return props.children;
}
