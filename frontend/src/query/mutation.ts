import { UseMutationResult } from "@tanstack/react-query";

/**
 * An alias type for UseMutationResult, which is the type returned by useMutation.
 */
export type Mutation = UseMutationResult<any, Error, any, any>;

export interface MutationProps {
    mutation: Mutation;
}
