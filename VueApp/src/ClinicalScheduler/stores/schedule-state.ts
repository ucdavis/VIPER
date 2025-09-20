import { ref, computed } from "vue"
import type {
    RotationScheduleData,
    RotationWithService,
    RecentClinician,
    WeekWithSchedules,
} from "../types/rotation-types"
import type { Clinician } from "../services/clinician-service"

/**
 * Create the state for the schedule store
 */
export function createScheduleState() {
    // State
    const rotationSchedules = ref<Map<string, RotationScheduleData>>(new Map())
    const clinicianSchedules = ref<Map<string, any>>(new Map())
    const currentYear = ref<number | null>(null)
    const selectedRotation = ref<RotationWithService | null>(null)
    const selectedClinician = ref<Clinician | null>(null)
    const recentClinicians = ref<RecentClinician[]>([])
    const recentRotations = ref<RotationWithService[]>([])

    const loadingStates = ref({
        rotationSchedule: false,
        clinicianSchedule: false,
        addingInstructor: false,
        removingInstructor: false,
        settingPrimary: false,
    })

    const errors = ref({
        rotationSchedule: null as string | null,
        clinicianSchedule: null as string | null,
        operation: null as string | null,
    })

    // Getters
    const currentRotationSchedule = computed(() => {
        if (!selectedRotation.value || !currentYear.value) {
            return null
        }
        const key = `${selectedRotation.value.rotId}-${currentYear.value}`
        return rotationSchedules.value.get(key) || null
    })

    const currentClinicianSchedule = computed(() => {
        if (!selectedClinician.value || !currentYear.value) {
            return null
        }
        const key = `${selectedClinician.value.mothraId}-${currentYear.value}`
        return clinicianSchedules.value.get(key) || null
    })

    const isLoading = computed(() => Object.values(loadingStates.value).some(Boolean))

    // Helper function to get week assignments
    function getWeekAssignments(weekId: number): any[] {
        const schedule = currentRotationSchedule.value
        if (!schedule || !schedule.schedulesBySemester) {
            return []
        }

        for (const semester of schedule.schedulesBySemester) {
            const week = semester.weeks.find((w) => w.weekId === weekId)
            if (week && week.instructorSchedules) {
                return week.instructorSchedules.map((schedule) => ({
                    id: schedule.instructorScheduleId,
                    clinicianName: schedule.fullName,
                    isPrimary: schedule.isPrimaryEvaluator,
                    mothraId: schedule.mothraId,
                }))
            }
        }

        return []
    }

    // Helper function to check if week requires primary evaluator
    function requiresPrimaryEvaluator(week: WeekWithSchedules): boolean {
        // Check if week requires primary evaluator and doesn't have one
        if (!week.requiresPrimaryEvaluator) {
            return false
        }

        const assignments = getWeekAssignments(week.weekId)
        return !assignments.some((a) => a.isPrimary)
    }

    return {
        // State refs
        rotationSchedules,
        clinicianSchedules,
        currentYear,
        selectedRotation,
        selectedClinician,
        recentClinicians,
        recentRotations,
        loadingStates,
        errors,

        // Computed
        currentRotationSchedule,
        currentClinicianSchedule,
        isLoading,

        // Helper functions
        getWeekAssignments,
        requiresPrimaryEvaluator,
    }
}
