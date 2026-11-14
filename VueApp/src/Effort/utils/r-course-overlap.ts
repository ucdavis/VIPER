import type { InstructorEffortRecordDto } from "../types"

const hasEffort = (r: InstructorEffortRecordDto) => (r.hours ?? 0) > 0 || (r.weeks ?? 0) > 0

/**
 * Department R-courses (e.g. VME 433R-001) that have effort alongside effort on the
 * generic RES 000R-001, which may mean the same teaching is counted twice.
 * Returns an empty list when the generic R-course has no effort.
 */
function overlappingRCourses(records: InstructorEffortRecordDto[]): string[] {
    const withEffort = records.filter(hasEffort)
    if (!withEffort.some((r) => r.course.isGenericRCourse)) {
        return []
    }
    const codes = withEffort
        .filter((r) => r.course.isRCourse && !r.course.isGenericRCourse)
        .map((r) => `${r.course.courseCode}-${r.course.seqNumb}`)
    return [...new Set(codes)]
}

export { overlappingRCourses }
