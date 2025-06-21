import { Card, CardList, Section, SectionCard } from "@blueprintjs/core";
import { Outlet, useLoaderData, useNavigate } from "@tanstack/react-router";
import { PropsWithChildren, ReactNode } from "react";
import { ElementObj } from "../api/backend-types";

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
                    return <ElementCard key={element.id} element={element} />;
                })}
            </DocumentCard>
        );
    });

    return (
        <>
            {cards}
            <Outlet />
        </>
    );
}

interface DocumentCardProps extends PropsWithChildren {
    name: string;
}

function DocumentCard(props: DocumentCardProps): ReactNode {
    return (
        <Section collapsible title={props.name}>
            <SectionCard padded={false} style={{ maxHeight: "300px" }}>
                <CardList bordered>{props.children}</CardList>
            </SectionCard>
        </Section>
    );
}

interface ElementCardProps extends PropsWithChildren {
    element: ElementObj;
}

function ElementCard(props: ElementCardProps): ReactNode {
    const { element } = props;
    const navigate = useNavigate();

    return (
        <Card
            interactive
            onClick={() =>
                navigate({
                    to: "/app/documents/$elementId",
                    params: { elementId: element.id }
                })
            }
        >
            <span>{element.name}</span>
        </Card>
    );
}
