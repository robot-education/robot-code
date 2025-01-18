export class HandledError extends Error {
    constructor(message: string) {
        super(message);
        Object.setPrototypeOf(this, HandledError.prototype);
    }
}

export class MissingPermissionError {
    constructor(public permission: string, public documentName?: string) {}

    public getMessage(): string {
        if (this.documentName) {
            return `You do not have ${this.permission} access to ${this.documentName}.`;
        }
        return `You do not have ${this.permission} access to this document.`;
    }

    public getDescription(): string {
        if (this.documentName) {
            return `You do not have ${this.permission} access to ${this.documentName}.`;
        }
        return `You do not have ${this.permission} access.`;
    }
}

export function catchMissingPermissionError(json: any) {
    if (json.type === "MISSING_PERMISSION") {
        return;
    }
    throw new MissingPermissionError(
        json.permission.toLowerCase(),
        json.documentName
    );
}
