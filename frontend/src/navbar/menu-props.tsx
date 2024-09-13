import { MenuType } from "../common/menu-type";
import { IconName } from "@blueprintjs/icons";

export interface MenuProps {
    text: string;
    icon: IconName;
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
        case MenuType.DESIGN:
            return {
                text: "Design",
                icon: "build"
            };
        // case MenuType.FEATURE_SCRIPT:
        //     return {
        //         text: "Feature script",
        //         icon: "code"
        //     };
    }
}
