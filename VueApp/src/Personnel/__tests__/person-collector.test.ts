import { flushPromises, mount } from "@vue/test-utils"
import { Notify, QInput, QSelect, QToggle, Quasar } from "quasar"
import { createMemoryHistory, createRouter } from "vue-router"
import PersonCollectorChoices from "../components/PersonCollectorChoices.vue"
import PersonCollectorFilters from "../components/PersonCollectorFilters.vue"
import PersonCollectorResults from "../components/PersonCollectorResults.vue"
import PersonCollector from "../pages/PersonCollector.vue"
import { routes } from "../router/routes"
import { emptyRequest } from "../services/person-collector-request"
import { personCollectorService } from "../services/person-collector-service"
import type { PersonCollectorForm, PersonCollectorResult } from "../types/person-collector-types"

/**
 * The Person Collector reloads its lists shortly after each change, shows one tab per list with
 * the ID columns the user may see, and offers the same lists in Excel.
 */

const mockCopy = vi.fn<(text: string) => Promise<void>>()
vi.mock("quasar", async (importOriginal) => ({
    ...(await importOriginal<typeof import("quasar")>()),
    copyToClipboard: (text: string) => mockCopy(text),
}))
vi.mock("../services/person-collector-service", () => ({
    personCollectorService: {
        getForm: vi.fn<(...args: unknown[]) => unknown>(),
        getResults: vi.fn<(...args: unknown[]) => unknown>(),
        exportExcel: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const RELOAD_WAIT_MS = 600

const form: PersonCollectorForm = {
    departments: [
        { key: "VME", label: "VME" },
        { key: "VSR", label: "VSR" },
    ],
    senateGroups: [{ key: "PROFESSOR", label: "Professors" }],
    federationGroups: [{ key: "ADJUNCT", label: "Adjunct Professors" }],
    studentClasses: [{ key: "V1", label: "V1" }],
    showLoginIds: false,
    showMoreIds: false,
}

const ada = {
    name: "Lovelace, Ada",
    email: "ada@ucdavis.edu",
    loginId: "alovelace",
    employeeId: "10123456",
    mothraId: "01234567",
    mailId: "alovelace",
    pidm: "123",
    bannerId: null,
}

function result(showIds: boolean): PersonCollectorResult {
    return {
        sections: [
            { key: "senate", title: "Senate Faculty", people: [ada, { ...ada, name: "Byron, Ada" }] },
            { key: "staff", title: "Staff Employees", people: [{ ...ada, name: null, email: null, mothraId: null }] },
        ],
        showLoginIds: showIds,
        showMoreIds: showIds,
    }
}

const quasar: [typeof Quasar, { plugins: { Notify: typeof Notify } }] = [Quasar, { plugins: { Notify } }]
const global = { plugins: [quasar] }

async function mountPage() {
    const wrapper = mount(PersonCollector, { global })
    await flushPromises()
    return wrapper
}

type PageWrapper = Awaited<ReturnType<typeof mountPage>>

/** Changes the choices the way the filter panel does, then waits out the reload delay. */
async function choose(wrapper: PageWrapper, choices: Partial<ReturnType<typeof emptyRequest>>) {
    wrapper.findComponent(PersonCollectorFilters).vm.$emit("update:modelValue", { ...emptyRequest(), ...choices })
    await vi.advanceTimersByTimeAsync(RELOAD_WAIT_MS)
    await flushPromises()
}

/** Fake timers for the reload delay, and a form and lists the page can load. */
function setUp() {
    vi.useFakeTimers()
    onTestFinished(() => {
        vi.useRealTimers()
    })
    vi.mocked(personCollectorService.getForm).mockResolvedValue(form)
    vi.mocked(personCollectorService.getResults).mockResolvedValue(result(false))
}

/** A reply the test settles by hand, to simulate a slow request. */
function slowReply<T>() {
    const handle: { settle?: (value: T) => void } = {}
    // eslint-disable-next-line avoid-new -- settled by the test to simulate a slow reply
    const promise = new Promise<T>((resolve) => {
        handle.settle = resolve
    })
    return { promise, settle: (value: T) => handle.settle?.(value) }
}

describe("personCollector.vue", () => {
    it("loads the lists after a change, without a submit button", async () => {
        expect.hasAssertions()
        setUp()
        const wrapper = await mountPage()
        const before = wrapper.text()

        await choose(wrapper, { departments: ["VME"], senateGroups: ["PROFESSOR"], fullDepartmentList: true })

        expect(before).toContain("Choose faculty, staff or students")
        expect(vi.mocked(personCollectorService.getResults).mock.calls).toStrictEqual([
            [{ ...emptyRequest(), departments: ["VME"], senateGroups: ["PROFESSOR"], fullDepartmentList: true }],
        ])
        expect(wrapper.text()).toContain("Senate Faculty")
        expect(wrapper.find('[role="status"].sr-only').text()).toBe("3 people in 2 lists")
    })

    it("waits for the choices to settle and ignores a reply that a newer change replaced", async () => {
        expect.hasAssertions()
        setUp()
        const first = slowReply<PersonCollectorResult>()
        vi.mocked(personCollectorService.getResults).mockReturnValueOnce(first.promise)
        const wrapper = await mountPage()

        await choose(wrapper, { staffMsp: true })
        await choose(wrapper, { staffPss: true })
        first.settle({ ...result(false), sections: [] })
        await flushPromises()

        expect(personCollectorService.getResults).toHaveBeenCalledTimes(2)
        expect(wrapper.text()).toContain("Staff Employees")
    })

    it("clears the lists when nothing is chosen and exports the current choices", async () => {
        expect.hasAssertions()
        setUp()
        const wrapper = await mountPage()
        await choose(wrapper, { staffVeterinarians: true })

        await wrapper.find("button.export-excel").trigger("click")
        await choose(wrapper, { departments: ["VME"] })

        expect(personCollectorService.exportExcel).toHaveBeenCalledWith({ ...emptyRequest(), staffVeterinarians: true })
        expect(personCollectorService.getResults).toHaveBeenCalledOnce()
        expect(wrapper.text()).toContain("Choose faculty, staff or students")
    })

    it("says when the form or the lists can't be loaded", async () => {
        expect.hasAssertions()
        setUp()
        vi.mocked(personCollectorService.getForm).mockResolvedValueOnce(null)
        const noForm = await mountPage()
        vi.mocked(personCollectorService.getResults).mockResolvedValue(null)
        const wrapper = await mountPage()

        await choose(wrapper, { staffMsp: true })

        expect(noForm.text()).toContain("could not be loaded")
        expect(wrapper.text()).toContain("The lists could not be loaded")
    })
})

describe("personCollectorFilters.vue", () => {
    it("writes each change back as a new request", () => {
        expect.hasAssertions()
        const wrapper = mount(PersonCollectorFilters, { props: { form, modelValue: emptyRequest() }, global })
        const selectValues = [["VME"], ["PROFESSOR"], ["ADJUNCT"], ["V1", "MPVM"]]

        for (const [index, select] of wrapper.findAllComponents(QSelect).entries()) {
            select.vm.$emit("update:modelValue", selectValues[index])
        }
        for (const toggle of wrapper.findAllComponents(QToggle)) {
            toggle.vm.$emit("update:modelValue", true)
        }

        expect(wrapper.emitted("update:modelValue")).toHaveLength(9)
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toStrictEqual([
            {
                departments: ["VME"],
                fullDepartmentList: true,
                senateGroups: ["PROFESSOR"],
                senateEmeriti: true,
                federationGroups: ["ADJUNCT"],
                staffMsp: true,
                staffPss: true,
                staffVeterinarians: true,
                studentClasses: ["V1"],
                studentMpvm: true,
            },
        ])
    })

    it("clears every choice", async () => {
        expect.hasAssertions()
        const chosen = { ...emptyRequest(), staffMsp: true, departments: ["VME"] }
        const wrapper = mount(PersonCollectorFilters, { props: { form, modelValue: chosen }, global })

        await wrapper.find("button").trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[emptyRequest()]])
    })
})

describe("personCollectorChoices.vue", () => {
    it("selects every choice from the menu's first entry, then clears them", async () => {
        expect.hasAssertions()
        const wrapper = mount(PersonCollectorChoices, {
            props: { label: "Departments", choices: form.departments, modelValue: ["VME"] },
            global,
        })
        const select = wrapper.findComponent(QSelect)

        ;(select.vm as unknown as { showPopup: () => void }).showPopup()
        await flushPromises()
        const allEntry = document.body.querySelector<HTMLElement>(".q-menu .q-item")
        allEntry?.click()
        await wrapper.setProps({ modelValue: ["VME", "VSR"] })
        await flushPromises()
        document.body.querySelector<HTMLElement>(".q-menu .q-item")?.click()

        expect(allEntry?.textContent).toContain("Select all")
        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[["VME", "VSR"]], [[]]])
    })

    it("removes a chip", async () => {
        expect.hasAssertions()
        const wrapper = mount(PersonCollectorChoices, {
            props: { label: "Departments", choices: form.departments, modelValue: ["VME", "VSR"], allLabel: "All" },
            global,
        })

        await wrapper.find(".q-chip__icon--remove").trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[["VSR"]]])
    })
})

describe("personCollectorResults.vue", () => {
    it("shows a tab per list with ID columns only when allowed", async () => {
        expect.hasAssertions()
        const withIds = mount(PersonCollectorResults, { props: { result: result(true) }, global })
        const withoutIds = mount(PersonCollectorResults, { props: { result: result(false) }, global })
        await flushPromises()

        expect(withIds.findAll('[role="tab"]').map((tab) => tab.text())).toStrictEqual([
            "Senate Faculty2",
            "Staff Employees1",
        ])
        expect(withIds.find("thead").text()).toContain("Mothra ID")
        expect(withoutIds.find("thead").text()).not.toContain("Login ID")
    })

    it("searches a list and greys out the count of an empty one", async () => {
        expect.hasAssertions()
        const withEmpty = {
            ...result(false),
            sections: [...result(false).sections, { key: "students", title: "Students", people: [] }],
        }
        const wrapper = mount(PersonCollectorResults, { props: { result: withEmpty }, global })

        wrapper.findComponent(QInput).vm.$emit("update:modelValue", "Byron")
        await flushPromises()

        expect(wrapper.findAll("tbody tr").map((row) => row.text())).toStrictEqual(["Byron, Adaada@ucdavis.edu"])
        expect(wrapper.findAll(".q-badge").at(-1)?.classes()).toContain("bg-grey-6")
    })

    it("keeps the open tab when it still exists and moves to the first when it doesn't", async () => {
        expect.hasAssertions()
        const wrapper = mount(PersonCollectorResults, { props: { result: result(false) }, global })
        await wrapper.findAll('[role="tab"]')[1]?.trigger("click")
        await wrapper.setProps({ result: { ...result(false) } })
        const kept = wrapper.find('[role="tab"][aria-selected="true"]').text()

        await wrapper.setProps({ result: { ...result(false), sections: [result(false).sections[0]!] } })

        expect(kept).toBe("Staff Employees1")
        expect(wrapper.find('[role="tab"][aria-selected="true"]').text()).toBe("Senate Faculty2")

        await wrapper.setProps({ result: { ...result(false), sections: [] } })

        expect(wrapper.findAll('[role="tab"]')).toHaveLength(0)
    })

    it("copies each email address once, and says when copying fails", async () => {
        expect.hasAssertions()
        mockCopy.mockResolvedValueOnce().mockRejectedValueOnce(new Error("denied"))
        const wrapper = mount(PersonCollectorResults, { props: { result: result(false) }, global })
        const copy = () => wrapper.findAll("button").find((button) => button.text().includes("Copy"))

        await copy()?.trigger("click")
        await flushPromises()
        await copy()?.trigger("click")
        await flushPromises()

        expect(mockCopy).toHaveBeenCalledWith("ada@ucdavis.edu")
        expect(document.body.textContent).toContain("could not be copied")
    })
})

describe("person collector route", () => {
    it("leaves access to the API, since the browser never loads this permission", () => {
        expect.hasAssertions()
        const router = createRouter({ history: createMemoryHistory(), routes })

        const route = router.resolve("/Personnel/personcollector/")

        expect(route.name).toBe("PersonCollector")
        expect(route.meta.permissions).toBeUndefined()
    })
})
