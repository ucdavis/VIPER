import { svmFrequentNumberService } from "../services/svm-frequent-number-service"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: vi.fn<(...args: unknown[]) => unknown>(),
        del: vi.fn<(...args: unknown[]) => unknown>(),
    }),
}))

describe("svmFrequentNumberService()", () => {
    it("returns the frequent numbers on success", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const numbers = [{ numberId: 1, label: "Front Desk", phone: "530-555-1000", sortOrder: null }]
        mockGet.mockResolvedValue({ success: true, result: numbers })

        const result = await svmFrequentNumberService.getFrequentNumbers()

        expect(result).toStrictEqual(numbers)
    })

    it("normalizes a failed request to an empty array", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await svmFrequentNumberService.getFrequentNumbers()

        expect(result).toStrictEqual([])
    })

    it("posts new frequent number data to the base endpoint", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPost.mockResolvedValue({ success: true, result: true })
        const formData = { label: "Front Desk", phone: "530-555-1000", entryId: -1 }

        await svmFrequentNumberService.addFrequentNumber(formData)

        expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("frequentnumbers"), formData)
    })
})
