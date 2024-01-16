import { Button } from "@blueprintjs/core";
import { ArrowRight } from "@blueprintjs/icons";

interface ExecuteButtonProps {
    onSubmit: () => void;
    loading?: boolean;
    disabled?: boolean;
}

export function ExecuteButton(props: ExecuteButtonProps) {
    return (
        <Button
            loading={props.loading ?? false}
            disabled={props.disabled ?? false}
            text="Execute"
            intent="primary"
            type="submit"
            rightIcon={<ArrowRight />}
            onClick={props.onSubmit}
        />
    );
}
