import { computed } from "vue"
import type { Ref } from "vue"
import type { ScheduleAssignment, ScheduleSemester } from "../components/ScheduleView.vue"

/**
 * Generic interface for selectable items that can be in delete mode
 */
interface SelectableItem {
    [key: string]: any
}

/**
 * Configuration for delete mode detection
 */
interface DeleteModeConfig<T extends SelectableItem> {
    selectedItems: Ref<T[]>
    selectedWeekIds: Ref<number[]>
    schedulesBySemester: Ref<ScheduleSemester[]>
    getWeekAssignments: (week: any) => ScheduleAssignment[]
    getItemIdentifier: (item: T) => string | number
    getAssignmentIdentifier: (assignment: ScheduleAssignment) => string | number
}

/**
 * Composable for detecting delete mode based on whether all selected items
 * exist in all selected weeks. This logic is shared between RotationScheduleView
 * and ClinicianScheduleView.
 *
 * @param config Configuration object for delete mode detection
 * @returns Computed property indicating whether we're in delete mode
 */
export function useDeleteMode<T extends SelectableItem>(config: DeleteModeConfig<T>) {
    const {
        selectedItems,
        selectedWeekIds,
        schedulesBySemester,
        getWeekAssignments,
        getItemIdentifier,
        getAssignmentIdentifier,
    } = config

    const isInDeleteMode = computed(() => {
        // Only in delete mode if we have selections
        if (selectedItems.value.length === 0 || selectedWeekIds.value.length === 0) {
            return false
        }

        // Get all weeks from the schedule
        const allWeeks = schedulesBySemester.value.flatMap((s: ScheduleSemester) => s.weeks)

        // Check if ALL selected items exist in ALL selected weeks
        for (const weekId of selectedWeekIds.value) {
            const week = allWeeks.find((w: any) => w.weekId === weekId)
            if (!week) {
                return false
            }

            const weekAssignments = getWeekAssignments(week)
            const weekItemIdentifiers = new Set(
                weekAssignments.map((assignment) => getAssignmentIdentifier(assignment)),
            )

            // Check if all selected items are in this week
            for (const item of selectedItems.value) {
                const itemIdentifier = getItemIdentifier(item)
                if (!weekItemIdentifiers.has(itemIdentifier)) {
                    return false // At least one item is not scheduled in this week
                }
            }
        }

        return true // All selected items exist in all selected weeks
    })

    return {
        isInDeleteMode,
    }
}
