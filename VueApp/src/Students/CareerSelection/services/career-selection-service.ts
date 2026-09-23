import { useFetch } from "@/composables/ViperFetch"
import { StudentAppService } from "@/Students/services/student-app-service"
import type {
    StudentCareerListItem,
    StudentCareerDetail,
    StudentCareerReport,
    StudentInfo,
    CareerDropdownOption,
    CareerOptionType,
    CareerOptionSaveResult,
    CareerSelectionOption,
    MentorOption,
} from "../types"

const OPTION_TYPE_SLUGS: Record<CareerOptionType, string> = {
    career: "career",
    species: "species",
    postGrad: "post-grad",
}

class CareerSelectionService extends StudentAppService<boolean> {
    constructor() {
        super("students/career-selection", {
            overviewExcel: "career-selection-overview.xlsx",
            excel: "career-selection.xlsx",
        })
    }

    // Dropdown options, keyed on the option type slug. GET serves admins and students (only
    // admins get usage counts); the writes are admin-only.
    private optionsUrl(type: CareerOptionType, id?: number): string {
        const url = `${this.baseUrl}/options/${OPTION_TYPE_SLUGS[type]}`
        return id === undefined ? url : `${url}/${id}`
    }

    downloadOverviewCsv = (): Promise<boolean> =>
        this.downloadExportFile("export/overview/csv", "career-selection-overview.csv")

    downloadCsv = (): Promise<boolean> => this.downloadExportFile("export/csv", "career-selection.csv")

    // Returns null on a failed request so the page can tell an empty list from a load failure.
    getOptions = async (type: CareerOptionType): Promise<CareerSelectionOption[] | null> => {
        const { get } = useFetch()
        const response = await get(this.optionsUrl(type))
        if (!response.success || !Array.isArray(response.result)) {
            return null
        }
        return response.result as CareerSelectionOption[]
    }

    createOption = async (type: CareerOptionType, label: string): Promise<CareerOptionSaveResult> => {
        const { post } = useFetch()
        const response = await post(this.optionsUrl(type), { label })
        return { success: response.success, errors: response.errors ?? [] }
    }

    updateOption = async (type: CareerOptionType, id: number, label: string): Promise<CareerOptionSaveResult> => {
        const { put } = useFetch()
        const response = await put(this.optionsUrl(type, id), { label })
        return { success: response.success, errors: response.errors ?? [] }
    }

    deleteOption = async (type: CareerOptionType, id: number): Promise<CareerOptionSaveResult> => {
        const { del } = useFetch()
        const response = await del(this.optionsUrl(type, id))
        return { success: response.success, errors: response.errors ?? [] }
    }

    // The form's choices for one dropdown, in the server's order. Empty if the request fails.
    getDropdownOptions = async (type: CareerOptionType): Promise<CareerDropdownOption[]> => {
        const options = await this.getOptions(type)
        return (options ?? []).map((o) => ({ label: o.label, value: o.id, isOther: o.isOther }))
    }

    // Mentor picker lookup. Returns null on a failed request (vs an empty array) so the picker
    // can tell "no matches" from "the fetch failed".
    searchMentors = async (search: string): Promise<MentorOption[] | null> => {
        const { get, createUrlSearchParams } = useFetch()
        const response = await get(`${this.baseUrl}/mentors?${createUrlSearchParams({ search })}`)
        return response.success ? (response.result as MentorOption[]) : null
    }

    getList = async (): Promise<StudentCareerListItem[]> => {
        const { get } = useFetch()
        const response = await get(this.baseUrl)
        if (!response.success || !response.result) {
            return []
        }
        return response.result as StudentCareerListItem[]
    }

    getDetail = async (personId: number): Promise<StudentCareerDetail | null> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/${personId}`)
        if (!response.success || !response.result) {
            return null
        }
        if (response.result?.studentInfo?.shortTermPlans === null) {
            response.result.studentInfo.shortTermPlans = ""
        }
        if (response.result?.studentInfo?.longTermPlans === null) {
            response.result.studentInfo.longTermPlans = ""
        }
        return response.result as StudentCareerDetail
    }

    updateCareerSelection = async (
        personId: number,
        data: StudentInfo,
    ): Promise<{ success: boolean; result: StudentCareerDetail | null; errors: string[] }> => {
        const { put } = useFetch()
        const response = await put(`${this.baseUrl}/${personId}`, data)
        return {
            success: response.success,
            result: response.success ? (response.result as StudentCareerDetail) : null,
            errors: response.errors ?? [],
        }
    }

    getReport = async (): Promise<StudentCareerReport[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/report`)
        if (!response.success || !response.result) {
            return []
        }
        return response.result as StudentCareerReport[]
    }
}

const careerSelectionService = new CareerSelectionService()
export { careerSelectionService }
