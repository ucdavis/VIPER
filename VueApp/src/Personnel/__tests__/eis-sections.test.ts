import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import EisAddress from "../pages/EisAddress.vue"
import EisAppointments from "../pages/EisAppointments.vue"
import EisHistory from "../pages/EisHistory.vue"
import { eisService } from "../services/eis-service"
import type { EisAddress as EisAddressData, EisAppointments as EisAppointmentsData } from "../types/eis-types"

/**
 * The Appointments, Appointment history and Address tabs each load their own data for the
 * employee in the route and say so when it can't be loaded.
 */

vi.mock("vue-router", () => ({ useRoute: () => ({ params: { employeeId: "10123456" } }) }))
vi.mock("../services/eis-service", () => ({
    eisService: {
        getAppointments: vi.fn<(...args: unknown[]) => unknown>(),
        getHistory: vi.fn<(...args: unknown[]) => unknown>(),
        getAddress: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const appointments: EisAppointmentsData = {
    appointments: [
        {
            number: "1",
            title: "PROF-AY",
            titleCode: "001100",
            grade: null,
            exemptStatus: "EXEMPT",
            department: "VM: VME",
            beginDate: "07/01/2020",
            endDate: null,
            distributions: [
                {
                    number: "1",
                    beginDate: "2025-07-01",
                    endDate: null,
                    percent: 75,
                    account: "12345",
                    step: "5",
                    dosCode: "REG",
                    amount: 75000,
                },
                {
                    number: "2",
                    beginDate: "2025-07-01",
                    endDate: "2026-06-30",
                    percent: 100,
                    account: null,
                    step: "5",
                    dosCode: "FEP",
                    amount: -2000,
                },
            ],
            total: 100000,
        },
    ],
    stipends: [{ effectiveDate: "2025-07-01", endDate: null, earningsCode: "STP", annualAmount: 6000 }],
    totalAnnual: 106000,
}

const address: EisAddressData = {
    permanent: {
        line1: "1 Shields Ave",
        line2: "Apt 2",
        city: "Davis",
        state: "CA",
        zip: "95616",
        releaseCampus: "N",
        releaseOrganization: "Y",
    },
    homePhone: { phone: "530-555-1000", releaseCampus: "1", releaseOrganization: null },
    campusListings: [
        { isPrimary: true, isPublic: true, title: "Professor", department: "VM: VME", address: "Davis", phone: null },
        { isPrimary: false, isPublic: false, title: null, department: null, address: null, phone: "530-752-0000" },
    ],
    campusDirectoryAvailable: true,
}

async function mountTab(component: object) {
    const wrapper = mount(component, { global: { plugins: [Quasar] } })
    await flushPromises()
    return wrapper
}

describe("eisAppointments.vue", () => {
    it("shows each appointment with its distributions, reductions, stipends and totals", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAppointments).mockResolvedValue(appointments)

        const wrapper = await mountTab(EisAppointments)
        const text = wrapper.text()

        expect(eisService.getAppointments).toHaveBeenCalledWith("10123456")
        expect(
            [
                "Appt. 1 — PROF-AY (001100) — EXEMPT — VM: VME — 07/01/2020",
                "INDEF",
                "75,000.00",
                "-2,000.00 (reduction)",
                "Appointment total 100,000.00",
                "Total annual: 106,000.00",
            ].filter((expected) => !text.includes(expected)),
        ).toStrictEqual([])
    })

    it("says when there is nothing to show or the load failed", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAppointments).mockResolvedValue({ appointments: [], stipends: [], totalAnnual: 0 })
        const empty = await mountTab(EisAppointments)
        vi.mocked(eisService.getAppointments).mockResolvedValue(null)
        const failed = await mountTab(EisAppointments)

        expect(empty.text()).toContain("No appointments or stipends.")
        expect(failed.text()).toContain("could not be loaded")
    })
})

describe("eisHistory.vue", () => {
    it("shows UCPath and PPS history, with a note for each empty list", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getHistory).mockResolvedValue({
            appointments: [
                {
                    actionDate: "2025-07-01",
                    title: "PROF-AY",
                    titleCode: "001100",
                    department: "VM: VME",
                    beginDate: "2025-07-01",
                    endDate: null,
                    step: "5",
                    percent: 1,
                    payRate: 150000,
                    comment: "MERIT",
                },
            ],
            ppsAppointments: [
                {
                    actionDate: null,
                    title: "ASST PROF",
                    titleCode: null,
                    department: null,
                    beginDate: null,
                    endDate: null,
                    step: null,
                    percent: null,
                    payRate: null,
                    comment: null,
                },
            ],
            leaves: [],
            ppsLeaves: [{ beginDate: "2001-01-01", returnDate: "2001-06-30", description: "SABBATICAL" }],
        })

        const wrapper = await mountTab(EisHistory)
        const text = wrapper.text()

        expect(
            ["PROF-AY (001100)", "150,000.00", "ASST PROF", "No leave data available.", "SABBATICAL"].filter(
                (expected) => !text.includes(expected),
            ),
        ).toStrictEqual([])
    })

    it("says when the history could not be loaded", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getHistory).mockResolvedValue(null)

        const wrapper = await mountTab(EisHistory)

        expect(wrapper.text()).toContain("could not be loaded")
    })
})

describe("eisAddress.vue", () => {
    it("shows the permanent address with release flags and the campus listings", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAddress).mockResolvedValue(address)

        const wrapper = await mountTab(EisAddress)
        const text = wrapper.text()

        expect(
            [
                "1 Shields Ave, Apt 2, Davis, CA 95616",
                "Not released",
                "530-555-1000",
                "Primary listing",
                "Additional listing 1",
                "Not public",
            ].filter((expected) => !text.includes(expected)),
        ).toStrictEqual([])
    })

    it("explains an unreachable directory, a missing address and a missing listing", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAddress).mockResolvedValue({
            permanent: { ...address.permanent!, line2: null, releaseCampus: "Y" },
            homePhone: null,
            campusListings: [],
            campusDirectoryAvailable: false,
        })
        const unreachable = await mountTab(EisAddress)

        vi.mocked(eisService.getAddress).mockResolvedValue({
            permanent: null,
            homePhone: null,
            campusListings: [],
            campusDirectoryAvailable: true,
        })
        const empty = await mountTab(EisAddress)

        expect(unreachable.text()).toContain("1 Shields Ave, Davis, CA 95616")
        expect(unreachable.text()).toContain("campus directory could not be reached")
        expect(empty.text()).toContain("No permanent address or home phone.")
        expect(empty.text()).toContain("No campus directory listing.")
    })

    it("says when the address could not be loaded", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAddress).mockResolvedValue(null)

        const wrapper = await mountTab(EisAddress)

        expect(wrapper.text()).toContain("could not be loaded")
    })
})
