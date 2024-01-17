import { Button } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";

interface CloseButtonProps {
    /**
     * An onClose handler.
     * If unspecified, the close button defaults to navigating up one level.
     */
    onClose?: () => void;
}

export function CloseButton(props: CloseButtonProps) {
    const navigate = useNavigate();
    const handleClose = props.onClose ?? (() => navigate(".."));
    return (
        <Button
            text="Close"
            intent="primary"
            icon="tick"
            onClick={handleClose}
        />
    );
}
