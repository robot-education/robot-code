import { createContext, useContext } from "react";

export interface ActionInfo {
    title: string;
    description: string;
    route: string;
}

const actionContext = createContext<ActionInfo>(null!);

export const ActionContextProvider = actionContext.Provider;

export function useActionInfo() {
    return useContext(actionContext);
}
