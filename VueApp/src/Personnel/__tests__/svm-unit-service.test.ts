import { svmUnitService } from "../services/svm-unit-service"

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
    }),
}))

describe("svmUnitService()", () => {
    it("returns the units for a section on success", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const units = [
            {
                unitId: 10,
                sectionId: 1,
                name: "Dean's Office",
                abbrv: "DO",
                sortOrder: null,
                fax: null,
                section: null,
                unitPersons: null,
            },
        ]
        mockGet.mockResolvedValue({ success: true, result: units })

        const result = await svmUnitService.getAllUnits()

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("svm/units"))
        expect(result).toStrictEqual(units)
    })

    it("normalizes a failed request to an empty array", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await svmUnitService.getAllUnits()

        expect(result).toStrictEqual([])
    })

    it("posts new unit data to the units endpoint", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPost.mockResolvedValue({ success: true, result: true })
        const dto = {
            fax: "",
            location: "",
            deanIam: "dean01",
            deanPhone: "",
            deanInterim: "",
            deanUnitPerson: -1,
            staffIam: "",
            staffPhone: "",
            staffInterim: "",
            staffUnitPerson: -1,
        }

        await svmUnitService.addUnitData(10, dto)

        expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("units/10"), dto)
    })

    it("deletes a list row by its row key", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockDel.mockResolvedValue({ success: true, result: true })

        await svmUnitService.deleteRow(7)

        expect(mockDel).toHaveBeenCalledWith(expect.stringContaining("rows/7"))
    })
})
