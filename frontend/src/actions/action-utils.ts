import { UseMutationResult } from "@tanstack/react-query";
import { createContext, useContext } from "react";

import { useCurrentMenuType, useMenuRouter } from "../common/menu-type";

const mutationContext = createContext<UseMutationResult>(null!);

export const MutationProvider = mutationContext.Provider;

export function useCurrentMutation() {
    return useContext(mutationContext);
}

export function useCloseMenuRouter(mutation: UseMutationResult) {
    const menuRouter = useMenuRouter();
    const currentMenuType = useCurrentMenuType();

    return () => {
        menuRouter(currentMenuType);
        mutation.reset();
    };
}
