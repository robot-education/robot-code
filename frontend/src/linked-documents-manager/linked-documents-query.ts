import { Document } from "../api/path";

export interface LinkedDocuments {
    parentDocuments: Document[];
    childDocuments: Document[];
}

export async function linkedDocumentsQuery(): Promise<LinkedDocuments> {
    // Call get links with current document path
    return {
        parentDocuments: [
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "Other document",
                documentId: "124adf1412",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "Third document",
                documentId: "1124tasdf",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            }
        ],
        childDocuments: [
            {
                name: "My document",
                documentId: "afafafafsd",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            },
            {
                name: "Other document",
                documentId: "124adf1412",
                workspaceId: "fadfdafadf",
                workspaceOrVersion: "w"
            }
        ]
    };
}
