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

    it("normalizes a failed request to an empty array", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await svmSectionService.getSections()

        expect(result).toStrictEqual([])
    })
})
