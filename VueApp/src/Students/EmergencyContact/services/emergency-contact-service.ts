import { useFetch, postForBlob, downloadBlob } from "@/composables/ViperFetch"
import type {
    StudentContactListItem,
    StudentContactDetail,
    UpdateStudentContactRequest,
    StudentContactReport,
    AppAccessStatus,
} from "../types"

const REVOKE_DELAY_MS = 1000

class EmergencyContactService {
    private baseUrl = `${import.meta.env.VITE_API_URL}students/emergency-contacts`

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

    getAccessStatus = async (): Promise<AppAccessStatus | null> => {
        const { get } = useFetch()
        const response = await get(`${this.baseUrl}/access/status`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as AppAccessStatus
    }

    toggleAppAccess = async (): Promise<boolean | null> => {
        const { post } = useFetch()
        const response = await post(`${this.baseUrl}/access/toggle-app`)
        if (!response.success) {
            return null
        }
        return response.result as boolean
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

    downloadExcel = async (): Promise<boolean> => {
        const { blob, filename } = await postForBlob(`${this.baseUrl}/export/excel`, {})
        if (blob.size === 0) {
            return false
        }
        downloadBlob(blob, filename ?? "emergency-contacts.xlsx")
        return true
    }

    openPdf = async (): Promise<boolean> => {
        const { blob } = await postForBlob(`${this.baseUrl}/export/pdf`, {})
        if (blob.size === 0) {
            return false
        }
        const url = globalThis.URL.createObjectURL(blob)
        globalThis.open(url, "_blank", "noopener")
        globalThis.setTimeout(() => globalThis.URL.revokeObjectURL(url), REVOKE_DELAY_MS)
        return true
    }
}

const emergencyContactService = new EmergencyContactService()
export { emergencyContactService }
