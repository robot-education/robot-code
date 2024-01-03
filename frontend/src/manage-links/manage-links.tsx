import { Dialog, DialogBody, DialogFooter } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";
import { LinkList } from "./link-list";
import { CloseButton } from "../components/close-button";

export function ManageLinks() {
    const navigate = useNavigate();
    return (
        <Dialog
            isOpen
            canEscapeKeyClose
            canOutsideClickClose
            onClose={() => navigate("..")}
            title="Manage document links"
        >
            <DialogBody>
                <LinkList />
            </DialogBody>
            <DialogFooter minimal actions={<CloseButton />} />
        </Dialog>
    );
}
