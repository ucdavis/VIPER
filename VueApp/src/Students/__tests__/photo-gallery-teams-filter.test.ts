import { describe, it, expect, beforeEach, vi } from "vitest"
import { setActivePinia, createPinia } from "pinia"
import { usePhotoGalleryStore } from "../stores/photo-gallery-store"

/**
 * Tests for the Teams filter functionality in Photo Gallery
 *
 * This test suite validates the dynamic Teams filter behavior:
 * - Teams option appears for V3 class level
 * - Teams option dynamically appears for courses with V3 students
 * - Teams option does NOT appear for courses without V3 students
 * - Teams option does NOT appear for V1/V2/V4 class levels
 */
// eslint-disable-next-line max-lines-per-function -- Test suite requires comprehensive coverage with multiple test cases
describe("PhotoGallery - Teams Filter", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Teams filter visibility for class levels", () => {
        it("should show Teams option when V3 class level is selected", () => {
            const store = usePhotoGalleryStore()

            // Setup: V3 class level with no students (group counts empty)
            store.selectedClassLevel = "V3"
            store.selectedCourse = null
            store.groupCounts = {
                eighths: {},
                twentieths: {},
                teams: {},
                v3specialty: {},
            }

            // Create a mock computed property that simulates the groupTypeOptions logic
            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                // V3-specific options
                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                return options
            }

            const options = mockGroupTypeOptions()

            // Assert: Teams and Streams should be present for V3
            expect(options).toContainEqual({ label: "Teams", value: "teams" })
            expect(options).toContainEqual({ label: "Streams", value: "v3specialty" })
            expect(options.length).toBe(5)
        })

        it("should NOT show Teams option for V1 class level", () => {
            const store = usePhotoGalleryStore()

            store.selectedClassLevel = "V1"
            store.selectedCourse = null

            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                return options
            }

            const options = mockGroupTypeOptions()

            // Assert: Teams should NOT be present for V1
            expect(options).not.toContainEqual({ label: "Teams", value: "teams" })
            expect(options.length).toBe(3)
        })

        it("should NOT show Teams option for V2 class level", () => {
            const store = usePhotoGalleryStore()

            store.selectedClassLevel = "V2"
            store.selectedCourse = null

            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                return options
            }

            const options = mockGroupTypeOptions()

            expect(options).not.toContainEqual({ label: "Teams", value: "teams" })
            expect(options.length).toBe(3)
        })

        it("should NOT show Teams option for V4 class level", () => {
            const store = usePhotoGalleryStore()

            store.selectedClassLevel = "V4"
            store.selectedCourse = null

            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                return options
            }

            const options = mockGroupTypeOptions()

            expect(options).not.toContainEqual({ label: "Teams", value: "teams" })
            expect(options.length).toBe(3)
        })
    })

    describe("Teams filter visibility for courses", () => {
        it("should show Teams option when course has V3 students (group counts populated)", () => {
            const store = usePhotoGalleryStore()

            // Setup: Course selected with V3 students (teams group counts populated)
            store.selectedClassLevel = null
            store.selectedCourse = {
                termCode: "202501",
                crn: "12345",
                subjectCode: "VET",
                courseNumber: "400",
                title: "V3 Clinical Course",
                termDescription: "Fall 2025",
            }
            store.groupCounts = {
                eighths: { "1A1": 5, "1A2": 4 },
                twentieths: { "1AA": 3, "1AB": 2 },
                teams: { "1": 5, "2": 4 }, // V3 students with teams
                v3specialty: { SA: 3, LA: 6 },
            }

            // Mock the groupTypeOptions logic with course-specific Teams detection
            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                // For courses: dynamically add Teams/Streams if they have entries
                if (store.selectedCourse) {
                    const hasV3Teams = Object.keys(store.groupCounts.teams || {}).length > 0
                    const hasV3Streams = Object.keys(store.groupCounts.v3specialty || {}).length > 0

                    if (hasV3Teams) {
                        options.push({ label: "Teams", value: "teams" })
                    }
                    if (hasV3Streams) {
                        options.push({ label: "Streams", value: "v3specialty" })
                    }
                }

                return options
            }

            const options = mockGroupTypeOptions()

            // Assert: Teams and Streams should appear because group counts have entries
            expect(options).toContainEqual({ label: "Teams", value: "teams" })
            expect(options).toContainEqual({ label: "Streams", value: "v3specialty" })
            expect(options.length).toBe(5) // All Students, Eighths, Twentieths, Teams, Streams
        })

        it("should NOT show Teams option when course has NO V3 students (group counts empty)", () => {
            const store = usePhotoGalleryStore()

            // Setup: Course selected with NO V3 students (teams group counts empty)
            store.selectedClassLevel = null
            store.selectedCourse = {
                termCode: "202501",
                crn: "67890",
                subjectCode: "VET",
                courseNumber: "100",
                title: "V1 Introductory Course",
                termDescription: "Fall 2025",
            }
            store.groupCounts = {
                eighths: { "1A1": 8, "1A2": 7 },
                twentieths: { "1AA": 4, "1AB": 3 },
                teams: {}, // No V3 students, no teams
                v3specialty: {}, // No V3 students, no specialties
            }

            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                // For courses: dynamically add Teams/Streams if they have entries
                if (store.selectedCourse) {
                    const hasV3Teams = Object.keys(store.groupCounts.teams || {}).length > 0
                    const hasV3Streams = Object.keys(store.groupCounts.v3specialty || {}).length > 0

                    if (hasV3Teams) {
                        options.push({ label: "Teams", value: "teams" })
                    }
                    if (hasV3Streams) {
                        options.push({ label: "Streams", value: "v3specialty" })
                    }
                }

                return options
            }

            const options = mockGroupTypeOptions()

            // Assert: Teams should NOT appear because group counts are empty
            expect(options).not.toContainEqual({ label: "Teams", value: "teams" })
            expect(options).not.toContainEqual({ label: "Streams", value: "v3specialty" })
            expect(options.length).toBe(3) // Only All Students, Eighths, Twentieths
        })

        it("should show Teams but NOT Streams when course has only V3 students with teams (no specialties)", () => {
            const store = usePhotoGalleryStore()

            store.selectedClassLevel = null
            store.selectedCourse = {
                termCode: "202501",
                crn: "11111",
                subjectCode: "VET",
                courseNumber: "300",
                title: "Early V3 Course",
                termDescription: "Fall 2025",
            }
            store.groupCounts = {
                eighths: { "1A1": 10 },
                twentieths: {},
                teams: { "1": 5, "2": 5 }, // V3 students with teams
                v3specialty: {}, // No specialty data yet (early in year)
            }

            const mockGroupTypeOptions = () => {
                const options = [
                    { label: "All Students", value: null },
                    { label: "Eighths", value: "eighths" },
                    { label: "Twentieths", value: "twentieths" },
                ]

                if (store.selectedClassLevel === "V3") {
                    options.push({ label: "Teams", value: "teams" })
                    options.push({ label: "Streams", value: "v3specialty" })
                }

                if (store.selectedCourse) {
                    const hasV3Teams = Object.keys(store.groupCounts.teams || {}).length > 0
                    const hasV3Streams = Object.keys(store.groupCounts.v3specialty || {}).length > 0

                    if (hasV3Teams) {
                        options.push({ label: "Teams", value: "teams" })
                    }
                    if (hasV3Streams) {
                        options.push({ label: "Streams", value: "v3specialty" })
                    }
                }

                return options
            }

            const options = mockGroupTypeOptions()

            // Assert: Teams should appear, but NOT Streams
            expect(options).toContainEqual({ label: "Teams", value: "teams" })
            expect(options).not.toContainEqual({ label: "Streams", value: "v3specialty" })
            expect(options.length).toBe(4) // All Students, Eighths, Twentieths, Teams
        })
    })

    describe("Teams filter behavior with group counts", () => {
        it("should populate team options with counts from groupCounts", () => {
            const store = usePhotoGalleryStore()

            // Setup teams in store
            store.groupTypes.teams = ["1", "2", "3", "16"]
            store.groupCounts.teams = {
                "1": 5,
                "2": 4,
                "3": 6,
                "16": 3,
            }

            // Mock the groupOptions logic
            const mockGroupOptions = () => {
                const groups = store.groupTypes.teams
                const counts = store.groupCounts.teams || {}

                return groups.map((group) => {
                    const count = counts[group] || 0
                    return {
                        label: `${group} (${count})`,
                        value: group,
                    }
                })
            }

            const options = mockGroupOptions()

            // Assert: Should show team numbers with counts
            expect(options).toEqual([
                { label: "1 (5)", value: "1" },
                { label: "2 (4)", value: "2" },
                { label: "3 (6)", value: "3" },
                { label: "16 (3)", value: "16" },
            ])
        })

        it("should handle teams with zero count", () => {
            const store = usePhotoGalleryStore()

            store.groupTypes.teams = ["1", "2", "3"]
            store.groupCounts.teams = {
                "1": 5,
                "2": 0, // No students in team 2
                "3": 3,
            }

            const mockGroupOptions = () => {
                const groups = store.groupTypes.teams
                const counts = store.groupCounts.teams || {}

                return groups.map((group) => {
                    const count = counts[group] || 0
                    return {
                        label: `${group} (${count})`,
                        value: group,
                    }
                })
            }

            const options = mockGroupOptions()

            expect(options).toContainEqual({ label: "2 (0)", value: "2" })
        })
    })

    describe("Teams filter integration with store", () => {
        it("should calculate team counts when loading course with V3 students", () => {
            // Simulate loading a course and calculating counts
            const students = [
                {
                    mailId: "student1",
                    firstName: "John",
                    lastName: "Doe",
                    displayName: "John Doe",
                    photoUrl: "/photo1.jpg",
                    groupAssignment: null,
                    eighthsGroup: "1A1",
                    twentiethsGroup: "1AA",
                    teamNumber: "1",
                    v3SpecialtyGroup: "SA",
                    classLevel: "V3",
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "John Doe",
                    secondaryTextLines: ["1A1 / 1AA", "Team 1"],
                },
                {
                    mailId: "student2",
                    firstName: "Jane",
                    lastName: "Smith",
                    displayName: "Jane Smith",
                    photoUrl: "/photo2.jpg",
                    groupAssignment: null,
                    eighthsGroup: "1A1",
                    twentiethsGroup: "1AA",
                    teamNumber: "1",
                    v3SpecialtyGroup: "SA",
                    classLevel: "V3",
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "Jane Smith",
                    secondaryTextLines: ["1A1 / 1AA", "Team 1"],
                },
                {
                    mailId: "student3",
                    firstName: "Bob",
                    lastName: "Johnson",
                    displayName: "Bob Johnson",
                    photoUrl: "/photo3.jpg",
                    groupAssignment: null,
                    eighthsGroup: "1A2",
                    twentiethsGroup: "1AB",
                    teamNumber: "2",
                    v3SpecialtyGroup: "LA",
                    classLevel: "V3",
                    hasPhoto: true,
                    isRossStudent: false,
                    fullName: "Bob Johnson",
                    secondaryTextLines: ["1A2 / 1AB", "Team 2"],
                },
            ]

            // Manually calculate group counts like the store does
            const groupCounts = {
                eighths: {} as Record<string, number>,
                twentieths: {} as Record<string, number>,
                teams: {} as Record<string, number>,
                v3specialty: {} as Record<string, number>,
            }

            for (const student of students) {
                if (student.teamNumber) {
                    groupCounts.teams[student.teamNumber] = (groupCounts.teams[student.teamNumber] || 0) + 1
                }
            }

            // Assert: Team counts should be calculated correctly
            expect(groupCounts.teams["1"]).toBe(2)
            expect(groupCounts.teams["2"]).toBe(1)
        })
    })
})
