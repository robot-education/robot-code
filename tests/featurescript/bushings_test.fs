FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "bushings/bushings.fs", version : "");
import(path : "testing.fs", version : "");

// Every bushing the set makes, through every level
function made(level is map) returns array
{
    var found = [];
    for (var _, entry in level)
    {
        found = append(found, entry.outerDiameter == undefined ? made(entry) : [entry]);
    }
    return concatenateArrays(found);
}

export function testEveryBushingsProfileCloses()
{
    for (var bushing in made(BUSHINGS))
    {
        const points = bushingProfile(bushing);
        expectEqual(points[0], points[size(points) - 1]);
        // Its flange is above the origin, as wide as its flange and as thick
        expectNear(points[0][1], bushing.flangeThickness - bushing.width, 1e-12 * meter);
        expectTrue(bushing.flangeDiameter > bushing.outerDiameter, bushing.name ~ "'s flange isn't wider than it");
    }
}
