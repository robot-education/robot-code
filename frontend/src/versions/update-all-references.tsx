import { useMutation } from "@tanstack/react-query";
import { ActionCard } from "../actions/action-card";
import { ActionForm } from "../actions/action-form";
import { ActionDialog } from "../actions/action-dialog";
import { selectApiDocumentPath } from "../app/onshape-params-slice";
import { useAppSelector } from "../app/hooks";
import { post } from "../api/api";
import { ExecuteButton } from "../components/execute-button";
import { ActionSuccess } from "../actions/action-success";
import { ActionSpinner } from "../actions/action-spinner";
import { ActionError } from "../actions/action-error";

const actionInfo = {
    title: "Update all references",
    description: "Update outdated references to the latest version.",
    route: "update-all-references"
};

export function UpdateAllReferencesCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

export function UpdateAllReferences() {
    const documentPath = useAppSelector(selectApiDocumentPath);
    const mutationFn = (): Promise<number> =>
        post("/update-references" + documentPath).then(
            (response) => response.updatedElements
        );
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    // I may eventually allow keeping old custom feature references explicitly
    // const [updateCustomFeatures, setUpdateCustomFeatures] = useState();
    // const options =

    const form = (
        <ActionForm
            executeButton={<ExecuteButton onSubmit={() => mutation.mutate()} />}
        />
    );

    let successMessage = "";
    if (mutation.isSuccess) {
        if (mutation.data > 0) {
            successMessage = `Successfully updated outdated references in ${mutation.data} tabs.`;
        } else {
            successMessage =
                "No outdated references were found. You're all set!";
        }
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && form}
            {mutation.isPending && (
                <ActionSpinner message="Updating references" />
            )}
            {mutation.isError && <ActionError />}
            {mutation.isSuccess && <ActionSuccess message={successMessage} />}
        </ActionDialog>
    );
}
