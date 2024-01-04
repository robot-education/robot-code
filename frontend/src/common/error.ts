export class HandledError<T> extends Error {
    public data: T;

    constructor(data: T) {
        super(
            "Internal error - this error is supposed to be handled! Contact Alex."
        );
        Object.setPrototypeOf(this, HandledError.prototype);
        this.data = data;
    }
}
