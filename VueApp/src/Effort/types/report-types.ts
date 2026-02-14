/**
 * Effort by type mapping (effort type ID -> hours/value).
 */
type EffortByType = Record<string, number>

/**
 * Course row in teaching activity report.
 */
type TeachingActivityCourseRow = {
    termCode: number
    courseId: number
    course: string
    crn: string
    units: number
    enrollment: number
    roleId: string
    effortByType: EffortByType
}

/**
 * Instructor group in teaching activity report.
 */
type TeachingActivityInstructorGroup = {
    mothraId: string
    instructor: string
    jobGroupId: string
    courses: TeachingActivityCourseRow[]
    instructorTotals: EffortByType
}

/**
 * Department group in teaching activity report.
 */
type TeachingActivityDepartmentGroup = {
    department: string
    instructors: TeachingActivityInstructorGroup[]
    departmentTotals: EffortByType
}

/**
 * Complete teaching activity report response.
 */
type TeachingActivityReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    filterPerson: string | null
    filterRole: string | null
    filterTitle: string | null
    effortTypes: string[]
    departments: TeachingActivityDepartmentGroup[]
}

/**
 * Filter parameters for report queries.
 */
type ReportFilterParams = {
    termCode?: number
    academicYear?: string
    department?: string
    personId?: number
    role?: string
    title?: string
}

/**
 * Option in the term/academic year dropdown.
 */
type TermDropdownOption = {
    label: string
    value: number | string
    isYear: boolean
}

export type {
    EffortByType,
    TeachingActivityCourseRow,
    TeachingActivityInstructorGroup,
    TeachingActivityDepartmentGroup,
    TeachingActivityReport,
    ReportFilterParams,
    TermDropdownOption,
}
