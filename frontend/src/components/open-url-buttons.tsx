import { Button } from "@blueprintjs/core";
import { openUrlInNewTab } from "../common/url";

interface OpenUrlButtonsProps {
    /**
     * The url to open or switch to.
     */
    url: string;
    /**
     * Text for the button which switches to the url.
     */
    openText: string;
    /**
     * Text for the button that opens the url in a new tab.
     */
    openInNewTabText: string;
}

/**
 * A pair of buttons which can be used to open a url in the current window or a new tab.
 */
export function OpenUrlButtons(props: OpenUrlButtonsProps) {
    return (
        <>
            <OpenUrlButton text={props.openText} url={props.url} />
            <OpenUrlInNewTabButton
                text={props.openInNewTabText}
                url={props.url}
            />
        </>
    );
}

interface UrlButtonProps {
    url: string;
    text: string;
}

export function OpenUrlInNewTabButton(props: UrlButtonProps) {
    return (
        <Button
            text={props.text}
            intent="primary"
            icon="link"
            onClick={() => {
                open(props.url);
            }}
        />
    );
}

export function OpenUrlButton(props: UrlButtonProps) {
    return (
        <Button
            text={props.text}
            intent="primary"
            icon="share"
            onClick={() => {
                openUrlInNewTab(props.url);
            }}
        />
    );
}
