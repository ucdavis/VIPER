import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import { svmFrequentNumberService } from "../services/svm-frequent-number-service"
import { svmSectionService } from "../services/svm-section-service"
import { svmUnitService } from "../services/svm-unit-service"
import type {
    SVMFrequentNumberAPIResponse,
    SVMSectionAPIResponse,
    SVMUnitAPIResponse,
    SVMUnitPerson,
} from "../types/svm-phone-types"

vi.mock("../services/svm-section-service", () => ({
    svmSectionService: { getSections: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("../services/svm-unit-service", () => ({
    svmUnitService: { getAllUnits: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("../services/svm-frequent-number-service", () => ({
    svmFrequentNumberService: { getFrequentNumbers: vi.fn<(...args: unknown[]) => unknown>() },
}))

function makeSection(overrides: Partial<SVMSectionAPIResponse> = {}): SVMSectionAPIResponse {
    return {
        sectionId: 1,
        name: "VMDO",
        includeAbbrv: false,
        unitName: "Unit",
        directorTitle: "Director",
        sortOrder: 1,
        ...overrides,
    }
}

function makeUnitPerson(overrides: Partial<SVMUnitPerson> = {}): SVMUnitPerson {
    return {
        unitPersonId: 1,
        unitId: 10,
        personIam: "dean01",
        office: "Room 100",
        posType: "Dean",
        interim: null,
        modifiedDate: null,
        modifiedBy: null,
        unit: null,
        person: {
            personIam: "dean01",
            phone: "530-555-1000",
            directPhone: "",
            office: "Room 100",
            modifiedDate: null,
            modifiedBy: null,
            unitPersons: null,
            phoneListUnitPersons: null,
            viperPerson: {
                personId: 1,
                firstName: "Dean",
                lastName: "Person",
                fullName: "Dean Person",
                iamId: "dean01",
                currentEmployee: true,
                mailId: "",
            },
            viperModPerson: null,
        },
        viperModPerson: null,
        ...overrides,
    }
}

function makeUnit(overrides: Partial<SVMUnitAPIResponse> = {}): SVMUnitAPIResponse {
    return {
        unitId: 10,
        sectionId: 1,
        name: "Dean's Office",
        abbrv: "DO",
        sortOrder: 1,
        fax: "530-555-9999",
        section: null,
        unitPersons: [],
        ...overrides,
    }
}

/** A unit whose only active person is admin staff - no dean/director row. */
function staffOnlyUnit(unitId: number, unitPersonId: number, iamId: string): SVMUnitAPIResponse {
    return makeUnit({
        unitId,
        name: `Unit ${unitId}`,
        unitPersons: [
            makeUnitPerson({
                unitPersonId,
                unitId,
                personIam: iamId,
                posType: "Staff",
                person: {
                    personIam: iamId,
                    phone: "530-555-2000",
                    directPhone: "",
                    office: "Room 100",
                    modifiedDate: null,
                    modifiedBy: null,
                    unitPersons: null,
                    phoneListUnitPersons: null,
                    viperPerson: {
                        personId: unitPersonId,
                        firstName: "Staff",
                        lastName: iamId,
                        fullName: `Staff ${iamId}`,
                        iamId,
                        currentEmployee: true,
                        mailId: "",
                    },
                    viperModPerson: null,
                },
            }),
        ],
    })
}

describe("getSVMData()", () => {
    it("builds view-mode columns with location/phone/fax fields, and edit-mode columns with edit/delete", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([])

        const view = await getSVMData(false)
        const edit = await getSVMData(true)

        const viewCols = view.newSections[0]!.cols!.map((c) => c.name)
        const editCols = edit.newSections[0]!.cols!.map((c) => c.name)
        expect(viewCols).toStrictEqual([
            "unitName",
            "location",
            "deanDirector",
            "dirPhone",
            "fax",
            "adminStaff",
            "adminPhone",
        ])
        expect(editCols).toStrictEqual(["unitName", "deanDirector", "adminStaff", "edit", "delete"])
    })

    it("includes the abbreviation column only in view mode when the section flags includeAbbrv", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection({ includeAbbrv: true })])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([])

        const view = await getSVMData(false)
        const edit = await getSVMData(true)

        expect(view.newSections[0]!.cols!.map((c) => c.name)).toContain("abbreviation")
        expect(edit.newSections[0]!.cols!.map((c) => c.name)).not.toContain("abbreviation")
    })

    it("splits leaders from the shared admin staff record and appends the interim suffix", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({
                unitPersons: [
                    makeUnitPerson({ interim: "Interim" }),
                    makeUnitPerson({
                        unitPersonId: 2,
                        personIam: "staff01",
                        posType: "Staff",
                        person: {
                            personIam: "staff01",
                            phone: "530-555-2000",
                            directPhone: "",
                            office: "Room 100",
                            modifiedDate: null,
                            modifiedBy: null,
                            unitPersons: null,
                            phoneListUnitPersons: null,
                            viperPerson: {
                                personId: 2,
                                firstName: "Staff",
                                lastName: "Person",
                                fullName: "Staff Person",
                                iamId: "staff01",
                                currentEmployee: true,
                                mailId: "",
                            },
                            viperModPerson: null,
                        },
                    }),
                ],
            }),
        ])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows).toHaveLength(1)
        const row = newSections[0]!.rows[0]!
        expect(row.deanDirectorDisplayName).toBe("Dean Person (Interim)")
        expect(row.adminStaffDisplayName).toBe("Staff Person")
        expect(row.adminStaffIam).toBe("staff01")
    })

    it("produces no rows, without throwing, when a unit has no unitPersons", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([makeUnit({ unitPersons: null })])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows).toStrictEqual([])
    })

    it("blanks the admin staff fields when a unit has a leader but no assigned staff", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([makeUnit({ unitPersons: [makeUnitPerson()] })])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows).toHaveLength(1)
        const row = newSections[0]!.rows[0]!
        expect(row.adminStaffDisplayName).toBe("")
        expect(row.adminStaffUnitPersonId).toBe(-1)
    })

    it("falls back to an empty person, without throwing, when a leader's person record is null", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({ unitPersons: [makeUnitPerson({ person: null })] }),
        ])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows).toHaveLength(1)
        const row = newSections[0]!.rows[0]!
        expect(row.deanDirectorFullName).toBe("")
        expect(row.deanDirectorIam).toBe("")
        expect(row.deanDirectorPhone).toBe("")
    })

    it("keeps a staff-only unit visible by standing in a blank dean/director", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([staffOnlyUnit(10, 7, "staff01")])

        const { newSections } = await getSVMData(false)

        // Rows are emitted per leader, so without the stand-in this unit would produce no row at
        // all and the staff member would silently vanish from the list.
        expect(newSections[0]!.rows).toHaveLength(1)
        const row = newSections[0]!.rows[0]!
        expect(row.adminStaffIam).toBe("staff01")
        expect(row.adminStaffUnitPersonId).toBe(7)
        expect(row.deanDirectorDisplayName).toBe("")
        // Must stay -1 so SVMPhonesMaintain's delete guard skips the nonexistent dean row.
        expect(row.deanDirectorUnitPersonId).toBe(-1)
    })

    it("gives staff-only rows distinct row keys", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            staffOnlyUnit(10, 7, "staff01"),
            staffOnlyUnit(11, 8, "staff02"),
        ])

        const { newSections } = await getSVMData(false)

        // The q-table row-key comes from entryId. The stand-in leader carries -1, so falling back
        // to the staff's id is what keeps two staff-only units in one section from colliding and
        // letting Vue reuse one row's DOM for the other.
        const keys = newSections[0]!.rows.map((r) => r.entryId)
        expect(keys).toStrictEqual([7, 8])
    })

    it("flags a row as the last for its unit only when the unit yields one row", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({
                unitPersons: [
                    makeUnitPerson({ unitPersonId: 1, personIam: "dean01" }),
                    makeUnitPerson({ unitPersonId: 2, personIam: "dean02" }),
                ],
            }),
        ])

        const { newSections: twoLeaders } = await getSVMData(false)

        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([makeUnit({ unitPersons: [makeUnitPerson()] })])

        const { newSections: oneLeader } = await getSVMData(false)

        // The delete confirmation reads this to decide whether to name the admin staff as being
        // removed, since the API keeps them while any leader row still lists them.
        expect(twoLeaders[0]!.rows.map((r) => r.isOnlyRowForUnit)).toStrictEqual([false, false])
        expect(oneLeader[0]!.rows[0]!.isOnlyRowForUnit).toBeTruthy()
    })
})

describe("getSVMData() - row shaping edge cases", () => {
    it("ignores a unit person carrying no PosType, who is neither a leader nor the staff", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({
                unitPersons: [makeUnitPerson(), makeUnitPerson({ unitPersonId: 2, posType: null })],
            }),
        ])

        const { newSections } = await getSVMData(false)

        // Only the Dean row: the PosType-less record is not a leader, so it gets no row of its
        // own, and it is not the staff either, so it does not fill the admin staff fields.
        expect(newSections[0]!.rows).toHaveLength(1)
        expect(newSections[0]!.rows[0]!.adminStaffUnitPersonId).toBe(-1)
    })

    it("hides a row whose leader and staff both lack a record to key it on", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({ unitPersons: [makeUnitPerson({ unitPersonId: -1 })] }),
        ])

        const { newSections } = await getSVMData(false)

        // The row key doubles as the delete target, so a row that cannot supply one is not
        // rendered at all rather than shown with a key that deletes nothing.
        expect(newSections[0]!.rows).toStrictEqual([])
    })

    it("blanks a leader's phone when their record carries none", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([makeSection()])
        const leader = makeUnitPerson()
        leader.person!.phone = null
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([makeUnit({ unitPersons: [leader] })])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows[0]!.deanDirectorPhone).toBe("")
    })
})

describe("getSVMData() - section grouping", () => {
    it("fetches units once and groups them onto their own sections", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([
            makeSection({ sectionId: 1, name: "VMDO" }),
            makeSection({ sectionId: 2, name: "Departments" }),
        ])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({ unitId: 10, sectionId: 1, name: "Dean's Office", unitPersons: [makeUnitPerson()] }),
            makeUnit({
                unitId: 20,
                sectionId: 2,
                name: "Anatomy",
                unitPersons: [makeUnitPerson({ unitPersonId: 5, unitId: 20 })],
            }),
        ])

        const { newSections, newUnitOptions } = await getSVMData(false)

        // One request for the whole list rather than one per section, so page load no longer
        // costs a round trip per section.
        expect(svmUnitService.getAllUnits).toHaveBeenCalledOnce()
        expect(newSections[0]!.rows.map((r) => r.unitName)).toStrictEqual(["Dean's Office"])
        expect(newSections[1]!.rows.map((r) => r.unitName)).toStrictEqual(["Anatomy"])
        expect(newUnitOptions).toStrictEqual([
            { section: 1, units: [{ label: "Dean's Office", value: "10" }] },
            { section: 2, units: [{ label: "Anatomy", value: "20" }] },
        ])
    })

    it("leaves a section with no units empty rather than borrowing another section's", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(svmSectionService.getSections).mockResolvedValue([
            makeSection({ sectionId: 1, name: "VMDO" }),
            makeSection({ sectionId: 2, name: "Departments" }),
        ])
        vi.mocked(svmUnitService.getAllUnits).mockResolvedValue([
            makeUnit({ unitId: 20, sectionId: 2, unitPersons: [makeUnitPerson({ unitId: 20 })] }),
        ])

        const { newSections } = await getSVMData(false)

        expect(newSections[0]!.rows).toStrictEqual([])
        expect(newSections[1]!.rows).toHaveLength(1)
    })
})

describe("getFrequentlyCalledNumbers()", () => {
    it("maps numberId to entryId", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const apiResponse: SVMFrequentNumberAPIResponse[] = [
            { numberId: 5, label: "Front Desk", phone: "530-555-1000", sortOrder: null },
        ]
        vi.mocked(svmFrequentNumberService.getFrequentNumbers).mockResolvedValue(apiResponse)

        const results = await getFrequentlyCalledNumbers()

        expect(results).toStrictEqual([{ label: "Front Desk", phone: "530-555-1000", entryId: 5 }])
    })
})
