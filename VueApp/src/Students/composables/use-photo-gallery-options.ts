import { computed } from "vue"
import type { Ref } from "vue"
import type { ClassYear, CoursesByTerm } from "../services/photo-gallery-service"

/**
 * Composable for building photo gallery dropdown options
 * Extracts option-building logic to reduce component complexity
 */
export function usePhotoGalleryOptions(classYears: Ref<ClassYear[]>, availableCourses: Ref<CoursesByTerm[]>) {
    // Shared class year options - used by both Photo Gallery and Student List
    const classYearOptions = computed(() =>
        classYears.value.map((cy) => ({
            label: `${cy.year} (${cy.classLevel})`,
            year: cy.year,
            classLevel: cy.classLevel,
        })),
    )

    // Photo Gallery uses combined options with both class years and courses
    const classLevelOptions = computed(() => {
        const options: Array<{
            label: string
            value: string | null
            type?: "class" | "course"
            termCode?: string
            crn?: string
            disable?: boolean
            header?: boolean
        }> = [{ label: "Select Class Year or Course", value: null }]

        // Add "Class Level" section header
        if (classYearOptions.value.length > 0) {
            options.push({
                label: "Class Level",
                value: null,
                disable: true,
                header: true,
            })

            // Add class years under the header
            for (const option of classYearOptions.value) {
                options.push({
                    label: option.label,
                    value: option.classLevel,
                    type: "class",
                })
            }
        }

        // Add courses grouped by term with section headers
        for (const termGroup of availableCourses.value) {
            if (termGroup.courses.length > 0) {
                // Add term section header (e.g., "Fall Semester 2025")
                options.push({
                    label: termGroup.termDescription,
                    value: null,
                    disable: true,
                    header: true,
                })

                // Add courses under this term, showing course number and title
                for (const course of termGroup.courses) {
                    options.push({
                        label: `${course.subjectCode}${course.courseNumber} - ${course.title}`,
                        value: `course:${course.termCode}:${course.crn}`,
                        type: "course",
                        termCode: course.termCode,
                        crn: course.crn,
                    })
                }
            }
        }

        return options
    })

    // Student List uses year as the value
    const studentListYearOptions = computed(() => [
        { label: "Select Class Year", value: null },
        ...classYearOptions.value.map((option) => ({
            label: option.label,
            value: option.year,
        })),
    ])

    return {
        classYearOptions,
        classLevelOptions,
        studentListYearOptions,
    }
}
