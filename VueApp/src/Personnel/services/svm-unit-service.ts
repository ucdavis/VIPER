import { useFetch } from "@/composables/ViperFetch"
import type { SVMUnitAPIResponse, SVMUnitNumberDTO } from "../types/svm-phone-types"

const { get, put, post, del } = useFetch()

/**
 * Service for SVMUnit and related API calls.
 */
class SVMUnitService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm`

    /**
     * Every unit on the list. The page renders all sections together, so these come back in one
     * request and are grouped by sectionId client-side. Returns null when the request fails, so a
     * caller can tell that apart from a list that genuinely has no units.
     */
    async getAllUnits(): Promise<SVMUnitAPIResponse[] | null> {
        const r = await get(`${this.baseUrl}/units`)
        if (!r.success || !r.result) {
            return null
        }
        return r.result as SVMUnitAPIResponse[]
    }

    async addUnitData(unitId: number, formData: SVMUnitNumberDTO) {
        return await post(`${this.baseUrl}/units/${unitId}`, formData)
    }

    async updateUnitData(unitId: number, formData: SVMUnitNumberDTO) {
        return await put(`${this.baseUrl}/units/${unitId}`, formData)
    }

    /**
     * Deletes one row of the SVM list. The server owns which underlying UnitPerson records that
     * covers, so the caller passes only the row key the table renders.
     */
    async deleteRow(entryId: number) {
        return await del(`${this.baseUrl}/rows/${entryId}`)
    }
}

const svmUnitService = new SVMUnitService()
export { svmUnitService }
