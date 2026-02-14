/**
 * Types for course effort display and management.
 * Used by CourseEffortTable, AddCourseEffortDialog, and CourseDetail page.
 */

/**
 * Effort record as returned by GET /api/effort/courses/{courseId}/effort.
 * Each record represents one instructor's effort entry for a specific session type on the course.
 */
type CourseEffortRecordDto = {
    effortId: number
    personId: number
    instructorName: string
    effortTypeId: string
    effortTypeDescription: string
    roleId: number
    roleDescription: string
    hours: number | null
    weeks: number | null
    effortValue: number | null
    effortLabel: string
    canEdit: boolean
    canDelete: boolean
    notes: string | null
    modifiedDate: string | null
}

/**
 * Response wrapper for GET /api/effort/courses/{courseId}/effort.
 * Includes the records array plus course-level permissions.
 */
type CourseEffortResponseDto = {
    courseId: number
    termCode: number
    canAddEffort: boolean
    isChildCourse: boolean
    records: CourseEffortRecordDto[]
}

/**
 * Instructor option for the "Add Course Effort" dialog instructor dropdown.
 * Returned by GET /api/effort/courses/{courseId}/possible-instructors.
 */
type CourseInstructorOptionDto = {
    personId: number
    firstName: string
    lastName: string
    fullName: string
    effortDept: string
}

/**
 * Grouped instructor options for the add effort dialog dropdown.
 */
type PossibleCourseInstructorsDto = {
    existingInstructors: CourseInstructorOptionDto[]
    otherInstructors: CourseInstructorOptionDto[]
}

export type { CourseEffortRecordDto, CourseEffortResponseDto, CourseInstructorOptionDto, PossibleCourseInstructorsDto }
