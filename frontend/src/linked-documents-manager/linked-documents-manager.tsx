import { Button, Dialog, DialogBody, DialogFooter } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";
import { LinkedDocumentsList } from "./linked-documents-list";
import { CloseButton } from "../components/close-button";

export function OpenLinkedDocumentsButton() {
    const navigate = useNavigate();
    return (
        <Button
            icon="diagram-tree"
            text="Manage links"
            onClick={() => navigate("/app/versions/linked-documents")}
            minimal
        />
    );
}

export function LinkedDocumentsManager() {
    const navigate = useNavigate();
    return (
        <Dialog
            isOpen
            canEscapeKeyClose
            canOutsideClickClose
            onClose={() => navigate("..")}
            title="Manage linked documents"
        >
            <DialogBody>
                <LinkedDocumentsList />
            </DialogBody>
            <DialogFooter minimal actions={<CloseButton />} />
        </Dialog>
    );
}
