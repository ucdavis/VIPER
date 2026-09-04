import { phoneListModifiedDateService } from "../services/phone-list-modified-date-service"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

describe("phoneListModifiedDateService()", () => {
    it("returns the modified date scoped to the given list code", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: "2026-01-01T00:00:00" })

        const result = await phoneListModifiedDateService.getModifiedDate("VMDO")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO/modifiedDate"))
        expect(result).toBe("2026-01-01T00:00:00")
    })

    it("returns null when there is no modified date on record", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: null })

        const result = await phoneListModifiedDateService.getModifiedDate("VMDO")

        expect(result).toBeNull()
    })
})
