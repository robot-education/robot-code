import {
    Alignment,
    Button,
    IconSize,
    InputGroup,
    Navbar,
    NavbarDivider,
    NavbarGroup,
    Size
} from "@blueprintjs/core";
import { ReactNode } from "react";

import robotIcon from "/robot-icon.svg";
import { useMutation } from "@tanstack/react-query";
import { apiPost } from "../api/api";

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
                <InputGroup type="search" leftIcon="search" size={Size.LARGE} />
            </NavbarGroup>
            <NavbarGroup align={Alignment.END}>
                <ReloadAllDocumentsButton />
            </NavbarGroup>
        </Navbar>
    );
}

function ReloadAllDocumentsButton(): ReactNode {
    const mutation = useMutation({
        mutationKey: ["save-all-documents"],
        mutationFn: () => {
            return apiPost("/save-all-documents");
        }
    });

    return (
        <Button
            icon="refresh"
            minimal
            text="Reload all documents"
            onClick={() => mutation.mutate()}
            loading={mutation.isPending}
            intent="primary"
        />
    );
}
