import { DialogBody, DialogFooter } from "@blueprintjs/core";
import { ReactNode } from "react";

interface ActionBodyProps {
    children: ReactNode;
    actions?: ReactNode;
}

export function ActionBody(props: ActionBodyProps) {
    return (
        <>
            <DialogBody>{props.children}</DialogBody>
            <DialogFooter minimal actions={props.actions} />
        </>
    );
}
