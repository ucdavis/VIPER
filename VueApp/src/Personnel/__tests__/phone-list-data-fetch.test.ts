import { getPhoneListData } from "../composables/phone-list-data-fetch"
import { phoneListUnitService } from "../services/phone-list-unit-service"
import type { PhoneListUnitAPIResponse, PhoneListUnitPerson } from "../types/phone-list-phone-types"

vi.mock("../services/phone-list-unit-service", () => ({
    phoneListUnitService: {
        getUnitsByList: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

function makeUnitPerson(overrides: Partial<PhoneListUnitPerson> = {}): PhoneListUnitPerson {
    return {
        phoneListUnitPersonId: 1,
        phoneListUnitId: 10,
        personIam: "person01",
        listFirst: false,
        person: {
            personIam: "person01",
            phone: "530-555-1000",
            directPhone: "530-555-2000",
            office: "Room 100",
            modifiedDate: null,
            modifiedBy: null,
            viperPerson: {
                personId: 1,
                firstName: "Ada",
                lastName: "Lovelace",
                fullName: "Ada Lovelace",
                iamId: "person01",
                currentEmployee: true,
                mailId: "alovelace",
            },
            viperModPerson: null,
        },
        modifiedBy: null,
        modifiedDate: null,
        viperModPerson: null,
        ...overrides,
    }
}

function makeUnit(persons: PhoneListUnitPerson[]): PhoneListUnitAPIResponse {
    return {
        phoneListUnitId: 10,
        phoneListId: 1,
        name: "Dean's Office",
        sortOrder: null,
        phoneListUnitPersons: persons,
    }
}

describe("getPhoneListData()", () => {
    it("omits the direct phone column for non-internal, view-only callers", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListUnitService.getUnitsByList).mockResolvedValue([makeUnit([makeUnitPerson()])])

        const units = await getPhoneListData("VMDO", false, false)

        const columnNames = units[0]!.cols!.map((c) => c.name)
        expect(columnNames).not.toContain("directPhone")
        expect(columnNames).not.toContain("edit")
        expect(columnNames).not.toContain("delete")
        expect(columnNames).not.toContain("listFirst")
    })

    it("shows the direct phone column for internal viewers even outside edit mode", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListUnitService.getUnitsByList).mockResolvedValue([makeUnit([makeUnitPerson()])])

        const units = await getPhoneListData("VMDO", false, true)

        const columnNames = units[0]!.cols!.map((c) => c.name)
        expect(columnNames).toContain("directPhone")
        expect(columnNames).not.toContain("edit")
    })

    it("shows maintain-only columns in edit mode regardless of internal status", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListUnitService.getUnitsByList).mockResolvedValue([makeUnit([makeUnitPerson()])])

        const units = await getPhoneListData("VMDO", true, false)

        const columnNames = units[0]!.cols!.map((c) => c.name)
        expect(columnNames).toContain("directPhone")
        expect(columnNames).toContain("listFirst")
        expect(columnNames).toContain("edit")
        expect(columnNames).toContain("delete")
    })

    it("drops rows for former employees whose person record is gone, and falls back to a sparse name otherwise", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListUnitService.getUnitsByList).mockResolvedValue([
            makeUnit([
                makeUnitPerson({ person: null }),
                makeUnitPerson({
                    phoneListUnitPersonId: 2,
                    personIam: "person02",
                    person: {
                        personIam: "person02",
                        phone: "530-555-3000",
                        directPhone: "530-555-4000",
                        office: "Room 200",
                        modifiedDate: null,
                        modifiedBy: null,
                        viperPerson: null,
                        viperModPerson: null,
                    },
                }),
            ]),
        ])

        const units = await getPhoneListData("VMDO", false, false)

        expect(units[0]!.rows).toHaveLength(1)
        expect(units[0]!.rows[0]!.name).toBe(", ")
        expect(units[0]!.rows[0]!.fullName).toBe("")
    })

    /**
     * The desktop table draws this column as a tick through its own cell slot. The card list has
     * no slot to draw into and would print the raw boolean, so the column formats itself and both
     * views read the same answer from one place.
     */
    it("formats listFirst as text, so a view without cell slots does not print a raw boolean", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListUnitService.getUnitsByList).mockResolvedValue([makeUnit([makeUnitPerson()])])

        const units = await getPhoneListData("VMDO", true, false)

        const listFirst = units[0]!.cols!.find((col) => col.name === "listFirst")!

        expect(listFirst.format!(true, {})).toBe("Yes")
        // Blank rather than "No", so an unset flag drops out of the card entirely.
        expect(listFirst.format!(false, {})).toBe("")
    })
})
