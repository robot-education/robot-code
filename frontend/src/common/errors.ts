import { getCurrentElementPath } from "../app/onshape-params";
import { capitalize } from "./str-utils";

/**
 * Errors which are generated and thrown on the client.
 * Unlike other errors, the message is usually displayed directly to the user.
 */
export class HandledError extends Error {
    constructor(message: string) {
        super(message);
        Object.setPrototypeOf(this, new.target.prototype);
    }
}

/**
 * Errors reported by the backend and handled on the client.
 */
export class ReportedError extends Error {
    public type: string;

    constructor(type: string) {
        super(type);
        Object.setPrototypeOf(this, new.target.prototype);
        this.type = type;
    }
}

export class MissingPermissionError extends ReportedError {
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

export class LinkedCycleError extends ReportedError {
    public constructor() {
        super("LINKED_CYCLE");
        Object.setPrototypeOf(this, new.target.prototype);
    }
}

export function reportLinkedCycleError(json: any) {
    if (json.type !== "LINKED_CYCLE") {
        return;
    }
    throw new LinkedCycleError();
}
