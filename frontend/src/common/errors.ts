export class HandledError extends Error {
    constructor(message: string) {
        super(message);
        Object.setPrototypeOf(this, HandledError.prototype);
    }
}
