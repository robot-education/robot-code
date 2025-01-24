export function isVersionNameValid(versionName?: string) {
    if (versionName === undefined || versionName === "") {
        return false;
    }
    return versionName.length <= 256;
}
