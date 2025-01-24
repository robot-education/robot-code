import {
    IconSize,
    Navbar,
    NavbarDivider,
    NavbarGroup
} from "@blueprintjs/core";
import { SelectMenuType } from "./select-menu-type";
import { ReactNode } from "react";

import robotIcon from "/robot-icon.svg";
import { OpenLinkManagerButton } from "../components/manage-links-button";

/**
 * Provides top-level navigation for the app.
 */
export function AppNavbar(): ReactNode {
    return (
        <Navbar>
            <NavbarGroup>
                <img
                    height={IconSize.LARGE}
                    src={robotIcon}
                    alt="Robot manager"
                />
                <NavbarDivider />
                <SelectMenuType />
                <NavbarDivider />
                <OpenLinkManagerButton />
            </NavbarGroup>
            {/* <NavbarGroup align={Alignment.RIGHT}>
                <Button icon={<Cog />} minimal />
            </NavbarGroup> */}
        </Navbar>
    );
}
