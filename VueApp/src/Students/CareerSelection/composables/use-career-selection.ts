import { ref, computed } from "vue"
import type { StudentCareerDetail, StudentInfo } from "../types"
import { careerSelectionService } from "../services/career-selection-service"

function emptyStudentInfo(): StudentInfo {
    return {
        direction: null,
        directionOther: "",
        primaryFocus: null,
        primaryFocusOther: "",
        secondaryFocus: null,
        secondaryFocusOther: "",
        mentorId: null,
        mentorName: "",
        mentorIamId: null,
        postGrad: null,
        shortTermPlans: "",
        longTermPlans: "",
    }
}

function useCareerSelection() {
    const loading = ref(false)
    const saving = ref(false)
    const detail = ref<StudentCareerDetail | null>(null)
    const saveErrors = ref<string[]>([])

    const studentInfo = ref<StudentInfo>(emptyStudentInfo()) // Form state

    const initialSnapshot = ref("") // Snapshot for dirty tracking

    function takeSnapshot(): string {
        return JSON.stringify({
            studentInfo: studentInfo.value,
        })
    }

    const currentSnapshot = computed(() => takeSnapshot())
    const isDirty = computed(() => initialSnapshot.value !== currentSnapshot.value)

    function populateForm(data: StudentCareerDetail): void {
        studentInfo.value = { ...data.studentInfo }
        initialSnapshot.value = takeSnapshot()
    }

    async function loadDetail(personId: number): Promise<void> {
        loading.value = true
        saveErrors.value = []
        const result = await careerSelectionService.getDetail(personId)
        detail.value = result
        if (result) {
            populateForm(result)
        }
        loading.value = false
    }

    async function save(personId: number): Promise<boolean> {
        saving.value = true
        saveErrors.value = []
        const response = await careerSelectionService.updateCareerSelection(personId, studentInfo.value)
        saving.value = false

        if (response.success && response.result) {
            detail.value = response.result
            populateForm(response.result)
            return true
        }

        saveErrors.value = response.errors
        return false
    }

    return {
        loading,
        saving,
        detail,
        saveErrors,
        studentInfo,
        isDirty,
        loadDetail,
        save,
    }
}

export { useCareerSelection }
