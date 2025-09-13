/**
 * Application constants for the Clinical Scheduler
 */

// UI Configuration
const UI_CONFIG = {
    NOTIFICATION_TIMEOUT: 3000, // Default, success, and info notifications
    NOTIFICATION_TIMEOUT_WARNING: 4000,
    NOTIFICATION_TIMEOUT_ERROR: 5000,
} as const

export { UI_CONFIG }
