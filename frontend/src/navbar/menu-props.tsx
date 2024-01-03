import { BlueprintIcons_16Id } from "@blueprintjs/icons/lib/esm/generated/16px/blueprint-icons-16";

import { MenuType } from "../common/menu-type";

export interface MenuProps {
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
        // case MenuType.FEATURE_SCRIPT:
        //     return {
        //         text: "Feature script",
        //         icon: "code"
        //     };
    }
}
