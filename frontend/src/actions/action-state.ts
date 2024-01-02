import { useCurrentMutation } from "./action-utils";

export enum ActionState {
    CONFIGURING = "idle",
    EXECUTING = "pending",
    ERROR = "error",
    SUCCESS = "success"
}

export function useCurrentActionState(): ActionState {
    const mutation = useCurrentMutation();
    return mutation.status as ActionState;
}
