import { nextTick } from "vue"
import { setActivePinia, createPinia } from "pinia"
import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { useUserStore } from "@/store/UserStore"
import StudentClassYear from "@/Students/pages/StudentClassYear.vue"
import { routes } from "../router/routes"

// Every class-year mutation requires SVMSecure.SIS.AllStudents on the server, so a user with
// only SVMSecure.Students must not be offered controls that would come back 403.
const SIS = "SVMSecure.SIS.AllStudents"
const STUDENTS_ONLY = ["SVMSecure.Students"]

vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: vi.fn<(...args: unknown[]) => unknown>().mockResolvedValue({ success: true, result: [] }),
        put: vi.fn<(...args: unknown[]) => unknown>(),
        del: vi.fn<(...args: unknown[]) => unknown>(),
    }),
}))

vi.mock("vue-router", () => ({
    useRoute: () => ({ query: {} }),
}))

function signInWith(permissions: string[]) {
    setActivePinia(createPinia())
    useUserStore().setPermissions(permissions)
}

function mountPage() {
    return mount(StudentClassYear, {
        global: {
            plugins: [[Quasar, {}]],
            provide: { apiURL: import.meta.env.VITE_API_URL, viperOneUrl: "http://localhost/" },
            // QDialog teleports to body; render it inline so its contents are assertable.
            stubs: { StatusBadge: true, teleport: true },
        },
    })
}

function enterClassYearRoute(query: Record<string, string>) {
    const classYearRoute = routes.find((r) => r.path === "/Students/StudentClassYear")
    const guard = classYearRoute?.beforeEnter as (to: { query: Record<string, string> }) => unknown

    return guard({ query })
}

// The update dialog only exists once a class year is selected.
async function mountPageWithDialogOpen() {
    const wrapper = mountPage()
    ;(wrapper.vm as unknown as { showForm: boolean }).showForm = true
    await nextTick()
    return wrapper
}

// The import control needs a selected class year as well as the permission, and the class
// year defaults to 0, so assert on it only once a year is chosen.
async function mountPageWithClassYear() {
    const wrapper = mountPage()
    ;(wrapper.vm as unknown as { classYear: { label: string; value: number } }).classYear = {
        label: "Class of 2028",
        value: 2028,
    }
    await nextTick()
    return wrapper
}

describe("student class year permissions", () => {
    it("offers delete and save to a user with SIS.AllStudents", async () => {
        expect.hasAssertions()
        signInWith([...STUDENTS_ONLY, SIS])

        const wrapper = await mountPageWithDialogOpen()
        const text = wrapper.text()

        expect(text).toContain("Delete")
        expect(text).toContain("Save")
    })

    it("hides delete and save from a user with only SVMSecure.Students", async () => {
        expect.hasAssertions()
        signInWith(STUDENTS_ONLY)

        const wrapper = await mountPageWithDialogOpen()
        const text = wrapper.text()

        // Guard against the assertions passing just because the dialog never opened.
        expect(text).toContain("Current class year")
        expect(text).not.toContain("Delete")
        expect(text).not.toContain("Save")
    })

    // Pairs with the test below: without this one, dropping the permission check from the
    // import button would still leave the negative assertion passing on the class year alone.
    it("offers the class year import link to a user with SIS.AllStudents", async () => {
        expect.hasAssertions()
        signInWith([...STUDENTS_ONLY, SIS])

        const wrapper = await mountPageWithClassYear()

        expect(wrapper.text()).toContain("Import students into")
    })

    it("hides the class year import link from a user without SIS.AllStudents", async () => {
        expect.hasAssertions()
        signInWith(STUDENTS_ONLY)

        const wrapper = await mountPageWithClassYear()

        expect(wrapper.text()).not.toContain("Import students into")
    })

    it("gates the class year import route on SIS.AllStudents", () => {
        expect.hasAssertions()

        const importRoute = routes.find((r) => r.path === "/Students/StudentClassYearImport")

        expect(importRoute?.meta?.permissions).toStrictEqual([SIS])
    })

    it("sends the Razor ?import bookmark to the import route, keeping the class year", () => {
        expect.hasAssertions()

        expect(enterClassYearRoute({ import: "1", classYear: "2028" })).toStrictEqual({
            path: "/Students/StudentClassYearImport",
            query: { classYear: "2028" },
        })
    })

    it("leaves the class year page alone when there is no import query", () => {
        expect.hasAssertions()

        expect(enterClassYearRoute({ classYear: "2028" })).toBeTruthy()
    })
})
