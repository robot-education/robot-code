import { UseMutationResult } from "@tanstack/react-query";
import { createContext, useContext } from "react";

const mutationContext = createContext<UseMutationResult>(null!);

export const MutationProvider = mutationContext.Provider;

export function useCurrentMutation() {
    return useContext(mutationContext);
}

export function useMutationResetHandler(mutation: UseMutationResult) {
    return () => {
        mutation.reset();
    };
}
