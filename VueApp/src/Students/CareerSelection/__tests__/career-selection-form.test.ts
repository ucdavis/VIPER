import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import CareerSelectionForm from "../pages/CareerSelectionForm.vue"
import { careerSelectionService } from "../services/career-selection-service"
import type { CareerDropdownOption, StudentCareerDetail } from "../types"

/**
 * Tests for the career selection form's loading. The record and the dropdown options load side by
 * side, and the form must not open until all of them have, or a saved choice could be cleared and
 * saved against an empty option list.
 */

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { pidm: "42" } }),
    useRouter: () => ({ push: vi.fn<(to: unknown) => void>(), replace: vi.fn<(to: unknown) => void>() }),
}))
vi.mock("@/composables/CheckPagePermission", () => ({ checkHasOnePermission: () => false }))
vi.mock("@/composables/use-confirm-leave", () => ({ useConfirmLeave: () => {} }))

const detail: StudentCareerDetail = {
    personId: 42,
    fullName: "Student, Test",
    classLevel: "V2",
    studentInfo: {
        direction: null,
        directionOther: "",
        primaryFocus: null,
        primaryFocusOther: "",
        secondaryFocus: null,
        secondaryFocusOther: "",
        postGrad: null,
        mentorId: null,
        mentorName: "",
        mentorIamId: null,
        shortTermPlans: "",
        longTermPlans: "",
    },
    canEdit: true,
    canViewStudentList: true,
    lastUpdated: null,
}

function mountForm() {
    return mount(CareerSelectionForm, {
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                StudentRecordPageShell: { name: "StudentRecordPageShell", template: "<div />", props: ["loading"] },
            },
        },
    })
}

describe("career selection form", () => {
    it("stays loading until the dropdown options arrive, not just the record", async () => {
        expect.hasAssertions()
        let resolveOptions: (options: CareerDropdownOption[]) => void = () => {}
        const pendingOptions = new Promise<CareerDropdownOption[]>((resolve) => {
            resolveOptions = resolve
        })
        vi.spyOn(careerSelectionService, "getDetail").mockResolvedValue(detail)
        vi.spyOn(careerSelectionService, "getDropdownOptions").mockImplementation((kind) =>
            kind === "postGrad" ? pendingOptions : Promise.resolve([]),
        )

        const wrapper = mountForm()
        await flushPromises()
        const shell = wrapper.findComponent({ name: "StudentRecordPageShell" })

        expect(shell.props("loading")).toBeTruthy()

        resolveOptions([])
        await flushPromises()

        expect(shell.props("loading")).toBeFalsy()
    })
})
