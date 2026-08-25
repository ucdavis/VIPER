import { useFetch } from "@/composables/ViperFetch"
import type { SVMFrequentNumberAPIResponse, SVMFrequentNumberRecord } from "../types/svm-phone-types"

const { get, post, put, del } = useFetch()

/**
 * Service for SVM Frequent Number API calls.
 */
class SVMFrequentNumberService {
    private baseUrl = `${import.meta.env.VITE_API_URL}phones/svm/frequentnumbers`

    async getFrequentNumbers(): Promise<SVMFrequentNumberAPIResponse[]> {
        const r = await get(this.baseUrl)

        const results = r.result
        if (!results || results.length === 0) {
            return []
        }
        return results as SVMFrequentNumberAPIResponse[]
    }

    async addFrequentNumber(formData: SVMFrequentNumberRecord) {
        return await post(this.baseUrl, formData)
    }

    async updateFrequentNumber(entryId: number, formData: SVMFrequentNumberRecord) {
        return await put(`${this.baseUrl}/${entryId}`, formData)
    }

    async deleteFrequentNumber(entryId: number) {
        return await del(`${this.baseUrl}/${entryId}`)
    }
}

const svmFrequentNumberService = new SVMFrequentNumberService()
export { svmFrequentNumberService }
