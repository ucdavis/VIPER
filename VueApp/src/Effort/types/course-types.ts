/**
 * Course management types for the Effort system.
 */

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
    // Course classification flags for effort type filtering
    /** True if this is a DVM or VET course (SubjCode is "DVM" or "VET"). */
    isDvm: boolean
    /** True if this is a 199/299 course (course number starts with 199 or 299). */
    is199299: boolean
    /** True if this is an R-course (course number ends with "R"). */
    isRCourse: boolean
}

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
    CourseDto,
    BannerCourseDto,
    CreateCourseRequest,
    UpdateCourseRequest,
    ImportCourseRequest,
    CourseRelationshipDto,
    CourseRelationshipsResult,
    CreateCourseRelationshipRequest,
}
