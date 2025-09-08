import { useScheduleStore } from "../stores/schedule"
import { useScheduleCrud } from "./use-schedule-crud"
import { useScheduleLoaders } from "./use-schedule-loaders"
import type { WeekWithSchedules, RotationWithService } from "../types/rotation-types"
import type { Clinician } from "../services/clinician-service"

export interface ScheduleOperationParams {
    rotation?: RotationWithService | null
    clinician?: Clinician | null
    year: number
}

export function useScheduleOperations(params: ScheduleOperationParams) {
    const scheduleStore = useScheduleStore()

    // Compose functionality from other composables
    const { canEditRotation, scheduleClinicianToWeek, removeInstructorAssignment, togglePrimaryStatus } =
        useScheduleCrud({ year: params.year })

    const { loadRotationSchedule, loadClinicianSchedule } = useScheduleLoaders(params.year)

    /**
     * Check if a week requires a primary evaluator and doesn't have one
     */
    function requiresPrimaryEvaluator(week: WeekWithSchedules): boolean {
        return scheduleStore.requiresPrimaryEvaluator(week)
    }

    /**
     * Get assignments for a specific week
     */
    function getWeekAssignments(weekId: number) {
        return scheduleStore.getWeekAssignments(weekId)
    }

    return {
        // Permission checks
        canEditRotation,

        // Schedule operations
        scheduleClinicianToWeek,
        removeInstructorAssignment,
        togglePrimaryStatus,

        // Data loading
        loadRotationSchedule,
        loadClinicianSchedule,

        // Helper functions
        requiresPrimaryEvaluator,
        getWeekAssignments,

        // Loading states from store
        isLoading: scheduleStore.isLoading,
        loadingStates: scheduleStore.loadingStates,
        errors: scheduleStore.errors,
    }
}
