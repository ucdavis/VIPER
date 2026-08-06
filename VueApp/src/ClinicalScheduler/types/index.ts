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
    shortName?: string
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

export {
    type ViewContext,
    type Rotation,
    type Service,
    type User,
    type UserPermissions,
    type ServicePermissionCheck,
    type RotationPermissionCheck,
    type InstructorSchedulePermissionCheck,
}
