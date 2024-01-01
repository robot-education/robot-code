import { MenuItem } from "@blueprintjs/core";
import { MenuType, getMenuProps, useMenuRouter } from "../common/menu-type";

interface MenuTypeSelectItemProps {
    menuType: MenuType;
}

export function SelectMenuItem(props: MenuTypeSelectItemProps) {
    const { menuType } = props;
    const menuRouter = useMenuRouter();
    return (
        <MenuItem
            {...getMenuProps(menuType)}
            onClick={() => menuRouter(menuType)}
        />
    );
}
