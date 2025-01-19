import { useMutation, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { useLoaderData } from "react-router-dom";
import { ActionCard } from "../../actions/action-card";
import { ActionInfo } from "../../actions/action-context";
import { ActionDialog } from "../../actions/action-dialog";
import { ActionError } from "../../actions/action-error";
import { ActionForm } from "../../actions/action-form";
import { ActionSpinner } from "../../actions/action-spinner";
import { ActionSuccess } from "../../actions/action-success";
import { post } from "../../api/api";
import { Workspace, WorkspacePath } from "../../api/path";
import { currentElementApiPath } from "../../app/onshape-params";
import { isVersionNameValid } from "../../common/version-utils";
import { ExecuteButton } from "../../components/execute-button";
import { VersionNameField } from "../../components/version-fields";
import { MutationProps } from "../../query/mutation";
import { linkedParentDocumentsKey } from "../../query/query-client";
import { FormGroup, HTMLSelect } from "@blueprintjs/core";
import { handleValueChange } from "../../common/handlers";

const actionInfo: ActionInfo = {
    title: "COTS climber",
    description:
        "Add a modifiable copy of a COTS telescoping climber from AndyMark or The Thrifty Bot to this document.",
    route: "climber"
};

export function ClimberCard() {
    return <ActionCard actionInfo={actionInfo} />;
}

enum ClimberType {
    ANDYMARK_TWO_STAGE,
    ANDYMARK_ONE_STAGE,
    THRIFTY_BOT_TWO_STAGE,
    THRIFTY_BOT_BASE_STAGE,
    THRIFTY_BOT_UPPER_STAGE
}

const ANDYMARK_DOCUMENT: WorkspacePath = {
    documentId: "f6b351288ae9e51981157206",
    instanceId: "5795c048d1be16a0d6c42fc0"
};

const THRIFTY_BOT_DOCUMENT: WorkspacePath = {
    documentId: "5f6bfa8b0d67209223f36096",
    instanceId: "e06c0e8950ea433637cdcdff"
};

function getClimberInfo(climberType: ClimberType): object {
    switch (climberType) {
        case ClimberType.ANDYMARK_ONE_STAGE:
            return Object.assign(
                {
                    elementsToExclude: [
                        "Two Stage Climber Parts",
                        "Two Stage Climber",
                        "am-4664 2IN Winch Kit",
                        "am-4662 2.0 to 1.5 Climber in a Box Bearing Assembly",
                        "am-4662 1.5 Nylon Bearing Clamp Sub Assembly"
                    ]
                },
                ANDYMARK_DOCUMENT
            );
        case ClimberType.ANDYMARK_TWO_STAGE:
            return Object.assign(
                {
                    elementsToExclude: [
                        "One Stage Climber Parts",
                        "One Stage Climber",
                        "am-4663 1.5IN Winch Kit",
                        "am-4667 1 Stage Climber in a Box.STEP",
                        "am-4667 1 Stage Climber in a Box parts"
                    ]
                },
                ANDYMARK_DOCUMENT
            );
        case ClimberType.THRIFTY_BOT_TWO_STAGE:
            return Object.assign(
                {
                    elementsToExclude: [
                        "Base Stage Climber Parts",
                        "Base Stage Climber",
                        "Upper Stage Climber Parts",
                        "Upper Stage Climber"
                    ]
                },
                THRIFTY_BOT_DOCUMENT
            );
        case ClimberType.THRIFTY_BOT_BASE_STAGE:
            return Object.assign(
                {
                    elementsToExclude: [
                        "Two Stage Climber Parts",
                        "Two Stage Climber",
                        "Upper Stage Climber Parts",
                        "Upper Stage Climber",
                        'TTB Telescope 1" End Block',
                        'TTB Telescope 1.5" Top Cap'
                    ]
                },
                THRIFTY_BOT_DOCUMENT
            );
        case ClimberType.THRIFTY_BOT_UPPER_STAGE:
            return Object.assign(
                {
                    elementsToExclude: [
                        "Two Stage Climber Parts",
                        "Two Stage Climber",
                        "Base Stage Climber Parts",
                        "Base Stage Climber",
                        'TTB Telescope 1.5" End Block',
                        'TTB Telescope 2" Top Cap'
                    ]
                },
                THRIFTY_BOT_DOCUMENT
            );
    }
}

interface ClimberArgs {
    versionName: string;
    climberType: ClimberType;
}

export function Climber() {
    const mutationFn = async (args: ClimberArgs) => {
        const body: any = getClimberInfo(args.climberType);
        body.versionName = args.versionName;
        return post("/copy-design" + currentElementApiPath(), { body });
    };
    const mutation = useMutation({
        mutationKey: [actionInfo.route],
        mutationFn
    });

    let actionSuccess = null;
    if (mutation.isSuccess) {
        actionSuccess = (
            <ActionSuccess
                message="Successfully added climber"
                // description={description}
            />
        );
    }

    return (
        <ActionDialog title={actionInfo.title} mutation={mutation}>
            {mutation.isIdle && <AddDesignForm mutation={mutation} />}
            {mutation.isPending && (
                <ActionSpinner message="Adding climber..." />
            )}
            {mutation.isError && <ActionError />}
            {actionSuccess}
        </ActionDialog>
    );
}

enum ClimberSupplier {
    ANDYMARK = "AndyMark",
    THRIFTY_BOT = "ThriftyBot"
}

enum AndymarkClimberStage {
    TWO = "2 stage",
    ONE = "1 stage"
}

enum ThriftyBotClimberStage {
    TWO = "2 stage",
    BASE = "Base stage",
    UPPER = "Upper stage"
}

function getClimberType(
    climberSupplier: ClimberSupplier,
    andymarkClimberStage: AndymarkClimberStage,
    thriftyBotClimberStage: ThriftyBotClimberStage
): ClimberType {
    switch (climberSupplier) {
        case ClimberSupplier.ANDYMARK:
            switch (andymarkClimberStage) {
                case AndymarkClimberStage.ONE:
                    return ClimberType.ANDYMARK_ONE_STAGE;
                case AndymarkClimberStage.TWO:
                    return ClimberType.ANDYMARK_TWO_STAGE;
            }
        case ClimberSupplier.THRIFTY_BOT:
            switch (thriftyBotClimberStage) {
                case ThriftyBotClimberStage.TWO:
                    return ClimberType.THRIFTY_BOT_TWO_STAGE;
                case ThriftyBotClimberStage.BASE:
                    return ClimberType.THRIFTY_BOT_BASE_STAGE;
                case ThriftyBotClimberStage.UPPER:
                    return ClimberType.THRIFTY_BOT_UPPER_STAGE;
            }
    }
}

function AddDesignForm(props: MutationProps) {
    const defaultName = useLoaderData() as string;
    const query = useQuery<Workspace[]>({ queryKey: linkedParentDocumentsKey });
    const [climberSupplier, setClimberSupplier] = useState(
        ClimberSupplier.ANDYMARK
    );
    const [andymarkClimberStage, setAndymarkClimberStage] = useState(
        AndymarkClimberStage.TWO
    );
    const [thriftyBotClimberStage, setThriftyBotClimberStage] = useState(
        ThriftyBotClimberStage.TWO
    );

    // Form fields and validation
    const [versionName, setVersionName] = useState(defaultName);
    const disabled = !isVersionNameValid(versionName);

    let climberStage;
    if (climberSupplier === ClimberSupplier.ANDYMARK) {
        climberStage = (
            <FormGroup label="Stages" labelFor="andymark-climber-stage">
                <HTMLSelect
                    id="andymark-climber-stage"
                    options={Object.values(AndymarkClimberStage)}
                    onChange={handleValueChange(setAndymarkClimberStage)}
                    value={andymarkClimberStage}
                />
            </FormGroup>
        );
    } else {
        climberStage = (
            <FormGroup label="Stages" labelFor="thrifty-bot-climber-stage">
                <HTMLSelect
                    id="thrifty-bot-climber-stage"
                    options={Object.values(ThriftyBotClimberStage)}
                    onChange={handleValueChange(setThriftyBotClimberStage)}
                    value={thriftyBotClimberStage}
                />
            </FormGroup>
        );
    }

    const climberField = (
        <>
            <FormGroup label="Supplier" labelFor="climber-supplier">
                <HTMLSelect
                    id="climber-supplier"
                    options={Object.values(ClimberSupplier)}
                    onChange={handleValueChange(setClimberSupplier)}
                    value={climberSupplier}
                />
            </FormGroup>
            {climberStage}
        </>
    );

    const executeButton = (
        <ExecuteButton
            loading={!disabled && query.isFetching}
            disabled={disabled}
            onSubmit={() => {
                props.mutation.mutate({
                    versionName,
                    climberType: getClimberType(
                        climberSupplier,
                        andymarkClimberStage,
                        thriftyBotClimberStage
                    )
                });
            }}
        />
    );
    return (
        <ActionForm
            description={actionInfo.description}
            actions={executeButton}
        >
            <VersionNameField
                versionName={versionName}
                onNameChange={setVersionName}
            />
            {climberField}
        </ActionForm>
    );
}
