import { describe, it, expect, vi, beforeEach } from "vitest"
import { courseService } from "../services/course-service"

/**
 * Tests for CourseService — evaluation-related and course effort methods.
 * Validates URL construction, request payloads, and response handling.
 */

// Mock the ViperFetch composable
const mockGet = vi.fn()
const mockPost = vi.fn()
const mockPut = vi.fn()
const mockDel = vi.fn()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
    }),
}))

// Term code format is YYYYMM (2024 Fall = 202410); the constant name conveys the semantic meaning
const TEST_TERM_CODE = 202_410

describe("CourseService — evaluation methods", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getCourseEvaluations", () => {
        it("should return evaluation status on successful response", async () => {
            const mockData = {
                canEditAdHoc: true,
                maxRatingCount: 500,
                instructors: [{ personId: 1, mothraId: "test", instructorName: "Test User", evaluations: [] }],
                courses: [{ courseId: 1, courseName: "DVM 443", crn: "12345" }],
            }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await courseService.getCourseEvaluations(1)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/courses/1/evaluations"))
            expect(result).toEqual(mockData)
        })

        it("should return empty default on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await courseService.getCourseEvaluations(1)

            expect(result).toEqual({ canEditAdHoc: false, maxRatingCount: 500, instructors: [], courses: [] })
        })

        it("should return empty default when result is null", async () => {
            mockGet.mockResolvedValue({ success: true, result: null })

            const result = await courseService.getCourseEvaluations(1)

            expect(result).toEqual({ canEditAdHoc: false, maxRatingCount: 500, instructors: [], courses: [] })
        })
    })

    describe("createEvaluation", () => {
        it("should return success result on successful creation", async () => {
            const mockResult = { success: true, quantId: 42 }
            mockPost.mockResolvedValue({ success: true, result: mockResult })

            const result = await courseService.createEvaluation(1, {
                courseId: 1,
                mothraId: "testuser",
                count1: 0,
                count2: 0,
                count3: 5,
                count4: 3,
                count5: 2,
            })

            expect(mockPost).toHaveBeenCalledWith(
                expect.stringContaining("/courses/1/evaluations"),
                expect.objectContaining({ mothraId: "testuser", count3: 5 }),
            )
            expect(result).toEqual(mockResult)
        })

        it("should return error on failed creation", async () => {
            mockPost.mockResolvedValue({ success: false, errors: ["harvested eval data exists for this course"] })

            const result = await courseService.createEvaluation(1, {
                courseId: 1,
                mothraId: "testuser",
                count1: 0,
                count2: 0,
                count3: 1,
                count4: 0,
                count5: 0,
            })

            expect(result).toEqual({ success: false, error: "harvested eval data exists for this course" })
        })

        it("should return generic error when errors array is empty", async () => {
            mockPost.mockResolvedValue({ success: false, errors: [] })

            const result = await courseService.createEvaluation(1, {
                courseId: 1,
                mothraId: "testuser",
                count1: 0,
                count2: 0,
                count3: 1,
                count4: 0,
                count5: 0,
            })

            expect(result).toEqual({ success: false, error: "Failed to create evaluation" })
        })
    })

    describe("updateEvaluation", () => {
        it("should return success result on successful update", async () => {
            const mockResult = { success: true, quantId: 42 }
            mockPut.mockResolvedValue({ success: true, result: mockResult })

            const result = await courseService.updateEvaluation(1, 42, {
                count1: 1,
                count2: 2,
                count3: 3,
                count4: 4,
                count5: 5,
            })

            expect(mockPut).toHaveBeenCalledWith(
                expect.stringContaining("/courses/1/evaluations/42"),
                expect.objectContaining({ count1: 1, count5: 5 }),
            )
            expect(result).toEqual(mockResult)
        })

        it("should return error on failed update", async () => {
            mockPut.mockResolvedValue({ success: false, errors: ["Not an ad-hoc evaluation"] })

            const result = await courseService.updateEvaluation(1, 42, {
                count1: 0,
                count2: 0,
                count3: 1,
                count4: 0,
                count5: 0,
            })

            expect(result).toEqual({ success: false, error: "Not an ad-hoc evaluation" })
        })
    })

    describe("deleteEvaluation", () => {
        it("should return true on successful delete", async () => {
            mockDel.mockResolvedValue({ success: true })

            const result = await courseService.deleteEvaluation(1, 42)

            expect(mockDel).toHaveBeenCalledWith(expect.stringContaining("/courses/1/evaluations/42"))
            expect(result).toBeTruthy()
        })

        it("should return false on failed delete", async () => {
            mockDel.mockResolvedValue({ success: false })

            const result = await courseService.deleteEvaluation(1, 42)

            expect(result).toBeFalsy()
        })
    })
})

describe("CourseService — course effort methods", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getCourseEffort", () => {
        it("should return effort data on successful response", async () => {
            const mockData = {
                courseId: 1,
                termCode: TEST_TERM_CODE,
                canAddEffort: true,
                isChildCourse: false,
                records: [{ id: 1, instructorName: "Test" }],
            }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await courseService.getCourseEffort(1)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/courses/1/effort"))
            expect(result).toEqual(mockData)
        })

        it("should return empty default on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await courseService.getCourseEffort(1)

            expect(result).toEqual({ courseId: 1, termCode: 0, canAddEffort: false, isChildCourse: false, records: [] })
        })

        it("should return empty default when result is null", async () => {
            mockGet.mockResolvedValue({ success: true, result: null })

            const result = await courseService.getCourseEffort(1)

            expect(result).toEqual({ courseId: 1, termCode: 0, canAddEffort: false, isChildCourse: false, records: [] })
        })
    })

    describe("getPossibleInstructors", () => {
        it("should return instructors on successful response", async () => {
            const mockData = {
                existingInstructors: [{ personId: 1, name: "Existing" }],
                otherInstructors: [{ personId: 2, name: "Other" }],
            }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await courseService.getPossibleInstructors(1)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/courses/1/possible-instructors"))
            expect(result).toEqual(mockData)
        })

        it("should return empty default on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await courseService.getPossibleInstructors(1)

            expect(result).toEqual({ existingInstructors: [], otherInstructors: [] })
        })

        it("should return empty default when result is null", async () => {
            mockGet.mockResolvedValue({ success: true, result: null })

            const result = await courseService.getPossibleInstructors(1)

            expect(result).toEqual({ existingInstructors: [], otherInstructors: [] })
        })
    })
})
