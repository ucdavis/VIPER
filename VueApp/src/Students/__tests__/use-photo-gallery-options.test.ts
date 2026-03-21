import { ref } from "vue"
import { usePhotoGalleryOptions } from "../composables/use-photo-gallery-options"
import type { ClassYear, CourseInfo } from "../services/photo-gallery-service"

describe(usePhotoGalleryOptions, () => {
    describe("classYearOptions", () => {
        it("should generate correct class year options with year and classLevel", () => {
            const classYears = ref<ClassYear[]>([
                { year: 2025, classLevel: "V4" },
                { year: 2026, classLevel: "V3" },
            ])
            const availableCourses = ref<CourseInfo[]>([])

            const { classYearOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classYearOptions.value).toEqual([
                { label: "2025 (V4)", year: 2025, classLevel: "V4" },
                { label: "2026 (V3)", year: 2026, classLevel: "V3" },
            ])
        })

        it("should handle empty class years", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([])

            const { classYearOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classYearOptions.value).toEqual([])
        })
    })

    describe("classLevelOptions", () => {
        it("should include default option at the beginning", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classLevelOptions.value[0]).toEqual({
                label: "Select Class Year or Course",
                value: null,
            })
        })

        it("should add Class Level section header when class years exist", () => {
            const classYears = ref<ClassYear[]>([{ year: 2025, classLevel: "V4" }])
            const availableCourses = ref<CourseInfo[]>([])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classLevelOptions.value[1]).toEqual({
                label: "Class Level",
                value: null,
                disable: true,
                header: true,
            })
        })

        it("should add class year options under the header", () => {
            const classYears = ref<ClassYear[]>([
                { year: 2025, classLevel: "V4" },
                { year: 2026, classLevel: "V3" },
            ])
            const availableCourses = ref<CourseInfo[]>([])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classLevelOptions.value[2]).toEqual({
                label: "2025 (V4)",
                value: "V4",
                type: "class",
            })
            expect(classLevelOptions.value[3]).toEqual({
                label: "2026 (V3)",
                value: "V3",
                type: "class",
            })
        })

        it("should group courses by term with section headers", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([
                {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Introduction to Veterinary Medicine",
                    termDescription: "Fall Semester 2025",
                },
            ])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            // Should have: default option, then term header
            expect(classLevelOptions.value[1]).toEqual({
                label: "Fall Semester 2025",
                value: null,
                disable: true,
                header: true,
            })
        })

        it("should format course labels as SUBJ123 - Title", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([
                {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Introduction to Veterinary Medicine",
                    termDescription: "Fall Semester 2025",
                },
            ])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(classLevelOptions.value[2]).toEqual({
                label: "VMD101 - Introduction to Veterinary Medicine",
                value: "course:202501:12345",
                type: "course",
                termCode: "202501",
                crn: "12345",
            })
        })

        it("should handle multiple courses in the same term", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([
                {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Intro Course",
                    termDescription: "Fall Semester 2025",
                },
                {
                    termCode: "202501",
                    crn: "67890",
                    subjectCode: "VMD",
                    courseNumber: "201",
                    title: "Advanced Course",
                    termDescription: "Fall Semester 2025",
                },
            ])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            // Should have: default, term header, two courses
            expect(classLevelOptions.value).toHaveLength(4)
            expect(classLevelOptions.value[1]!.label).toBe("Fall Semester 2025")
            expect(classLevelOptions.value[2]!.label).toBe("VMD101 - Intro Course")
            expect(classLevelOptions.value[3]!.label).toBe("VMD201 - Advanced Course")
        })

        it("should skip when no courses available", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            // Should only have the default option
            expect(classLevelOptions.value).toHaveLength(1)
        })

        it("should combine class years and courses correctly", () => {
            const classYears = ref<ClassYear[]>([{ year: 2025, classLevel: "V4" }])
            const availableCourses = ref<CourseInfo[]>([
                {
                    termCode: "202501",
                    crn: "12345",
                    subjectCode: "VMD",
                    courseNumber: "101",
                    title: "Test Course",
                    termDescription: "Fall Semester 2025",
                },
            ])

            const { classLevelOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            // Should have: default, class header, class option, term header, course option
            expect(classLevelOptions.value).toHaveLength(5)
            expect(classLevelOptions.value[0]!.label).toBe("Select Class Year or Course")
            expect(classLevelOptions.value[1]!.label).toBe("Class Level")
            expect(classLevelOptions.value[2]!.label).toBe("2025 (V4)")
            expect(classLevelOptions.value[3]!.label).toBe("Fall Semester 2025")
            expect(classLevelOptions.value[4]!.label).toBe("VMD101 - Test Course")
        })
    })
})

describe("usePhotoGalleryOptions - studentListYearOptions", () => {
    describe("studentListYearOptions", () => {
        it("should create options with year as value", () => {
            const classYears = ref<ClassYear[]>([
                { year: 2025, classLevel: "V4" },
                { year: 2026, classLevel: "V3" },
            ])
            const availableCourses = ref<CourseInfo[]>([])

            const { studentListYearOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(studentListYearOptions.value).toEqual([
                { label: "Select Class Year", value: null },
                { label: "2025 (V4)", value: 2025 },
                { label: "2026 (V3)", value: 2026 },
            ])
        })

        it("should include default option at the beginning", () => {
            const classYears = ref<ClassYear[]>([])
            const availableCourses = ref<CourseInfo[]>([])

            const { studentListYearOptions } = usePhotoGalleryOptions(classYears, availableCourses)

            expect(studentListYearOptions.value).toEqual([{ label: "Select Class Year", value: null }])
        })
    })
})
