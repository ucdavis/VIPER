/**
 * Harvest types for the Effort system.
 */

type HarvestPersonPreview = {
    mothraId: string
    personId: number
    fullName: string
    firstName: string
    lastName: string
    department: string
    titleCode: string
    titleDescription: string
    source: string
    isNew: boolean
}

type HarvestCoursePreview = {
    crn: string
    subjCode: string
    crseNumb: string
    seqNumb: string
    enrollment: number
    units: number
    custDept: string
    source: string
    isNew: boolean
}

type HarvestRecordPreview = {
    mothraId: string
    personName: string
    crn: string
    courseCode: string
    effortType: string
    hours: number | null
    weeks: number | null
    roleId: number
    roleName: string
    source: string
}

type HarvestSummary = {
    totalInstructors: number
    totalCourses: number
    totalEffortRecords: number
    guestAccounts: number
}

type HarvestWarning = {
    phase: string
    message: string
    details: string
}

type HarvestError = {
    phase: string
    message: string
    details: string
}

type HarvestPreviewDto = {
    termCode: number
    termName: string
    crestInstructors: HarvestPersonPreview[]
    crestCourses: HarvestCoursePreview[]
    crestEffort: HarvestRecordPreview[]
    nonCrestInstructors: HarvestPersonPreview[]
    nonCrestCourses: HarvestCoursePreview[]
    nonCrestEffort: HarvestRecordPreview[]
    clinicalInstructors: HarvestPersonPreview[]
    clinicalCourses: HarvestCoursePreview[]
    clinicalEffort: HarvestRecordPreview[]
    guestAccounts: HarvestPersonPreview[]
    removedInstructors: HarvestPersonPreview[]
    removedCourses: HarvestCoursePreview[]
    summary: HarvestSummary
    warnings: HarvestWarning[]
    errors: HarvestError[]
}

type HarvestResultDto = {
    success: boolean
    termCode: number
    harvestedDate: string | null
    summary: HarvestSummary
    warnings: HarvestWarning[]
    errorMessage: string | null
}

export type {
    HarvestPersonPreview,
    HarvestCoursePreview,
    HarvestRecordPreview,
    HarvestSummary,
    HarvestWarning,
    HarvestError,
    HarvestPreviewDto,
    HarvestResultDto,
}
