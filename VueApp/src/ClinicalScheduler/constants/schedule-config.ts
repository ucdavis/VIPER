/**
 * Shared configuration constants for schedule views
 * Reduces duplication between ClinicianScheduleView and RotationScheduleView
 */

export const SCHEDULE_MESSAGES = {
    // Empty state messages
    EMPTY_STATE: {
        EDITABLE: "Click to add assignment",
        EDITABLE_CLINICIAN: "Click to add rotation",
        EDITABLE_ROTATION: "Click to add clinician",
        READ_ONLY: "No assignments",
    },

    // Primary evaluator messages
    PRIMARY_EVALUATOR: {
        TITLE: "Primary evaluator. To transfer primary status, click the star on another clinician.",
        MAKE_PRIMARY: "Click to make this clinician the primary evaluator.",
        REMOVAL_DISABLED: "Cannot remove primary clinician. Make another clinician primary first.",
        WARNING_ICON: "Primary evaluator required for this week",
    },

    // No data messages
    NO_DATA: {
        DEFAULT: "No schedule data available",
        NO_WEEKS: (rotationName: string, year: number) => `${rotationName} has no weeks scheduled for ${year}.`,
        NO_ASSIGNMENTS_CLINICIAN: (clinicianName: string, year: number) =>
            `${clinicianName} has no rotation assignments for ${year}.`,
        NO_ASSIGNMENTS_ROTATION: (rotationName: string, year: number) =>
            `${rotationName} has no clinician assignments for ${year}.`,
    },

    // Selection messages
    SELECTION: {
        NO_CLINICIAN: "Please select a clinician to view their schedule.",
        NO_ROTATION: "Please select a rotation to view its schedule.",
        INSTRUCTIONS:
            "This list of clinicians should contain any clinician scheduled for the rotation in the current or previous year. The user can click on a clinician to select them, and then click on any week to schedule them.",
    },

    // Recent selections
    RECENT: {
        EMPTY_CLINICIANS: "No recent clinicians. Please add a clinician below.",
        EMPTY_ROTATIONS: "No recent rotations. Please add a rotation below.",
    },
} as const

export const SCHEDULE_LABELS = {
    // Section labels
    RECENT_CLINICIANS: "Recent Clinicians:",
    RECENT_ROTATIONS: "Recent Rotations:",
    ADD_NEW_CLINICIAN: "Add New Clinician:",
    ADD_NEW_ROTATION: "Add New Rotation:",

    // Item types
    ITEM_TYPE: {
        CLINICIAN: "clinician",
        ROTATION: "rotation",
    },
} as const

export const SCHEDULE_VIEW_CONFIG = {
    // Common view props
    DEFAULT_PROPS: {
        showLegend: true,
        showPrimaryToggle: true,
        labelSpacing: "xs" as const,
        selectorSpacing: "none" as const,
    },

    // Rotation view specific
    ROTATION_VIEW: {
        showWarningInLegend: true,
        showWarningIcon: true,
        requiresPrimaryForWeek: true,
        labelSpacing: "md" as const,
        selectorSpacing: "lg" as const,
    },

    // Clinician view specific
    CLINICIAN_VIEW: {
        showWarningInLegend: false,
        showWarningIcon: false,
        requiresPrimaryForWeek: false,
    },
} as const

// Type helpers for the constants
export type ScheduleMessageKey = keyof typeof SCHEDULE_MESSAGES
export type ScheduleLabelKey = keyof typeof SCHEDULE_LABELS
export type ViewConfigKey = keyof typeof SCHEDULE_VIEW_CONFIG
