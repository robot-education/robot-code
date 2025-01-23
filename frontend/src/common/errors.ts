import { getCurrentElementPath } from "../app/onshape-params";
import { capitalize } from "./str-utils";

export class HandledError extends Error {
    constructor(message: string) {
        super(message);
        Object.setPrototypeOf(this, new.target.prototype);
    }
}

export class MissingPermissionError extends Error {
    public constructor(
        public permission: string,
        public isCurrentDocument: boolean,
        public documentName?: string
    ) {
        super("MISSING_PERMISSION");
        Object.setPrototypeOf(this, new.target.prototype);
    }

    public getDescription(unknownAccessMessage?: string): string {
        if (this.isCurrentDocument) {
            // You need to have Write access to this document.
            return `You need to have ${this.permission} access to this document.`;
        } else if (this.documentName) {
            // You need to have Write access to My Test Document.
            return `You need to have ${this.permission} access to ${this.documentName}.`;
        }
        return (
            unknownAccessMessage ??
            `You don't have access to a needed document.`
        );
    }
}

export function reportMissingPermissionError(json: any) {
    if (json.type !== "MISSING_PERMISSION") {
        return;
    }
    const isCurrentDocument =
        json.documentId === getCurrentElementPath().documentId;
    let permission;
    if (json.permission === "LINK") {
        // Change link to link document to match UI
        permission = "Link document";
    } else {
        permission = capitalize(json.permission);
    }
    throw new MissingPermissionError(
        permission,
        isCurrentDocument,
        json.documentName
    );
}
