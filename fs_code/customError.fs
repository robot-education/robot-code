FeatureScript 2014;

enum StatusType
{
    OK,
    ERROR,
    WARNING,
    INFO
}

const CUSTOM_ERROR_MAP = {
        // common errors
        CustomError.SELECT_GEOMETRY : "Select outer geometry to use.",
        CustomError.SELECT_PLATE_PLANE : "Select a plate plane to use.",
        CustomError.SELECT_UP_TO_ENTITY : "Select an entity to terminate the plate.",

        CustomError.INVALID_GEOMETRY_SELECTION : "Selected outer geometry is invalid. Check input.",

        CustomError.PLATE_GEOMETRY_TOUCHING : "Selected outer geometry interferes with each other.",
        CustomError.PLATE_GEOMETRY_FAILED : "Failed to create a bounded plate. Check input.",
        CustomError.PLATE_GEOMETRY_MULTIPLE_PLATES : "Failed to create a single bounded plate. Check input.",
        CustomError.FACES_TO_REMOVE_NOT_PARALLEL : "Faces to remove are not parallel with the plate plane.",

        CustomError.FACES_TO_REMOVE_FAILED : "Faces to remove failed to cut the plate.",

        CustomError.EXTRUDE_INPUT_NOT_PARALLEL : "Selected faces are not parallel to each other.",
        CustomError.EXTRUDE_INPUT_NOT_PARALLEL_TO_PLANE : "The selected plate plane is not parallel to the selected faces.",
        CustomError.EXTRUDE_UP_TO_COINCIDENT : "The up to entity cannot be coincident with the plate plane.",
        CustomError.EXTRUDE_UP_TO_NOT_PARALLEL : "The up to entity must be parallel to the plate plane.",
        CustomError.EXTRUDE_PROJECT_FAILED : "Failed to project one or more faces onto the plate plane. Check input.",
        CustomError.EXTRUDE_FAILED : "Failed to extrude plates. Check input.",

        CustomError.FILLET_PARTIALLY_FAILED : "Failed to fillet some plate edges. Check input.",
        CustomError.FILLET_FAILED : "Failed to fillet plate. Check input.",


        CustomError.INVALID_PLATE_SELECTION : "One or more selected entities are not plates created by another plate feature.",
        CustomError.BOOLEANED_PLATE : "The selected plate has been booleaned. Output may be unexpected.",
        CustomError.INVALID_PLATE_EDIT : "The selected plate has been edited by a non-plate feature. Output may be unexpected.",
        CustomError.UNABLE_TO_DETERMINE_PLANE : "Unable to extract a plate plane from selections. Try selecting a plate plane.",
    };

export enum CustomError
{
    SELECT_GEOMETRY,
    SELECT_PLATE_PLANE,
    SELECT_UP_TO_ENTITY,

    INVALID_GEOMETRY_SELECTION,
    PLATE_GEOMETRY_TOUCHING,
    PLATE_GEOMETRY_FAILED,
    PLATE_GEOMETRY_MULTIPLE_PLATES,
    FACES_TO_REMOVE_NOT_PARALLEL,
    FACES_TO_REMOVE_FAILED,

    EXTRUDE_INPUT_NOT_PARALLEL,
    EXTRUDE_INPUT_NOT_PARALLEL_TO_PLANE,
    EXTRUDE_UP_TO_COINCIDENT,
    EXTRUDE_UP_TO_NOT_PARALLEL,
    EXTRUDE_PROJECT_FAILED,
    EXTRUDE_FAILED,

    FILLET_PARTIALLY_FAILED,
    FILLET_FAILED,

    INVALID_PLATE_SELECTION,
    BOOLEANED_PLATE,
    INVALID_PLATE_EDIT,
    UNABLE_TO_DETERMINE_PLANE,
}

/**
 * @param customError : @autocomplete `CustomError.`
 */
export function customError(customError is CustomError) returns string
{
    return CUSTOM_ERROR_MAP[customError];
}
