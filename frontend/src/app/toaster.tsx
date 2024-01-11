import { Intent, OverlayToaster } from "@blueprintjs/core";
import { TickCircle, WarningSign } from "@blueprintjs/icons";

export const toaster = OverlayToaster.create({
    maxToasts: 3
});

// Use Blueprint types to prevent type issues
export const successToastArgs = {
    icon: <TickCircle />,
    intent: Intent.SUCCESS
};

export const errorToastArgs = {
    icon: <WarningSign />,
    intent: Intent.DANGER
};

export function showInfoToast(message: string, key?: string): string {
    return toaster.show(
        {
            icon: "info-sign",
            intent: "primary",
            message
        },
        key
    );
}

export function showSuccessToast(message: string, key?: string): string {
    return toaster.show(
        {
            ...successToastArgs,
            message
        },
        key
    );
}

export function showErrorToast(message: string, key?: string): string {
    return toaster.show(
        {
            ...errorToastArgs,
            message: message
        },
        key
    );
}

export function showInternalErrorToast(message: string, key?: string): string {
    return showErrorToast(
        message + " If the problem persists, contact Alex.",
        key
    );
}
