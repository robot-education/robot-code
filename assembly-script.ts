export const parseBaseScript = `function(context is Context, args)
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
const toValue = function(value) returns string
precondition
{
    value is boolean || value is string || value is number || value is array || value is map;
}
{
    if (value is array)
    {
        return toArray(value);
    }
    else if (value is map)
    {
        return toMap(value);
    }
    else if (value is boolean || value is number)
    {
        return toString(value);
    }
    else if (value is string)
    {
        return '"' ~ value ~ '"';
    }
    throw regenError("Failed to load JSON.");
};
const toMap = function(arg is map) returns string
{
    var str = '{';
    var i = 0;
    for (var key, value in arg)
    {
        str ~= '"' ~ key ~ '" : ' ~ toValue(value);
        if (i != size(arg) - 1)
        {
            str ~= ',';
        }
        i += 1;
    }
    return str ~ '}';
};
const toArray = function(arg is array) returns string
{
    var str = '[';
    for (var i, value in arg)
    {
        str ~= toValue(value);
        if (i != size(arg) - 1)
        {
            str ~= ',';
        }
    }
    return str ~ ']';
};
const toJson = function(arg) returns string
precondition
{
    arg is map || arg is array;
}
{
    if (arg is map || arg is array)
    {
        return toValue(arg);
    }
    throw regenError("Failed to load JSON.");
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
            const parsed = match(attribute.url, ".*/(\\\\w+)/w/(\\\\w+)/e/(\\\\w+)");

            const id = lastModifyingOperationId(context, base);
            const mateId = parseId(resize(id, size(id) - 1));

            const value = {
                    "mateId" : mateId,
                    "documentId" : parsed.captures[1],
                    "workspaceId" : parsed.captures[2],
                    "elementId" : parsed.captures[3]
                };
            result["mates"] = append(result["mates"], value);
        }
    }
    print(toJson(result));
}`
export const parseTargetScript = `function(context is Context, args)
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
const toValue = function(value) returns string
precondition
{
    value is boolean || value is string || value is number || value is array || value is map;
}
{
    if (value is array)
    {
        return toArray(value);
    }
    else if (value is map)
    {
        return toMap(value);
    }
    else if (value is boolean || value is number)
    {
        return toString(value);
    }
    else if (value is string)
    {
        return '"' ~ value ~ '"';
    }
    throw regenError("Failed to load JSON.");
};
const toMap = function(arg is map) returns string
{
    var str = '{';
    var i = 0;
    for (var key, value in arg)
    {
        str ~= '"' ~ key ~ '" : ' ~ toValue(value);
        if (i != size(arg) - 1)
        {
            str ~= ',';
        }
        i += 1;
    }
    return str ~ '}';
};
const toArray = function(arg is array) returns string
{
    var str = '[';
    for (var i, value in arg)
    {
        str ~= toValue(value);
        if (i != size(arg) - 1)
        {
            str ~= ',';
        }
    }
    return str ~ ']';
};
const toJson = function(arg) returns string
precondition
{
    arg is map || arg is array;
}
{
    if (arg is map || arg is array)
    {
        return toValue(arg);
    }
    throw regenError("Failed to load JSON.");
};
    const mateConnector = qEverything(EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR)->qNthElement(0);
    const targetMateId = lastModifyingOperationId(context, mateConnector)[0];
    print(toJson({ "targetMateId" : targetMateId }));
}`
