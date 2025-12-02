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
    canEdit: boolean
}

interface RotationPermissionCheck {
    canEdit: boolean
}

interface InstructorSchedulePermissionCheck {
    canEditOwn: boolean
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
    type InstructorScheduleRequest,
    type InstructorScheduleResponse,
    type ScheduleConflict,
    type SetPrimaryEvaluatorRequest,
    type AuditEntry,
}
