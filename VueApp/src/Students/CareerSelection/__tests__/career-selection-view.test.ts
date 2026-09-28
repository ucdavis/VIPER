import { mount, flushPromises } from "@vue/test-utils"
import { setActivePinia, createPinia } from "pinia"
import { Quasar } from "quasar"
import { useUserStore } from "@/store/UserStore"
import { setAccessNotice } from "@/Students/composables/use-access-notice"
import { CAREER_SELECTION_ACCESS_MESSAGES } from "../constants/access-messages"
import CareerSelectionView from "../pages/CareerSelectionView.vue"
import { careerSelectionService } from "../services/career-selection-service"
import type { StudentCareerDetail, StudentInfo } from "../types"

/**
 * Tests for the read-only career selection record. Its work is turning the stored answers into
 * the label/value pairs the page lists: a choice normally reads as its own label, but the
 * catch-all reads as the free text the student typed instead.
 */

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { pidm: "42" } }),
    useRouter: () => ({ push: vi.fn<(to: unknown) => void>() }),
}))

function option(label: string, isOther = false) {
    return { label, value: 1, isOther }
}

function studentInfo(overrides: Partial<StudentInfo> = {}): StudentInfo {
    return {
        direction: option("Private Practice"),
        directionOther: "",
        primaryFocus: option("Equine"),
        primaryFocusOther: "",
        secondaryFocus: option("Small Animal"),
        secondaryFocusOther: "",
        postGrad: option("Internship"),
        mentorId: 7,
        mentorName: "Mentor, Alex",
        mentorIamId: "iam-7",
        shortTermPlans: "Build a rural practice.",
        longTermPlans: "Teach eventually.",
        ...overrides,
    }
}

/** The record on the route is person 42; `viewerId` is who is signed in. */
async function mountView(info: Partial<StudentInfo> = {}, { canEdit = false, viewerId = 1 } = {}) {
    setActivePinia(createPinia())
    useUserStore().userInfo.userId = viewerId

    const detail: StudentCareerDetail = {
        personId: 42,
        fullName: "Student, Test",
        classLevel: "V2",
        studentInfo: studentInfo(info),
        canEdit,
        canViewStudentList: false,
        lastUpdated: null,
    }
    vi.spyOn(careerSelectionService, "getDetail").mockResolvedValue(detail)

    const wrapper = mount(CareerSelectionView, {
        global: {
            plugins: [[Quasar, {}]],
            stubs: { StudentRecordPageShell: { template: "<div><slot /></div>" } },
        },
    })
    await flushPromises()
    return wrapper
}

/** The rendered pairs, as { label: value }, read straight off the description list. */
function pairs(wrapper: Awaited<ReturnType<typeof mountView>>): Record<string, string> {
    const terms = wrapper.findAll("dt")
    const values = wrapper.findAll("dd")
    return Object.fromEntries(terms.map((term, i) => [term.text(), values[i].text()]))
}

describe("career selection view", () => {
    it("pairs every answer with the question it answers", async () => {
        expect.hasAssertions()
        const wrapper = await mountView()

        // A dt/dd pair per answer is what makes the association programmatic rather than visual.
        expect(wrapper.findAll("dt")).toHaveLength(7)
        expect(wrapper.findAll("dd")).toHaveLength(7)
    })

    it("reads each choice as its own label", async () => {
        expect.hasAssertions()
        const wrapper = await mountView()

        expect(pairs(wrapper)).toStrictEqual({
            "Career Direction": "Private Practice",
            "Primary Focus": "Equine",
            "Secondary Focus": "Small Animal",
            "Post-Graduation Plans": "Internship",
            "Mentoring Faculty": "Mentor, Alex",
            "Short Term Plans": "Build a rural practice.",
            "Long Term Plans": "Teach eventually.",
        })
    })

    it("reads a catch-all choice as the free text it belongs to", async () => {
        expect.hasAssertions()
        // Each catch-all has its own free-text field, so this also catches them being crossed.
        const wrapper = await mountView({
            direction: option("Other", true),
            directionOther: "Zoo medicine",
            primaryFocus: option("Other", true),
            primaryFocusOther: "Reptiles",
            secondaryFocus: option("Other", true),
            secondaryFocusOther: "Raptors",
        })

        const shown = pairs(wrapper)
        expect(shown["Career Direction"]).toBe("Zoo medicine")
        expect(shown["Primary Focus"]).toBe("Reptiles")
        expect(shown["Secondary Focus"]).toBe("Raptors")
    })

    it("sends a catch-all post-graduation plan to the short term plans", async () => {
        expect.hasAssertions()
        // Post-graduation is the one choice with no free-text field of its own.
        const wrapper = await mountView({ postGrad: option("Other", true) })

        expect(pairs(wrapper)["Post-Graduation Plans"]).toBe("Other - See short-term plans")
    })

    it("shows a dash for an answer the student has not given", async () => {
        expect.hasAssertions()
        const wrapper = await mountView({ direction: null, mentorName: "", longTermPlans: "" })

        const shown = pairs(wrapper)
        expect(shown["Career Direction"]).toBe("—")
        expect(shown["Mentoring Faculty"]).toBe("—")
        expect(shown["Long Term Plans"]).toBe("—")
    })

    it("keeps the paragraphs of a plan", async () => {
        expect.hasAssertions()
        const wrapper = await mountView({ longTermPlans: "Build a practice.\n\nThen teach." })

        const longTerm = wrapper.findAll("dd").at(-1)
        // The newlines survive to the DOM, and the class is what renders them as line breaks.
        expect(longTerm?.element.textContent).toBe("Build a practice.\n\nThen teach.")
        expect(longTerm?.classes()).toContain("detail-value")
    })

    it("shows the catch-all's dash when its free text was never filled in", async () => {
        expect.hasAssertions()
        const wrapper = await mountView({ direction: option("Other", true), directionOther: "" })

        expect(pairs(wrapper)["Career Direction"]).toBe("—")
    })
})

describe("career selection view while editing is closed", () => {
    const CLOSED = "Making changes is not allowed at this time."

    it("tells a student why they cannot edit their own record", async () => {
        expect.hasAssertions()
        // Without it, the Edit button just disappears, which looks the same as something broken.
        const wrapper = await mountView({}, { canEdit: false, viewerId: 42 })

        expect(wrapper.text()).toContain(CLOSED)
    })

    it("says nothing while the student can edit", async () => {
        expect.hasAssertions()
        const wrapper = await mountView({}, { canEdit: true, viewerId: 42 })

        expect(wrapper.text()).not.toContain(CLOSED)
    })

    it("says nothing to staff viewing another student's record", async () => {
        expect.hasAssertions()
        // A read-only viewer or mentor never could edit this record, so the closure is not news.
        const wrapper = await mountView({}, { canEdit: false, viewerId: 7 })

        expect(wrapper.text()).not.toContain(CLOSED)
    })
})

describe("career selection view after a redirect", () => {
    it("says why a student was sent here from a page they may not use", async () => {
        expect.hasAssertions()
        // A student refused on the options page is taken to their own record, and told why.
        setAccessNotice(CAREER_SELECTION_ACCESS_MESSAGES.MANAGE_OPTIONS)

        const wrapper = await mountView({}, { viewerId: 42 })

        expect(wrapper.text()).toContain(CAREER_SELECTION_ACCESS_MESSAGES.MANAGE_OPTIONS)
    })
})
