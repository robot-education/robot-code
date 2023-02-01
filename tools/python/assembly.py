from apikey.onshape import Onshape, Path, ApiPath
from fs_constants import BACKEND_PATH
from AssemblyClient import AssemblyClient, Part
from assembly_feature import fasten_mate

import os
import json
from typing import Optional

parse_target = '''
function(context is Context, args)
{
    const mateConnector = qEverything(EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR)->qNthElement(0);

    // mate feature's id is targetMateId
    const targetMateId = lastModifyingOperationId(context, mateConnector)[0];

    // part id is first part of id
    // evaluateQuery?
    // const targetPartId = lastModifyingOperationId(context, part)[0];

    print('{ "targetMateId" : "' ~ targetMateId ~ '" }');
}
'''

parse_part = '''
function(context is Context, args)
{ 
    const ASSEMBLY_ATTRIBUTE = "assemblyAttribute";
    const parseId = function(id is Id) returns string 
        {
            var result = "";
            for (var i, comp in id)
            {
                result ~= comp;
                if (i != size(id) - 1)
                    result ~= ".";
            }
            return result;
        };

    var result = '{';
    const mateConnectors = evaluateQuery(context, qEverything(EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR)->qHasAttribute(ASSEMBLY_ATTRIBUTE));
    var first = true;
    for (var mateConnector in mateConnectors)
    {
        const url = getAttribute(context, {
                    "entity" : mateConnector,
                    "name" : ASSEMBLY_ATTRIBUTE
                });
        if (url == undefined) { continue; }

        if (first) { first = false; }
        else { result ~= ", "; }

        const parsed = match(url, ".*/documents/(\\\\w+)/w/(\\\\w+)/e/(\\\\w+)");

        const id = lastModifyingOperationId(context, mateConnector);
        const mateId = parseId(resize(id, size(id) - 1));

        result ~= '"' ~ mateId ~ '" : ';
        result ~= '{ "entityId": "' ~ parsed.captures[1];
        result ~= '", "middleId": "' ~ parsed.captures[2];
        result ~= '", "elementId": "' ~ parsed.captures[3];
        result ~= '" }';
    }
    print(result ~ '}');
}
'''

def write_to_file(file_name: str, content: str, file_path: Optional[str] = None) -> None:
    path = os.path.join(file_path, file_name) if file_path is not None else file_name
    file = open(path, 'w', encoding='utf-8')
    file.write(content)
    file.close()

class MateConnector():
    def __init__(self, coord_system: object) -> None:
        # used to check if mate is used
        self._coord_system  = coord_system
        # filled in by featurescript. Represents target part
        self.target_path = None
        self.target_part_id = None
    
class PartMateConnectorMap():
    def __init__(self, part_studio_path: Path, part_id: str, mate_connectors: dict[str, MateConnector]) -> None:
        # a dict mapping mate_ids to MateConnectors
        self.mate_connectors = mate_connectors
        # used for featurescript target evaluation
        self.part_studio_path = part_studio_path
        # used to match instances to self
        self.part_id = part_id
    
    def remove_invalid_mate_connectors(self, instances: [object], features: [object]) -> None:
        for instance in instances:
            if instance['type'] != "Part" or self.part_studio_path.element_id != instance['elementId']:
                continue
            instance_id = instance['id']
            for feature in features:
                if 'matedEntities' not in feature['featureData']:
                    continue

                for occurence in feature['featureData']['matedEntities']:
                    if occurence['matedOccurrence'] != instance_id:
                        continue
                    for mate_connector in self.mate_connectors.values(): 
                        if mate_connector.coord_system == occurence['matedCS']:
                            self.mate_connectors.pop(mate_connector)
                            break

    def is_valid(self) -> bool:
        return bool(self.mate_connectors) # true if dict is not empty
    
    def generate_queries(self) -> [object]:
        '''Returns an array of feature_ids'''
        queries = []
        for mate_id in self.mate_connectors:
            queries.append({ 'id': mate_id })
        return queries
    
    def delete_mates_missing_targets(self):
        self.mate_connectors = {mate_id: mate_connector for mate_id, mate_connector in self.mate_connectors.items() if mate_connector.target_path is not None}
        # for mate_id, mate_connector in self.mate_connectors.items():
        #     if mate_connector.target_path is None:
        #         self.mate_connectors.pop(mate_id)
    
    def instance_matches(self, instance: object) -> bool:
        return instance['partId'] == self.part_id and instance['documentId'] == self.part_studio_path.document_id and instance['elementId'] == self.part_studio_path.element_id

'''
Collect a mapping of parts to mate connectors by looping through parts.
(Optional) Cross reference each mate connector against the list of mate features and occurences,
excluding those that are used everywhere they exist.

Remove any parts without valid mate connectors.

For each part, check the given mate connectors for attributes.
If an attribute doesn't exist, remove the mate connector from the part map.
Otherwise, store target information alongside the mate connector.

Remove any parts without valid attributes.

Loop through all instances. For each instance, find the corresponding part to mate connector map. For each valid
mate connector, check if the mate connector is unused for the given instance; if it is, use the attribute
to add a target instance to the assembly.
'''

def get_part_mate_maps(assembly: object) -> [PartMateConnectorMap]:
    part_maps = []
    instances = assembly['rootAssembly']['instances']
    features = assembly['rootAssembly']['features']
    for part in assembly['parts']:
        if 'mateConnectors' not in part:
            continue

        mate_connector_dict = {}
        for mate_entity in part['mateConnectors']:
           mate_connector_dict[mate_entity['featureId']] = MateConnector(mate_entity['mateConnectorCS']) 

        path = Path(part['documentId'], part['documentMicroversion'], part['elementId'])
        path.middle_type = 'm'

        part_map = PartMateConnectorMap(path, part['partId'], mate_connector_dict)
        part_map.remove_invalid_mate_connectors(instances, features)
        if part_map.is_valid():
            part_maps.append(part_map)
    return part_maps

def add_instances_to_assembly(client: AssemblyClient, assembly_path: str, assembly: object,
        part_mate_maps: [PartMateConnectorMap], target_mate_connectors: dict[str, str]) -> None:

        for instance in assembly['rootAssembly']['instances']:
            instance_id = instance['id']
            map = None
            for part_mate_map in part_mate_maps:
                if part_mate_map.part_studio_path.element_id == instance['elementId']:
                    map = part_mate_map
                    break

            if map is None:
                continue
        
            for mate_id, mate_connector in map.mate_connectors.items():
                if mate_id not in target_mate_connectors:
                    continue

                target_instance_id = client.add_part_studio_to_assembly(assembly_path, mate_connector.target_path)
                target_mate_id = target_mate_connectors[mate_id]

                mate = fasten_mate('Auto mate', instance_id, mate_id,
                    target_instance_id, target_mate_id)
                client.add_assembly_feature(assembly_path, mate)

'''
Workflow:
    1. Get all parts in assembly
    2. For each part, evaluate featurescript to get target mate connectors and part studios
    3. Add each target part studio to the assembly
    4. Transform each target part studio according to the first mate connector in it
    5. Fasten each part studio to its corresponding target mate connector
'''
def main(): 
    api = Onshape(logging=False)
    client = AssemblyClient(api)

    assembly_path = BACKEND_PATH
    assembly_path.element_id = 'abf33310eecb1c0ecb7f73c7'

    assembly = client.get_assembly(assembly_path)
    part_mate_maps = get_part_mate_maps(assembly)

    # A dictionary mapping mate_connector ids to target_mate_connector_ids
    target_mate_connectors = {}
    for part_mate_map in part_mate_maps:
        json_str = client.evaluate_feature_script(part_mate_map.part_studio_path, parse_part)['console']
        dictionary = json.loads(json_str)
        for mate_id, path in dictionary.items():
            if mate_id not in part_mate_map.mate_connectors:
                continue
            target_path = Path(
                path['entityId'],
                path['middleId'],
                path['elementId'])
            part_mate_map.mate_connectors[mate_id].target_path = target_path
            if mate_id in target_mate_connectors:
                continue
            result = client.evaluate_feature_script(target_path, parse_target)

            dictionary = json.loads(result['console'])
            target_mate_connectors[mate_id] = dictionary['targetMateId']
        
    # delete mates without valid target_paths
    for part_mate_map in part_mate_maps:
        part_mate_map.delete_mates_missing_targets()
    
    part_mate_maps = [x for x in part_mate_maps if x.is_valid()]

    # iterate over instances and add part_mate_maps as needed
    # create fasten mates
    add_instances_to_assembly(client, assembly_path, assembly, part_mate_maps, target_mate_connectors)

    # features = client.get_assembly_features(assembly_path)

if __name__ == '__main__':
    main()