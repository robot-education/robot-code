import { ActionCard } from "../actions/action-card";
import { ActionInfo } from "../actions/action-context";

const actionInfo: ActionInfo = {
    title: "Push version",
    description:
        "Create a version and push it to one or more target documents.",
    route: "push-version"
};

export function PushVersionCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

export function PushVersion() {
    return null;
    // return (
    // <ActionDialog actionInfo={actionInfo}>
    //     <PushVersionForm />
    //     <ActionSpinner message="Pushing versions" />
    //     <ActionError />
    // </ActionDialog>
    // );
}
