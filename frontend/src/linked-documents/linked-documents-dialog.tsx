import { Button, Dialog, DialogBody, DialogFooter } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";
import { CloseButton } from "../components/close-button";
import { LinkType } from "./document-link-type";
import { LinkedDocumentsList } from "./linked-documents-list";

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

export function LinkedDocumentsDialog() {
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
                <LinkedDocumentsList
                    linkType={LinkType.PARENTS}
                    title="Parent documents"
                    subtitle="Documents which use things created in this document."
                />
                <br />
                <LinkedDocumentsList
                    linkType={LinkType.CHILDREN}
                    title="Child documents"
                    subtitle="Documents which create things used in this document."
                />
            </DialogBody>
            <DialogFooter minimal actions={<CloseButton />} />
        </Dialog>
    );
}
