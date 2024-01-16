import {
    Button,
    Card,
    CardList,
    Classes,
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
import { Cross, Repeat } from "@blueprintjs/icons";

function getDocumentCards(
    linkType: LinkType,
    documents: Workspace[]
): JSX.Element[] {
    return documents.map((document) => {
        return (
            <Card className="link-card" key={document.documentId}>
                <span>{document.name}</span>
                <DocumentOptionsMenu
                    linkType={linkType}
                    instancePath={document}
                />
            </Card>
        );
    });
}

interface LinkedDocumentsProps extends LinkTypeProps {
    title: string;
    subtitle: string;
}

export function LinkedDocumentsList(props: LinkedDocumentsProps) {
    const [isManuallyRefetching, setManuallyRefetching] = useState(false);
    const query = useQuery<Workspace[]>({
        queryKey: ["linked-documents", props.linkType]
    });

    let body;
    // Also show spinner when query is invalidated by reset button
    if (query.isPending || isManuallyRefetching) {
        // Could also render some dummy cards wrapped in a skeleton
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
                    <Cross
                        className={Classes.INTENT_DANGER}
                        size={NonIdealStateIconSize.STANDARD}
                    />
                }
                iconMuted={false}
            />
        );
    }

    const resetButton = (
        <Button
            icon={<Repeat />}
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
