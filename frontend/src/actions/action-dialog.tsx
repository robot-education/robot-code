import { ReactNode } from "react";
import { Dialog } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";

export interface ActionDialogProps {
    title: string;
    isPending: boolean;
    actions?: ReactNode;
    children: ReactNode;
}

export function ActionDialog(props: ActionDialogProps) {
    const { title, isPending, children } = props;
    const navigate = useNavigate();
    return (
        <Dialog
            isOpen
            title={title}
            canOutsideClickClose={!isPending}
            canEscapeKeyClose={!isPending}
            isCloseButtonShown={!isPending}
            onClose={() => navigate("..")}
        >
            {children}
        </Dialog>
    );
}
