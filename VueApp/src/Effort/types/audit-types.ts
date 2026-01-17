/**
 * Audit types for the Effort system.
 */

type ChangeDetail = {
    oldValue: string | null
    newValue: string | null
}

type EffortAuditRow = {
    id: number
    tableName: string
    recordId: number
    action: string
    changedDate: string
    changedBy: number
    changedByName: string
    instructorPersonId: number | null
    instructorName: string | null
    termCode: number | null
    termName: string | null
    courseCode: string | null
    crn: string | null
    changes: string | null
    changesDetail: Record<string, ChangeDetail> | null
}

type ModifierInfo = {
    personId: number
    fullName: string
}

export type { ChangeDetail, EffortAuditRow, ModifierInfo }
