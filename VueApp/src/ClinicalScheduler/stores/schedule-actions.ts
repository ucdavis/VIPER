/**
 * Action functions for the schedule store
 */
import type { Ref } from "vue"
import { RotationService } from "../services/rotation-service"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import { ClinicianService } from "../services/clinician-service"
import { ScheduleCache, scheduleCache } from "./schedule-cache"
import type { RotationScheduleData, RecentClinician, RotationWithService } from "../types/rotation-types"
import type { ApiResult } from "../types/api"

interface LoadingStates {
    rotationSchedule: boolean
    clinicianSchedule: boolean
    addingInstructor: boolean
    removingInstructor: boolean
    settingPrimary: boolean
}

interface Errors {
    rotationSchedule: string | null
    clinicianSchedule: string | null
    operation: string | null
}

interface LoadRotationScheduleParams {
    rotationId: number
    year: number | undefined
    currentYear: Ref<number | null>
    rotationSchedules: Ref<Map<string, RotationScheduleData>>
    recentClinicians: Ref<RecentClinician[]>
    loadingStates: Ref<LoadingStates>
    errors: Ref<Errors>
}

interface LoadClinicianScheduleParams {
    mothraId: string
    year: number | undefined
    currentYear: Ref<number | null>
    clinicianSchedules: Ref<Map<string, any>>
    recentRotations: Ref<RotationWithService[]>
    loadingStates: Ref<LoadingStates>
    errors: Ref<Errors>
}

interface RemoveInstructorParams {
    scheduleId: number
    loadingStates: Ref<LoadingStates>
    errors: Ref<Errors>
}

interface SetPrimaryEvaluatorParams {
    scheduleId: number
    isPrimary: boolean
    loadingStates: Ref<LoadingStates>
    errors: Ref<Errors>
}

/**
 * Load rotation schedule with caching
 */
async function loadRotationSchedule(params: LoadRotationScheduleParams): Promise<ApiResult<RotationScheduleData>> {
    const { rotationId, year, currentYear, rotationSchedules, recentClinicians, loadingStates, errors } = params
    const effectiveYear = year || currentYear.value || new Date().getFullYear()
    const key = ScheduleCache.getRotationKey(rotationId, effectiveYear)

    // Check cache first
    const cached = rotationSchedules.value.get(key)
    if (cached && !scheduleCache.isStale(key)) {
        return { success: true, result: cached, errors: [] } as ApiResult<RotationScheduleData>
    }

    loadingStates.value.rotationSchedule = true
    errors.value.rotationSchedule = null

    try {
        const result = await RotationService.getRotationSchedule(rotationId, { year: effectiveYear })

        if (result.success && result.result) {
            rotationSchedules.value.set(key, result.result)
            scheduleCache.markFresh(key)

            // Update recent clinicians if available
            if (result.result.recentClinicians) {
                recentClinicians.value = result.result.recentClinicians
            }
        } else {
            errors.value.rotationSchedule = result.errors?.join(", ") || "Failed to load rotation schedule"
        }

        return result
    } catch {
        const errorMessage = "An unexpected error occurred while loading rotation schedule"
        errors.value.rotationSchedule = errorMessage
        return { success: false, result: null as any, errors: [errorMessage] }
    } finally {
        loadingStates.value.rotationSchedule = false
    }
}

/**
 * Load clinician schedule with caching
 */
async function loadClinicianSchedule(params: LoadClinicianScheduleParams): Promise<ApiResult<any>> {
    const { mothraId, year, currentYear, clinicianSchedules, recentRotations, loadingStates, errors } = params
    const effectiveYear = year || currentYear.value || new Date().getFullYear()
    const key = ScheduleCache.getClinicianKey(mothraId, effectiveYear)

    // Check cache first
    const cached = clinicianSchedules.value.get(key)
    if (cached && !scheduleCache.isStale(key)) {
        return { success: true, result: cached, errors: [] } as ApiResult<any>
    }

    loadingStates.value.clinicianSchedule = true
    errors.value.clinicianSchedule = null

    try {
        const result = await ClinicianService.getClinicianSchedule(mothraId, { year: effectiveYear })

        if (result.success && result.result) {
            clinicianSchedules.value.set(key, result.result)
            scheduleCache.markFresh(key)

            // Update recent rotations if available (check for property existence)
            if (result.result && "recentRotations" in result.result) {
                recentRotations.value = (result.result as any).recentRotations
            }
        } else {
            errors.value.clinicianSchedule = result.errors?.join(", ") || "Failed to load clinician schedule"
        }

        return result
    } catch {
        const errorMessage = "An unexpected error occurred while loading clinician schedule"
        errors.value.clinicianSchedule = errorMessage
        return { success: false, result: null as any, errors: [errorMessage] }
    } finally {
        loadingStates.value.clinicianSchedule = false
    }
}

/**
 * Add instructor to schedule
 */
async function addInstructorToSchedule(
    params: {
        mothraId: string
        rotationId: number
        weekIds: number[]
        gradYear: number
        isPrimaryEvaluator?: boolean
    },
    loadingStates: Ref<LoadingStates>,
    errors: Ref<Errors>,
): Promise<ApiResult<any>> {
    loadingStates.value.addingInstructor = true
    errors.value.operation = null

    try {
        const result = await InstructorScheduleService.addInstructor({
            MothraId: params.mothraId,
            RotationId: params.rotationId,
            WeekIds: params.weekIds,
            GradYear: params.gradYear,
            IsPrimaryEvaluator: params.isPrimaryEvaluator || false,
        })

        if (result.success) {
            // Invalidate cache for this rotation
            const key = ScheduleCache.getRotationKey(params.rotationId, params.gradYear)
            scheduleCache.invalidate(key)
        } else {
            errors.value.operation = result.errors?.join(", ") || "Failed to add instructor"
        }

        return result
    } catch {
        const errorMessage = "An unexpected error occurred while adding instructor"
        errors.value.operation = errorMessage
        return { success: false, result: null, errors: [errorMessage] }
    } finally {
        loadingStates.value.addingInstructor = false
    }
}

/**
 * Remove instructor from schedule
 */
async function removeInstructorFromSchedule(params: RemoveInstructorParams): Promise<ApiResult<any>> {
    const { scheduleId, loadingStates, errors } = params
    loadingStates.value.removingInstructor = true
    errors.value.operation = null

    try {
        const result = await InstructorScheduleService.removeInstructor(scheduleId)

        if (result.success) {
            // Invalidate all caches as we don't know which rotation was affected
            scheduleCache.invalidateAll()
        } else {
            errors.value.operation = result.errors?.join(", ") || "Failed to remove instructor"
        }

        return result
    } catch {
        const errorMessage = "An unexpected error occurred while removing instructor"
        errors.value.operation = errorMessage
        return { success: false, result: null, errors: [errorMessage] }
    } finally {
        loadingStates.value.removingInstructor = false
    }
}

/**
 * Set primary evaluator status
 */
async function setPrimaryEvaluator(params: SetPrimaryEvaluatorParams): Promise<ApiResult<any>> {
    const { scheduleId, isPrimary, loadingStates, errors } = params
    loadingStates.value.settingPrimary = true
    errors.value.operation = null

    try {
        const result = await InstructorScheduleService.setPrimaryEvaluator(scheduleId, isPrimary)

        if (result.success) {
            // Invalidate all caches as we don't know which rotation was affected
            scheduleCache.invalidateAll()
        } else {
            errors.value.operation = result.errors?.join(", ") || "Failed to set primary evaluator"
        }

        return result
    } catch {
        const errorMessage = "An unexpected error occurred while setting primary evaluator"
        errors.value.operation = errorMessage
        return { success: false, result: null, errors: [errorMessage] }
    } finally {
        loadingStates.value.settingPrimary = false
    }
}

// Consolidated exports
export {
    loadRotationSchedule,
    loadClinicianSchedule,
    addInstructorToSchedule,
    removeInstructorFromSchedule,
    setPrimaryEvaluator,
}
