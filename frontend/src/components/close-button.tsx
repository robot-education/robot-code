import { Button } from "@blueprintjs/core";
import { useNavigate } from "react-router-dom";

interface CloseButtonProps {
    onClose?: () => void;
}

export function CloseButton(props: CloseButtonProps) {
    const { onClose } = props;
    const navigate = useNavigate();
    return (
        <Button
            text="Close"
            intent="primary"
            icon="tick"
            onClick={() => {
                if (onClose) {
                    onClose();
                }
                navigate("..");
            }}
        />
    );
}
