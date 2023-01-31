from apikey.onshape import Onshape, Path, ApiPath
from fs_constants import BACKEND_PATH
from AssemblyClient import AssemblyClient, Part
import os
import json
from typing import Optional

evaluate_script = '''
function(context is Context, args)
    { 
        const ASSEMBLY_ATTRIBUTE = "assemblyAttribute";
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

            if (first) {
                first = false;
            }
            else { 
                result ~= ", ";
            }

            const parsed = match(url, ".*/documents/(\\\\w+)/w/(\\\\w+)/e/(\\\\w+)");

            var id = lastModifyingOperationId(context, mateConnector);
            id = resize(id, size(id) - 1);
            var str = "";
            for (var i, comp in id)
            {
                str ~= comp;
                if (i != size(id) - 1)
                    str ~= ".";
            }

            result ~= '"' ~ str ~ '" : ';
            result ~= '{ "entity_id": "' ~ parsed.captures[1];
            result ~= '", "middle_id": "' ~ parsed.captures[2];
            result ~= '", "element_id": "' ~ parsed.captures[3];
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

def parse_assembly(assembly: object):
        parts = []
        for element in assembly['instances']:
            if not element['isStandardContent']:
                parts.append(Part(
                    element['documentId'], 
                    element['documentMicroversion'],
                    element['elementId'],
                    element['partId'])
                 )
        return parts

class MateConnector():
    def __init__(self, coord_system: object) -> None:
        self._coord_system  = coord_system

        self.target_path = None
        self.target_transform = None


    # def remove_unused_mate_connectors(self, features: [object]) -> bool:
    #     '''Returns true if the mate connector is used in features.'''
    #     for feature in features:
    #         if (feature['matedEntities'])
    
class PartMateConnectorMap():
    def __init__(self, part_studio_path: Path, mate_connectors: object) -> None:
        self.part_studio_path = part_studio_path
        self.mate_connectors = mate_connectors
    
    def remove_invalid_mate_connectors(self, instances: [object], features: [object]) -> None:
        for instance in instances:
            if instance['type'] != "Part" or self.part_studio_path.element_id != instance['elementId']:
                continue
            id = instance['id']
            for feature in features:
                if 'matedEntities' not in feature['featureData']:
                    continue

                for occurence in feature['featureData']['matedEntities']:
                    if occurence['matedOccurrence'] != id:
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

        path = Path(part['documentId'], part['documentMicroversion'], part['elementId'])
        path.middle_type = 'm'

        mate_connector_dict = {}
        for entity in part['mateConnectors']:
           mate_connector_dict[entity['featureId']] = MateConnector(entity['mateConnectorCS']) 

        part_map = PartMateConnectorMap(path, mate_connector_dict)
        part_map.remove_invalid_mate_connectors(instances, features)
        if part_map.is_valid():
            part_maps.append(part_map)
    return part_maps

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

    assembly_path = BACKEND_PATH.copy()
    assembly_path.element_id = 'bf30db7102f13da03c8c41fd'


    assembly = client.get_assembly(assembly_path)

    part_mate_maps = get_part_mate_maps(assembly)

    for part_mate_map in part_mate_maps:
        str_result = client.evaluate_feature_script(
            part_mate_map.part_studio_path, evaluate_script, part_mate_map.generate_queries())['console']
        dictionary = json.loads(str_result)
        for id, path in dictionary.items():
            if id not in part_mate_map.mate_connectors:
                continue
            target_path = Path(
                path['entity_id'],
                path['middle_id'],
                path['element_id'])
            part_mate_map.mate_connectors[id].target_path = target_path
            # client.add_part_studio_to_assembly(assembly_path, target_path)
            print(client.add_fasten_mate(assembly_path))

    # delete mates without valid target_paths


    # iterate over instances and add part_mate_maps as needed
    # create fasten mates


            
    # features = client.get_assembly_features(assembly_path)
    # body = { 'script' : code }
    # partStudioPath.eid = 'c481a23972b3b7259bd9758b'
    # result = api.request('post', ApiPath('partstudios', partStudioPath, 'featurescript'), body=body).json()
    # print(result['result'])

if __name__ == '__main__':
    main()