import { Button } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";
import { useCurrentMenuType } from "../common/menu-type";

interface OpenLinkManagerButtonProps {
    /**
     * @default true
     */
    minimal?: boolean;
}

export function OpenLinkManagerButton(props: OpenLinkManagerButtonProps) {
    const navigate = useNavigate();
    const currentMenuType = useCurrentMenuType();
    const isMinimal = props.minimal ?? true;
    return (
        <Button
            icon="diagram-tree"
            intent={isMinimal ? "none" : "primary"}
            text="Manage links"
            onClick={() => navigate(`/app/${currentMenuType}/link-manager`)}
            minimal={isMinimal}
        />
    );
}
