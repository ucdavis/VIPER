import { emergencyContactService } from "../services/emergency-contact-service"
import type { UpdateStudentContactRequest } from "../types"

/**
 * Tests for EmergencyContactService.
 * Validates URL construction, request methods, and response handling.
 */

// Mock the ViperFetch composable
const mockGet = vi.fn()
const mockPut = vi.fn()
const mockPost = vi.fn()
const mockPostForBlob = vi.fn()
const mockDownloadBlob = vi.fn()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        put: (...args: unknown[]) => mockPut(...args),
        post: (...args: unknown[]) => mockPost(...args),
    }),
    postForBlob: (...args: unknown[]) => mockPostForBlob(...args),
    downloadBlob: (...args: unknown[]) => mockDownloadBlob(...args),
}))

const EMPTY_CONTACT = {
    name: null,
    relationship: null,
    workPhone: null,
    homePhone: null,
    cellPhone: null,
    email: null,
}

function makeEmptyRequest(overrides: Partial<UpdateStudentContactRequest> = {}): UpdateStudentContactRequest {
    return {
        studentInfo: { address: null, city: null, zip: null, homePhone: null, cellPhone: null },
        contactPermanent: false,
        localContact: { ...EMPTY_CONTACT },
        emergencyContact: { ...EMPTY_CONTACT },
        permanentContact: { ...EMPTY_CONTACT },
        ...overrides,
    }
}

describe("EmergencyContactService — read operations", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getList", () => {
        it("should return list on successful response", async () => {
            const mockData = [
                { personId: 1, fullName: "Smith, John", classLevel: "V1" },
                { personId: 2, fullName: "Doe, Jane", classLevel: "V2" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await emergencyContactService.getList()

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/students/emergency-contacts"))
            expect(result).toEqual(mockData)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await emergencyContactService.getList()

            expect(result).toEqual([])
        })
    })

    describe("getDetail", () => {
        it("should return detail on successful response", async () => {
            const mockData = {
                personId: 1,
                fullName: "Smith, John",
                canEdit: true,
                studentInfo: { address: "123 Main St" },
            }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await emergencyContactService.getDetail(1)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/students/emergency-contacts/1"))
            expect(result).toEqual(mockData)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await emergencyContactService.getDetail(1)

            expect(result).toBeNull()
        })
    })

    describe("getReport", () => {
        it("should return report data on successful response", async () => {
            const mockData = [{ personId: 1, fullName: "Smith, John", classLevel: "V1" }]
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await emergencyContactService.getReport()

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/students/emergency-contacts/report"))
            expect(result).toEqual(mockData)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await emergencyContactService.getReport()

            expect(result).toEqual([])
        })
    })

    describe("canEdit", () => {
        it("should return true when user can edit", async () => {
            mockGet.mockResolvedValue({ success: true, result: true })

            const result = await emergencyContactService.canEdit(1)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/can-edit/1"))
            expect(result).toBeTruthy()
        })

        it("should return false on failure", async () => {
            mockGet.mockResolvedValue({ success: false })

            const result = await emergencyContactService.canEdit(1)

            expect(result).toBeFalsy()
        })
    })
})

describe("EmergencyContactService — write operations", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("updateContact", () => {
        it("should return success result on successful update", async () => {
            const mockResult = { personId: 1, fullName: "Smith, John", canEdit: true }
            mockPut.mockResolvedValue({ success: true, result: mockResult })

            const request = makeEmptyRequest({
                studentInfo: { address: "123 Main", city: "Davis", zip: "95616", homePhone: null, cellPhone: null },
            })
            const result = await emergencyContactService.updateContact(1, request)

            expect(mockPut).toHaveBeenCalledWith(
                expect.stringContaining("/students/emergency-contacts/1"),
                expect.objectContaining({ contactPermanent: false }),
            )
            expect(result.success).toBeTruthy()
            expect(result.result).toEqual(mockResult)
        })

        it("should return errors on failed update", async () => {
            mockPut.mockResolvedValue({ success: false, errors: ["Validation failed"] })

            const request = makeEmptyRequest()
            const result = await emergencyContactService.updateContact(1, request)

            expect(result.success).toBeFalsy()
            expect(result.errors).toEqual(["Validation failed"])
        })
    })

    describe("toggleAppAccess", () => {
        it("should return new state on success", async () => {
            mockPost.mockResolvedValue({ success: true, result: true })

            const result = await emergencyContactService.toggleAppAccess()

            expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("/access/toggle-app"))
            expect(result).toBeTruthy()
        })

        it("should return null on failure", async () => {
            mockPost.mockResolvedValue({ success: false })

            const result = await emergencyContactService.toggleAppAccess()

            expect(result).toBeNull()
        })
    })

    describe("toggleIndividualAccess", () => {
        it("should call correct endpoint with personId", async () => {
            mockPost.mockResolvedValue({ success: true, result: true })

            const result = await emergencyContactService.toggleIndividualAccess(42)

            expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("/access/42/toggle"))
            expect(result).toBeTruthy()
        })
    })
})

describe("EmergencyContactService — access status and exports", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getAccessStatus", () => {
        it("should return access status on successful response", async () => {
            const mockData = { appOpen: true, individualGrants: [] }
            mockGet.mockResolvedValue({ success: true, result: mockData })

            const result = await emergencyContactService.getAccessStatus()

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/access/status"))
            expect(result).toEqual(mockData)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await emergencyContactService.getAccessStatus()

            expect(result).toBeNull()
        })
    })

    describe("downloadExcel", () => {
        it("should download blob with correct filename", async () => {
            const mockBlob = new Blob(["test"], { type: "application/octet-stream" })
            mockPostForBlob.mockResolvedValue({ blob: mockBlob, filename: "report.xlsx" })

            const result = await emergencyContactService.downloadExcel()

            expect(mockPostForBlob).toHaveBeenCalledWith(expect.stringContaining("/export/excel"), {})
            expect(mockDownloadBlob).toHaveBeenCalledWith(mockBlob, "report.xlsx")
            expect(result).toBeTruthy()
        })

        it("should return false when blob is empty", async () => {
            const emptyBlob = new Blob([], { type: "application/octet-stream" })
            mockPostForBlob.mockResolvedValue({ blob: emptyBlob, filename: null })

            const result = await emergencyContactService.downloadExcel()

            expect(result).toBeFalsy()
        })
    })
})
