import { useQuasar } from "quasar"

/**
 * Format error message from error object
 */
function formatError(err: unknown, defaultMessage: string): string {
    return err instanceof Error ? err.message : defaultMessage
}

/**
 * Composable for schedule-related notifications
 */
export function useScheduleNotifications() {
    const $q = useQuasar()

    const TIMEOUT_WARNING_MS = 6000
    const TIMEOUT_SUCCESS_MS = 4000

    /**
     * Helper to create a notification with common defaults
     */
    function createNotification(type: "positive" | "negative" | "warning", message: string, options?: any) {
        $q.notify({
            type,
            message,
            ...options,
        })
    }

    /**
     * Show permission denied notification
     */
    function notifyPermissionDenied(): void {
        createNotification("negative", "You don't have permission to edit this rotation")
    }

    /**
     * Show already scheduled warning
     */
    function notifyAlreadyScheduled(clinicianName: string): void {
        createNotification("warning", `${clinicianName} is already scheduled for this week`)
    }

    /**
     * Show success notification for adding clinician
     */
    function notifyAddSuccess(clinicianName: string, weekNumber: number, warningMessage?: string): void {
        let message = `✓ ${clinicianName} successfully added to Week ${weekNumber}`
        if (warningMessage) {
            message += `\n\n${warningMessage}`
        }

        createNotification(warningMessage ? "warning" : "positive", message, {
            timeout: warningMessage ? TIMEOUT_WARNING_MS : TIMEOUT_SUCCESS_MS,
            multiLine: !!warningMessage,
            actions: [
                {
                    icon: "close",
                    color: "white",
                    handler: () => {
                        /* dismiss */
                    },
                },
            ],
        })
    }

    /**
     * Show success notification for removing clinician
     */
    function notifyRemoveSuccess(clinicianName: string): void {
        createNotification("positive", `${clinicianName} removed from schedule`)
    }

    /**
     * Show success notification for primary status change
     */
    function notifyPrimaryStatusChange(clinicianName: string, isPrimary: boolean): void {
        const action = isPrimary ? "set as" : "removed as"
        createNotification("positive", `${clinicianName} ${action} primary evaluator`)
    }

    /**
     * Show error notification
     */
    function notifyError(message: string): void {
        createNotification("negative", message)
    }

    return {
        notifyPermissionDenied,
        notifyAlreadyScheduled,
        notifyAddSuccess,
        notifyRemoveSuccess,
        notifyPrimaryStatusChange,
        notifyError,
        formatError,
    }
}
