import { svmSectionService } from "../services/svm-section-service"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

describe("svmSectionService()", () => {
    it("returns the sections on success", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const sections = [
            { sectionId: 1, name: "VMDO", includeAbbrv: false, unitName: null, directorTitle: "Dean", sortOrder: 1 },
        ]
        mockGet.mockResolvedValue({ success: true, result: sections })

        const result = await svmSectionService.getSections()

        expect(result).toStrictEqual(sections)
    })

    it("returns null for a failed request, which an empty array could not distinguish", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await svmSectionService.getSections()

        expect(result).toBeNull()
    })

    it("returns an empty array for a list that genuinely has no sections", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [] })

        const result = await svmSectionService.getSections()

        // Not null: the page renders no sections rather than raising an error banner.
        expect(result).toStrictEqual([])
    })
})
