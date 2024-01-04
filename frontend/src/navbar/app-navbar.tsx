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
import { OpenLinkedDocumentsButton } from "../linked-documents-manager/linked-documents-manager";

/**
 * Provides top-level navigation for the app.
 */
export function AppNavbar(): ReactNode {
    const menuType = useCurrentMenuType();
    const referencesButton = menuType === MenuType.VERSIONS && (
        <>
            <NavbarDivider />
            <OpenLinkedDocumentsButton />
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
