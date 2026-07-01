import type { QTableColumn, QVueGlobals } from "quasar"
import type { CourseRelationshipDto } from "../types"

// Columns for the child course-relationship table, shared by CourseDetail and
// CourseLinkDialog. Returns a fresh array so callers don't share mutable state.
function courseRelationshipColumns(): QTableColumn[] {
    return [
        {
            name: "course",
            label: "Course",
            field: (row: CourseRelationshipDto) =>
                row.childCourse ? `${row.childCourse.courseCode}-${row.childCourse.seqNumb}` : "",
            align: "left",
            sortable: true,
        },
        {
            name: "crn",
            label: "CRN",
            field: (row: CourseRelationshipDto) => row.childCourse?.crn ?? "",
            align: "left",
        },
        {
            name: "enrollment",
            label: "Enrollment",
            field: (row: CourseRelationshipDto) => row.childCourse?.enrollment ?? 0,
            align: "left",
        },
        {
            name: "units",
            label: "Units",
            field: (row: CourseRelationshipDto) => row.childCourse?.units ?? 0,
            align: "left",
        },
        {
            name: "relationshipType",
            label: "Type",
            field: "relationshipType",
            align: "center",
        },
        {
            name: "actions",
            label: "Actions",
            field: "actions",
            align: "center",
        },
    ]
}

/**
 * Confirm removal of a course link via a Quasar dialog, running onConfirm when
 * the user accepts.
 */
function confirmRemoveCourseLink($q: QVueGlobals, relationship: CourseRelationshipDto, onConfirm: () => void): void {
    const { childCourse } = relationship
    const courseName = childCourse ? `${childCourse.courseCode}-${childCourse.seqNumb}` : "this course"

    $q.dialog({
        title: "Remove Link",
        message: `Are you sure you want to remove the link to ${courseName}?`,
        cancel: true,
        persistent: true,
    }).onOk(onConfirm)
}

export { courseRelationshipColumns, confirmRemoveCourseLink }
