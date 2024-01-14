import { get } from "../api/api";
import { selectApiDocumentPath } from "../app/onshape-params-slice";
import { store } from "../app/store";

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
        get(
            "/default-name" + nameType + selectApiDocumentPath(store.getState())
        ).then((result) => result.name);
}
