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

// Dept Summary types

type DeptSummaryInstructorRow = {
    mothraId: string
    instructor: string
    jobGroupId: string
    effortByType: EffortByType
}

type DeptSummaryDepartmentGroup = {
    department: string
    instructors: DeptSummaryInstructorRow[]
    departmentTotals: EffortByType
    facultyCount: number
    facultyWithCliCount: number
    departmentAverages: EffortByType
}

type DeptSummaryReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    filterPerson: string | null
    filterRole: string | null
    filterTitle: string | null
    effortTypes: string[]
    departments: DeptSummaryDepartmentGroup[]
}

// School Summary types

type SchoolSummaryDepartmentRow = {
    department: string
    effortTotals: EffortByType
    facultyCount: number
    facultyWithCliCount: number
    averages: EffortByType
}

type SchoolSummaryTotalsRow = {
    effortTotals: EffortByType
    facultyCount: number
    facultyWithCliCount: number
    averages: EffortByType
}

type SchoolSummaryReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    filterPerson: string | null
    filterRole: string | null
    filterTitle: string | null
    effortTypes: string[]
    departments: SchoolSummaryDepartmentRow[]
    grandTotals: SchoolSummaryTotalsRow
}

// Merit Detail types

type MeritDetailCourseRow = {
    termCode: number
    courseId: number
    course: string
    units: number
    enrollment: number
    roleId: string
    effortByType: EffortByType
}

type MeritDetailInstructorGroup = {
    mothraId: string
    instructor: string
    jobGroupId: string
    jobGroupDescription: string | null
    courses: MeritDetailCourseRow[]
    instructorTotals: EffortByType
}

type MeritDetailDepartmentGroup = {
    department: string
    instructors: MeritDetailInstructorGroup[]
    departmentTotals: EffortByType
}

type MeritDetailReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    filterPersonId: number | null
    filterRole: string | null
    effortTypes: string[]
    departments: MeritDetailDepartmentGroup[]
}

// Merit Average types

type MeritAverageTermRow = {
    termCode: number
    termName: string
    effortByType: EffortByType
}

type MeritAverageInstructorRow = {
    mothraId: string
    instructor: string
    jobGroupId: string
    jobGroupDescription: string | null
    percentAdmin: number
    terms: MeritAverageTermRow[]
    effortByType: EffortByType
}

type MeritAverageDepartmentGroup = {
    department: string
    instructors: MeritAverageInstructorRow[]
    groupTotals: EffortByType
    groupAverages: EffortByType
    facultyCount: number
    facultyWithCliCount: number
}

type MeritAverageJobGroup = {
    jobGroupDescription: string
    departments: MeritAverageDepartmentGroup[]
}

type MeritAverageReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    filterPersonId: number | null
    effortTypes: string[]
    jobGroups: MeritAverageJobGroup[]
}

// Merit Summary types

type MeritSummaryDepartmentGroup = {
    department: string
    departmentTotals: EffortByType
    departmentAverages: EffortByType
    facultyCount: number
    facultyWithCliCount: number
}

type MeritSummaryJobGroup = {
    jobGroupDescription: string
    departments: MeritSummaryDepartmentGroup[]
}

type MeritSummaryReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    effortTypes: string[]
    jobGroups: MeritSummaryJobGroup[]
}

export type {
    EffortByType,
    TeachingActivityCourseRow,
    TeachingActivityInstructorGroup,
    TeachingActivityDepartmentGroup,
    TeachingActivityReport,
    ReportFilterParams,
    TermDropdownOption,
    DeptSummaryInstructorRow,
    DeptSummaryDepartmentGroup,
    DeptSummaryReport,
    SchoolSummaryDepartmentRow,
    SchoolSummaryTotalsRow,
    SchoolSummaryReport,
    MeritDetailCourseRow,
    MeritDetailInstructorGroup,
    MeritDetailDepartmentGroup,
    MeritDetailReport,
    MeritAverageTermRow,
    MeritAverageInstructorRow,
    MeritAverageDepartmentGroup,
    MeritAverageJobGroup,
    MeritAverageReport,
    MeritSummaryDepartmentGroup,
    MeritSummaryJobGroup,
    MeritSummaryReport,
}
