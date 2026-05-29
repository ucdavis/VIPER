import { ref } from "vue"
import { useQuasar } from "quasar"
import { recordService } from "../services/record-service"
import type { InstructorEffortRecordDto } from "../types"

type ReloadCallback = () => Promise<void>

/**
 * Composable for shared effort record management logic between
 * MyEffort.vue and InstructorDetail.vue pages.
 *
 * Handles dialog state, mutation handlers (create/update/delete),
 * and post-mutation reload with error handling.
 */
function useEffortRecordManagement(reloadData: ReloadCallback) {
    const $q = useQuasar()

    // Dialog state
    const showAddDialog = ref(false)
    const showEditDialog = ref(false)
    const showImportDialog = ref(false)
    const selectedRecord = ref<InstructorEffortRecordDto | null>(null)
    const preSelectedCourseId = ref<number | null>(null)

    // Dialog openers
    function openAddDialog() {
        showAddDialog.value = true
    }

    function openEditDialog(record: InstructorEffortRecordDto) {
        selectedRecord.value = record
        showEditDialog.value = true
    }

    function openImportDialog() {
        showImportDialog.value = true
    }

    // Post-mutation reload with error handling
    async function safeReload() {
        try {
            await reloadData()
        } catch {
            $q.notify({
                type: "negative",
                message: "Failed to refresh data — please reload the page",
            })
        }
    }

    // Mutation handlers
    async function onRecordCreated() {
        $q.notify({
            type: "positive",
            message: "Effort record created successfully",
        })
        preSelectedCourseId.value = null
        await safeReload()
    }

    async function onRecordUpdated() {
        $q.notify({
            type: "positive",
            message: "Effort record updated successfully",
        })
        await safeReload()
    }

    function onCourseImported(courseId: number) {
        preSelectedCourseId.value = courseId
        showAddDialog.value = true
    }

    async function deleteRecord(recordId: number, originalModifiedDate: string | null) {
        try {
            const result = await recordService.deleteEffortRecord(recordId, originalModifiedDate)
            if (result.success) {
                $q.notify({
                    type: "positive",
                    message: "Effort record deleted successfully",
                })
                await safeReload()
            } else {
                $q.notify({
                    type: "negative",
                    message: result.error ?? "Failed to delete effort record",
                })
                if (result.isConflict) {
                    await safeReload()
                }
            }
        } catch {
            $q.notify({
                type: "negative",
                message: "An error occurred while deleting the record",
            })
        }
    }

    return {
        // Dialog state
        showAddDialog,
        showEditDialog,
        showImportDialog,
        selectedRecord,
        preSelectedCourseId,
        // Dialog openers
        openAddDialog,
        openEditDialog,
        openImportDialog,
        // Mutation handlers
        onRecordCreated,
        onRecordUpdated,
        onCourseImported,
        deleteRecord,
    }
}

/**
 * Formats a date string for display in effort pages.
 * Both pages produce identical output (e.g., "1/15/2025, 2:30 PM").
 */
function formatEffortDate(dateString: string | null): string {
    if (!dateString) {
        return ""
    }
    const date = new Date(dateString)
    return date.toLocaleString("en-US", {
        month: "numeric",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
    })
}

export { useEffortRecordManagement, formatEffortDate }
