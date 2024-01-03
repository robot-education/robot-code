import {
    Alignment,
    Button,
    Navbar,
    NavbarDivider,
    NavbarGroup,
    NavbarHeading
} from "@blueprintjs/core";
import { SelectMenu } from "./select-menu";
import { ReactNode } from "react";
import { MenuType, useCurrentMenuType } from "../common/menu-type";
import { useNavigate } from "react-router-dom";

/**
 * Provides top-level navigation for the app.
 */
export function AppNavbar(): ReactNode {
    const menuType = useCurrentMenuType();
    const navigate = useNavigate();
    const referencesButton = menuType === MenuType.VERSIONS && (
        <>
            <NavbarDivider />
            <Button
                icon="diagram-tree"
                text="Manage links"
                onClick={() => navigate(`/app/versions/manage-links`)}
                minimal
            />
        </>
    );

    return (
        <Navbar>
            <NavbarGroup>
                <NavbarHeading>Robot manager</NavbarHeading>
                <NavbarDivider />
                <SelectMenu />
                {referencesButton}
            </NavbarGroup>
            <NavbarGroup align={Alignment.RIGHT}>
                <Button icon="cog" minimal />
            </NavbarGroup>
        </Navbar>
    );
}
