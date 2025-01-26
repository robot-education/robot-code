import { Callout } from "@blueprintjs/core";
import { ActionCard } from "../../actions/action-card";
import { ActionInfo } from "../../actions/action-context";
import { ActionForm } from "../../actions/action-form";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ActionDialog } from "../../actions/action-dialog";
import { post } from "../../api/api";
import { currentInstanceApiPath } from "../../app/onshape-params";
import { ActionError } from "../../actions/action-error";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { ExecuteButton } from "../../components/execute-button";
import { WorkspacePath } from "../../api/path";
import { OpenLinkManagerButton } from "../../components/manage-links-button";
import { OnSubmitProps } from "../../common/handlers";
import { getLinkedDocumentsOptions } from "../../query/query-client";
import { LinkType } from "../../link-manager/link-types";

const actionInfo: ActionInfo = {
    title: "Update child references",
    description:
        "Update all outdated references to children to the latest version.",
    route: "update-child-references"
};

export function UpdateChildReferencesCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

interface UpdateChildReferencesArgs {
    instancePaths: WorkspacePath[];
}

export function UpdateChildReferences() {
    const mutationFn = async (args: UpdateChildReferencesArgs) => {
        return post("/update-references" + currentInstanceApiPath(), {
            body: {
                childDocumentIds: args.instancePaths
            }
        }).then((response) => response.updatedElements);
    };

    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    let successMessage = "";
    if (mutation.isSuccess) {
        if (mutation.data > 0) {
            successMessage = `Successfully updated outdated references in ${mutation.data} tabs`;
        } else {
            successMessage = "No outdated references were found";
        }
    }

    return (
        <ActionDialog title={actionInfo.title} isPending={mutation.isPending}>
            {mutation.isIdle && (
                <UpdateChildReferencesForm onSubmit={mutation.mutate} />
            )}
            {mutation.isPending && (
                <ActionSpinner message="Creating and pushing version..." />
            )}
            {mutation.isError && <ActionError />}
            {mutation.isSuccess && <ActionSuccess message={successMessage} />}
        </ActionDialog>
    );
}

function UpdateChildReferencesForm(
    props: OnSubmitProps<UpdateChildReferencesArgs>
) {
    const query = useQuery(getLinkedDocumentsOptions(LinkType.CHILDREN));

    let noChildrenCallout = null;
    if (query.isSuccess && query.data.length == 0) {
        noChildrenCallout = (
            <>
                <Callout title="No linked child documents" intent="warning">
                    <p>
                        This document doesn't have any linked children to update
                        from.
                    </p>
                    <OpenLinkManagerButton minimal={false} />
                </Callout>
                <br />
            </>
        );
    }

    const actions = (
        <ExecuteButton
            loading={query.isFetching}
            onSubmit={() =>
                props.onSubmit({
                    instancePaths: query.data ?? []
                })
            }
        />
    );

    return (
        <ActionForm description={actionInfo.description} actions={actions}>
            {noChildrenCallout}
        </ActionForm>
    );
}
