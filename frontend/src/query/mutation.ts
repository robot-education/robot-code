export type ExecuteAction<T, R> = (args: T, signal: AbortSignal) => Promise<R>;

export function useActionMutation<T, R>(args: T, execute: ExecuteAction<T, R>) {
    const controller = new AbortController();
    return execute(args, controller.signal);
}
