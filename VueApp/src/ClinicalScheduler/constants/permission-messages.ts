/**
 * Permission-related error messages and constants for the Clinical Scheduler
 */

// Access denied messages for UI components
const ACCESS_DENIED_MESSAGES = {
    CLINICAL_SCHEDULER: "You do not have permission to access the Clinical Scheduler.",
    ROTATION_VIEW: "You do not have permission to access the Schedule by Rotation view.",
    CLINICIAN_VIEW: "You do not have permission to access the Schedule by Clinician view.",
    UNAUTHORIZED_ROTATION: "You do not have permission to view this rotation.",
} as const

// Subtitle messages for access denied components
const ACCESS_DENIED_SUBTITLES = {
    CLINICAL_SCHEDULER: "Contact your administrator if you need access to schedule management features.",
    ROTATION_VIEW:
        "This feature is not available with your current permission level. Contact your administrator if you need access to rotation scheduling features.",
    CLINICIAN_VIEW:
        "This feature is not available with rotation-specific permissions. Contact your administrator if you need full access to scheduling features.",
    UNAUTHORIZED_ROTATION: "You can only access rotations that you have been granted permission to edit.",
} as const

// Error messages for schedule operations
const SCHEDULE_OPERATION_ERRORS = {
    NO_PERMISSION_EDIT_ROTATION: "You do not have permission to edit this rotation",
    NO_PERMISSION_SCHEDULE_ROTATION: "You do not have permission to schedule this rotation",
    NO_PERMISSION_REMOVE_INSTRUCTOR: "You do not have permission to remove this instructor.",
    NO_PERMISSION_MODIFY_PRIMARY_EVALUATOR: "You do not have permission to modify primary evaluator status.",
} as const

// All permission-related messages
const PERMISSION_MESSAGES = {
    ...ACCESS_DENIED_MESSAGES,
    ...SCHEDULE_OPERATION_ERRORS,
} as const

export { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES, SCHEDULE_OPERATION_ERRORS, PERMISSION_MESSAGES }
