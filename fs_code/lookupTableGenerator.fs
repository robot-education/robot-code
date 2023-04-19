FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");
import(path : "472bc4c291e1d2d6f9b98937", version : "a912ae70fed2318ae5be227c");

/**
 * A type defining a single Node of a lookup table.
 * A lookup table can be generated from an array of lookup Nodees using recursion.
 * At each step, each lookup Node's value is appened to the value.
 * When a leaf is reached (as denoted by a `LookupPayload` with `next` equal to `undefined`), the final value is printed.
 */
export type LookupNode typecheck canBeLookupNode;

export predicate canBeLookupNode(value)
{
    value is map;
    value.name is string;
    value.displayName is string;
    value["default"] is string;

    value.entries is array;
    for (var entry in value.entries)
    {
        canBeLookupPayload(entry);
    }

    isIn(value["default"], extractFromArrayOfMaps(value.entries, "key"));
}

/**
 * @param name {string} : @autocomplete `"name"`
 * @param displayName {string} : @autocomplete `"displayName"`
 * @param defaultEntry {string} : @autocomplete `"default"`
 * @param entries {array} :
 *      @autocomplete ```[
 *                  lookupPayload("key", {})
 *               ]```
 */
export function lookupNode(name is string, displayName is string, defaultEntry is string, entries is array) returns LookupNode
precondition
{
    for (var entry in entries)
    {
        canBeLookupPayload(entry);
    }
}
{
    return { "name" : name, "displayName" : displayName, "default" : defaultEntry, "entries" : entries } as LookupNode;
}

/**
 * @type {{
 *      @field key {string} :
 *          The name of this payload. Is visible to the user at runtime.
 *      @field value {map} :
 *          The value of this lookup value, which is added on to the final terminal value.
 *      @field next {LookupNode} :
 *          The next lookup node in the tree. If `undefined`, this Node is a leaf, and the terminal value
 *          is automatically computed.
 *          If `next` is a `string`, the value of the `string` is used as the terminal value.
 * }}
 */
export type LookupPayload typecheck canBeLookupPayload;

export predicate canBeLookupPayload(value)
{
    value is map;
    value.key is string;
    value.value is map;
    canBeLookupNode(value.next) || value.next is string || value.next is undefined;
}

export function lookupPayload(key is string, value is map) returns LookupPayload
{
    return { "key" : key, "value" : value } as LookupPayload;
}

export function lookupPayload(key is string, value is map, next is LookupNode) returns LookupPayload
{
    return { "key" : key, "value" : value, "next" : next } as LookupPayload;
}

export function lookupPayload(key is string, value is map, next is string) returns LookupPayload
{
    return { "key" : key, "value" : value, "next" : next } as LookupPayload;
}

export function setNextNode(lookupNode is LookupNode, next is LookupNode) returns LookupNode
{
    lookupNode.entries = mapArray(lookupNode.entries, function(entry is LookupPayload) returns LookupPayload
        {
            return mergeMaps(entry, { "next" : next });
        });
    return lookupNode;
}

/**
 * Prints a table defined by a root `LookupNode`.
 */
export function printTable(tableName is string, root is LookupNode, exportVariable is boolean)
{
    if (exportVariable)
    {
        print("export ");
    }

    print("const " ~ tableName ~ " = {");
    printLookupNode(root, {});
    println("};");
    println("");
}

export function printTable(tableName is string, root is LookupNode)
{
    return printTable(tableName, root, true);
}

/**
 * Prints a node and all its children.
 */
function printLookupNode(lookupNode is LookupNode, value is map)
precondition
{
    canBeLookupNode(lookupNode);
}
{
    println("");
    println('\"name\" : \"' ~ lookupNode.name ~ '\",');
    println('\"displayName\" : \"' ~ lookupNode.displayName ~ '\",');
    println('\"default\" : \"' ~ lookupNode["default"] ~ '\",');
    println('\"entries\" : {');
    for (var entry in lookupNode.entries)
    {
        print('\"' ~ entry.key ~ '\" : ');

        if (entry.next is LookupNode || entry.next is undefined)
        {
            print('{');
            printLookupNode(entry.next, mergeMaps(entry.value, value));
            println('},');
        }
        else if (entry.next is string)
        {
            print(entry.next);
            println(',');
        }
        else
        {
            throw regenError("Invalid entry in lookup node. Expected string, LookupNode, or undefined, recieved: " ~ entry.next);
        }
    }
    println('},');
}

/**
 * Prints a terminal node.
 */
function printLookupNode(lookupNode is undefined, value is map)
{
    for (var key, val in value)
    {
        print('\"' ~ key ~ '\" : ' ~ val ~ ', ');
    }
}
