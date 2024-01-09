import { URLSearchParamsInit, createSearchParams } from "react-router-dom";

function getUrl(path: string, query?: URLSearchParamsInit): string {
    path = "/api" + path;
    if (query) {
        path += `?${createSearchParams(query)}`;
    }
    return path;
}

interface PostOptions {
    query?: URLSearchParamsInit;
    body?: object;
    signal?: AbortSignal;
}

/**
 * Makes a post request to a backend /api route.
 */
export async function post(path: string, options: PostOptions): Promise<any> {
    return fetch(getUrl(path, options.query), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(options.body ?? {}),
        signal: options.signal
    })
        .then((res) => {
            if (!res.ok) {
                throw res;
            }
            return res.json();
        })
        .catch(() => null);
}

/**
 * Makes a get request to a backend /api route.
 */
export async function get(
    path: string,
    query?: URLSearchParamsInit
): Promise<any> {
    return fetch(getUrl(path, query))
        .then((res) => {
            if (!res.ok) {
                throw res;
            }
            return res.json();
        })
        .catch(() => null);
}

/**
 * Makes a delete request to a backend /api route.
 * Note delete is a reserved keyword in JavaScript.
 */
export async function _delete(
    path: string,
    query?: URLSearchParamsInit
): Promise<any> {
    return fetch(getUrl(path, query), { method: "DELETE" })
        .then((res) => {
            if (!res.ok) {
                throw res;
            }
            return res.json();
        })
        .catch(() => null);
}
