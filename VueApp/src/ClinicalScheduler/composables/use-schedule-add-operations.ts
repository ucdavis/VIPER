import { useScheduleStore } from "../stores/schedule"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import { useScheduleStateUpdater } from "./use-schedule-state-updater"
import { useScheduleDialogs } from "./use-schedule-dialogs"
import { useScheduleNotifications } from "./use-schedule-notifications"
import type { WeekWithSchedules, RotationWithService } from "../types/rotation-types"
import type { Clinician } from "../services/clinician-service"

export interface AddOperationParams {
    year: number
    canEditRotation: (rotation: RotationWithService | null) => boolean
}

/**
 * Composable for adding clinicians to schedule
 */
export function useScheduleAddOperations(params: AddOperationParams) {
    const scheduleStore = useScheduleStore()
    const { addScheduleToWeek } = useScheduleStateUpdater()
    const { confirmConflictDialog } = useScheduleDialogs()
    const { notifyPermissionDenied, notifyAlreadyScheduled, notifyAddSuccess, notifyError, formatError } =
        useScheduleNotifications()

    /**
     * Schedule a clinician to a specific week
     */
    async function scheduleClinicianToWeek(
        week: WeekWithSchedules,
        clinician: Clinician,
        rotation: RotationWithService,
    ): Promise<boolean> {
        // Permission check
        if (!params.canEditRotation(rotation)) {
            notifyPermissionDenied()
            return false
        }

        // Check if clinician is already scheduled
        const existingAssignments = scheduleStore.getWeekAssignments(week.weekId)
        if (existingAssignments.some((a) => a.mothraId === clinician.mothraId)) {
            notifyAlreadyScheduled(clinician.fullName)
            return false
        }

        try {
            // Check for conflicts
            const conflictResult = await InstructorScheduleService.checkConflicts({
                mothraId: clinician.mothraId,
                rotationId: rotation.rotId,
                weekIds: [week.weekId],
                gradYear: params.year,
            })

            // Handle conflicts if any
            if (conflictResult.result && conflictResult.result.length > 0) {
                const [conflict] = conflictResult.result
                if (conflict) {
                    const proceed = await confirmConflictDialog(clinician.fullName, conflict.name)
                    if (!proceed) {
                        return false
                    }
                }
            }

            // Add instructor using the store
            const result = await scheduleStore.addInstructorToSchedule({
                mothraId: clinician.mothraId,
                rotationId: rotation.rotId,
                weekIds: [week.weekId],
                gradYear: params.year,
                isPrimaryEvaluator: scheduleStore.requiresPrimaryEvaluator(week),
            })

            if (result.success) {
                // Update local state for optimistic UI update
                const scheduleData = scheduleStore.currentRotationSchedule
                if (scheduleData) {
                    addScheduleToWeek(scheduleData, week.weekId, {
                        instructorScheduleId:
                            result.result?.schedules?.[0]?.instructorScheduleId || result.result?.scheduleIds?.[0] || 0,
                        mothraId: clinician.mothraId,
                        clinicianName: clinician.fullName,
                        isPrimaryEvaluator: scheduleStore.requiresPrimaryEvaluator(week),
                    })
                }

                // Show success message
                notifyAddSuccess(clinician.fullName, week.weekNumber, result.result?.warningMessage)
                return true
            }
            throw new Error(result.errors?.join(", ") || "Failed to add instructor")
        } catch (err) {
            notifyError(formatError(err, "Failed to schedule clinician"))
            return false
        }
    }

    return {
        scheduleClinicianToWeek,
    }
}
