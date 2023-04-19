FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");


const UPPER_CASE_LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'];
const LOWER_CASE_LETTERS = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

export function toLowerCase(s) returns string
{
    for (var i = 0; i < 26; i += 1)
    {
        s = replace(s, UPPER_CASE_LETTERS[i], LOWER_CASE_LETTERS[i]);
    }
    return s;
}