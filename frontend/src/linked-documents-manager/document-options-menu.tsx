import {
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    Popover
} from "@blueprintjs/core";
import { makeUrl, openUrl, openUrlInNewTab } from "../common/url";
import { DocumentPath } from "../api/path";
import { _delete } from "../api/api";

interface DocumentOptionsMenuProps {
    documentPath: DocumentPath;
}

export function DocumentOptionsMenu(props: DocumentOptionsMenuProps) {
    const url = makeUrl(props.documentPath);
    const handleDeleteLink = () => {
        _delete("/document-link", props.documentPath);
    };
    const menu = (
        <Menu>
            <MenuItem
                text="Switch to document"
                icon="link"
                intent="primary"
                onClick={() => openUrl(url)}
            />
            <MenuItem
                text="Open in new tab"
                icon="share"
                intent="primary"
                onClick={() => openUrlInNewTab(url)}
            />
            <MenuDivider />
            <MenuItem
                text="Delete link"
                icon="cross"
                intent="danger"
                onClick={handleDeleteLink}
            />
        </Menu>
    );
    return (
        <Popover content={menu} placement="bottom-end" minimal>
            <Button
                alignText="left"
                intent="primary"
                text="Options"
                rightIcon="caret-down"
                minimal
            />
        </Popover>
    );
}
