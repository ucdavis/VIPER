// Clinical Scheduler types will be defined here

export interface Rotation {
    rotId: number
    serviceId: number
    name: string
    abbreviation: string
    subjectCode?: string
    courseNumber?: string
}

export interface Service {
    serviceId: number
    serviceName: string
    shortName: string
    scheduleEditPermission?: string | null
    userCanEdit?: boolean | null
}

// Permission-related types
export interface User {
    mothraId: string
    displayName: string
}

export interface UserPermissions {
    user: User
    permissions: {
        hasManagePermission: boolean
        servicePermissions: Record<number, boolean>
        editableServiceCount: number
    }
    editableServices: Service[]
}

export interface ServicePermissionCheck {
    serviceId: number
    canEdit: boolean
    requiredPermission: string
    user: User
}

export interface RotationPermissionCheck {
    rotationId: number
    canEdit: boolean
    user: User
}

export interface PermissionSummary {
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
export interface InstructorScheduleRequest {
    mothraId: string
    rotationId: number
    weekIds: number[]
    isPrimaryEvaluator?: boolean
}

export interface InstructorScheduleResponse {
    scheduleIds: number[]
    message?: string
}

export interface ScheduleConflict {
    weekId: number
    weekNumber: number
    rotationId: number
    rotationName: string
    dateStart: string
    dateEnd: string
    isAlreadyScheduled: boolean
}

export interface SetPrimaryEvaluatorRequest {
    isPrimary: boolean
}

export interface AuditEntry {
    auditId: number
    action: string
    details: string
    modifiedBy: string
    modifiedDate: string
    mothraId?: string
    instructorName?: string
}