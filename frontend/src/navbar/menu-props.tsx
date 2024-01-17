import { MenuType } from "../common/menu-type";
import { IconName } from "@blueprintjs/icons";

export interface MenuProps {
    text: string;
    icon: IconName;
}

export function getMenuProps(menuType: MenuType): MenuProps {
    switch (menuType) {
        case MenuType.HOME:
            return {
                text: "Home",
                icon: "home"
            };
        case MenuType.VERSIONS:
            return {
                text: "Versions",
                icon: "git-branch"
            };
        // case MenuType.FEATURE_SCRIPT:
        //     return {
        //         text: "Feature script",
        //         icon: "code"
        //     };
    }
}
