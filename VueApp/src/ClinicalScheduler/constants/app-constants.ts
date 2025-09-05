/**
 * Application constants for the Clinical Scheduler
 */

// UI Configuration
const UI_CONFIG = {
    NOTIFICATION_TIMEOUT: 3000,
    LOADING_SPINNER_SIZE: "3rem",
    ACCESS_DENIED_ICON_SIZE: "48px",
} as const

// Component sizes and dimensions
const COMPONENT_SIZES = {
    MIN_ROTATION_SELECTOR_WIDTH: "300px",
    MIN_YEAR_SELECTOR_WIDTH: "120px",
} as const

// CSS classes
const CSS_CLASSES = {
    NO_ACCESS_CONTAINER: "no-access-container",
    NO_ACCESS_CARD: "no-access-card",
    CLINICAL_SCHEDULER_CONTAINER: "clinical-scheduler-container",
} as const

export { UI_CONFIG, COMPONENT_SIZES, CSS_CLASSES }
