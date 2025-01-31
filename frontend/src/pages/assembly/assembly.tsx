import React from "react";
import { DocumentPath } from "../../api/path";
import { get } from "../../api/api";

export function Assembly(): JSX.Element {
    const handleClick = async () => {
        const documentPath: DocumentPath = {
            documentId: "ede0ee534bea0d64ab73636b"
        };
        console.log(get("/documents/" + documentPath.documentId));
    };

    return (
        <div className="flex flex-col items-center justify-center h-screen">
            <button
                className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
                onClick={handleClick}
            >
                Fetch Document Info
            </button>
        </div>
    );
}
