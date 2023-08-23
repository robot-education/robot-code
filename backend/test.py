from library.api.utils import parse_query

query = {
    "queryType": "UNION",
    "subqueries": [
        {
            "disambiguationData": [
                {
                    "disambiguationType": "ORIGINAL_DEPENDENCY",
                    "originals": [
                        {
                            "entityType": "EDGE",
                            "historyType": "CREATION",
                            "operationId": ["Fb6.wireOp"],
                            "queryType": "SKETCH_ENTITY",
                            "sketchEntityId": "rGN",
                        }
                    ],
                }
            ],
            "entityType": "EDGE",
            "historyType": "CREATION",
            "isStart": "false",
            "operationId": ["FHCY.opExtrude"],
            "queryType": "CAP_EDGE",
        }
    ],
}

print(parse_query.parse_query(query))
