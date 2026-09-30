import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import CareerSelectionForm from "../pages/CareerSelectionForm.vue"
import { careerSelectionService } from "../services/career-selection-service"
import type { CareerDropdownOption, StudentCareerDetail } from "../types"

/**
 * Tests for the career selection form's loading. The record loads first, since it says whether the
 * user may edit, and the dropdown options only for those who can. The form must not open until
 * all of them have, or a saved choice could be cleared and saved against an empty option list.
 */

const { mockReplace } = vi.hoisted(() => ({ mockReplace: vi.fn<(to: unknown) => void>() }))

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { pidm: "42" } }),
    useRouter: () => ({ push: vi.fn<(to: unknown) => void>(), replace: mockReplace }),
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

    // The route guard admits by the client's permissions; whether this record can be edited is
    // the server's answer, the same one that decides whether the view page shows an Edit button.
    it("sends staff the server will not let edit on to the view page", async () => {
        expect.hasAssertions()
        vi.spyOn(careerSelectionService, "getDetail").mockResolvedValue({ ...detail, canEdit: false })
        const getOptions = vi.spyOn(careerSelectionService, "getDropdownOptions").mockResolvedValue([])

        mountForm()
        await flushPromises()

        expect(mockReplace).toHaveBeenCalledWith({ name: "CareerSelectionView", params: { pidm: 42 } })
        // The options endpoint serves only editors, so asking for them here would raise a
        // permission error before the redirect.
        expect(getOptions).not.toHaveBeenCalled()
    })

    it("fetches no options when the record does not load", async () => {
        expect.hasAssertions()
        vi.spyOn(careerSelectionService, "getDetail").mockResolvedValue(null)
        const getOptions = vi.spyOn(careerSelectionService, "getDropdownOptions").mockResolvedValue([])

        const wrapper = mountForm()
        await flushPromises()

        expect(getOptions).not.toHaveBeenCalled()
        expect(wrapper.findComponent({ name: "StudentRecordPageShell" }).props("loading")).toBeFalsy()
    })

    it("opens the form for anyone the server lets edit", async () => {
        expect.hasAssertions()
        // The client checks nothing here (checkHasOnePermission is mocked to deny everything),
        // so only the server's canEdit keeps the form open.
        vi.spyOn(careerSelectionService, "getDetail").mockResolvedValue(detail)
        const getOptions = vi.spyOn(careerSelectionService, "getDropdownOptions").mockResolvedValue([])

        mountForm()
        await flushPromises()

        expect(mockReplace).not.toHaveBeenCalled()
        expect(getOptions).toHaveBeenCalledTimes(3)
    })
})
