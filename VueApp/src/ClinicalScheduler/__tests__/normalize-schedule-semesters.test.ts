import { normalizeScheduleSemesters } from "../composables/use-schedule-normalization"
import type { ScheduleSemester } from "../components/ScheduleView.vue"

describe(normalizeScheduleSemesters, () => {
    it("should return empty array when input is undefined", () => {
        const result = normalizeScheduleSemesters()
        expect(result).toEqual([])
    })

    it("should return empty array when input is null", () => {
        const result = normalizeScheduleSemesters(null as any)
        expect(result).toEqual([])
    })

    it("should normalize weeks in each semester", () => {
        const semesters: ScheduleSemester[] = [
            {
                semester: "Fall 2024",
                weeks: [
                    { weekId: 1, dateEnd: "2024-01-07" },
                    { weekId: 2, dateEnd: null },
                    { weekId: 3, dateEnd: undefined },
                ],
            },
            {
                semester: "Spring 2025",
                weeks: [
                    { weekId: 4, dateEnd: "" },
                    { weekId: 5, dateEnd: "2025-01-14" },
                ],
            },
        ]

        const result = normalizeScheduleSemesters(semesters)

        expect(result).toEqual([
            {
                semester: "Fall 2024",
                weeks: [
                    { weekId: 1, dateEnd: "2024-01-07" },
                    { weekId: 2, dateEnd: "" },
                    { weekId: 3, dateEnd: "" },
                ],
            },
            {
                semester: "Spring 2025",
                weeks: [
                    { weekId: 4, dateEnd: "" },
                    { weekId: 5, dateEnd: "2025-01-14" },
                ],
            },
        ])
    })

    it("should handle empty weeks array", () => {
        const semesters: ScheduleSemester[] = [
            {
                semester: "Fall 2024",
                weeks: [],
            },
        ]

        const result = normalizeScheduleSemesters(semesters)

        expect(result).toEqual([
            {
                semester: "Fall 2024",
                weeks: [],
            },
        ])
    })

    it("should handle empty semesters array", () => {
        const semesters: ScheduleSemester[] = []

        const result = normalizeScheduleSemesters(semesters)

        expect(result).toEqual([])
    })

    it("should preserve all semester properties", () => {
        const semesters: ScheduleSemester[] = [
            {
                semester: "Fall 2024",
                weeks: [
                    {
                        weekId: 1,
                        dateEnd: null,
                        startDate: "2024-01-01",
                        rotations: ["Surgery"],
                    },
                ],
                isActive: true,
                totalWeeks: 12,
            } as any,
        ]

        const result = normalizeScheduleSemesters(semesters)

        expect(result).toEqual([
            {
                semester: "Fall 2024",
                weeks: [
                    {
                        weekId: 1,
                        dateEnd: "",
                        startDate: "2024-01-01",
                        rotations: ["Surgery"],
                    },
                ],
                isActive: true,
                totalWeeks: 12,
            },
        ])
    })

    it("should handle complex nested week structures", () => {
        const semesters: ScheduleSemester[] = [
            {
                semester: "Fall 2024",
                weeks: [
                    {
                        weekId: 1,
                        dateEnd: null,
                        instructors: [{ name: "Dr. Smith", isPrimary: true }],
                        schedule: {
                            rotationId: 1,
                            assignments: [],
                        },
                    },
                ],
            },
        ]

        const result = normalizeScheduleSemesters(semesters)

        expect(result[0].weeks[0]).toEqual({
            weekId: 1,
            dateEnd: "",
            instructors: [{ name: "Dr. Smith", isPrimary: true }],
            schedule: {
                rotationId: 1,
                assignments: [],
            },
        })
    })
})
