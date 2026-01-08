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
    jobGroupId: string | null
    title: string | null
    adminUnit: string | null
    effortVerified: string | null
    reportUnit: string | null
    volunteerWos: boolean
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

// Instructor management types
type AaudPersonDto = {
    personId: number
    firstName: string
    lastName: string
    middleInitial: string | null
    fullName: string
    effortDept: string | null
    deptName: string | null
    titleCode: string | null
    title: string | null
    jobGroupId: string | null
}

type CreateInstructorRequest = {
    personId: number
    termCode: number
}

type UpdateInstructorRequest = {
    effortDept: string
    effortTitleCode: string
    jobGroupId: string | null
    reportUnits: string[] | null
    volunteerWos: boolean
}

type ReportUnitDto = {
    abbrev: string
    unit: string
}

type DepartmentDto = {
    code: string
    name: string
    group: string
}

type CanDeleteResult = {
    canDelete: boolean
    recordCount: number
}

// Effort Type types (read-only)
type EffortTypeDto = {
    id: number
    class: string
    name: string
    showOnTemplate: boolean
    isActive: boolean
    instructorCount: number
}

// Instructor by type types
type InstructorByTypeDto = {
    personId: number
    firstName: string
    lastName: string
    fullName: string
    academicYear: string
}

type InstructorsByTypeResponseDto = {
    typeId: number
    typeName: string
    typeClass: string
    instructors: InstructorByTypeDto[]
}

type InstructorEffortRecordDto = {
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
    course: CourseDto
}

type TitleCodeDto = {
    code: string
    name: string
}

type JobGroupDto = {
    code: string
    name: string
}

// Unit management types
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

// Session Type management types
type SessionTypeDto = {
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

type CreateSessionTypeRequest = {
    id: string
    description: string
    usesWeeks?: boolean
    facultyCanEnter?: boolean
    allowedOnDvm?: boolean
    allowedOn199299?: boolean
    allowedOnRCourses?: boolean
}

type UpdateSessionTypeRequest = {
    description: string
    usesWeeks: boolean
    isActive: boolean
    facultyCanEnter: boolean
    allowedOnDvm: boolean
    allowedOn199299: boolean
    allowedOnRCourses: boolean
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
    AaudPersonDto,
    CreateInstructorRequest,
    UpdateInstructorRequest,
    ReportUnitDto,
    DepartmentDto,
    CanDeleteResult,
    InstructorEffortRecordDto,
    TitleCodeDto,
    JobGroupDto,
    EffortTypeDto,
    InstructorByTypeDto,
    InstructorsByTypeResponseDto,
    UnitDto,
    CreateUnitRequest,
    UpdateUnitRequest,
    SessionTypeDto,
    CreateSessionTypeRequest,
    UpdateSessionTypeRequest,
}
