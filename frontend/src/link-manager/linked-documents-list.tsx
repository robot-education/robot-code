import {
    Button,
    Card,
    CardList,
    Icon,
    NonIdealState,
    NonIdealStateIconSize,
    Section,
    SectionCard,
    Spinner
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
    const cards = documents.map((document) => (
        <Card
            className="link-card"
            key={document.documentId + "/" + document.instanceId}
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

export function LinkedDocumentsList(props: LinkedDocumentsProps) {
    const [isManuallyRefetching, setManuallyRefetching] = useState(false);
    const query = useQuery<Workspace[]>({
        queryKey: linkedDocumentsKey(props.linkType)
    });

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
