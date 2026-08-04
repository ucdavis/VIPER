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
    isNew: boolean
}

type HarvestSummary = {
    totalInstructors: number
    totalCourses: number
    totalEffortRecords: number
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

type PercentRolloverItemPreview = {
    sourcePercentageId: number
    personId: number
    personName: string
    mothraId: string
    typeName: string
    typeClass: string
    percentageValue: number
    unitName: string | null
    modifier: string | null
    compensated: boolean
    currentEndDate: string
    proposedStartDate: string
    proposedEndDate: string
}

type PercentRolloverPreviewDto = {
    isRolloverApplicable: boolean
    sourceAcademicYear: number
    targetAcademicYear: number
    sourceAcademicYearDisplay: string
    targetAcademicYearDisplay: string
    oldEndDate: string
    newStartDate: string
    newEndDate: string
    assignments: PercentRolloverItemPreview[]
    existingAssignments: PercentRolloverItemPreview[]
    excludedByAudit: PercentRolloverItemPreview[]
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

/**
 * Import mode for clinical import operations.
 */
type ClinicalImportMode = "AddNewOnly" | "ClearReplace" | "Sync"

/**
 * Preview DTO for clinical import.
 */
type ClinicalImportPreviewDto = {
    mode: ClinicalImportMode
    addCount: number
    updateCount: number
    deleteCount: number
    skipCount: number
    previewGeneratedAt: string
    assignments: ClinicalAssignmentPreview[]
    warnings: string[]
}

/**
 * Preview item for a single clinical assignment.
 */
type ClinicalAssignmentPreview = {
    status: string // "New", "Update", "Delete", "Skip"
    existingRecordId: number | null
    mothraId: string
    instructorName: string
    courseNumber: string
    effortType: string
    weeks: number
    existingWeeks: number | null
    roleName: string
}

export type {
    HarvestRecordPreview,
    PercentRolloverItemPreview,
    PercentRolloverPreviewDto,
    HarvestPreviewDto,
    HarvestResultDto,
    ClinicalImportMode,
    ClinicalImportPreviewDto,
    ClinicalAssignmentPreview,
}
