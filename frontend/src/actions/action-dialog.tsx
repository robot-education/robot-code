import { ReactNode } from "react";
import { Dialog } from "@blueprintjs/core";
import { UseMutationResult } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";

export interface ActionProps {
    title: string;
    mutation: UseMutationResult<any, Error, any, any>;
    actions?: ReactNode;
    children: ReactNode;
}

export function ActionDialog(props: ActionProps) {
    const { title, mutation, children } = props;
    const navigate = useNavigate();
    return (
        <Dialog
            isOpen
            title={title}
            canOutsideClickClose={!mutation.isPending}
            canEscapeKeyClose={!mutation.isPending}
            isCloseButtonShown={!mutation.isPending}
            onClose={() => navigate("..")}
        >
            {children}
        </Dialog>
    );
}
