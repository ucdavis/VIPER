import type { EffortByType } from "./report-types"

// Clinical Effort types

type ClinicalEffortInstructorRow = {
    mothraId: string
    instructor: string
    clinicalPercent: number
    effortByType: EffortByType
    cliRatio: number | null
}

type ClinicalEffortJobGroup = {
    jobGroupDescription: string
    instructors: ClinicalEffortInstructorRow[]
}

type ClinicalEffortReport = {
    termName: string
    academicYear: string | null
    clinicalType: number
    clinicalTypeName: string
    effortTypes: string[]
    jobGroups: ClinicalEffortJobGroup[]
}

// Scheduled CLI Weeks types

type ScheduledCliWeeksTermRow = {
    termCode: number
    termName: string
    weeksByService: Record<string, number>
    termTotal: number
}

type ScheduledCliWeeksInstructorRow = {
    mothraId: string
    instructor: string
    terms: ScheduledCliWeeksTermRow[]
    totalWeeks: number
}

type ScheduledCliWeeksReport = {
    termName: string
    academicYear: string | null
    termNames: string[]
    services: string[]
    instructors: ScheduledCliWeeksInstructorRow[]
}

// Zero Effort types

type ZeroEffortInstructorRow = {
    mothraId: string
    instructor: string
    department: string
    jobGroupId: string
    verified: boolean
}

type ZeroEffortReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    instructors: ZeroEffortInstructorRow[]
}

// Evaluation Summary types

type EvalInstructorSummary = {
    mothraId: string
    instructor: string
    weightedAverage: number
    totalResponses: number
    totalEnrolled: number
}

type EvalDepartmentGroup = {
    department: string
    instructors: EvalInstructorSummary[]
    departmentAverage: number
    totalResponses: number
}

type EvalSummaryReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    departments: EvalDepartmentGroup[]
}

// Evaluation Detail types

type EvalCourseDetail = {
    course: string
    crn: string
    termCode: number
    role: string
    average: number
    median: number | null
    numResponses: number
    numEnrolled: number
}

type EvalDetailInstructor = {
    mothraId: string
    instructor: string
    courses: EvalCourseDetail[]
    instructorAverage: number
    instructorMedian: number | null
}

type EvalDetailDepartmentGroup = {
    department: string
    instructors: EvalDetailInstructor[]
    departmentAverage: number
}

type EvalDetailReport = {
    termCode: number
    termName: string
    academicYear: string | null
    filterDepartment: string | null
    departments: EvalDetailDepartmentGroup[]
}

// Year Statistics types

type YearStatsGrouping = {
    groupName: string
    instructorCount: number
    sums: EffortByType
    averages: EffortByType
    medians: EffortByType
    teachingHoursSum: number
    teachingHoursAverage: number
    teachingHoursMedian: number
}

type InstructorEffortDetail = {
    mothraId: string
    instructor: string
    department: string
    discipline: string
    jobGroup: string
    efforts: EffortByType
    teachingHours: number
}

type YearStatsSubReport = {
    label: string
    instructors: InstructorEffortDetail[]
    sums: EffortByType
    averages: EffortByType
    medians: EffortByType
    teachingHoursSum: number
    teachingHoursAverage: number
    teachingHoursMedian: number
    instructorCount: number
    byDepartment: YearStatsGrouping[]
    byDiscipline: YearStatsGrouping[]
    byTitle: YearStatsGrouping[]
}

type YearStatisticsReport = {
    academicYear: string
    effortTypes: string[]
    svm: YearStatsSubReport
    dvm: YearStatsSubReport
    resident: YearStatsSubReport
    undergradGrad: YearStatsSubReport
}

// Multi-Year Merit + Evaluation types

type MultiYearCourseRow = {
    course: string
    termCode: number
    units: number
    enrollment: number
    role: string
    efforts: EffortByType
}

type MultiYearMeritYear = {
    year: number
    yearLabel: string
    courses: MultiYearCourseRow[]
    yearTotals: EffortByType
}

type MultiYearMeritSection = {
    years: MultiYearMeritYear[]
    grandTotals: EffortByType
    yearlyAverages: EffortByType
    departmentAverages: EffortByType
    departmentFacultyCount: number
}

type MultiYearEvalCourse = {
    course: string
    crn: string
    termCode: number
    role: string
    average: number
    median: number | null
    numResponses: number
    numEnrolled: number
}

type MultiYearEvalYear = {
    year: number
    yearLabel: string
    courses: MultiYearEvalCourse[]
    yearAverage: number
    yearMedian: number | null
}

type MultiYearEvalSection = {
    years: MultiYearEvalYear[]
    overallAverage: number
    overallMedian: number | null
    departmentAverage: number | null
}

type MultiYearReport = {
    mothraId: string
    instructor: string
    department: string
    startYear: number
    endYear: number
    useAcademicYear: boolean
    excludedClinicalTerms: string[]
    excludedDidacticTerms: string[]
    effortTypes: string[]
    meritSection: MultiYearMeritSection
    evalSection: MultiYearEvalSection
}

type SabbaticalDto = {
    personId: number
    excludeClinicalTerms: string | null
    excludeDidacticTerms: string | null
    modifiedDate: string | null
    modifiedBy: string | null
}

export type {
    ClinicalEffortInstructorRow,
    ClinicalEffortJobGroup,
    ClinicalEffortReport,
    ScheduledCliWeeksTermRow,
    ScheduledCliWeeksInstructorRow,
    ScheduledCliWeeksReport,
    ZeroEffortInstructorRow,
    ZeroEffortReport,
    EvalInstructorSummary,
    EvalDepartmentGroup,
    EvalSummaryReport,
    EvalCourseDetail,
    EvalDetailInstructor,
    EvalDetailDepartmentGroup,
    EvalDetailReport,
    YearStatsGrouping,
    InstructorEffortDetail,
    YearStatsSubReport,
    YearStatisticsReport,
    MultiYearCourseRow,
    MultiYearMeritYear,
    MultiYearMeritSection,
    MultiYearEvalCourse,
    MultiYearEvalYear,
    MultiYearEvalSection,
    MultiYearReport,
    SabbaticalDto,
}
