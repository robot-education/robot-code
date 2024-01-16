import { MenuType } from "../common/menu-type";
import { GitBranch, Home } from "@blueprintjs/icons";

export interface MenuProps {
    text: string;
    icon: JSX.Element;
}

export function getMenuProps(menuType: MenuType): MenuProps {
    switch (menuType) {
        case MenuType.PART_STUDIO:
            return {
                text: "Part studio",
                icon: <Home />
            };
        case MenuType.ASSEMBLY:
            return {
                text: "Assembly",
                icon: <Home />
            };
        case MenuType.VERSIONS:
            return {
                text: "Versions",
                icon: <GitBranch />
            };
        // case MenuType.FEATURE_SCRIPT:
        //     return {
        //         text: "Feature script",
        //         icon: <Code />
        //     };
    }
}
