import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { createMemoryHistory, createRouter } from "vue-router"
import EisPerson from "../pages/EisPerson.vue"
import EisSummary from "../pages/EisSummary.vue"
import { eisService } from "../services/eis-service"
import type { EisPersonHeader } from "../types/eis-types"

/**
 * EisPerson loads the header once for every tab and shares it with the Summary tab. An employee
 * the API won't show (unknown, or outside the user's units) gets one neutral message.
 */

vi.mock("../services/eis-service", () => ({
    eisService: {
        getHeader: vi.fn<(...args: unknown[]) => unknown>(),
        photoUrl: (employeeId: string) => `/photo/${employeeId}`,
    },
}))

const header: EisPersonHeader = {
    employeeId: "10123456",
    name: "Lovelace, Ada",
    email: "aalovelace@ucdavis.edu",
    dateOfBirth: "12/1815",
    age: 61,
    gender: "F",
    ethnicity: null,
    hireDate: "1995-07-01",
    employmentStatus: "ACTIVE",
    citizenship: "US CITIZEN",
    visa: null,
    primaryAffiliation: "FACULTY",
    primaryTitle: "PROF-AY",
    jobGroup: "114",
    homeDepartment: "VM: VME",
    alternateDepartment: null,
    bargainingUnit: "99",
    laborRelationsUnit: null,
    isStaff: true,
    staffProgram: "MSP",
    staffStatus: "CAREER",
    isFaculty: true,
    facultyProgram: "SENATE",
    ladderRank: "Y",
    serviceCreditMonths: 360,
    vacationHours: 120.5,
    sickHours: 800,
    ptoHours: 8,
}

const Blank = { template: "<div />" }

async function open(path: string, loaded: EisPersonHeader | null) {
    vi.mocked(eisService.getHeader).mockResolvedValue(loaded)
    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: "/Personnel/EIS", name: "EisSelectPerson", component: Blank },
            {
                path: "/Personnel/EIS/:employeeId",
                component: EisPerson,
                children: [
                    { path: "", name: "EisSummary", component: EisSummary },
                    { path: "Appointments", name: "EisAppointments", component: Blank },
                    { path: "History", name: "EisHistory", component: Blank },
                    { path: "Address", name: "EisAddress", component: Blank },
                ],
            },
        ],
    })
    await router.push(path)
    const wrapper = mount({ template: "<router-view />" }, { global: { plugins: [Quasar, router] } })
    await flushPromises()
    return wrapper
}

describe("eisPerson.vue", () => {
    it("shows the header, the tabs and the summary details", async () => {
        expect.hasAssertions()
        const wrapper = await open("/Personnel/EIS/10123456", header)

        expect(eisService.getHeader).toHaveBeenCalledWith("10123456")
        expect(wrapper.find("h1").text()).toContain("Lovelace, Ada")
        expect(wrapper.find("img").attributes("src")).toBe("/photo/10123456")
        expect(
            ["Staff program", "Ladder rank", "Appointment history", "360 months (30.00 yrs)", "PTO balance"].filter(
                (expected) => !wrapper.text().includes(expected),
            ),
        ).toStrictEqual([])
        expect(wrapper.find("a[href='mailto:aalovelace@ucdavis.edu']").exists()).toBeTruthy()
    })

    it("leaves out staff, faculty and PTO details the employee doesn't have", async () => {
        expect.hasAssertions()
        const wrapper = await open("/Personnel/EIS/10123456", {
            ...header,
            email: null,
            age: null,
            isStaff: false,
            isFaculty: false,
            ptoHours: null,
        })

        expect(
            ["Staff program", "Ladder rank", "PTO balance"].filter((unexpected) => wrapper.text().includes(unexpected)),
        ).toStrictEqual([])
        expect(wrapper.find("a[href^='mailto:']").exists()).toBeFalsy()
    })

    it("shows one neutral message for an employee the API won't show", async () => {
        expect.hasAssertions()
        const wrapper = await open("/Personnel/EIS/10999999", null)

        expect(wrapper.text()).toContain("This employee isn't available")
        expect(wrapper.find("img").exists()).toBeFalsy()
    })
})
