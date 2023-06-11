function(context is Context, args)
{
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
const parseMateConnectorId = function(context is Context, query is Query) returns string
{
    const id = lastModifyingOperationId(context, query);
    return parseId(resize(id, size(id) - 1));
};
const toJson = function(arg) returns string
    precondition
    {
        arg is map || arg is array;
    }
    {
        const toJsonImplementation = function(arg, recurse) returns string
            {
                if (arg is map)
                {
                    var str = '{';
                    var i = 0;
                    for (var key, value in arg)
                    {
                        str ~= '"' ~ key ~ '" : ' ~ recurse(value, recurse);
                        if (i != size(arg) - 1)
                        {
                            str ~= ',';
                        }
                        i += 1;
                    }
                    return str ~ '}';
                }
                else if (arg is array)
                {
                    var str = '[';
                    for (var i, value in arg)
                    {
                        str ~= recurse(value, recurse);
                        if (i != size(arg) - 1)
                        {
                            str ~= ',';
                        }
                    }
                    return str ~ ']';
                }
                else if (arg is boolean || arg is number)
                {
                    return toString(arg);
                }
                else if (arg is string)
                {
                    return '"' ~ arg ~ '"';
                }
                throw regenError("Failed to load JSON.");
            };
        return toJsonImplementation(arg, toJsonImplementation);
    };
    const ASSEMBLY_ATTRIBUTE = "assemblyAttribute";

    const bases = evaluateQuery(context, qHasAttribute(ASSEMBLY_ATTRIBUTE));

    var result = {
        "valid" : size(bases) > 0,
        "mates" : [],
        "mirrors" : []
    };
    for (var base in bases)
    {
        const attribute = getAttribute(context, {
                    "entity" : base,
                    "name" : ASSEMBLY_ATTRIBUTE
                });

        if (attribute["type"] as string == "MATE")
        {
            const parsed = match(attribute.url, ".*/(\\w+)/w/(\\w+)/e/(\\w+)");

            const value = {
                    "mateId" : parseMateConnectorId(context, base),
                    "documentId" : parsed.captures[1],
                    "workspaceId" : parsed.captures[2],
                    "elementId" : parsed.captures[3]
                };
            result.mates = append(result.mates, value);
        }
        else if (attribute["type"] as string == "MIRROR")
        {
            var value = {
                "endMateId" : parseMateConnectorId(context, base),
                "mateToOrigin" : attribute.mateToOrigin
            };
            
            if (!attribute.mateToOrigin)
            {
                value.startMateId = parseMateConnectorId(context, attribute.startMate);
            }

            result.mirrors = append(result.mirrors, value);
        }
    }
    print(toJson(result));
}