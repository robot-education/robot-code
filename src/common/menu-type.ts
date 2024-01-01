import { BlueprintIcons_16Id } from "@blueprintjs/icons/lib/esm/generated/16px/blueprint-icons-16";
import { useLocation, useNavigate } from "react-router-dom";

export enum MenuType {
    PART_STUDIO = "part-studio",
    ASSEMBLY = "assembly",
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

interface MenuProps {
    text: string;
    icon: BlueprintIcons_16Id;
}

export function getMenuProps(menuType: MenuType): MenuProps {
    switch (menuType) {
        case MenuType.PART_STUDIO:
            return {
                text: "Part studio",
                icon: "home"
            };
        case MenuType.ASSEMBLY:
            return {
                text: "Assembly",
                icon: "home"
            };
        case MenuType.VERSIONS:
            return {
                text: "Versions",
                icon: "git-branch"
            };
    }
}
