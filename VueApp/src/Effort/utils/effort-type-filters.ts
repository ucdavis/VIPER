import type { EffortTypeOptionDto } from "../types"

type CourseClassification = {
    isDvm: boolean
    is199299: boolean
    isRCourse: boolean
}

/** Filter effort types to only those allowed for the given course classification. */
function filterEffortTypesByCourse(
    effortTypes: EffortTypeOptionDto[],
    course: CourseClassification,
): EffortTypeOptionDto[] {
    return effortTypes.filter((et) => {
        if (course.isDvm && !et.allowedOnDvm) {
            return false
        }
        if (course.is199299 && !et.allowedOn199299) {
            return false
        }
        if (course.isRCourse && !et.allowedOnRCourses) {
            return false
        }
        return true
    })
}

export { filterEffortTypesByCourse }
export type { CourseClassification }
