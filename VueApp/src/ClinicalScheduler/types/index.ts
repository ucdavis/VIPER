// Clinical Scheduler types will be defined here

// View context type for filtering clinicians
type ViewContext = "clinician" | "rotation"

interface Rotation {
    rotId: number
    serviceId: number
    name: string
    abbreviation: string
    subjectCode?: string
    courseNumber?: string
}

interface Service {
    serviceId: number
    serviceName: string
    scheduleEditPermission?: string | null
    userCanEdit?: boolean | null
}

// Permission-related types
interface User {
    mothraId: string
    displayName: string
}

interface UserPermissions {
    user: User
    permissions: {
        hasAdminPermission: boolean
        hasManagePermission: boolean
        hasEditClnSchedulesPermission: boolean
        hasEditOwnSchedulePermission: boolean
        servicePermissions: Record<number, boolean>
        editableServiceCount: number
    }
    editableServices: Service[]
}

interface ServicePermissionCheck {
    serviceId: number
    canEdit: boolean
    requiredPermission: string
    user: User
}

interface RotationPermissionCheck {
    rotationId: number
    canEdit: boolean
    user: User
}

interface InstructorSchedulePermissionCheck {
    instructorScheduleId: number
    canEditOwn: boolean
    user: User
}

interface PermissionSummary {
    user: User
    summary: {
        totalServices: number
        editableServices: number
        servicesWithCustomPermissions: number
        defaultPermission: string
    }
    services: Array<{
        serviceId: number
        serviceName: string
        shortName: string
        requiredPermission: string
        userCanEdit: boolean
    }>
}

// Instructor Schedule types for edit functionality
interface InstructorScheduleRequest {
    mothraId: string
    rotationId: number
    weekIds: number[]
    isPrimaryEvaluator?: boolean
}

interface InstructorScheduleResponse {
    scheduleIds: number[]
    message?: string
}

interface ScheduleConflict {
    weekId: number
    weekNumber: number
    rotationId: number
    name: string
    dateStart: string
    dateEnd: string
    isAlreadyScheduled: boolean
}

interface SetPrimaryEvaluatorRequest {
    isPrimary: boolean
}

interface AuditEntry {
    auditId: number
    action: string
    details: string
    modifiedBy: string
    modifiedDate: string
    mothraId?: string
    instructorName?: string
}

export {
    type ViewContext,
    type Rotation,
    type Service,
    type User,
    type UserPermissions,
    type ServicePermissionCheck,
    type RotationPermissionCheck,
    type InstructorSchedulePermissionCheck,
    type PermissionSummary,
    type InstructorScheduleRequest,
    type InstructorScheduleResponse,
    type ScheduleConflict,
    type SetPrimaryEvaluatorRequest,
    type AuditEntry,
}
