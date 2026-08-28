import { phoneListUnitService } from "../services/phone-list-unit-service"

/**
 * Every call is addressed by the list's code. The API resolves that code to a list and runs its
 * own permission check against that list, so the code in the path is what scopes the request -
 * these tests pin that the code actually reaches the URL.
 */

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

const formData = {
    unitId: 1,
    office: "",
    employeeIam: "person01",
    phone: "",
    directPhone: "",
    listFirst: false,
}

describe("phoneListUnitService()", () => {
    it("returns the API results for getUnitsByList", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const units = [
            {
                phoneListUnitId: 1,
                phoneListId: 1,
                name: "Dean's Office",
                sortOrder: null,
                phoneListUnitPersons: [],
            },
        ]
        mockGet.mockResolvedValue({ success: true, result: units })

        const result = await phoneListUnitService.getUnitsByList("VMDO")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO/units"))
        expect(result).toStrictEqual(units)
    })

    it("normalizes a null or empty getUnitsByList result to an empty array", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await phoneListUnitService.getUnitsByList("VMDO")

        expect(result).toStrictEqual([])
    })

    it("posts new unit-person data to the unitPerson endpoint for the given list", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPost.mockResolvedValue({ success: true, result: true })

        await phoneListUnitService.addUnitPersonData("VMDO", formData)

        expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO/unitPerson"), formData)
    })

    it("puts updated unit-person data under the list code and record id", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPut.mockResolvedValue({ success: true, result: true })

        await phoneListUnitService.updateUnitPersonData("VMDO", 7, formData)

        expect(mockPut).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO/unitPerson/7"), formData)
    })

    it("deletes a unit-person record under the list code", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockDel.mockResolvedValue({ success: true, result: true })

        await phoneListUnitService.deleteUnitPersonData("VMDO", 7)

        expect(mockDel).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO/unitPerson/7"))
    })
})
