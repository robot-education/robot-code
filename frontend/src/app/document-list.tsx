import { Card, CardList, Section, SectionCard } from "@blueprintjs/core";
import { useLoaderData } from "@tanstack/react-router";
import { PropsWithChildren, ReactNode } from "react";

export function DocumentList(): ReactNode {
    const data = useLoaderData({ from: "/app/documents" });

    const cards = data.documents.map((document) => {
        return (
            <DocumentCard key={document.id} name={document.name}>
                {document.elementIds.map((elementId) => {
                    const element = data.elements.find(
                        (element) => element.id === elementId
                    );
                    if (!element) {
                        return null;
                    }
                    return (
                        <Card key={element.id} interactive>
                            <span>{element.name}</span>
                        </Card>
                    );
                })}
            </DocumentCard>
        );
    });

    return cards;
}

interface DocumentCardProps extends PropsWithChildren {
    name: string;
}

function DocumentCard(props: DocumentCardProps): ReactNode {
    // const [isOpen, setIsOpen] = useState(false);
    return (
        <Section collapsible title={props.name}>
            <SectionCard padded={false} style={{ maxHeight: "300px" }}>
                <CardList bordered>{props.children}</CardList>
            </SectionCard>
        </Section>
    );
}
