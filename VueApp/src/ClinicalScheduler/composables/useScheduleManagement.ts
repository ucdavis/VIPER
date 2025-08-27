import { ref } from 'vue'
import { useQuasar } from 'quasar'
import InstructorScheduleService from '../services/InstructorScheduleService'

export interface ScheduleAddRequest {
    mothraId: string
    rotationId: number
    weekId: number
    year: number
    isPrimaryEvaluator?: boolean
    clinicianName?: string
    rotationName?: string
}

export interface ScheduleUpdateCallbacks {
    onSuccess?: (result: any) => void
    onConflict?: (conflicts: any[]) => Promise<boolean>
    onError?: (error: Error) => void
}

export function useScheduleManagement() {
    const $q = useQuasar()
    const isAddingSchedule = ref(false)
    const isRemovingSchedule = ref(false)
    const isUpdatingSchedule = ref(false)

    /**
     * Add an instructor to a schedule with conflict checking
     */
    async function addInstructorToSchedule(
        request: ScheduleAddRequest,
        callbacks?: ScheduleUpdateCallbacks
    ) {
        isAddingSchedule.value = true

        try {
            // Check for conflicts first
            const conflictResult = await InstructorScheduleService.checkConflicts(
                request.mothraId,
                request.rotationId,
                [request.weekId],
                request.year
            )

            if (conflictResult.result && conflictResult.result.length > 0) {
                const shouldProceed = callbacks?.onConflict 
                    ? await callbacks.onConflict(conflictResult.result)
                    : await showConflictDialog(conflictResult.result, request)
                
                if (!shouldProceed) {
                    return { success: false, cancelled: true }
                }
            }

            // Add the instructor to the schedule
            const result = await InstructorScheduleService.addInstructor({
                MothraId: request.mothraId,
                RotationId: request.rotationId,
                WeekIds: [request.weekId],
                GradYear: request.year,
                IsPrimaryEvaluator: request.isPrimaryEvaluator || false
            })

            if (result.success) {
                showSuccessNotification(result, request)
                callbacks?.onSuccess?.(result)
                return result
            } else {
                throw new Error(result.errors.join(', '))
            }
        } catch (error) {
            console.error('Error scheduling instructor:', error)
            const err = error instanceof Error ? error : new Error('Unknown error')
            
            if (callbacks?.onError) {
                callbacks.onError(err)
            } else {
                showErrorNotification(err, request)
            }
            
            return { success: false, error: err }
        } finally {
            isAddingSchedule.value = false
        }
    }

    /**
     * Remove an instructor from a schedule
     */
    async function removeInstructorFromSchedule(
        scheduleId: number,
        rotationName?: string,
        callbacks?: ScheduleUpdateCallbacks
    ) {
        isRemovingSchedule.value = true

        try {
            const confirmed = await showRemovalConfirmation(rotationName)
            if (!confirmed) {
                return { success: false, cancelled: true }
            }

            const result = await InstructorScheduleService.removeInstructor(scheduleId)
            
            if (result.success) {
                $q.notify({
                    type: 'positive',
                    message: rotationName 
                        ? `${rotationName} removed from week`
                        : 'Schedule removed successfully'
                })
                callbacks?.onSuccess?.(result)
                return result
            } else {
                throw new Error(result.errors.join(', '))
            }
        } catch (error) {
            console.error('Error removing schedule:', error)
            const err = error instanceof Error ? error : new Error('Unknown error')
            
            if (callbacks?.onError) {
                callbacks.onError(err)
            } else {
                $q.notify({
                    type: 'negative',
                    message: err.message || 'Failed to remove schedule'
                })
            }
            
            return { success: false, error: err }
        } finally {
            isRemovingSchedule.value = false
        }
    }

    /**
     * Update primary evaluator status
     */
    async function updatePrimaryEvaluator(
        scheduleId: number,
        isPrimary: boolean,
        callbacks?: ScheduleUpdateCallbacks
    ) {
        isUpdatingSchedule.value = true

        try {
            const result = await InstructorScheduleService.updatePrimaryEvaluator(
                scheduleId,
                isPrimary
            )

            if (result.success) {
                $q.notify({
                    type: 'positive',
                    message: isPrimary 
                        ? 'Set as primary evaluator'
                        : 'Removed as primary evaluator'
                })
                callbacks?.onSuccess?.(result)
                return result
            } else {
                throw new Error(result.errors.join(', '))
            }
        } catch (error) {
            console.error('Error updating primary evaluator:', error)
            const err = error instanceof Error ? error : new Error('Unknown error')
            
            if (callbacks?.onError) {
                callbacks.onError(err)
            } else {
                $q.notify({
                    type: 'negative',
                    message: 'Failed to update primary evaluator status'
                })
            }
            
            return { success: false, error: err }
        } finally {
            isUpdatingSchedule.value = false
        }
    }

    /**
     * Show conflict dialog
     */
    async function showConflictDialog(
        conflicts: any[],
        request: ScheduleAddRequest
    ): Promise<boolean> {
        const conflictNames = conflicts.map(c => c.rotationName).join(', ')
        const clinicianName = request.clinicianName || 'This instructor'
        const rotationName = request.rotationName || 'this rotation'
        
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title: 'Multiple Rotation Assignment',
                message: `${clinicianName} is already scheduled for ${conflictNames} during this week. Do you want to add ${rotationName} as an additional rotation?`,
                cancel: true,
                persistent: true,
                ok: {
                    label: 'Add Rotation',
                    color: 'primary'
                },
                cancel: {
                    label: 'Cancel'
                }
            }).onOk(() => resolve(true))
              .onCancel(() => resolve(false))
        })
    }

    /**
     * Show removal confirmation
     */
    async function showRemovalConfirmation(rotationName?: string): Promise<boolean> {
        const message = rotationName 
            ? `Remove ${rotationName} from this week's schedule?`
            : 'Remove this schedule assignment?'
            
        return new Promise<boolean>((resolve) => {
            $q.dialog({
                title: 'Confirm Removal',
                message: message,
                cancel: true,
                persistent: true
            }).onOk(() => resolve(true))
              .onCancel(() => resolve(false))
        })
    }

    /**
     * Show success notification
     */
    function showSuccessNotification(result: any, request: ScheduleAddRequest) {
        let message = request.rotationName 
            ? `✅ ${request.rotationName} scheduled successfully`
            : '✅ Schedule added successfully'
            
        if (result.result?.warningMessage) {
            message += `\n\n${result.result.warningMessage}`
        }
        
        $q.notify({
            type: result.result?.warningMessage ? 'warning' : 'positive',
            message: message,
            html: false, // Explicitly prevent HTML rendering to avoid XSS
            timeout: result.result?.warningMessage ? 6000 : 4000,
            multiLine: result.result?.warningMessage ? true : false
        })
    }

    /**
     * Show error notification with meaningful messages
     */
    function showErrorNotification(error: Error, request?: ScheduleAddRequest) {
        let userMessage = 'Failed to update schedule'
        
        if (error.message.includes('already scheduled') || error.message.includes('already exists')) {
            userMessage = request?.rotationName 
                ? `${request.rotationName} is already scheduled for this week`
                : 'This assignment already exists'
        } else if (error.message.includes('permission')) {
            userMessage = 'You do not have permission to update this schedule'
        } else if (error.message.includes('not found')) {
            userMessage = 'The selected item was not found'
        } else if (error.message) {
            userMessage = error.message
        }
        
        $q.notify({
            type: 'negative',
            message: userMessage,
            timeout: 4000
        })
    }

    return {
        isAddingSchedule,
        isRemovingSchedule,
        isUpdatingSchedule,
        addInstructorToSchedule,
        removeInstructorFromSchedule,
        updatePrimaryEvaluator,
        showConflictDialog,
        showRemovalConfirmation
    }
}