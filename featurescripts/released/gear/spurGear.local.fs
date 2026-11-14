FeatureScript 1576;
import(path : "onshape/std/common.fs", version : "1576.0");
import(path : "onshape/std/table.fs", version : "1576.0");
icon::import(path : "84e649fcf78ef2cf627fcf5e", version : "4adb2fbb86b2ebf683c17b68");
image::import(path : "043497848f0f1de8551a633b", version : "9991013568200c693d71d69f");


export enum GearInputType
{
    annotation { "Name" : "Module" }
    MODULE,
    annotation { "Name" : "Diametral pitch" }
    DIAMETRAL_PITCH,
    annotation { "Name" : "Circular pitch" }
    CIRCULAR_PITCH
}

export enum RootFilletType
{
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "1/4" }
    QUARTER,
    annotation { "Name" : "1/3" }
    THIRD,
    annotation { "Name" : "Full" }
    FULL
}

export enum DedendumFactor
{
    annotation { "Name" : "1.157 x addendum" }
    D157,
    annotation { "Name" : "1.20 x addendum" }
    D200,
    annotation { "Name" : "1.25 x addendum" }
    D250
}

export enum GearChamferType
{
    annotation { "Name" : "Equal distance" }
    EQUAL_OFFSETS,
    annotation { "Name" : "Two distances" }
    TWO_OFFSETS,
    annotation { "Name" : "Distance and angle" }
    OFFSET_ANGLE
}

export enum HelixDirection
{
    annotation { "Name" : "Clockwise" }
    CW,
    annotation { "Name" : "Counterclockwise" }
    CCW
}

const TEETH_BOUNDS =
{
            (unitless) : [4, 25, 1000]
        } as IntegerBoundSpec;

const PRESSURE_ANGLE_BOUNDS =
{
            (degree) : [12, 20, 45]
        } as AngleBoundSpec;

const MODULE_BOUNDS =
{
            (meter) : [1e-5, 0.001, 500],
            (centimeter) : 0.1,
            (millimeter) : 1.0,
            (inch) : 0.04
        } as LengthBoundSpec;

const CENTERHOLE_BOUNDS =
{
            (meter) : [1e-5, 0.01, 500],
            (centimeter) : 1.0,
            (millimeter) : 10.0,
            (inch) : 0.375
        } as LengthBoundSpec;

const KEY_BOUNDS =
{
            (meter) : [1e-5, 0.003, 500],
            (centimeter) : 0.3,
            (millimeter) : 3.0,
            (inch) : 0.125
        } as LengthBoundSpec;

const CHAMFER_ANGLE_BOUNDS =
{
            (degree) : [0.1, 60, 179.9]
        } as AngleBoundSpec;

const CHAMFER_BOUNDS =
{
            (meter) : [1e-5, 0.0005, 500],
            (centimeter) : 0.05,
            (millimeter) : 0.5,
            (inch) : 0.02
        } as LengthBoundSpec;

const BACKLASH_BOUNDS =
{
            (meter) : [-500, 0.0, 500],
            (centimeter) : 0,
            (millimeter) : 0,
            (inch) : 0
        } as LengthBoundSpec;

const HELIX_ANGLE_BOUNDS =
{
            (degree) : [5, 15, 45]
        } as AngleBoundSpec;

annotation { "Feature Type Name" : "Spur gear",
        "Editing Logic Function" : "editGearLogic",
        "Feature Name Template" : "Spur gear (#teeth teeth)",
        "Feature Type Description" : "Creates a spur or helical gear with optional center hole and keyway",
        "Description Image" : image::BLOB_DATA,
        "Icon" : icon::BLOB_DATA }
export const SpurGear = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Sketch vertex or mate connector",
                    "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR,
                    "MaxNumberOfPicks" : 1,
                    "Description" : "Select a sketch vertex or mate connector to locate the gear" }
        definition.center is Query;

        annotation { "Name" : "Depth" }
        isLength(definition.gearDepth, BLEND_BOUNDS);

        annotation { "Name" : "Opposite direction", "UIHint" : "OPPOSITE_DIRECTION" }
        definition.flipGear is boolean;

        annotation { "Name" : "Symmetric", "Default" : false }
        definition.symmetric is boolean;

        annotation { "Name" : "Number of teeth" }
        isInteger(definition.numTeeth, TEETH_BOUNDS);

        annotation { "Name" : "Gear size and tooth pitch calculation method",
                    "Description" : "<b>Module</b> - Pitch diameter divided by number of teeth<br>" ~
                    "<b>Diametral pitch</b> - Number of teeth divided by pitch diameter (in inches)<br>" ~
                    "<b>Circular pitch</b> - Pitch circumference divided by number of teeth" }
        definition.GearInputType is GearInputType;

        if (definition.GearInputType == GearInputType.MODULE)
        {
            annotation { "Name" : "Module" }
            isLength(definition.module, MODULE_BOUNDS);
        }
        else if (definition.GearInputType == GearInputType.DIAMETRAL_PITCH)
        {
            annotation { "Name" : "Diametral pitch / inch" }
            isReal(definition.diametralPitch, POSITIVE_REAL_BOUNDS);
        }
        else if (definition.GearInputType == GearInputType.CIRCULAR_PITCH)
        {
            annotation { "Name" : "Circular pitch" }
            isLength(definition.circularPitch, LENGTH_BOUNDS);
        }

        annotation { "Name" : "Pitch circle diameter" }
        isLength(definition.pitchCircleDiameter, LENGTH_BOUNDS);

        annotation { "Name" : "Pressure angle" }
        isAngle(definition.pressureAngle, PRESSURE_ANGLE_BOUNDS);

        annotation { "Name" : "Root fillet", "Default" : RootFilletType.THIRD, "UIHint" : "SHOW_LABEL" }
        definition.rootFillet is RootFilletType;

        annotation { "Group Name" : "Profile offsets", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Backlash",
                        "Description" : "A positive value adds clearance between all meshing faces" }
            isLength(definition.backlash, BACKLASH_BOUNDS);

            annotation { "Name" : "Dedendum", "Default" : DedendumFactor.D250, "UIHint" : "SHOW_LABEL" }
            definition.dedendumFactor is DedendumFactor;

            annotation { "Name" : "Root radius",
                        "Description" : "A positive value adds more clearance at the root" }
            isLength(definition.offsetClearance, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Tip radius",
                        "Description" : "A negative value reduces the outside diameter of the gear" }
            isLength(definition.offsetDiameter, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Clocking angle",
                        "Description" : "Adjust this value to mesh gear trains in a Part Studio" }
            isAngle(definition.offsetAngle, ANGLE_360_ZERO_DEFAULT_BOUNDS);
        }

        annotation { "Name" : "Helical",
                    "Description" : "Create helical or herringbone gear" }
        definition.helical is boolean;

        if (definition.helical)
        {
            annotation { "Group Name" : "Helical", "Collapsed By Default" : false, "Driving Parameter" : "helical" }
            {
                annotation { "Name" : "Angle" }
                isAngle(definition.helixAngle, HELIX_ANGLE_BOUNDS);

                annotation { "Name" : "Direction of helix" }
                definition.handedness is HelixDirection;

                annotation { "Name" : "Double helix" }
                definition.double is boolean;
            }
        }

        annotation { "Name" : "Chamfer", "Default" : false }
        definition.chamfer is boolean;

        if (definition.chamfer)
        {
            annotation { "Group Name" : "Chamfer", "Collapsed By Default" : false, "Driving Parameter" : "chamfer" }
            {
                annotation { "Name" : "Chamfer type", "Default" : GearChamferType.OFFSET_ANGLE }
                definition.chamferType is GearChamferType;

                if (definition.chamferType != GearChamferType.TWO_OFFSETS)
                {
                    annotation { "Name" : "Distance" }
                    isLength(definition.width, CHAMFER_BOUNDS);
                }
                else
                {
                    annotation { "Name" : "Distance 1" }
                    isLength(definition.width1, CHAMFER_BOUNDS);
                }

                if (definition.chamferType == GearChamferType.OFFSET_ANGLE ||
                    definition.chamferType == GearChamferType.TWO_OFFSETS)
                {
                    annotation { "Name" : "Opposite direction", "Default" : false, "UIHint" : "OPPOSITE_DIRECTION" }
                    definition.oppositeDirection is boolean;
                }

                if (definition.chamferType == GearChamferType.TWO_OFFSETS)
                {
                    annotation { "Name" : "Distance 2" }
                    isLength(definition.width2, CHAMFER_BOUNDS);
                }
                else if (definition.chamferType == GearChamferType.OFFSET_ANGLE)
                {
                    annotation { "Name" : "Angle" }
                    isAngle(definition.angle, CHAMFER_ANGLE_BOUNDS);
                }
            }
        }

        annotation { "Name" : "Center bore" }
        definition.centerHole is boolean;

        if (definition.centerHole)
        {
            annotation { "Group Name" : "Center Bore", "Collapsed By Default" : false, "Driving Parameter" : "centerHole" }
            {
                annotation { "Name" : "Bore diameter" }
                isLength(definition.centerHoleDia, CENTERHOLE_BOUNDS);

                annotation { "Name" : "Keyway" }
                definition.key is boolean;

                if (definition.key)
                {
                    annotation { "Name" : "Key width" }
                    isLength(definition.keyWidth, KEY_BOUNDS);

                    annotation { "Name" : "Key height" }
                    isLength(definition.keyHeight, KEY_BOUNDS);
                }
            }
        }
    }

    {
        if (definition.centerHole && definition.centerHoleDia >= definition.pitchCircleDiameter - 4 * definition.module)
        {
            throw regenError("Center hole diameter must be less than root diameter", ["centerHoleDia"]);
        }

        if (definition.key && definition.keyHeight / 2 + definition.centerHoleDia >= definition.pitchCircleDiameter - 4 * definition.module)
        {
            throw regenError("Center hole diameter plus key height must be less than root diameter", ["keyHeight"]);
        }

        const remainderTransform = getRemainderPatternTransform(context, {
                    "references" : definition.center
                });

        var sketchPlane = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);

        // else find location of selected vertex and its sketch plane or use mate connector to create a new sketch for the gear profile
        if (!isQueryEmpty(context, definition.center))
        {
            try silent
            {
                sketchPlane = plane(evMateConnector(context, {
                                "mateConnector" : definition.center
                            }));
            }
            catch
            {
                sketchPlane = evOwnerSketchPlane(context, { "entity" : definition.center });
                sketchPlane.origin = evVertexPoint(context, { "vertex" : definition.center });
            }
        }

        var dedendumFactor = 1.25;

        if (definition.dedendumFactor == DedendumFactor.D157)
            dedendumFactor = 1.157;
        else if (definition.dedendumFactor == DedendumFactor.D200)
            dedendumFactor = 1.2;

        var gearData = { "center" : vector(0, 0) * meter };
        gearData.addendum = definition.module + definition.offsetDiameter;
        gearData.dedendum = dedendumFactor * definition.module + definition.offsetClearance;
        gearData.outerRadius = definition.pitchCircleDiameter / 2 + gearData.addendum;
        gearData.innerRadius = definition.pitchCircleDiameter / 2 - gearData.dedendum;

        const blank = createBlank(context, id, definition, gearData, sketchPlane);
        const tooth = createTooth(context, id, definition, gearData, sketchPlane);

        var transforms = [];
        var instanceNames = [];

        for (var i = 1; i < definition.numTeeth; i += 1)
        {
            const instanceTransform = rotationAround(line(sketchPlane.origin, sketchPlane.normal), i * (360 / definition.numTeeth) * degree);
            transforms = append(transforms, instanceTransform);
            instanceNames = append(instanceNames, "" ~ i);
        }

        opPattern(context, id + "pattern", {
                    "entities" : tooth,
                    "transforms" : transforms,
                    "instanceNames" : instanceNames
                });

        opBoolean(context, id + "hobbed", {
                    "tools" : qUnion(tooth, qCreatedBy(id + "pattern", EntityType.BODY)),
                    "targets" : blank,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        opDeleteBodies(context, id + "delete", {
                    "entities" : qSubtraction(qCreatedBy(id, EntityType.BODY), blank)
                });

        // create Pitch Circle Diameter sketch for aligning gear trains
        const PCDSketch = newSketchOnPlane(context, id + "PCDsketch", { "sketchPlane" : sketchPlane });

        skCircle(PCDSketch, "PCD", {
                    "center" : gearData.center,
                    "radius" : definition.pitchCircleDiameter / 2,
                    "construction" : true
                });

        skSolve(PCDSketch);

        transformResultIfNecessary(context, id, remainderTransform);

        setGearData(context, id, definition, gearData);
    });

function setGearData(context is Context, id is Id, definition is map, gearData is map)
{
    var gearInputType = "Module";
    var gearInputSize = definition.module;

    if (definition.GearInputType == GearInputType.DIAMETRAL_PITCH)
    {
        gearInputType = "Diametral pitch";
        gearInputSize = definition.diametralPitch;
    }
    else if (definition.GearInputType == GearInputType.CIRCULAR_PITCH)
    {
        gearInputType = "Circular pitch";
        gearInputSize = definition.circularPitch;
    }

    setAttribute(context, {
                "entities" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                "name" : "spurGear",
                "attribute" : {
                    "numTeeth" : definition.numTeeth,
                    "gearInputType" : gearInputType,
                    "gearInputSize" : gearInputSize,
                    "pitchCircleDiameter" : definition.pitchCircleDiameter,
                    "outsideDiameter" : gearData.outerRadius * 2,
                    "pressureAngle" : definition.pressureAngle,
                } // TODO: center hole, chamfer, helical
            });

    setFeatureComputedParameter(context, id, {
                "name" : "teeth",
                "value" : definition.numTeeth
            });

    setProperty(context, {
                "entities" : qBodyType(qCreatedBy(id, EntityType.BODY), BodyType.SOLID),
                "propertyType" : PropertyType.NAME,
                "value" : "Spur gear (" ~ definition.numTeeth ~ " teeth)"
            });
}

function createBlank(context is Context, id is Id, definition is map, gearData is map, sketchPlane is Plane) returns Query
{
    const blankSketch = newSketchOnPlane(context, id + "blankSketch", { "sketchPlane" : sketchPlane });

    skCircle(blankSketch, "addendum", {
                "center" : gearData.center,
                "radius" : gearData.outerRadius
            });

    if (definition.centerHole)
    {
        if (definition.key)
        {
            const xDirection = (definition.keyWidth / 2) * vector(1, 0);
            const yDirection = ((definition.keyHeight + definition.centerHoleDia) / 2) * vector(0, 1);

            skPolyline(blankSketch, "keyway", {
                        "points" : [
                            gearData.center + xDirection,
                            gearData.center + xDirection + yDirection,
                            gearData.center - xDirection + yDirection,
                            gearData.center - xDirection
                        ]
                    });
        }

        skCircle(blankSketch, "center", {
                    "center" : gearData.center,
                    "radius" : definition.centerHoleDia / 2
                });
    }

    skSolve(blankSketch);

    opExtrude(context, id + "blank", extrudeParams(definition, qSketchRegion(id + "blankSketch", true), sketchPlane));

    if (definition.chamfer)
    {
        definition.entities = qLargest(qCreatedBy(id + "blank", EntityType.EDGE));

        if (definition.flipGear)
            definition.oppositeDirection = !definition.oppositeDirection;

        try
        {
            opChamfer(context, id + "chamfer", definition);
        }
        catch
        {
            throw regenError("Chamfer failed, check inputs", ["width", "width1", "width2", "angle"]);
        }
    }

    return qCreatedBy(id + "blank", EntityType.BODY);
}

function createTooth(context is Context, id is Id, definition is map, gearData is map, sketchPlane is Plane) returns Query
{
    const base = definition.pitchCircleDiameter * cos(definition.pressureAngle);

    // angle between root of teeth
    const alpha = sqrt(definition.pitchCircleDiameter ^ 2 - base ^ 2) / base * radian - definition.pressureAngle;
    const beta = 360 / (4 * definition.numTeeth) * degree - alpha;

    const toothSketch = newSketchOnPlane(context, id + "toothSketch", { "sketchPlane" : sketchPlane });

    // build involute splines for each tooth
    var involute1 = [];
    var involute2 = [];

    for (var t = 0; t <= 2; t += (1 / 50)) // (1/50) is the hard-coded involute spline tolerance
    {
        // involute definition math
        const angle = (t + (definition.backlash / cos(definition.pressureAngle)) / definition.pitchCircleDiameter / 2) * radian;
        const offset = beta + definition.offsetAngle;
        const ca = cos(angle + offset);
        const sa = sin(angle + offset);
        const cab = cos(offset - beta * 2 - angle);
        const sab = sin(offset - beta * 2 - angle);
        var point1;
        var point2;

        if (base >= definition.pitchCircleDiameter - 2 * gearData.dedendum && t == 0) // special case when base cylinder diameter is greater than dedendum
        {
            // calculate involute spline point
            point1 = vector((definition.pitchCircleDiameter / 2 - gearData.dedendum) * ca, (definition.pitchCircleDiameter / 2 - gearData.dedendum) * sa);
            point2 = vector((definition.pitchCircleDiameter / 2 - gearData.dedendum) * cab, (definition.pitchCircleDiameter / 2 - gearData.dedendum) * sab);
        }
        else
        {
            point1 = vector(base * 0.5 * (ca + t * sa), base * 0.5 * (sa - t * ca));
            point2 = vector(base * 0.5 * (cab - t * sab), base * 0.5 * (sab + t * cab));
        }

        // and add to array
        involute1 = append(involute1, point1);
        involute2 = append(involute2, point2);

        // if involute points go outside the outer diameter of the gear then stop
        if (sqrt(point1[0] ^ 2 + point1[1] ^ 2) >= (definition.pitchCircleDiameter / 2 + gearData.addendum))
            break;
    }

    // create involute sketch splines
    skFitSpline(toothSketch, "spline1", { "points" : involute1 });
    skFitSpline(toothSketch, "spline2", { "points" : involute2 });

    const regionPoint = vector((definition.pitchCircleDiameter / 2 - gearData.dedendum / 2) * cos(definition.offsetAngle), ((definition.pitchCircleDiameter / 2 - gearData.dedendum / 2) * sin(definition.offsetAngle)));

    skCircle(toothSketch, "addendum", { "center" : gearData.center, "radius" : gearData.outerRadius });
    skCircle(toothSketch, "dedendum", { "center" : gearData.center, "radius" : gearData.innerRadius });

    if (definition.rootFillet != RootFilletType.NONE)
    {
        skCircle(toothSketch, "fillet", { "center" : regionPoint, "radius" : 0.1 * millimeter, "construction" : true });

        skConstraint(toothSketch, "fix1", { "constraintType" : ConstraintType.FIX, "localFirst" : "dedendum" });
        skConstraint(toothSketch, "fix2", { "constraintType" : ConstraintType.FIX, "localFirst" : "spline1" });
        skConstraint(toothSketch, "fix3", { "constraintType" : ConstraintType.FIX, "localFirst" : "spline2" });
        skConstraint(toothSketch, "tangent1", { "constraintType" : ConstraintType.TANGENT, "localFirst" : "fillet", "localSecond" : "dedendum" });
        skConstraint(toothSketch, "tangent2", { "constraintType" : ConstraintType.TANGENT, "localFirst" : "fillet", "localSecond" : "spline1" });
        skConstraint(toothSketch, "tangent3", { "constraintType" : ConstraintType.TANGENT, "localFirst" : "fillet", "localSecond" : "spline2" });
    }

    skSolve(toothSketch);

    var toothId = id + "tooth";

    opExtrude(context, toothId, extrudeParams(definition, qContainsPoint(qCreatedBy(id + "toothSketch", EntityType.FACE), planeToWorld(sketchPlane, regionPoint)), sketchPlane));

    if (definition.rootFillet != RootFilletType.NONE)
    {
        const filletEdges = qClosestTo(qNonCapEntity(id + "tooth", EntityType.EDGE), sketchPlane.origin);

        var rootFilletRadius = evCurveDefinition(context, { "edge" : sketchEntityQuery(id + "toothSketch", EntityType.EDGE, "fillet") }).radius;

        if (definition.rootFillet == RootFilletType.THIRD)
            rootFilletRadius /= 1.5;
        else if (definition.rootFillet == RootFilletType.QUARTER)
            rootFilletRadius /= 2;

        opFillet(context, id + "fillet", {
                    "entities" : filletEdges,
                    "radius" : rootFilletRadius
                });
    }

    if (definition.helical)
    {
        const profileFace = qCapEntity(toothId, CapType.START, EntityType.FACE);
        const helicalPitch = (PI * definition.pitchCircleDiameter) / tan(definition.helixAngle);
        var clockwise = definition.handedness == HelixDirection.CW;

        if (definition.double)
            clockwise = !clockwise;

        if (definition.flipGear && definition.double)
            clockwise = !clockwise;

        opHelix(context, id + "helix", {
                    "direction" : sketchPlane.normal * (definition.flipGear ? -1 : 1),
                    "axisStart" : sketchPlane.origin,
                    "startPoint" : sketchPlane.origin + sketchPlane.x * definition.pitchCircleDiameter / 2,
                    "interval" : [0, definition.gearDepth / helicalPitch / (definition.double ? 2 : 1)],
                    "clockwise" : clockwise,
                    "helicalPitch" : helicalPitch,
                    "spiralPitch" : 0 * meter
                });

        toothId = id + "toothHelix";

        opSweep(context, toothId, {
                    "profiles" : profileFace,
                    "path" : qCreatedBy(id + "helix", EntityType.EDGE)
                });

        if (definition.double)
        {
            opPattern(context, id + "mirror", {
                        "entities" : qCreatedBy(toothId, EntityType.BODY),
                        "transforms" : [mirrorAcross(evPlane(context, {
                                        "face" : qCapEntity(toothId, CapType.END, EntityType.FACE)
                                    }))],
                        "instanceNames" : ["1"] });

            opBoolean(context, id + "double", {
                        "tools" : qUnion(qCreatedBy(toothId, EntityType.BODY), qCreatedBy(id + "mirror", EntityType.BODY)),
                        "operationType" : BooleanOperationType.UNION
                    });
        }
    }

    return qCreatedBy(toothId, EntityType.BODY);
}

function extrudeParams(definition is map, entities is Query, sketchPlane is Plane) returns map
{
    var extrudeParams = {
        "entities" : entities,
        "direction" : sketchPlane.normal * (definition.flipGear ? -1 : 1),
        "endBound" : BoundingType.BLIND,
        "endDepth" : definition.gearDepth
    };

    if (definition.symmetric)
    {
        extrudeParams = mergeMaps(extrudeParams,
            {
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : definition.gearDepth / 2,
                    "endDepth" : definition.gearDepth / 2
                });
    }

    return extrudeParams;
}

export function editGearLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    if (oldDefinition.numTeeth != definition.numTeeth)
    {
        definition.pitchCircleDiameter = definition.numTeeth * definition.module;
    }

    var module;

    if (oldDefinition.module != definition.module)
    {
        module = definition.module;
    }
    else if (oldDefinition.diametralPitch != definition.diametralPitch)
    {
        module = inch / definition.diametralPitch;
    }
    else if (oldDefinition.circularPitch != definition.circularPitch)
    {
        module = definition.circularPitch / PI;
    }
    else if (oldDefinition.pitchCircleDiameter != definition.pitchCircleDiameter)
    {
        module = definition.pitchCircleDiameter / definition.numTeeth;
    }

    if (module != undefined)
    {
        definition.module = module;
        definition.circularPitch = module * PI;
        definition.pitchCircleDiameter = definition.numTeeth * module;
        definition.diametralPitch = 1 * inch / module;
    }

    return definition;
}

annotation { "Table Type Name" : "Gears", "Icon" : icon::BLOB_DATA }
export const spurGearsTable = defineTable(function(context is Context, definition is map) returns Table
    precondition
    {
    }
    {
        const columnDefinitions = [
                tableColumnDefinition("quantity", "Qty"),
                tableColumnDefinition("numTeeth", "Teeth"),
                tableColumnDefinition("gearInputType", "Pitch type"),
                tableColumnDefinition("gearInputSize", "Pitch size"),
                tableColumnDefinition("pitchCircleDiameter", "Pitch diameter"),
                tableColumnDefinition("outsideDiameter", "Outside diameter"),
                tableColumnDefinition("pressureAngle", "Pressure angle"),
            ];

        // Group by same values
        var uniqueGears = {};
        const partsWithGearAttributes = qHasAttribute("spurGear");

        for (var part in evaluateQuery(context, partsWithGearAttributes))
        {
            const gearAttributes = getAttributes(context, {
                        "entities" : part,
                        "name" : "spurGear"
                    });

            if (gearAttributes == [])
            {
                continue;
            }

            const attribute = gearAttributes[0];
            if (uniqueGears[attribute] == undefined)
            {
                uniqueGears[attribute] = [];
            }

            uniqueGears[attribute] = append(uniqueGears[attribute], part);
        }

        var rows = [];

        for (var gearEntry in uniqueGears)
        {
            const parts = gearEntry.value;
            var gearData = gearEntry.key;
            gearData.quantity = size(gearEntry.value);
            rows = append(rows, tableRow(gearData, qUnion(parts)));
        }

        return table("Gears", columnDefinitions, rows);
    });
