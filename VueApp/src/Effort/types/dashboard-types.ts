import type { TermDto } from "./term-types"

/**
 * Dashboard statistics for the Effort system.
 */
type DashboardStatsDto = {
    currentTerm: TermDto | null
    totalInstructors: number
    verifiedInstructors: number
    pendingInstructors: number
    verificationPercent: number
    totalCourses: number
    coursesWithInstructors: number
    coursesWithoutInstructors: number
    totalRecords: number
    instructorsWithNoRecords: number
    instructorsWithEffortMismatch: number
    hygieneSummary: DataHygieneSummaryDto
}

/**
 * Summary of data hygiene alerts.
 */
type DataHygieneSummaryDto = {
    activeAlerts: number
    resolvedAlerts: number
    ignoredAlerts: number
}

/**
 * Department verification breakdown.
 */
type DepartmentVerificationDto = {
    departmentCode: string
    departmentName: string
    totalInstructors: number
    verifiedInstructors: number
    unverifiedInstructors: number
    verificationPercent: number
    meetsThreshold: boolean
    status: "NeedsFollowup" | "OnTrack" | "Complete"
}

/**
 * Data hygiene alert.
 */
type EffortChangeAlertDto = {
    alertType: string
    title: string
    description: string
    entityType: "Instructor" | "Course" | "Department"
    entityId: string
    entityName: string
    departmentCode: string
    severity: "High" | "Medium" | "Low"
    status: "Active" | "Resolved" | "Ignored"
    isResolved: boolean
    isIgnored: boolean
    reviewedDate: string | null
}

/**
 * Recent change from audit log for dashboard display.
 */
type RecentChangeDto = {
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
    changesDetail: Record<string, { oldValue: string | null; newValue: string | null }> | null
}

export type {
    DashboardStatsDto,
    DataHygieneSummaryDto,
    DepartmentVerificationDto,
    EffortChangeAlertDto,
    RecentChangeDto,
}
