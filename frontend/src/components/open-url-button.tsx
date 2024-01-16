import { Button } from "@blueprintjs/core";
import { openUrlInNewTab } from "../common/url";
import { Share } from "@blueprintjs/icons";

interface UrlButtonProps {
    url: string;
    text: string;
}

export function OpenUrlButton(props: UrlButtonProps) {
    return (
        <Button
            text={props.text}
            intent="primary"
            icon={<Share />}
            onClick={() => {
                openUrlInNewTab(props.url);
            }}
        />
    );
}
