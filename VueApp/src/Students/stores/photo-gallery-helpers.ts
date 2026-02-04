/**
 * Photo Gallery Utilities
 *
 * Shared functions for organizing and categorizing students by their group memberships.
 * Used across multiple components for consistent student grouping and display.
 */
import type { StudentPhoto } from "../services/photo-gallery-service"

/**
 * Maps a student's group membership to the corresponding group value
 * based on the specified group type (eighths, twentieths, teams, v3specialty)
 */
function getStudentGroupValue(student: StudentPhoto, groupType: string | null): string | null {
    if (!groupType) {
        return null
    }

    switch (groupType) {
        case "eighths": {
            return student.eighthsGroup
        }
        case "twentieths": {
            return student.twentiethsGroup
        }
        case "teams": {
            return student.teamNumber
        }
        case "v3specialty": {
            return student.v3SpecialtyGroup
        }
        default: {
            return null
        }
    }
}

/**
 * Calculates the number of students in each group across all group types
 * Used for displaying group counts in the UI
 */
function calculateGroupCounts(students: StudentPhoto[]): Record<string, Record<string, number>> {
    const counts = {
        eighths: {} as Record<string, number>,
        twentieths: {} as Record<string, number>,
        teams: {} as Record<string, number>,
        v3specialty: {} as Record<string, number>,
    }

    for (const student of students) {
        if (student.eighthsGroup) {
            counts.eighths[student.eighthsGroup] = (counts.eighths[student.eighthsGroup] || 0) + 1
        }
        if (student.twentiethsGroup) {
            counts.twentieths[student.twentiethsGroup] = (counts.twentieths[student.twentiethsGroup] || 0) + 1
        }
        if (student.teamNumber) {
            counts.teams[student.teamNumber] = (counts.teams[student.teamNumber] || 0) + 1
        }
        if (student.v3SpecialtyGroup) {
            counts.v3specialty[student.v3SpecialtyGroup] = (counts.v3specialty[student.v3SpecialtyGroup] || 0) + 1
        }
    }
    return counts
}

/**
 * Groups students by their membership in the specified group type and sorts them.
 * Students without a group assignment are placed in an "Unassigned" group.
 * Groups are sorted naturally (handles "Team 1", "Team 10" correctly) with Unassigned last.
 */
function groupStudentsByType(students: StudentPhoto[], groupType: string | null): Map<string, StudentPhoto[]> {
    const grouped = new Map<string, StudentPhoto[]>()

    if (!groupType) {
        // No grouping - return all students in a single group
        grouped.set(
            "All Students",
            students.toSorted((a: StudentPhoto, b: StudentPhoto) => a.lastName.localeCompare(b.lastName)),
        )
        return grouped
    }

    // Group students
    for (const student of students) {
        const groupValue = getStudentGroupValue(student, groupType)
        if (groupValue) {
            if (!grouped.has(groupValue)) {
                grouped.set(groupValue, [])
            }
            grouped.get(groupValue)!.push(student)
        } else {
            // Students without a group go into "Unassigned"
            if (!grouped.has("Unassigned")) {
                grouped.set("Unassigned", [])
            }
            grouped.get("Unassigned")!.push(student)
        }
    }

    // Sort students within each group by lastName
    for (const [groupName, groupStudents] of grouped) {
        grouped.set(
            groupName,
            groupStudents.toSorted((a: StudentPhoto, b: StudentPhoto) => a.lastName.localeCompare(b.lastName)),
        )
    }

    // Sort the map keys (group names) naturally
    const sortedGrouped = new Map<string, StudentPhoto[]>()
    const sortedKeys = [...grouped.keys()].toSorted((a: string, b: string) => {
        // Put "Unassigned" at the end
        if (a === "Unassigned") {
            return 1
        }
        if (b === "Unassigned") {
            return -1
        }
        // Natural sort for group names (handles "Team 1", "Team 2", "Team 10" correctly)
        return a.localeCompare(b, undefined, { numeric: true })
    })

    for (const key of sortedKeys) {
        sortedGrouped.set(key, grouped.get(key)!)
    }

    return sortedGrouped
}

export { getStudentGroupValue, calculateGroupCounts, groupStudentsByType }
