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
}

/**
 * Makes a post request to a backend /api route.
 */
export async function post(path: string, options?: PostOptions): Promise<any> {
    return fetch(getUrl(path, options?.query), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(options?.body ?? {})
    }).then((res) => {
        if (!res.ok) {
            throw new Error("Network response failed.");
        }
        return res.json();
    });
}

/**
 * Makes a get request to a backend /api route.
 */
export async function get(
    path: string,
    query?: URLSearchParamsInit
): Promise<any> {
    return fetch(getUrl(path, query), {
        cache: "no-store"
    }).then((res) => {
        if (!res.ok) {
            throw new Error("Network response failed.");
        }
        return res.json();
    });
}

/**
 * Makes a delete request to a backend /api route.
 * Note delete is a reserved keyword in JavaScript.
 */
export async function del(
    path: string,
    query?: URLSearchParamsInit
): Promise<any> {
    return fetch(getUrl(path, query), { method: "DELETE" }).then((res) => {
        if (!res.ok) {
            throw new Error("Network response failed.");
        }
        return res.json();
    });
}
