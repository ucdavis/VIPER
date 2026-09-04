import { svmModifiedDateService } from "../services/svm-modified-date-service"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

describe("svmModifiedDateService()", () => {
    it("returns the modified date", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: "2026-01-01T00:00:00" })

        const result = await svmModifiedDateService.getModifiedDate()

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("svm/modifiedDate"))
        expect(result).toBe("2026-01-01T00:00:00")
    })

    it("returns null when there is no modified date on record", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: null })

        const result = await svmModifiedDateService.getModifiedDate()

        expect(result).toBeNull()
    })
})
