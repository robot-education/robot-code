import {
    Button,
    Card,
    CardList,
    Section,
    SectionCard,
    Spinner
} from "@blueprintjs/core";
import { Document } from "../api/path";
import { AddLinkCard } from "./add-link-card";
import { useQuery } from "@tanstack/react-query";
import { get } from "../api/api";
import { selectApiDocumentPath } from "../app/onshape-params-slice";
import { useAppSelector } from "../app/hooks";
import { LinkType, LinkTypeProps } from "./document-link-type";
import { DocumentOptionsMenu } from "./document-options-menu";
import { NonIdealStateOverride } from "../components/non-ideal-state-override";
import { queryClient } from "../query/query-client";
import { showInfoToast } from "../app/toaster";

function getDocumentCards(
    linkType: LinkType,
    documents: Document[]
): JSX.Element[] {
    return documents.map((document) => {
        return (
            <Card className="link-card" key={document.documentId}>
                <span>{document.name}</span>
                <DocumentOptionsMenu
                    linkType={linkType}
                    documentPath={document}
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
    const apiPath = useAppSelector(selectApiDocumentPath);
    const queryFn = async (): Promise<Document[]> => {
        console.log("Call backend");
        // Call get links with current document path
        const result = await get(
            `/linked-documents/${props.linkType}` + apiPath
        );
        return result.documents;
    };
    const query = useQuery({
        queryKey: ["linked-documents", props.linkType],
        queryFn
    });

    let body;
    // Also show spinner when query is invalidated by reset button
    if (query.isPending || query.isRefetching) {
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
            <NonIdealStateOverride
                title="Failed to load linked documents."
                description="If the problem persits, contact Alex."
                iconIntent="danger"
                icon="cross"
            />
        );
    }

    const resetButton = (
        <Button
            icon="repeat"
            minimal
            onClick={async (event) => {
                event.stopPropagation();
                await queryClient.resetQueries({
                    queryKey: ["linked-documents", props.linkType]
                });
                showInfoToast("Refreshed documents.");
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
