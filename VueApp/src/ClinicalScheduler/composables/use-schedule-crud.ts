import { useScheduleStore } from "../stores/schedule"
import { usePermissionsStore } from "../stores/permissions"
import { useScheduleStateUpdater } from "./use-schedule-state-updater"
import { useScheduleDialogs } from "./use-schedule-dialogs"
import { useScheduleNotifications } from "./use-schedule-notifications"
import { useScheduleAddOperations } from "./use-schedule-add-operations"
import type { RotationWithService } from "../types/rotation-types"

interface ScheduleCrudParams {
    year: number
}

/**
 * Composable for schedule CRUD operations
 */
export function useScheduleCrud(params: ScheduleCrudParams) {
    const scheduleStore = useScheduleStore()
    const permissionsStore = usePermissionsStore()
    const { removeScheduleFromWeek, updateSchedulePrimaryStatus } = useScheduleStateUpdater()
    const { confirmRemovalDialog, confirmPrimaryRemovalDialog } = useScheduleDialogs()
    const { notifyPermissionDenied, notifyRemoveSuccess, notifyPrimaryStatusChange, notifyError, formatError } =
        useScheduleNotifications()

    /**
     * Check if user has permission to edit a rotation's service
     */
    function canEditRotation(rotation: RotationWithService | null): boolean {
        if (!rotation || !rotation.service) {
            return false
        }
        return permissionsStore.canEditService(rotation.service.serviceId)
    }

    // Use the extracted add operations
    const { scheduleClinicianToWeek } = useScheduleAddOperations({
        year: params.year,
        canEditRotation,
    })

    /**
     * Remove an instructor from the schedule
     */
    async function removeInstructorAssignment(
        scheduleId: number,
        clinicianName: string,
        rotation: RotationWithService,
    ): Promise<boolean> {
        // Permission check
        if (!canEditRotation(rotation)) {
            notifyPermissionDenied()
            return false
        }

        // Confirm removal
        const confirmed = await confirmRemovalDialog(clinicianName)
        if (!confirmed) {
            return false
        }

        try {
            const result = await scheduleStore.removeInstructorFromSchedule(scheduleId)

            if (result.success) {
                // Update local state for optimistic UI update
                const scheduleData = scheduleStore.currentRotationSchedule
                if (scheduleData) {
                    removeScheduleFromWeek(scheduleData, scheduleId)
                }

                notifyRemoveSuccess(clinicianName)

                return true
            }
            throw new Error(result.errors?.join(", ") || "Failed to remove instructor")
        } catch (err) {
            notifyError(formatError(err, "Failed to remove instructor"))
            return false
        }
    }

    /**
     * Toggle primary evaluator status
     */
    async function togglePrimaryStatus(params: {
        scheduleId: number
        currentIsPrimary: boolean
        clinicianName: string
        rotation: RotationWithService
    }): Promise<boolean> {
        const { scheduleId, currentIsPrimary, clinicianName, rotation } = params

        // Permission check
        if (!canEditRotation(rotation)) {
            notifyPermissionDenied()
            return false
        }

        // If removing primary status, confirm
        if (currentIsPrimary) {
            const confirmed = await confirmPrimaryRemovalDialog(clinicianName)
            if (!confirmed) {
                return false
            }
        }

        try {
            const result = await scheduleStore.setPrimaryEvaluator(scheduleId, !currentIsPrimary)

            if (result.success) {
                // Update local state for optimistic UI update
                const scheduleData = scheduleStore.currentRotationSchedule
                if (scheduleData) {
                    updateSchedulePrimaryStatus(scheduleData, scheduleId, !currentIsPrimary)
                }

                notifyPrimaryStatusChange(clinicianName, !currentIsPrimary)

                return true
            }
            throw new Error(result.errors?.join(", ") || "Failed to update primary status")
        } catch (err) {
            notifyError(formatError(err, "Failed to update primary status"))
            return false
        }
    }

    return {
        canEditRotation,
        scheduleClinicianToWeek,
        removeInstructorAssignment,
        togglePrimaryStatus,
    }
}
