import { describe, it, expect, vi, beforeEach } from "vitest"

/**
 * Tests for InstructorService — instructor management methods.
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

import { instructorService } from "../services/instructor-service"

// oxlint-disable-next-line eslint/no-underscore-dangle
const TEST_TERM_CODE = 202_410

describe("InstructorService — instructor list and detail", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getInstructors", () => {
        it("should return instructors on successful response", async () => {
            const mockData = [
                { personId: 1, fullName: "Alice Smith", department: "VMB" },
                { personId: 2, fullName: "Bob Jones", department: "PMI" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.getInstructors(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors?termCode=202410"))
            expect(result).toEqual(mockData)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.getInstructors(TEST_TERM_CODE)

            expect(result).toEqual([])
        })

        it("should include dept filter in query string", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await instructorService.getInstructors(TEST_TERM_CODE, "VMB")

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("dept=VMB"))
        })

        it("should include meritOnly filter in query string", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await instructorService.getInstructors(TEST_TERM_CODE, undefined, true)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("meritOnly=true"))
        })
    })

    describe("getInstructor", () => {
        it("should return instructor on successful response", async () => {
            const mockData = { personId: 1, fullName: "Alice Smith", department: "VMB" }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.getInstructor(1, TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors/1?termCode=202410"))
            expect(result).toEqual(mockData)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.getInstructor(1, TEST_TERM_CODE)

            expect(result).toBeNull()
        })
    })

    describe("searchPossibleInstructors", () => {
        it("should return AAUD persons on successful response", async () => {
            const mockData = [
                { mothraId: "abc123", fullName: "Carol Davis" },
                { mothraId: "def456", fullName: "Dan White" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.searchPossibleInstructors(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors/search?termCode=202410"))
            expect(result).toEqual(mockData)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.searchPossibleInstructors(TEST_TERM_CODE)

            expect(result).toEqual([])
        })

        it("should include search term in query string", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await instructorService.searchPossibleInstructors(TEST_TERM_CODE, "smith")

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("q=smith"))
        })
    })
})

describe("InstructorService — create, update, delete", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("createInstructor", () => {
        it("should return success with instructor on successful creation", async () => {
            const mockInstructor = { personId: 1, fullName: "Alice Smith" }
            mockPost.mockResolvedValue({ success: true, result: mockInstructor })

            const result = await instructorService.createInstructor({
                mothraId: "abc123",
                termCode: TEST_TERM_CODE,
            } as never)

            expect(mockPost).toHaveBeenCalledWith(
                expect.stringContaining("/instructors"),
                expect.objectContaining({ mothraId: "abc123" }),
            )
            expect(result).toEqual({ success: true, instructor: mockInstructor })
        })

        it("should return error on failed creation", async () => {
            mockPost.mockResolvedValue({ success: false, errors: ["Instructor already exists for this term"] })

            const result = await instructorService.createInstructor({
                mothraId: "abc123",
                termCode: TEST_TERM_CODE,
            } as never)

            expect(result).toEqual({ success: false, error: "Instructor already exists for this term" })
        })

        it("should return generic error when errors array is empty", async () => {
            mockPost.mockResolvedValue({ success: false, errors: [] })

            const result = await instructorService.createInstructor({
                mothraId: "abc123",
                termCode: TEST_TERM_CODE,
            } as never)

            expect(result).toEqual({ success: false, error: "Failed to add instructor" })
        })
    })

    describe("updateInstructor", () => {
        it("should return success with instructor on successful update", async () => {
            const mockInstructor = { personId: 1, fullName: "Alice Smith", department: "PMI" }
            mockPut.mockResolvedValue({ success: true, result: mockInstructor })

            const result = await instructorService.updateInstructor(1, TEST_TERM_CODE, {
                department: "PMI",
            } as never)

            expect(mockPut).toHaveBeenCalledWith(
                expect.stringContaining("/instructors/1?termCode=202410"),
                expect.objectContaining({ department: "PMI" }),
            )
            expect(result).toEqual({ success: true, instructor: mockInstructor })
        })

        it("should return error on failed update", async () => {
            mockPut.mockResolvedValue({ success: false, errors: ["Validation failed"] })

            const result = await instructorService.updateInstructor(1, TEST_TERM_CODE, {
                department: "PMI",
            } as never)

            expect(result).toEqual({ success: false, error: "Validation failed" })
        })

        it("should return generic error when errors array is missing", async () => {
            mockPut.mockResolvedValue({ success: false })

            const result = await instructorService.updateInstructor(1, TEST_TERM_CODE, {
                department: "PMI",
            } as never)

            expect(result).toEqual({ success: false, error: "Failed to update instructor" })
        })
    })

    describe("deleteInstructor", () => {
        it("should return true on successful delete", async () => {
            mockDel.mockResolvedValue({ success: true })

            const result = await instructorService.deleteInstructor(1, TEST_TERM_CODE)

            expect(mockDel).toHaveBeenCalledWith(expect.stringContaining("/instructors/1?termCode=202410"))
            expect(result).toBe(true)
        })

        it("should return false on failed delete", async () => {
            mockDel.mockResolvedValue({ success: false })

            const result = await instructorService.deleteInstructor(1, TEST_TERM_CODE)

            expect(result).toBe(false)
        })
    })

    describe("canDeleteInstructor", () => {
        it("should return can-delete result on successful response", async () => {
            const mockData = { canDelete: true, recordCount: 3 }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.canDeleteInstructor(1, TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors/1/can-delete?termCode=202410"))
            expect(result).toEqual({ canDelete: true, recordCount: 3 })
        })

        it("should return safe default on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.canDeleteInstructor(1, TEST_TERM_CODE)

            expect(result).toEqual({ canDelete: false, recordCount: 0 })
        })
    })
})

describe("InstructorService — departments and job groups", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getInstructorDepartments", () => {
        it("should return departments on successful response", async () => {
            const mockData = [
                { code: "VMB", name: "Molecular Biosciences" },
                { code: "PMI", name: "Pathology, Microbiology & Immunology" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.getInstructorDepartments()

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors/departments"))
            expect(result).toEqual(mockData)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.getInstructorDepartments()

            expect(result).toEqual([])
        })
    })

    describe("getJobGroups", () => {
        it("should fetch job groups without params", async () => {
            const mockData = [{ groupName: "Ladder Rank", count: 12 }]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await instructorService.getJobGroups()

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/instructors/job-groups"))
            expect(result).toEqual(mockData)
        })

        it("should include termCode and department in query string", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await instructorService.getJobGroups(TEST_TERM_CODE, "VME")

            const calledUrl = mockGet.mock.calls[0]![0] as string
            expect(calledUrl).toContain("termCode=202410")
            expect(calledUrl).toContain("department=VME")
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await instructorService.getJobGroups()

            expect(result).toEqual([])
        })
    })
})
