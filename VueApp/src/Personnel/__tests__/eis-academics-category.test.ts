import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import EisAcademics from "../pages/EisAcademics.vue"
import EisAppointmentCategory from "../pages/EisAppointmentCategory.vue"
import { eisService } from "../services/eis-service"
import type { EisAcademics as EisAcademicsData, EisAppointmentCategories } from "../types/eis-types"

/**
 * The Awards & degrees tab shows MyInfoVault entries as text, and the Appointment category tab
 * lets EIS administrators set and clear the manual categories.
 */

const EMPLOYEE_ID = "10123456"
const mocked = vi.hoisted(() => ({ route: { params: { employeeId: "" } } }))
vi.mock("vue-router", async () => {
    // Reactive, so a test can move to another employee the way the person picker does.
    const { reactive } = await import("vue")
    mocked.route = reactive({ params: { employeeId: "" } })
    return { useRoute: () => mocked.route }
})
vi.mock("../services/eis-service", () => ({
    eisService: {
        getAcademics: vi.fn<(...args: unknown[]) => unknown>(),
        getCategories: vi.fn<(...args: unknown[]) => unknown>(),
        setFlag: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const academics: EisAcademicsData = {
    hasMivAccount: true,
    degrees: [{ year: "1994", degree: "DVM", institution: "UC Davis", location: "Davis, CA", field: "Medicine" }],
    boards: [{ year: "2001-present", text: "ACVIM" }],
    memberships: ["AVMA"],
    researchFocus: ["Virology"],
    specialtyFocus: ["Equine"],
    honors: [{ year: "2010", text: "Teaching award" }],
}

function categories(canEditFlags: boolean, directorApplies = false): EisAppointmentCategories {
    return {
        academicYear: "2026-2027",
        canEditFlags,
        categories: [
            { label: "Dean", applies: true, flagCode: null },
            { label: "Resident", applies: false, flagCode: null },
            { label: "Director", applies: directorApplies, flagCode: 4 },
        ],
    }
}

beforeEach(() => {
    mocked.route.params.employeeId = EMPLOYEE_ID
})

async function mountTab(component: object) {
    const wrapper = mount(component, { global: { plugins: [Quasar] } })
    await flushPromises()
    return wrapper
}

function missing(text: string, expected: string[]): string[] {
    return expected.filter((item) => !text.includes(item))
}

describe("eisAcademics.vue", () => {
    it("shows each kind of MyInfoVault entry", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAcademics).mockResolvedValue(academics)

        const wrapper = await mountTab(EisAcademics)

        expect(
            missing(wrapper.text(), [
                "UC Davis",
                "2001-present",
                "ACVIM",
                "AVMA",
                "Virology",
                "Equine",
                "Teaching award",
            ]),
        ).toStrictEqual([])
    })

    it("says when each list is empty", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAcademics).mockResolvedValue({
            ...academics,
            degrees: [],
            boards: [],
            memberships: [],
            researchFocus: [],
            specialtyFocus: [],
            honors: [],
        })

        const wrapper = await mountTab(EisAcademics)

        expect(
            missing(wrapper.text(), [
                "No degree information.",
                "No board or license information.",
                "No membership information.",
                "No research focus information.",
                "No specialty focus information.",
                "No award or honor information.",
            ]),
        ).toStrictEqual([])
    })

    it("explains a missing MyInfoVault account or a failed load", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getAcademics).mockResolvedValue({ ...academics, hasMivAccount: false })
        const noAccount = await mountTab(EisAcademics)

        vi.mocked(eisService.getAcademics).mockResolvedValue(null)
        const failed = await mountTab(EisAcademics)

        expect(noAccount.text()).toContain("no MyInfoVault account")
        expect(failed.text()).toContain("could not be loaded")
    })
})

describe("eisAppointmentCategory.vue", () => {
    it("ticks the categories that apply and locks the manual ones for non-admins", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getCategories).mockResolvedValue(categories(false))

        const wrapper = await mountTab(EisAppointmentCategory)
        const text = wrapper.text()

        expect(
            missing(text, ["Dean(applies)", "Resident(does not apply)", "2026-2027", "Only EIS administrators"]),
        ).toStrictEqual([])
        expect(wrapper.find('[role="checkbox"]').attributes("aria-disabled")).toBe("true")
    })

    it("lets an admin set a manual category and shows the updated categories", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getCategories).mockResolvedValue(categories(true))
        vi.mocked(eisService.setFlag).mockResolvedValue(categories(true, true))

        const wrapper = await mountTab(EisAppointmentCategory)
        await wrapper.find('[role="checkbox"]').trigger("click")
        await flushPromises()

        expect(eisService.setFlag).toHaveBeenCalledWith(EMPLOYEE_ID, 4, true)
        expect(wrapper.find('[role="checkbox"]').attributes("aria-checked")).toBe("true")
        expect(wrapper.text()).not.toContain("Only EIS administrators")
        expect(document.body.textContent).toContain("Category updated.")
    })

    it("ignores a save that finishes after the user has moved to another employee", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getCategories).mockResolvedValue(categories(true))
        const save: { finish?: (updated: EisAppointmentCategories | null) => void } = {}
        vi.mocked(eisService.setFlag).mockReturnValue(
            new Promise((resolve) => {
                save.finish = resolve
            }),
        )
        const wrapper = await mountTab(EisAppointmentCategory)
        await wrapper.find('[role="checkbox"]').trigger("click")

        mocked.route.params.employeeId = "10765432"
        await flushPromises()
        save.finish?.(categories(true, true))
        await flushPromises()

        expect(eisService.setFlag).toHaveBeenCalledWith(EMPLOYEE_ID, 4, true)
        expect(eisService.getCategories).toHaveBeenLastCalledWith("10765432")
        expect(wrapper.find('[role="checkbox"]').attributes("aria-checked")).toBe("false")
        expect(wrapper.find('[role="checkbox"]').attributes("aria-disabled")).not.toBe("true")
    })

    it("keeps a save's result when a reload of the same employee resolves after it", async () => {
        expect.hasAssertions()
        const reload: { finish?: (value: EisAppointmentCategories | null) => void } = {}
        vi.mocked(eisService.getCategories)
            .mockResolvedValueOnce(categories(true)) // The initial load for this employee
            .mockResolvedValueOnce(categories(true)) // The load after navigating away
            .mockReturnValueOnce(
                // The load after navigating back, held open - this is the stale response.
                new Promise((resolve) => {
                    reload.finish = resolve
                }),
            )
        const save: { finish?: (updated: EisAppointmentCategories | null) => void } = {}
        vi.mocked(eisService.setFlag).mockReturnValue(
            new Promise((resolve) => {
                save.finish = resolve
            }),
        )
        const wrapper = await mountTab(EisAppointmentCategory)
        await wrapper.find('[role="checkbox"]').trigger("click")

        mocked.route.params.employeeId = "10765432"
        await flushPromises()
        mocked.route.params.employeeId = EMPLOYEE_ID
        await flushPromises()

        // The save completes first, with the category now applied...
        save.finish?.(categories(true, true))
        await flushPromises()
        // ...and the reload that started before it resolves afterward, with the category as it
        // was before the save - a response to a request the server saw before the save committed.
        reload.finish?.(categories(true, false))
        await flushPromises()

        expect(wrapper.find('[role="checkbox"]').attributes("aria-checked")).toBe("true")
    })

    it("says when a change fails or the categories could not be loaded", async () => {
        expect.hasAssertions()
        vi.mocked(eisService.getCategories).mockResolvedValue(categories(true))
        vi.mocked(eisService.setFlag).mockResolvedValue(null)
        const wrapper = await mountTab(EisAppointmentCategory)
        await wrapper.find('[role="checkbox"]').trigger("click")
        await flushPromises()

        vi.mocked(eisService.getCategories).mockResolvedValue(null)
        const failed = await mountTab(EisAppointmentCategory)

        expect(wrapper.text()).toContain("could not be changed")
        expect(wrapper.find('[role="checkbox"]').attributes("aria-checked")).toBe("false")
        expect(failed.text()).toContain("could not be loaded")
    })
})
