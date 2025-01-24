import { Outlet } from "react-router-dom";
import { UpdateFeatureScriptVersionCard } from "./update-featurescript-version";

export function Code(): JSX.Element {
    return (
        <>
            <UpdateFeatureScriptVersionCard />
            <Outlet />
        </>
    );
}
