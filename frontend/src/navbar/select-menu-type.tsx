import { Button, Menu, Popover } from "@blueprintjs/core";
import { SelectMenuItem } from "./select-menu-item";
import { MenuType, useCurrentMenuType } from "../app/menu-type";
import { getMenuProps } from "./menu-props";
import { getCurrentElementType } from "../app/onshape-params";
import { ElementType } from "../common/element-type";

export function SelectMenuType() {
    const currentMenuType = useCurrentMenuType();
    const elementType = getCurrentElementType();

    const currentMenuButton = (
        <Button
            {...getMenuProps(currentMenuType)}
            rightIcon="caret-down"
            intent="primary"
            minimal
        />
    );

    let currentMenuTypes = Object.values(MenuType).filter(
        (menuType) => menuType !== currentMenuType
    );

    if (elementType == ElementType.PART_STUDIO) {
        currentMenuTypes = currentMenuTypes.filter(
            (menuType) => menuType !== MenuType.ASSEMBLY
        );
    } else if (elementType == ElementType.ASSEMBLY) {
        currentMenuTypes = currentMenuTypes.filter(
            (menuType) => menuType !== MenuType.PART_STUDIO
        );
    }

    const menuItems = currentMenuTypes.map((menuType) => (
        <SelectMenuItem key={menuType} menuType={menuType} />
    ));

    const menu = <Menu>{menuItems}</Menu>;

    return (
        <Popover content={menu} minimal placement="bottom-start">
            {currentMenuButton}
        </Popover>
    );
}
