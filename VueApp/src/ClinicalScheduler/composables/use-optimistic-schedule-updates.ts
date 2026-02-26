import { ref } from "vue"
import type { RotationScheduleData } from "../services/rotation-service"
import type { ClinicianScheduleData } from "../services/clinician-service"
import { InstructorScheduleService } from "../services/instructor-schedule-service"
import { useScheduleStateUpdater } from "./use-schedule-state-updater"
import { isErrorKind } from "../services/error-transformer"
import { safeDeepClone } from "../utils/safe-deep-clone"
import {
    isRotationSchedule,
    updateClinicianScheduleWithRotation,
    removeRotationFromClinicianSchedule,
    updatePrimaryInClinicianSchedule,
} from "../utils/schedule-update-helpers"
import type {
    ScheduleAssignmentData,
    AddScheduleParams,
    TogglePrimaryParams,
    ScheduleData,
} from "../utils/schedule-update-helpers"

interface OptimisticUpdateOptions {
    // Called after operation completes successfully. Receives
    // (wasPrimary?: boolean, instructorName?: string) so callers
    // Can present contextual notifications (e.g. primary evaluator removed).
    onSuccess?: (wasPrimary?: boolean, instructorName?: string) => void
    onError?: (error: string) => void
}

async function addClinicianToRotation(
    rotationData: RotationScheduleData,
    weekId: number,
    assignmentData: ScheduleAssignmentData,
): Promise<number> {
    const result = await InstructorScheduleService.addInstructor({
        MothraId: assignmentData.clinicianMothraId!,
        RotationId: rotationData.rotation?.rotId || 0,
        WeekIds: [weekId],
        GradYear: assignmentData.gradYear,
        IsPrimaryEvaluator: assignmentData.isPrimary ?? false,
    })

    if (!result.success) {
        // Use typed error if available, fallback to string errors
        if (result.error && isErrorKind(result.error, "PermissionError")) {
            throw new Error(`Permission denied: ${result.error.message}`)
        }
        throw new Error(result.errors.join(", "))
    }

    const scheduleId = result.result?.scheduleIds?.[0]
    if (!scheduleId) {
        throw new Error("API returned success but no schedule ID - this indicates a server error")
    }
    return scheduleId
}

async function addRotationToClinician(
    clinicianData: ClinicianScheduleData,
    weekId: number,
    assignmentData: ScheduleAssignmentData,
): Promise<number> {
    const result = await InstructorScheduleService.addInstructor({
        MothraId: clinicianData.clinician?.mothraId || "",
        RotationId: assignmentData.rotationId!,
        WeekIds: [weekId],
        GradYear: assignmentData.gradYear,
        IsPrimaryEvaluator: assignmentData.isPrimary ?? false,
    })

    if (!result.success) {
        // Use typed error if available, fallback to string errors
        if (result.error && isErrorKind(result.error, "PermissionError")) {
            throw new Error(`Permission denied: ${result.error.message}`)
        }
        throw new Error(result.errors.join(", "))
    }

    const scheduleId = result.result?.scheduleIds?.[0]
    if (!scheduleId) {
        throw new Error("API returned success but no schedule ID - this indicates a server error")
    }
    return scheduleId
}

async function handleAddSchedule(params: AddScheduleParams, options: OptimisticUpdateOptions, addScheduleToWeek: any) {
    const { scheduleData, weekId, assignmentData } = params
    let newScheduleId = 0

    if (isRotationSchedule(scheduleData)) {
        if (!assignmentData.clinicianMothraId || !assignmentData.clinicianName) {
            throw new Error("Clinician information required for rotation schedule")
        }

        newScheduleId = await addClinicianToRotation(scheduleData, weekId, assignmentData)
        addScheduleToWeek(scheduleData, weekId, {
            instructorScheduleId: newScheduleId,
            mothraId: assignmentData.clinicianMothraId,
            clinicianName: assignmentData.clinicianName,
            isPrimaryEvaluator: assignmentData.isPrimary ?? false,
        })
    } else {
        if (!assignmentData.rotationId) {
            throw new Error("Rotation ID required for clinician schedule")
        }

        newScheduleId = await addRotationToClinician(scheduleData, weekId, assignmentData)
        updateClinicianScheduleWithRotation({
            clinicianData: scheduleData,
            weekId,
            scheduleId: newScheduleId,
            assignmentData,
        })
    }

    options.onSuccess?.()
}

interface RemoveScheduleParams {
    scheduleData: ScheduleData
    scheduleId: number
    removeScheduleFromWeek: any
}

async function handleRemoveSchedule(params: RemoveScheduleParams, options: OptimisticUpdateOptions) {
    const { scheduleData, scheduleId, removeScheduleFromWeek } = params

    // Perform optimistic UI update immediately
    if (isRotationSchedule(scheduleData)) {
        removeScheduleFromWeek(scheduleData, scheduleId)
    } else {
        removeRotationFromClinicianSchedule(scheduleData, scheduleId)
    }

    const result = await InstructorScheduleService.removeInstructor(scheduleId)
    if (!result.success) {
        // Enhanced error handling with typed errors
        if (result.error) {
            // Provide specific feedback for conflict errors (e.g., primary evaluator)
            if (isErrorKind(result.error, "ConflictError")) {
                throw new Error(`Cannot remove: ${result.error.message}`)
            }
            // Permission errors get special handling
            if (isErrorKind(result.error, "PermissionError")) {
                throw new Error(`Access denied: ${result.error.message}`)
            }
        }
        throw new Error(result.errors.join(", "))
    }

    // Prepare data for the view to handle notifications
    const wasPrimary = result.result?.wasPrimaryEvaluator || false
    const instructorName = result.result?.instructorName || "Instructor"

    // Let the view handle all notifications through onSuccess callback
    options.onSuccess?.(wasPrimary, instructorName)
}

async function handleTogglePrimary(
    params: TogglePrimaryParams,
    options: OptimisticUpdateOptions,
    updateSchedulePrimaryStatus: any,
) {
    const { scheduleData, scheduleId, isPrimary, requiresPrimaryEvaluator } = params

    if (isRotationSchedule(scheduleData)) {
        updateSchedulePrimaryStatus(scheduleData, scheduleId, isPrimary)
    } else {
        updatePrimaryInClinicianSchedule(scheduleData, scheduleId, isPrimary)
    }

    const result = await InstructorScheduleService.setPrimaryEvaluator(scheduleId, isPrimary, requiresPrimaryEvaluator)
    if (!result.success) {
        // Enhanced error handling with typed errors
        if (result.error) {
            // Provide context-aware messages based on error type
            if (isErrorKind(result.error, "NotFoundError")) {
                throw new Error("Schedule entry not found. Please refresh the page.")
            }
            if (isErrorKind(result.error, "PermissionError")) {
                throw new Error(`Permission denied: ${result.error.message}`)
            }
        }
        throw new Error(result.errors.join(", "))
    }

    options.onSuccess?.()
}

/**
 * Composable for managing schedule updates with rollback capability.
 *
 * Behavior:
 * - ADD operations: Pessimistic - waits for API success before updating UI
 * - REMOVE operations: Optimistic - updates UI immediately, rolls back on failure
 * - TOGGLE PRIMARY operations: Optimistic - updates UI immediately, rolls back on failure
 *
 * All operations maintain a rollback state to restore original data on failure.
 * Operations are queued to ensure sequential processing without blocking the UI.
 */
export function useScheduleUpdatesWithRollback() {
    const { addScheduleToWeek, removeScheduleFromWeek, updateSchedulePrimaryStatus } = useScheduleStateUpdater()
    const rollbackState = ref<ScheduleData | null>(null)
    const operationQueue: Array<() => Promise<void>> = []
    const isProcessingQueue = ref(false)

    const handleRollback = (scheduleData: ScheduleData) => {
        if (rollbackState.value) {
            Object.assign(scheduleData, rollbackState.value)
            rollbackState.value = null
        }
    }

    interface ErrorContext {
        scheduleData: ScheduleData
        error: unknown
        options: OptimisticUpdateOptions
        defaultMsg: string
    }

    const handleError = (context: ErrorContext) => {
        const { scheduleData, error, options, defaultMsg } = context
        handleRollback(scheduleData)
        const errorMessage = error instanceof Error ? error.message : defaultMsg
        options.onError?.(errorMessage)
        throw error
    }

    const processQueue = async () => {
        if (isProcessingQueue.value || operationQueue.length === 0) {
            return
        }

        isProcessingQueue.value = true

        while (operationQueue.length > 0) {
            const operation = operationQueue.shift()
            if (operation) {
                try {
                    // eslint-disable-next-line no-await-in-loop -- Operations must be processed sequentially to maintain order
                    await operation()
                } catch {
                    // Error already handled in the operation
                    // The error has been processed and user has been notified
                    // Through the error handling in the operation itself
                }
            }
        }

        isProcessingQueue.value = false
    }

    const queueOperation = (operation: () => Promise<void>) => {
        operationQueue.push(operation)
        // Start processing if not already running
        processQueue()
    }

    return {
        /**
         * Add a schedule assignment (pessimistic - waits for API success)
         * Creates a rollback snapshot but only updates UI after successful API call
         * Operations are queued for sequential processing
         */
        addScheduleWithRollback(params: AddScheduleParams, options: OptimisticUpdateOptions = {}) {
            queueOperation(async () => {
                try {
                    rollbackState.value = safeDeepClone(params.scheduleData)
                    await handleAddSchedule(params, options, addScheduleToWeek)
                    rollbackState.value = null
                } catch (error) {
                    handleError({
                        scheduleData: params.scheduleData,
                        error,
                        options,
                        defaultMsg: "Failed to add schedule",
                    })
                }
            })
        },

        /**
         * Remove a schedule assignment (optimistic - updates UI immediately)
         * Updates UI first, then calls API. Rolls back UI on API failure.
         * Operations are queued for sequential processing
         */
        removeScheduleWithRollback(
            scheduleData: ScheduleData,
            scheduleId: number,
            options: OptimisticUpdateOptions = {},
        ) {
            queueOperation(async () => {
                try {
                    rollbackState.value = safeDeepClone(scheduleData)
                    await handleRemoveSchedule({ scheduleData, scheduleId, removeScheduleFromWeek }, options)
                    rollbackState.value = null
                } catch (error) {
                    handleError({ scheduleData, error, options, defaultMsg: "Failed to remove schedule" })
                }
            })
        },

        /**
         * Toggle primary evaluator status (optimistic - updates UI immediately)
         * Updates UI first, then calls API. Rolls back UI on API failure.
         * Operations are queued for sequential processing
         */
        togglePrimaryWithRollback(params: TogglePrimaryParams, options: OptimisticUpdateOptions = {}) {
            queueOperation(async () => {
                try {
                    rollbackState.value = safeDeepClone(params.scheduleData)
                    await handleTogglePrimary(params, options, updateSchedulePrimaryStatus)
                    rollbackState.value = null
                } catch (error) {
                    handleError({
                        scheduleData: params.scheduleData,
                        error,
                        options,
                        defaultMsg: "Failed to update primary status",
                    })
                }
            })
        },
    }
}
