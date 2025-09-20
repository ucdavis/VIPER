import { defineStore } from "pinia"
import { scheduleCache } from "./schedule-cache"
import { createScheduleState } from "./schedule-state"
import {
    loadRotationSchedule,
    loadClinicianSchedule,
    addInstructorToSchedule,
    removeInstructorFromSchedule,
    setPrimaryEvaluator,
} from "./schedule-actions"
import type { RotationWithService } from "../types/rotation-types"
import type { Clinician } from "../services/clinician-service"

export const useScheduleStore = defineStore("schedule", () => {
    // Create state and getters
    const {
        rotationSchedules,
        clinicianSchedules,
        currentYear,
        selectedRotation,
        selectedClinician,
        recentClinicians,
        recentRotations,
        loadingStates,
        errors,
        currentRotationSchedule,
        currentClinicianSchedule,
        isLoading,
        getWeekAssignments,
        requiresPrimaryEvaluator,
    } = createScheduleState()

    // Delegate to action functions
    const loadRotationScheduleAction = (rotationId: number, year?: number) =>
        loadRotationSchedule({
            rotationId,
            year,
            currentYear,
            rotationSchedules,
            recentClinicians,
            loadingStates,
            errors,
        })

    const loadClinicianScheduleAction = (mothraId: string, year?: number) =>
        loadClinicianSchedule({
            mothraId,
            year,
            currentYear,
            clinicianSchedules,
            recentRotations,
            loadingStates,
            errors,
        })

    const addInstructorToScheduleAction = (params: {
        mothraId: string
        rotationId: number
        weekIds: number[]
        gradYear: number
        isPrimaryEvaluator?: boolean
    }) => addInstructorToSchedule(params, loadingStates, errors)

    const removeInstructorFromScheduleAction = (scheduleId: number) =>
        removeInstructorFromSchedule({ scheduleId, loadingStates, errors })

    const setPrimaryEvaluatorAction = (scheduleId: number, isPrimary: boolean) =>
        setPrimaryEvaluator({ scheduleId, isPrimary, loadingStates, errors })

    // Cache management
    function clearScheduleCache() {
        rotationSchedules.value.clear()
        clinicianSchedules.value.clear()
        scheduleCache.invalidateAll()
        errors.value = {
            rotationSchedule: null,
            clinicianSchedule: null,
            operation: null,
        }
    }

    // Setters
    function setCurrentYear(year: number | null) {
        currentYear.value = year
    }

    function setSelectedRotation(rotation: RotationWithService | null) {
        selectedRotation.value = rotation
    }

    function setSelectedClinician(clinician: Clinician | null) {
        selectedClinician.value = clinician
    }

    return {
        // State
        rotationSchedules,
        clinicianSchedules,
        currentYear,
        selectedRotation,
        selectedClinician,
        recentClinicians,
        recentRotations,
        loadingStates,
        errors,

        // Getters
        currentRotationSchedule,
        currentClinicianSchedule,
        isLoading,

        // Helper functions
        getWeekAssignments,
        requiresPrimaryEvaluator,

        // Actions
        loadRotationSchedule: loadRotationScheduleAction,
        loadClinicianSchedule: loadClinicianScheduleAction,
        addInstructorToSchedule: addInstructorToScheduleAction,
        removeInstructorFromSchedule: removeInstructorFromScheduleAction,
        setPrimaryEvaluator: setPrimaryEvaluatorAction,
        clearScheduleCache,

        // Setters
        setCurrentYear,
        setSelectedRotation,
        setSelectedClinician,
    }
})
