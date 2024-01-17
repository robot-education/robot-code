import { useMutation } from "@tanstack/react-query";
import { ActionCard } from "../actions/action-card";
import { ActionForm } from "../actions/action-form";
import { ActionDialog } from "../actions/action-dialog";
import { currentInstanceApiPath } from "../app/onshape-params";
import { post } from "../api/api";
import { ExecuteButton } from "../components/execute-button";
import { ActionSuccess } from "../actions/action-success";
import { ActionSpinner } from "../actions/action-spinner";
import { ActionError } from "../actions/action-error";
import { Callout } from "@blueprintjs/core";

const actionInfo = {
    title: "Update all references",
    description:
        "Update all outdated references in the document to the latest version.",
    route: "update-all-references"
};

export function UpdateAllReferencesCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

export function UpdateAllReferences() {
    const documentPath = currentInstanceApiPath();
    const mutationFn = (): Promise<number> =>
        post("/update-references" + documentPath).then(
            (response) => response.updatedElements
        );
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    // I may eventually allow keeping old custom feature references explicitly
    // Hard to tell what's a FeatureScript or not though, without going reference by reference (yikes)
    // const [updateCustomFeatures, setUpdateCustomFeatures] = useState();

    const form = (
        <ActionForm
            description={actionInfo.description}
            actions={<ExecuteButton onSubmit={() => mutation.mutate()} />}
        >
            <Callout intent="warning" title="Document-wide changes">
                This action will update every external reference in the entire
                document to the latest version in one go. Use with caution.
            </Callout>
        </ActionForm>
    );

    let successMessage = "";
    if (mutation.isSuccess) {
        if (mutation.data > 0) {
            successMessage = `Successfully updated outdated references in ${mutation.data} tabs`;
        } else {
            successMessage = "No outdated references were found";
        }
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && form}
            {mutation.isPending && (
                <ActionSpinner message="Updating references..." />
            )}
            {mutation.isError && <ActionError />}
            {mutation.isSuccess && <ActionSuccess message={successMessage} />}
        </ActionDialog>
    );
}
