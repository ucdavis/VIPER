/**
 * TypeScript types for the Effort system.
 */

type TermDto = {
    termCode: number
    termName: string
    status: string
    harvestedDate: string | null
    openedDate: string | null
    closedDate: string | null
    isOpen: boolean
    canEdit: boolean
    // State transition properties for term management UI
    canOpen: boolean
    canClose: boolean
    canReopen: boolean
    canUnopen: boolean
    canDelete: boolean
}

type PersonDto = {
    personId: number
    termCode: number
    firstName: string
    lastName: string
    middleInitial: string | null
    fullName: string
    effortTitleCode: string
    effortDept: string
    percentAdmin: number
    title: string | null
    adminUnit: string | null
    effortVerified: string | null
    reportUnit: string | null
    percentClinical: number | null
    isVerified: boolean
}

type CourseDto = {
    id: number
    crn: string
    termCode: number
    subjCode: string
    crseNumb: string
    seqNumb: string
    courseCode: string
    enrollment: number
    units: number
    custDept: string
    /** Parent course ID if this course is linked as a child. Null/undefined if not a child. */
    parentCourseId?: number | null
}

type RecordDto = {
    id: number
    courseId: number
    personId: number
    termCode: number
    sessionType: string
    role: number
    roleDescription: string
    hours: number | null
    weeks: number | null
    crn: string
    modifiedDate: string | null
    effortValue: number | null
    effortLabel: string
}

type AvailableTermDto = {
    termCode: number
    termName: string
    startDate: string
}

// Audit types
type ChangeDetail = {
    oldValue: string | null
    newValue: string | null
}

type EffortAuditRow = {
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
    changesDetail: Record<string, ChangeDetail> | null
}

type ModifierInfo = {
    personId: number
    fullName: string
}

type TermOptionDto = {
    termCode: number
    termName: string
}

// Course management types
type BannerCourseDto = {
    crn: string
    subjCode: string
    crseNumb: string
    seqNumb: string
    title: string
    enrollment: number
    unitType: string // F=Fixed, V=Variable
    unitLow: number
    unitHigh: number
    deptCode: string
    courseCode: string
    isVariableUnits: boolean
    alreadyImported: boolean
    importedUnitValues: number[]
}

type CreateCourseRequest = {
    termCode: number
    crn: string
    subjCode: string
    crseNumb: string
    seqNumb: string
    enrollment: number
    units: number
    custDept: string
}

type UpdateCourseRequest = {
    enrollment: number
    units: number
    custDept: string
}

type ImportCourseRequest = {
    termCode: number
    crn: string
    units?: number // For variable-unit courses
}

// Course relationship types
type CourseRelationshipDto = {
    id: number
    parentCourseId: number
    childCourseId: number
    relationshipType: "CrossList" | "Section"
    childCourse?: CourseDto
    parentCourse?: CourseDto
}

type CourseRelationshipsResult = {
    parentRelationship: CourseRelationshipDto | null
    childRelationships: CourseRelationshipDto[]
}

type CreateCourseRelationshipRequest = {
    childCourseId: number
    relationshipType: "CrossList" | "Section"
}

export type {
    TermDto,
    PersonDto,
    CourseDto,
    RecordDto,
    AvailableTermDto,
    EffortAuditRow,
    ChangeDetail,
    ModifierInfo,
    TermOptionDto,
    BannerCourseDto,
    CreateCourseRequest,
    UpdateCourseRequest,
    ImportCourseRequest,
    CourseRelationshipDto,
    CourseRelationshipsResult,
    CreateCourseRelationshipRequest,
}
