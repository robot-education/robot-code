

def fasten_mate(name: str, instance_id: str, mate_id: str, 
    target_instance_id: str, target_mate_id: str) -> object:
    return {
        'type' : 64,
        'version' : 2,
        'message' : {
            'featureType' : 'mate',
            'name' : name,
            'parameters' : [
                mate_type_parameter('FASTENED'),
                fasten_mate_parameter([
                    mate_query(target_instance_id, target_mate_id),
                    mate_query(instance_id, mate_id)
                ]),
                primary_axis_parameter()
            ]
        }
    }

def fasten_mate_parameter(queries: [object]) -> object:
    return {
        'type' : 67,
        'message' : {
            'parameterId' : 'mateConnectorsQuery',
            'queries' : queries
        }
    }

def mate_query(instance_id: str, mate_id: str) -> object: 
    return {
        'type' : 1324,
        'message' : {
            'featureId' : mate_id,
            'path' : [instance_id]
        }
    }

def mate_type_parameter(value: str) -> object:
    return {
        'type' : 145,
        'message' : {
            'parameterId' : 'mateType',
            'value' : value,
            'enumName' : 'Mate type'
        }
    }

def primary_axis_parameter() -> object:
    return {
        'type' : 144,
        'message' : {
            'parameterId' : 'primaryAxisAlignment',
            'value' : 'false'
        }
    }

def mate_connector() -> object:
    return {
        'type' : 66,
        'message' : {
            'featureType' : 'mateConnector',
            'name' : 'Mate connector',
            'parameters' : [
            #     {
            #     'type' : 67,
            #     'message' : {
            #         'queries' : [{
            #             'type' : 1083,
            #             'message' : {'inferenceType' : 'CENTROID', 'geometryIds' : [ 'JKW' ], 'path' : [ 'M420TZZdOPK8489yw' ]}
            #         }],
            #         'parameterId' : 'originQuery'
            #     }
            # }
            ]
        }
    }