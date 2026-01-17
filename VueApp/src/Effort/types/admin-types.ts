/**
 * Admin/configuration types for the Effort system.
 */

type UnitDto = {
    id: number
    name: string
    isActive: boolean
    usageCount: number
    canDelete: boolean
}

type CreateUnitRequest = {
    name: string
}

type UpdateUnitRequest = {
    name: string
    isActive: boolean
}

type EffortTypeDto = {
    id: string
    description: string
    usesWeeks: boolean
    isActive: boolean
    facultyCanEnter: boolean
    allowedOnDvm: boolean
    allowedOn199299: boolean
    allowedOnRCourses: boolean
    usageCount: number
    canDelete: boolean
}

type CreateEffortTypeRequest = {
    id: string
    description: string
    usesWeeks?: boolean
    facultyCanEnter?: boolean
    allowedOnDvm?: boolean
    allowedOn199299?: boolean
    allowedOnRCourses?: boolean
}

type UpdateEffortTypeRequest = {
    description: string
    usesWeeks: boolean
    isActive: boolean
    facultyCanEnter: boolean
    allowedOnDvm: boolean
    allowedOn199299: boolean
    allowedOnRCourses: boolean
}

export type {
    UnitDto,
    CreateUnitRequest,
    UpdateUnitRequest,
    EffortTypeDto,
    CreateEffortTypeRequest,
    UpdateEffortTypeRequest,
}
