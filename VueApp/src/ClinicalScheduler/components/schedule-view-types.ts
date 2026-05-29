export interface WeekItem {
    weekId: number
    weekNumber: number
    dateStart: string
    dateEnd?: string
    requiresPrimaryEvaluator?: boolean
    [key: string]: unknown
}

export interface ScheduleAssignment {
    id: number
    displayName: string
    isPrimary?: boolean
    [key: string]: unknown
}

export interface ScheduleSemester {
    semester?: string // For clinician view
    name?: string // For rotation view
    displayName?: string // For rotation view
    weeks: (WeekItem & { dateEnd: string })[]
}

export type ViewMode = "rotation" | "clinician"
