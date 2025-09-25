/**
 * Application constants for the Clinical Scheduler
 */

// UI Configuration
const UI_CONFIG = {
    NOTIFICATION_TIMEOUT: 3000, // Default, success, and info notifications
    NOTIFICATION_TIMEOUT_WARNING: 4000,
    NOTIFICATION_TIMEOUT_ERROR: 5000,
} as const

// Animation Configuration
const ANIMATIONS = {
    HIGHLIGHT_DURATION_MS: 2000, // Duration for newly added assignment highlight animation
} as const

export { UI_CONFIG, ANIMATIONS }
