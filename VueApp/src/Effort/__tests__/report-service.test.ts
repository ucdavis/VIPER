import { describe, it, expect, vi, beforeEach } from "vitest"

/**
 * Tests for Report service.
 *
 * These tests validate the report service methods and error handling
 * for the Effort teaching activity reports feature.
 */

// Mock the ViperFetch composable
const mockGet = vi.fn()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
    }),
}))

// Import service after mocking
import { reportService } from "../services/report-service"

// Test data - term codes use YYYYXX format (no numeric separators)
// oxlint-disable-next-line unicorn/numeric-separators-style
const TEST_TERM_CODE = 202410

const mockReport = {
    termCode: TEST_TERM_CODE,
    termName: "Fall Quarter 2024",
    filterDepartment: null,
    filterPerson: null,
    filterRole: null,
    filterTitle: null,
    effortTypes: ["CLI", "LEC"],
    departments: [
        {
            department: "VME",
            instructors: [
                {
                    mothraId: "A12345678",
                    instructor: "Smith, John",
                    jobGroupId: "REG",
                    courses: [
                        {
                            courseId: 1,
                            course: "VME 400-001",
                            crn: "40076",
                            units: 4.0,
                            enrollment: 25,
                            roleId: "Instructor",
                            effortByType: { LEC: 30.0, CLI: 10.0 },
                        },
                    ],
                    instructorTotals: { LEC: 30.0, CLI: 10.0 },
                },
            ],
            departmentTotals: { LEC: 30.0, CLI: 10.0 },
        },
    ],
}

describe("ReportService", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getTeachingActivityGrouped", () => {
        it("should return report on successful response", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("teaching/grouped"))
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining(`termCode=${TEST_TERM_CODE}`))
            expect(result).toEqual(mockReport)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should return null when result is undefined", async () => {
            mockGet.mockResolvedValue({ success: true, result: undefined })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should include department filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, department: "VME" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("department=VME"))
        })

        it("should include personId filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, personId: 123 })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("personId=123"))
        })

        it("should include role filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, role: "I" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("role=I"))
        })

        it("should include title filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, title: "REG" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("title=REG"))
        })

        it("should include all filters when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({
                termCode: TEST_TERM_CODE,
                department: "VME",
                personId: 123,
                role: "I",
                title: "REG",
            })

            const calledUrl = mockGet.mock.calls[0][0] as string
            expect(calledUrl).toContain(`termCode=${TEST_TERM_CODE}`)
            expect(calledUrl).toContain("department=VME")
            expect(calledUrl).toContain("personId=123")
            expect(calledUrl).toContain("role=I")
            expect(calledUrl).toContain("title=REG")
        })

        it("should not include optional filters when not provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            const calledUrl = mockGet.mock.calls[0][0] as string
            expect(calledUrl).toContain(`termCode=${TEST_TERM_CODE}`)
            expect(calledUrl).not.toContain("department=")
            expect(calledUrl).not.toContain("personId=")
            expect(calledUrl).not.toContain("role=")
            expect(calledUrl).not.toContain("title=")
        })
    })

    describe("getTeachingActivityIndividual", () => {
        it("should return report on successful response", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            const result = await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("teaching/individual"))
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining(`termCode=${TEST_TERM_CODE}`))
            expect(result).toEqual(mockReport)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should include department filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE, department: "APC" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("department=APC"))
        })
    })
})
