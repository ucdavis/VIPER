import { useFetch } from "@/composables/ViperFetch"
import { StudentAppService } from "@/Students/services/student-app-service"
import type {
    StudentContactListItem,
    StudentContactDetail,
    UpdateStudentContactRequest,
    StudentContactReport,
    AppAccessStatus,
} from "../types"

class EmergencyContactService extends StudentAppService<AppAccessStatus> {
    constructor() {
        super("students/emergency-contacts", {
            overviewExcel: "emergency-contact-overview.xlsx",
            excel: "emergency-contacts.xlsx",
        })
    }

    getList = async (): Promise<StudentContactListItem[]> => {
        const { get } = useFetch()
        const response = await get(this.baseUrl)
        if (!response.success || !response.result) {
            return []
        }
        return response.result as StudentContactListItem[]
    }

    getDetail = async (personId: number): Promise<StudentContactDetail | null> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/${personId}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as StudentContactDetail
    }

    updateContact = async (
        personId: number,
        data: UpdateStudentContactRequest,
    ): Promise<{ success: boolean; result: StudentContactDetail | null; errors: string[] }> => {
        const { put } = useFetch()
        const response = await put(`${this.baseUrl}/${personId}`, data)
        return {
            success: response.success,
            result: response.success ? (response.result as StudentContactDetail) : null,
            errors: response.errors ?? [],
        }
    }

    getReport = async (): Promise<StudentContactReport[]> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/report`)
        if (!response.success || !response.result) {
            return []
        }
        return response.result as StudentContactReport[]
    }

    toggleIndividualAccess = async (personId: number): Promise<boolean | null> => {
        const { post } = useFetch()
        const response = await post(`${this.baseUrl}/access/${personId}/toggle`)
        if (!response.success) {
            return null
        }
        return response.result as boolean
    }

    canEdit = async (personId: number): Promise<boolean> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/can-edit/${personId}`)
        if (!response.success) {
            return false
        }
        return response.result as boolean
    }
}

const emergencyContactService = new EmergencyContactService()
export { emergencyContactService }
