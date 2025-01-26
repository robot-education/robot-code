import {
    Button,
    Card,
    CardList,
    EntityTitle,
    Icon,
    Intent,
    NonIdealState,
    NonIdealStateIconSize,
    Section,
    SectionCard,
    Spinner,
    Tooltip
} from "@blueprintjs/core";
import { AddLinkCard } from "./add-link-card";
import { useQuery } from "@tanstack/react-query";
import {
    isOpenableDocument,
    LinkedDocument,
    LinkType,
    LinkTypeProps
} from "./link-types";
import { DocumentOptionsMenu } from "./document-options-menu";
import { useState } from "react";
import { toInstanceApiPath } from "../api/path";
import { getLinkedDocumentsOptions } from "../query/query-client";

interface LinkedDocumentTitleProps {
    document: LinkedDocument;
}

function LinkedDocumentTitle(props: LinkedDocumentTitleProps): JSX.Element {
    const document = props.document;
    if (!isOpenableDocument(document)) {
        return (
            <Tooltip
                content="Failed to load document info. Do you have permission to access it?"
                minimal
            >
                <EntityTitle
                    title="Unknown Document"
                    icon={<Icon icon="error" intent={Intent.DANGER} />}
                />
            </Tooltip>
        );
    }
    return (
        <EntityTitle
            title={document.name}
            subtitle={document.workspaceName}
            icon="document"
        />
    );
}

function getDocumentCards(
    linkType: LinkType,
    documents: LinkedDocument[]
): JSX.Element {
    const cards = documents.map((document) => {
        return (
            <Card className="link-card" key={toInstanceApiPath(document)}>
                <LinkedDocumentTitle document={document} />
                <DocumentOptionsMenu
                    linkType={linkType}
                    workspacePath={document}
                />
            </Card>
        );
    });
    return <>{cards}</>;
}

interface LinkedDocumentsProps extends LinkTypeProps {
    title: string;
    subtitle: string;
}

export function LinkedDocumentsList(props: LinkedDocumentsProps) {
    const [isManuallyRefetching, setManuallyRefetching] = useState(false);
    const query = useQuery(getLinkedDocumentsOptions(props.linkType));
    let body;
    // Also show spinner when query is invalidated by reset button
    if (query.isPending || isManuallyRefetching) {
        body = (
            <Card style={{ justifyContent: "center" }}>
                <Spinner intent={"primary"} />
            </Card>
        );
    } else if (query.isSuccess) {
        body = getDocumentCards(props.linkType, query.data);
    } else if (query.isError) {
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

    return (
        <Section
            title={props.title}
            subtitle={props.subtitle}
            rightElement={resetButton}
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
