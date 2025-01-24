import { get } from "../api/api";
import { currentInstanceApiPath } from "../app/onshape-params";

export enum DefaultNameType {
    ASSEMBLY = "/assembly",
    VERSION = "/version",
    PART_STUDIO = "/part-studio"
}

/**
 * Returns a loader which can be used to fetch default names of various forms.
 */
export function makeDefaultNameLoader(nameType: DefaultNameType) {
    return () =>
        get("/default-name" + nameType + currentInstanceApiPath()).then(
            (result) => result.name
        );
}
