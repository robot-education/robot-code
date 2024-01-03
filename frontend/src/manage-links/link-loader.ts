import { Document } from "../api/path";

export interface LinkedDocuments {
    parentDocuments: Document[];
    childDocuments: Document[];
}

export async function loadLinks(): Promise<LinkedDocuments> {
    // Call get links with current document path
    return {
        parentDocuments: [
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf"
            },
            {
                name: "Other document",
                documentId: "124adf1412",
                workspaceId: "fadfdafadf"
            },
            {
                name: "Third document",
                documentId: "1124tasdf",
                workspaceId: "fadfdafadf"
            }
        ],
        childDocuments: [
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf"
            },
            {
                name: "Other document",
                documentId: "124adf1412",
                workspaceId: "fadfdafadf"
            }
        ]
    };
}
