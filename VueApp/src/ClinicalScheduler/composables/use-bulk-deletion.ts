import { ref } from "vue"
import { useQuasar } from "quasar"
import type { ScheduleData } from "../utils/schedule-update-helpers"
import { UI_CONFIG } from "../constants/app-constants"

interface BulkDeletionItem {
    scheduleId: number
    displayName: string
    weekNumber: number
}

interface BulkDeletionCallbackOptions {
    onSuccess: (wasPrimary?: boolean, instructorName?: string) => void
    onError: () => void
}

interface BulkDeletionOptions {
    confirmationTitle: string
    confirmationMessage: string
    successMessage: (count: number) => string
    errorMessage: string
    removeScheduleWithRollback: (
        scheduleData: ScheduleData,
        scheduleId: number,
        options: BulkDeletionCallbackOptions,
    ) => void
    clearSelections: () => void
    skipConfirmation?: boolean
}

/**
 * Composable for handling bulk deletion operations with animations
 * Used by both RotationScheduleView and ClinicianScheduleView
 */
export function useBulkDeletion() {
    const $q = useQuasar()
    const isDeleting = ref(false)

    async function executeBulkDeletion(
        scheduleData: ScheduleData,
        itemsToDelete: BulkDeletionItem[],
        options: BulkDeletionOptions,
    ) {
        if (itemsToDelete.length === 0) {
            return
        }

        // Don't set isDeleting yet if we need to show confirmation
        // This prevents any UI state changes before confirmation
        if (options.skipConfirmation) {
            // If confirmation was already handled, set isDeleting immediately
            isDeleting.value = true
        } else {
            try {
                await $q.dialog({
                    title: options.confirmationTitle,
                    message: options.confirmationMessage,
                    cancel: true,
                    persistent: true,
                })
                // User clicked OK, now set isDeleting and proceed
                isDeleting.value = true
            } catch {
                // User clicked Cancel or dismissed dialog
                // Don't set isDeleting, just return
                return
            }
        }

        try {
            // Process deletions - all at once for concurrent animations
            let totalSuccess = 0
            let totalErrors = 0
            let completedCount = 0
            const removedPrimaryEvaluators: { name: string; weekNumber: number }[] = []

            // Check if all operations completed and show results
            const checkCompletion = () => {
                if (completedCount === itemsToDelete.length) {
                    // All operations complete - show results
                    const messages = []
                    if (totalSuccess > 0) {
                        messages.push(options.successMessage(totalSuccess))
                    }
                    if (totalErrors > 0) {
                        messages.push(`${totalErrors} failed`)
                    }

                    if (messages.length > 0) {
                        $q.notify({
                            type: totalErrors > 0 ? "warning" : "positive",
                            message: messages.join(", "),
                        })
                    }

                    // Show warning for removed primary evaluators
                    if (removedPrimaryEvaluators.length > 0) {
                        const uniqueWeeks = [...new Set(removedPrimaryEvaluators.map((pe) => pe.weekNumber))].toSorted()
                        const uniqueInstructorNames = [...new Set(removedPrimaryEvaluators.map((pe) => pe.name))]
                        const instructorNames = uniqueInstructorNames.join(", ")
                        $q.notify({
                            type: "warning",
                            message: `Primary evaluator${uniqueInstructorNames.length > 1 ? "s" : ""} ${instructorNames} removed. Week${uniqueWeeks.length > 1 ? "s" : ""} ${uniqueWeeks.join(", ")} may need new primary evaluator${uniqueWeeks.length > 1 ? "s" : ""}.`,
                            timeout: UI_CONFIG.NOTIFICATION_TIMEOUT_WARNING,
                            icon: "star_outline",
                        })
                    }

                    // Clear selections after bulk operation
                    options.clearSelections()
                    isDeleting.value = false
                }
            }

            for (const item of itemsToDelete) {
                options.removeScheduleWithRollback(scheduleData, item.scheduleId, {
                    // oxlint-disable-next-line no-loop-func -- shared counters are intentional for bulk-operation tracking
                    onSuccess: (wasPrimary, instructorName) => {
                        totalSuccess += 1
                        if (wasPrimary && instructorName) {
                            removedPrimaryEvaluators.push({
                                name: instructorName,
                                weekNumber: item.weekNumber,
                            })
                        }
                        completedCount += 1
                        checkCompletion()
                    },
                    // oxlint-disable-next-line no-loop-func -- shared counters are intentional for bulk-operation tracking
                    onError: () => {
                        totalErrors += 1
                        completedCount += 1
                        checkCompletion()
                    },
                })
            }
        } catch {
            // User cancelled or error in dialog - reset loading state
            isDeleting.value = false
        }
    }

    return {
        isDeleting,
        executeBulkDeletion,
    }
}
