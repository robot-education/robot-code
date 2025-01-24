import { FormGroup, InputGroup, TextArea } from "@blueprintjs/core";
import { handleStringChange } from "../common/handlers";
import { isVersionNameValid } from "../common/version-utils";

interface VersionNameFieldProps {
    versionName: string;
    onNameChange: (name: string) => void;
}

export function VersionNameField(props: VersionNameFieldProps) {
    const valid = isVersionNameValid(props.versionName);
    return (
        <FormGroup
            label="Version name"
            labelInfo="(required)"
            labelFor="version-name"
        >
            <InputGroup
                style={{ width: "auto" }}
                id="version-name"
                value={props.versionName}
                intent={valid ? undefined : "danger"}
                minLength={1}
                maxLength={256}
                onChange={handleStringChange(props.onNameChange)}
                placeholder="Name"
            />
        </FormGroup>
    );
}

interface VersionDescriptionFieldProps {
    description: string;
    onDescriptionChange: (description: string) => void;
}

export function VersionDescriptionField(props: VersionDescriptionFieldProps) {
    return (
        <FormGroup
            label="Version description"
            labelInfo="(optional)"
            labelFor="version-description"
        >
            <TextArea
                fill
                id="version-description"
                value={props.description}
                minLength={0}
                maxLength={10000}
                onChange={handleStringChange(props.onDescriptionChange)}
                placeholder="Description"
            />
        </FormGroup>
    );
}
