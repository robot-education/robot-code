import { useLocation, useNavigate } from "react-router-dom";

export enum MenuType {
    HOME = "home",
    VERSIONS = "versions"
    // FEATURE_SCRIPT = "feature-script"
}

/**
 * Returns the MenuType of the current selected menu.
 */
export function useCurrentMenuType(): MenuType {
    let pathname = useLocation().pathname;
    pathname = pathname.replace("/app/", "");
    for (const type of Object.values(MenuType)) {
        if (pathname.startsWith(type)) {
            return type;
        }
    }
    throw new Error("No valid menu type");
}

/**
 * Returns a handler which can be invoked to open a given menu.
 */
export function useMenuRouter() {
    const navigate = useNavigate();
    return (menuType: MenuType) => {
        navigate("/app/" + menuType);
    };
}
