/**
 * Types for evaluation data display and ad-hoc entry.
 * Used by EvaluationStatusMatrix and EditEvaluationDialog components.
 */

/**
 * Course info for evaluation matrix column headers.
 */
type EvalCourseInfoDto = {
    courseId: number
    courseName: string
    crn: string
}

/**
 * Evaluation entry for one instructor on one course.
 */
type CourseEvalEntryDto = {
    courseId: number
    crn: string
    status: "HarvestedEval" | "AdHoc" | "None"
    canEdit: boolean
    quantId: number | null
    mean: number | null
    standardDeviation: number | null
    respondents: number | null
    count1: number | null
    count2: number | null
    count3: number | null
    count4: number | null
    count5: number | null
}

/**
 * Per-instructor evaluation status across all courses.
 */
type InstructorEvalStatusDto = {
    personId: number
    mothraId: string
    instructorName: string
    evaluations: CourseEvalEntryDto[]
}

/**
 * Full evaluation status response for a course (and its children).
 */
type CourseEvaluationStatusDto = {
    canEditAdHoc: boolean
    maxRatingCount: number
    instructors: InstructorEvalStatusDto[]
    courses: EvalCourseInfoDto[]
}

/**
 * Request to create an ad-hoc evaluation.
 */
type CreateAdHocEvalRequest = {
    courseId: number
    mothraId: string
    count1: number
    count2: number
    count3: number
    count4: number
    count5: number
}

/**
 * Request to update an ad-hoc evaluation.
 */
type UpdateAdHocEvalRequest = {
    count1: number
    count2: number
    count3: number
    count4: number
    count5: number
}

/**
 * Result of ad-hoc evaluation create/update.
 */
type AdHocEvalResultDto = {
    success: boolean
    error?: string
    quantId?: number
}

export type {
    EvalCourseInfoDto,
    CourseEvalEntryDto,
    InstructorEvalStatusDto,
    CourseEvaluationStatusDto,
    CreateAdHocEvalRequest,
    UpdateAdHocEvalRequest,
    AdHocEvalResultDto,
}
