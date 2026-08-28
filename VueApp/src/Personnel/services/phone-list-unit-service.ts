import { useFetch } from "@/composables/ViperFetch"
import type { PhoneListUnitAPIResponse, PhoneListUnitPersonDTO } from "../types/phone-list-phone-types"

const { get, post, put, del } = useFetch()

/**
 * Service for PhoneListUnit. Every call is scoped to a list code, which the API resolves to a
 * list and then uses for its own permission check.
 */
class PhoneListUnitService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/phonelist`

    private listUrl(code: string) {
        return `${this.baseUrl}/${encodeURIComponent(code)}`
    }

    async getUnitsByList(code: string): Promise<PhoneListUnitAPIResponse[]> {
        const r = await get(`${this.listUrl(code)}/units`)
        const results = r.result
        if (!results || results.length === 0) {
            return []
        }
        return results as PhoneListUnitAPIResponse[]
    }

    async addUnitPersonData(code: string, formData: PhoneListUnitPersonDTO) {
        return await post(`${this.listUrl(code)}/unitPerson`, formData)
    }

    async updateUnitPersonData(code: string, unitPersonId: number, formData: PhoneListUnitPersonDTO) {
        return await put(`${this.listUrl(code)}/unitPerson/${unitPersonId}`, formData)
    }

    async deleteUnitPersonData(code: string, deletionUnitPersonId: number) {
        return await del(`${this.listUrl(code)}/unitPerson/${deletionUnitPersonId}`)
    }
}

const phoneListUnitService = new PhoneListUnitService()
export { phoneListUnitService }
