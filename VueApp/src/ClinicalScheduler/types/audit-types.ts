/**
 * Types for the schedule-change audit log.
 */

interface AuditLogEntry {
    scheduleAuditId: number
    area: string
    mothraId: string | null
    personName: string
    action: string
    rotationId: number | null
    rotationName: string
    weekId: number | null
    weekNum: number
    weekStart: string | null
    term: string
    modifiedBy: string
    modifiedByName: string
    timeStamp: string
}

// A selectable person option in the audit trail filters — reused for both the
// "Modified By" (change author) and "Person" (affected student/clinician) dropdowns.
interface AuditModifier {
    mothraId: string
    displayName: string
}

// A selectable term option for the audit trail "Term" filter, scoped to a grad year.
interface AuditTerm {
    termCode: number
    term: string
}

export type { AuditLogEntry, AuditModifier, AuditTerm }
