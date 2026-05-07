// Types shared between ScheduleView.vue and plain .ts consumers (composables,
// tests). Lives here because env.d.ts's `*.vue` wildcard hides Vue SFC named
// exports from non-Vue importers.

interface ScheduleAssignment {
    id: number
    displayName: string
    isPrimary?: boolean
    [key: string]: unknown
}

interface ScheduleSemester {
    semester?: string // For clinician view
    name?: string // For rotation view
    displayName?: string // For rotation view
    weeks: {
        weekId: number
        weekNumber: number
        dateStart: string
        dateEnd: string
        requiresPrimaryEvaluator?: boolean
        [key: string]: unknown
    }[]
}

export type { ScheduleAssignment, ScheduleSemester }
