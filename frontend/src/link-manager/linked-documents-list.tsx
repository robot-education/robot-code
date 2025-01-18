import {
    Button,
    Card,
    CardList,
    Icon,
    IconSize,
    Intent,
    NonIdealState,
    NonIdealStateIconSize,
    Section,
    SectionCard,
    Spinner,
    Tooltip
} from "@blueprintjs/core";
import { Workspace } from "../api/path";
import { AddLinkCard } from "./add-link-card";
import { useQuery } from "@tanstack/react-query";
import { LinkType, LinkTypeProps } from "./document-link-type";
import { DocumentOptionsMenu } from "./document-options-menu";
import { useState } from "react";
import { linkedDocumentsKey } from "../query/query-client";

function getDocumentCards(
    linkType: LinkType,
    documents: Workspace[]
): JSX.Element {
    console.log(documents);
    const cards = documents.map((document) => (
        <Card
            className="link-card"
            key={document.documentId + "|" + document.instanceId}
        >
            <span>{document.name}</span>
            <DocumentOptionsMenu linkType={linkType} workspacePath={document} />
        </Card>
    ));
    return <>{cards}</>;
}

interface LinkedDocumentsProps extends LinkTypeProps {
    title: string;
    subtitle: string;
}

interface LinkedDocumentsResult {
    documents: Workspace[];
    invalidLinks?: number;
}

export function LinkedDocumentsList(props: LinkedDocumentsProps) {
    const [isManuallyRefetching, setManuallyRefetching] = useState(false);
    const query = useQuery<LinkedDocumentsResult>({
        queryKey: linkedDocumentsKey(props.linkType)
    });

    let body;
    let invalidLinks = undefined;
    // Also show spinner when query is invalidated by reset button
    if (query.isPending || isManuallyRefetching) {
        body = (
            <Card style={{ justifyContent: "center" }}>
                <Spinner intent={"primary"} />
            </Card>
        );
    } else if (query.isSuccess) {
        invalidLinks = query.data.invalidLinks;
        body = getDocumentCards(props.linkType, query.data.documents);
    } else if (query.isError) {
        // if (query.error instanceof MissingPermissionError) {
        //     const error = query.error;
        //     if (error.documentName) {
        //         description = `You do not have ${error.permission} access to ${error.documentName}.`;
        //     } else {
        //         description = `You do not have ${error.permission} access to all of the linked documents.`;
        //     }
        // }
        body = (
            <NonIdealState
                title="Failed to load linked documents."
                description="If the problem persits, contact Alex."
                icon={
                    <Icon
                        icon="cross"
                        intent="danger"
                        size={NonIdealStateIconSize.STANDARD}
                    />
                }
                iconMuted={false}
            />
        );
    }

    const resetButton = (
        <Button
            icon="repeat"
            minimal
            onClick={async (event) => {
                event.stopPropagation();
                setManuallyRefetching(true);
                await query.refetch();
                setManuallyRefetching(false);
            }}
        />
    );

    let invalidLinksAlert = null;
    if (invalidLinks) {
        invalidLinksAlert = (
            <Tooltip>
                <Icon
                    icon="warning-sign"
                    size={IconSize.LARGE}
                    intent={Intent.WARNING}
                />
            </Tooltip>
        );
    }

    const rightElement = (
        <>
            {invalidLinksAlert}
            {resetButton}
        </>
    );

    return (
        <Section
            title={props.title}
            subtitle={props.subtitle}
            rightElement={rightElement}
            collapsible
            collapseProps={{ defaultIsOpen: true }}
        >
            <SectionCard padded={false}>
                <CardList bordered={false} style={{ maxHeight: "200px" }}>
                    {body}
                    <AddLinkCard linkType={props.linkType} />
                </CardList>
            </SectionCard>
        </Section>
    );
}
