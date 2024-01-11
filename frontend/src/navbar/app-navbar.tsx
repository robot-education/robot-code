import {
    Navbar,
    NavbarDivider,
    NavbarGroup,
    NavbarHeading
} from "@blueprintjs/core";
import { SelectMenu } from "./select-menu";
import { ReactNode } from "react";
import { OpenLinkedDocumentsButton } from "../linked-documents/linked-documents-dialog";

/**
 * Provides top-level navigation for the app.
 */
export function AppNavbar(): ReactNode {
    const documentManagerButton = (
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
                {documentManagerButton}
            </NavbarGroup>
            {/* <NavbarGroup align={Alignment.RIGHT}>
                <Button icon="cog" minimal />
            </NavbarGroup> */}
        </Navbar>
    );
}
