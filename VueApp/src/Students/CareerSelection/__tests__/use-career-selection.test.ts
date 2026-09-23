import { useCareerSelection } from "../composables/use-career-selection"
import { careerSelectionService } from "../services/career-selection-service"
import type { StudentCareerDetail, StudentInfo } from "../types"

/**
 * Tests for useCareerSelection: the form state behind the career selection edit page, including
 * the dirty tracking the unsaved-changes prompt depends on.
 */

vi.mock("../services/career-selection-service", () => ({
    careerSelectionService: {
        getDetail: vi.fn<(...args: unknown[]) => unknown>(),
        updateCareerSelection: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const mockGetDetail = vi.mocked(careerSelectionService.getDetail)
const mockUpdate = vi.mocked(careerSelectionService.updateCareerSelection)

function studentInfo(overrides: Partial<StudentInfo> = {}): StudentInfo {
    return {
        direction: { label: "Academia", value: 1, isOther: false },
        directionOther: "",
        primaryFocus: null,
        primaryFocusOther: "",
        secondaryFocus: null,
        secondaryFocusOther: "",
        postGrad: null,
        mentorId: 500,
        mentorName: "Vet, Ann",
        mentorIamId: "IAM500",
        shortTermPlans: "Internship",
        longTermPlans: "",
        ...overrides,
    }
}

function detail(overrides: Partial<StudentCareerDetail> = {}): StudentCareerDetail {
    return {
        personId: 100,
        fullName: "Student, Test",
        classLevel: "V1",
        canEdit: true,
        canViewStudentList: false,
        studentInfo: studentInfo(),
        lastUpdated: "2026-04-17T10:00:00",
        ...overrides,
    }
}

describe("loading a record", () => {
    it("fills the form from the loaded record", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, studentInfo: form, detail: loaded } = useCareerSelection()

        await loadDetail(100)

        expect(mockGetDetail).toHaveBeenCalledWith(100)
        expect(loaded.value?.fullName).toBe("Student, Test")
        expect(form.value.shortTermPlans).toBe("Internship")
    })

    it("leaves the form empty when the record cannot be read", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(null)
        const { loadDetail, studentInfo: form, detail: loaded } = useCareerSelection()

        await loadDetail(100)

        expect(loaded.value).toBeNull()
        expect(form.value.shortTermPlans).toBe("")
    })

    it("clears the loading flag once the record is in", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, loading } = useCareerSelection()

        const pending = loadDetail(100)
        expect(loading.value).toBeTruthy()
        await pending

        expect(loading.value).toBeFalsy()
    })

    it("copies the record rather than editing it in place", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        // The form must not mutate the loaded record, which the page also reads from.
        const loadedRecord = detail()
        mockGetDetail.mockResolvedValue(loadedRecord)
        const { loadDetail, studentInfo: form } = useCareerSelection()

        await loadDetail(100)
        form.value.shortTermPlans = "Changed"

        expect(loadedRecord.studentInfo.shortTermPlans).toBe("Internship")
    })
})

describe("dirty tracking", () => {
    it("starts clean after loading", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, isDirty } = useCareerSelection()

        await loadDetail(100)

        expect(isDirty.value).toBeFalsy()
    })

    it("notices an edited field", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, studentInfo: form, isDirty } = useCareerSelection()
        await loadDetail(100)

        form.value.longTermPlans = "Practice ownership"

        expect(isDirty.value).toBeTruthy()
    })

    it("notices a changed dropdown", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, studentInfo: form, isDirty } = useCareerSelection()
        await loadDetail(100)

        form.value.direction = { label: "Industry", value: 2, isOther: false }

        expect(isDirty.value).toBeTruthy()
    })

    it("reads as clean again when the edit is undone", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, studentInfo: form, isDirty } = useCareerSelection()
        await loadDetail(100)

        form.value.longTermPlans = "Practice ownership"
        form.value.longTermPlans = ""

        expect(isDirty.value).toBeFalsy()
    })
})

describe("saving", () => {
    it("sends the form and keeps the refreshed record", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const saved = detail({ lastUpdated: "2026-04-18T09:00:00" })
        mockUpdate.mockResolvedValue({ success: true, result: saved, errors: [] })
        const { loadDetail, save, detail: loaded, studentInfo: form } = useCareerSelection()
        await loadDetail(100)
        form.value.longTermPlans = "Practice ownership"

        const succeeded = await save(100)

        expect(succeeded).toBeTruthy()
        // The form is repopulated from the response, so assert on what was sent, not on form.value.
        expect(mockUpdate).toHaveBeenCalledWith(100, expect.objectContaining({ longTermPlans: "Practice ownership" }))
        expect(loaded.value?.lastUpdated).toBe("2026-04-18T09:00:00")
    })

    it("reads as clean after a successful save", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, save, studentInfo: form, isDirty } = useCareerSelection()
        await loadDetail(100)
        form.value.longTermPlans = "Practice ownership"
        mockUpdate.mockResolvedValue({
            success: true,
            result: detail({ studentInfo: studentInfo({ longTermPlans: "Practice ownership" }) }),
            errors: [],
        })

        await save(100)

        expect(isDirty.value).toBeFalsy()
    })

    it("keeps the edits and reports the errors when the save is refused", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, save, studentInfo: form, saveErrors, isDirty } = useCareerSelection()
        await loadDetail(100)
        form.value.longTermPlans = "Practice ownership"
        mockUpdate.mockResolvedValue({ success: false, result: null, errors: ["Select a career direction."] })

        const succeeded = await save(100)

        expect(succeeded).toBeFalsy()
        expect(saveErrors.value).toStrictEqual(["Select a career direction."])
        expect(form.value.longTermPlans).toBe("Practice ownership")
        expect(isDirty.value).toBeTruthy()
    })

    it("clears earlier errors when a later save succeeds", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGetDetail.mockResolvedValue(detail())
        const { loadDetail, save, saveErrors } = useCareerSelection()
        await loadDetail(100)
        mockUpdate.mockResolvedValue({ success: false, result: null, errors: ["Select a career direction."] })
        await save(100)
        mockUpdate.mockResolvedValue({ success: true, result: detail(), errors: [] })

        await save(100)

        expect(saveErrors.value).toStrictEqual([])
    })

    it("clears the saving flag once the save is done", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockUpdate.mockResolvedValue({ success: true, result: detail(), errors: [] })
        const { save, saving } = useCareerSelection()

        const pending = save(100)
        expect(saving.value).toBeTruthy()
        await pending

        expect(saving.value).toBeFalsy()
    })
})
