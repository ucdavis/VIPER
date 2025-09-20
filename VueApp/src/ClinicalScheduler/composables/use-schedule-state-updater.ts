import type { RotationScheduleData } from "../services/rotation-service"
import type { RecentClinician } from "../types/rotation-types"

interface ScheduleEntry {
    instructorScheduleId: number
    mothraId: string
    instructorName?: string
    fullName?: string
    isPrimaryEvaluator: boolean
}

interface WeekWithSchedules {
    weekId: number
    schedules?: ScheduleEntry[]
    instructorSchedules?: ScheduleEntry[]
}

interface SemesterWithWeeks {
    semester: string
    weeks: WeekWithSchedules[]
}

/**
 * Find a week by ID across all semesters
 */
function findWeekInSemesters(semesters: SemesterWithWeeks[], weekId: number): WeekWithSchedules | null {
    for (const semester of semesters) {
        const week = semester.weeks.find((w) => w.weekId === weekId)
        if (week) {
            return week
        }
    }
    return null
}

/**
 * Find a week containing a specific schedule ID
 */
function findWeekByScheduleId(semesters: SemesterWithWeeks[], scheduleId: number): WeekWithSchedules | null {
    for (const semester of semesters) {
        const week = semester.weeks.find((w) => {
            const schedules = w.instructorSchedules || []
            return schedules.some((s: ScheduleEntry) => s.instructorScheduleId === scheduleId)
        })
        if (week) {
            return week
        }
    }
    return null
}

/**
 * Add clinician to recent list if not already present
 */
function addToRecentClinicians(scheduleData: RotationScheduleData, mothraId: string, clinicianName: string): void {
    if (
        scheduleData.recentClinicians &&
        !scheduleData.recentClinicians.some((c: RecentClinician) => c.mothraId === mothraId)
    ) {
        scheduleData.recentClinicians.push({
            fullName: clinicianName,
            mothraId: mothraId,
        })
    }
}

/**
 * Clear other primary evaluators in the same week
 */
function clearOtherPrimaryEvaluators(schedules: ScheduleEntry[], currentScheduleId: number): void {
    for (const s of schedules) {
        if (s.instructorScheduleId !== currentScheduleId) {
            s.isPrimaryEvaluator = false
        }
    }
}

/**
 * Add a new schedule to a specific week
 */
function addScheduleToWeek(
    scheduleData: RotationScheduleData,
    weekId: number,
    newSchedule: {
        instructorScheduleId: number
        mothraId: string
        clinicianName: string
        isPrimaryEvaluator: boolean
    },
): void {
    if (!scheduleData.schedulesBySemester) {
        return
    }

    const week = findWeekInSemesters(scheduleData.schedulesBySemester, weekId)
    if (!week) {
        return
    }

    // Initialize instructorSchedules array if it doesn't exist
    if (!week.instructorSchedules) {
        week.instructorSchedules = []
    }

    // Add the new schedule entry
    const scheduleEntry: ScheduleEntry = {
        instructorScheduleId: newSchedule.instructorScheduleId,
        mothraId: newSchedule.mothraId,
        fullName: newSchedule.clinicianName,
        isPrimaryEvaluator: newSchedule.isPrimaryEvaluator,
    }

    week.instructorSchedules.push(scheduleEntry)

    // Add clinician to recent list
    addToRecentClinicians(scheduleData, newSchedule.mothraId, newSchedule.clinicianName)
}

/**
 * Remove a schedule from a week
 */
function removeScheduleFromWeek(scheduleData: RotationScheduleData, scheduleId: number): boolean {
    if (!scheduleData.schedulesBySemester) {
        return false
    }

    const week = findWeekByScheduleId(scheduleData.schedulesBySemester, scheduleId)
    if (!week) {
        return false
    }

    // Use instructorSchedules for RotationScheduleView
    const schedules = week.instructorSchedules
    if (!schedules) {
        return false
    }

    const scheduleIndex = schedules.findIndex((s: ScheduleEntry) => s.instructorScheduleId === scheduleId)
    if (scheduleIndex !== -1) {
        schedules.splice(scheduleIndex, 1)
        return true
    }

    return false
}

/**
 * Update the primary evaluator status for a schedule
 */
function updateSchedulePrimaryStatus(
    scheduleData: RotationScheduleData,
    scheduleId: number,
    isPrimary: boolean,
): boolean {
    if (!scheduleData.schedulesBySemester) {
        return false
    }

    const week = findWeekByScheduleId(scheduleData.schedulesBySemester, scheduleId)
    if (!week) {
        return false
    }

    // Use instructorSchedules for RotationScheduleView
    const schedules = week.instructorSchedules
    if (!schedules) {
        return false
    }

    const schedule = schedules.find((s: ScheduleEntry) => s.instructorScheduleId === scheduleId)
    if (!schedule) {
        return false
    }

    // Update the primary status
    schedule.isPrimaryEvaluator = isPrimary

    // If setting as primary, clear other primary evaluators in the same week
    if (isPrimary) {
        clearOtherPrimaryEvaluators(schedules, scheduleId)
    }

    return true
}

export function useScheduleStateUpdater() {
    return {
        findWeekInSemesters,
        findWeekByScheduleId,
        addScheduleToWeek,
        removeScheduleFromWeek,
        updateSchedulePrimaryStatus,
    }
}
