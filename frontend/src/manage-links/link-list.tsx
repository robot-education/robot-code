import {
    Button,
    Card,
    CardList,
    Icon,
    InputGroup,
    Intent,
    Section,
    SectionCard
} from "@blueprintjs/core";
import { useLoaderData } from "react-router-dom";
import { Document } from "../api/path";
import { LinkedDocuments } from "./link-loader";
import { useState } from "react";

export function LinkList() {
    const linkedDocuments = useLoaderData() as LinkedDocuments;

    const parentDocuments = (
        <Section
            title="Parent documents"
            subtitle="Documents which use things created in this document."
            collapsible
        >
            <SectionCard padded={false}>
                <DocumentList documents={linkedDocuments.parentDocuments} />
            </SectionCard>
        </Section>
    );

    const childDocuments = (
        <Section
            title="Child documents"
            subtitle="Documents which created things used in this document."
            collapsible
        >
            <SectionCard padded={false}>
                <DocumentList documents={linkedDocuments.childDocuments} />
            </SectionCard>
        </Section>
    );

    return (
        <>
            {parentDocuments}
            <br />
            {childDocuments}
        </>
    );
}

interface DocumentListProps {
    documents: Document[];
}

function DocumentList(props: DocumentListProps): JSX.Element {
    const documentCards = props.documents.map((document) => {
        const closeButton = (
            <Button text="Delete" icon="cross" intent={Intent.DANGER} minimal />
        );
        return (
            <Card className="link-card" key={document.documentId}>
                <span>{document.name}</span>
                {closeButton}
            </Card>
        );
    });

    return (
        <CardList bordered={false}>
            {documentCards}
            <AddLinkCard />
        </CardList>
    );
}

function AddLinkCard() {
    const [url, setUrl] = useState("");
    const linkIcon = <Icon icon="link" />;
    return (
        <Card className="link-card" key="add">
            <InputGroup
                className="link-card-url-input"
                value={url}
                onValueChange={(value) => setUrl(value)}
                leftElement={linkIcon}
                placeholder="Document url"
                fill={true}
            />
            <Button text="Add" icon="add" minimal intent="primary" />
        </Card>
    );
}
