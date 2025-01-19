import { MenuItem } from "@blueprintjs/core";
import { MenuType, useMenuRouter } from "../app/menu-type";
import { getMenuProps } from "./menu-props";

interface MenuTypeSelectItemProps {
    menuType: MenuType;
}

export function SelectMenuItem(props: MenuTypeSelectItemProps) {
    const menuRouter = useMenuRouter();
    return (
        <MenuItem
            {...getMenuProps(props.menuType)}
            onClick={() => menuRouter(props.menuType)}
        />
    );
}
