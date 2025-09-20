import type { Ref } from "vue"
import { useQuasar } from "quasar"
import type { ScheduleSemester } from "../components/ScheduleView.vue"
import type { ScheduleData } from "../utils/schedule-update-helpers"
import { useBulkDeletion } from "./use-bulk-deletion"
import { showBulkDeleteConfirmation } from "../utils/confirmation-dialog"

/**
 * Generic interface for items that can be bulk deleted
 */
interface BulkDeletableItem {
    [key: string]: any
}

/**
 * Interface for assignments that can be deleted
 */
interface DeletableAssignment {
    id: number
    [key: string]: any
}

/**
 * Configuration for bulk deletion collection
 */
interface BulkDeletionCollectionConfig<TItem extends BulkDeletableItem> {
    selectedItems: Ref<TItem[]>
    selectedWeekIds: Ref<number[]>
    schedulesBySemester: Ref<ScheduleSemester[]>
    getWeekAssignments: (week: any) => DeletableAssignment[]
    matchAssignmentToItem: (assignment: DeletableAssignment, item: TItem) => boolean
    getItemDisplayName: (item: TItem) => string
    getItemTypeName: () => string
    canDeleteAssignment?: (assignment: DeletableAssignment, item: TItem) => boolean
}

/**
 * Configuration for bulk deletion execution
 */

interface BulkDeletionExecutionConfig {
    scheduleData: ScheduleData
    removeScheduleWithRollback: (
        scheduleData: ScheduleData,
        scheduleId: number,
        options: {
            onSuccess?: (wasPrimary?: boolean, instructorName?: string) => void
            onError?: (error: string) => void
        },
    ) => void
    clearSelections: () => void
    showManualConfirmation?: boolean
}

/**
 * Collect items to delete from selected items and weeks
 */
function collectItemsToDelete<TItem extends BulkDeletableItem>(config: BulkDeletionCollectionConfig<TItem>) {
    const {
        selectedItems,
        selectedWeekIds,
        schedulesBySemester,
        getWeekAssignments,
        matchAssignmentToItem,
        canDeleteAssignment,
    } = config

    const schedulesToDelete: { scheduleId: number; displayName: string; weekNumber: number }[] = []
    const allWeeks = schedulesBySemester.value.flatMap((s) => s.weeks)

    for (const weekId of selectedWeekIds.value) {
        const week = allWeeks.find((w: any) => w.weekId === weekId)
        if (week) {
            const weekAssignments = getWeekAssignments(week)

            for (const item of selectedItems.value) {
                const assignment = weekAssignments.find((a) => matchAssignmentToItem(a, item))
                if (assignment && (!canDeleteAssignment || canDeleteAssignment(assignment, item))) {
                    schedulesToDelete.push({
                        scheduleId: assignment.id,
                        displayName: config.getItemDisplayName(item),
                        weekNumber: (week as any).weekNumber || 0,
                    })
                }
            }
        }
    }

    return schedulesToDelete
}

/**
 * Build confirmation message for bulk deletion
 */
function buildConfirmationMessage<TItem extends BulkDeletableItem>(options: {
    selectedItems: TItem[]
    selectedWeekIds: number[]
    schedulesToDelete: { scheduleId: number; displayName: string; weekNumber: number }[]
    getItemDisplayName: (item: TItem) => string
    getItemTypeName: () => string
}): string {
    const { selectedItems, selectedWeekIds, schedulesToDelete, getItemDisplayName, getItemTypeName } = options
    const itemNames = selectedItems.map(getItemDisplayName).join(" / ")
    const weekCount = selectedWeekIds.length
    const itemType = getItemTypeName()

    return `Remove ${selectedItems.length} ${itemType}(s) (${itemNames}) from ${weekCount} week(s)? This will delete ${schedulesToDelete.length} assignment(s).`
}

/**
 * Composable for shared bulk deletion logic between rotation and clinician views
 */
export function useBulkDeletionLogic() {
    const $q = useQuasar()
    const { executeBulkDeletion } = useBulkDeletion()

    /**
     * Execute bulk deletion with shared logic
     */
    async function executeBulkDeletionLogic<TItem extends BulkDeletableItem>(
        collectionConfig: BulkDeletionCollectionConfig<TItem>,
        executionConfig: BulkDeletionExecutionConfig,
    ) {
        const { selectedItems, selectedWeekIds, getItemTypeName } = collectionConfig

        const {
            scheduleData,
            removeScheduleWithRollback,
            clearSelections,
            showManualConfirmation = false,
        } = executionConfig

        if (selectedItems.value.length === 0 || selectedWeekIds.value.length === 0) {
            return
        }

        // Collect items to delete
        const schedulesToDelete = collectItemsToDelete(collectionConfig)

        if (schedulesToDelete.length === 0) {
            $q.notify({
                type: "info",
                message: `No assignments found to delete for the selected ${getItemTypeName()}s and weeks`,
            })
            return
        }

        // Build confirmation message
        const confirmMessage = buildConfirmationMessage({
            selectedItems: selectedItems.value,
            selectedWeekIds: selectedWeekIds.value,
            schedulesToDelete,
            getItemDisplayName: collectionConfig.getItemDisplayName,
            getItemTypeName,
        })

        // Handle confirmation - either manual or built-in
        if (showManualConfirmation) {
            const userConfirmed = await showBulkDeleteConfirmation($q, confirmMessage)
            if (!userConfirmed) {
                return
            }
        }

        // Execute bulk deletion
        await executeBulkDeletion(scheduleData, schedulesToDelete, {
            confirmationTitle: "Bulk Delete Confirmation",
            confirmationMessage: confirmMessage,
            successMessage: (count) => `${count} assignment(s) deleted`,
            errorMessage: "Failed to delete assignments. Please try again.",
            removeScheduleWithRollback,
            clearSelections,
            skipConfirmation: showManualConfirmation, // Skip built-in confirmation if we already showed manual
        })
    }

    return {
        executeBulkDeletionLogic,
    }
}
