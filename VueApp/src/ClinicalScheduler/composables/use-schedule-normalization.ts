import { isRotationExcluded } from "../constants/rotation-constants"
import type { ScheduleSemester } from "../components/ScheduleView.vue"
import type { ClinicianScheduleData } from "../services/clinician-service"
import type { RotationScheduleData } from "../services/rotation-service"

/**
 * Shared utility for normalizing schedule data across the application.
 * Handles common transformations like ensuring dateEnd values and filtering excluded rotations.
 */

/**
 * Normalize a single week by ensuring dateEnd is always a string
 */
function normalizeWeek<T extends { dateEnd?: string | null }>(week: T): T & { dateEnd: string } {
    return {
        ...week,
        dateEnd: week.dateEnd || "",
    }
}

/**
 * Normalize all weeks in a semester
 */
function normalizeSemesterWeeks<T extends { weeks: Array<{ dateEnd?: string | null }> }>(semester: T): T {
    return {
        ...semester,
        weeks: semester.weeks.map(normalizeWeek),
    }
}

/**
 * Normalize all semesters in schedule data
 */
function normalizeScheduleSemesters(semesters: ScheduleSemester[] | undefined): ScheduleSemester[] {
    if (!semesters) {
        return []
    }
    return semesters.map(normalizeSemesterWeeks)
}

/**
 * Filter out excluded rotations from a list
 */
function filterExcludedRotations<T extends { name?: string | null }>(rotations: T[]): T[] {
    return rotations.filter((rotation) => !isRotationExcluded(rotation.name))
}

/**
 * Get a list of assigned rotation names, excluding system-excluded rotations
 */
function getAssignedRotationNames(rotations: Array<{ name?: string | null }> | undefined): string[] {
    if (!rotations) {
        return []
    }

    return rotations.map((r) => r.name).filter((name): name is string => Boolean(name) && !isRotationExcluded(name))
}

/**
 * Normalize clinician schedule data
 */
function normalizeClinicianSchedule(scheduleData: ClinicianScheduleData | null): ScheduleSemester[] {
    if (!scheduleData?.schedulesBySemester) {
        return []
    }

    return scheduleData.schedulesBySemester.map((semester) => ({
        ...semester,
        weeks: semester.weeks.map((week) => ({
            ...normalizeWeek(week),
            // Transform rotations if they exist (for clinician view)
            rotations: (week as any).rotations ? filterExcludedRotations((week as any).rotations) : undefined,
        })),
    }))
}

/**
 * Normalize rotation schedule data
 */
function normalizeRotationSchedule(
    scheduleData: RotationScheduleData | null,
): Array<{ name: string; displayName: string; weeks: any[] }> {
    if (!scheduleData?.schedulesBySemester) {
        return []
    }

    return scheduleData.schedulesBySemester.map((semester) => ({
        name: semester.semester.toLowerCase().replaceAll(/\s+/g, "-"),
        displayName: semester.semester,
        weeks: semester.weeks.map(normalizeWeek),
    }))
}

/**
 * Composable that provides schedule normalization utilities
 */
function useScheduleNormalization() {
    return {
        normalizeWeek,
        normalizeSemesterWeeks,
        normalizeScheduleSemesters,
        filterExcludedRotations,
        getAssignedRotationNames,
        normalizeClinicianSchedule,
        normalizeRotationSchedule,
    }
}

// Export all functions together
export {
    normalizeWeek,
    normalizeSemesterWeeks,
    normalizeScheduleSemesters,
    filterExcludedRotations,
    getAssignedRotationNames,
    normalizeClinicianSchedule,
    normalizeRotationSchedule,
    useScheduleNormalization,
}
