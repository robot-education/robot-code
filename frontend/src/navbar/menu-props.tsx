import { MenuType } from "../app/menu-type";
import { IconName } from "@blueprintjs/icons";

export interface MenuProps {
    text: string;
    icon: IconName;
}

export function getMenuProps(menuType: MenuType): MenuProps {
    switch (menuType) {
        case MenuType.PART_STUDIO:
            return {
                text: "Deriver",
                icon: "home"
            };
        case MenuType.ASSEMBLY:
            return {
                text: "Inserter",
                icon: "home"
            };
    }
}
