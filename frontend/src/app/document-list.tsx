import { Card, CardList, Section, SectionCard } from "@blueprintjs/core";
import { useLoaderData } from "@tanstack/react-router";
import { PropsWithChildren, ReactNode } from "react";
import { toElementApiPath } from "../api/path";
import { useMutation } from "@tanstack/react-query";
import { useOnshapeParams } from "./onshape-params";
import { apiPost } from "../api/api";
import { ElementType } from "../api/element-type";
import { ElementObj } from "../router";

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

    return cards;
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

    const onshapeParams = useOnshapeParams();

    const insertMutation = useMutation({
        mutationKey: ["insert", element.id],
        mutationFn: async () => {
            if (onshapeParams.elementType == ElementType.ASSEMBLY) {
                return apiPost(
                    "/add-to-assembly" + toElementApiPath(onshapeParams),
                    { body: element }
                );
            }

            return apiPost(
                "/add-to-part-studio" + toElementApiPath(onshapeParams),
                { body: element }
            );
        }
    });

    return (
        <Card interactive onClick={() => insertMutation.mutate()}>
            <span>{element.name}</span>
        </Card>
    );
}
