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

export type { AuditLogEntry, AuditModifier }
