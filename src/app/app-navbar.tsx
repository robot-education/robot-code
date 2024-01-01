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

/**
 * Provides top-level navigation for the app.
 */
export function AppNavbar(): ReactNode {
    return (
        <Navbar>
            <NavbarGroup>
                <NavbarHeading>Robot manager</NavbarHeading>
                <NavbarDivider />
                <SelectMenu />
            </NavbarGroup>
            <NavbarGroup align={Alignment.RIGHT}>
                <Button icon="cog" minimal />
            </NavbarGroup>
        </Navbar>
    );
}
