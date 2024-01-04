import { useCurrentMutation } from "./common/action-utils";

export enum ActionState {
    CONFIGURING = "idle",
    EXECUTING = "pending",
    ERROR = "error",
    SUCCESS = "success"
}

export function getActionState() {}

export function useCurrentActionState(): ActionState {
    const mutation = useCurrentMutation();
    return mutation.status as ActionState;
}
