/**
 * Percentage assignment types for the Effort system.
 */

type PercentageDto = {
    id: number
    personId: number
    percentAssignTypeId: number
    typeName: string
    typeClass: string
    unitId: number | null
    unitName: string | null
    modifier: string | null
    comment: string | null
    percentageValue: number
    startDate: string
    endDate: string | null
    compensated: boolean
    isActive: boolean
    modifiedDate: string | null
}

type CreatePercentageRequest = {
    personId: number
    percentAssignTypeId: number
    unitId: number
    modifier: string | null
    comment: string | null
    percentageValue: number
    startDate: string
    endDate: string | null
    compensated: boolean
}

type UpdatePercentageRequest = Omit<CreatePercentageRequest, "personId">

type PercentageValidationResult = {
    isValid: boolean
    errors: string[]
    warnings: string[]
}

export type { PercentageDto, CreatePercentageRequest, UpdatePercentageRequest, PercentageValidationResult }
