FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

export enum ReferenceType
{
    annotation { "Name" : "Origin" }
    ORIGIN,
    annotation { "Name" : "Mate connector" }
    MATE_CONNECTOR,
    annotation { "Name" : "Plane point" }
    PLANE_POINT
}

annotation { "Feature Type Name" : "Assembly align" }
export const assemblyAlign = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // This seems like it should work, but it's actually very bad!
        annotation { "Name" : "First body to align", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES, "MaxNumberOfPicks" : 1 }
        definition.firstBodyToAlign is Query;

        annotation { "Name" : "Second body to align", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES, "MaxNumberOfPicks" : 1 }
        definition.secondBodyToAlign is Query;

        annotation { "Name" : "Reference type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.referenceType is ReferenceType;

        if (definition.referenceType == ReferenceType.MATE_CONNECTOR)
        {
            annotation { "Name" : "Mate connector", "Filter" : BodyType.MATE_CONNECTOR, "MaxNumberOfPicks" : 1 }
            definition.mateConnector is Query;
        }
        else if (definition.referenceType == ReferenceType.PLANE_POINT)
        {
            annotation { "Name" : "Plane",
                        "Filter" : GeometryType.PLANE,
                        "MaxNumberOfPicks" : 1 }
            definition.plane is Query;

            annotation { "Name" : "Point",
                        "Filter" : EntityType.VERTEX || BodyType.MATE_CONNECTOR,
                        "UIHint" : "PREVENT_CREATING_NEW_MATE_CONNECTORS",
                        "MaxNumberOfPicks" : 1 }
            definition.point is Query;
        }
    }
    {
        verifyNonemptyQuery(context, definition, "firstBodyToAlign", "Select the first body to align.");
        verifyNonemptyQuery(context, definition, "secondBodyToAlign", "Select the second body to align.");


        var cSys = WORLD_COORD_SYSTEM;
        if (definition.referenceType == ReferenceType.MATE_CONNECTOR)
        {
            verifyNonemptyQuery(context, definition, "mateConnector ", "Select a mate connector.");
            
            cSys = evMateConnector(context, { "mateConnector" : definition.mateConnector  });
        }
        else if (definition.referenceType == ReferenceType.PLANE_POINT)
        {
            verifyNonemptyQuery(context, definition, "plane", "Select a reference plane.");
            verifyNonemptyQuery(context, definition, "point", "Select a reference point.");
            
            const plane = evPlane(context, {  "face" : definition.plane });
            const point = project(plane, evVertexPoint(context, { "vertex" : definition.point }));
            cSys = coordSystem(point, plane.x, plane.normal);
        }

        opMateConnector(context, id + "firstMateConnector", {
                    "coordSystem" : cSys,
                    "owner" : definition.firstBodyToAlign
                });

        opMateConnector(context, id + "secondMateConnector", {
                    "coordSystem" : cSys,
                    "owner" : definition.secondBodyToAlign
                });
    });
