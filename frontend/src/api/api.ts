import { URLSearchParamsInit, createSearchParams } from "react-router-dom";

function getUrl(path: string, query: URLSearchParamsInit): string {
    path = "/api" + path;
    if (query) {
        path += `?${createSearchParams(query)}`;
    }
    return path;
}

/**
 * Makes a post request to a backend /api route.
 */
export async function post(
    path: string,
    signal: AbortSignal,
    query: URLSearchParamsInit,
    body: object = {}
): Promise<any> {
    return fetch(getUrl(path, query), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
        signal
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
    query: URLSearchParamsInit
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
