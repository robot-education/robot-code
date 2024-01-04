import { Card, CardList, Section, SectionCard } from "@blueprintjs/core";
import { useLoaderData } from "react-router-dom";
import { Document } from "../api/path";
import { LinkedDocuments } from "./linked-documents-query";
import { DocumentOptionsMenu } from "./document-options-menu";
import { AddLinkCard } from "./add-link-card";
import { ReactNode } from "react";

function getDocumentCards(documents: Document[]): JSX.Element[] {
    return documents.map((document) => {
        return (
            <Card className="link-card" key={document.documentId}>
                <span>{document.name}</span>
                <DocumentOptionsMenu documentPath={document} />
            </Card>
        );
    });
}

export function LinkedDocumentsList() {
    const linkedDocuments = useLoaderData() as LinkedDocuments;

    const parentDocuments = (
        <LinkedDocumentsSection
            title="Parent documents"
            subtitle="Documents which use things created in this document."
        >
            {getDocumentCards(linkedDocuments.parentDocuments)}
        </LinkedDocumentsSection>
    );

    const childDocuments = (
        <LinkedDocumentsSection
            title="Child documents"
            subtitle="Documents which created things used in this document."
        >
            {getDocumentCards(linkedDocuments.childDocuments)}
        </LinkedDocumentsSection>
    );

    return (
        <>
            {parentDocuments}
            <br />
            {childDocuments}
        </>
    );
}

interface LinkedDocumentsProps {
    title: string;
    subtitle: string;
    children: ReactNode;
}

function LinkedDocumentsSection(props: LinkedDocumentsProps) {
    return (
        <Section title={props.title} subtitle={props.subtitle} collapsible>
            <SectionCard padded={false}>
                <CardList bordered={false} style={{ maxHeight: "200px" }}>
                    {props.children}
                    <AddLinkCard />
                </CardList>
            </SectionCard>
        </Section>
    );
}
