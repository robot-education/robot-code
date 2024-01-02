import { FocusStyleManager } from "@blueprintjs/core";
import { Outlet } from "react-router-dom";

FocusStyleManager.onlyShowFocusOnTabs();

export function Root() {
    return <Outlet />;
}
